using System;
using UnityEngine;
using UnityEngine.EventSystems;
using PetGrooming.Core;
using PetGrooming.Systems;

namespace PetGrooming.UI.MobileUI
{
    /// <summary>
    /// Main controller for mobile HUD, managing mobile UI components lifecycle.
    /// Implements singleton pattern for global access.
    /// Handles device detection and UI mode switching.
    /// Integrates joystick input to character movement.
    /// 
    /// Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 1.8
    /// </summary>
    public class MobileHUDManager : MonoBehaviour
    {
        #region Singleton
        
        private static MobileHUDManager _instance;
        
        /// <summary>
        /// Singleton instance accessor.
        /// </summary>
        public static MobileHUDManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<MobileHUDManager>();
                }
                return _instance;
            }
        }
        
        #endregion

        #region Serialized Fields
        
        [Header("UI Component References")]
        [Tooltip("Virtual joystick for movement input")]
        [SerializeField] private VirtualJoystick _joystick;
        
        [Tooltip("Skill wheel UI for groomer skills")]
        [SerializeField] private SkillWheelUI _skillWheel;
        
        [Tooltip("Struggle button UI for pet escape")]
        [SerializeField] private StruggleButtonUI _struggleButton;
        
        [Tooltip("Desktop UI root to hide when mobile mode is active")]
        [SerializeField] private GameObject _desktopUI;
        
        [Header("Settings")]
        [Tooltip("Mobile HUD settings asset")]
        [SerializeField] private MobileHUDSettings _settings;
        
        [Tooltip("Auto-detect touch device on start")]
        [SerializeField] private bool _autoDetectDevice = true;
        
        [Tooltip("Force mobile mode in editor (for testing)")]
        [SerializeField] private bool _forceMobileInEditor = true;
        
        [Tooltip("PlayerPrefs key for UI mode preference")]
        [SerializeField] private string _uiModePrefsKey = "MobileUIMode";
        
        /// <summary>
        /// Default PlayerPrefs key for UI mode preference.
        /// </summary>
        public const string DefaultUIModePrefKey = "MobileUIMode";
        
        [Header("Character References")]
        [Tooltip("Reference to player movement component")]
        [SerializeField] private PlayerMovement _playerMovement;
        
        [Tooltip("Reference to groomer controller")]
        [SerializeField] private GroomerController _groomerController;
        
        [Header("Screen Adaptation")]
        [Tooltip("Screen adapter component for UI scaling")]
        [SerializeField] private ScreenAdapter _screenAdapter;
        
        [Header("Multi-Touch")]
        [Tooltip("Multi-touch handler for simultaneous inputs")]
        [SerializeField] private MultiTouchHandler _multiTouchHandler;
        
        #endregion

        #region Private Fields
        
        private bool _isMobileMode;
        private bool _isInitialized;
        private CharacterType _controlledCharacter = CharacterType.Groomer;
        
        #endregion

        #region Enums
        
        /// <summary>
        /// Type of character being controlled.
        /// </summary>
        public enum CharacterType
        {
            Groomer,
            Pet
        }
        
        #endregion

        #region Properties
        
        /// <summary>
        /// Whether mobile HUD mode is currently active.
        /// Requirement 5.4, 5.5: UI mode visibility toggle.
        /// </summary>
        public bool IsMobileMode => _isMobileMode;
        
        /// <summary>
        /// Reference to the virtual joystick.
        /// </summary>
        public VirtualJoystick Joystick => _joystick;
        
        /// <summary>
        /// Reference to the skill wheel.
        /// </summary>
        public SkillWheelUI SkillWheel => _skillWheel;
        
        /// <summary>
        /// Reference to the struggle button.
        /// </summary>
        public StruggleButtonUI StruggleButton => _struggleButton;
        
        /// <summary>
        /// Reference to the desktop UI.
        /// </summary>
        public GameObject DesktopUI => _desktopUI;
        
        /// <summary>
        /// Reference to the screen adapter.
        /// </summary>
        public ScreenAdapter ScreenAdapter => _screenAdapter;
        
        /// <summary>
        /// Reference to the multi-touch handler.
        /// </summary>
        public MultiTouchHandler MultiTouchHandler => _multiTouchHandler;
        
        /// <summary>
        /// Currently controlled character type.
        /// </summary>
        public CharacterType ControlledCharacter => _controlledCharacter;
        
        #endregion

        #region Events
        
        /// <summary>
        /// Fired when UI mode changes.
        /// </summary>
        public event Action<bool> OnMobileModeChanged;
        
        /// <summary>
        /// Fired when controlled character changes.
        /// </summary>
        public event Action<CharacterType> OnControlledCharacterChanged;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            // Singleton setup
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[MobileHUDManager] Duplicate instance detected, destroying this one.");
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
        }
        
        private void Start()
        {
            Initialize();
        }
        
        private void Update()
        {
            if (!_isInitialized || !_isMobileMode) return;
            
            // Update movement input from joystick
            UpdateMovementInput();
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
            
            UnsubscribeFromEvents();
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Enables mobile HUD mode.
        /// Requirement 5.1: Enable Mobile_HUD on touch device.
        /// Requirement 5.4: Hide standard skill bar when mobile mode enabled.
        /// </summary>
        public void EnableMobileHUD()
        {
            if (_isMobileMode) return;
            
            _isMobileMode = true;
            
            // Show mobile UI components
            SetMobileUIVisibility(true);
            
            // Hide desktop UI
            SetDesktopUIVisibility(false);
            
            // Save preference
            SaveUIPreference(true);
            
            OnMobileModeChanged?.Invoke(true);
            
            Debug.Log("[MobileHUDManager] Mobile HUD enabled.");
        }
        
        /// <summary>
        /// Disables mobile HUD mode.
        /// Requirement 5.2: Use standard keyboard/mouse UI on desktop.
        /// Requirement 5.5: Hide mobile UI when disabled.
        /// </summary>
        public void DisableMobileHUD()
        {
            if (!_isMobileMode) return;
            
            _isMobileMode = false;
            
            // Hide mobile UI components
            SetMobileUIVisibility(false);
            
            // Show desktop UI
            SetDesktopUIVisibility(true);
            
            // Save preference
            SaveUIPreference(false);
            
            OnMobileModeChanged?.Invoke(false);
            
            Debug.Log("[MobileHUDManager] Mobile HUD disabled.");
        }
        
        /// <summary>
        /// Toggles between mobile and desktop UI modes.
        /// Requirement 5.3: Manual toggle between modes.
        /// </summary>
        public void ToggleMobileHUD()
        {
            if (_isMobileMode)
            {
                DisableMobileHUD();
            }
            else
            {
                EnableMobileHUD();
            }
        }
        
        /// <summary>
        /// Sets the controlled character type.
        /// Adjusts UI based on character (Groomer shows skill wheel, Pet shows struggle button).
        /// </summary>
        public void SetControlledCharacter(CharacterType characterType)
        {
            if (_controlledCharacter == characterType) return;
            
            _controlledCharacter = characterType;
            
            UpdateUIForCharacter();
            
            OnControlledCharacterChanged?.Invoke(characterType);
            
            Debug.Log($"[MobileHUDManager] Controlled character set to: {characterType}");
        }
        
        /// <summary>
        /// Gets the current movement input from the joystick.
        /// Requirement 1.8: Movement input equivalent to keyboard/gamepad.
        /// </summary>
        /// <returns>Normalized movement vector from joystick.</returns>
        public Vector2 GetMovementInput()
        {
            if (!_isMobileMode || _joystick == null)
            {
                return Vector2.zero;
            }
            
            return _joystick.InputVector;
        }
        
        /// <summary>
        /// Sets the player movement reference for joystick integration.
        /// </summary>
        public void SetPlayerMovement(PlayerMovement playerMovement)
        {
            _playerMovement = playerMovement;
        }
        
        /// <summary>
        /// Sets the groomer controller reference.
        /// </summary>
        public void SetGroomerController(GroomerController groomerController)
        {
            _groomerController = groomerController;
            
            // Bind skill wheel to groomer skills if available
            if (_skillWheel != null && groomerController != null)
            {
                var skillManager = groomerController.GetComponent<Systems.Skills.GroomerSkillManager>();
                if (skillManager != null)
                {
                    _skillWheel.BindToGroomerSkills(skillManager);
                }
            }
        }
        
        /// <summary>
        /// Applies settings from MobileHUDSettings asset.
        /// </summary>
        public void ApplySettings()
        {
            if (_settings == null) return;
            
            if (_joystick != null)
            {
                _joystick.SetSettings(_settings);
            }
            
            if (_skillWheel != null)
            {
                _skillWheel.SetSettings(_settings);
            }
            
            if (_struggleButton != null)
            {
                _struggleButton.SetSettings(_settings);
            }
            
            if (_screenAdapter != null)
            {
                _screenAdapter.SetSettings(_settings);
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
        
        #region UI Preference Persistence (Requirement 5.6)
        
        /// <summary>
        /// Gets the saved UI mode preference.
        /// Requirement 5.6: UI mode preference saved and restored.
        /// </summary>
        /// <returns>True if mobile mode was saved, false if desktop mode, null if no preference saved.</returns>
        public bool? GetSavedUIPreference()
        {
            if (!PlayerPrefs.HasKey(_uiModePrefsKey))
            {
                return null;
            }
            return PlayerPrefs.GetInt(_uiModePrefsKey) == 1;
        }
        
        /// <summary>
        /// Clears the saved UI mode preference.
        /// Useful for testing or resetting to default behavior.
        /// </summary>
        public void ClearSavedUIPreference()
        {
            if (PlayerPrefs.HasKey(_uiModePrefsKey))
            {
                PlayerPrefs.DeleteKey(_uiModePrefsKey);
                PlayerPrefs.Save();
                Debug.Log("[MobileHUDManager] UI preference cleared.");
            }
        }
        
        /// <summary>
        /// Checks if a UI mode preference has been saved.
        /// </summary>
        /// <returns>True if a preference exists.</returns>
        public bool HasSavedUIPreference()
        {
            return PlayerPrefs.HasKey(_uiModePrefsKey);
        }
        
        /// <summary>
        /// Gets the PlayerPrefs key used for UI mode preference.
        /// </summary>
        public string UIModePrefKey => _uiModePrefsKey;
        
        #endregion
        
        #endregion

        #region Static Methods (for testing)
        
        /// <summary>
        /// Determines the expected visibility state of mobile UI based on mode.
        /// Used for property-based testing.
        /// 
        /// Property 11: UI Mode Visibility Toggle
        /// Validates: Requirements 5.4, 5.5
        /// </summary>
        /// <param name="isMobileMode">Whether mobile mode is enabled</param>
        /// <returns>Expected mobile UI visibility</returns>
        public static bool GetExpectedMobileUIVisibility(bool isMobileMode)
        {
            return isMobileMode;
        }
        
        /// <summary>
        /// Determines the expected visibility state of desktop UI based on mode.
        /// Used for property-based testing.
        /// 
        /// Property 11: UI Mode Visibility Toggle
        /// Validates: Requirements 5.4, 5.5
        /// </summary>
        /// <param name="isMobileMode">Whether mobile mode is enabled</param>
        /// <returns>Expected desktop UI visibility</returns>
        public static bool GetExpectedDesktopUIVisibility(bool isMobileMode)
        {
            return !isMobileMode;
        }
        
        /// <summary>
        /// Validates that UI visibility states are mutually exclusive.
        /// Used for property-based testing.
        /// 
        /// Property 11: UI Mode Visibility Toggle
        /// </summary>
        /// <param name="mobileUIVisible">Whether mobile UI is visible</param>
        /// <param name="desktopUIVisible">Whether desktop UI is visible</param>
        /// <returns>True if visibility states are valid (mutually exclusive)</returns>
        public static bool ValidateUIVisibilityStates(bool mobileUIVisible, bool desktopUIVisible)
        {
            // Mobile and desktop UI should be mutually exclusive
            return mobileUIVisible != desktopUIVisible;
        }
        
        /// <summary>
        /// Determines if device should use mobile mode based on touch support.
        /// Used for property-based testing.
        /// 
        /// Requirement 5.1, 5.2: Auto-detect device type.
        /// </summary>
        /// <param name="isTouchDevice">Whether device supports touch</param>
        /// <returns>Whether mobile mode should be enabled</returns>
        public static bool ShouldUseMobileMode(bool isTouchDevice)
        {
            return isTouchDevice;
        }
        
        /// <summary>
        /// Validates UI mode transition.
        /// Used for property-based testing.
        /// </summary>
        /// <param name="previousMode">Previous mobile mode state</param>
        /// <param name="toggleAction">Whether toggle was called</param>
        /// <param name="newMode">New mobile mode state</param>
        /// <returns>True if transition is valid</returns>
        public static bool ValidateUIToggle(bool previousMode, bool toggleAction, bool newMode)
        {
            if (toggleAction)
            {
                // After toggle, mode should be inverted
                return newMode == !previousMode;
            }
            else
            {
                // Without toggle, mode should remain unchanged
                return newMode == previousMode;
            }
        }
        
        /// <summary>
        /// Validates that UI preference persistence works correctly.
        /// Used for property-based testing.
        /// 
        /// Requirement 5.6: UI mode preference saved and restored.
        /// </summary>
        /// <param name="savedMode">The mode that was saved</param>
        /// <param name="loadedMode">The mode that was loaded</param>
        /// <returns>True if saved and loaded modes match</returns>
        public static bool ValidateUIPreferencePersistence(bool savedMode, bool loadedMode)
        {
            return savedMode == loadedMode;
        }
        
        /// <summary>
        /// Validates that initial UI mode is determined correctly.
        /// Used for property-based testing.
        /// 
        /// Requirement 5.6: Restore saved preference on startup.
        /// </summary>
        /// <param name="hasSavedPreference">Whether a preference was saved</param>
        /// <param name="savedPreference">The saved preference value (if any)</param>
        /// <param name="isTouchDevice">Whether device supports touch</param>
        /// <param name="autoDetect">Whether auto-detection is enabled</param>
        /// <param name="resultMode">The resulting UI mode</param>
        /// <returns>True if result mode is correct</returns>
        public static bool ValidateInitialUIMode(
            bool hasSavedPreference, 
            bool savedPreference, 
            bool isTouchDevice, 
            bool autoDetect, 
            bool resultMode)
        {
            // If saved preference exists, it should be used
            if (hasSavedPreference)
            {
                return resultMode == savedPreference;
            }
            
            // If auto-detect is enabled, use touch device detection
            if (autoDetect)
            {
                return resultMode == isTouchDevice;
            }
            
            // Default to desktop mode
            return resultMode == false;
        }
        
        #endregion

        #region Private Methods
        
        private void Initialize()
        {
            if (_isInitialized) return;
            
            // Find references if not assigned
            FindReferences();
            
            // Subscribe to events
            SubscribeToEvents();
            
            // Apply settings
            ApplySettings();
            
            // Determine initial UI mode
            bool shouldUseMobile = DetermineInitialUIMode();
            
            if (shouldUseMobile)
            {
                EnableMobileHUD();
            }
            else
            {
                DisableMobileHUD();
            }
            
            // Update UI for current character
            UpdateUIForCharacter();
            
            _isInitialized = true;
            
            Debug.Log("[MobileHUDManager] Initialization complete.");
        }
        
        private void FindReferences()
        {
            // Find joystick if not assigned
            if (_joystick == null)
            {
                _joystick = GetComponentInChildren<VirtualJoystick>(true);
            }
            
            // Find skill wheel if not assigned
            if (_skillWheel == null)
            {
                _skillWheel = GetComponentInChildren<SkillWheelUI>(true);
            }
            
            // Find struggle button if not assigned
            if (_struggleButton == null)
            {
                _struggleButton = GetComponentInChildren<StruggleButtonUI>(true);
            }
            
            // Find screen adapter if not assigned
            if (_screenAdapter == null)
            {
                _screenAdapter = GetComponentInChildren<ScreenAdapter>(true);
                
                // Create one if not found
                if (_screenAdapter == null)
                {
                    _screenAdapter = gameObject.AddComponent<ScreenAdapter>();
                }
            }
            
            // Find multi-touch handler if not assigned
            if (_multiTouchHandler == null)
            {
                _multiTouchHandler = GetComponentInChildren<MultiTouchHandler>(true);
                
                // Create one if not found
                if (_multiTouchHandler == null)
                {
                    _multiTouchHandler = gameObject.AddComponent<MultiTouchHandler>();
                }
            }
            
            // Set up screen adapter containers
            if (_screenAdapter != null)
            {
                RectTransform joystickRect = _joystick != null ? _joystick.GetComponent<RectTransform>() : null;
                RectTransform skillWheelRect = _skillWheel != null ? _skillWheel.GetComponent<RectTransform>() : null;
                RectTransform struggleRect = _struggleButton != null ? _struggleButton.GetComponent<RectTransform>() : null;
                
                _screenAdapter.SetContainers(joystickRect, skillWheelRect, struggleRect);
            }
            
            // Set up multi-touch handler references
            if (_multiTouchHandler != null)
            {
                _multiTouchHandler.SetReferences(_joystick, _skillWheel, _struggleButton);
            }
            
            // Find player movement if not assigned
            if (_playerMovement == null)
            {
                var groomer = FindObjectOfType<GroomerController>();
                if (groomer != null)
                {
                    _playerMovement = groomer.GetComponent<PlayerMovement>();
                    _groomerController = groomer;
                }
            }
            
            // Find desktop UI (skill bar) if not assigned
            if (_desktopUI == null)
            {
                var skillBar = FindObjectOfType<UI.SkillBarUI>();
                if (skillBar != null)
                {
                    _desktopUI = skillBar.gameObject;
                }
            }
        }
        
        private void SubscribeToEvents()
        {
            // Subscribe to skill wheel capture button
            if (_skillWheel != null)
            {
                _skillWheel.OnCapturePressed += HandleCapturePressed;
            }
            
            // Subscribe to struggle button completion
            if (_struggleButton != null)
            {
                _struggleButton.OnStruggleComplete += HandleStruggleComplete;
            }
        }
        
        private void UnsubscribeFromEvents()
        {
            if (_skillWheel != null)
            {
                _skillWheel.OnCapturePressed -= HandleCapturePressed;
            }
            
            if (_struggleButton != null)
            {
                _struggleButton.OnStruggleComplete -= HandleStruggleComplete;
            }
        }
        
        private bool DetermineInitialUIMode()
        {
            // Check saved preference first
            // Requirement 5.6: Restore saved preference
            if (PlayerPrefs.HasKey(_uiModePrefsKey))
            {
                return PlayerPrefs.GetInt(_uiModePrefsKey) == 1;
            }
            
            // Force mobile mode in editor for testing
            #if UNITY_EDITOR
            if (_forceMobileInEditor)
            {
                Debug.Log("[MobileHUDManager] Forcing mobile mode in editor for testing.");
                return true;
            }
            #endif
            
            // Auto-detect if enabled
            // Requirement 5.1, 5.2: Auto-detect device type
            if (_autoDetectDevice)
            {
                return IsTouchDevice();
            }
            
            return false;
        }
        
        private bool IsTouchDevice()
        {
            // Check if device supports touch
            // Requirement 5.1: Touch-enabled device detection
            return Input.touchSupported || 
                   Application.platform == RuntimePlatform.Android ||
                   Application.platform == RuntimePlatform.IPhonePlayer;
        }
        
        private void SetMobileUIVisibility(bool visible)
        {
            // Joystick visibility
            if (_joystick != null)
            {
                _joystick.gameObject.SetActive(visible);
            }
            
            // Skill wheel visibility (only for Groomer)
            if (_skillWheel != null)
            {
                _skillWheel.gameObject.SetActive(visible && _controlledCharacter == CharacterType.Groomer);
            }
            
            // Struggle button is controlled by capture state, not mode
            // It will be shown/hidden based on pet capture state
        }
        
        private void SetDesktopUIVisibility(bool visible)
        {
            if (_desktopUI != null)
            {
                _desktopUI.SetActive(visible);
            }
        }
        
        private void UpdateUIForCharacter()
        {
            if (!_isMobileMode) return;
            
            switch (_controlledCharacter)
            {
                case CharacterType.Groomer:
                    // Show skill wheel, hide struggle button
                    if (_skillWheel != null)
                    {
                        _skillWheel.gameObject.SetActive(true);
                    }
                    if (_struggleButton != null)
                    {
                        _struggleButton.Hide();
                    }
                    break;
                    
                case CharacterType.Pet:
                    // Hide skill wheel, struggle button controlled by capture state
                    if (_skillWheel != null)
                    {
                        _skillWheel.gameObject.SetActive(false);
                    }
                    break;
            }
        }
        
        private void UpdateMovementInput()
        {
            if (_playerMovement == null || _joystick == null) return;
            
            // Get joystick input
            Vector2 input = _joystick.InputVector;
            
            // Apply to player movement
            // Requirement 1.8: Joystick input equivalent to keyboard/gamepad
            _playerMovement.SetMobileInput(input);
        }
        
        private void HandleCapturePressed()
        {
            // Trigger capture on groomer controller
            if (_groomerController != null)
            {
                _groomerController.TryCapturePet();
            }
        }
        
        private void HandleStruggleComplete()
        {
            // Handle struggle completion (pet escape)
            Debug.Log("[MobileHUDManager] Struggle complete - pet escape triggered");
            // The actual escape logic is handled by PetAI
        }
        
        private void SaveUIPreference(bool isMobileMode)
        {
            // Requirement 5.6: Save UI mode preference
            PlayerPrefs.SetInt(_uiModePrefsKey, isMobileMode ? 1 : 0);
            PlayerPrefs.Save();
        }
        
        #endregion

        #region Editor Support
#if UNITY_EDITOR
        /// <summary>
        /// Sets references for testing purposes.
        /// </summary>
        public void SetReferencesForTesting(
            VirtualJoystick joystick, 
            SkillWheelUI skillWheel, 
            StruggleButtonUI struggleButton,
            GameObject desktopUI)
        {
            _joystick = joystick;
            _skillWheel = skillWheel;
            _struggleButton = struggleButton;
            _desktopUI = desktopUI;
        }
        
        /// <summary>
        /// Sets mobile mode directly for testing.
        /// </summary>
        public void SetMobileModeForTesting(bool isMobileMode)
        {
            _isMobileMode = isMobileMode;
        }
        
        /// <summary>
        /// Gets initialization state for testing.
        /// </summary>
        public bool IsInitializedForTesting => _isInitialized;
        
        /// <summary>
        /// Forces initialization for testing.
        /// </summary>
        public void InitializeForTesting()
        {
            _isInitialized = false;
            Initialize();
        }
        
        /// <summary>
        /// Resets singleton for testing.
        /// </summary>
        public static void ResetInstanceForTesting()
        {
            _instance = null;
        }
        
        private void OnValidate()
        {
            if (Application.isPlaying && _isInitialized)
            {
                ApplySettings();
            }
        }
#endif
        #endregion
    }
}
