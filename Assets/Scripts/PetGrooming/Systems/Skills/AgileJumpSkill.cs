using System;
using UnityEngine;
using PetGrooming.Core;
using PetGrooming.AI;

namespace PetGrooming.Systems.Skills
{
    /// <summary>
    /// Agile Jump skill for Cat pets.
    /// Performs a double jump to cross obstacles.
    /// Requirement 4.2: Double jump with 6 second cooldown.
    /// </summary>
    public class AgileJumpSkill : SkillBase
    {
        #region Serialized Fields
        [Header("Agile Jump Settings")]
        [Tooltip("Height of the first jump")]
        public float FirstJumpHeight = 2f;
        
        [Tooltip("Height of the second jump")]
        public float SecondJumpHeight = 1.5f;
        
        [Tooltip("Forward distance covered during jump")]
        public float JumpForwardDistance = 3f;
        
        [Tooltip("Duration of the jump animation")]
        public float JumpDuration = 0.5f;
        
        [Header("Configuration")]
        [Tooltip("Phase 2 game configuration")]
        public Phase2GameConfig GameConfig;
        #endregion

        #region Private Fields
        private PetAI _ownerPet;
        private Transform _ownerTransform;
        private bool _isJumping;
        private bool _hasUsedSecondJump;
        private float _jumpTimer;
        private Vector3 _jumpStartPosition;
        private Vector3 _jumpTargetPosition;
        private float _currentJumpHeight;
        #endregion

        #region Properties
        /// <summary>
        /// Whether the cat is currently in a jump.
        /// </summary>
        public bool IsJumping => _isJumping;
        
        /// <summary>
        /// Whether the second jump has been used.
        /// </summary>
        public bool HasUsedSecondJump => _hasUsedSecondJump;
        #endregion

        #region Events
        /// <summary>
        /// Fired when the first jump starts.
        /// </summary>
        public event Action OnFirstJumpStarted;
        
        /// <summary>
        /// Fired when the second jump starts.
        /// </summary>
        public event Action OnSecondJumpStarted;
        
        /// <summary>
        /// Fired when the jump sequence completes.
        /// </summary>
        public event Action OnJumpCompleted;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            
            SkillName = "Agile Jump";
            
            // Apply config values if available
            if (GameConfig != null)
            {
                Cooldown = GameConfig.AgileJumpCooldown;
            }
            else
            {
                // Default cooldown: 6 seconds (Requirement 4.2)
                Cooldown = 6f;
            }
            
            _ownerTransform = transform;
        }

