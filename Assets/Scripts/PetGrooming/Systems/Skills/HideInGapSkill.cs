using System;
using UnityEngine;
using PetGrooming.Core;
using PetGrooming.AI;

namespace PetGrooming.Systems.Skills
{
    /// <summary>
    /// Hide In Gap skill for Cat pets.
    /// Makes the cat invisible while stationary, semi-transparent when moving.
    /// Requirement 4.4: Invisible for 3 seconds while stationary with 14 second cooldown.
    /// Requirement 4.5: Semi-transparent (50% opacity) when moving while using skill.
    /// </summary>
    public class HideInGapSkill : SkillBase
    {
        #region Serialized Fields
        [Header("Hide In Gap Settings")]
        [Tooltip("Duration of the invisibility effect in seconds")]
        public float InvisibilityDuration = 3f;
        
        [Tooltip("Opacity when stationary (0 = fully invisible)")]
        [Range(0f, 1f)]
        public float StationaryOpacity = 0f;
        
        [Tooltip("Opacity when moving (0.5 = 50% visible)")]
        [Range(0f, 1f)]
        public float MovingOpacity = 0.5f;
        
        [Tooltip("Movement threshold to determine if cat is moving")]
        public float MovementThreshold = 0.1f;
        
        [Header("Configuration")]
        [Tooltip("Phase 2 game configuration")]
        public Phase2GameConfig GameConfig;
        #endregion

        #region Private Fields
        private PetAI _ownerPet;
        private Transform _ownerTransform;
        private bool _isHiding;
        private float _hideTimer;
        private Vector3 _lastPosition;
        private float _currentOpacity;
        #endregion

        #region Properties
        /// <summary>
        /// Whether the cat is currently hiding.
        /// </summary>
        public bool IsHiding => _isHiding;
        
        /// <summary>
        /// Current opacity of the cat (0 = invisible, 1 = fully visible).
        /// </summary>
        public float CurrentOpacity => _currentOpacity;
        
        /// <summary>
        /// Remaining time for the hide effect.
        /// </summary>
        public float RemainingHideTime => _hideTimer;
        #endregion

        #region Events
        /// <summary>
        /// Fired when hiding starts.
        /// </summary>
        public event Action OnHideStarted;
        
        /// <summary>
        /// Fired when hiding ends.
        /// </summary>
        public event Action OnHideEnded;
        
        /// <summary>
        /// Fired when opacity changes. Parameter is the new opacity value.
        /// </summary>
        public event Action<float> OnOpacityChanged;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            
            SkillName = "Hide In Gap";
            
            // Apply config values if available
            if (GameConfig != null)
            {
                Cooldown = GameConfig.HideInGapCooldown;
                InvisibilityDuration = GameConfig.HideInGapDuration;
                MovingOpacity = GameConfig.HideInGapMovingOpacity;
            }
            else
            {
                // Default cooldown: 14 seconds (Requirement 4.4)
                Cooldown = 14f;
                InvisibilityDuration = 3f;
                MovingOpacity = 0.5f;
            }
            
            _ownerTransform = transform;
            _currentOpacity = 1f;
        }

        protected override void Update()
        {
            base.Update();
            
            if (_isHiding)
            {
                UpdateHideState();
            }
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
            _lastPosition = _ownerTransform.position;
        }

        /// <summary>
        /// Checks if the skill can be activated.
        /// </summary>
        public override bool CanActivate()
        {
            // Can activate if ready and not currently hiding
            return base.CanActivate() && !_isHiding;
        }

        /// <summary>
        /// Activates the Hide In Gap skill.
        /// Requirement 4.4: Becomes invisible for 3 seconds while stationary.
        /// </summary>
        public override void Activate()
        {
            base.Activate();
            StartHiding();
        }

        /// <summary>
        /// Cancels the hide effect early.
        /// </summary>
        public void CancelHide()
        {
            if (_isHiding)
            {
                EndHiding();
            }
        }

        /// <summary>
        /// Gets the current visibility state based on movement.
        /// </summary>
        /// <returns>True if the cat is currently visible (moving while hiding)</returns>
        public bool IsCurrentlyVisible()
        {
            if (!_isHiding) return true;
            return _currentOpacity > 0f;
        }
        #endregion

        #region Private Methods
        private void StartHiding()
        {
            _isHiding = true;
            _hideTimer = InvisibilityDuration;
            _lastPosition = _ownerTransform.position;
            
            // Start fully invisible (stationary)
            SetOpacity(StationaryOpacity);
            
            // Apply invisibility effect to pet
            if (_ownerPet != null)
            {
                _ownerPet.SetInvisible(true, StationaryOpacity, InvisibilityDuration);
            }
            
            OnHideStarted?.Invoke();
            Debug.Log("[HideInGapSkill] Hiding started");
        }

        private void UpdateHideState()
        {
            _hideTimer -= Time.deltaTime;
            
            // Check if cat is moving
            bool isMoving = IsMoving(_ownerTransform.position, _lastPosition, MovementThreshold, Time.deltaTime);
            _lastPosition = _ownerTransform.position;
            
            // Update opacity based on movement
            // Requirement 4.4: Fully invisible while stationary
            // Requirement 4.5: Semi-transparent (50%) when moving
            float targetOpacity = CalculateOpacity(isMoving, StationaryOpacity, MovingOpacity);
            
            if (!Mathf.Approximately(_currentOpacity, targetOpacity))
            {
                SetOpacity(targetOpacity);
                
                // Update pet's invisibility state
                if (_ownerPet != null)
                {
                    _ownerPet.UpdateInvisibilityOpacity(isMoving);
                }
            }
            
            // Check if hide duration has ended
            if (_hideTimer <= 0f)
            {
                EndHiding();
            }
        }

