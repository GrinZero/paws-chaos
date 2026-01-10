using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PetGrooming.Core;
using StarterAssets;

namespace PetGrooming.UI.MobileUI
{
    /// <summary>
    /// Virtual joystick component for mobile touch input.
    /// Implements touch handlers to process drag input and output normalized direction vectors.
    /// Supports dynamic repositioning mode where joystick appears at touch location.
    /// 
    /// Requirements: 1.1, 1.3, 1.4, 1.5, 1.6, 1.7
    /// </summary>
    /// <remarks>
    /// [已废弃] 此组件已被 Unity 官方 OnScreenStick 组件替代。
    /// 请使用 UnityEngine.InputSystem.OnScreen.OnScreenStick 代替。
    /// 迁移指南：参见 .kiro/specs/mobile-input-migration/design.md
    /// </remarks>
    [Obsolete("VirtualJoystick 已废弃，请使用 Unity 官方的 OnScreenStick 组件。参见 mobile-input-migration 规范。")]
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        #region Serialized Fields
        
        [Header("UI References")]
        [Tooltip("The outer circle background of the joystick")]
        [SerializeField] private RectTransform _background;
        
        [Tooltip("The inner draggable handle of the joystick")]
        [SerializeField] private RectTransform _handle;
        
        [Tooltip("Image component of the handle for opacity control")]
        [SerializeField] private Image _handleImage;
        
        [Tooltip("Image component of the background for opacity control")]
        [SerializeField] private Image _backgroundImage;
        
        [Header("Settings")]
        [Tooltip("Mobile HUD settings asset")]
        [SerializeField] private MobileHUDSettings _settings;
        
        [Tooltip("Handle movement range as multiplier of background radius")]
        [SerializeField] private float _handleRange = 1f;
        
        [Tooltip("Enable dynamic joystick positioning")]
        [SerializeField] private bool _dynamicPosition = true;
        
        [Tooltip("Canvas for coordinate conversion")]
        [SerializeField] private Canvas _canvas;
        
        [Header("Animation")]
        [Tooltip("Duration for handle to return to center")]
        [SerializeField] private float _returnDuration = 0.1f;
        
        [Tooltip("Handle opacity when idle")]
        [SerializeField] private float _idleOpacity = 0.6f;
        
        [Tooltip("Handle opacity when active")]
        [SerializeField] private float _activeOpacity = 1f;
        
        [Header("Player Reference")]
        [Tooltip("Reference to StarterAssetsInputs for ThirdPersonController")]
        [SerializeField] private StarterAssetsInputs _starterAssetsInputs;
        
        #endregion

        #region Private Fields
        
        private RectTransform _rectTransform;
        private Vector2 _startPosition;
        private Vector2 _direction;
        private float _magnitude;
        private bool _isActive;
        private float _backgroundRadius;
        private Coroutine _returnCoroutine;
        private int _currentPointerId = -1;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// Normalized direction vector from joystick input.
        /// X and Y components are in range [-1, 1].
        /// </summary>
        public Vector2 Direction => _direction;
        
        /// <summary>
        /// Magnitude of the joystick input (0 to 1).
        /// </summary>
        public float Magnitude => _magnitude;
        
        /// <summary>
        /// Whether the joystick is currently being touched.
        /// </summary>
        public bool IsActive => _isActive;
        
        /// <summary>
        /// Combined input vector (Direction * Magnitude).
        /// </summary>
        public Vector2 InputVector => _direction * _magnitude;
        
        #endregion

        #region Events
        
        /// <summary>
        /// Fired when joystick direction changes.
        /// </summary>
        public event Action<Vector2> OnJoystickMoved;
        
