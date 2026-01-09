using UnityEngine;

namespace PetGrooming.Core
{
    /// <summary>
    /// ScriptableObject containing Phase 2 game configuration parameters.
    /// Extends the base game with multi-pet support, skill systems, pet cage, and camera settings.
    /// Requirements: 1.1, 1.2, 3.2-3.8, 4.2-4.4, 5.2-5.5, 6.1, 6.2
    /// </summary>
    [CreateAssetMenu(fileName = "Phase2GameConfig", menuName = "PetGrooming/Phase2GameConfig")]
    public class Phase2GameConfig : ScriptableObject
    {
        #region Match Settings
        [Header("Match Settings")]
        [Tooltip("Duration of a match in seconds (5 minutes for Phase 2)")]
        public float MatchDuration = 300f;
        
        [Tooltip("Mischief threshold for 2-pet mode")]
        public int TwoPetMischiefThreshold = 800;
        
        [Tooltip("Mischief threshold for 3-pet mode")]
        public int ThreePetMischiefThreshold = 1000;
        
        [Tooltip("Points below threshold to trigger alert state")]
        public int AlertThresholdOffset = 100;
        #endregion

        #region Pet Cage Settings
        [Header("Pet Cage Settings")]
        [Tooltip("Maximum time a pet can be stored in cage (seconds)")]
        public float CageStorageTime = 60f;
        
        [Tooltip("Time remaining when warning indicator appears (seconds)")]
        public float CageWarningTime = 10f;
        
        [Tooltip("Invulnerability duration after pet is released from cage (seconds)")]
        public float ReleaseInvulnerabilityTime = 3f;
        #endregion

        #region Groomer Skill Cooldowns
        [Header("Groomer Skill Cooldowns")]
        [Tooltip("Cooldown for Capture Net skill (seconds)")]
        public float CaptureNetCooldown = 8f;
        
        [Tooltip("Cooldown for Leash skill (seconds)")]
        public float LeashCooldown = 12f;
        
        [Tooltip("Cooldown for Calming Spray skill (seconds)")]
        public float CalmingSprayCooldown = 13f;
        #endregion

        #region Cat Skill Cooldowns
        [Header("Cat Skill Cooldowns")]
        [Tooltip("Cooldown for Agile Jump skill (seconds)")]
        public float AgileJumpCooldown = 6f;
        
        [Tooltip("Cooldown for Fur Distraction skill (seconds)")]
        public float FurDistractionCooldown = 10f;
        
        [Tooltip("Cooldown for Hide In Gap skill (seconds)")]
        public float HideInGapCooldown = 14f;
        #endregion

        #region Dog Skill Cooldowns
        [Header("Dog Skill Cooldowns")]
        [Tooltip("Cooldown for Power Charge skill (seconds)")]
        public float PowerChargeCooldown = 8f;
        
        [Tooltip("Cooldown for Intimidating Bark skill (seconds)")]
        public float IntimidatingBarkCooldown = 12f;
        
        [Tooltip("Cooldown for Steal Tool skill (seconds)")]
        public float StealToolCooldown = 12f;
        #endregion


        #region Capture Net Skill Effects
        [Header("Capture Net Skill Effects")]
        [Tooltip("Movement speed reduction when hit by Capture Net (0.5 = 50% reduction)")]
        [Range(0f, 1f)]
        public float CaptureNetSlowAmount = 0.5f;
        
        [Tooltip("Duration of Capture Net slow effect (seconds)")]
        public float CaptureNetSlowDuration = 3f;
        
        [Tooltip("Projectile speed for Capture Net")]
        public float CaptureNetProjectileSpeed = 15f;
        #endregion

        #region Leash Skill Effects
        [Header("Leash Skill Effects")]
        [Tooltip("Maximum range for Leash skill")]
        public float LeashMaxRange = 10f;
        
        [Tooltip("Pull speed when Leash connects")]
        public float LeashPullSpeed = 8f;
        
        [Tooltip("Chance for Cat to break free from Leash (0.6 = 60%)")]
        [Range(0f, 1f)]
        public float LeashCatBreakFreeChance = 0.6f;
        
        [Tooltip("Chance for Dog to break free from Leash (0.4 = 40%)")]
        [Range(0f, 1f)]
        public float LeashDogBreakFreeChance = 0.4f;
        #endregion

        #region Calming Spray Skill Effects
        [Header("Calming Spray Skill Effects")]
        [Tooltip("Effect radius for Calming Spray")]
        public float CalmingSprayRadius = 3f;
        
        [Tooltip("Stun duration when hit by Calming Spray (seconds)")]
        public float CalmingSprayStunDuration = 1f;
        #endregion

        #region Fur Distraction Skill Effects
        [Header("Fur Distraction Skill Effects")]
        [Tooltip("Duration of vision block from Fur Distraction (seconds)")]
        public float FurDistractionDuration = 2f;
        #endregion

        #region Hide In Gap Skill Effects
        [Header("Hide In Gap Skill Effects")]
        [Tooltip("Duration of invisibility from Hide In Gap (seconds)")]
        public float HideInGapDuration = 3f;
        
        [Tooltip("Opacity when moving while using Hide In Gap (0.5 = 50%)")]
        [Range(0f, 1f)]
        public float HideInGapMovingOpacity = 0.5f;
        #endregion

        #region Power Charge Skill Effects
        [Header("Power Charge Skill Effects")]
        [Tooltip("Dash distance for Power Charge")]
        public float PowerChargeDashDistance = 5f;
        
        [Tooltip("Knockback force applied by Power Charge")]
        public float PowerChargeKnockbackForce = 10f;
        #endregion

