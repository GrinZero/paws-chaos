using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;
using PetGrooming.Core;
using PetGrooming.Systems;

namespace PetGrooming.UI.MobileUI
{
    /// <summary>
    /// 移动HUD的主控制器，管理移动UI组件的生命周期。
    /// 实现单例模式以实现全局访问。
    /// 处理设备检测和UI模式切换。
    /// 将摇杆输入集成到角色移动中。
    /// 
    /// 需求：5.1, 5.2, 5.3, 5.4, 5.5, 1.8
    /// </summary>
    public class MobileHUDManager : MonoBehaviour
    {
        #region Singleton
        
        private static MobileHUDManager _instance;
        
        /// <summary>
        /// 单例实例访问器。
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
        
        [Header("UI组件引用")]
        [Tooltip("用于移动输入的虚拟摇杆")]
        [SerializeField] private VirtualJoystick _joystick;
        
        [Tooltip("美容师技能的技能轮UI")]
        [SerializeField] private SkillWheelUI _skillWheel;
        
        [Tooltip("宠物逃脱的挣扎按钮UI")]
        [SerializeField] private StruggleButtonUI _struggleButton;
        
        [Tooltip("移动模式激活时要隐藏的桌面UI根节点")]
        [SerializeField] private GameObject _desktopUI;
        
        [Header("设置")]
        [Tooltip("移动HUD设置资源")]
        [SerializeField] private MobileHUDSettings _settings;
        
        [Tooltip("启动时自动检测触摸设备")]
        [SerializeField] private bool _autoDetectDevice = true;
        
        [Tooltip("在编辑器中强制移动模式（用于测试）")]
        [SerializeField] private bool _forceMobileInEditor = true;
        
        [Tooltip("UI模式偏好的PlayerPrefs键")]
        [SerializeField] private string _uiModePrefsKey = "MobileUIMode";
        
        /// <summary>
        /// UI模式偏好的默认PlayerPrefs键。
        /// </summary>
        public const string DefaultUIModePrefKey = "MobileUIMode";
        
        [Header("角色引用")]
        [Tooltip("美容师控制器引用")]
        [SerializeField] private GroomerController _groomerController;
        
        [Header("屏幕适配")]
        [Tooltip("用于UI缩放的屏幕适配器组件")]
        [SerializeField] private ScreenAdapter _screenAdapter;
        
        [Header("多点触控")]
        [Tooltip("用于同时输入的多点触控处理器")]
        [SerializeField] private MultiTouchHandler _multiTouchHandler;
        
        [Header("屏幕控制（输入系统）")]
        [Tooltip("移动输入的OnScreenStick（替代VirtualJoystick）")]
        [SerializeField] private OnScreenStick _onScreenStick;
        
        [Tooltip("技能按钮的OnScreenButton数组")]
        [SerializeField] private OnScreenButton[] _skillButtons;
        
        [Header("技能按钮视觉效果")]
        [Tooltip("用于冷却显示的SkillButtonVisual组件")]
        [SerializeField] private SkillButtonVisual[] _skillButtonVisuals;
        
        #endregion

        #region Private Fields
        
        private bool _isMobileMode;
        private bool _isInitialized;
        private CharacterType _controlledCharacter = CharacterType.Groomer;
        
        #endregion

        #region Enums
        
        /// <summary>
        /// 正在控制的角色类型。
        /// </summary>
        public enum CharacterType
        {
            Groomer,
            Pet
        }
        
        #endregion

        #region Properties
        
        /// <summary>
        /// 移动HUD模式当前是否激活。
        /// 需求 5.4, 5.5: UI模式可见性切换。
        /// </summary>
        public bool IsMobileMode => _isMobileMode;
        
        /// <summary>
        /// 虚拟摇杆的引用。
        /// </summary>
        public VirtualJoystick Joystick => _joystick;
        
        /// <summary>
        /// 技能轮的引用。
        /// </summary>
        public SkillWheelUI SkillWheel => _skillWheel;
        
        /// <summary>
        /// 挣扎按钮的引用。
        /// </summary>
        public StruggleButtonUI StruggleButton => _struggleButton;
        
        /// <summary>
        /// 桌面UI的引用。
        /// </summary>
        public GameObject DesktopUI => _desktopUI;
        
        /// <summary>
        /// 屏幕适配器的引用。
        /// </summary>
        public ScreenAdapter ScreenAdapter => _screenAdapter;
        