        private void EndHiding()
        {
            _isHiding = false;
            _hideTimer = 0f;
            
            // Restore full visibility
            SetOpacity(1f);
            
            // Remove invisibility effect from pet
            if (_ownerPet != null)
            {
                _ownerPet.SetInvisible(false);
            }
            
            OnHideEnded?.Invoke();
            Debug.Log("[HideInGapSkill] Hiding ended");
        }

        private void SetOpacity(float opacity)
        {
            _currentOpacity = opacity;
            OnOpacityChanged?.Invoke(opacity);
        }
        #endregion

        #region Static Methods (Testable)
        /// <summary>
        /// Determines if the cat is moving based on position change.
        /// </summary>
        /// <param name="currentPosition">Current position</param>
        /// <param name="lastPosition">Previous position</param>
        /// <param name="threshold">Movement threshold</param>
        /// <param name="deltaTime">Time since last check</param>
        /// <returns>True if moving</returns>
        public static bool IsMoving(Vector3 currentPosition, Vector3 lastPosition, float threshold, float deltaTime)
        {
            if (deltaTime <= 0f) return false;
            
            float distance = Vector3.Distance(currentPosition, lastPosition);
            float speed = distance / deltaTime;
            
            return speed > threshold;
        }

        /// <summary>
        /// Calculates the opacity based on movement state.
        /// Property 11: Hide In Gap Visibility State
        /// Requirement 4.4: Invisible (0%) while stationary
        /// Requirement 4.5: Semi-transparent (50%) when moving
        /// </summary>
        /// <param name="isMoving">Whether the cat is moving</param>
        /// <param name="stationaryOpacity">Opacity when stationary</param>
        /// <param name="movingOpacity">Opacity when moving</param>
        /// <returns>Target opacity value</returns>
        public static float CalculateOpacity(bool isMoving, float stationaryOpacity, float movingOpacity)
        {
            return isMoving ? movingOpacity : stationaryOpacity;
        }

        /// <summary>
        /// Validates the visibility state matches requirements.
        /// Property 11: Hide In Gap Visibility State
        /// </summary>
        /// <param name="isMoving">Whether the cat is moving</param>
        /// <param name="opacity">Current opacity</param>
        /// <returns>True if opacity matches expected state</returns>
        public static bool ValidateVisibilityState(bool isMoving, float opacity)
        {
            const float Tolerance = 0.001f;
            
            if (isMoving)
            {
                // Requirement 4.5: 50% opacity when moving
                return Mathf.Abs(opacity - 0.5f) < Tolerance;
            }
            else
            {
                // Requirement 4.4: 0% opacity (invisible) when stationary
                return Mathf.Abs(opacity - 0f) < Tolerance;
            }
        }

        /// <summary>
        /// Validates the cooldown matches requirements.
        /// Requirement 4.4: 14 second cooldown.
        /// </summary>
        /// <param name="cooldown">The cooldown to validate</param>
        /// <returns>True if cooldown matches requirement</returns>
        public static bool ValidateCooldown(float cooldown)
        {
            const float RequiredCooldown = 14f;
            const float Tolerance = 0.001f;
            return Mathf.Abs(cooldown - RequiredCooldown) < Tolerance;
        }

        /// <summary>
        /// Validates the invisibility duration matches requirements.
        /// Requirement 4.4: 3 second duration.
        /// </summary>
        /// <param name="duration">The duration to validate</param>
        /// <returns>True if duration matches requirement</returns>
        public static bool ValidateDuration(float duration)
        {
            const float RequiredDuration = 3f;
            const float Tolerance = 0.001f;
            return Mathf.Abs(duration - RequiredDuration) < Tolerance;
        }

        /// <summary>
        /// Gets the expected opacity for a given movement state.
        /// Used for property testing.
        /// </summary>
        /// <param name="isMoving">Whether the cat is moving</param>
        /// <returns>Expected opacity value</returns>
        public static float GetExpectedOpacity(bool isMoving)
        {
            // Requirement 4.4: 0% when stationary
            // Requirement 4.5: 50% when moving
            return isMoving ? 0.5f : 0f;
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
                Cooldown = config.HideInGapCooldown;
                InvisibilityDuration = config.HideInGapDuration;
                MovingOpacity = config.HideInGapMovingOpacity;
            }
        }

        /// <summary>
        /// Sets the hiding state for testing.
        /// </summary>
        public void SetHidingStateForTesting(bool isHiding, float remainingTime = 0f)
        {
            _isHiding = isHiding;
            _hideTimer = remainingTime;
        }

        /// <summary>
        /// Sets the opacity for testing.
        /// </summary>
        public void SetOpacityForTesting(float opacity)
        {
            _currentOpacity = opacity;
        }

        /// <summary>
        /// Sets the last position for testing movement detection.
        /// </summary>
        public void SetLastPositionForTesting(Vector3 position)
        {
            _lastPosition = position;
        }
#endif

        private void OnDrawGizmosSelected()
        {
            // Draw visibility indicator
            if (_isHiding)
            {
                Gizmos.color = new Color(0f, 1f, 1f, _currentOpacity);
                Gizmos.DrawWireSphere(transform.position, 0.5f);
            }
        }
        #endregion
    }
}
