using System;
using UnityEngine;
using PetGrooming.Core;
using PetGrooming.AI;

namespace PetGrooming.Systems.Skills
{
    /// <summary>
    /// Calming Spray skill for the Groomer.
    /// Creates an area effect that stuns pets for 1 second.
    /// Requirements: 3.7, 3.8
    /// </summary>
    public class CalmingSpraySkill : SkillBase
    {
        #region Serialized Fields
        [Header("Calming Spray Settings")]
        [Tooltip("Radius of the spray effect")]
        public float EffectRadius = 3f;
        
        [Tooltip("Duration of the stun effect in seconds")]
        public float StunDuration = 1f;
        
        [Header("Visual")]
        [Tooltip("Particle system for spray effect")]
        public ParticleSystem SprayEffect;
        
        [Tooltip("Duration of the spray visual")]
        public float SprayVisualDuration = 0.5f;
        
        [Header("Configuration")]
        [Tooltip("Phase 2 game configuration")]
        public Phase2GameConfig GameConfig;
        
        [Header("Layer Settings")]
        [Tooltip("Layer mask for pet detection")]
        public LayerMask PetLayerMask = -1;
        #endregion

        #region Private Fields
        private Transform _ownerTransform;
        #endregion

        #region Events
        /// <summary>
        /// Fired when the spray is activated.
        /// </summary>
        public event Action OnSprayActivated;
        
        /// <summary>
        /// Fired when a pet is stunned by the spray.
        /// </summary>
        public event Action<PetAI> OnPetStunned;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            
            SkillName = "Calming Spray";
            
            // Apply config values if available
            if (GameConfig != null)
            {
                Cooldown = GameConfig.CalmingSprayCooldown;
                EffectRadius = GameConfig.CalmingSprayRadius;
                StunDuration = GameConfig.CalmingSprayStunDuration;
            }
            else
            {
                // Default cooldown: 13 seconds (Requirement 3.8)
                Cooldown = 13f;
            }
            
            _ownerTransform = transform;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Sets the owner transform for effect origin.
        /// </summary>
        public void SetOwner(Transform owner)
        {
            _ownerTransform = owner;
        }

        /// <summary>
        /// Activates the Calming Spray skill.
        /// Requirement 3.7: Creates an area effect that stuns pets for 1 second.
        /// </summary>
        public override void Activate()
        {
            base.Activate();
            ReleaseSpray();
        }

        /// <summary>
        /// Gets all pets within the effect radius.
        /// </summary>
        /// <returns>Array of pets in range</returns>
        public PetAI[] GetPetsInRange()
        {
            Vector3 center = _ownerTransform != null ? _ownerTransform.position : transform.position;
            return FindPetsInRadius(center, EffectRadius, PetLayerMask);
        }
        #endregion

        #region Private Methods
        private void ReleaseSpray()
        {
            Vector3 center = _ownerTransform != null ? _ownerTransform.position : transform.position;
            
            // Play visual effect
            if (SprayEffect != null)
            {
                SprayEffect.transform.position = center;
                SprayEffect.Play();
            }
            
            OnSprayActivated?.Invoke();
            
            // Find and stun all pets in range
            PetAI[] petsInRange = FindPetsInRadius(center, EffectRadius, PetLayerMask);
            ApplyStunToAllPets(petsInRange, StunDuration);
            
            Debug.Log($"[CalmingSpraySkill] Spray released, stunned {petsInRange.Length} pets for {StunDuration}s");
        }

        private void ApplyStunToAllPets(PetAI[] pets, float duration)
        {
            foreach (PetAI pet in pets)
            {
                if (pet != null)
                {
                    ApplyStunEffect(pet, duration);
                    OnPetStunned?.Invoke(pet);
                }
            }
        }
        #endregion