        /// <summary>
        /// Fired when joystick is released.
        /// </summary>
        public event Action OnJoystickReleased;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            
            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
            }
            
            ValidateReferences();
            ApplySettings();
        }
        
        private void Start()
        {
            _startPosition = _background.anchoredPosition;
            CalculateBackgroundRadius();
            ResetHandle();
        }
        
        private void OnDisable()
        {
            // Reset state when disabled
            ResetJoystick();
        }
        
        #endregion

        #region IPointerDownHandler
        
        /// <summary>
        /// Called when pointer is pressed down on the joystick.
        /// Requirement 1.3: Touch within background moves handle to touch position.
        /// Requirement 1.7: Dynamic repositioning support.
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (_isActive) return;
            
            _currentPointerId = eventData.pointerId;
            _isActive = true;
            
            // Stop any return animation
            if (_returnCoroutine != null)
            {
                StopCoroutine(_returnCoroutine);
                _returnCoroutine = null;
            }
            
            // Dynamic positioning: move joystick to touch location
            if (_dynamicPosition)
            {
                Vector2 localPoint;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rectTransform.parent as RectTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out localPoint))
                {
                    _background.anchoredPosition = localPoint;
                }
            }
            
            // Update handle opacity
            SetHandleOpacity(_activeOpacity);
            
            // Process the initial touch
            OnDrag(eventData);
        }
        
        #endregion

        #region IDragHandler
        
        /// <summary>
        /// Called when pointer is dragged.
        /// Requirement 1.4: Output normalized direction vector.
        /// Requirement 1.5: Clamp handle to background edge.
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            if (!_isActive || eventData.pointerId != _currentPointerId) return;
            
            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _background,
                eventData.position,
                eventData.pressEventCamera,
                out localPoint))
            {
                return;
            }
            
            // Calculate direction and magnitude
            Vector2 offset = localPoint;
            float distance = offset.magnitude;
            float maxDistance = _backgroundRadius * _handleRange;
            
            // Clamp handle position to background radius (Requirement 1.5)
            if (distance > maxDistance)
            {
                offset = offset.normalized * maxDistance;
                distance = maxDistance;
            }
            
            // Update handle position
            _handle.anchoredPosition = offset;
            
            // Calculate normalized direction (Requirement 1.4)
            _magnitude = Mathf.Clamp01(distance / maxDistance);
            _direction = distance > 0.001f ? offset.normalized : Vector2.zero;
            
            // Ensure direction components are clamped to [-1, 1]
            _direction = ClampDirection(_direction);
            
            // 发送输入到 ThirdPersonController（通过 StarterAssetsInputs）
            SendInputToPlayer();
            
            // Fire event
            OnJoystickMoved?.Invoke(_direction);
        }
        
        #endregion

        #region IPointerUpHandler
        
        /// <summary>
        /// Called when pointer is released.
        /// Requirement 1.6: Handle returns to center with smooth animation.
        /// </summary>
        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isActive || eventData.pointerId != _currentPointerId) return;
            
            _isActive = false;
            _currentPointerId = -1;
            
            // Reset direction and magnitude
            _direction = Vector2.zero;
            _magnitude = 0f;
            
            // 清零输入
            SendInputToPlayer();
            
            // Update handle opacity
            SetHandleOpacity(_idleOpacity);
            
            // Animate handle back to center
            if (_returnDuration > 0f && gameObject.activeInHierarchy)
            {
                _returnCoroutine = StartCoroutine(ReturnHandleToCenter());
            }
            else
            {
                ResetHandle();
            }
            
            // Reset background position if dynamic
            if (_dynamicPosition)
            {
                _background.anchoredPosition = _startPosition;
            }
            
            // Fire event
            OnJoystickReleased?.Invoke();
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Resets the joystick to its initial state.
        /// </summary>
        public void ResetJoystick()
        {
            _isActive = false;
            _currentPointerId = -1;
            _direction = Vector2.zero;
            _magnitude = 0f;
            
            if (_returnCoroutine != null)
            {
                StopCoroutine(_returnCoroutine);
                _returnCoroutine = null;
            }
            
            ResetHandle();
            SetHandleOpacity(_idleOpacity);
            
            if (_dynamicPosition && _background != null)
            {
                _background.anchoredPosition = _startPosition;
            }
        }
        
        /// <summary>
        /// Applies settings from MobileHUDSettings asset.
        /// </summary>
        public void ApplySettings()
        {
            if (_settings == null) return;
            
            _handleRange = _settings.HandleRange;
            _dynamicPosition = _settings.DynamicJoystick;
            _returnDuration = _settings.HandleReturnDuration;
            _idleOpacity = _settings.HandleIdleOpacity;
            _activeOpacity = _settings.HandleActiveOpacity;
            
            // Apply sizes
            if (_background != null)
            {
                _background.sizeDelta = new Vector2(_settings.JoystickSize, _settings.JoystickSize);
            }
            
            if (_handle != null)
            {
                _handle.sizeDelta = new Vector2(_settings.HandleSize, _settings.HandleSize);
            }
            
            CalculateBackgroundRadius();
        }
        
        /// <summary>
        /// Sets the settings asset and applies it.
        /// </summary>
        public void SetSettings(MobileHUDSettings settings)
        {
            _settings = settings;
            ApplySettings();
        }
        
        #endregion

        #region Static Methods (for testing)
        
        /// <summary>
        /// Calculates the clamped handle position given an input offset.
        /// Used for property-based testing.
        /// Requirement 1.5: Handle clamped to background edge.
        /// </summary>
        /// <param name="inputOffset">Raw input offset from center</param>
        /// <param name="maxRadius">Maximum radius (background radius * handle range)</param>
        /// <returns>Clamped handle position</returns>
        public static Vector2 CalculateClampedHandlePosition(Vector2 inputOffset, float maxRadius)
        {
            if (maxRadius <= 0f) return Vector2.zero;
            
            float distance = inputOffset.magnitude;
            if (distance <= maxRadius)
            {
                return inputOffset;
            }
            
            // Clamp to edge of circle
            return inputOffset.normalized * maxRadius;
        }
        
        /// <summary>
        /// Calculates the normalized direction from a handle position.
        /// Used for property-based testing.
        /// Requirement 1.4: Output normalized direction vector.
        /// </summary>
        /// <param name="handlePosition">Current handle position</param>
        /// <returns>Normalized direction with components in [-1, 1]</returns>
        public static Vector2 CalculateNormalizedDirection(Vector2 handlePosition)
        {
            if (handlePosition.sqrMagnitude < 0.0001f)
            {
                return Vector2.zero;
            }
            
            Vector2 normalized = handlePosition.normalized;
            return ClampDirection(normalized);
        }
        
        /// <summary>
        /// Clamps direction components to [-1, 1] range.
        /// Requirement 1.4: Direction components in range [-1, 1].
        /// </summary>
        public static Vector2 ClampDirection(Vector2 direction)
        {
            return new Vector2(
                Mathf.Clamp(direction.x, -1f, 1f),
                Mathf.Clamp(direction.y, -1f, 1f)
            );
        }
        
        /// <summary>
        /// Checks if a handle position is within the valid radius.
        /// Used for property-based testing.
        /// </summary>
        /// <param name="handlePosition">Handle position to check</param>
        /// <param name="maxRadius">Maximum allowed radius</param>
        /// <returns>True if position is within or at the radius</returns>
        public static bool IsHandleWithinRadius(Vector2 handlePosition, float maxRadius)
        {
            return handlePosition.magnitude <= maxRadius + 0.0001f; // Small epsilon for float comparison
        }
        
        #endregion

        #region Private Methods
        
        private void ValidateReferences()
        {
            if (_background == null)
            {
                Debug.LogError("[VirtualJoystick] Background RectTransform is not assigned!");
            }
            
            if (_handle == null)
            {
                Debug.LogError("[VirtualJoystick] Handle RectTransform is not assigned!");
            }
            
            if (_handleImage == null && _handle != null)
            {
                _handleImage = _handle.GetComponent<Image>();
            }
            
            // 自动获取背景 Image 组件
            if (_backgroundImage == null && _background != null)
            {
                _backgroundImage = _background.GetComponent<Image>();
            }
            
            // 自动查找 StarterAssetsInputs（如果未手动指定）
            if (_starterAssetsInputs == null)
            {
                _starterAssetsInputs = FindFirstObjectByType<StarterAssetsInputs>();
            }
        }
        
        private void CalculateBackgroundRadius()
        {
            if (_background != null)
            {
                _backgroundRadius = _background.sizeDelta.x * 0.5f;
            }
        }
        
        private void ResetHandle()
        {
            if (_handle != null)
            {
                _handle.anchoredPosition = Vector2.zero;
            }
        }
        
        private void SetHandleOpacity(float opacity)
        {
            // 设置手柄透明度
            if (_handleImage != null)
            {
                Color color = _handleImage.color;
                color.a = opacity;
                _handleImage.color = color;
            }
            
            // 同时设置背景透明度（稍微降低一点）
            if (_backgroundImage != null)
            {
                Color bgColor = _backgroundImage.color;
                bgColor.a = opacity * 0.8f; // 背景透明度略低于手柄
                _backgroundImage.color = bgColor;
            }
        }
        
        /// <summary>
        /// 发送输入到 ThirdPersonController（通过 StarterAssetsInputs）
        /// </summary>
        private void SendInputToPlayer()
        {
            if (_starterAssetsInputs != null)
            {
                // InputVector = Direction * Magnitude，提供模拟摇杆的渐进输入
                _starterAssetsInputs.MoveInput(InputVector);
            }
        }
        
        private System.Collections.IEnumerator ReturnHandleToCenter()
        {
            Vector2 startPos = _handle.anchoredPosition;
            float elapsed = 0f;
            
            while (elapsed < _returnDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _returnDuration;
                t = t * t * (3f - 2f * t); // Smoothstep
                
                _handle.anchoredPosition = Vector2.Lerp(startPos, Vector2.zero, t);
                yield return null;
            }
            
            _handle.anchoredPosition = Vector2.zero;
            _returnCoroutine = null;
        }
        
        #endregion

        #region Editor Support
#if UNITY_EDITOR
        /// <summary>
        /// Sets references for testing purposes.
        /// </summary>
        public void SetReferencesForTesting(RectTransform background, RectTransform handle, Image handleImage)
        {
            _background = background;
            _handle = handle;
            _handleImage = handleImage;
            CalculateBackgroundRadius();
        }
        
        /// <summary>
        /// Sets configuration for testing purposes.
        /// </summary>
        public void SetConfigForTesting(float handleRange, bool dynamicPosition, float returnDuration)
        {
            _handleRange = handleRange;
            _dynamicPosition = dynamicPosition;
            _returnDuration = returnDuration;
        }
        
        /// <summary>
        /// Gets the background radius for testing.
        /// </summary>
        public float GetBackgroundRadiusForTesting()
        {
            return _backgroundRadius;
        }
        
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                ApplySettings();
            }
        }
#endif
        #endregion
    }
}