        #region Intimidating Bark Skill Effects
        [Header("Intimidating Bark Skill Effects")]
        [Tooltip("Effect radius for Intimidating Bark")]
        public float IntimidatingBarkRadius = 4f;
        
        [Tooltip("Movement speed reduction from Intimidating Bark (0.2 = 20% reduction)")]
        [Range(0f, 1f)]
        public float IntimidatingBarkSlowAmount = 0.2f;
        
        [Tooltip("Duration of Intimidating Bark slow effect (seconds)")]
        public float IntimidatingBarkDuration = 3f;
        #endregion

        #region Steal Tool Skill Effects
        [Header("Steal Tool Skill Effects")]
        [Tooltip("Range to detect nearest Grooming Station for Steal Tool")]
        public float StealToolRange = 5f;
        
        [Tooltip("Additional grooming steps added when tool is stolen")]
        public int StealToolExtraSteps = 1;
        #endregion


        #region Alert System Settings
        [Header("Alert System Settings")]
        [Tooltip("Groomer speed bonus during alert state (0.1 = 10% increase)")]
        [Range(0f, 0.5f)]
        public float AlertGroomerSpeedBonus = 0.1f;
        
        [Tooltip("Interval between alert light flashes (seconds)")]
        public float AlertFlashInterval = 0.5f;
        
        [Tooltip("Mischief points added when pet skill hits Groomer")]
        public int PetSkillHitMischief = 30;
        #endregion

        #region Camera Settings
        [Header("Camera Settings")]
        [Tooltip("Camera follow interpolation speed")]
        public float CameraFollowSpeed = 5f;
        
        [Tooltip("Default camera offset from target")]
        public Vector3 CameraDefaultOffset = new Vector3(0, 8, -6);
        
        [Tooltip("Default camera field of view")]
        public float CameraDefaultFOV = 60f;
        
        [Tooltip("Zoom multiplier when Groomer captures a pet")]
        public float CameraCaptureZoomMultiplier = 1.2f;
        
        [Tooltip("Camera zoom interpolation speed")]
        public float CameraZoomSpeed = 2f;
        
        [Tooltip("Minimum distance camera can be from target (collision avoidance)")]
        public float CameraMinDistance = 2f;
        
        [Tooltip("Screen shake intensity during alert state")]
        public float AlertShakeIntensity = 0.1f;
        
        [Tooltip("Screen shake duration during alert state")]
        public float AlertShakeDuration = 0.5f;
        #endregion

        #region Pet Type Settings
        [Header("Pet Type Settings")]
        [Tooltip("Collision radius for Cat pets")]
        public float CatCollisionRadius = 0.5f;
        
        [Tooltip("Collision radius for Dog pets")]
        public float DogCollisionRadius = 1.0f;
        
        [Tooltip("Base escape chance for Cat pets")]
        [Range(0f, 1f)]
        public float CatBaseEscapeChance = 0.4f;
        
        [Tooltip("Base escape chance for Dog pets")]
        [Range(0f, 1f)]
        public float DogBaseEscapeChance = 0.3f;
        
        [Tooltip("Knockback force for Cat collisions")]
        public float CatKnockbackForce = 5f;
        
        [Tooltip("Knockback force for Dog collisions")]
        public float DogKnockbackForce = 10f;
        
        [Tooltip("Base movement speed for Dog pets (same as Groomer)")]
        public float DogMoveSpeed = 5f;
        #endregion

        #region Helper Methods
        /// <summary>
        /// Gets the mischief threshold based on game mode.
        /// </summary>
        /// <param name="petCount">Number of pets in the match (2 or 3)</param>
        /// <returns>The mischief threshold for the given pet count</returns>
        public int GetMischiefThreshold(int petCount)
        {
            return petCount >= 3 ? ThreePetMischiefThreshold : TwoPetMischiefThreshold;
        }

        /// <summary>
        /// Gets the alert trigger threshold based on game mode.
        /// </summary>
        /// <param name="petCount">Number of pets in the match (2 or 3)</param>
        /// <returns>The mischief value at which alert state triggers</returns>
        public int GetAlertThreshold(int petCount)
        {
            return GetMischiefThreshold(petCount) - AlertThresholdOffset;
        }

        /// <summary>
        /// Gets the break free chance for Leash skill based on pet type.
        /// </summary>
        /// <param name="isCat">True if the pet is a Cat, false if Dog</param>
        /// <returns>The break free chance (0-1)</returns>
        public float GetLeashBreakFreeChance(bool isCat)
        {
            return isCat ? LeashCatBreakFreeChance : LeashDogBreakFreeChance;
        }

        /// <summary>
        /// Gets the collision radius based on pet type.
        /// </summary>
        /// <param name="isCat">True if the pet is a Cat, false if Dog</param>
        /// <returns>The collision radius</returns>
        public float GetCollisionRadius(bool isCat)
        {
            return isCat ? CatCollisionRadius : DogCollisionRadius;
        }

        /// <summary>
        /// Gets the base escape chance based on pet type.
        /// </summary>
        /// <param name="isCat">True if the pet is a Cat, false if Dog</param>
        /// <returns>The base escape chance (0-1)</returns>
        public float GetBaseEscapeChance(bool isCat)
        {
            return isCat ? CatBaseEscapeChance : DogBaseEscapeChance;
        }

        /// <summary>
        /// Gets the knockback force based on pet type.
        /// </summary>
        /// <param name="isCat">True if the pet is a Cat, false if Dog</param>
        /// <returns>The knockback force</returns>
        public float GetKnockbackForce(bool isCat)
        {
            return isCat ? CatKnockbackForce : DogKnockbackForce;
        }
        #endregion
    }
}
