using System;
using UnityEngine;
using PetGrooming.Core;
using PetGrooming.AI;

namespace PetGrooming.Systems.Skills
{
    /// <summary>
    /// Fur Distraction skill for Cat pets.
    /// Throws a fur ball that blocks Groomer vision for 2 seconds.
    /// Requirement 4.3: Fur ball blocks vision for 2 seconds with 10 second cooldown.
    /// </summary>
    public class FurDistractionSkill : SkillBase
    {
        #region Serialized Fields
        [Header("Fur Distraction Settings")]
        [Tooltip("Speed of the fur ball projectile")]
        public float ProjectileSpeed = 10f;
        
        [Tooltip("Duration of the vision block effect in seconds")]
        public float VisionBlockDuration = 2f;
        
        [Tooltip("Maximum range of the projectile")]
        public float MaxRange = 12f;
        
        [Tooltip("Prefab for the fur ball projectile")]
        public GameObject FurBallPrefab;
        
        [Header("Visual Effect Settings")]
        [Tooltip("Prefab for the vision block effect on the Groomer")]
        public GameObject VisionBlockEffectPrefab;
        
        [Header("Configuration")]
        [Tooltip("Phase 2 game configuration")]
        public Phase2GameConfig GameConfig;
        #endregion

        #region Private Fields
        private PetAI _ownerPet;
        private Transform _ownerTransform;
        #endregion

        #region Events
        /// <summary>
        /// Fired when the fur ball is thrown.
        /// </summary>
        public event Action OnFurBallThrown;
        
        /// <summary>
        /// Fired when the fur ball hits the Groomer.
        /// </summary>
        public event Action<GroomerController> OnGroomerHit;
        
        /// <summary>
        /// Fired when the vision block effect ends.
        /// </summary>
        public event Action OnVisionBlockEnded;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            
            SkillName = "Fur Distraction";
            
            // Apply config values if available
            if (GameConfig != null)
            {
                Cooldown = GameConfig.FurDistractionCooldown;
                VisionBlockDuration = GameConfig.FurDistractionDuration;
            }
            else
            {
                // Default cooldown: 10 seconds (Requirement 4.3)
                Cooldown = 10f;
                VisionBlockDuration = 2f;
            }
            
            _ownerTransform = transform;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Sets the owner pet for this skill.
        /// </summary>
        public void SetOwner(PetAI pet)
        {
            _ownerPet = pet;
            _ownerTransform = pet != null ? pet.transform : transform;
        }

        /// <summary>
        /// Activates the Fur Distraction skill.
        /// Requirement 4.3: Throws a fur ball that blocks Groomer vision for 2 seconds.
        /// </summary>
        public override void Activate()
        {
            base.Activate();
            ThrowFurBall();
        }

        /// <summary>
        /// Called when the fur ball hits the Groomer.
        /// Applies the vision block effect.
        /// </summary>
        /// <param name="groomer">The Groomer that was hit</param>
        public void OnProjectileHit(GroomerController groomer)
        {
            if (groomer == null) return;
            
            ApplyVisionBlockEffect(groomer, VisionBlockDuration);
            
            // Add mischief for pet skill hitting Groomer
            // Property 18: Pet Skill Hit Mischief Value
            // Requirement 6.6: Pet skill hit adds 30 points
            if (MischiefSystem.Instance != null)
            {
                MischiefSystem.Instance.AddPetSkillHitMischief();
            }
            
            OnGroomerHit?.Invoke(groomer);
            
            Debug.Log($"[FurDistractionSkill] Hit Groomer, blocking vision for {VisionBlockDuration}s");
        }
        #endregion

        #region Private Methods
        private void ThrowFurBall()
        {
            Vector3 launchPosition = _ownerTransform.position + Vector3.up * 0.5f;
            
            // Find the Groomer to aim at
            GroomerController groomer = FindObjectOfType<GroomerController>();
            Vector3 targetDirection;
            
            if (groomer != null)
            {
                targetDirection = (groomer.transform.position - launchPosition).normalized;
            }
            else
            {
                targetDirection = _ownerTransform.forward;
            }
            
            if (FurBallPrefab != null)
            {
                GameObject projectile = Instantiate(FurBallPrefab, launchPosition, Quaternion.LookRotation(targetDirection));
                
                // Initialize the projectile
                FurBallProjectile furBall = projectile.GetComponent<FurBallProjectile>();
                if (furBall != null)
                {
                    furBall.Initialize(this, targetDirection, ProjectileSpeed, MaxRange);
                }
                else
                {
                    // Fallback: Add basic movement
                    Rigidbody rb = projectile.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.linearVelocity = targetDirection * ProjectileSpeed;
                    }
                    
                    // Destroy after max range time
                    Destroy(projectile, MaxRange / ProjectileSpeed);
                }
            }
            else
            {
                // No prefab - do raycast-based hit detection
                PerformRaycastHit(launchPosition, targetDirection);
            }
            
