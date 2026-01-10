using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;
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
        [Tooltip("Reference to groomer controller")]
        [SerializeField] private GroomerController _groomerController;
        
        [Header("Screen Adaptation")]
        [Tooltip("Screen adapter component for UI scaling")]
        [SerializeField] private ScreenAdapter _screenAdapter;
        
        [Header("Multi-Touch")]
        [Tooltip("Multi-touch handler for simultaneous inputs")]
        [SerializeField] private MultiTouchHandler _multiTouchHandler;
        
        [Header("OnScreen Controls (Input System)")]
        [Tooltip("OnScreenStick for movement input (replaces VirtualJoystick)")]
        [SerializeField] private OnScreenStick _onScreenStick;
        
        [Tooltip("OnScreenButton array for skill buttons")]
        [SerializeField] private OnScreenButton[] _skillButtons;
        
        [Header("Skill Button Visuals")]
        [Tooltip("SkillButtonVisual components for cooldown display")]
        [SerializeField] private SkillButtonVisual[] _skillButtonVisuals;
        
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
        /// Reference to the OnScreenStick (Input System).
        /// </summary>
        public OnScreenStick OnScreenStick => _onScreenStick;
        
        /// <summary>
        /// Reference to the OnScreenButton array (Input System).
        /// </summary>
        public OnScreenButton[] SkillButtons => _skillButtons;
        
        /// <summary>
        /// Reference to the SkillButtonVisual array.
        /// </summary>
        public SkillButtonVisual[] SkillButtonVisuals => _skillButtonVisuals;
        
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
            // 输入处理已迁移到 OnScreenStick，不再需要在 Update 中处理
            // OnScreenStick 通过 Input System 直接发送输入到 StarterAssetsInputs
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
        /// 注意：迁移后，输入由 OnScreenStick 直接发送到 Input System，
        /// 此方法仅用于兼容性，返回 VirtualJoystick 的当前值。
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
        
        #region OnScreenControl Management (Requirement 4.4)
        
        /// <summary>
        /// Enables all OnScreenControl components (OnScreenStick and OnScreenButtons).
        /// Requirement 4.4: Support enabling/disabling OnScreenControl components.
        /// 
        /// Property 3: 控件启用/禁用状态同步
        /// Validates: Requirements 4.4
        /// </summary>
        public void EnableMobileControls()
        {
            // Enable OnScreenStick
            if (_onScreenStick != null)
            {
                _onScreenStick.enabled = true;
            }
            
            // Enable all OnScreenButtons
            if (_skillButtons != null)
            {
                foreach (var button in _skillButtons)
                {
                    if (button != null)
                    {
                        button.enabled = true;
                    }
                }
            }
            
            Debug.Log("[MobileHUDManager] Mobile controls enabled.");
        }
        
        /// <summary>
        /// Disables all OnScreenControl components (OnScreenStick and OnScreenButtons).
        /// Requirement 4.4: Support enabling/disabling OnScreenControl components.
        /// 
        /// Property 3: 控件启用/禁用状态同步
        /// Validates: Requirements 4.4
        /// </summary>
        public void DisableMobileControls()
        {
            // Disable OnScreenStick
            if (_onScreenStick != null)
            {
                _onScreenStick.enabled = false;
            }
            
            // Disable all OnScreenButtons
            if (_skillButtons != null)
            {
                foreach (var button in _skillButtons)
                {
                    if (button != null)
                    {
                        button.enabled = false;
                    }
                }
            }
            
            Debug.Log("[MobileHUDManager] Mobile controls disabled.");
        }
        
        /// <summary>
        /// Gets the enabled state of all OnScreenControl components.
        /// Used for property-based testing.
        /// </summary>
        /// <returns>Tuple of (stickEnabled, allButtonsEnabled)</returns>
        public (bool stickEnabled, bool allButtonsEnabled) GetOnScreenControlStates()
        {
            bool stickEnabled = _onScreenStick != null && _onScreenStick.enabled;
            
            bool allButtonsEnabled = true;
            if (_skillButtons != null && _skillButtons.Length > 0)
            {
                foreach (var button in _skillButtons)
                {
                    if (button != null && !button.enabled)
                    {
                        allButtonsEnabled = false;
                        break;
                    }
                }
            }
            
            return (stickEnabled, allButtonsEnabled);
        }
        
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
        
        /// <summary>
        /// Validates that OnScreenControl enabled states match the expected state.
        /// Used for property-based testing.
        /// 
        /// Property 3: 控件启用/禁用状态同步
        /// Validates: Requirements 4.4
        /// </summary>
        /// <param name="expectedEnabled">Expected enabled state</param>
        /// <param name="stickEnabled">Actual OnScreenStick enabled state</param>
        /// <param name="allButtonsEnabled">Whether all OnScreenButtons are enabled</param>
        /// <returns>True if all states match expected</returns>
        public static bool ValidateOnScreenControlStates(bool expectedEnabled, bool stickEnabled, bool allButtonsEnabled)
        {
            return stickEnabled == expectedEnabled && allButtonsEnabled == expectedEnabled;
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
            
            // 确保 OnScreenControl 组件能够正常工作
            EnsureOnScreenControlsWork();
            
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
        
        /// <summary>
        /// 确保 OnScreenControl 组件能够正常工作。
        /// OnScreenStick 和 OnScreenButton 通过模拟 Gamepad 输入工作，
        /// 需要确保 PlayerInput 能够接收这些输入。
        /// 
        /// 关键原理：OnScreenStick 会创建一个虚拟 Gamepad 设备，
        /// 但 PlayerInput 默认可能只监听 KeyboardMouse 方案。
        /// 解决方案是让 PlayerInput 同时监听所有设备。
        /// </summary>
        private void EnsureOnScreenControlsWork()
        {
            // 查找场景中的 PlayerInput 组件
            var playerInput = FindObjectOfType<UnityEngine.InputSystem.PlayerInput>();
            if (playerInput == null)
            {
                Debug.LogWarning("[MobileHUDManager] No PlayerInput found in scene! OnScreenControls may not work.");
                return;
            }
            
            Debug.Log($"[MobileHUDManager] Found PlayerInput on: {playerInput.gameObject.name}");
            Debug.Log($"[MobileHUDManager] Current control scheme: {playerInput.currentControlScheme}");
            Debug.Log($"[MobileHUDManager] Notification behavior: {playerInput.notificationBehavior}");
            
            // 检查是否有虚拟 Gamepad 设备
            var gamepads = UnityEngine.InputSystem.Gamepad.all;
            Debug.Log($"[MobileHUDManager] Total Gamepad devices: {gamepads.Count}");
            
            // 关键修复：禁用自动控制方案切换，让 PlayerInput 同时响应所有设备
            // 这样无论当前是什么控制方案，OnScreenStick 的虚拟 Gamepad 输入都能被接收
            if (_onScreenStick != null)
            {
                // 方案1：禁用自动切换，保持当前方案但允许所有设备输入
                playerInput.neverAutoSwitchControlSchemes = true;
                
                // 方案2：尝试切换到 Gamepad 方案（需要有 Gamepad 设备）
                // OnScreenStick 启用时会自动创建虚拟 Gamepad
                if (gamepads.Count > 0)
                {
                    try
                    {
                        // 使用虚拟 Gamepad 设备切换控制方案
                        var virtualGamepad = gamepads[gamepads.Count - 1]; // 最后添加的通常是虚拟的
                        playerInput.SwitchCurrentControlScheme("Gamepad", virtualGamepad);
                        Debug.Log($"[MobileHUDManager] Switched to Gamepad scheme with device: {virtualGamepad.name}");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[MobileHUDManager] Failed to switch control scheme: {e.Message}");
                    }
                }
                else
                {
                    Debug.LogWarning("[MobileHUDManager] No Gamepad devices found. OnScreenStick may not create virtual device until first touch.");
                    // 延迟检查：OnScreenStick 可能在第一次触摸时才创建虚拟设备
                    StartCoroutine(DelayedControlSchemeSwitch(playerInput));
                }
            }
        }
        
        /// <summary>
        /// 延迟切换控制方案，等待 OnScreenStick 创建虚拟 Gamepad。
        /// </summary>
        private System.Collections.IEnumerator DelayedControlSchemeSwitch(UnityEngine.InputSystem.PlayerInput playerInput)
        {
            // 等待一帧，让 OnScreenStick 有机会初始化
            yield return null;
            
            var gamepads = UnityEngine.InputSystem.Gamepad.all;
            if (gamepads.Count > 0)
            {
                try
                {
                    var virtualGamepad = gamepads[gamepads.Count - 1];
                    playerInput.SwitchCurrentControlScheme("Gamepad", virtualGamepad);
                    Debug.Log($"[MobileHUDManager] Delayed switch to Gamepad scheme with device: {virtualGamepad.name}");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[MobileHUDManager] Delayed switch failed: {e.Message}");
                }
            }
        }
        
        private void FindReferences()
        {
            // Find joystick if not assigned (旧版组件，V2 可能为 null)
            if (_joystick == null)
            {
                _joystick = GetComponentInChildren<VirtualJoystick>(true);
            }
            
            // Find skill wheel if not assigned (旧版组件，V2 可能为 null)
            if (_skillWheel == null)
            {
                _skillWheel = GetComponentInChildren<SkillWheelUI>(true);
            }
            
            // Find struggle button if not assigned (旧版组件，V2 可能为 null)
            if (_struggleButton == null)
            {
                _struggleButton = GetComponentInChildren<StruggleButtonUI>(true);
            }
            
            // Find screen adapter if not assigned (可选组件)
            if (_screenAdapter == null)
            {
                _screenAdapter = GetComponentInChildren<ScreenAdapter>(true);
                // V2 模式不强制创建
            }
            
            // Find multi-touch handler if not assigned (可选组件)
            if (_multiTouchHandler == null)
            {
                _multiTouchHandler = GetComponentInChildren<MultiTouchHandler>(true);
                // V2 模式不强制创建
            }
            
            // Set up screen adapter containers (仅当组件存在时)
            if (_screenAdapter != null)
            {
                RectTransform joystickRect = _joystick != null ? _joystick.GetComponent<RectTransform>() : null;
                RectTransform skillWheelRect = _skillWheel != null ? _skillWheel.GetComponent<RectTransform>() : null;
                RectTransform struggleRect = _struggleButton != null ? _struggleButton.GetComponent<RectTransform>() : null;
                
                _screenAdapter.SetContainers(joystickRect, skillWheelRect, struggleRect);
            }
            
            // Set up multi-touch handler references (仅当组件存在时)
            if (_multiTouchHandler != null)
            {
                _multiTouchHandler.SetReferences(_joystick, _skillWheel, _struggleButton);
            }
            
            // Find OnScreenStick if not assigned (V2 核心组件)
            if (_onScreenStick == null)
            {
                _onScreenStick = GetComponentInChildren<OnScreenStick>(true);
            }
            
            // 验证 OnScreenStick 配置
            if (_onScreenStick != null)
            {
                Debug.Log($"[MobileHUDManager] Found OnScreenStick: {_onScreenStick.gameObject.name}, " +
                          $"enabled: {_onScreenStick.enabled}, " +
                          $"controlPath: {_onScreenStick.controlPath}");
            }
            else
            {
                Debug.LogWarning("[MobileHUDManager] OnScreenStick not found in MobileHUD hierarchy!");
            }
            
            // Find OnScreenButtons if not assigned (V2 核心组件)
            if (_skillButtons == null || _skillButtons.Length == 0)
            {
                _skillButtons = GetComponentsInChildren<OnScreenButton>(true);
            }
            
            // 验证 OnScreenButton 配置
            if (_skillButtons != null && _skillButtons.Length > 0)
            {
                Debug.Log($"[MobileHUDManager] Found {_skillButtons.Length} OnScreenButtons:");
                foreach (var btn in _skillButtons)
                {
                    if (btn != null)
                    {
                        Debug.Log($"  - {btn.gameObject.name}: enabled={btn.enabled}, controlPath={btn.controlPath}");
                    }
                }
            }
            else
            {
                Debug.LogWarning("[MobileHUDManager] No OnScreenButtons found in MobileHUD hierarchy!");
            }
            
            // Find SkillButtonVisuals if not assigned (V2 核心组件)
            if (_skillButtonVisuals == null || _skillButtonVisuals.Length == 0)
            {
                _skillButtonVisuals = GetComponentsInChildren<SkillButtonVisual>(true);
            }
            
            // Find player's groomer controller if not assigned
            if (_groomerController == null)
            {
                _groomerController = FindObjectOfType<GroomerController>();
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
            // Subscribe to skill wheel capture button (仅当旧组件存在时)
            if (_skillWheel != null)
            {
                _skillWheel.OnCapturePressed += HandleCapturePressed;
            }
            
            // Subscribe to struggle button completion (仅当旧组件存在时)
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
            // Force mobile mode in editor for testing (优先级最高)
            #if UNITY_EDITOR
            if (_forceMobileInEditor)
            {
                Debug.Log("[MobileHUDManager] Forcing mobile mode in editor for testing.");
                return true;
            }
            #endif
            
            // Check saved preference
            // Requirement 5.6: Restore saved preference
            if (PlayerPrefs.HasKey(_uiModePrefsKey))
            {
                return PlayerPrefs.GetInt(_uiModePrefsKey) == 1;
            }
            
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
            // 首先激活 MobileHUD 根对象（如果是启用模式）
            // 这是关键！prefab 默认是禁用的，必须先激活根对象
            if (visible)
            {
                gameObject.SetActive(true);
            }
            
            // Joystick visibility (legacy VirtualJoystick)
            if (_joystick != null)
            {
                _joystick.gameObject.SetActive(visible);
            }
            
            // OnScreenStick visibility (Input System)
            // 注意：OnScreenStick 组件在 Handle 子对象上，需要找到父容器来控制可见性
            if (_onScreenStick != null)
            {
                // 找到 OnScreenStick 的根容器（向上查找直到找到名为 "OnScreenStick" 的对象或到达 MobileHUD 根）
                Transform stickContainer = _onScreenStick.transform;
                while (stickContainer.parent != null && 
                       stickContainer.parent.name != "MobileHUD_V2" && 
                       stickContainer.parent.name != "MobileHUD")
                {
                    stickContainer = stickContainer.parent;
                }
                stickContainer.gameObject.SetActive(visible);
            }
            
            // Skill wheel visibility (only for Groomer)
            if (_skillWheel != null)
            {
                _skillWheel.gameObject.SetActive(visible && _controlledCharacter == CharacterType.Groomer);
            }
            
            // OnScreenButtons visibility
            if (_skillButtons != null)
            {
                foreach (var button in _skillButtons)
                {
                    if (button != null)
                    {
                        button.gameObject.SetActive(visible);
                    }
                }
            }
            
            // SkillButtonVisuals visibility
            if (_skillButtonVisuals != null)
            {
                foreach (var visual in _skillButtonVisuals)
                {
                    if (visual != null)
                    {
                        visual.gameObject.SetActive(visible);
                    }
                }
            }
            
            // Enable/disable OnScreenControl components
            if (visible)
            {
                EnableMobileControls();
            }
            else
            {
                DisableMobileControls();
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
        /// Sets OnScreenControl references for testing purposes.
        /// </summary>
        public void SetOnScreenControlsForTesting(
            OnScreenStick onScreenStick,
            OnScreenButton[] skillButtons,
            SkillButtonVisual[] skillButtonVisuals)
        {
            _onScreenStick = onScreenStick;
            _skillButtons = skillButtons;
            _skillButtonVisuals = skillButtonVisuals;
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