        #region Static Methods (Testable)
        /// <summary>
        /// Finds all pets within a radius.
        /// </summary>
        /// <param name="center">Center of the search area</param>
        /// <param name="radius">Search radius</param>
        /// <param name="layerMask">Layer mask for filtering</param>
        /// <returns>Array of pets found</returns>
        public static PetAI[] FindPetsInRadius(Vector3 center, float radius, LayerMask layerMask)
        {
            Collider[] colliders = Physics.OverlapSphere(center, radius, layerMask);
            
            // Count pets first
            int petCount = 0;
            foreach (Collider col in colliders)
            {
                PetAI pet = col.GetComponent<PetAI>();
                if (pet == null)
                {
                    pet = col.GetComponentInParent<PetAI>();
                }
                if (pet != null)
                {
                    petCount++;
                }
            }
            
            // Create array and populate
            PetAI[] pets = new PetAI[petCount];
            int index = 0;
            foreach (Collider col in colliders)
            {
                PetAI pet = col.GetComponent<PetAI>();
                if (pet == null)
                {
                    pet = col.GetComponentInParent<PetAI>();
                }
                if (pet != null && index < pets.Length)
                {
                    // Avoid duplicates
                    bool isDuplicate = false;
                    for (int i = 0; i < index; i++)
                    {
                        if (pets[i] == pet)
                        {
                            isDuplicate = true;
                            break;
                        }
                    }
                    
                    if (!isDuplicate)
                    {
                        pets[index++] = pet;
                    }
                }
            }
            
            // Resize if we had duplicates
            if (index < pets.Length)
            {
                PetAI[] resized = new PetAI[index];
                Array.Copy(pets, resized, index);
                return resized;
            }
            
            return pets;
        }

        /// <summary>
        /// Applies stun effect to a pet.
        /// Property 10: Calming Spray Stun Effect
        /// Requirement 3.7: Stuns pets for 1 second.
        /// </summary>
        /// <param name="pet">The pet to stun</param>
        /// <param name="duration">Stun duration in seconds</param>
        public static void ApplyStunEffect(PetAI pet, float duration)
        {
            if (pet == null) return;
            
            // Create stun effect data
            SkillEffectData stunEffect = SkillEffectData.CreateStun(duration, "Calming Spray");
            
            // Apply to pet
            IEffectReceiver effectReceiver = pet.GetComponent<IEffectReceiver>();
            if (effectReceiver != null)
            {
                effectReceiver.ApplyEffect(stunEffect);
            }
            else
            {
                Debug.Log($"[CalmingSpraySkill] Applied stun effect for {duration}s");
            }
        }

        /// <summary>
        /// Checks if a position is within the effect radius.
        /// </summary>
        /// <param name="center">Center of the effect</param>
        /// <param name="targetPosition">Position to check</param>
        /// <param name="radius">Effect radius</param>
        /// <returns>True if within radius</returns>
        public static bool IsInEffectRadius(Vector3 center, Vector3 targetPosition, float radius)
        {
            float distance = Vector3.Distance(center, targetPosition);
            return distance <= radius;
        }

        /// <summary>
        /// Validates if the stun effect parameters are correct per requirements.
        /// Property 10: Calming Spray Stun Effect
        /// </summary>
        /// <param name="stunDuration">The stun duration to validate</param>
        /// <returns>True if parameters match requirements</returns>
        public static bool ValidateStunEffectParameters(float stunDuration)
        {
            // Requirement 3.7: Stuns pets for 1 second
            const float RequiredDuration = 1f;
            const float Tolerance = 0.001f;
            
            return Mathf.Abs(stunDuration - RequiredDuration) < Tolerance;
        }

        /// <summary>
        /// Calculates the number of pets that would be affected.
        /// </summary>
        /// <param name="petPositions">Array of pet positions</param>
        /// <param name="center">Center of the effect</param>
        /// <param name="radius">Effect radius</param>
        /// <returns>Number of pets in range</returns>
        public static int CalculateAffectedPetCount(Vector3[] petPositions, Vector3 center, float radius)
        {
            int count = 0;
            foreach (Vector3 pos in petPositions)
            {
                if (IsInEffectRadius(center, pos, radius))
                {
                    count++;
                }
            }
            return count;
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
                Cooldown = config.CalmingSprayCooldown;
                EffectRadius = config.CalmingSprayRadius;
                StunDuration = config.CalmingSprayStunDuration;
            }
        }
        
        /// <summary>
        /// Sets effect parameters for testing.
        /// </summary>
        public void SetEffectParametersForTesting(float radius, float duration)
        {
            EffectRadius = radius;
            StunDuration = duration;
        }
#endif

        private void OnDrawGizmosSelected()
        {
            // Draw effect radius
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Vector3 center = _ownerTransform != null ? _ownerTransform.position : transform.position;
            Gizmos.DrawSphere(center, EffectRadius);
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(center, EffectRadius);
        }
        #endregion
    }
}
