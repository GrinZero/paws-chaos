using System;
using System.Collections;
using UnityEngine;
using PetGrooming.Core;
using PetGrooming.AI;

namespace PetGrooming.Systems.Skills
{
    /// <summary>
    /// Leash skill for the Groomer.
    /// Fires a hook that pulls a hit pet toward the Groomer.
    /// Pet has a chance to break free (Cat 60%, Dog 40%).
    /// Requirements: 3.4, 3.5, 3.6
    /// </summary>
    public class LeashSkill : SkillBase
    {
        #region Enums
        /// <summary>
        /// Pet type for break free chance calculation.
        /// </summary>
        public enum PetType { Cat, Dog }
        #endregion

        #region Serialized Fields
        [Header("Leash Settings")]
        [Tooltip("Maximum range of the leash")]
        public float MaxRange = 10f;
        
        [Tooltip("Speed at which the pet is pulled")]
        public float PullSpeed = 8f;
        
        [Tooltip("Chance for Cat to break free (0.6 = 60%)")]
        [Range(0f, 1f)]
        public float CatBreakFreeChance = 0.6f;
        
        [Tooltip("Chance for Dog to break free (0.4 = 40%)")]
        [Range(0f, 1f)]
        public float DogBreakFreeChance = 0.4f;
        
        [Header("Visual")]
        [Tooltip("Line renderer for leash visual")]
        public LineRenderer LeashVisual;
        
        [Tooltip("Duration of the pull animation")]
        public float PullDuration = 0.5f;
        
        [Header("References")]
        [Tooltip("Transform from which the leash is fired")]
        public Transform LaunchPoint;
        
        [Header("Configuration")]
        [Tooltip("Phase 2 game configuration")]
        public Phase2GameConfig GameConfig;
        #endregion

        #region Private Fields
        private Transform _ownerTransform;
        private bool _isPulling;
        private PetAI _targetPet;
        #endregion

        #region Events
        /// <summary>
        /// Fired when the leash hits a pet.
        /// </summary>
        public event Action<PetAI> OnLeashHit;
        
        /// <summary>
        /// Fired when a pet breaks free from the leash.
        /// </summary>
        public event Action<PetAI> OnPetBreakFree;
        
        /// <summary>
        /// Fired when the pull is complete.
        /// </summary>
        public event Action<PetAI> OnPullComplete;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            
            SkillName = "Leash";
            
            // Apply config values if available
            if (GameConfig != null)
            {
                Cooldown = GameConfig.LeashCooldown;
                MaxRange = GameConfig.LeashMaxRange;
                PullSpeed = GameConfig.LeashPullSpeed;
                CatBreakFreeChance = GameConfig.LeashCatBreakFreeChance;
                DogBreakFreeChance = GameConfig.LeashDogBreakFreeChance;
            }
            else
            {
                // Default cooldown: 12 seconds (Requirement 3.6)
                Cooldown = 12f;
            }
            
            _ownerTransform = transform;
        }

        private void Start()
        {
            if (LaunchPoint == null)
            {
                LaunchPoint = _ownerTransform;
            }
            
            // Initialize line renderer if present
            if (LeashVisual != null)
            {
                LeashVisual.enabled = false;
            }
        }

