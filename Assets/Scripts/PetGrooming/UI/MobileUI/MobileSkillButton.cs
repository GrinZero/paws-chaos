using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using PetGrooming.Core;
using PetGrooming.Systems.Skills;

namespace PetGrooming.UI.MobileUI
{
    /// <summary>
    /// 移动技能按钮组件，用于触摸交互。
    /// 处理触摸输入、冷却显示和视觉反馈动画。
    /// 
    /// 需求：2.5, 2.6, 2.7, 2.8, 4.1, 4.2, 4.3, 4.4
    /// </summary>
    /// <remarks>
    /// [已废弃] 此组件已被 Unity 官方 OnScreenButton + SkillButtonVisual 组合替代。
    /// 输入处理：使用 UnityEngine.InputSystem.OnScreen.OnScreenButton
    /// 视觉效果：使用 PetGrooming.UI.MobileUI.SkillButtonVisual
    /// 迁移指南：参见 .kiro/specs/mobile-input-migration/design.md
    /// </remarks>
    [Obsolete("MobileSkillButton 已废弃，请使用 OnScreenButton + SkillButtonVisual 组合。参见 mobile-input-migration 规范。")]
    public class MobileSkillButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        #region Serialized Fields
        
        [Header("UI引用")]
        [Tooltip("按钮的背景图像")]
        [SerializeField] private Image _background;
        
        [Tooltip("技能图标图像")]
        [SerializeField] private Image _iconImage;
        
        [Tooltip("冷却遮罩图像（径向填充）")]
        [SerializeField] private Image _cooldownOverlay;
        
        [Tooltip("冷却时间文本")]
        [SerializeField] private TextMeshProUGUI _cooldownText;
        
        [Tooltip("就绪动画的发光效果图像")]
        [SerializeField] private Image _glowEffect;
        
        [Header("设置")]
        [Tooltip("移动HUD设置资源")]
        [SerializeField] private MobileHUDSettings _settings;
        
        [Tooltip("按钮大小（像素）")]
        [SerializeField] private float _buttonSize = 100f;
        
        [Tooltip("技能就绪时的颜色")]
        [SerializeField] private Color _readyColor = Color.white;
        
        [Tooltip("技能冷却中时的颜色（去饱和）")]
        [SerializeField] private Color _cooldownColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        
        #endregion

        #region Private Fields
        
        private RectTransform _rectTransform;
        private SkillBase _boundSkill;
        private Vector3 _originalScale;
        private Coroutine _pressAnimationCoroutine;
        private Coroutine _readyAnimationCoroutine;
        private Coroutine _failAnimationCoroutine;
        private Coroutine _glowPulseCoroutine;
        private bool _isPressed;
        private float _lastTapTime;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// 技能当前是否在冷却中。
        /// </summary>
        public bool IsOnCooldown => _boundSkill != null && !_boundSkill.IsReady;
        
        /// <summary>
        /// 剩余冷却时间（秒）。
        /// </summary>
        public float RemainingCooldown => _boundSkill != null ? _boundSkill.RemainingCooldown : 0f;
        
        /// <summary>
        /// 绑定的技能引用。
        /// </summary>
        public SkillBase BoundSkill => _boundSkill;
        
        /// <summary>
        /// 当前按钮大小。
        /// </summary>
        public float ButtonSize => _buttonSize;
        
        #endregion

        #region Events
        
        /// <summary>
        /// 当按钮被按下时触发。
        /// </summary>
        public event Action OnButtonPressed;
        
        /// <summary>
        /// 当按钮被释放时触发。
        /// </summary>
        public event Action OnButtonReleased;
        
        /// <summary>
        /// 当技能激活失败时触发（冷却中）。
        /// </summary>
        public event Action OnActivationFailed;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _originalScale = transform.localScale;
            
