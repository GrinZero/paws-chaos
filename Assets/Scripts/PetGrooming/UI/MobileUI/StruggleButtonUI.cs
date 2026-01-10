using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using PetGrooming.Core;

namespace PetGrooming.UI.MobileUI
{
    /// <summary>
    /// Struggle button UI component for pets when captured.
    /// Handles rapid tap detection, progress accumulation, and visual feedback.
    /// 
    /// Requirements: 2.5.1, 2.5.3, 2.5.4, 2.5.5
    /// </summary>
    public class StruggleButtonUI : MonoBehaviour, IPointerDownHandler
    {
        #region Serialized Fields
        
        [Header("UI References")]
        [Tooltip("Main button image")]
        [SerializeField] private Image _buttonImage;
        
        [Tooltip("Progress ring image (radial fill)")]
        [SerializeField] private Image _progressRing;
        
        [Tooltip("Prompt text (挣扎)")]
        [SerializeField] private TextMeshProUGUI _promptText;
        
        [Header("Settings")]
        [Tooltip("Mobile HUD settings asset")]
        [SerializeField] private MobileHUDSettings _settings;
        
        [Tooltip("Button size in pixels")]
        [SerializeField] private float _buttonSize = 160f;
        
        [Tooltip("Number of taps required to complete struggle")]
        [SerializeField] private int _tapsRequired = 10;
        
        [Tooltip("Time window for struggle taps (seconds)")]
        [SerializeField] private float _tapWindow = 3f;
        
        [Tooltip("Button color (orange/red theme)")]
        [SerializeField] private Color _buttonColor = new Color(0.9f, 0.3f, 0.2f, 1f);
        
        #endregion

        #region Private Fields
        
        private RectTransform _rectTransform;
        private int _currentTaps;
        private float _tapWindowStartTime;
        private bool _isVisible;
        private bool _isCaptured;
        private Coroutine _pulseCoroutine;
        private Coroutine _tapFeedbackCoroutine;
        private Vector3 _originalScale;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// Current number of taps in the current window.
        /// </summary>
        public int CurrentTaps => _currentTaps;
        
        /// <summary>
        /// Progress value from 0 to 1.
        /// </summary>
        public float Progress => CalculateProgress(_currentTaps, _tapsRequired);
        
        /// <summary>
        /// Number of taps required to complete struggle.
        /// </summary>
        public int TapsRequired => _tapsRequired;
        
        /// <summary>
        /// Time window for taps.
        /// </summary>
        public float TapWindow => _tapWindow;
        
        /// <summary>
        /// Whether the button is currently visible.
        /// </summary>
        public bool IsVisible => _isVisible;
        
        /// <summary>
        /// Whether the pet is currently captured.
        /// </summary>
        public bool IsCaptured => _isCaptured;
        
        #endregion

        #region Events
        
        /// <summary>
        /// Fired when struggle is completed (enough taps within window).
        /// </summary>
        public event Action OnStruggleComplete;
        
        /// <summary>
        /// Fired when progress changes.
        /// </summary>
        public event Action<float> OnProgressChanged;
        
        /// <summary>
        /// Fired on each tap.
        /// </summary>
        public event Action OnTap;
        
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
            // Initialize hidden state
            Hide();
        }
        
        private void Update()
        {
            // Check if tap window has expired
            if (_isVisible && _currentTaps > 0)
            {
                if (Time.time - _tapWindowStartTime > _tapWindow)
                {
                    // Window expired, reset progress
                    ResetProgress();
                }
            }
        }
        
        private void OnDisable()
        {
            StopAllAnimations();
        }
        
        #endregion

        #region IPointerDownHandler
        