        /// <summary>
        /// 多点触控处理器的引用。
        /// </summary>
        public MultiTouchHandler MultiTouchHandler => _multiTouchHandler;
        
        /// <summary>
        /// OnScreenStick的引用（输入系统）。
        /// </summary>
        public OnScreenStick OnScreenStick => _onScreenStick;
        
        /// <summary>
        /// OnScreenButton数组的引用（输入系统）。
        /// </summary>
        public OnScreenButton[] SkillButtons => _skillButtons;
        
        /// <summary>
        /// SkillButtonVisual数组的引用。
        /// </summary>
        public SkillButtonVisual[] SkillButtonVisuals => _skillButtonVisuals;
        
        /// <summary>
        /// 当前控制的角色类型。
        /// </summary>
        public CharacterType ControlledCharacter => _controlledCharacter;
        
        #endregion

        #region Events
        
        /// <summary>
        /// 当UI模式改变时触发。
        /// </summary>
        public event Action<bool> OnMobileModeChanged;
        
        /// <summary>
        /// 当控制的角色改变时触发。
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
        /// 启用移动HUD模式。
        /// 需求 5.1: 在触摸设备上启用Mobile_HUD。
        /// 需求 5.4: 当移动模式启用时隐藏标准技能栏。
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
        /// 禁用移动HUD模式。
        /// 需求 5.2: 在桌面端使用标准键盘/鼠标UI。
        /// 需求 5.5: 当禁用时隐藏移动UI。
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
        /// 在移动和桌面UI模式之间切换。
        /// 需求 5.3: 手动在模式之间切换。
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
        /// 设置控制的角色类型。
        /// 根据角色调整UI（美容师显示技能轮，宠物显示挣扎按钮）。
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
        /// 从摇杆获取当前移动输入。
        /// 注意：迁移后，输入由 OnScreenStick 直接发送到 Input System，
        /// 此方法仅用于兼容性，返回 VirtualJoystick 的当前值。
        /// </summary>
        /// <returns>从摇杆获取的归一化移动向量。</returns>
        public Vector2 GetMovementInput()
        {
            if (!_isMobileMode || _joystick == null)
            {
                return Vector2.zero;
            }
            
            return _joystick.InputVector;
        }
        
        /// <summary>
        /// 设置美容师控制器引用。
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
            
            // 绑定 SkillButtonVisual 到技能系统
            BindSkillButtonVisuals(groomerController);
        }
        
        /// <summary>
        /// 绑定 SkillButtonVisual 组件到 GroomerSkillManager 的技能。
        /// 这样技能按钮就能显示冷却效果和就绪动画。
        /// </summary>
        private void BindSkillButtonVisuals(GroomerController groomerController)
        {
            if (groomerController == null) return;
            
            var skillManager = groomerController.GetComponent<Systems.Skills.GroomerSkillManager>();
            if (skillManager == null)
            {
                Debug.LogWarning("[MobileHUDManager] GroomerSkillManager not found on GroomerController!");
                return;
            }
            
            if (_skillButtonVisuals == null || _skillButtonVisuals.Length == 0)
            {
                Debug.LogWarning("[MobileHUDManager] No SkillButtonVisuals assigned!");
                return;
            }
            
            // 加载技能图标数据
            var skillIconData = Resources.Load<Core.SkillIconData>("SkillIconData");
            
            // 绑定技能到视觉组件
            // 按钮顺序：Skill1 (CaptureNet), Skill2 (Leash), Skill3 (CalmingSpray), MainButton (Capture)
            for (int i = 0; i < _skillButtonVisuals.Length && i < skillManager.SkillCount; i++)
            {
                var visual = _skillButtonVisuals[i];
                var skill = skillManager.GetSkill(i);
                
                if (visual != null && skill != null)
                {
                    visual.BindToSkill(skill);
                    
                    // 设置技能图标
                    if (skillIconData != null)
                    {
                        var iconEntry = skillIconData.GetIconForSkill(skill.SkillName);
                        if (iconEntry != null)
                        {
                            visual.SetIconFromData(iconEntry);
                        }
                    }
                    
                    Debug.Log($"[MobileHUDManager] Bound skill '{skill.SkillName}' to SkillButtonVisual {i}");
                }
            }
            
            // 设置捕获按钮图标（第4个按钮，如果存在）
            if (_skillButtonVisuals.Length > 3 && _skillButtonVisuals[3] != null && skillIconData != null)
            {
                var captureIconEntry = skillIconData.CaptureButton;
                if (captureIconEntry != null)
                {
                    _skillButtonVisuals[3].SetIconFromData(captureIconEntry);
                }
            }
            
            // 订阅技能激活事件以播放按下动画
            skillManager.OnSkillActivated += OnSkillActivated;
            skillManager.OnSkillActivationFailed += OnSkillActivationFailed;
            
            Debug.Log($"[MobileHUDManager] Bound {_skillButtonVisuals.Length} skill button visuals to GroomerSkillManager");
        }
        
