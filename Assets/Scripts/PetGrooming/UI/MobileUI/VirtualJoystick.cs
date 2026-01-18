using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PetGrooming.Core;
using StarterAssets;

namespace PetGrooming.UI.MobileUI
{
    /// <summary>
    /// 虚拟摇杆组件，用于移动触摸输入。
    /// 实现触摸处理器以处理拖动输入并输出归一化方向向量。
    /// 支持动态重定位模式，其中摇杆出现在触摸位置。
    /// 
    /// 需求：1.1, 1.3, 1.4, 1.5, 1.6, 1.7
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
        
        [Header("UI引用")]
        [Tooltip("摇杆的外圈背景")]
        [SerializeField] private RectTransform _background;
        
        [Tooltip("摇杆的内圈可拖动手柄")]
        [SerializeField] private RectTransform _handle;
        
        [Tooltip("手柄的Image组件，用于透明度控制")]
        [SerializeField] private Image _handleImage;
        
        [Tooltip("背景的Image组件，用于透明度控制")]
        [SerializeField] private Image _backgroundImage;
        
        [Header("设置")]
        [Tooltip("移动HUD设置资源")]
        [SerializeField] private MobileHUDSettings _settings;
        
        [Tooltip("手柄移动范围，作为背景半径的倍数")]
        [SerializeField] private float _handleRange = 1f;
        
        [Tooltip("启用动态摇杆定位")]
        [SerializeField] private bool _dynamicPosition = true;
        
        [Tooltip("用于坐标转换的Canvas")]
        [SerializeField] private Canvas _canvas;
        
        [Header("动画")]
        [Tooltip("手柄返回中心的持续时间")]
        [SerializeField] private float _returnDuration = 0.1f;
        
        [Tooltip("空闲时手柄的透明度")]
        [SerializeField] private float _idleOpacity = 0.6f;
        
        [Tooltip("激活时手柄的透明度")]
        [SerializeField] private float _activeOpacity = 1f;
        
        [Header("玩家引用")]
        [Tooltip("对ThirdPersonController的StarterAssetsInputs引用")]
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
        /// 来自摇杆输入的归一化方向向量。
        /// X和Y分量的范围为[-1, 1]。
        /// </summary>
        public Vector2 Direction => _direction;
        
        /// <summary>
        /// 摇杆输入的大小（0到1）。
        /// </summary>
        public float Magnitude => _magnitude;
        
        /// <summary>
        /// 摇杆当前是否被触摸。
        /// </summary>
        public bool IsActive => _isActive;
        
        /// <summary>
        /// 组合输入向量（Direction * Magnitude）。
        /// </summary>
        public Vector2 InputVector => _direction * _magnitude;
        
        #endregion

        #region Events
        
        /// <summary>
        /// 当摇杆方向改变时触发。
        /// </summary>
        public event Action<Vector2> OnJoystickMoved;
        
        /// <summary>
        /// 当摇杆被释放时触发。
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
        /// 当指针在摇杆上按下时调用。
        /// 需求 1.3: 背景内的触摸将手柄移动到触摸位置。
        /// 需求 1.7: 动态重定位支持。
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
        /// 当指针被拖动时调用。
        /// 需求 1.4: 输出归一化方向向量。
        /// 需求 1.5: 将手柄限制在背景边缘。
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
        /// 当指针被释放时调用。
        /// 需求 1.6: 手柄以平滑动画返回中心。
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
        /// 将摇杆重置到初始状态。
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
        /// 应用来自MobileHUDSettings资源的设置。
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
        /// 设置设置资源并应用它。
        /// </summary>
        public void SetSettings(MobileHUDSettings settings)
        {
            _settings = settings;
            ApplySettings();
        }
        
        #endregion

        #region Static Methods (for testing)
        
        /// <summary>
        /// 计算给定输入偏移量的受限手柄位置。
        /// 用于基于属性的测试。
        /// 需求 1.5: 手柄限制在背景边缘。
        /// </summary>
        /// <param name="inputOffset">从中心的原始输入偏移</param>
        /// <param name="maxRadius">最大半径（背景半径 * 手柄范围）</param>
        /// <returns>受限的手柄位置</returns>
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
        /// 从手柄位置计算归一化方向。
        /// 用于基于属性的测试。
        /// 需求 1.4: 输出归一化方向向量。
        /// </summary>
        /// <param name="handlePosition">当前手柄位置</param>
        /// <returns>分量在[-1, 1]范围内的归一化方向</returns>
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
        /// 将方向分量限制在[-1, 1]范围内。
        /// 需求 1.4: 方向分量在[-1, 1]范围内。
        /// </summary>
        public static Vector2 ClampDirection(Vector2 direction)
        {
            return new Vector2(
                Mathf.Clamp(direction.x, -1f, 1f),
                Mathf.Clamp(direction.y, -1f, 1f)
            );
        }
        
        /// <summary>
        /// 检查手柄位置是否在有效半径内。
        /// 用于基于属性的测试。
        /// </summary>
        /// <param name="handlePosition">要检查的手柄位置</param>
        /// <param name="maxRadius">最大允许半径</param>
        /// <returns>如果位置在半径内或在半径上则为true</returns>
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
