using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PetGrooming.Core;
using PetGrooming.Systems.Skills;

namespace PetGrooming.UI.MobileUI
{
    /// <summary>
    /// 技能按钮视觉效果组件，负责冷却显示和动画。
    /// 与 OnScreenButton 输入处理分离，仅处理 UI 视觉反馈。
    /// 
    /// Requirements: 7.1, 7.2, 7.4, 7.5
    /// </summary>
    public class SkillButtonVisual : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("UI References")]
        [Tooltip("技能图标 Image")]
        [SerializeField] private Image _iconImage;
        
        [Tooltip("冷却遮罩 Image (radial fill)")]
        [SerializeField] private Image _cooldownOverlay;
        
        [Tooltip("冷却时间文本")]
        [SerializeField] private TextMeshProUGUI _cooldownText;
        
        [Tooltip("就绪发光效果 Image")]
        [SerializeField] private Image _glowEffect;
        
        [Header("Settings")]
        [Tooltip("Mobile HUD 设置资产")]
        [SerializeField] private MobileHUDSettings _settings;
        
        [Tooltip("就绪时图标颜色")]
        [SerializeField] private Color _readyColor = Color.white;
        
        [Tooltip("冷却中图标颜色 (灰度)")]
        [SerializeField] private Color _cooldownColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        
        #endregion

        #region Private Fields
        
        private SkillBase _boundSkill;
        private Vector3 _originalScale;
        private Coroutine _readyAnimationCoroutine;
        private Coroutine _pressAnimationCoroutine;
        private Coroutine _glowPulseCoroutine;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// 绑定的技能引用。
        /// </summary>
        public SkillBase BoundSkill => _boundSkill;
        
        /// <summary>
        /// 技能是否在冷却中。
        /// </summary>
        public bool IsOnCooldown => _boundSkill != null && !_boundSkill.IsReady;
        
        /// <summary>
        /// 剩余冷却时间（秒）。
        /// </summary>
        public float RemainingCooldown => _boundSkill != null ? _boundSkill.RemainingCooldown : 0f;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            _originalScale = transform.localScale;
            ValidateReferences();
        }
        
        private void Start()
        {
            // 初始化冷却显示
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

        #region Public Methods
        
        /// <summary>
        /// 绑定技能到此视觉组件。
        /// Requirement 7.5: SkillButtonVisual 观察技能状态并更新 UI。
        /// </summary>
        /// <param name="skill">要绑定的技能</param>
        public void BindToSkill(SkillBase skill)
        {
            UnbindSkill();
            
            _boundSkill = skill;
            
            if (_boundSkill != null)
            {
                // 订阅技能事件
                _boundSkill.OnCooldownChanged += OnSkillCooldownChanged;
                _boundSkill.OnSkillReady += OnSkillBecameReady;
                
                // 初始化图标
                if (_boundSkill.Icon != null)
                {
                    SetIcon(_boundSkill.Icon);
                }
                
                // 初始化冷却显示
                UpdateCooldownDisplay(_boundSkill.RemainingCooldown, _boundSkill.Cooldown);
            }
        }
        
        /// <summary>
        /// 设置技能图标。
        /// </summary>
        /// <param name="icon">图标 Sprite</param>
        public void SetIcon(Sprite icon)
        {
            if (_iconImage != null)
            {
                _iconImage.sprite = icon;
                _iconImage.enabled = icon != null;
            }
        }
        
        /// <summary>
        /// 从 SkillIconData 设置图标。
        /// </summary>
        /// <param name="iconEntry">图标数据条目</param>
        public void SetIconFromData(SkillIconData.SkillIconEntry iconEntry)
        {
            if (iconEntry == null) return;
            
            SetIcon(iconEntry.Icon);
            
            if (_glowEffect != null && iconEntry.GlowSprite != null)
            {
                _glowEffect.sprite = iconEntry.GlowSprite;
            }
            
            // 应用主题颜色到发光效果
            if (_glowEffect != null)
            {
                Color glowColor = iconEntry.ThemeColor;
                glowColor.a = 0.8f;
                _glowEffect.color = glowColor;
            }
        }
        
        /// <summary>
        /// 更新冷却显示。
        /// Requirement 7.1: 显示径向冷却遮罩。
        /// Requirement 7.2: 显示剩余冷却时间（秒）。
        /// </summary>
        /// <param name="remaining">剩余冷却时间</param>
        /// <param name="total">总冷却时间</param>
        public void UpdateCooldownDisplay(float remaining, float total)
        {
            var state = CalculateCooldownDisplayState(remaining, total);
            
            // 更新遮罩可见性和填充量
            if (_cooldownOverlay != null)
            {
                _cooldownOverlay.enabled = state.overlayVisible;
                _cooldownOverlay.fillAmount = state.fillAmount;
            }
            
            // 更新文本可见性和内容
            if (_cooldownText != null)
            {
                _cooldownText.enabled = state.textVisible;
                _cooldownText.text = state.displayText;
            }
            
            // 根据冷却状态更新图标颜色
            SetIconColor(state.overlayVisible ? _cooldownColor : _readyColor);
        }
        
        /// <summary>
        /// 播放就绪动画（技能冷却结束时）。
        /// Requirement 7.4: 技能就绪时播放发光/脉冲动画。
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
        /// 播放按下动画（缩放反馈）。
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
        /// 播放释放动画（带弹跳效果）。
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
        /// 应用 MobileHUDSettings 设置。
        /// </summary>
        public void ApplySettings()
        {
            if (_settings == null) return;
            
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
        /// 设置 MobileHUDSettings 并应用。
        /// </summary>
        /// <param name="settings">设置资产</param>
        public void SetSettings(MobileHUDSettings settings)
        {
            _settings = settings;
            ApplySettings();
        }
        
        #endregion

        #region Static Methods (用于属性测试)
        
        /// <summary>
        /// 计算冷却显示状态。
        /// 用于属性测试验证。
        /// Requirement 7.1, 7.2: 冷却遮罩和文本可见性。
        /// </summary>
        /// <param name="remainingCooldown">剩余冷却时间</param>
        /// <param name="totalCooldown">总冷却时间</param>
        /// <returns>元组 (overlayVisible, textVisible, displayText, fillAmount)</returns>
        public static (bool overlayVisible, bool textVisible, string displayText, float fillAmount) 
            CalculateCooldownDisplayState(float remainingCooldown, float totalCooldown)
        {
            bool isOnCooldown = remainingCooldown > 0f;
            bool overlayVisible = isOnCooldown;
            bool textVisible = isOnCooldown;
            
            // 计算填充量 (1 = 完全冷却, 0 = 就绪)
            float fillAmount = totalCooldown > 0f 
                ? Mathf.Clamp01(remainingCooldown / totalCooldown) 
                : 0f;
            
            // 计算显示文本（向上取整到最近的秒）
            string displayText = isOnCooldown 
                ? Mathf.CeilToInt(remainingCooldown).ToString() 
                : "";
            
            return (overlayVisible, textVisible, displayText, fillAmount);
        }
        
        /// <summary>
        /// 计算按下缩放值。
        /// 用于属性测试验证。
        /// </summary>
        /// <param name="originalScale">原始缩放</param>
        /// <param name="pressScaleMultiplier">缩放乘数（默认 0.95）</param>
        /// <returns>按下时的缩放值</returns>
        public static Vector3 CalculatePressScale(Vector3 originalScale, float pressScaleMultiplier = 0.95f)
        {
            return originalScale * pressScaleMultiplier;
        }
        
        /// <summary>
        /// 判断是否应显示冷却遮罩。
        /// 用于属性测试验证。
        /// </summary>
        /// <param name="remainingCooldown">剩余冷却时间</param>
        /// <returns>是否显示遮罩</returns>
        public static bool ShouldShowCooldownOverlay(float remainingCooldown)
        {
            return remainingCooldown > 0f;
        }
        
        /// <summary>
        /// 计算冷却文本。
        /// 用于属性测试验证。
        /// </summary>
        /// <param name="remainingCooldown">剩余冷却时间</param>
        /// <returns>显示文本</returns>
        public static string CalculateCooldownText(float remainingCooldown)
        {
            if (remainingCooldown <= 0f) return "";
            return Mathf.CeilToInt(remainingCooldown).ToString();
        }
        
        #endregion

        #region Private Methods
        
        private void ValidateReferences()
        {
            if (_iconImage == null)
            {
                Debug.LogWarning("[SkillButtonVisual] Icon Image 未配置！");
            }
            
            if (_cooldownOverlay == null)
            {
                Debug.LogWarning("[SkillButtonVisual] Cooldown Overlay Image 未配置！");
            }
            
            if (_cooldownText == null)
            {
                Debug.LogWarning("[SkillButtonVisual] Cooldown Text 未配置！");
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
        
        private void OnSkillCooldownChanged(float remainingCooldown)
        {
            if (_boundSkill != null)
            {
                UpdateCooldownDisplay(remainingCooldown, _boundSkill.Cooldown);
            }
        }
        
        private void OnSkillBecameReady()
        {
            // Requirement 7.4: 技能就绪时播放动画
            PlayReadyAnimation();
            
            // 更新图标颜色
            SetIconColor(_readyColor);
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
        
        private void StopAllAnimations()
        {
            StopReadyAnimation();
            StopPressAnimation();
            StopGlowPulse();
        }
        
        private void StopReadyAnimation()
        {
            if (_readyAnimationCoroutine != null)
            {
                StopCoroutine(_readyAnimationCoroutine);
                _readyAnimationCoroutine = null;
            }
        }
        
        private void StopPressAnimation()
        {
            if (_pressAnimationCoroutine != null)
            {
                StopCoroutine(_pressAnimationCoroutine);
                _pressAnimationCoroutine = null;
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
            
            // 过冲以产生弹跳效果
            Vector3 overshootScale = _originalScale * 1.05f;
            float halfDuration = duration * 0.6f;
            
            // 前半段：放大超过原始大小
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                t = t * t * (3f - 2f * t); // Smoothstep
                transform.localScale = Vector3.Lerp(startScale, overshootScale, t);
                yield return null;
            }
            
            // 后半段：回到原始大小
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
            
            // 脉冲缩放
            Vector3 pulseScale = _originalScale * 1.1f;
            float elapsed = 0f;
            
            // 放大
            while (elapsed < duration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration * 0.5f);
                transform.localScale = Vector3.Lerp(_originalScale, pulseScale, t);
                yield return null;
            }
            
            // 缩小
            elapsed = 0f;
            while (elapsed < duration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration * 0.5f);
                transform.localScale = Vector3.Lerp(pulseScale, _originalScale, t);
                yield return null;
            }
            
            transform.localScale = _originalScale;
            
            // 开始持续发光脉冲
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
        
        #endregion

        #region Editor Support
#if UNITY_EDITOR
        /// <summary>
        /// 为测试设置引用。
        /// </summary>
        public void SetReferencesForTesting(Image icon, Image cooldownOverlay, 
            TextMeshProUGUI cooldownText, Image glowEffect)
        {
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
        /// 获取冷却遮罩可见性（测试用）。
        /// </summary>
        public bool GetCooldownOverlayVisibleForTesting()
        {
            return _cooldownOverlay != null && _cooldownOverlay.enabled;
        }
        
        /// <summary>
        /// 获取冷却文本（测试用）。
        /// </summary>
        public string GetCooldownTextForTesting()
        {
            return _cooldownText != null ? _cooldownText.text : "";
        }
        
        /// <summary>
        /// 获取冷却遮罩填充量（测试用）。
        /// </summary>
        public float GetCooldownFillAmountForTesting()
        {
            return _cooldownOverlay != null ? _cooldownOverlay.fillAmount : 0f;
        }
        
        /// <summary>
        /// 获取发光效果可见性（测试用）。
        /// </summary>
        public bool GetGlowVisibleForTesting()
        {
            return _glowEffect != null && _glowEffect.enabled;
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
