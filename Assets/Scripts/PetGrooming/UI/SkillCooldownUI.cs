using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PetGrooming.Systems.Skills;

namespace PetGrooming.UI
{
    /// <summary>
    /// UI component for displaying a single skill's cooldown state.
    /// Displays skill icon, cooldown overlay with radial fill, and remaining time text.
    /// Requirements: 7.1, 7.2, 7.3, 7.4
    /// </summary>
    public class SkillCooldownUI : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("UI Elements")]
        [Tooltip("Image component for displaying the skill icon")]
        [SerializeField] private Image _skillIcon;
        
        [Tooltip("Image component for cooldown overlay (uses radial fill)")]
        [SerializeField] private Image _cooldownOverlay;
        
        [Tooltip("Text component for displaying remaining cooldown seconds")]
        [SerializeField] private TextMeshProUGUI _cooldownText;
        
        [Tooltip("Animator for ready indicator animation")]
        [SerializeField] private Animator _readyAnimator;
        
        [Header("Visual Settings")]
        [Tooltip("Color of the skill icon when ready")]
        [SerializeField] private Color _readyColor = Color.white;
        
        [Tooltip("Color of the skill icon when on cooldown")]
        [SerializeField] private Color _cooldownColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        
        [Tooltip("Color of the cooldown overlay")]
        [SerializeField] private Color _overlayColor = new Color(0f, 0f, 0f, 0.7f);
        
        [Header("Animation Settings")]
        [Tooltip("Animation trigger name for ready state")]
        [SerializeField] private string _readyTrigger = "Ready";
        
        [Tooltip("Animation trigger name for activated state")]
        [SerializeField] private string _activatedTrigger = "Activated";
        
        [Tooltip("Enable pulse animation when skill becomes ready")]
        [SerializeField] private bool _enableReadyPulse = true;
        
        [Tooltip("Duration of the ready pulse animation")]
        [SerializeField] private float _readyPulseDuration = 0.3f;
        
        [Tooltip("Scale multiplier for ready pulse")]
        [SerializeField] private float _readyPulseScale = 1.2f;
        
        [Header("Ready Glow Effect")]
        [Tooltip("Image component for glow effect when ready")]
        [SerializeField] private Image _glowImage;
        
        [Tooltip("Enable glow effect when skill is ready")]
        [SerializeField] private bool _enableReadyGlow = true;
        
        [Tooltip("Glow color when skill is ready")]
        [SerializeField] private Color _glowColor = new Color(1f, 0.9f, 0.5f, 0.8f);
        
        [Tooltip("Glow pulse speed")]
        [SerializeField] private float _glowPulseSpeed = 2f;
        
        [Tooltip("Minimum glow alpha")]
        [SerializeField] private float _glowMinAlpha = 0.3f;
        
        [Tooltip("Maximum glow alpha")]
        [SerializeField] private float _glowMaxAlpha = 0.8f;
        
        #endregion

        #region Private Fields
        
        private SkillBase _skill;
        private RectTransform _rectTransform;
        private Vector3 _originalScale;
        private bool _isAnimatingPulse;
        private float _pulseTimer;
        private float _glowTimer;
        private bool _isGlowing;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// The skill being displayed by this UI component.
        /// </summary>
        public SkillBase Skill => _skill;
        
        /// <summary>
        /// Whether the skill is currently ready.
        /// </summary>
        public bool IsReady => _skill != null && _skill.IsReady;
        
        #endregion

        #region Events
        
        /// <summary>
        /// Fired when the skill becomes ready.
        /// </summary>
        public event Action OnSkillReady;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform != null)
            {
                _originalScale = _rectTransform.localScale;
            }
            
            ValidateReferences();
            InitializeOverlay();
        }
        
        private void Update()
        {
            UpdatePulseAnimation();
            UpdateGlowAnimation();
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromSkill();
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Sets the skill to display.
        /// Requirement 7.1: Display each skill as an icon with cooldown overlay.
        /// </summary>
        /// <param name="skill">The skill to display</param>
        public void SetSkill(SkillBase skill)
        {
            UnsubscribeFromSkill();
            
            _skill = skill;
            
            if (_skill != null)
            {
                SubscribeToSkill();
                UpdateIcon();
                UpdateCooldown(_skill.RemainingCooldown, _skill.Cooldown);
            }
            else
            {
                ClearDisplay();
            }
        }
        
        /// <summary>
        /// Updates the cooldown display.
        /// Requirement 7.2: Show radial fill animation indicating remaining time.
        /// Requirement 7.3: Display remaining seconds as text.
        /// </summary>
        /// <param name="remaining">Remaining cooldown time in seconds</param>
        /// <param name="total">Total cooldown duration in seconds</param>
        public void UpdateCooldown(float remaining, float total)
        {
            bool isOnCooldown = remaining > 0f;
            
            // Update overlay fill
            if (_cooldownOverlay != null)
            {
                _cooldownOverlay.gameObject.SetActive(isOnCooldown);
                if (isOnCooldown && total > 0f)
                {
                    // Radial fill from 1 (full) to 0 (empty) as cooldown progresses
                    _cooldownOverlay.fillAmount = remaining / total;
                }
            }
            
            // Update cooldown text
            if (_cooldownText != null)
            {
                _cooldownText.gameObject.SetActive(isOnCooldown);
                if (isOnCooldown)
                {
                    _cooldownText.text = FormatCooldownTime(remaining);
                }
            }
            
            // Update icon color
            if (_skillIcon != null)
            {
                _skillIcon.color = isOnCooldown ? _cooldownColor : _readyColor;
            }
        }
        
        /// <summary>
        /// Shows the ready indicator animation.
        /// Requirement 7.4: Play ready indicator animation when skill becomes available.
        /// </summary>
        public void ShowReady()
        {
            // Trigger animator if available
            if (_readyAnimator != null && !string.IsNullOrEmpty(_readyTrigger))
            {
                _readyAnimator.SetTrigger(_readyTrigger);
            }
            
            // Start pulse animation
            if (_enableReadyPulse && _rectTransform != null)
            {
                StartPulseAnimation();
            }
            
            // Start glow effect
            if (_enableReadyGlow)
            {
                StartGlow();
            }
            
            OnSkillReady?.Invoke();
        }
        
        /// <summary>
        /// Shows the activated indicator.
        /// </summary>
        public void ShowActivated()
        {
            if (_readyAnimator != null && !string.IsNullOrEmpty(_activatedTrigger))
            {
                _readyAnimator.SetTrigger(_activatedTrigger);
            }
            
            // Stop glow when skill is activated
            StopGlow();
        }
        
        /// <summary>
        /// Clears the display (no skill assigned).
        /// </summary>
        public void ClearDisplay()
        {
            if (_skillIcon != null)
            {
                _skillIcon.sprite = null;
                _skillIcon.color = _cooldownColor;
            }
            
            if (_cooldownOverlay != null)
            {
                _cooldownOverlay.gameObject.SetActive(false);
            }
            
            if (_cooldownText != null)
            {
                _cooldownText.gameObject.SetActive(false);
            }
        }
        
        #endregion

        #region Private Methods
        
        private void ValidateReferences()
        {
            if (_skillIcon == null)
                Debug.LogWarning($"[SkillCooldownUI] Skill icon is not assigned on {gameObject.name}!");
            if (_cooldownOverlay == null)
                Debug.LogWarning($"[SkillCooldownUI] Cooldown overlay is not assigned on {gameObject.name}!");
            if (_cooldownText == null)
                Debug.LogWarning($"[SkillCooldownUI] Cooldown text is not assigned on {gameObject.name}!");
        }
        
        private void InitializeOverlay()
        {
            if (_cooldownOverlay != null)
            {
                // Set up radial fill for cooldown overlay
                _cooldownOverlay.type = Image.Type.Filled;
                _cooldownOverlay.fillMethod = Image.FillMethod.Radial360;
                _cooldownOverlay.fillOrigin = (int)Image.Origin360.Top;
                _cooldownOverlay.fillClockwise = false;
                _cooldownOverlay.color = _overlayColor;
                _cooldownOverlay.gameObject.SetActive(false);
            }
        }
        
        private void SubscribeToSkill()
        {
            if (_skill == null) return;
            
            _skill.OnCooldownChanged += OnCooldownChanged;
            _skill.OnSkillActivated += OnSkillActivated;
            _skill.OnSkillReady += OnSkillBecameReady;
        }
        
        private void UnsubscribeFromSkill()
        {
            if (_skill == null) return;
            
            _skill.OnCooldownChanged -= OnCooldownChanged;
            _skill.OnSkillActivated -= OnSkillActivated;
            _skill.OnSkillReady -= OnSkillBecameReady;
        }
        
        private void UpdateIcon()
        {
            if (_skillIcon != null && _skill != null)
            {
                _skillIcon.sprite = _skill.Icon;
                _skillIcon.color = _skill.IsReady ? _readyColor : _cooldownColor;
            }
        }
        
        private void OnCooldownChanged(float remaining)
        {
            if (_skill != null)
            {
                UpdateCooldown(remaining, _skill.Cooldown);
            }
        }
        
        private void OnSkillActivated()
        {
            ShowActivated();
        }
        
        private void OnSkillBecameReady()
        {
            ShowReady();
        }
        
        private void StartPulseAnimation()
        {
            _isAnimatingPulse = true;
            _pulseTimer = 0f;
        }
        
        private void UpdatePulseAnimation()
        {
            if (!_isAnimatingPulse || _rectTransform == null) return;
            
            _pulseTimer += Time.deltaTime;
            float progress = _pulseTimer / _readyPulseDuration;
            
            if (progress >= 1f)
            {
                _rectTransform.localScale = _originalScale;
                _isAnimatingPulse = false;
                return;
            }
            
            // Pulse out then back in
            float scale;
            if (progress < 0.5f)
            {
                // Scale up
                scale = Mathf.Lerp(1f, _readyPulseScale, progress * 2f);
            }
            else
            {
                // Scale down
                scale = Mathf.Lerp(_readyPulseScale, 1f, (progress - 0.5f) * 2f);
            }
            
            _rectTransform.localScale = _originalScale * scale;
        }
        
        private void UpdateGlowAnimation()
        {
            if (!_enableReadyGlow || _glowImage == null) return;
            
            bool shouldGlow = _skill != null && _skill.IsReady;
            
            if (shouldGlow != _isGlowing)
            {
                _isGlowing = shouldGlow;
                _glowImage.gameObject.SetActive(shouldGlow);
                _glowTimer = 0f;
            }
            
            if (_isGlowing)
            {
                _glowTimer += Time.deltaTime * _glowPulseSpeed;
                float alpha = Mathf.Lerp(_glowMinAlpha, _glowMaxAlpha, (Mathf.Sin(_glowTimer) + 1f) * 0.5f);
                Color color = _glowColor;
                color.a = alpha;
                _glowImage.color = color;
            }
        }
        
        private void StartGlow()
        {
            if (!_enableReadyGlow || _glowImage == null) return;
            
            _isGlowing = true;
            _glowTimer = 0f;
            _glowImage.gameObject.SetActive(true);
        }
        
        private void StopGlow()
        {
            if (_glowImage == null) return;
            
            _isGlowing = false;
            _glowImage.gameObject.SetActive(false);
        }
        
        #endregion

        #region Static Helper Methods
        
        /// <summary>
        /// Formats cooldown time for display.
        /// Shows decimal for times under 1 second, whole numbers otherwise.
        /// </summary>
        /// <param name="seconds">Time in seconds</param>
        /// <returns>Formatted time string</returns>
        public static string FormatCooldownTime(float seconds)
        {
            if (seconds <= 0f)
            {
                return "";
            }
            
            if (seconds < 1f)
            {
                return seconds.ToString("0.0");
            }
            
            return Mathf.CeilToInt(seconds).ToString();
        }
        
        /// <summary>
        /// Calculates the fill amount for a cooldown overlay.
        /// </summary>
        /// <param name="remaining">Remaining cooldown time</param>
        /// <param name="total">Total cooldown duration</param>
        /// <returns>Fill amount (0-1)</returns>
        public static float CalculateFillAmount(float remaining, float total)
        {
            if (total <= 0f) return 0f;
            return Mathf.Clamp01(remaining / total);
        }
        
        #endregion

        #region Editor Support
#if UNITY_EDITOR
        /// <summary>
        /// Sets references for testing purposes.
        /// </summary>
        public void SetReferencesForTesting(Image skillIcon, Image cooldownOverlay, TextMeshProUGUI cooldownText)
        {
            _skillIcon = skillIcon;
            _cooldownOverlay = cooldownOverlay;
            _cooldownText = cooldownText;
            InitializeOverlay();
        }
        
        /// <summary>
        /// Sets the animator for testing purposes.
        /// </summary>
        public void SetAnimatorForTesting(Animator animator)
        {
            _readyAnimator = animator;
        }
#endif
        #endregion
    }
}
