using System;
using UnityEngine;
using PetGrooming.Core;
using PetGrooming.AI;

namespace PetGrooming.Systems.Skills
{
    /// <summary>
    /// Intimidating Bark skill for Dog pets.
    /// Creates an area effect that reduces Groomer speed by 20% for 3 seconds.
    /// Requirements: 5.4
    /// </summary>
    public class IntimidatingBarkSkill : SkillBase
    {
        #region Serialized Fields
        [Header("Intimidating Bark Settings")]
        [Tooltip("Radius of the bark effect")]
        public float EffectRadius = 4f;
        
        [Tooltip("Movement speed reduction (0.2 = 20% reduction)")]
        [Range(0f, 1f)]
        public float SlowAmount = 0.2f;
        
        [Tooltip("Duration of the slow effect in seconds")]
        public float SlowDuration = 3f;
        
        [Tooltip("Visual effect for the bark")]
        public ParticleSystem BarkEffect;
        
        [Tooltip("Audio source for bark sound")]
        public AudioSource BarkSound;
        
        [Header("Configuration")]
        [Tooltip("Phase 2 game configuration")]
        public Phase2GameConfig GameConfig;
        #endregion

        #region Private Fields
        private PetAI _ownerPet;
        #endregion

        #region Events
        /// <summary>
        /// Fired when the bark affects the Groomer.
        /// </summary>
        public event Action<GroomerController> OnGroomerAffected;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            
            SkillName = "Intimidating Bark";
            
