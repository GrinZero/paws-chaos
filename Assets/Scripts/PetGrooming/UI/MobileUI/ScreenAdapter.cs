using System;
using UnityEngine;
using PetGrooming.Core;

namespace PetGrooming.UI.MobileUI
{
    /// <summary>
    /// Screen adapter component that handles UI scaling and positioning
    /// based on screen size and aspect ratio.
    /// Ensures UI elements remain visible and properly positioned across different devices.
    /// 
    /// Requirements: 3.6
    /// Property 8: Screen Size Adaptation
    /// </summary>
    public class ScreenAdapter : MonoBehaviour
    {
        #region Constants
        
        /// <summary>
        /// Reference screen width for UI design (1920x1080 baseline).
        /// </summary>
        public const float ReferenceWidth = 1920f;
        
        /// <summary>
        /// Reference screen height for UI design.
        /// </summary>
        public const float ReferenceHeight = 1080f;
        
        /// <summary>
        /// Minimum scale factor to prevent UI from becoming too small.
        /// </summary>
        public const float MinScaleFactor = 0.5f;
        
        /// <summary>
        /// Maximum scale factor to prevent UI from becoming too large.
        /// </summary>
        public const float MaxScaleFactor = 1.5f;
        
        /// <summary>
        /// Safe area margin in pixels (for notches, rounded corners).
        /// </summary>
        public const float SafeAreaMargin = 20f;
        
        #endregion

        #region Serialized Fields
        
        [Header("UI References")]
        [Tooltip("RectTransform of the joystick container")]
        [SerializeField] private RectTransform _joystickContainer;
        
        [Tooltip("RectTransform of the skill wheel container")]
        [SerializeField] private RectTransform _skillWheelContainer;
        
        [Tooltip("RectTransform of the struggle button container")]
        [SerializeField] private RectTransform _struggleButtonContainer;
        
        [Header("Settings")]
        [Tooltip("Mobile HUD settings asset")]
        [SerializeField] private MobileHUDSettings _settings;
        
        [Tooltip("Enable automatic adaptation on screen size change")]
        [SerializeField] private bool _autoAdapt = true;
        
        [Tooltip("Use device safe area for positioning")]
        [SerializeField] private bool _useSafeArea = true;
        
        #endregion

        #region Private Fields
        
        private Canvas _canvas;
        private RectTransform _canvasRect;
        private Vector2 _lastScreenSize;
        private float _currentScaleFactor = 1f;
        private Rect _currentSafeArea;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// Current scale factor applied to UI elements.
        /// </summary>
        public float CurrentScaleFactor => _currentScaleFactor;
        
        /// <summary>
        /// Current safe area rect.
        /// </summary>
        public Rect CurrentSafeArea => _currentSafeArea;
        
        /// <summary>
        /// Whether auto-adaptation is enabled.
        /// </summary>
        public bool AutoAdapt
        {
            get => _autoAdapt;
            set => _autoAdapt = value;
        }
        
        #endregion

        #region Events
        
        /// <summary>
        /// Fired when screen adaptation is applied.
        /// </summary>
        public event Action<float> OnAdaptationApplied;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas != null)
            {
                _canvasRect = _canvas.GetComponent<RectTransform>();
            }
            
