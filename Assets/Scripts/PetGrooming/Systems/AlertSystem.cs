using System;
using System.Collections;
using UnityEngine;
using PetGrooming.Core;

namespace PetGrooming.Systems
{
    /// <summary>
    /// Manages the alert state when mischief value approaches the threshold.
    /// Requirements: 6.3, 6.4, 6.5
    /// Property 16: Alert State Trigger Condition
    /// Property 17: Alert State Speed Bonus
    /// </summary>
    public class AlertSystem : MonoBehaviour
    {
        #region Singleton
        
        private static AlertSystem _instance;
        
        public static AlertSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<AlertSystem>();
                    if (_instance == null)
                    {
                        Debug.LogError("[AlertSystem] No AlertSystem instance found in scene!");
                    }
                }
                return _instance;
            }
        }
        
        #endregion

        #region Serialized Fields
        
        [Header("Configuration")]
        [SerializeField] private Phase2GameConfig _phase2Config;
        
        [Header("Alert Lights")]
        [Tooltip("Lights that flash during alert state")]
        [SerializeField] private Light[] _alertLights;
        
        [Header("Alert Sound")]
        [Tooltip("Audio source for alert sound")]
        [SerializeField] private AudioSource _alertSound;
        
        [Header("Visual Settings")]
        [Tooltip("Color of alert lights when on")]
        [SerializeField] private Color _alertLightColor = Color.red;
        
        [Tooltip("Original color of alert lights")]
        [SerializeField] private Color _normalLightColor = Color.white;
        
        #endregion

        #region Private Fields
        
        private Coroutine _flashCoroutine;
        private bool _lightsOn;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// Whether the alert state is currently active.
        /// Property 16: Alert State Trigger Condition
        /// Requirement 6.3: Alert triggers at (threshold - 100)
        /// </summary>
        public bool IsAlertActive { get; private set; }
        
        /// <summary>
        /// Speed bonus applied to Groomer during alert state.
        /// Property 17: Alert State Speed Bonus
        /// Requirement 6.5: 10% movement speed bonus
        /// </summary>
        public float GroomerSpeedBonus => _phase2Config != null ? _phase2Config.AlertGroomerSpeedBonus : 0.1f;
        
        /// <summary>
        /// Interval between light flashes in seconds.
        /// Requirement 6.4: Flashing lights
        /// </summary>
        public float FlashInterval => _phase2Config != null ? _phase2Config.AlertFlashInterval : 0.5f;
        
        /// <summary>
        /// Reference to the Phase 2 game configuration.
        /// </summary>
        public Phase2GameConfig Phase2Config => _phase2Config;
        
        #endregion

        #region Events
        
        /// <summary>
        /// Fired when alert state starts.
        /// </summary>
        public event Action OnAlertStarted;
        
        /// <summary>
        /// Fired when alert state ends.
        /// </summary>
        public event Action OnAlertEnded;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            // Singleton setup
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[AlertSystem] Duplicate AlertSystem detected, destroying this instance.");
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            
            if (_phase2Config == null)
            {
                Debug.LogWarning("[AlertSystem] Phase2GameConfig is not assigned, using default values.");
            }
        }
        
        private void Start()
        {
            // Subscribe to mischief value changes
            if (MischiefSystem.Instance != null)
            {
                MischiefSystem.Instance.OnMischiefValueChanged += OnMischiefValueChanged;
            }
            
            // Initialize lights to off state
            SetLightsState(false);
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (MischiefSystem.Instance != null)
            {
                MischiefSystem.Instance.OnMischiefValueChanged -= OnMischiefValueChanged;
            }
            
            if (_instance == this)
            {
                _instance = null;
            }
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Starts the alert state.
        /// Requirement 6.4: Display flashing lights and play alert sound.
        /// Requirement 6.5: Groomer receives 10% speed bonus.
        /// </summary>
        public void StartAlert()
        {
            if (IsAlertActive)
            {
                Debug.Log("[AlertSystem] Alert already active.");
                return;
            }
            
            IsAlertActive = true;
            
            // Start light flashing
            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
            }
            _flashCoroutine = StartCoroutine(FlashLightsCoroutine());
            
            // Play alert sound
            PlayAlertSound();
            
            OnAlertStarted?.Invoke();
            
            Debug.Log("[AlertSystem] Alert state started!");
        }
        
        /// <summary>
        /// Stops the alert state.
        /// </summary>
        public void StopAlert()
        {
            if (!IsAlertActive)
            {
                Debug.Log("[AlertSystem] Alert not active.");
                return;
            }
            
            IsAlertActive = false;
            
            // Stop light flashing
            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
                _flashCoroutine = null;
            }
            
            // Reset lights to normal
            SetLightsState(false);
            
            // Stop alert sound
            StopAlertSound();
            
            OnAlertEnded?.Invoke();
            
            Debug.Log("[AlertSystem] Alert state ended.");
        }
        
        /// <summary>
        /// Checks if alert should be triggered based on current mischief value.
        /// Property 16: Alert State Trigger Condition
        /// Requirement 6.3: Alert triggers at (threshold - 100)
        /// </summary>
        /// <param name="currentMischief">Current mischief value</param>
        public void CheckAlertCondition(int currentMischief)
        {
            if (MischiefSystem.Instance == null)
            {
                return;
            }
            
            int threshold = MischiefSystem.Instance.MischiefThreshold;
            int alertThreshold = GetAlertTriggerThreshold(threshold);
            
            bool shouldBeAlert = ShouldTriggerAlert(currentMischief, alertThreshold);
            
            if (shouldBeAlert && !IsAlertActive)
            {
                StartAlert();
            }
            // Note: Alert doesn't stop once triggered (per design - it continues until game ends)
        }
        
        /// <summary>
        /// Gets the effective speed multiplier for the Groomer.
        /// Property 17: Alert State Speed Bonus
        /// Requirement 6.5: 10% movement speed bonus during alert.
        /// </summary>
        /// <returns>Speed multiplier (1.0 if not alert, 1.1 if alert)</returns>
        public float GetGroomerSpeedMultiplier()
        {
            return CalculateGroomerSpeedMultiplier(IsAlertActive, GroomerSpeedBonus);
        }
        
        #endregion

        #region Private Methods
        
        private void OnMischiefValueChanged(int newValue)
        {
            CheckAlertCondition(newValue);
        }
        
        private IEnumerator FlashLightsCoroutine()
        {
            while (IsAlertActive)
            {
                // Toggle lights
                _lightsOn = !_lightsOn;
                SetLightsState(_lightsOn);
                
                yield return new WaitForSeconds(FlashInterval);
            }
        }
        
        private void SetLightsState(bool on)
        {
            if (_alertLights == null || _alertLights.Length == 0)
            {
                return;
            }
            
            foreach (Light light in _alertLights)
            {
                if (light != null)
                {
                    light.enabled = on;
                    light.color = on ? _alertLightColor : _normalLightColor;
                }
            }
        }
        
        private void PlayAlertSound()
        {
            if (_alertSound != null && !_alertSound.isPlaying)
            {
                _alertSound.loop = true;
                _alertSound.Play();
            }
        }
        
        private void StopAlertSound()
        {
            if (_alertSound != null && _alertSound.isPlaying)
            {
                _alertSound.Stop();
            }
        }
        
        #endregion

        #region Static Calculation Methods (Testable)
        
        /// <summary>
        /// Calculates the alert trigger threshold.
        /// Property 16: Alert State Trigger Condition
        /// Requirement 6.3: Alert triggers at (threshold - 100)
        /// </summary>
        /// <param name="mischiefThreshold">The mischief threshold for pet victory</param>
        /// <param name="alertOffset">The offset from threshold (default 100)</param>
        /// <returns>The mischief value at which alert triggers</returns>
        public static int GetAlertTriggerThreshold(int mischiefThreshold, int alertOffset = 100)
        {
            return mischiefThreshold - alertOffset;
        }
        
        /// <summary>
        /// Determines if alert should be triggered based on mischief value.
        /// Property 16: Alert State Trigger Condition
        /// Requirement 6.3: Alert triggers when mischief reaches (threshold - 100)
        /// </summary>
        /// <param name="currentMischief">Current mischief value</param>
        /// <param name="alertTriggerThreshold">The threshold at which alert triggers</param>
        /// <returns>True if alert should be active</returns>
        public static bool ShouldTriggerAlert(int currentMischief, int alertTriggerThreshold)
        {
            return currentMischief >= alertTriggerThreshold;
        }
        
        /// <summary>
        /// Calculates the Groomer's speed multiplier during alert state.
        /// Property 17: Alert State Speed Bonus
        /// Requirement 6.5: 10% movement speed bonus during alert.
        /// </summary>
        /// <param name="isAlertActive">Whether alert state is active</param>
        /// <param name="speedBonus">The speed bonus percentage (0.1 = 10%)</param>
        /// <returns>Speed multiplier (1.0 + bonus if alert, 1.0 otherwise)</returns>
        public static float CalculateGroomerSpeedMultiplier(bool isAlertActive, float speedBonus)
        {
            if (isAlertActive)
            {
                return 1.0f + speedBonus;
            }
            return 1.0f;
        }
        
        /// <summary>
        /// Validates the alert trigger condition.
        /// Property 16: Alert State Trigger Condition
        /// Requirement 6.3: When mischief value reaches (threshold - 100), alert state shall be active.
        /// </summary>
        /// <param name="mischiefValue">Current mischief value</param>
        /// <param name="mischiefThreshold">Mischief threshold for pet victory</param>
        /// <param name="alertOffset">Offset from threshold for alert trigger</param>
        /// <param name="isAlertActive">Whether alert is currently active</param>
        /// <returns>True if the alert state is correct for the given mischief value</returns>
        public static bool ValidateAlertTriggerCondition(
            int mischiefValue, 
            int mischiefThreshold, 
            int alertOffset, 
            bool isAlertActive)
        {
            int alertTriggerThreshold = GetAlertTriggerThreshold(mischiefThreshold, alertOffset);
            bool shouldBeAlert = ShouldTriggerAlert(mischiefValue, alertTriggerThreshold);
            
            // If mischief is at or above alert threshold, alert should be active
            if (shouldBeAlert)
            {
                return isAlertActive;
            }
            
            // If mischief is below alert threshold, alert can be either state
            // (once triggered, alert stays on until game ends)
            return true;
        }
        
        /// <summary>
        /// Validates the alert speed bonus.
        /// Property 17: Alert State Speed Bonus
        /// Requirement 6.5: Groomer receives 10% speed bonus during alert.
        /// </summary>
        /// <param name="isAlertActive">Whether alert is active</param>
        /// <param name="expectedBonus">Expected speed bonus (0.1 = 10%)</param>
        /// <param name="actualMultiplier">Actual speed multiplier applied</param>
        /// <returns>True if the speed bonus is correctly applied</returns>
        public static bool ValidateAlertSpeedBonus(
            bool isAlertActive, 
            float expectedBonus, 
            float actualMultiplier)
        {
            float expectedMultiplier = CalculateGroomerSpeedMultiplier(isAlertActive, expectedBonus);
            return Mathf.Approximately(actualMultiplier, expectedMultiplier);
        }
        
        #endregion

        #region Editor Support
        
#if UNITY_EDITOR
        /// <summary>
        /// Sets the Phase 2 config for testing purposes.
        /// </summary>
        public void SetPhase2ConfigForTesting(Phase2GameConfig config)
        {
            _phase2Config = config;
        }
        
        /// <summary>
        /// Sets the alert state directly for testing purposes.
        /// </summary>
        public void SetAlertStateForTesting(bool isActive)
        {
            IsAlertActive = isActive;
        }
        
        /// <summary>
        /// Gets the alert lights for testing purposes.
        /// </summary>
        public Light[] GetAlertLightsForTesting()
        {
            return _alertLights;
        }
        
        /// <summary>
        /// Sets the alert lights for testing purposes.
        /// </summary>
        public void SetAlertLightsForTesting(Light[] lights)
        {
            _alertLights = lights;
        }
        
        /// <summary>
        /// Gets the alert sound for testing purposes.
        /// </summary>
        public AudioSource GetAlertSoundForTesting()
        {
            return _alertSound;
        }
        
        /// <summary>
        /// Sets the alert sound for testing purposes.
        /// </summary>
        public void SetAlertSoundForTesting(AudioSource sound)
        {
            _alertSound = sound;
        }
#endif
        
        #endregion
    }
}