        /// <summary>
        /// Called when pointer is pressed on the button.
        /// Requirement 2.5.3: Rapid taps increase escape chance.
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_isVisible || !_isCaptured) return;
            
            // Register tap
            RegisterTap();
            
            // Play tap feedback
            PlayTapFeedback();
            
            // Fire tap event
            OnTap?.Invoke();
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Shows the struggle button.
        /// Requirement 2.5.1: Display when pet is captured.
        /// </summary>
        public void Show()
        {
            _isVisible = true;
            gameObject.SetActive(true);
            
            // Start pulse animation
            StartPulseAnimation();
            
            // Reset progress when showing
            ResetProgress();
        }
        
        /// <summary>
        /// Hides the struggle button.
        /// Requirement 2.5.5: Hidden when pet is not captured.
        /// </summary>
        public void Hide()
        {
            _isVisible = false;
            gameObject.SetActive(false);
            
            StopAllAnimations();
            ResetProgress();
        }
        
        /// <summary>
        /// Sets the captured state.
        /// Controls visibility based on capture state.
        /// </summary>
        public void SetCapturedState(bool isCaptured)
        {
            _isCaptured = isCaptured;
            
            if (_isCaptured)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }
        
        /// <summary>
        /// Resets the tap progress.
        /// </summary>
        public void ResetProgress()
        {
            _currentTaps = 0;
            _tapWindowStartTime = 0f;
            UpdateProgressDisplay();
            OnProgressChanged?.Invoke(0f);
        }
        
        /// <summary>
        /// Applies settings from MobileHUDSettings asset.
        /// </summary>
        public void ApplySettings()
        {
            if (_settings != null)
            {
                _buttonSize = _settings.StruggleButtonSize;
                _tapsRequired = _settings.StruggleTapsRequired;
                _tapWindow = _settings.StruggleTapWindow;
                _buttonColor = _settings.StruggleButtonColor;
            }
            
            if (_rectTransform != null)
            {
                _rectTransform.sizeDelta = new Vector2(_buttonSize, _buttonSize);
            }
            
            if (_buttonImage != null)
            {
                _buttonImage.color = _buttonColor;
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
        
        #endregion

        #region Static Methods (for testing)
        
        /// <summary>
        /// Calculates progress value from tap count.
        /// Used for property-based testing.
        /// Requirement 2.5.4: Progress indicator.
        /// </summary>
        /// <param name="currentTaps">Current number of taps</param>
        /// <param name="requiredTaps">Required number of taps</param>
        /// <returns>Progress value clamped to [0, 1]</returns>
        public static float CalculateProgress(int currentTaps, int requiredTaps)
        {
            if (requiredTaps <= 0) return 0f;
            return Mathf.Clamp01((float)currentTaps / requiredTaps);
        }
        
        /// <summary>
        /// Determines if struggle button should be visible based on capture state.
        /// Used for property-based testing.
        /// Requirement 2.5.1, 2.5.5: Visibility based on capture state.
        /// </summary>
        /// <param name="isCaptured">Whether the pet is captured</param>
        /// <returns>True if button should be visible</returns>
        public static bool ShouldBeVisible(bool isCaptured)
        {
            return isCaptured;
        }
        
        /// <summary>
        /// Calculates accumulated progress after N taps.
        /// Used for property-based testing.
        /// Requirement 2.5.3: Tap accumulation.
        /// </summary>
        /// <param name="tapCount">Number of taps</param>
        /// <param name="requiredTaps">Required taps for completion</param>
        /// <returns>Progress value</returns>
        public static float CalculateAccumulatedProgress(int tapCount, int requiredTaps)
        {
            return CalculateProgress(tapCount, requiredTaps);
        }
        
        /// <summary>
        /// Determines if struggle is complete.
        /// Used for property-based testing.
        /// </summary>
        /// <param name="currentTaps">Current tap count</param>
        /// <param name="requiredTaps">Required taps</param>
        /// <returns>True if struggle is complete</returns>
        public static bool IsStruggleComplete(int currentTaps, int requiredTaps)
        {
            return currentTaps >= requiredTaps;
        }
        
        #endregion

        #region Private Methods
        
        private void ValidateReferences()
        {
            if (_buttonImage == null)
            {
                Debug.LogWarning("[StruggleButtonUI] Button Image is not assigned!");
            }
            
            if (_progressRing == null)
            {
                Debug.LogWarning("[StruggleButtonUI] Progress Ring Image is not assigned!");
            }
            
            if (_promptText == null)
            {
                Debug.LogWarning("[StruggleButtonUI] Prompt Text is not assigned!");
            }
        }
        
        private void RegisterTap()
        {
            // Start new window if this is first tap or window expired
            if (_currentTaps == 0 || Time.time - _tapWindowStartTime > _tapWindow)
            {
                _tapWindowStartTime = Time.time;
                _currentTaps = 0;
            }
            
            _currentTaps++;
            
            // Update display
            UpdateProgressDisplay();
            
            // Fire progress event
            float progress = Progress;
            OnProgressChanged?.Invoke(progress);
            
            // Check for completion
            if (_currentTaps >= _tapsRequired)
            {
                OnStruggleCompleted();
            }
        }
        
        private void UpdateProgressDisplay()
        {
            if (_progressRing != null)
            {
                _progressRing.fillAmount = Progress;
            }
        }
        
        private void OnStruggleCompleted()
        {
            // Fire completion event
            OnStruggleComplete?.Invoke();
            
            // Play completion animation
            PlayCompletionAnimation();
            
            // Reset for next attempt
            ResetProgress();
        }
        
        private void StartPulseAnimation()
        {
            StopPulseAnimation();
            
            if (gameObject.activeInHierarchy)
            {
                _pulseCoroutine = StartCoroutine(PulseAnimation());
            }
        }
        
        private void StopPulseAnimation()
        {
            if (_pulseCoroutine != null)
            {
                StopCoroutine(_pulseCoroutine);
                _pulseCoroutine = null;
            }
        }
        
        private void StopAllAnimations()
        {
            StopPulseAnimation();
            
            if (_tapFeedbackCoroutine != null)
            {
                StopCoroutine(_tapFeedbackCoroutine);
                _tapFeedbackCoroutine = null;
            }
            
            transform.localScale = _originalScale;
        }
        
        private void PlayTapFeedback()
        {
            if (_tapFeedbackCoroutine != null)
            {
                StopCoroutine(_tapFeedbackCoroutine);
            }
            
            if (gameObject.activeInHierarchy)
            {
                _tapFeedbackCoroutine = StartCoroutine(TapFeedbackAnimation());
            }
        }
        
        private void PlayCompletionAnimation()
        {
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(CompletionAnimation());
            }
        }
        
        #endregion

        #region Animation Coroutines
        
        private IEnumerator PulseAnimation()
        {
            float pulseSpeed = _settings != null ? _settings.GlowPulseSpeed : 2f;
            
            while (true)
            {
                float scale = 1f + Mathf.Sin(Time.time * pulseSpeed * Mathf.PI) * 0.05f;
                transform.localScale = _originalScale * scale;
                yield return null;
            }
        }
        
        private IEnumerator TapFeedbackAnimation()
        {
            float pressScale = _settings != null ? _settings.PressScale : 0.95f;
            float duration = _settings != null ? _settings.PressAnimationDuration : 0.1f;
            
            // Scale down
            Vector3 targetScale = _originalScale * pressScale;
            float elapsed = 0f;
            
            while (elapsed < duration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration * 0.5f);
                transform.localScale = Vector3.Lerp(_originalScale, targetScale, t);
                yield return null;
            }
            
            // Scale back up
            elapsed = 0f;
            while (elapsed < duration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration * 0.5f);
                transform.localScale = Vector3.Lerp(targetScale, _originalScale, t);
                yield return null;
            }
            
            transform.localScale = _originalScale;
            _tapFeedbackCoroutine = null;
        }
        
        private IEnumerator CompletionAnimation()
        {
            // Flash and scale up
            Vector3 expandedScale = _originalScale * 1.2f;
            float duration = 0.2f;
            float elapsed = 0f;
            
            // Flash color
            Color originalColor = _buttonImage != null ? _buttonImage.color : _buttonColor;
            if (_buttonImage != null)
            {
                _buttonImage.color = Color.white;
            }
            
            // Scale up
            while (elapsed < duration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration * 0.5f);
                transform.localScale = Vector3.Lerp(_originalScale, expandedScale, t);
                yield return null;
            }
            
            // Scale back down
            elapsed = 0f;
            while (elapsed < duration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration * 0.5f);
                transform.localScale = Vector3.Lerp(expandedScale, _originalScale, t);
                yield return null;
            }
            
            // Restore color
            if (_buttonImage != null)
            {
                _buttonImage.color = originalColor;
            }
            
            transform.localScale = _originalScale;
        }
        
        #endregion

        #region Editor Support