            ValidateReferences();
            ApplySettings();
        }
        
        private void Start()
        {
            // Initialize cooldown display
            UpdateCooldownDisplay(0f, 1f);
            HideGlow();
        }
        
        private void OnDisable()
        {
            StopAllAnimations();
            ResetVisualState();
        }
        
        private void OnDestroy()
        {
            UnbindSkill();
        }
        
        #endregion

        #region IPointerDownHandler
        
        /// <summary>
        /// 当指针在按钮上按下时调用。
        /// 需求 4.1: 按钮按下时缩小作为反馈。
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (_isPressed) return;
            
            // Debounce check
            if (_settings != null && Time.time - _lastTapTime < _settings.TapDebounceInterval)
            {
                return;
            }
            _lastTapTime = Time.time;
            
            _isPressed = true;
            
            // Play press animation (Requirement 4.1)
            PlayPressAnimation();
            
            // Fire event
            OnButtonPressed?.Invoke();
            
            // Try to activate skill
            TryActivateSkill();
        }
        
        #endregion

        #region IPointerUpHandler
        
        /// <summary>
        /// 当指针被释放时调用。
        /// 需求 4.2: 按钮返回正常大小并带有弹跳效果。
        /// </summary>
        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isPressed) return;
            
            _isPressed = false;
            
            // Play release animation (Requirement 4.2)
            PlayReleaseAnimation();
            
            // Fire event
            OnButtonReleased?.Invoke();
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// 将此按钮绑定到技能。
        /// </summary>
        public void SetSkill(SkillBase skill)
        {
            UnbindSkill();
            
            _boundSkill = skill;
            
            if (_boundSkill != null)
            {
                _boundSkill.OnCooldownChanged += OnSkillCooldownChanged;
                _boundSkill.OnSkillReady += OnSkillBecameReady;
                
                // Initialize display
                if (_boundSkill.Icon != null)
                {
                    SetIcon(_boundSkill.Icon);
                }
                
                UpdateCooldownDisplay(_boundSkill.RemainingCooldown, _boundSkill.Cooldown);
            }
        }
        
        /// <summary>
        /// 设置技能图标精灵。
        /// </summary>
        public void SetIcon(Sprite icon)
        {
            if (_iconImage != null)
            {
                _iconImage.sprite = icon;
                _iconImage.enabled = icon != null;
            }
        }
        
        /// <summary>
        /// 从SkillIconData条目设置图标。
        /// </summary>
        public void SetIconFromData(SkillIconData.SkillIconEntry iconEntry)
        {
            if (iconEntry == null) return;
            
            SetIcon(iconEntry.Icon);
            
            if (_glowEffect != null && iconEntry.GlowSprite != null)
            {
                _glowEffect.sprite = iconEntry.GlowSprite;
            }
            
            // Apply theme color to glow
            if (_glowEffect != null)
            {
                Color glowColor = iconEntry.ThemeColor;
                glowColor.a = 0.8f;
                _glowEffect.color = glowColor;
            }
        }
        
        /// <summary>
        /// 更新冷却显示。
        /// 需求 2.6: 径向冷却遮罩。
        /// 需求 2.7: 冷却时间文本。
        /// </summary>
        public void UpdateCooldown(float remaining, float total)
        {
            UpdateCooldownDisplay(remaining, total);
        }
        
        /// <summary>
        /// 当技能可用时播放就绪动画。
        /// 需求 2.8: 就绪时播放发光/脉冲动画。
        /// </summary>
        public void PlayReadyAnimation()
        {
            StopReadyAnimation();
            
            if (gameObject.activeInHierarchy)
            {
                _readyAnimationCoroutine = StartCoroutine(ReadyPulseAnimation());
            }
        }
        
        /// <summary>
        /// 播放按下缩放动画。
        /// 需求 4.1: 缩小到95%。
        /// </summary>
        public void PlayPressAnimation()
        {
            StopPressAnimation();
            
            if (gameObject.activeInHierarchy)
            {
                _pressAnimationCoroutine = StartCoroutine(PressScaleAnimation());
            }
        }
        
        /// <summary>
        /// 播放释放动画，带有弹跳效果。
        /// 需求 4.2: 返回正常大小并带有弹跳效果。
        /// </summary>
        public void PlayReleaseAnimation()
        {
            StopPressAnimation();
            
            if (gameObject.activeInHierarchy)
            {
                _pressAnimationCoroutine = StartCoroutine(ReleaseScaleAnimation());
            }
        }
        
        /// <summary>
        /// 当激活失败时播放失败动画。
        /// 需求 4.4: 抖动并显示"冷却中"文本。
        /// </summary>
        public void PlayFailAnimation()
        {
            StopFailAnimation();
            
            if (gameObject.activeInHierarchy)
            {
                _failAnimationCoroutine = StartCoroutine(FailShakeAnimation());
            }
        }
        
        /// <summary>
        /// 应用来自MobileHUDSettings资源的设置。
        /// </summary>
        public void ApplySettings()
        {
            if (_settings == null) return;
            
            _buttonSize = _settings.SkillButtonSize;
            
            if (_rectTransform != null)
            {
                _rectTransform.sizeDelta = new Vector2(_buttonSize, _buttonSize);
            }
            
            if (_background != null)
            {
                _background.color = _settings.ButtonBackgroundColor;
            }
            
            if (_cooldownOverlay != null)
            {
                _cooldownOverlay.color = _settings.CooldownOverlayColor;
            }
            
            if (_glowEffect != null)
            {
                _glowEffect.color = _settings.ReadyGlowColor;
            }
        }
        
        /// <summary>
        /// 设置设置资源并应用它。
        /// </summary>
        public void SetSettings(MobileHUDSettings settings)
        {
            _settings = settings;
            ApplySettings();
        }
        
        /// <summary>
        /// 设置按钮大小。
        /// </summary>
        public void SetButtonSize(float size)
        {
            _buttonSize = size;
            
            if (_rectTransform != null)
            {
                _rectTransform.sizeDelta = new Vector2(_buttonSize, _buttonSize);
            }
        }
        
        #endregion

        #region Static Methods (for testing)
        
        /// <summary>
        /// 计算冷却显示状态。
        /// 用于基于属性的测试。
        /// 需求 2.6, 2.7: 冷却遮罩和文本可见性。
        /// </summary>
        /// <param name="remainingCooldown">剩余冷却时间</param>
        /// <param name="totalCooldown">总冷却持续时间</param>
        /// <returns>(overlayVisible, textVisible, displayText, fillAmount)的元组</returns>
        public static (bool overlayVisible, bool textVisible, string displayText, float fillAmount) 
            CalculateCooldownDisplayState(float remainingCooldown, float totalCooldown)
        {
            bool isOnCooldown = remainingCooldown > 0f;
            bool overlayVisible = isOnCooldown;
            bool textVisible = isOnCooldown;
            
            // Calculate fill amount (1 = full cooldown, 0 = ready)
            float fillAmount = totalCooldown > 0f 
                ? Mathf.Clamp01(remainingCooldown / totalCooldown) 
                : 0f;
            
            // Calculate display text (rounded up to nearest second)
            string displayText = isOnCooldown 
                ? Mathf.CeilToInt(remainingCooldown).ToString() 
                : "";
            
            return (overlayVisible, textVisible, displayText, fillAmount);
        }
        
        /// <summary>
        /// 计算按下缩放值。
        /// 用于基于属性的测试。
        /// 需求 4.1: 按下时缩放到95%。
        /// </summary>
        /// <param name="originalScale">按钮的原始缩放</param>
        /// <param name="pressScaleMultiplier">缩放乘数（默认0.95）</param>
        /// <returns>按下时的缩放值</returns>
        public static Vector3 CalculatePressScale(Vector3 originalScale, float pressScaleMultiplier = 0.95f)
        {
            return originalScale * pressScaleMultiplier;
        }
        
        /// <summary>
        /// 判断是否应显示冷却遮罩。
        /// 用于基于属性的测试。
        /// </summary>
        public static bool ShouldShowCooldownOverlay(float remainingCooldown)
        {
            return remainingCooldown > 0f;
        }
        
        /// <summary>
        /// 计算要显示的冷却文本。
        /// 用于基于属性的测试。
        /// </summary>
        public static string CalculateCooldownText(float remainingCooldown)
        {
            if (remainingCooldown <= 0f) return "";
            return Mathf.CeilToInt(remainingCooldown).ToString();
        }
        
        #endregion

        #region Private Methods
        
        private void ValidateReferences()
        {
            if (_background == null)
            {
                Debug.LogWarning("[MobileSkillButton] Background Image is not assigned!");
            }
            
            if (_iconImage == null)
            {
                Debug.LogWarning("[MobileSkillButton] Icon Image is not assigned!");
            }
            
            if (_cooldownOverlay == null)
            {
                Debug.LogWarning("[MobileSkillButton] Cooldown Overlay Image is not assigned!");
            }
            
            if (_cooldownText == null)
            {
                Debug.LogWarning("[MobileSkillButton] Cooldown Text is not assigned!");
            }
        }
        
        private void UnbindSkill()
        {
            if (_boundSkill != null)
            {
                _boundSkill.OnCooldownChanged -= OnSkillCooldownChanged;
                _boundSkill.OnSkillReady -= OnSkillBecameReady;
                _boundSkill = null;
            }
        }
        
        private void TryActivateSkill()
        {
            if (_boundSkill == null) return;
            
            if (_boundSkill.TryActivate())
            {
                // Requirement 4.3: Flash on successful activation
                PlayActivationFlash();
                
                // Trigger haptic feedback if enabled
                TriggerHapticFeedback();
            }
            else
            {
                // Requirement 4.4: Shake on failed activation
                PlayFailAnimation();
                OnActivationFailed?.Invoke();
            }
        }
        
        private void OnSkillCooldownChanged(float remainingCooldown)
        {
            if (_boundSkill != null)
            {
                UpdateCooldownDisplay(remainingCooldown, _boundSkill.Cooldown);
            }
        }
        
        private void OnSkillBecameReady()
        {
            // Requirement 2.8: Play ready animation
            PlayReadyAnimation();
            
            // Update icon color
            SetIconColor(_readyColor);
        }
        
        private void UpdateCooldownDisplay(float remaining, float total)
        {
            var state = CalculateCooldownDisplayState(remaining, total);
            
            // Update overlay visibility and fill
            if (_cooldownOverlay != null)
            {
                _cooldownOverlay.enabled = state.overlayVisible;
                _cooldownOverlay.fillAmount = state.fillAmount;
            }
            
            // Update text visibility and content
            if (_cooldownText != null)
            {
                _cooldownText.enabled = state.textVisible;
                _cooldownText.text = state.displayText;
            }
            
            // Update icon color based on cooldown state
            SetIconColor(state.overlayVisible ? _cooldownColor : _readyColor);
        }
        
        private void SetIconColor(Color color)
        {
            if (_iconImage != null)
            {
                _iconImage.color = color;
            }
        }
        
        private void ShowGlow()
        {
            if (_glowEffect != null)
            {
                _glowEffect.enabled = true;
            }
        }
        
        private void HideGlow()
        {
            if (_glowEffect != null)
            {
                _glowEffect.enabled = false;
            }
        }
        
        private void PlayActivationFlash()
        {
            // Brief flash effect on successful activation
            if (_background != null)
            {
                StartCoroutine(FlashAnimation());
            }
        }
        
        private void TriggerHapticFeedback()
        {
            if (_settings != null && _settings.EnableHapticFeedback)
            {
                // Unity's Handheld.Vibrate() for basic haptic feedback
                #if UNITY_IOS || UNITY_ANDROID
                Handheld.Vibrate();
                #endif
            }
        }
        
        private void StopAllAnimations()
        {
            StopPressAnimation();
            StopReadyAnimation();
            StopFailAnimation();
            StopGlowPulse();
        }
        
        private void StopPressAnimation()
        {
            if (_pressAnimationCoroutine != null)
            {
                StopCoroutine(_pressAnimationCoroutine);
                _pressAnimationCoroutine = null;
            }
        }
        
        private void StopReadyAnimation()
        {
            if (_readyAnimationCoroutine != null)
            {
                StopCoroutine(_readyAnimationCoroutine);
                _readyAnimationCoroutine = null;
            }
        }
        
        private void StopFailAnimation()
        {
            if (_failAnimationCoroutine != null)
            {
                StopCoroutine(_failAnimationCoroutine);
                _failAnimationCoroutine = null;
            }
        }
        
        private void StopGlowPulse()
        {
            if (_glowPulseCoroutine != null)
            {
                StopCoroutine(_glowPulseCoroutine);
                _glowPulseCoroutine = null;
            }
        }
        
        private void ResetVisualState()
        {
            transform.localScale = _originalScale;
            HideGlow();
        }
        
        #endregion

        #region Animation Coroutines
        
        private IEnumerator PressScaleAnimation()
        {
            float duration = _settings != null ? _settings.PressAnimationDuration : 0.1f;
            float pressScale = _settings != null ? _settings.PressScale : 0.95f;
            
            Vector3 targetScale = _originalScale * pressScale;
            Vector3 startScale = transform.localScale;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }
            
            transform.localScale = targetScale;
            _pressAnimationCoroutine = null;
        }
        
        private IEnumerator ReleaseScaleAnimation()
        {
            float duration = _settings != null ? _settings.PressAnimationDuration : 0.1f;
            
            Vector3 startScale = transform.localScale;
            float elapsed = 0f;
            
            // Overshoot for bounce effect
            Vector3 overshootScale = _originalScale * 1.05f;
            float halfDuration = duration * 0.6f;
            
            // First half: scale up past original
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                t = t * t * (3f - 2f * t); // Smoothstep
                transform.localScale = Vector3.Lerp(startScale, overshootScale, t);
                yield return null;
            }
            
            // Second half: settle to original
            elapsed = 0f;
            float secondHalfDuration = duration * 0.4f;
            startScale = transform.localScale;
            
            while (elapsed < secondHalfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / secondHalfDuration;
                t = t * t * (3f - 2f * t);
                transform.localScale = Vector3.Lerp(startScale, _originalScale, t);
                yield return null;
            }
            
            transform.localScale = _originalScale;
            _pressAnimationCoroutine = null;
        }
        
        private IEnumerator ReadyPulseAnimation()
        {
            float duration = _settings != null ? _settings.ReadyPulseDuration : 0.3f;
            
            ShowGlow();
            
            // Pulse scale
            Vector3 pulseScale = _originalScale * 1.1f;
            float elapsed = 0f;
            
            // Scale up
            while (elapsed < duration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration * 0.5f);
                transform.localScale = Vector3.Lerp(_originalScale, pulseScale, t);
                yield return null;
            }
            
            // Scale down
            elapsed = 0f;
            while (elapsed < duration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration * 0.5f);
                transform.localScale = Vector3.Lerp(pulseScale, _originalScale, t);
                yield return null;
            }
            
            transform.localScale = _originalScale;
            
            // Start continuous glow pulse
            _glowPulseCoroutine = StartCoroutine(GlowPulseAnimation());
            
            _readyAnimationCoroutine = null;
        }
        
        private IEnumerator GlowPulseAnimation()
        {
            if (_glowEffect == null) yield break;
            
            float speed = _settings != null ? _settings.GlowPulseSpeed : 2f;
            Color baseColor = _glowEffect.color;
            
            while (true)
            {
                float alpha = (Mathf.Sin(Time.time * speed * Mathf.PI) + 1f) * 0.5f;
                alpha = Mathf.Lerp(0.3f, 0.8f, alpha);
                
                Color color = baseColor;
                color.a = alpha;
                _glowEffect.color = color;
                
                yield return null;
            }
        }
        
        private IEnumerator FailShakeAnimation()
        {
            float duration = _settings != null ? _settings.FailShakeDuration : 0.2f;
            float intensity = _settings != null ? _settings.FailShakeIntensity : 10f;
            
            Vector3 originalPosition = transform.localPosition;
            float elapsed = 0f;
            
            // Show "冷却中" text briefly
            if (_cooldownText != null)
            {
                string originalText = _cooldownText.text;
                _cooldownText.text = "冷却中";
                _cooldownText.enabled = true;
                
                // Shake
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float progress = elapsed / duration;
                    float damping = 1f - progress;
                    
                    float offsetX = Mathf.Sin(elapsed * 50f) * intensity * damping;
                    transform.localPosition = originalPosition + new Vector3(offsetX, 0f, 0f);
                    
                    yield return null;
                }
                
                transform.localPosition = originalPosition;
                
                // Restore original text after a short delay
                yield return new WaitForSeconds(0.3f);
                
                if (_boundSkill != null && _boundSkill.RemainingCooldown > 0f)
                {
                    _cooldownText.text = Mathf.CeilToInt(_boundSkill.RemainingCooldown).ToString();
                }
                else
                {
                    _cooldownText.text = originalText;
                    _cooldownText.enabled = false;
                }
            }
            else
            {
                // Just shake without text
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float progress = elapsed / duration;
                    float damping = 1f - progress;
                    
                    float offsetX = Mathf.Sin(elapsed * 50f) * intensity * damping;
                    transform.localPosition = originalPosition + new Vector3(offsetX, 0f, 0f);
                    
                    yield return null;
                }
                
                transform.localPosition = originalPosition;
            }
            
            _failAnimationCoroutine = null;
        }
        
        private IEnumerator FlashAnimation()
        {
            if (_background == null) yield break;
            
            Color originalColor = _background.color;
            Color flashColor = Color.white;
            
            // Flash to white
            _background.color = flashColor;
            yield return new WaitForSeconds(0.05f);
            
            // Return to original
            _background.color = originalColor;
        }
        
        #endregion

        #region Editor Support
