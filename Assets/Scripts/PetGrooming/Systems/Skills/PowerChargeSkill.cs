using System;
using System.Collections;
using UnityEngine;
using PetGrooming.Core;
using PetGrooming.AI;

namespace PetGrooming.Systems.Skills
{
    /// <summary>
    /// Power Charge skill for Dog pets.
    /// Dashes forward knocking back the Groomer and obstacles.
    /// If the Groomer is holding a captured pet, the pet is released.
    /// Requirements: 5.2, 5.3
    /// </summary>
    public class PowerChargeSkill : SkillBase
    {
        #region Serialized Fields
        [Header("Power Charge Settings")]
        [Tooltip("Distance of the dash")]
        public float DashDistance = 5f;
        
        [Tooltip("Speed of the dash")]
        public float DashSpeed = 15f;
        
        [Tooltip("Knockback force applied to Groomer and obstacles")]
        public float KnockbackForce = 10f;
        
        [Tooltip("Radius for detecting targets during charge")]
        public float ChargeRadius = 1.5f;
        
        [Tooltip("Layer mask for detecting targets")]
        public LayerMask TargetLayers;
        
        [Header("Configuration")]
        [Tooltip("Phase 2 game configuration")]
        public Phase2GameConfig GameConfig;
        #endregion

        #region Private Fields
        private PetAI _ownerPet;
        private bool _isCharging;
        private Vector3 _chargeDirection;
        private float _chargeDistanceTraveled;
        #endregion

        #region Properties
        /// <summary>
        /// Whether the dog is currently charging.
        /// </summary>
        public bool IsCharging => _isCharging;
        #endregion

        #region Events
        /// <summary>
        /// Fired when the charge hits the Groomer.
        /// </summary>
        public event Action<GroomerController> OnGroomerHit;
        
        /// <summary>
        /// Fired when the charge releases a captured pet.
        /// </summary>
        public event Action<PetAI> OnCapturedPetReleased;
        
        /// <summary>
        /// Fired when the charge hits an obstacle.
        /// </summary>
        public event Action<GameObject> OnObstacleHit;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            
            SkillName = "Power Charge";
            
            // Apply config values if available
            if (GameConfig != null)
            {
                Cooldown = GameConfig.PowerChargeCooldown;
                DashDistance = GameConfig.PowerChargeDashDistance;
                KnockbackForce = GameConfig.PowerChargeKnockbackForce;
            }
            else
            {
                // Default cooldown: 8 seconds (Requirement 5.2)
                Cooldown = 8f;
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
        /// Checks if the skill can be activated.
        /// Cannot activate while already charging.
        /// </summary>
        public override bool CanActivate()
        {
            return base.CanActivate() && !_isCharging;
        }

        /// <summary>
        /// Activates the Power Charge skill.
        /// Requirement 5.2: Dog dashes forward knocking back Groomer and obstacles.
        /// </summary>
        public override void Activate()
        {
            base.Activate();
            StartCharge();
        }

        /// <summary>
        /// Called when the charge hits a target.
        /// </summary>
        public void OnChargeHit(Collider other)
        {
            if (!_isCharging) return;
            
            // Check for Groomer
            GroomerController groomer = other.GetComponent<GroomerController>();
            if (groomer == null)
            {
                groomer = other.GetComponentInParent<GroomerController>();
            }
            
            if (groomer != null)
            {
                HandleGroomerHit(groomer);
                return;
            }
            
            // Check for destructible objects
            DestructibleObject destructible = other.GetComponent<DestructibleObject>();
            if (destructible != null)
            {
                HandleObstacleHit(other.gameObject, destructible);
            }
        }
        #endregion

        #region Private Methods
        private void StartCharge()
        {
            if (_ownerPet == null)
            {
                Debug.LogWarning("[PowerChargeSkill] No owner pet assigned!");
                return;
            }
            
            _isCharging = true;
            _chargeDistanceTraveled = 0f;
            _chargeDirection = _ownerPet.transform.forward;
            
            StartCoroutine(ChargeCoroutine());
            
            Debug.Log("[PowerChargeSkill] Charge started!");
        }

        private IEnumerator ChargeCoroutine()
        {
            while (_isCharging && _chargeDistanceTraveled < DashDistance)
            {
                float moveDistance = DashSpeed * Time.deltaTime;
                Vector3 movement = _chargeDirection * moveDistance;
                
                // Move the pet
                if (_ownerPet != null)
                {
                    _ownerPet.transform.position += movement;
                }
                
                _chargeDistanceTraveled += moveDistance;
                
                // Check for collisions during charge
                CheckChargeCollisions();
                
                yield return null;
            }
            
            EndCharge();
        }

        private void CheckChargeCollisions()
        {
            if (_ownerPet == null) return;
            
            Collider[] hits = Physics.OverlapSphere(_ownerPet.transform.position, ChargeRadius, TargetLayers);
            
            foreach (Collider hit in hits)
            {
                // Skip self
                if (hit.transform == _ownerPet.transform || hit.transform.IsChildOf(_ownerPet.transform))
                {
                    continue;
                }
                
                OnChargeHit(hit);
            }
        }

        private void HandleGroomerHit(GroomerController groomer)
        {
            // Apply knockback to groomer
            ApplyKnockback(groomer.transform, KnockbackForce);
            
            // Check if groomer is carrying a pet and release it
            // Requirement 5.3: If Groomer is holding a captured pet, the pet is released
            if (groomer.IsCarryingPet && groomer.CapturedPet != null)
            {
                PetAI releasedPet = groomer.CapturedPet;
                ReleaseCapturedPet(groomer);
                OnCapturedPetReleased?.Invoke(releasedPet);
                
                Debug.Log("[PowerChargeSkill] Released captured pet from Groomer!");
            }
            
            // Add mischief for pet skill hitting Groomer
            // Property 18: Pet Skill Hit Mischief Value
            // Requirement 6.6: Pet skill hit adds 30 points
            if (MischiefSystem.Instance != null)
            {
                MischiefSystem.Instance.AddPetSkillHitMischief();
            }
            
            OnGroomerHit?.Invoke(groomer);
            
            Debug.Log("[PowerChargeSkill] Hit Groomer!");
        }

        private void HandleObstacleHit(GameObject obstacle, DestructibleObject destructible)
        {
            // Apply knockback to obstacle
            Rigidbody rb = obstacle.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 knockbackDir = (obstacle.transform.position - _ownerPet.transform.position).normalized;
                rb.AddForce(knockbackDir * KnockbackForce, ForceMode.Impulse);
            }
            
            OnObstacleHit?.Invoke(obstacle);
            
            Debug.Log($"[PowerChargeSkill] Hit obstacle: {obstacle.name}");
        }

