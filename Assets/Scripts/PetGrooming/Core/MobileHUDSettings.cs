using UnityEngine;

namespace PetGrooming.Core
{
    /// <summary>
    /// ScriptableObject containing mobile HUD configuration parameters.
    /// Defines settings for virtual joystick, skill wheel, struggle button, and animations.
    /// Requirements: 1.1, 1.2, 2.2, 3.1
    /// </summary>
    [CreateAssetMenu(fileName = "MobileHUDSettings", menuName = "PetGrooming/MobileHUDSettings")]
    public class MobileHUDSettings : ScriptableObject
    {
        #region Joystick Settings
        [Header("Joystick Settings")]
        [Tooltip("Diameter of the joystick background in pixels (150-200 recommended)")]
        [Range(100f, 250f)]
        public float JoystickSize = 180f;
        
        [Tooltip("Diameter of the joystick handle in pixels (60-80 recommended)")]
        [Range(40f, 100f)]
        public float HandleSize = 70f;
        
        [Tooltip("Offset from bottom-left corner of screen")]
        public Vector2 JoystickOffset = new Vector2(150f, 150f);
        
        [Tooltip("Enable dynamic joystick positioning (appears where player first touches)")]
        public bool DynamicJoystick = true;
        
        [Tooltip("Handle movement range as multiplier of background radius")]
        [Range(0.5f, 1.5f)]
        public float HandleRange = 1f;
        
        [Tooltip("Duration for handle to return to center on release (seconds)")]
        [Range(0.05f, 0.3f)]
        public float HandleReturnDuration = 0.1f;
        
        [Tooltip("Joystick handle opacity when idle")]
        [Range(0.3f, 1f)]
        public float HandleIdleOpacity = 0.6f;
        
        [Tooltip("Joystick handle opacity when active")]
        [Range(0.5f, 1f)]
        public float HandleActiveOpacity = 1f;
        #endregion

        #region Skill Wheel Settings
        [Header("Skill Wheel Settings")]
        [Tooltip("Diameter of the capture button in pixels (largest button)")]
        [Range(100f, 180f)]
        public float CaptureButtonSize = 140f;
        
        [Tooltip("Diameter of skill buttons in pixels")]
        [Range(60f, 140f)]
        public float SkillButtonSize = 100f;
        
        [Tooltip("Radius of the arc for skill button arrangement")]
        [Range(100f, 250f)]
        public float ArcRadius = 150f;
        
        [Tooltip("Starting angle of the skill button arc (degrees, 0 = right, 90 = up)")]
        [Range(90f, 180f)]
        public float ArcStartAngle = 135f;
        
        [Tooltip("Total angular span of the skill button arc (degrees)")]
        [Range(45f, 120f)]
        public float ArcSpan = 90f;
        
        [Tooltip("Offset from bottom-right corner of screen")]
        public Vector2 SkillWheelOffset = new Vector2(-100f, 100f);
        
        [Tooltip("Minimum spacing between skill buttons in pixels")]
        [Range(10f, 40f)]
        public float ButtonSpacing = 20f;
        #endregion

        #region Struggle Button Settings
        [Header("Struggle Button Settings")]
        [Tooltip("Diameter of the struggle button in pixels")]
        [Range(120f, 200f)]
        public float StruggleButtonSize = 160f;
        
        [Tooltip("Number of taps required to complete struggle")]
        [Range(5, 20)]
        public int StruggleTapsRequired = 10;
        
        [Tooltip("Time window for struggle taps (seconds)")]
        [Range(1f, 5f)]
        public float StruggleTapWindow = 3f;
        
        [Tooltip("Offset from center-right of screen")]
        public Vector2 StruggleButtonOffset = new Vector2(-150f, 0f);
        #endregion

        #region Animation Settings
        [Header("Animation Settings")]
        [Tooltip("Scale multiplier when button is pressed")]
        [Range(0.85f, 0.99f)]
        public float PressScale = 0.95f;
        
        [Tooltip("Duration of press scale animation (seconds)")]
        [Range(0.05f, 0.2f)]
        public float PressAnimationDuration = 0.1f;
        
        [Tooltip("Duration of ready pulse animation (seconds)")]
        [Range(0.2f, 0.5f)]
        public float ReadyPulseDuration = 0.3f;
        
        [Tooltip("Speed of glow pulse effect")]
        [Range(1f, 4f)]
        public float GlowPulseSpeed = 2f;
        
        [Tooltip("Duration of fail shake animation (seconds)")]
        [Range(0.1f, 0.5f)]
        public float FailShakeDuration = 0.2f;
        
        [Tooltip("Intensity of fail shake animation")]
        [Range(5f, 20f)]
        public float FailShakeIntensity = 10f;
        #endregion

        #region Visual Settings
        [Header("Visual Settings")]
        [Tooltip("Background color for skill buttons")]
        public Color ButtonBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.7f);
        
        [Tooltip("Color for cooldown overlay")]
        public Color CooldownOverlayColor = new Color(0f, 0f, 0f, 0.6f);
        
        [Tooltip("Color for ready glow effect")]
        public Color ReadyGlowColor = new Color(1f, 1f, 0.5f, 0.8f);
        
        [Tooltip("Color for struggle button (orange/red theme)")]
        public Color StruggleButtonColor = new Color(0.9f, 0.3f, 0.2f, 1f);
        #endregion

        #region Input Settings
        [Header("Input Settings")]
        [Tooltip("Minimum interval between button taps for debouncing (seconds)")]
        [Range(0.03f, 0.1f)]
        public float TapDebounceInterval = 0.05f;
        
        [Tooltip("Enable haptic feedback on skill activation")]
        public bool EnableHapticFeedback = true;
        #endregion
    }
}