            _lastScreenSize = new Vector2(Screen.width, Screen.height);
            _currentSafeArea = Screen.safeArea;
        }
        
        private void Start()
        {
            ApplyAdaptation();
        }
        
        private void Update()
        {
            if (!_autoAdapt) return;
            
            // Check for screen size changes
            Vector2 currentScreenSize = new Vector2(Screen.width, Screen.height);
            Rect currentSafeArea = Screen.safeArea;
            
            if (currentScreenSize != _lastScreenSize || currentSafeArea != _currentSafeArea)
            {
                _lastScreenSize = currentScreenSize;
                _currentSafeArea = currentSafeArea;
                ApplyAdaptation();
            }
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Applies screen adaptation to all UI elements.
        /// </summary>
        public void ApplyAdaptation()
        {
            // Calculate scale factor
            _currentScaleFactor = CalculateScaleFactor(Screen.width, Screen.height);
            
            // Get safe area bounds
            Rect safeArea = _useSafeArea ? Screen.safeArea : new Rect(0, 0, Screen.width, Screen.height);
            
            // Apply to joystick
            if (_joystickContainer != null)
            {
                ApplyJoystickAdaptation(_joystickContainer, _currentScaleFactor, safeArea);
            }
            
            // Apply to skill wheel
            if (_skillWheelContainer != null)
            {
                ApplySkillWheelAdaptation(_skillWheelContainer, _currentScaleFactor, safeArea);
            }
            
            // Apply to struggle button
            if (_struggleButtonContainer != null)
            {
                ApplyStruggleButtonAdaptation(_struggleButtonContainer, _currentScaleFactor, safeArea);
            }
            
            OnAdaptationApplied?.Invoke(_currentScaleFactor);
        }
        
        /// <summary>
        /// Sets the settings asset and re-applies adaptation.
        /// </summary>
        public void SetSettings(MobileHUDSettings settings)
        {
            _settings = settings;
            ApplyAdaptation();
        }
        
        /// <summary>
        /// Sets UI container references.
        /// </summary>
        public void SetContainers(RectTransform joystick, RectTransform skillWheel, RectTransform struggleButton)
        {
            _joystickContainer = joystick;
            _skillWheelContainer = skillWheel;
            _struggleButtonContainer = struggleButton;
        }
        
        #endregion

        #region Static Methods (for testing)
        
        /// <summary>
        /// Calculates the scale factor based on screen dimensions.
        /// Used for property-based testing.
        /// 
        /// Property 8: Screen Size Adaptation
        /// Validates: Requirements 3.6
        /// </summary>
        /// <param name="screenWidth">Current screen width in pixels</param>
        /// <param name="screenHeight">Current screen height in pixels</param>
        /// <returns>Scale factor clamped between MinScaleFactor and MaxScaleFactor</returns>
        public static float CalculateScaleFactor(float screenWidth, float screenHeight)
        {
            if (screenWidth <= 0 || screenHeight <= 0)
            {
                return 1f;
            }
            
            // Calculate scale based on the smaller dimension ratio
            float widthRatio = screenWidth / ReferenceWidth;
            float heightRatio = screenHeight / ReferenceHeight;
            
            // Use the smaller ratio to ensure UI fits on screen
            float scaleFactor = Mathf.Min(widthRatio, heightRatio);
            
            // Clamp to valid range
            return Mathf.Clamp(scaleFactor, MinScaleFactor, MaxScaleFactor);
        }
        
        /// <summary>
        /// Calculates the adapted position for the joystick.
        /// Ensures joystick stays in bottom-left quadrant within safe area.
        /// 
        /// Property 8: Screen Size Adaptation
        /// </summary>
        /// <param name="baseOffset">Base offset from corner</param>
        /// <param name="joystickSize">Size of the joystick</param>
        /// <param name="scaleFactor">Current scale factor</param>
        /// <param name="safeArea">Safe area rect</param>
        /// <param name="screenSize">Screen size</param>
        /// <returns>Adapted position that keeps joystick within bounds</returns>
        public static Vector2 CalculateJoystickPosition(
            Vector2 baseOffset, 
            float joystickSize, 
            float scaleFactor,
            Rect safeArea,
            Vector2 screenSize)
        {
            float scaledSize = joystickSize * scaleFactor;
            float halfSize = scaledSize / 2f;
            
            // Calculate position from bottom-left of safe area
            float x = safeArea.xMin + baseOffset.x * scaleFactor;
            float y = safeArea.yMin + baseOffset.y * scaleFactor;
            
            // Ensure joystick stays within safe area bounds
            x = Mathf.Max(x, safeArea.xMin + halfSize + SafeAreaMargin);
            y = Mathf.Max(y, safeArea.yMin + halfSize + SafeAreaMargin);
            
            // Ensure joystick doesn't exceed center of screen (stays in left half)
            x = Mathf.Min(x, screenSize.x / 2f - halfSize);
            y = Mathf.Min(y, screenSize.y / 2f);
            
            return new Vector2(x, y);
        }
        
        /// <summary>
        /// Calculates the adapted position for the skill wheel.
        /// Ensures skill wheel stays in bottom-right quadrant within safe area.
        /// 
        /// Property 8: Screen Size Adaptation
        /// </summary>
        /// <param name="baseOffset">Base offset from corner (negative x for right side)</param>
        /// <param name="wheelRadius">Radius of the skill wheel</param>
        /// <param name="scaleFactor">Current scale factor</param>
        /// <param name="safeArea">Safe area rect</param>
        /// <param name="screenSize">Screen size</param>
        /// <returns>Adapted position that keeps skill wheel within bounds</returns>
        public static Vector2 CalculateSkillWheelPosition(
            Vector2 baseOffset,
            float wheelRadius,
            float scaleFactor,
            Rect safeArea,
            Vector2 screenSize)
        {
            float scaledRadius = wheelRadius * scaleFactor;
            
            // Calculate position from bottom-right of safe area
            float x = safeArea.xMax + baseOffset.x * scaleFactor;
            float y = safeArea.yMin + baseOffset.y * scaleFactor;
            
            // Ensure skill wheel stays within safe area bounds (right edge)
            x = Mathf.Min(x, safeArea.xMax - scaledRadius - SafeAreaMargin);
            
            // Ensure skill wheel stays within safe area bounds (bottom edge)
            y = Mathf.Max(y, safeArea.yMin + scaledRadius + SafeAreaMargin);
            
            // Ensure skill wheel doesn't go above half screen
            y = Mathf.Min(y, screenSize.y / 2f);
            
            // Ensure skill wheel center stays in right half of screen
            // But also ensure it doesn't go off the left edge
            float minX = screenSize.x / 2f;
            
            // If screen is too narrow, prioritize keeping within bounds over quadrant
            if (minX + scaledRadius > safeArea.xMax - SafeAreaMargin)
            {
                // Screen is too narrow - center the wheel as best we can
                x = Mathf.Max(scaledRadius + SafeAreaMargin, Mathf.Min(x, safeArea.xMax - scaledRadius - SafeAreaMargin));
            }
            else
            {
                x = Mathf.Max(x, minX);
            }
            
            // Final bounds check - ensure wheel is fully within screen
            x = Mathf.Clamp(x, scaledRadius + SafeAreaMargin, screenSize.x - scaledRadius - SafeAreaMargin);
            y = Mathf.Clamp(y, scaledRadius + SafeAreaMargin, screenSize.y - scaledRadius - SafeAreaMargin);
            
            return new Vector2(x, y);
        }
        
        /// <summary>
        /// Validates that a UI element is within screen bounds.
        /// Used for property-based testing.
        /// 
        /// Property 8: Screen Size Adaptation
        /// </summary>
        /// <param name="position">Center position of the element</param>
        /// <param name="size">Size of the element</param>
        /// <param name="screenSize">Screen dimensions</param>
        /// <returns>True if element is fully within screen bounds</returns>
        public static bool IsWithinScreenBounds(Vector2 position, Vector2 size, Vector2 screenSize)
        {
            float halfWidth = size.x / 2f;
            float halfHeight = size.y / 2f;
            
            // Check all edges
            bool leftOk = position.x - halfWidth >= 0;
            bool rightOk = position.x + halfWidth <= screenSize.x;
            bool bottomOk = position.y - halfHeight >= 0;
            bool topOk = position.y + halfHeight <= screenSize.y;
            
            return leftOk && rightOk && bottomOk && topOk;
        }
        
        /// <summary>
        /// Validates that joystick is in the bottom-left quadrant.
        /// Used for property-based testing.
        /// 
        /// Property 8: Screen Size Adaptation
        /// </summary>
        /// <param name="position">Center position of the joystick</param>
        /// <param name="screenSize">Screen dimensions</param>
        /// <returns>True if joystick center is in bottom-left quadrant</returns>
        public static bool IsInBottomLeftQuadrant(Vector2 position, Vector2 screenSize)
        {
            return position.x <= screenSize.x / 2f && position.y <= screenSize.y / 2f;
        }
        
        /// <summary>
        /// Validates that skill wheel is in the bottom-right quadrant.
        /// Used for property-based testing.
        /// 
        /// Property 8: Screen Size Adaptation
        /// </summary>
        /// <param name="position">Center position of the skill wheel</param>
        /// <param name="screenSize">Screen dimensions</param>
        /// <returns>True if skill wheel center is in bottom-right quadrant</returns>
        public static bool IsInBottomRightQuadrant(Vector2 position, Vector2 screenSize)
        {
            return position.x >= screenSize.x / 2f && position.y <= screenSize.y / 2f;
        }
        
        /// <summary>
        /// Calculates the total bounds of a UI element including its children.
        /// </summary>
        /// <param name="centerPosition">Center position</param>
        /// <param name="radius">Radius or half-size</param>
        /// <returns>Bounds rect</returns>
        public static Rect CalculateElementBounds(Vector2 centerPosition, float radius)
        {
            return new Rect(
                centerPosition.x - radius,
                centerPosition.y - radius,
                radius * 2f,
                radius * 2f
            );
        }
        
        /// <summary>
        /// Validates that two UI elements don't overlap.
        /// </summary>
        /// <param name="bounds1">First element bounds</param>
        /// <param name="bounds2">Second element bounds</param>
        /// <returns>True if elements don't overlap</returns>
        public static bool ElementsDoNotOverlap(Rect bounds1, Rect bounds2)
        {
            return !bounds1.Overlaps(bounds2);
        }
        
        #endregion

        #region Private Methods
        
        private void ApplyJoystickAdaptation(RectTransform container, float scaleFactor, Rect safeArea)
        {
            if (_settings == null) return;
            
            // Apply scale
            container.localScale = Vector3.one * scaleFactor;
            
            // Calculate and apply position
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            Vector2 position = CalculateJoystickPosition(
                _settings.JoystickOffset,
                _settings.JoystickSize,
                scaleFactor,
                safeArea,
                screenSize
            );
            
            // Convert screen position to canvas position
            if (_canvasRect != null)
            {
                Vector2 canvasPosition = ScreenToCanvasPosition(position);
                container.anchoredPosition = canvasPosition;
            }
        }
        
        private void ApplySkillWheelAdaptation(RectTransform container, float scaleFactor, Rect safeArea)
        {
            if (_settings == null) return;
            
            // Apply scale
            container.localScale = Vector3.one * scaleFactor;
            
            // Calculate total wheel radius (capture button + arc radius + skill button)
            float totalRadius = _settings.ArcRadius + _settings.SkillButtonSize;
            
            // Calculate and apply position
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            Vector2 position = CalculateSkillWheelPosition(
                _settings.SkillWheelOffset,
                totalRadius,
                scaleFactor,
                safeArea,
                screenSize
            );
            
            // Convert screen position to canvas position
            if (_canvasRect != null)
            {
                Vector2 canvasPosition = ScreenToCanvasPosition(position);
                container.anchoredPosition = canvasPosition;
            }
        }
        
        private void ApplyStruggleButtonAdaptation(RectTransform container, float scaleFactor, Rect safeArea)
        {
            if (_settings == null) return;
            
            // Apply scale
            container.localScale = Vector3.one * scaleFactor;
            
            // Struggle button is positioned at center-right
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            float scaledSize = _settings.StruggleButtonSize * scaleFactor;
            
            // Position at center-right of safe area
            float x = safeArea.xMax + _settings.StruggleButtonOffset.x * scaleFactor;
            float y = safeArea.center.y + _settings.StruggleButtonOffset.y * scaleFactor;
            
            // Ensure within bounds
            x = Mathf.Min(x, safeArea.xMax - scaledSize / 2f - SafeAreaMargin);
            
            Vector2 position = new Vector2(x, y);
            
            // Convert screen position to canvas position
            if (_canvasRect != null)
            {
                Vector2 canvasPosition = ScreenToCanvasPosition(position);
                container.anchoredPosition = canvasPosition;
            }
        }
        
        private Vector2 ScreenToCanvasPosition(Vector2 screenPosition)
        {
            if (_canvas == null || _canvasRect == null) return screenPosition;
            
            // For Screen Space - Overlay canvas
            if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // Convert from screen coordinates to canvas coordinates
                Vector2 canvasSize = _canvasRect.sizeDelta;
                Vector2 screenSize = new Vector2(Screen.width, Screen.height);
                
                float x = (screenPosition.x / screenSize.x) * canvasSize.x - canvasSize.x / 2f;
                float y = (screenPosition.y / screenSize.y) * canvasSize.y - canvasSize.y / 2f;
                
                return new Vector2(x, y);
            }
            
            return screenPosition;
        }
        
        #endregion

        #region Editor Support
#if UNITY_EDITOR
        /// <summary>
        /// Forces adaptation for testing.
        /// </summary>
        public void ForceAdaptationForTesting()
        {
            ApplyAdaptation();
        }
        
        /// <summary>
        /// Sets screen size for testing (simulates different devices).
        /// </summary>
        public void SetScreenSizeForTesting(float width, float height)
        {
            _lastScreenSize = new Vector2(width, height);
        }
        
        /// <summary>
        /// Gets the canvas rect for testing.
        /// </summary>
        public RectTransform GetCanvasRectForTesting()
        {
            return _canvasRect;
        }
        
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                ApplyAdaptation();
            }
        }
#endif
        #endregion
    }
}
