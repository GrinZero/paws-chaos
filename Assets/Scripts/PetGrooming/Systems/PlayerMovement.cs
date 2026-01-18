using System;
using UnityEngine;
using UnityEngine.InputSystem;
using PetGrooming.Core;

namespace PetGrooming.Systems
{
    /// <summary>
    /// 处理玩家移动输入以及 CharacterController 的移动。
    /// 同时支持 WASD 和方向键，并使用相机朝向进行相对移动。
    /// </summary>
    /// <remarks>
    /// [已废弃] 此组件已被 Unity Starter Assets 的 ThirdPersonController 替代。
    /// ThirdPersonController 通过 StarterAssetsInputs 接收输入，
    /// 移动端输入由 OnScreenStick 自动发送到 PlayerInput 组件。
    /// 迁移指南：参见 .kiro/specs/mobile-input-migration/design.md
    /// </remarks>
    [Obsolete("PlayerMovement 已废弃，请使用 ThirdPersonController + StarterAssetsInputs。参见 mobile-input-migration 规范。")]
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Configuration")]
        [SerializeField] private GameConfig _gameConfig;

        [Header("Movement Settings")]
        [Tooltip("Base movement speed")]
        [SerializeField] private float _moveSpeed = 5f;

        [Tooltip("Rotation smooth time (lower = faster rotation)")]
        [Range(0.0f, 0.3f)]
        [SerializeField] private float _rotationSmoothTime = 0.12f;

        [Tooltip("Gravity applied to the character")]
        [SerializeField] private float _gravity = -20f;

        [Header("Camera Reference")]
        [Tooltip("Reference to the main camera for camera-relative movement")]
        [SerializeField] private Transform _cameraTransform;

        [Header("Animation")]
        [Tooltip("Reference to the Animator component")]
        [SerializeField] private Animator _animator;
        
        [Tooltip("Animator parameter name for movement speed")]
        [SerializeField] private string _speedParameterName = "Speed";
        
        [Tooltip("Animator parameter name for motion speed")]
        [SerializeField] private string _motionSpeedParameterName = "MotionSpeed";

        #endregion

        #region Private Fields

        private CharacterController _characterController;
        private GroomerController _groomerController;
        private Vector3 _velocity;
        private Vector3 _moveDirection;
        private bool _isGrounded;
        private Vector2 _mobileInput;
        private bool _useMobileInput;
        
        // 动画参数 ID（缓存以提高性能）
        private int _animIDSpeed;
        private int _animIDMotionSpeed;
        private float _animationBlend;
        
        // 旋转平滑
        private float _targetRotation;
        private float _rotationVelocity;

        #endregion

        #region Properties

        /// <summary>
        /// 来自配置或序列化字段的基础移动速度。
        /// </summary>
        public float BaseMoveSpeed => _gameConfig != null ? _gameConfig.GroomerMoveSpeed : _moveSpeed;

        /// <summary>
        /// 当前实际移动速度（考虑抱宠减速与警戒加速）。
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
        /// 玩家当前是否在地面上。
        /// </summary>
        public bool IsGrounded => _isGrounded;

        /// <summary>
        /// 当前的移动方向（归一化向量）。
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
            