        private void EndCharge()
        {
            _isCharging = false;
            Debug.Log("[PowerChargeSkill] Charge ended.");
        }
        #endregion

        #region Static Methods (Testable)
        /// <summary>
        /// Applies knockback force to a target.
        /// </summary>
        /// <param name="target">Target transform</param>
        /// <param name="force">Knockback force</param>
        public static void ApplyKnockback(Transform target, float force)
        {
            if (target == null) return;
            
            Rigidbody rb = target.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Apply knockback in the direction away from the charge
                Vector3 knockbackDir = target.forward * -1f;
                rb.AddForce(knockbackDir * force, ForceMode.Impulse);
            }
            
            // Also try CharacterController for Groomer
            CharacterController cc = target.GetComponent<CharacterController>();
            if (cc != null)
            {
                // CharacterController doesn't support physics forces directly
                // The knockback effect would need to be handled by the GroomerController
            }
        }

        /// <summary>
        /// Releases a captured pet from the Groomer.
        /// Property 12: Power Charge Releases Captured Pet
        /// Requirement 5.3: If Groomer is holding a captured pet, the pet is released.
        /// </summary>
        /// <param name="groomer">The groomer holding the pet</param>
        /// <returns>True if a pet was released</returns>
        public static bool ReleaseCapturedPet(GroomerController groomer)
        {
            if (groomer == null) return false;
            if (!groomer.IsCarryingPet || groomer.CapturedPet == null) return false;
            
            // Release the pet
            groomer.ReleasePet();
            return true;
        }

        /// <summary>
        /// Checks if a Power Charge hit should release a captured pet.
        /// Property 12: Power Charge Releases Captured Pet
        /// </summary>
        /// <param name="isCarryingPet">Whether the groomer is carrying a pet</param>
        /// <returns>True if the pet should be released</returns>
        public static bool ShouldReleaseCapturedPet(bool isCarryingPet)
        {
            return isCarryingPet;
        }

        /// <summary>
        /// Calculates the charge end position.
        /// </summary>
        /// <param name="startPosition">Starting position</param>
        /// <param name="direction">Charge direction</param>
        /// <param name="distance">Charge distance</param>
        /// <returns>End position of the charge</returns>
        public static Vector3 CalculateChargeEndPosition(Vector3 startPosition, Vector3 direction, float distance)
        {
            return startPosition + direction.normalized * distance;
        }

        /// <summary>
        /// Validates the Power Charge cooldown matches requirements.
        /// Requirement 5.2: 8 second cooldown
        /// </summary>
        /// <param name="cooldown">The cooldown to validate</param>
        /// <returns>True if cooldown matches requirement</returns>
        public static bool ValidateCooldown(float cooldown)
        {
            const float RequiredCooldown = 8f;
            const float Tolerance = 0.001f;
            return Mathf.Abs(cooldown - RequiredCooldown) < Tolerance;
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
                Cooldown = config.PowerChargeCooldown;
                DashDistance = config.PowerChargeDashDistance;
                KnockbackForce = config.PowerChargeKnockbackForce;
            }
        }

        /// <summary>
        /// Sets the owner for testing purposes.
        /// </summary>
        public void SetOwnerForTesting(PetAI owner)
        {
            _ownerPet = owner;
        }

        /// <summary>
        /// Sets the charging state for testing purposes.
        /// </summary>
        public void SetChargingStateForTesting(bool isCharging)
        {
            _isCharging = isCharging;
        }
#endif

        private void OnDrawGizmosSelected()
        {
            // Draw charge radius
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, ChargeRadius);
            
            // Draw charge direction and distance
            if (_ownerPet != null || Application.isPlaying)
            {
                Gizmos.color = Color.yellow;
                Vector3 start = transform.position;
                Vector3 end = start + transform.forward * DashDistance;
                Gizmos.DrawLine(start, end);
                Gizmos.DrawWireSphere(end, 0.3f);
            }
        }
        #endregion
    }
}
