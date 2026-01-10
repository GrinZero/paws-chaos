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
    /// Mobile skill button component for touch interaction.
    /// Handles touch input, cooldown display, and visual feedback animations.
    /// 
    /// Requirements: 2.5, 2.6, 2.7, 2.8, 4.1, 4.2, 4.3, 4.4
    /// </summary>
    public class MobileSkillButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        #region Serialized Fields
        
        [Header("UI References")]
        [Tooltip("Background image of the button")]
        [SerializeField] private Image _background;
        
        [Tooltip("Skill icon image")]
        [SerializeField] private Image _iconImage;
        
        [Tooltip("Cooldown overlay image (radial fill)")]
        [SerializeField] private Image _cooldownOverlay;
        
        [Tooltip("Cooldown time text")]
        [SerializeField] private TextMeshProUGUI _cooldownText;
        
        [Tooltip("Glow effect image for ready animation")]
        [SerializeField] private Image _glowEffect;
        
        [Header("Settings")]
        [Tooltip("Mobile HUD settings asset")]
        [SerializeField] private MobileHUDSettings _settings;
        
        [Tooltip("Button size in pixels")]
        [SerializeField] private float _buttonSize = 100f;
        
        [Tooltip("Color when skill is ready")]
        [SerializeField] private Color _readyColor = Color.white;
        
        [Tooltip("Color when skill is on cooldown (desaturated)")]
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
        /// Whether the skill is currently on cooldown.
        /// </summary>
        public bool IsOnCooldown => _boundSkill != null && !_boundSkill.IsReady;
        
        /// <summary>
        /// Remaining cooldown time in seconds.
        /// </summary>
        public float RemainingCooldown => _boundSkill != null ? _boundSkill.RemainingCooldown : 0f;
        
        /// <summary>
        /// The bound skill reference.
        /// </summary>
        public SkillBase BoundSkill => _boundSkill;
        
        /// <summary>
        /// Current button size.
        /// </summary>
        public float ButtonSize => _buttonSize;
        
        #endregion

        #region Events
        
        /// <summary>
        /// Fired when the button is pressed.
        /// </summary>
        public event Action OnButtonPressed;
        
        /// <summary>
        /// Fired when the button is released.
        /// </summary>
        public event Action OnButtonReleased;
        
        /// <summary>
        /// Fired when skill activation fails (on cooldown).
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
        /// Called when pointer is pressed on the button.
        /// Requirement 4.1: Button scales down as press feedback.
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
        /// Called when pointer is released.
        /// Requirement 4.2: Button returns to normal scale with bounce.
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
        /// Binds this button to a skill.
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
        /// Sets the skill icon sprite.
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
        /// Sets the icon from SkillIconData entry.
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
        /// Updates the cooldown display.
        /// Requirement 2.6: Radial cooldown overlay.
        /// Requirement 2.7: Cooldown time text.
        /// </summary>
        public void UpdateCooldown(float remaining, float total)
        {
            UpdateCooldownDisplay(remaining, total);
        }
        
        /// <summary>
        /// Plays the ready animation when skill becomes available.
        /// Requirement 2.8: Glow/pulse animation when ready.
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
        /// Plays the press scale animation.
        /// Requirement 4.1: Scale down to 95%.
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
        /// Plays the release animation with bounce.
        /// Requirement 4.2: Return to normal with bounce.
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
        /// Plays the fail animation when activation fails.
        /// Requirement 4.4: Shake and show "冷却中" text.
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
        /// Applies settings from MobileHUDSettings asset.
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
        /// Sets the settings asset and applies it.
        /// </summary>
        public void SetSettings(MobileHUDSettings settings)
        {
            _settings = settings;
            ApplySettings();
        }
        
        /// <summary>
        /// Sets the button size.
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
        /// Calculates the cooldown display state.
        /// Used for property-based testing.
        /// Requirement 2.6, 2.7: Cooldown overlay and text visibility.
        /// </summary>
        /// <param name="remainingCooldown">Remaining cooldown time</param>
        /// <param name="totalCooldown">Total cooldown duration</param>
        /// <returns>Tuple of (overlayVisible, textVisible, displayText, fillAmount)</returns>
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
        /// Calculates the press scale value.
        /// Used for property-based testing.
        /// Requirement 4.1: Scale to 95% on press.
        /// </summary>
        /// <param name="originalScale">Original scale of the button</param>
        /// <param name="pressScaleMultiplier">Scale multiplier (default 0.95)</param>
        /// <returns>The pressed scale value</returns>
        public static Vector3 CalculatePressScale(Vector3 originalScale, float pressScaleMultiplier = 0.95f)
        {
            return originalScale * pressScaleMultiplier;
        }
        
        /// <summary>
        /// Determines if cooldown overlay should be visible.
        /// Used for property-based testing.
        /// </summary>
        public static bool ShouldShowCooldownOverlay(float remainingCooldown)
        {
            return remainingCooldown > 0f;
        }
        
        /// <summary>
        /// Calculates the cooldown text to display.
        /// Used for property-based testing.
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
        /// Sets references for testing purposes.
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
        /// Gets the current scale for testing.
        /// </summary>
        public Vector3 GetCurrentScaleForTesting()
        {
            return transform.localScale;
        }
        
        /// <summary>
        /// Gets the original scale for testing.
        /// </summary>
        public Vector3 GetOriginalScaleForTesting()
        {
            return _originalScale;
        }
        
        /// <summary>
        /// Sets the original scale for testing.
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