            // Apply config values if available
            if (GameConfig != null)
            {
                Cooldown = GameConfig.IntimidatingBarkCooldown;
                EffectRadius = GameConfig.IntimidatingBarkRadius;
                SlowAmount = GameConfig.IntimidatingBarkSlowAmount;
                SlowDuration = GameConfig.IntimidatingBarkDuration;
            }
            else
            {
                // Default cooldown: 12 seconds (Requirement 5.4)
                Cooldown = 12f;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Sets the owner pet for this skill.
        /// </summary>
        public void SetOwner(PetAI owner)
        {
            _ownerPet = owner;
        }

        /// <summary>
        /// Activates the Intimidating Bark skill.
        /// Requirement 5.4: Creates area effect reducing Groomer speed by 20% for 3 seconds.
        /// </summary>
        public override void Activate()
        {
            base.Activate();
            PerformBark();
        }
        #endregion

        #region Private Methods
        private void PerformBark()
        {
            Vector3 barkOrigin = _ownerPet != null ? _ownerPet.transform.position : transform.position;
            
            // Play visual effect
            if (BarkEffect != null)
            {
                BarkEffect.Play();
            }
            
            // Play sound effect
            if (BarkSound != null)
            {
                BarkSound.Play();
            }
            
            // Find all targets in range
            Collider[] hits = Physics.OverlapSphere(barkOrigin, EffectRadius);
            
            foreach (Collider hit in hits)
            {
                // Check for Groomer
                GroomerController groomer = hit.GetComponent<GroomerController>();
                if (groomer == null)
                {
                    groomer = hit.GetComponentInParent<GroomerController>();
                }
                
                if (groomer != null)
                {
                    ApplySlowToGroomer(groomer);
                }
            }
            
            Debug.Log($"[IntimidatingBarkSkill] Bark performed! Radius: {EffectRadius}, Slow: {SlowAmount * 100}% for {SlowDuration}s");
        }

        private void ApplySlowToGroomer(GroomerController groomer)
        {
            if (groomer == null) return;
            
            // Apply slow effect to groomer
            IEffectReceiver effectReceiver = groomer.GetComponent<IEffectReceiver>();
            if (effectReceiver != null)
            {
                SkillEffectData slowEffect = SkillEffectData.CreateSlow(SlowAmount, SlowDuration, "Intimidating Bark");
                effectReceiver.ApplyEffect(slowEffect);
            }
            
            // Add mischief for pet skill hitting Groomer
            // Property 18: Pet Skill Hit Mischief Value
            // Requirement 6.6: Pet skill hit adds 30 points
            if (MischiefSystem.Instance != null)
            {
                MischiefSystem.Instance.AddPetSkillHitMischief();
            }
            
            OnGroomerAffected?.Invoke(groomer);
            
            Debug.Log($"[IntimidatingBarkSkill] Applied {SlowAmount * 100}% slow to Groomer for {SlowDuration}s");
        }
        #endregion

        #region Static Methods (Testable)
        /// <summary>
        /// Calculates the slow effect parameters.
        /// Property 13: Intimidating Bark Slow Effect
        /// Requirement 5.4: 20% slow for 3 seconds
        /// </summary>
        /// <param name="baseSlowAmount">Base slow amount from config</param>
        /// <param name="baseDuration">Base duration from config</param>
        /// <returns>Tuple of (slowAmount, duration)</returns>
        public static (float slowAmount, float duration) CalculateSlowEffect(float baseSlowAmount, float baseDuration)
        {
            return (baseSlowAmount, baseDuration);
        }

        /// <summary>
        /// Validates if the slow effect parameters are correct per requirements.
        /// Property 13: Intimidating Bark Slow Effect
        /// Requirement 5.4: 20% slow for 3 seconds
        /// </summary>
        /// <param name="slowAmount">The slow amount to validate</param>
        /// <param name="duration">The duration to validate</param>
        /// <returns>True if parameters match requirements</returns>
        public static bool ValidateSlowEffectParameters(float slowAmount, float duration)
        {
            const float RequiredSlowAmount = 0.2f;
            const float RequiredDuration = 3f;
            const float Tolerance = 0.001f;
            
            return Mathf.Abs(slowAmount - RequiredSlowAmount) < Tolerance &&
                   Mathf.Abs(duration - RequiredDuration) < Tolerance;
        }

        /// <summary>
        /// Validates the Intimidating Bark cooldown matches requirements.
        /// Requirement 5.4: 12 second cooldown
        /// </summary>
        /// <param name="cooldown">The cooldown to validate</param>
        /// <returns>True if cooldown matches requirement</returns>
        public static bool ValidateCooldown(float cooldown)
        {
            const float RequiredCooldown = 12f;
            const float Tolerance = 0.001f;
            return Mathf.Abs(cooldown - RequiredCooldown) < Tolerance;
        }

        /// <summary>
        /// Checks if a position is within the bark effect radius.
        /// </summary>
        /// <param name="barkOrigin">Origin of the bark</param>
        /// <param name="targetPosition">Position to check</param>
        /// <param name="effectRadius">Radius of the effect</param>
        /// <returns>True if within range</returns>
        public static bool IsWithinEffectRadius(Vector3 barkOrigin, Vector3 targetPosition, float effectRadius)
        {
            float distance = Vector3.Distance(barkOrigin, targetPosition);
            return distance <= effectRadius;
        }

        /// <summary>
        /// Creates a slow effect data for the Intimidating Bark.
        /// </summary>
        /// <param name="slowAmount">Amount of slow (0.2 = 20%)</param>
        /// <param name="duration">Duration in seconds</param>
        /// <returns>SkillEffectData for the slow effect</returns>
        public static SkillEffectData CreateBarkSlowEffect(float slowAmount, float duration)
        {
            return SkillEffectData.CreateSlow(slowAmount, duration, "Intimidating Bark");
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
                Cooldown = config.IntimidatingBarkCooldown;
                EffectRadius = config.IntimidatingBarkRadius;
                SlowAmount = config.IntimidatingBarkSlowAmount;
                SlowDuration = config.IntimidatingBarkDuration;
            }
        }

        /// <summary>
        /// Sets the owner for testing purposes.
        /// </summary>
        public void SetOwnerForTesting(PetAI owner)
        {
            _ownerPet = owner;
        }
#endif

        private void OnDrawGizmosSelected()
        {
            // Draw effect radius
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, EffectRadius);
            
            Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
            Gizmos.DrawWireSphere(transform.position, EffectRadius);
        }
        #endregion
    }
}