        /// <summary>
        /// 技能激活时的回调，播放按下动画并停止发光。
        /// </summary>
        private void OnSkillActivated(int skillIndex, Systems.Skills.SkillBase skill)
        {
            if (_skillButtonVisuals != null && skillIndex < _skillButtonVisuals.Length)
            {
                var visual = _skillButtonVisuals[skillIndex];
                if (visual != null)
                {
                    // 停止发光动画并播放按下效果
                    visual.OnSkillUsed();
                }
            }
        }
        
        /// <summary>
        /// 技能激活失败时的回调（冷却中）。
        /// </summary>
        private void OnSkillActivationFailed(int skillIndex, Systems.Skills.SkillBase skill)
        {
            // 可以播放一个"不可用"的抖动动画
            Debug.Log($"[MobileHUDManager] Skill {skillIndex} activation failed - on cooldown");
        }
        
        /// <summary>
        /// 应用来自MobileHUDSettings资源的设置。
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
        /// 设置设置资源并应用它。
        /// </summary>
        public void SetSettings(MobileHUDSettings settings)
        {
            _settings = settings;
            ApplySettings();
        }
        
        #region UI Preference Persistence (Requirement 5.6)
        
        /// <summary>
        /// 获取保存的UI模式偏好。
        /// 需求 5.6: UI模式偏好已保存并恢复。
        /// </summary>
        /// <returns>如果保存了移动模式则为true，如果保存了桌面模式则为false，如果没有保存偏好则为null。</returns>
        public bool? GetSavedUIPreference()
        {
            if (!PlayerPrefs.HasKey(_uiModePrefsKey))
            {
                return null;
            }
            return PlayerPrefs.GetInt(_uiModePrefsKey) == 1;
        }
        
        /// <summary>
        /// 清除保存的UI模式偏好。
        /// 用于测试或重置为默认行为。
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
        /// 检查是否保存了UI模式偏好。
        /// </summary>
        /// <returns>如果存在偏好则为true。</returns>
        public bool HasSavedUIPreference()
        {
            return PlayerPrefs.HasKey(_uiModePrefsKey);
        }
        
        /// <summary>
        /// 获取用于UI模式偏好的PlayerPrefs键。
        /// </summary>
        public string UIModePrefKey => _uiModePrefsKey;
        
        #endregion
        
        #region OnScreenControl Management (Requirement 4.4)
        
        /// <summary>
        /// 启用所有OnScreenControl组件（OnScreenStick和OnScreenButtons）。
        /// 需求 4.4: 支持启用/禁用OnScreenControl组件。
        /// 
        /// 属性 3: 控件启用/禁用状态同步
        /// 验证: 需求 4.4
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
        /// 禁用所有OnScreenControl组件（OnScreenStick和OnScreenButtons）。
        /// 需求 4.4: 支持启用/禁用OnScreenControl组件。
        /// 
        /// 属性 3: 控件启用/禁用状态同步
        /// 验证: 需求 4.4
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
        /// 获取所有OnScreenControl组件的启用状态。
        /// 用于基于属性的测试。
        /// </summary>
        /// <returns>(stickEnabled, allButtonsEnabled)的元组</returns>
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
        /// 根据模式确定移动UI的预期可见性状态。
        /// 用于基于属性的测试。
        /// 
        /// 属性 11: UI模式可见性切换
        /// 验证: 需求 5.4, 5.5
        /// </summary>
        /// <param name="isMobileMode">移动模式是否启用</param>
        /// <returns>预期的移动UI可见性</returns>
        public static bool GetExpectedMobileUIVisibility(bool isMobileMode)
        {
            return isMobileMode;
        }
        