        protected override void Update()
        {
            base.Update();
            
            if (_isJumping)
            {
                UpdateJump();
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
        }

        /// <summary>
        /// Checks if the skill can be activated.
        /// </summary>
        public override bool CanActivate()
        {
            // Can activate if ready and not currently jumping
            return base.CanActivate() && !_isJumping;
        }

        /// <summary>
        /// Activates the Agile Jump skill.
        /// Requirement 4.2: Performs a double jump to cross obstacles.
        /// </summary>
        public override void Activate()
        {
            base.Activate();
            StartJumpSequence();
        }

        /// <summary>
        /// Triggers the second jump if available.
        /// </summary>
        public void TriggerSecondJump()
        {
            if (_isJumping && !_hasUsedSecondJump)
            {
                StartSecondJump();
            }
        }

        /// <summary>
        /// Cancels the current jump.
        /// </summary>
        public void CancelJump()
        {
            if (_isJumping)
            {
                _isJumping = false;
                _hasUsedSecondJump = false;
                OnJumpCompleted?.Invoke();
            }
        }
        #endregion

        #region Private Methods
        private void StartJumpSequence()
        {
            _isJumping = true;
            _hasUsedSecondJump = false;
            _jumpTimer = 0f;
            _jumpStartPosition = _ownerTransform.position;
            _currentJumpHeight = FirstJumpHeight;
            
            // Calculate target position (forward from current facing)
            Vector3 forward = _ownerTransform.forward;
            _jumpTargetPosition = _jumpStartPosition + forward * (JumpForwardDistance * 0.5f);
            
            OnFirstJumpStarted?.Invoke();
            
            Debug.Log("[AgileJumpSkill] First jump started");
        }

        private void StartSecondJump()
        {
            _hasUsedSecondJump = true;
            _jumpTimer = 0f;
            _jumpStartPosition = _ownerTransform.position;
            _currentJumpHeight = SecondJumpHeight;
            
            // Calculate target position for second jump
            Vector3 forward = _ownerTransform.forward;
            _jumpTargetPosition = _jumpStartPosition + forward * (JumpForwardDistance * 0.5f);
            
            OnSecondJumpStarted?.Invoke();
            
            Debug.Log("[AgileJumpSkill] Second jump started");
        }

        private void UpdateJump()
        {
            _jumpTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(_jumpTimer / JumpDuration);
            
            // Calculate position along jump arc
            Vector3 newPosition = CalculateJumpPosition(
                _jumpStartPosition, 
                _jumpTargetPosition, 
                _currentJumpHeight, 
                progress
            );
            
            _ownerTransform.position = newPosition;
            
            // Check if jump phase is complete
            if (progress >= 1f)
            {
                if (!_hasUsedSecondJump)
                {
                    // Automatically trigger second jump
                    StartSecondJump();
                }
                else
                {
                    // Jump sequence complete
                    CompleteJump();
                }
            }
        }

        private void CompleteJump()
        {
            _isJumping = false;
            _hasUsedSecondJump = false;
            
            OnJumpCompleted?.Invoke();
            
            Debug.Log("[AgileJumpSkill] Jump sequence completed");
        }
        #endregion

        #region Static Methods (Testable)
        /// <summary>
        /// Calculates the position along a jump arc.
        /// </summary>
        /// <param name="start">Start position</param>
        /// <param name="end">End position</param>
        /// <param name="height">Maximum height of the arc</param>
        /// <param name="progress">Progress along the arc (0-1)</param>
        /// <returns>Position along the jump arc</returns>
        public static Vector3 CalculateJumpPosition(Vector3 start, Vector3 end, float height, float progress)
        {
            // Linear interpolation for horizontal movement
            Vector3 horizontalPos = Vector3.Lerp(start, end, progress);
            
            // Parabolic arc for vertical movement
            float verticalOffset = CalculateJumpArcHeight(height, progress);
            
            return new Vector3(horizontalPos.x, start.y + verticalOffset, horizontalPos.z);
        }

        /// <summary>
        /// Calculates the height offset for a parabolic jump arc.
        /// </summary>
        /// <param name="maxHeight">Maximum height of the arc</param>
        /// <param name="progress">Progress along the arc (0-1)</param>
        /// <returns>Height offset at the given progress</returns>
        public static float CalculateJumpArcHeight(float maxHeight, float progress)
        {
            // Parabolic arc: h = 4 * maxHeight * t * (1 - t)
            // This gives 0 at t=0, maxHeight at t=0.5, and 0 at t=1
            return 4f * maxHeight * progress * (1f - progress);
        }

        /// <summary>
        /// Validates the cooldown matches requirements.
        /// Requirement 4.2: 6 second cooldown.
        /// </summary>
        /// <param name="cooldown">The cooldown to validate</param>
        /// <returns>True if cooldown matches requirement</returns>
        public static bool ValidateCooldown(float cooldown)
        {
            const float RequiredCooldown = 6f;
            const float Tolerance = 0.001f;
            return Mathf.Abs(cooldown - RequiredCooldown) < Tolerance;
        }

        /// <summary>
        /// Calculates the total distance covered by a double jump.
        /// </summary>
        /// <param name="forwardDistancePerJump">Forward distance per jump</param>
        /// <returns>Total forward distance</returns>
        public static float CalculateTotalJumpDistance(float forwardDistancePerJump)
        {
            // Double jump covers distance twice
            return forwardDistancePerJump;
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
                Cooldown = config.AgileJumpCooldown;
            }
        }

        /// <summary>
        /// Sets the jumping state for testing.
        /// </summary>
        public void SetJumpingStateForTesting(bool isJumping, bool hasUsedSecondJump = false)
        {
            _isJumping = isJumping;
            _hasUsedSecondJump = hasUsedSecondJump;
        }
#endif

        private void OnDrawGizmosSelected()
        {
            // Draw jump trajectory preview
            if (_ownerTransform != null)
            {
                Gizmos.color = Color.cyan;
                Vector3 start = _ownerTransform.position;
                Vector3 end = start + _ownerTransform.forward * JumpForwardDistance;
                
                // Draw arc
                int segments = 20;
                Vector3 prevPos = start;
                for (int i = 1; i <= segments; i++)
                {
                    float t = (float)i / segments;
                    Vector3 pos = CalculateJumpPosition(start, end, FirstJumpHeight + SecondJumpHeight, t);
                    Gizmos.DrawLine(prevPos, pos);
                    prevPos = pos;
                }
            }
        }
        #endregion
    }
}