            // 获取 Animator 组件
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }
            
            // 缓存动画参数 ID
            if (_animator != null)
            {
                _animIDSpeed = Animator.StringToHash(_speedParameterName);
                _animIDMotionSpeed = Animator.StringToHash(_motionSpeedParameterName);
            }
        }

        private void Update()
        {
            // 即使游戏尚未开始也允许移动（便于测试）
            // 仅在游戏暂停或结束时才阻止移动
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
                _velocity.y = -2f; // 施加一个很小的向下力以保持在地面上
            }
        }

        private void HandleMovementInput()
        {
            float horizontal;
            float vertical;
            
            // 优先检查移动端输入
            // 需求 1.8：虚拟摇杆输入等价于键盘/手柄
            if (_useMobileInput && _mobileInput.sqrMagnitude > 0.01f)
            {
                horizontal = _mobileInput.x;
                vertical = _mobileInput.y;
            }
            else
            {
                // 使用新输入系统处理键盘输入
                var keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    horizontal = 0f;
                    vertical = 0f;
                    
                    // WASD 键
                    if (keyboard.aKey.isPressed) horizontal -= 1f;
                    if (keyboard.dKey.isPressed) horizontal += 1f;
                    if (keyboard.wKey.isPressed) vertical += 1f;
                    if (keyboard.sKey.isPressed) vertical -= 1f;
                    
                    // 方向键
                    if (keyboard.leftArrowKey.isPressed) horizontal -= 1f;
                    if (keyboard.rightArrowKey.isPressed) horizontal += 1f;
                    if (keyboard.upArrowKey.isPressed) vertical += 1f;
                    if (keyboard.downArrowKey.isPressed) vertical -= 1f;
                    
                    // 将输入限制在 -1 到 1 之间
                    horizontal = Mathf.Clamp(horizontal, -1f, 1f);
                    vertical = Mathf.Clamp(vertical, -1f, 1f);
                }
                else
                {
                    horizontal = 0f;
                    vertical = 0f;
                }
            }

            // 计算输入方向
            Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;

            if (inputDirection.magnitude >= 0.1f)
            {
                // 转换为相机朝向相关的世界方向
                _moveDirection = GetCameraRelativeDirection(inputDirection);

                // 旋转角色朝向移动方向
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

            // 获取相机的前向与右向向量（投影到水平面）
            Vector3 cameraForward = _cameraTransform.forward;
            Vector3 cameraRight = _cameraTransform.right;

            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();

            // 按照相机方向计算世界空间中的移动向量
            Vector3 worldDirection = cameraForward * inputDirection.z + cameraRight * inputDirection.x;
            return worldDirection.normalized;
        }

        private void RotateTowardsMovement(Vector3 direction)
        {
            if (direction.magnitude < 0.1f)
            {
                return;
            }

            // 使用 SmoothDampAngle 实现平滑旋转（参考 ThirdPersonController）
            // 计算目标旋转角度（相机方向已在 GetCameraRelativeDirection 中处理）
            _targetRotation = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            
            // 使用平滑阻尼插值，避免方向突变时的抖动
            float rotation = Mathf.SmoothDampAngle(
                transform.eulerAngles.y, 
                _targetRotation, 
                ref _rotationVelocity, 
                _rotationSmoothTime);
            
            // 只旋转 Y 轴
            transform.rotation = Quaternion.Euler(0f, rotation, 0f);
        }

        private void ApplyGravity()
        {
            _velocity.y += _gravity * Time.deltaTime;
        }

        private void ApplyMovement()
        {
            // 计算水平方向的位移
            Vector3 horizontalMovement = _moveDirection * CurrentMoveSpeed * Time.deltaTime;

            // 与竖直方向（重力）的位移合并
            Vector3 finalMovement = horizontalMovement + new Vector3(0f, _velocity.y * Time.deltaTime, 0f);

            // 通过 CharacterController 应用移动
            _characterController.Move(finalMovement);
            
            // 更新动画参数
            UpdateAnimation();
        }
        
        private void UpdateAnimation()
        {
            if (_animator == null) return;
            
            // 计算目标速度（基于移动方向的模长）
            float targetSpeed = _moveDirection.magnitude > 0.1f ? CurrentMoveSpeed : 0f;
            
            // 平滑过渡动画混合值
            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * 10f);
            if (_animationBlend < 0.01f) _animationBlend = 0f;
            
            // 设置动画参数
            _animator.SetFloat(_animIDSpeed, _animationBlend);
            _animator.SetFloat(_animIDMotionSpeed, _moveDirection.magnitude > 0.1f ? 1f : 0f);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 设置用于相对移动计算的相机 Transform。
        /// </summary>
        public void SetCameraTransform(Transform cameraTransform)
        {
            _cameraTransform = cameraTransform;
        }

        /// <summary>
        /// 对玩家施加外力/击退效果。
        /// </summary>
        public void ApplyKnockback(Vector3 force)
        {
            _velocity += force;
        }
        
        /// <summary>
        /// 设置来自虚拟摇杆的移动端输入。
        /// 需求 1.8：移动端摇杆输入等价于键盘/手柄。
        /// </summary>
        /// <param name="input">摇杆归一化输入向量（两个分量范围均为 -1 到 1）。</param>
        public void SetMobileInput(Vector2 input)
        {
            _mobileInput = input;
            _useMobileInput = input.sqrMagnitude > 0.01f;
        }
        
        /// <summary>
        /// 清除移动端输入。
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