        /// <summary>
        /// 根据模式确定桌面UI的预期可见性状态。
        /// 用于基于属性的测试。
        /// 
        /// 属性 11: UI模式可见性切换
        /// 验证: 需求 5.4, 5.5
        /// </summary>
        /// <param name="isMobileMode">移动模式是否启用</param>
        /// <returns>预期的桌面UI可见性</returns>
        public static bool GetExpectedDesktopUIVisibility(bool isMobileMode)
        {
            return !isMobileMode;
        }
        
        /// <summary>
        /// 验证UI可见性状态是否互斥。
        /// 用于基于属性的测试。
        /// 
        /// 属性 11: UI模式可见性切换
        /// </summary>
        /// <param name="mobileUIVisible">移动UI是否可见</param>
        /// <param name="desktopUIVisible">桌面UI是否可见</param>
        /// <returns>如果可见性状态有效（互斥）则为true</returns>
        public static bool ValidateUIVisibilityStates(bool mobileUIVisible, bool desktopUIVisible)
        {
            // Mobile and desktop UI should be mutually exclusive
            return mobileUIVisible != desktopUIVisible;
        }
        
        /// <summary>
        /// 根据触摸支持确定设备是否应该使用移动模式。
        /// 用于基于属性的测试。
        /// 
        /// 需求 5.1, 5.2: 自动检测设备类型。
        /// </summary>
        /// <param name="isTouchDevice">设备是否支持触摸</param>
        /// <returns>是否应该启用移动模式</returns>
        public static bool ShouldUseMobileMode(bool isTouchDevice)
        {
            return isTouchDevice;
        }
        
        /// <summary>
        /// 验证UI模式转换。
        /// 用于基于属性的测试。
        /// </summary>
        /// <param name="previousMode">之前的移动模式状态</param>
        /// <param name="toggleAction">是否调用了切换</param>
        /// <param name="newMode">新的移动模式状态</param>
        /// <returns>如果转换有效则为true</returns>
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
        /// 验证UI偏好持久化是否正常工作。
        /// 用于基于属性的测试。
        /// 
        /// 需求 5.6: UI模式偏好已保存并恢复。
        /// </summary>
        /// <param name="savedMode">保存的模式</param>
        /// <param name="loadedMode">加载的模式</param>
        /// <returns>如果保存和加载的模式匹配则为true</returns>
        public static bool ValidateUIPreferencePersistence(bool savedMode, bool loadedMode)
        {
            return savedMode == loadedMode;
        }
        
        /// <summary>
        /// 验证初始UI模式是否正确确定。
        /// 用于基于属性的测试。
        /// 
        /// 需求 5.6: 在启动时恢复保存的偏好。
        /// </summary>
        /// <param name="hasSavedPreference">是否保存了偏好</param>
        /// <param name="savedPreference">保存的偏好值（如果有）</param>
        /// <param name="isTouchDevice">设备是否支持触摸</param>
        /// <param name="autoDetect">是否启用自动检测</param>
        /// <param name="resultMode">结果UI模式</param>
        /// <returns>如果结果模式正确则为true</returns>
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
        /// 验证OnScreenControl启用状态是否与预期状态匹配。
        /// 用于基于属性的测试。
        /// 
        /// 属性 3: 控件启用/禁用状态同步
        /// 验证: 需求 4.4
        /// </summary>
        /// <param name="expectedEnabled">预期的启用状态</param>
        /// <param name="stickEnabled">实际的OnScreenStick启用状态</param>
        /// <param name="allButtonsEnabled">所有OnScreenButtons是否启用</param>
        /// <returns>如果所有状态都匹配预期则为true</returns>
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
        /// 
        /// 关键原理：OnScreenStick 在被拖动时会创建虚拟 Gamepad 设备。
        /// PlayerInput 需要切换到 Gamepad 方案并绑定正确的设备才能接收输入。
        /// </summary>
        private void EnsureOnScreenControlsWork()
        {
            var playerInput = FindObjectOfType<UnityEngine.InputSystem.PlayerInput>();
            if (playerInput == null)
            {
                Debug.LogWarning("[MobileHUDManager] No PlayerInput found in scene!");
                return;
            }
            
            Debug.Log($"[MobileHUDManager] PlayerInput on: {playerInput.gameObject.name}, scheme: {playerInput.currentControlScheme}");
            
            if (_onScreenStick == null)
            {
                Debug.LogWarning("[MobileHUDManager] OnScreenStick not found!");
                return;
            }
            
            // 关键修复：持续监控并确保 PlayerInput 绑定到正确的虚拟 Gamepad
            StartCoroutine(MonitorAndBindGamepad(playerInput));
        }
        
