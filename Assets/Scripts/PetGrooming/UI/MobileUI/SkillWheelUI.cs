using System;
using UnityEngine;
using PetGrooming.Core;
using PetGrooming.Systems.Skills;

namespace PetGrooming.UI.MobileUI
{
    /// <summary>
    /// Skill wheel UI component that manages the arc layout of skill buttons.
    /// Handles the capture button and skill buttons arrangement in a fan/arc pattern.
    /// 
    /// Requirements: 2.1, 2.2, 2.3, 3.2, 3.3, 3.4
    /// </summary>
    public class SkillWheelUI : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Button References")]
        [Tooltip("The main capture button (largest, bottom-right)")]
        [SerializeField] private MobileSkillButton _captureButton;
        
        [Tooltip("Array of skill buttons arranged in arc")]
        [SerializeField] private MobileSkillButton[] _skillButtons;
        
        [Header("Layout Settings")]
        [Tooltip("Mobile HUD settings asset")]
        [SerializeField] private MobileHUDSettings _settings;
        
        [Tooltip("Radius of the arc for skill button arrangement")]
        [SerializeField] private float _arcRadius = 150f;
        
        [Tooltip("Starting angle of the arc (degrees, 0 = right, 90 = up)")]
        [SerializeField] private float _arcStartAngle = 135f;
        
        [Tooltip("Total angular span of the arc (degrees)")]
        [SerializeField] private float _arcSpan = 90f;
        
        [Tooltip("Minimum spacing between buttons in pixels")]
        [SerializeField] private float _minButtonSpacing = 20f;
        
        [Header("Skill Icon Data")]
        [Tooltip("Skill icon configuration data")]
        [SerializeField] private SkillIconData _skillIconData;
        
        #endregion

        #region Private Fields
        
        private RectTransform _rectTransform;
        private GroomerSkillManager _boundSkillManager;
        private bool _isInitialized;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// The capture button reference.
        /// </summary>
        public MobileSkillButton CaptureButton => _captureButton;
        
        /// <summary>
        /// Array of skill buttons.
        /// </summary>
        public MobileSkillButton[] SkillButtons => _skillButtons;
        
        /// <summary>
        /// Number of skill buttons in the wheel.
        /// </summary>
        public int SkillButtonCount => _skillButtons != null ? _skillButtons.Length : 0;
        
        /// <summary>
        /// Arc radius for button arrangement.
        /// </summary>
        public float ArcRadius => _arcRadius;
        
        /// <summary>
        /// Arc start angle in degrees.
        /// </summary>
        public float ArcStartAngle => _arcStartAngle;
        
        /// <summary>
        /// Arc span in degrees.
        /// </summary>
        public float ArcSpan => _arcSpan;
        
        /// <summary>
        /// Minimum button spacing.
        /// </summary>
        public float MinButtonSpacing => _minButtonSpacing;
        
        #endregion

        #region Events
        
        /// <summary>
        /// Fired when the capture button is pressed.
        /// </summary>
        public event Action OnCapturePressed;
        