            OnFurBallThrown?.Invoke();
            Debug.Log("[FurDistractionSkill] Fur ball thrown");
        }

        private void PerformRaycastHit(Vector3 origin, Vector3 direction)
        {
            RaycastHit hit;
            if (Physics.Raycast(origin, direction, out hit, MaxRange))
            {
                GroomerController groomer = hit.collider.GetComponent<GroomerController>();
                if (groomer == null)
                {
                    groomer = hit.collider.GetComponentInParent<GroomerController>();
                }
                
                if (groomer != null)
                {
                    OnProjectileHit(groomer);
                }
            }
        }
        #endregion

        #region Static Methods (Testable)
        /// <summary>
        /// Applies the vision block effect to the Groomer.
        /// Requirement 4.3: Blocks Groomer vision for 2 seconds.
        /// </summary>
        /// <param name="groomer">The Groomer to affect</param>
        /// <param name="duration">Duration of the effect in seconds</param>
        public static void ApplyVisionBlockEffect(GroomerController groomer, float duration)
        {
            if (groomer == null) return;
            
            // Create a vision block effect data
            // This could be implemented as a UI overlay or post-processing effect
            // For now, we'll use the effect receiver interface if available
            IEffectReceiver effectReceiver = groomer.GetComponent<IEffectReceiver>();
            if (effectReceiver != null)
            {
                // Use a custom effect type or stun-like effect for vision block
                var effect = new SkillEffectData(SkillEffectType.Stun, 0.5f, duration, "Fur Distraction");
                effectReceiver.ApplyEffect(effect);
            }
            
            Debug.Log($"[FurDistractionSkill] Applied vision block effect for {duration}s");
        }

        /// <summary>
        /// Validates the vision block duration matches requirements.
        /// Requirement 4.3: 2 second vision block.
        /// </summary>
        /// <param name="duration">The duration to validate</param>
        /// <returns>True if duration matches requirement</returns>
        public static bool ValidateVisionBlockDuration(float duration)
        {
            const float RequiredDuration = 2f;
            const float Tolerance = 0.001f;
            return Mathf.Abs(duration - RequiredDuration) < Tolerance;
        }

        /// <summary>
        /// Validates the cooldown matches requirements.
        /// Requirement 4.3: 10 second cooldown.
        /// </summary>
        /// <param name="cooldown">The cooldown to validate</param>
        /// <returns>True if cooldown matches requirement</returns>
        public static bool ValidateCooldown(float cooldown)
        {
            const float RequiredCooldown = 10f;
            const float Tolerance = 0.001f;
            return Mathf.Abs(cooldown - RequiredCooldown) < Tolerance;
        }

        /// <summary>
        /// Calculates the projectile travel time.
        /// </summary>
        /// <param name="distance">Distance to target</param>
        /// <param name="speed">Projectile speed</param>
        /// <returns>Travel time in seconds</returns>
        public static float CalculateTravelTime(float distance, float speed)
        {
            if (speed <= 0f) return float.MaxValue;
            return distance / speed;
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
                Cooldown = config.FurDistractionCooldown;
                VisionBlockDuration = config.FurDistractionDuration;
            }
        }
#endif

        private void OnDrawGizmosSelected()
        {
            // Draw max range
            Gizmos.color = Color.magenta;
            Vector3 start = transform.position + Vector3.up * 0.5f;
            Gizmos.DrawLine(start, start + transform.forward * MaxRange);
            Gizmos.DrawWireSphere(start + transform.forward * MaxRange, 0.3f);
        }
        #endregion
    }

    /// <summary>
    /// Projectile component for the fur ball.
    /// </summary>
    public class FurBallProjectile : MonoBehaviour
    {
        private FurDistractionSkill _sourceSkill;
        private Vector3 _direction;
        private float _speed;
        private float _maxRange;
        private Vector3 _startPosition;
        private bool _hasHit;

        public void Initialize(FurDistractionSkill skill, Vector3 direction, float speed, float maxRange)
        {
            _sourceSkill = skill;
            _direction = direction.normalized;
            _speed = speed;
            _maxRange = maxRange;
            _startPosition = transform.position;
            _hasHit = false;
        }

        private void Update()
        {
            if (_hasHit) return;
            
            // Move projectile
            transform.position += _direction * _speed * Time.deltaTime;
            
            // Check if exceeded max range
            float distanceTraveled = Vector3.Distance(_startPosition, transform.position);
            if (distanceTraveled >= _maxRange)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hasHit) return;
            
            GroomerController groomer = other.GetComponent<GroomerController>();
            if (groomer == null)
            {
                groomer = other.GetComponentInParent<GroomerController>();
            }
            
            if (groomer != null)
            {
                _hasHit = true;
                _sourceSkill?.OnProjectileHit(groomer);
                Destroy(gameObject);
            }
        }
    }
}
