using UnityEngine;
using UnityEngine.InputSystem;
using PetGrooming.Core;

namespace PetGrooming.Systems
{
    /// <summary>
    /// Handles player movement input and character controller movement.
    /// Supports both WASD and Arrow keys, with camera-relative movement.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Configuration")]
        [SerializeField] private GameConfig _gameConfig;

        [Header("Movement Settings")]
        [Tooltip("Base movement speed")]
        [SerializeField] private float _moveSpeed = 5f;

        [Tooltip("Rotation speed for turning")]
        [SerializeField] private float _rotationSpeed = 720f;

        [Tooltip("Gravity applied to the character")]
        [SerializeField] private float _gravity = -20f;

        [Header("Camera Reference")]
        [Tooltip("Reference to the main camera for camera-relative movement")]
        [SerializeField] private Transform _cameraTransform;

        #endregion

        #region Private Fields

        private CharacterController _characterController;
        private GroomerController _groomerController;
        private Vector3 _velocity;
        private Vector3 _moveDirection;
        private bool _isGrounded;
        private Vector2 _mobileInput;
        private bool _useMobileInput;

        #endregion

        #region Properties

        /// <summary>
        /// Base movement speed from config or serialized field.
        /// </summary>
        public float BaseMoveSpeed => _gameConfig != null ? _gameConfig.GroomerMoveSpeed : _moveSpeed;

        /// <summary>
        /// Current effective movement speed (accounting for carry state and alert).
        /// </summary>
        public float CurrentMoveSpeed
        {
            get
            {
                if (_groomerController != null)
                {
                    return _groomerController.CurrentMoveSpeed;
                }
                return BaseMoveSpeed;
            }
        }

        /// <summary>
        /// Whether the player is currently grounded.
        /// </summary>
        public bool IsGrounded => _isGrounded;

        /// <summary>
        /// Current movement direction (normalized).
        /// </summary>
        public Vector3 MoveDirection => _moveDirection;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _groomerController = GetComponent<GroomerController>();

            if (_cameraTransform == null && Camera.main != null)
            {
                _cameraTransform = Camera.main.transform;
            }
        }

        private void Update()
        {
            // Allow movement even if game hasn't started (for testing)
            // Only block if game is paused or ended
            if (GameManager.Instance != null)
            {
                var state = GameManager.Instance.CurrentState;
                if (state == GameManager.GameState.Paused ||
                    state == GameManager.GameState.GroomerWin ||
                    state == GameManager.GameState.PetWin)
                {
                    return;
                }
            }

            HandleGroundCheck();
            HandleMovementInput();
            ApplyGravity();
            ApplyMovement();
        }

        #endregion

        #region Private Methods

        private void HandleGroundCheck()
        {
            _isGrounded = _characterController.isGrounded;

            if (_isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f; // Small downward force to keep grounded
            }
        }

        private void HandleMovementInput()
        {
            float horizontal;
            float vertical;
            
            // Check for mobile input first
            // Requirement 1.8: Mobile joystick input equivalent to keyboard/gamepad
            if (_useMobileInput && _mobileInput.sqrMagnitude > 0.01f)
            {
                horizontal = _mobileInput.x;
                vertical = _mobileInput.y;
            }
            else
            {
                // Use new Input System for keyboard input
                var keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    horizontal = 0f;
                    vertical = 0f;
                    
                    // WASD
                    if (keyboard.aKey.isPressed) horizontal -= 1f;
                    if (keyboard.dKey.isPressed) horizontal += 1f;
                    if (keyboard.wKey.isPressed) vertical += 1f;
                    if (keyboard.sKey.isPressed) vertical -= 1f;
                    
                    // Arrow keys
                    if (keyboard.leftArrowKey.isPressed) horizontal -= 1f;
                    if (keyboard.rightArrowKey.isPressed) horizontal += 1f;
                    if (keyboard.upArrowKey.isPressed) vertical += 1f;
                    if (keyboard.downArrowKey.isPressed) vertical -= 1f;
                    
                    // Clamp to -1, 1
                    horizontal = Mathf.Clamp(horizontal, -1f, 1f);
                    vertical = Mathf.Clamp(vertical, -1f, 1f);
                }
                else
                {
                    horizontal = 0f;
                    vertical = 0f;
                }
            }

            // Calculate input direction
            Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;

            if (inputDirection.magnitude >= 0.1f)
            {
                // Convert to camera-relative direction
                _moveDirection = GetCameraRelativeDirection(inputDirection);

                // Rotate character to face movement direction
                RotateTowardsMovement(_moveDirection);
            }
            else
            {
                _moveDirection = Vector3.zero;
            }
        }

        private Vector3 GetCameraRelativeDirection(Vector3 inputDirection)
        {
            if (_cameraTransform == null)
            {
                return inputDirection;
            }

            // Get camera forward and right vectors (flattened to horizontal plane)
            Vector3 cameraForward = _cameraTransform.forward;
            Vector3 cameraRight = _cameraTransform.right;

            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();

            // Calculate world-space movement direction relative to camera
            Vector3 worldDirection = cameraForward * inputDirection.z + cameraRight * inputDirection.x;
            return worldDirection.normalized;
        }

        private void RotateTowardsMovement(Vector3 direction)
        {
            if (direction.magnitude < 0.1f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                _rotationSpeed * Time.deltaTime);
        }

        private void ApplyGravity()
        {
            _velocity.y += _gravity * Time.deltaTime;
        }

        private void ApplyMovement()
        {
            // Calculate horizontal movement
            Vector3 horizontalMovement = _moveDirection * CurrentMoveSpeed * Time.deltaTime;

            // Combine with vertical velocity (gravity)
            Vector3 finalMovement = horizontalMovement + new Vector3(0f, _velocity.y * Time.deltaTime, 0f);

            // Apply movement through CharacterController
            _characterController.Move(finalMovement);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the camera transform for camera-relative movement.
        /// </summary>
        public void SetCameraTransform(Transform cameraTransform)
        {
            _cameraTransform = cameraTransform;
        }

        /// <summary>
        /// Applies an external force/knockback to the player.
        /// </summary>
        public void ApplyKnockback(Vector3 force)
        {
            _velocity += force;
        }
        
        /// <summary>
        /// Sets mobile input from virtual joystick.
        /// Requirement 1.8: Mobile joystick input equivalent to keyboard/gamepad.
        /// </summary>
        /// <param name="input">Normalized input vector from joystick (-1 to 1 on both axes)</param>
        public void SetMobileInput(Vector2 input)
        {
            _mobileInput = input;
            _useMobileInput = input.sqrMagnitude > 0.01f;
        }
        
        /// <summary>
        /// Clears mobile input.
        /// </summary>
        public void ClearMobileInput()
        {
            _mobileInput = Vector2.zero;
            _useMobileInput = false;
        }

        #endregion

        #region Editor Support

#if UNITY_EDITOR
        public void SetGameConfigForTesting(GameConfig config)
        {
            _gameConfig = config;
        }
#endif

        #endregion
    }
}