        /// <summary>
        /// Fired when a skill button is pressed.
        /// </summary>
        public event Action<int> OnSkillPressed;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            ApplySettings();
        }
        
        private void Start()
        {
            Initialize();
        }
        
        private void OnDestroy()
        {
            UnbindFromSkillManager();
            UnsubscribeFromButtons();
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Binds the skill wheel to a GroomerSkillManager.
        /// </summary>
        public void BindToGroomerSkills(GroomerSkillManager skillManager)
        {
            UnbindFromSkillManager();
            
            _boundSkillManager = skillManager;
            
            if (_boundSkillManager == null)
            {
                Debug.LogWarning("[SkillWheelUI] Attempted to bind to null GroomerSkillManager");
                return;
            }
            
            // Bind skill buttons to skills
            for (int i = 0; i < _skillButtons.Length && i < _boundSkillManager.SkillCount; i++)
            {
                if (_skillButtons[i] != null)
                {
                    SkillBase skill = _boundSkillManager.GetSkill(i);
                    _skillButtons[i].SetSkill(skill);
                    
                    // Apply icon from SkillIconData if available
                    ApplySkillIcon(i, skill);
                }
            }
            
            // Apply capture button icon
            ApplyCaptureButtonIcon();
            
            Debug.Log($"[SkillWheelUI] Bound to GroomerSkillManager with {_boundSkillManager.SkillCount} skills");
        }
        
        /// <summary>
        /// Sets all buttons interactable state.
        /// </summary>
        public void SetButtonsInteractable(bool interactable)
        {
            if (_captureButton != null)
            {
                _captureButton.gameObject.SetActive(interactable);
            }
            
            foreach (var button in _skillButtons)
            {
                if (button != null)
                {
                    button.gameObject.SetActive(interactable);
                }
            }
        }
        
        /// <summary>
        /// Shows or hides the capture button.
        /// </summary>
        public void ShowCaptureButton(bool show)
        {
            if (_captureButton != null)
            {
                _captureButton.gameObject.SetActive(show);
            }
        }
        
        /// <summary>
        /// Applies layout settings and repositions buttons.
        /// </summary>
        public void ApplyLayout()
        {
            ArrangeSkillButtonsInArc();
        }
        
        /// <summary>
        /// Sets the settings asset and applies it.
        /// </summary>
        public void SetSettings(MobileHUDSettings settings)
        {
            _settings = settings;
            ApplySettings();
            ApplyLayout();
        }
        
        /// <summary>
        /// Sets the skill icon data asset.
        /// </summary>
        public void SetSkillIconData(SkillIconData iconData)
        {
            _skillIconData = iconData;
            
            // Re-apply icons if already bound
            if (_boundSkillManager != null)
            {
                for (int i = 0; i < _skillButtons.Length && i < _boundSkillManager.SkillCount; i++)
                {
                    if (_skillButtons[i] != null)
                    {
                        ApplySkillIcon(i, _boundSkillManager.GetSkill(i));
                    }
                }
                ApplyCaptureButtonIcon();
            }
        }
        
        #endregion

        #region Static Methods (for testing)
        
        /// <summary>
        /// Calculates button positions along an arc.
        /// Used for property-based testing.
        /// 
        /// Property 3: Skill Button Arc Arrangement
        /// Validates: Requirements 2.3, 3.2
        /// </summary>
        /// <param name="buttonCount">Number of buttons to arrange</param>
        /// <param name="arcRadius">Radius of the arc</param>
        /// <param name="arcStartAngle">Starting angle in degrees (0 = right, 90 = up)</param>
        /// <param name="arcSpan">Total angular span in degrees</param>
        /// <returns>Array of positions relative to center</returns>
        public static Vector2[] CalculateArcPositions(int buttonCount, float arcRadius, float arcStartAngle, float arcSpan)
        {
            if (buttonCount <= 0)
            {
                return Array.Empty<Vector2>();
            }
            
            Vector2[] positions = new Vector2[buttonCount];
            
            // Calculate angular spacing
            float angleStep = buttonCount > 1 ? arcSpan / (buttonCount - 1) : 0f;
            
            for (int i = 0; i < buttonCount; i++)
            {
                // Calculate angle for this button
                float angle = arcStartAngle + (i * angleStep);
                float angleRad = angle * Mathf.Deg2Rad;
                
                // Calculate position on arc
                float x = Mathf.Cos(angleRad) * arcRadius;
                float y = Mathf.Sin(angleRad) * arcRadius;
                
                positions[i] = new Vector2(x, y);
            }
            
            return positions;
        }
        
        /// <summary>
        /// Calculates the minimum distance between any two adjacent buttons.
        /// Used for property-based testing.
        /// 
        /// Property 7: Button Spacing Minimum
        /// Validates: Requirements 3.3
        /// </summary>
        /// <param name="positions">Array of button center positions</param>
        /// <param name="buttonSizes">Array of button sizes (diameters)</param>
        /// <returns>Minimum edge-to-edge distance between adjacent buttons</returns>
        public static float CalculateMinimumButtonSpacing(Vector2[] positions, float[] buttonSizes)
        {
            if (positions == null || positions.Length < 2)
            {
                return float.MaxValue;
            }
            
            float minSpacing = float.MaxValue;
            
            for (int i = 0; i < positions.Length - 1; i++)
            {
                // Calculate center-to-center distance
                float centerDistance = Vector2.Distance(positions[i], positions[i + 1]);
                
                // Calculate edge-to-edge distance (subtract radii)
                float radius1 = buttonSizes != null && i < buttonSizes.Length ? buttonSizes[i] / 2f : 0f;
                float radius2 = buttonSizes != null && i + 1 < buttonSizes.Length ? buttonSizes[i + 1] / 2f : 0f;
                
                float edgeDistance = centerDistance - radius1 - radius2;
                
                if (edgeDistance < minSpacing)
                {
                    minSpacing = edgeDistance;
                }
            }
            
            return minSpacing;
        }
        
        /// <summary>
        /// Validates that all buttons are within the arc span.
        /// Used for property-based testing.
        /// </summary>
        /// <param name="positions">Array of button positions</param>
        /// <param name="arcStartAngle">Starting angle in degrees</param>
        /// <param name="arcSpan">Total angular span in degrees</param>
        /// <returns>True if all buttons are within the arc</returns>
        public static bool ValidateButtonsWithinArc(Vector2[] positions, float arcStartAngle, float arcSpan)
        {
            if (positions == null || positions.Length == 0)
            {
                return true;
            }
            
            float endAngle = arcStartAngle + arcSpan;
            
            foreach (var pos in positions)
            {
                // Calculate angle of this position
                float angle = Mathf.Atan2(pos.y, pos.x) * Mathf.Rad2Deg;
                
                // Normalize angle to positive range
                while (angle < 0) angle += 360f;
                while (arcStartAngle < 0) arcStartAngle += 360f;
                while (endAngle < 0) endAngle += 360f;
                
                // Check if within arc (with small tolerance for floating point)
                const float tolerance = 0.01f;
                if (angle < arcStartAngle - tolerance || angle > endAngle + tolerance)
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Calculates the angular spacing between buttons.
        /// </summary>
        /// <param name="buttonCount">Number of buttons</param>
        /// <param name="arcSpan">Total arc span in degrees</param>
        /// <returns>Angular spacing in degrees</returns>
        public static float CalculateAngularSpacing(int buttonCount, float arcSpan)
        {
            if (buttonCount <= 1)
            {
                return 0f;
            }
            
            return arcSpan / (buttonCount - 1);
        }
        
        #endregion

        #region Private Methods
        
        private void Initialize()
        {
            if (_isInitialized) return;
            
            SubscribeToButtons();
            ArrangeSkillButtonsInArc();
            
            _isInitialized = true;
        }
        
        private void ApplySettings()
        {
            if (_settings == null) return;
            
            _arcRadius = _settings.ArcRadius;
            _arcStartAngle = _settings.ArcStartAngle;
            _arcSpan = _settings.ArcSpan;
            _minButtonSpacing = _settings.ButtonSpacing;
            
            // Apply settings to capture button
            if (_captureButton != null)
            {
                _captureButton.SetSettings(_settings);
                _captureButton.SetButtonSize(_settings.CaptureButtonSize);
            }
            
            // Apply settings to skill buttons
            foreach (var button in _skillButtons)
            {
                if (button != null)
                {
                    button.SetSettings(_settings);
                    button.SetButtonSize(_settings.SkillButtonSize);
                }
            }
        }
        
        private void ArrangeSkillButtonsInArc()
        {
            if (_skillButtons == null || _skillButtons.Length == 0) return;
            
            // Calculate positions
            Vector2[] positions = CalculateArcPositions(
                _skillButtons.Length,
                _arcRadius,
                _arcStartAngle,
                _arcSpan
            );
            
            // Apply positions to buttons
            for (int i = 0; i < _skillButtons.Length && i < positions.Length; i++)
            {
                if (_skillButtons[i] != null)
                {
                    RectTransform buttonRect = _skillButtons[i].GetComponent<RectTransform>();
                    if (buttonRect != null)
                    {
                        buttonRect.anchoredPosition = positions[i];
                    }
                }
            }
        }
        
        private void SubscribeToButtons()
        {
            // Subscribe to capture button
            if (_captureButton != null)
            {
                _captureButton.OnButtonPressed += HandleCapturePressed;
            }
            
            // Subscribe to skill buttons
            for (int i = 0; i < _skillButtons.Length; i++)
            {
                if (_skillButtons[i] != null)
                {
                    int index = i; // Capture for closure
                    _skillButtons[i].OnButtonPressed += () => HandleSkillPressed(index);
                }
            }
        }
        
        private void UnsubscribeFromButtons()
        {
            if (_captureButton != null)
            {
                _captureButton.OnButtonPressed -= HandleCapturePressed;
            }
            
            // Note: Can't easily unsubscribe lambda expressions
            // This is acceptable as buttons are destroyed with the wheel
        }
        
        private void UnbindFromSkillManager()
        {
            if (_boundSkillManager == null) return;
            
            // Clear skill bindings
            foreach (var button in _skillButtons)
            {
                if (button != null)
                {
                    button.SetSkill(null);
                }
            }
            
            _boundSkillManager = null;
        }
        
        private void HandleCapturePressed()
        {
            OnCapturePressed?.Invoke();
            
            // If bound to skill manager, this would trigger capture action
            // The actual capture logic is handled by GroomerController
            Debug.Log("[SkillWheelUI] Capture button pressed");
        }
        
        private void HandleSkillPressed(int skillIndex)
        {
            OnSkillPressed?.Invoke(skillIndex);
            
            // If bound to skill manager, try to activate the skill
            if (_boundSkillManager != null)
            {
                _boundSkillManager.TryActivateSkill(skillIndex);
            }
            
            Debug.Log($"[SkillWheelUI] Skill button {skillIndex} pressed");
        }
        
        private void ApplySkillIcon(int skillIndex, SkillBase skill)
        {
            if (_skillIconData == null || skill == null) return;
            
            SkillIconData.SkillIconEntry iconEntry = null;
            
            // Match skill type to icon entry
            if (skill is CaptureNetSkill)
            {
                iconEntry = _skillIconData.CaptureNet;
            }
            else if (skill is LeashSkill)
            {
                iconEntry = _skillIconData.Leash;
            }
            else if (skill is CalmingSpraySkill)
            {
                iconEntry = _skillIconData.CalmingSpray;
            }
            
            if (iconEntry != null && _skillButtons[skillIndex] != null)
            {
                _skillButtons[skillIndex].SetIconFromData(iconEntry);
            }
        }
        
        private void ApplyCaptureButtonIcon()
        {
            if (_skillIconData == null || _captureButton == null) return;
            
            if (_skillIconData.CaptureButton != null)
            {
                _captureButton.SetIconFromData(_skillIconData.CaptureButton);
            }
        }
        
        #endregion

        #region Editor Support
#if UNITY_EDITOR
        /// <summary>
        /// Sets references for testing purposes.
        /// </summary>
        public void SetReferencesForTesting(MobileSkillButton captureButton, MobileSkillButton[] skillButtons)
        {
            _captureButton = captureButton;
            _skillButtons = skillButtons;
        }
        
        /// <summary>
        /// Sets layout parameters for testing.
        /// </summary>
        public void SetLayoutParametersForTesting(float arcRadius, float arcStartAngle, float arcSpan, float minSpacing)
        {
            _arcRadius = arcRadius;
            _arcStartAngle = arcStartAngle;
            _arcSpan = arcSpan;
            _minButtonSpacing = minSpacing;
        }
        
        /// <summary>
        /// Gets button positions for testing.
        /// </summary>
        public Vector2[] GetButtonPositionsForTesting()
        {
            if (_skillButtons == null) return Array.Empty<Vector2>();
            
            Vector2[] positions = new Vector2[_skillButtons.Length];
            for (int i = 0; i < _skillButtons.Length; i++)
            {
                if (_skillButtons[i] != null)
                {
                    RectTransform rect = _skillButtons[i].GetComponent<RectTransform>();
                    positions[i] = rect != null ? rect.anchoredPosition : Vector2.zero;
                }
            }
            return positions;
        }
        
        /// <summary>
        /// Gets button sizes for testing.
        /// </summary>
        public float[] GetButtonSizesForTesting()
        {
            if (_skillButtons == null) return Array.Empty<float>();
            
            float[] sizes = new float[_skillButtons.Length];
            for (int i = 0; i < _skillButtons.Length; i++)
            {
                if (_skillButtons[i] != null)
                {
                    sizes[i] = _skillButtons[i].ButtonSize;
                }
            }
            return sizes;
        }
        
        private void OnValidate()
        {
            if (Application.isPlaying && _isInitialized)
            {
                ApplySettings();
                ApplyLayout();
            }
        }
#endif
        #endregion
    }
}
