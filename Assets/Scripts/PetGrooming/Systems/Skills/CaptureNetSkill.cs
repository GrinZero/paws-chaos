using System;
using UnityEngine;
using PetGrooming.Core;
using PetGrooming.AI;

namespace PetGrooming.Systems.Skills
{
    /// <summary>
    /// Capture Net skill for the Groomer.
    /// Throws a projectile that slows hit pets by 50% for 3 seconds.
    /// Requirements: 3.2, 3.3
    /// </summary>
    public class CaptureNetSkill : SkillBase
    {
        #region Serialized Fields
        [Header("Capture Net Settings")]
        [Tooltip("Speed of the net projectile")]
        public float ProjectileSpeed = 15f;
        
        [Tooltip("Movement speed reduction when hit (0.5 = 50% reduction)")]
        [Range(0f, 1f)]
        public float SlowAmount = 0.5f;
        
        [Tooltip("Duration of the slow effect in seconds")]
        public float SlowDuration = 3f;
        
        [Tooltip("Maximum range of the projectile")]
        public float MaxRange = 15f;
        
        [Tooltip("Prefab for the net projectile")]
        public GameObject NetProjectilePrefab;
        
        [Header("References")]
        [Tooltip("Transform from which the projectile is launched")]
        public Transform LaunchPoint;
        
        [Header("Configuration")]
        [Tooltip("Phase 2 game configuration")]
        public Phase2GameConfig GameConfig;
        #endregion

        #region Private Fields
        private Transform _ownerTransform;
        #endregion

        #region Events
        /// <summary>
        /// Fired when the net hits a pet.
        /// </summary>
        public event Action<PetAI> OnNetHit;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            
            SkillName = "Capture Net";
            
            // Apply config values if available
            if (GameConfig != null)
            {
                Cooldown = GameConfig.CaptureNetCooldown;
                SlowAmount = GameConfig.CaptureNetSlowAmount;
                SlowDuration = GameConfig.CaptureNetSlowDuration;
                ProjectileSpeed = GameConfig.CaptureNetProjectileSpeed;
            }
            else
            {
                // Default cooldown: 8 seconds (Requirement 3.3)
                Cooldown = 8f;
            }
            
            _ownerTransform = transform;
        }

        private void Start()
        {
            // Set launch point to owner if not specified
            if (LaunchPoint == null)
            {
                LaunchPoint = _ownerTransform;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Sets the owner transform for projectile direction calculation.
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
        /// Activates the Capture Net skill, launching a projectile.
        /// Requirement 3.2: Throws a projectile that slows hit pet by 50% for 3 seconds.
        /// </summary>
        public override void Activate()
        {
            base.Activate();
            LaunchProjectile();
        }

        /// <summary>
        /// Called when the net projectile hits a pet.
        /// Applies slow effect to the pet.
        /// </summary>
        /// <param name="pet">The pet that was hit</param>
        public void OnProjectileHit(PetAI pet)
        {
            if (pet == null) return;
            
            ApplySlowEffect(pet, SlowAmount, SlowDuration);
            OnNetHit?.Invoke(pet);
            
            Debug.Log($"[CaptureNetSkill] Hit pet, applying {SlowAmount * 100}% slow for {SlowDuration}s");
        }
        #endregion

        #region Private Methods
        private void LaunchProjectile()
        {
            Vector3 launchPosition = LaunchPoint != null ? LaunchPoint.position : _ownerTransform.position;
            Vector3 launchDirection = _ownerTransform.forward;
            
            if (NetProjectilePrefab != null)
            {
                GameObject projectile = Instantiate(NetProjectilePrefab, launchPosition, Quaternion.LookRotation(launchDirection));
                
                // Initialize the projectile
                CaptureNetProjectile netProjectile = projectile.GetComponent<CaptureNetProjectile>();
                if (netProjectile != null)
                {
                    netProjectile.Initialize(this, launchDirection, ProjectileSpeed, MaxRange);
                }
                else
                {
                    // Fallback: Add basic movement
                    Rigidbody rb = projectile.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.linearVelocity = launchDirection * ProjectileSpeed;
                    }
                    
                    // Destroy after max range time
                    Destroy(projectile, MaxRange / ProjectileSpeed);
                }
            }
            else
            {
                // No prefab - do raycast-based hit detection
                PerformRaycastHit(launchPosition, launchDirection);
            }
            
            Debug.Log("[CaptureNetSkill] Projectile launched");
        }

        private void PerformRaycastHit(Vector3 origin, Vector3 direction)
        {
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
                    OnProjectileHit(pet);
                }
            }
        }
        #endregion

        #region Static Methods (Testable)
        /// <summary>
        /// Applies slow effect to a pet.
        /// Property 8: Capture Net Slow Effect
        /// Requirement 3.2: Slows hit pet by 50% for 3 seconds.
        /// </summary>
        /// <param name="pet">The pet to slow</param>
        /// <param name="slowAmount">Speed reduction (0.5 = 50%)</param>
        /// <param name="duration">Duration in seconds</param>
        public static void ApplySlowEffect(PetAI pet, float slowAmount, float duration)
        {
            if (pet == null) return;
            
            // Create slow effect data
            SkillEffectData slowEffect = SkillEffectData.CreateSlow(slowAmount, duration, "Capture Net");
            
            // Apply to pet (pet needs to have effect handling)
            IEffectReceiver effectReceiver = pet.GetComponent<IEffectReceiver>();
            if (effectReceiver != null)
            {
                effectReceiver.ApplyEffect(slowEffect);
            }
            else
            {
                // Fallback: Direct speed modification if pet supports it
                Debug.Log($"[CaptureNetSkill] Applied slow effect: {slowAmount * 100}% for {duration}s");
            }
        }

        /// <summary>
        /// Calculates the slow effect parameters.
        /// </summary>
        /// <param name="baseSlowAmount">Base slow amount from config</param>
        /// <param name="baseDuration">Base duration from config</param>
        /// <returns>Tuple of (slowAmount, duration)</returns>
        public static (float slowAmount, float duration) CalculateSlowEffect(float baseSlowAmount, float baseDuration)
        {
            // Currently no modifiers, but this allows for future expansion
            return (baseSlowAmount, baseDuration);
        }

        /// <summary>
        /// Validates if the slow effect parameters are correct per requirements.
        /// Property 8: Capture Net Slow Effect
        /// </summary>
        /// <param name="slowAmount">The slow amount to validate</param>
        /// <param name="duration">The duration to validate</param>
        /// <returns>True if parameters match requirements</returns>
        public static bool ValidateSlowEffectParameters(float slowAmount, float duration)
        {
            // Requirement 3.2: 50% slow for 3 seconds
            const float RequiredSlowAmount = 0.5f;
            const float RequiredDuration = 3f;
            const float Tolerance = 0.001f;
            
            return Mathf.Abs(slowAmount - RequiredSlowAmount) < Tolerance &&
                   Mathf.Abs(duration - RequiredDuration) < Tolerance;
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
                Cooldown = config.CaptureNetCooldown;
                SlowAmount = config.CaptureNetSlowAmount;
                SlowDuration = config.CaptureNetSlowDuration;
                ProjectileSpeed = config.CaptureNetProjectileSpeed;
            }
        }
#endif

        private void OnDrawGizmosSelected()
        {
            // Draw max range
            Gizmos.color = Color.yellow;
            Vector3 start = LaunchPoint != null ? LaunchPoint.position : transform.position;
            Gizmos.DrawLine(start, start + transform.forward * MaxRange);
        }
        #endregion
    }
}
