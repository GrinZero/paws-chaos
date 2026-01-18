using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PetGrooming.Systems.Skills;

namespace PetGrooming.UI
{
    /// <summary>
    /// 用于显示单个技能冷却状态的 UI 组件。
    /// 显示技能图标、带径向填充的冷却覆盖层和剩余时间文本。
    /// 需求：7.1, 7.2, 7.3, 7.4
    /// </summary>
    public class SkillCooldownUI : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("UI Elements")]
        [Tooltip("用于显示技能图标的 Image 组件")]
        [SerializeField] private Image _skillIcon;
        
        [Tooltip("用于冷却覆盖层的 Image 组件（使用径向填充）")]
        [SerializeField] private Image _cooldownOverlay;
        
        [Tooltip("用于显示剩余冷却秒数的 Text 组件")]
        [SerializeField] private TextMeshProUGUI _cooldownText;
        
        [Tooltip("就绪指示器动画的 Animator")]
        [SerializeField] private Animator _readyAnimator;
        
        [Header("视觉设置")]
        [Tooltip("技能就绪时的图标颜色")]
        [SerializeField] private Color _readyColor = Color.white;
        
        [Tooltip("技能冷却时的图标颜色")]
        [SerializeField] private Color _cooldownColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        
        [Tooltip("冷却覆盖层的颜色")]
        [SerializeField] private Color _overlayColor = new Color(0f, 0f, 0f, 0.7f);
        
        [Header("动画设置")]
        [Tooltip("就绪状态的动画触发器名称")]
        [SerializeField] private string _readyTrigger = "Ready";
        
        [Tooltip("激活状态的动画触发器名称")]
        [SerializeField] private string _activatedTrigger = "Activated";
        
        [Tooltip("技能就绪时启用脉冲动画")]
        [SerializeField] private bool _enableReadyPulse = true;
        
        [Tooltip("就绪脉冲动画的持续时间")]
        [SerializeField] private float _readyPulseDuration = 0.3f;
        
        [Tooltip("就绪脉冲的缩放倍数")]
        [SerializeField] private float _readyPulseScale = 1.2f;
        
        [Header("就绪发光效果")]
        [Tooltip("就绪时发光效果的 Image 组件")]
        [SerializeField] private Image _glowImage;
        
        [Tooltip("技能就绪时启用发光效果")]
        [SerializeField] private bool _enableReadyGlow = true;
        
        [Tooltip("技能就绪时的发光颜色")]
        [SerializeField] private Color _glowColor = new Color(1f, 0.9f, 0.5f, 0.8f);
        
        [Tooltip("发光脉冲速度")]
        [SerializeField] private float _glowPulseSpeed = 2f;
        
        [Tooltip("最小发光透明度")]
        [SerializeField] private float _glowMinAlpha = 0.3f;
        
        [Tooltip("最大发光透明度")]
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
        /// 此 UI 组件显示的技能。
        /// </summary>
        public SkillBase Skill => _skill;
        
        /// <summary>
        /// 技能当前是否就绪。
        /// </summary>
        public bool IsReady => _skill != null && _skill.IsReady;
        
        #endregion

        #region Events
        
        /// <summary>
        /// 当技能就绪时触发。
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
        /// 设置要显示的技能。
        /// 需求 7.1：将每个技能显示为带有冷却覆盖层的图标。
        /// </summary>
        /// <param name="skill">要显示的技能</param>
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
        /// 更新冷却显示。
        /// 需求 7.2：显示指示剩余时间的径向填充动画。
        /// 需求 7.3：将剩余秒数显示为文本。
        /// </summary>
        /// <param name="remaining">剩余冷却时间（秒）</param>
        /// <param name="total">总冷却持续时间（秒）</param>
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
        /// 显示就绪指示器动画。
        /// 需求 7.4：当技能可用时播放就绪指示器动画。
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
        /// 显示激活指示器。
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
        /// 清除显示（未分配技能）。
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
        /// 格式化冷却时间以显示。
        /// 1秒以下显示小数，否则显示整数。
        /// </summary>
        /// <param name="seconds">时间（秒）</param>
        /// <returns>格式化的时间字符串</returns>
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
        /// 计算冷却覆盖层的填充量。
        /// </summary>
        /// <param name="remaining">剩余冷却时间</param>
        /// <param name="total">总冷却持续时间</param>
        /// <returns>填充量（0-1）</returns>
        public static float CalculateFillAmount(float remaining, float total)
        {
            if (total <= 0f) return 0f;
            return Mathf.Clamp01(remaining / total);
        }
        
        #endregion

        #region Editor Support
#if UNITY_EDITOR
        /// <summary>
        /// 为测试目的设置引用。
        /// </summary>
        public void SetReferencesForTesting(Image skillIcon, Image cooldownOverlay, TextMeshProUGUI cooldownText)
        {
            _skillIcon = skillIcon;
            _cooldownOverlay = cooldownOverlay;
            _cooldownText = cooldownText;
            InitializeOverlay();
        }
        
        /// <summary>
        /// 为测试目的设置动画器。
        /// </summary>
        public void SetAnimatorForTesting(Animator animator)
        {
            _readyAnimator = animator;
        }
#endif
        #endregion
    }
}