#if UNITY_EDITOR
        /// <summary>
        /// Sets references for testing purposes.
        /// </summary>
        public void SetReferencesForTesting(Image buttonImage, Image progressRing, TextMeshProUGUI promptText)
        {
            _buttonImage = buttonImage;
            _progressRing = progressRing;
            _promptText = promptText;
        }
        
        /// <summary>
        /// Sets tap configuration for testing.
        /// </summary>
        public void SetTapConfigForTesting(int tapsRequired, float tapWindow)
        {
            _tapsRequired = tapsRequired;
            _tapWindow = tapWindow;
        }
        
        /// <summary>
        /// Simulates a tap for testing.
        /// </summary>
        public void SimulateTapForTesting()
        {
            if (_isVisible && _isCaptured)
            {
                RegisterTap();
            }
        }
        
        /// <summary>
        /// Sets captured state directly for testing.
        /// </summary>
        public void SetCapturedStateForTesting(bool isCaptured)
        {
            _isCaptured = isCaptured;
        }
        
        /// <summary>
        /// Sets visibility directly for testing.
        /// </summary>
        public void SetVisibilityForTesting(bool isVisible)
        {
            _isVisible = isVisible;
        }
        
        /// <summary>
        /// Gets progress ring fill amount for testing.
        /// </summary>
        public float GetProgressRingFillForTesting()
        {
            return _progressRing != null ? _progressRing.fillAmount : 0f;
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