        /// <summary>
        /// 持续监控 Gamepad 设备，确保 PlayerInput 绑定到 OnScreenStick 创建的虚拟 Gamepad。
        /// </summary>
        private System.Collections.IEnumerator MonitorAndBindGamepad(UnityEngine.InputSystem.PlayerInput playerInput)
        {
            // 等待一帧让 OnScreenStick 初始化
            yield return null;
            
            float checkInterval = 0.5f;
            
            while (true)
            {
                var gamepads = UnityEngine.InputSystem.Gamepad.all;
                
                // 检查是否需要重新绑定
                bool needRebind = playerInput.devices.Count == 0;
                
                if (!needRebind && gamepads.Count > 0)
                {
                    // 检查当前绑定的设备是否在 gamepads 列表中
                    bool foundBoundDevice = false;
                    foreach (var device in playerInput.devices)
                    {
                        foreach (var gp in gamepads)
                        {
                            if (device == gp)
                            {
                                foundBoundDevice = true;
                                break;
                            }
                        }
                        if (foundBoundDevice) break;
                    }
                    needRebind = !foundBoundDevice;
                }
                
                if (needRebind && gamepads.Count > 0)
                {
                    // 使用第一个可用的 Gamepad（OnScreenStick 创建的虚拟设备）
                    var targetGamepad = gamepads[0];
                    
                    try
                    {
                        // 尝试多种控制方案名称
                        string[] schemeNames = { "Gamepad", "Xbox Controller", "XboxController" };
                        bool success = false;
                        
                        foreach (var scheme in schemeNames)
                        {
                            try
                            {
                                playerInput.SwitchCurrentControlScheme(scheme, targetGamepad);
                                Debug.Log($"[MobileHUDManager] ✓ Bound to {targetGamepad.name} with scheme '{scheme}'");
                                success = true;
                                break;
                            }
                            catch
                            {
                                // 尝试下一个方案名称
                            }
                        }
                        
                        if (!success)
                        {
                            // 如果所有方案都失败，尝试直接设置设备
                            Debug.LogWarning("[MobileHUDManager] All scheme names failed, trying direct device assignment...");
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[MobileHUDManager] Bind failed: {e.Message}");
                    }
                }
                
                yield return new WaitForSeconds(checkInterval);
            }
        }
        
        /// <summary>
        /// 强制切换到 Gamepad 控制方案。
        /// 持续尝试直到成功或超时。
        /// </summary>
        private System.Collections.IEnumerator ForceGamepadScheme(UnityEngine.InputSystem.PlayerInput playerInput)
        {
            float timeout = 3f;
            float elapsed = 0f;
            
            while (elapsed < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
                
                var gamepads = UnityEngine.InputSystem.Gamepad.all;
                Debug.Log($"[MobileHUDManager] Checking for Gamepad... count: {gamepads.Count}, elapsed: {elapsed:F1}s");
                
                if (gamepads.Count > 0)
                {
                    try
                    {
                        var virtualGamepad = gamepads[gamepads.Count - 1];
                        playerInput.SwitchCurrentControlScheme("Gamepad", virtualGamepad);
                        Debug.Log($"[MobileHUDManager] ✓ Successfully switched to Gamepad scheme: {virtualGamepad.name}");
                        yield break;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[MobileHUDManager] Switch failed: {e.Message}");
                    }
                }
            }
            
            Debug.LogError("[MobileHUDManager] ✗ Failed to switch to Gamepad scheme after timeout! " +
                          "OnScreenStick may not work. Check if OnScreenStick component is enabled.");
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
            
            // 自动绑定技能按钮视觉效果到技能系统
            if (_groomerController != null)
            {
                BindSkillButtonVisuals(_groomerController);
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
        /// 为测试目的设置引用。
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
        /// 为测试目的设置OnScreenControl引用。
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
        /// 为测试直接设置移动模式。
        /// </summary>
        public void SetMobileModeForTesting(bool isMobileMode)
        {
            _isMobileMode = isMobileMode;
        }
        
        /// <summary>
        /// 获取测试的初始化状态。
        /// </summary>
        public bool IsInitializedForTesting => _isInitialized;
        
        /// <summary>
        /// 为测试强制初始化。
        /// </summary>
        public void InitializeForTesting()
        {
            _isInitialized = false;
            Initialize();
        }
        
        /// <summary>
        /// 为测试重置单例。
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