#if UNITY_EDITOR
        /// <summary>
        /// 为测试目的设置引用。
        /// </summary>
        public void SetReferencesForTesting(Image background, Image icon, Image cooldownOverlay, 
            TextMeshProUGUI cooldownText, Image glowEffect)
        {
            _background = background;
            _iconImage = icon;
            _cooldownOverlay = cooldownOverlay;
            _cooldownText = cooldownText;
            _glowEffect = glowEffect;
        }
        
        /// <summary>
        /// 获取当前缩放（测试用）。
        /// </summary>
        public Vector3 GetCurrentScaleForTesting()
        {
            return transform.localScale;
        }
        
        /// <summary>
        /// 获取原始缩放（测试用）。
        /// </summary>
        public Vector3 GetOriginalScaleForTesting()
        {
            return _originalScale;
        }
        
        /// <summary>
        /// 设置原始缩放（测试用）。
        /// </summary>
        public void SetOriginalScaleForTesting(Vector3 scale)
        {
            _originalScale = scale;
        }
        
        /// <summary>
        /// Gets cooldown overlay visibility for testing.
        /// </summary>
        public bool GetCooldownOverlayVisibleForTesting()
        {
            return _cooldownOverlay != null && _cooldownOverlay.enabled;
        }
        
        /// <summary>
        /// Gets cooldown text for testing.
        /// </summary>
        public string GetCooldownTextForTesting()
        {
            return _cooldownText != null ? _cooldownText.text : "";
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