        protected override void Update()
        {
            base.Update();
            
            // Update leash visual during pull
            if (_isPulling && LeashVisual != null && _targetPet != null)
            {
                UpdateLeashVisual();
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Sets the owner transform for direction calculation.
        /// </summary>
        public void SetOwner(Transform owner)
        {
            _ownerTransform = owner;
            if (LaunchPoint == null)
            {
                LaunchPoint = owner;
            }
        }

        /// <summary>
        /// Checks if the skill can be activated.
        /// Cannot activate while already pulling.
        /// </summary>
        public override bool CanActivate()
        {
            return base.CanActivate() && !_isPulling;
        }

        /// <summary>
        /// Activates the Leash skill, firing a hook.
        /// Requirement 3.4: Fires a hook that pulls a hit pet toward the Groomer.
        /// </summary>
        public override void Activate()
        {
            base.Activate();
            FireLeash();
        }

        /// <summary>
        /// Gets the break free chance for a specific pet type.
        /// Property 9: Leash Break Free Chance By Pet Type
        /// Requirement 3.5: Cat 60%, Dog 40%
        /// </summary>
        /// <param name="petType">The type of pet</param>
        /// <returns>Break free chance (0-1)</returns>
        public float GetBreakFreeChance(PetType petType)
        {
            return CalculateBreakFreeChance(petType, CatBreakFreeChance, DogBreakFreeChance);
        }
        #endregion

        #region Private Methods
        private void FireLeash()
        {
            Vector3 origin = LaunchPoint != null ? LaunchPoint.position : _ownerTransform.position;
            Vector3 direction = _ownerTransform.forward;
            
            // Raycast to find pet
            RaycastHit hit;
            if (Physics.Raycast(origin, direction, out hit, MaxRange))
            {
                PetAI pet = hit.collider.GetComponent<PetAI>();
                if (pet == null)
                {
                    pet = hit.collider.GetComponentInParent<PetAI>();
                }
                
                if (pet != null)
                {
                    OnLeashHitPet(pet);
                    return;
                }
            }
            
            // Also check with sphere cast for better hit detection
            RaycastHit[] hits = Physics.SphereCastAll(origin, 0.5f, direction, MaxRange);
            foreach (var sphereHit in hits)
            {
                PetAI pet = sphereHit.collider.GetComponent<PetAI>();
                if (pet == null)
                {
                    pet = sphereHit.collider.GetComponentInParent<PetAI>();
                }
                
                if (pet != null)
                {
                    OnLeashHitPet(pet);
                    return;
                }
            }
            
            Debug.Log("[LeashSkill] No target hit");
        }

        private void OnLeashHitPet(PetAI pet)
        {
            _targetPet = pet;
            OnLeashHit?.Invoke(pet);
            
            // Determine pet type and check break free
            PetType petType = DeterminePetType(pet);
            bool brokeFreee = TryBreakFree(petType);
            
            if (brokeFreee)
            {
                Debug.Log($"[LeashSkill] Pet broke free! (Type: {petType})");
                OnPetBreakFree?.Invoke(pet);
                _targetPet = null;
            }
            else
            {
                Debug.Log($"[LeashSkill] Pet caught! Starting pull (Type: {petType})");
                StartCoroutine(PullPetCoroutine(pet));
            }
        }

        private PetType DeterminePetType(PetAI pet)
        {
            // Check if pet has a type indicator
            // This could be expanded based on actual PetAI implementation
            string petName = pet.gameObject.name.ToLower();
            if (petName.Contains("dog"))
            {
                return PetType.Dog;
            }
            
            // Default to Cat
            return PetType.Cat;
        }

        private bool TryBreakFree(PetType petType)
        {
            float breakFreeChance = GetBreakFreeChance(petType);
            float roll = UnityEngine.Random.value;
            return roll < breakFreeChance;
        }

        private IEnumerator PullPetCoroutine(PetAI pet)
        {
            _isPulling = true;
            
            if (LeashVisual != null)
            {
                LeashVisual.enabled = true;
            }
            
            Vector3 startPosition = pet.transform.position;
            Vector3 targetPosition = _ownerTransform.position + _ownerTransform.forward * 1.5f;
            
            float elapsed = 0f;
            float distance = Vector3.Distance(startPosition, targetPosition);
            float duration = distance / PullSpeed;
            
            while (elapsed < duration && pet != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Smooth pull
                pet.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                
                yield return null;
            }
            
            _isPulling = false;
            
            if (LeashVisual != null)
            {
                LeashVisual.enabled = false;
            }
            
            if (pet != null)
            {
                OnPullComplete?.Invoke(pet);
            }
            
            _targetPet = null;
        }

        private void UpdateLeashVisual()
        {
            if (LeashVisual == null || _targetPet == null) return;
            
            Vector3 start = LaunchPoint != null ? LaunchPoint.position : _ownerTransform.position;
            Vector3 end = _targetPet.transform.position;
            
            LeashVisual.SetPosition(0, start);
            LeashVisual.SetPosition(1, end);
        }
        #endregion

        #region Static Methods (Testable)
        /// <summary>
        /// Calculates the break free chance based on pet type.
        /// Property 9: Leash Break Free Chance By Pet Type
        /// Requirement 3.5: Cat 60%, Dog 40%
        /// </summary>
        /// <param name="petType">The type of pet</param>
        /// <param name="catChance">Break free chance for cats</param>
        /// <param name="dogChance">Break free chance for dogs</param>
        /// <returns>Break free chance (0-1)</returns>
        public static float CalculateBreakFreeChance(PetType petType, float catChance, float dogChance)
        {
            return petType == PetType.Cat ? catChance : dogChance;
        }

        /// <summary>
        /// Determines if a pet breaks free based on random roll.
        /// </summary>
        /// <param name="breakFreeChance">The chance to break free (0-1)</param>
        /// <param name="randomValue">Random value (0-1) for deterministic testing</param>
        /// <returns>True if the pet breaks free</returns>
        public static bool DetermineBreakFree(float breakFreeChance, float randomValue)
        {
            return randomValue < breakFreeChance;
        }

        /// <summary>
        /// Validates if the break free chances match requirements.
        /// Property 9: Leash Break Free Chance By Pet Type
        /// </summary>
        /// <param name="catChance">Cat break free chance</param>
        /// <param name="dogChance">Dog break free chance</param>
        /// <returns>True if chances match requirements</returns>
        public static bool ValidateBreakFreeChances(float catChance, float dogChance)
        {
            // Requirement 3.5: Cat 60%, Dog 40%
            const float RequiredCatChance = 0.6f;
            const float RequiredDogChance = 0.4f;
            const float Tolerance = 0.001f;
            
            return Mathf.Abs(catChance - RequiredCatChance) < Tolerance &&
                   Mathf.Abs(dogChance - RequiredDogChance) < Tolerance;
        }

        /// <summary>
        /// Calculates the pull duration based on distance and speed.
        /// </summary>
        /// <param name="distance">Distance to pull</param>
        /// <param name="pullSpeed">Speed of the pull</param>
        /// <returns>Duration in seconds</returns>
        public static float CalculatePullDuration(float distance, float pullSpeed)
        {
            if (pullSpeed <= 0f) return 0f;
            return distance / pullSpeed;
        }
        #endregion

        #region Editor Support
#if UNITY_EDITOR
        /// <summary>
        /// Sets config for testing purposes.
        /// </summary>
        public void SetConfigForTesting(Phase2GameConfig config)
        {
            GameConfig = config;
            if (config != null)
            {
                Cooldown = config.LeashCooldown;
                MaxRange = config.LeashMaxRange;
                PullSpeed = config.LeashPullSpeed;
                CatBreakFreeChance = config.LeashCatBreakFreeChance;
                DogBreakFreeChance = config.LeashDogBreakFreeChance;
            }
        }
        
        /// <summary>
        /// Sets break free chances for testing.
        /// </summary>
        public void SetBreakFreeChancesForTesting(float catChance, float dogChance)
        {
            CatBreakFreeChance = catChance;
            DogBreakFreeChance = dogChance;
        }
#endif

        private void OnDrawGizmosSelected()
        {
            // Draw max range
            Gizmos.color = Color.blue;
            Vector3 start = LaunchPoint != null ? LaunchPoint.position : transform.position;
            Gizmos.DrawLine(start, start + transform.forward * MaxRange);
            
            // Draw range sphere
            Gizmos.color = new Color(0f, 0f, 1f, 0.2f);
            Gizmos.DrawWireSphere(start, MaxRange);
        }
        #endregion
    }
}
