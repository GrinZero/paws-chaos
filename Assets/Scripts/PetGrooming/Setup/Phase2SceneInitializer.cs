using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using PetGrooming.Core;
using PetGrooming.Systems;
using PetGrooming.Systems.Skills;
using PetGrooming.AI;
using PetGrooming.UI;
using PetGrooming.UI.MobileUI;
using StarterAssets;

namespace PetGrooming.Setup
{
    /// <summary>
    /// Runtime initializer for Phase 2 scene.
    /// Automatically sets up missing components and references when the game starts.
    /// Attach this to a GameObject in your scene for automatic setup.
    /// </summary>
    public class Phase2SceneInitializer : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private Phase2GameConfig _phase2Config;
        [SerializeField] private GameConfig _gameConfig;

        [Header("Auto Setup Options")]
        [SerializeField] private bool _autoSetupGroomer = true;
        [SerializeField] private bool _autoSetupCamera = true;
        [SerializeField] private bool _autoSetupGameSystems = true;
        [SerializeField] private bool _autoSetupUI = true;
        [SerializeField] private bool _autoSetupMobileHUD = true;

        [Header("Groomer Settings")]
        [SerializeField] private Vector3 _groomerSpawnPosition = new Vector3(0f, 0f, 5f);

        [Header("Mobile HUD Settings")]
        [Tooltip("Mobile HUD prefab to instantiate")]
        [SerializeField] private GameObject _mobileHUDPrefab;
        
        [Tooltip("Mobile HUD settings asset")]
        [SerializeField] private MobileHUDSettings _mobileHUDSettings;

        private void Awake()
        {
            // Run early initialization
            EarlyInitialize();
        }

        private void Start()
        {
            // Run late initialization to ensure all objects are ready
            LateInitialize();
        }

        private void EarlyInitialize()
        {
            Debug.Log("[Phase2SceneInitializer] Starting early initialization...");

            if (_autoSetupGroomer)
            {
                SetupGroomer();
            }
        }

        private void LateInitialize()
        {
            Debug.Log("[Phase2SceneInitializer] Starting late initialization...");

            if (_autoSetupCamera)
            {
                SetupCamera();
            }

            if (_autoSetupGameSystems)
            {
                SetupGameSystems();
            }

            if (_autoSetupUI)
            {
                SetupUI();
            }

            if (_autoSetupMobileHUD)
            {
                SetupMobileHUD();
            }

            // Start the game automatically
            StartGame();

            Debug.Log("[Phase2SceneInitializer] Initialization complete!");
        }

        private void StartGame()
        {
            // Auto-start the game so controls work
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartMatch();
                Debug.Log("[Phase2SceneInitializer] Game started automatically.");
            }
        }

        public void Initialize()
        {
            Debug.Log("[Phase2SceneInitializer] Starting scene initialization...");

            if (_autoSetupGroomer)
            {
                SetupGroomer();
            }

            if (_autoSetupCamera)
            {
                SetupCamera();
            }

            if (_autoSetupGameSystems)
            {
                SetupGameSystems();
            }

            Debug.Log("[Phase2SceneInitializer] Scene initialization complete!");
        }

        private void SetupGroomer()
        {
            // Find existing groomer or create one
            GroomerController groomer = FindObjectOfType<GroomerController>();
            
            if (groomer == null)
            {
                Debug.Log("[Phase2SceneInitializer] Creating Groomer...");
                groomer = CreateGroomer();
            }

            // 迁移后架构：使用 ThirdPersonController 作为唯一的移动控制器
            // Requirement 5.1, 5.2, 5.3: 确保玩家有 ThirdPersonController、PlayerInput、StarterAssetsInputs 组件
            
            // 确保 ThirdPersonController 存在并启用
            var thirdPersonController = groomer.GetComponent<StarterAssets.ThirdPersonController>();
            if (thirdPersonController != null)
            {
                thirdPersonController.enabled = true;
                Debug.Log("[Phase2SceneInitializer] ThirdPersonController enabled as sole movement controller.");
            }
            else
            {
                Debug.LogWarning("[Phase2SceneInitializer] ThirdPersonController not found on Groomer!");
            }
            
            // 确保 PlayerInput 组件存在
            var playerInput = groomer.GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if (playerInput != null)
            {
                Debug.Log("[Phase2SceneInitializer] PlayerInput component found.");
            }
            else
            {
                Debug.LogWarning("[Phase2SceneInitializer] PlayerInput component not found on Groomer!");
            }
            
            // 确保 StarterAssetsInputs 组件存在
            var starterAssetsInputs = groomer.GetComponent<StarterAssets.StarterAssetsInputs>();
            if (starterAssetsInputs != null)
            {
                Debug.Log("[Phase2SceneInitializer] StarterAssetsInputs component found.");
                
                // 连接技能输入事件到 GroomerSkillManager
                // Requirement 3.3: 连接技能输入事件到技能管理器
                GroomerSkillManager skillManager = groomer.GetComponent<GroomerSkillManager>();
                if (skillManager != null)
                {
                    ConnectSkillInputEvents(starterAssetsInputs, skillManager, groomer);
                }
            }
            else
            {
                Debug.LogWarning("[Phase2SceneInitializer] StarterAssetsInputs component not found on Groomer!");
            }

            // PlayerMovement 已废弃，不再添加
            // 输入现在由 OnScreenStick 通过 Input System 直接发送到 StarterAssetsInputs
            PlayerMovement playerMovement = groomer.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                // 标记为废弃但不删除，保持向后兼容
                playerMovement.enabled = false;
                Debug.Log("[Phase2SceneInitializer] PlayerMovement disabled (deprecated, using ThirdPersonController).");
            }

            // Ensure GroomerSkillManager exists
            GroomerSkillManager existingSkillManager = groomer.GetComponent<GroomerSkillManager>();
            if (existingSkillManager == null)
            {
                Debug.Log("[Phase2SceneInitializer] Adding GroomerSkillManager to Groomer...");
                existingSkillManager = groomer.gameObject.AddComponent<GroomerSkillManager>();
            }

            Debug.Log("[Phase2SceneInitializer] Groomer setup complete.");
        }
        
        /// <summary>
        /// 连接 StarterAssetsInputs 的技能输入事件到 GroomerSkillManager。
        /// Requirement 3.3: 连接技能输入事件到技能管理器。
        /// Requirement 6.6: StarterAssetsInputs 扩展支持技能输入。
        /// </summary>
        private void ConnectSkillInputEvents(
            StarterAssets.StarterAssetsInputs inputs, 
            GroomerSkillManager skillManager,
            GroomerController groomer)
        {
            // 连接技能1事件 (CaptureNet)
            inputs.OnSkill1Pressed += () => skillManager.TryActivateSkill(0);
            
            // 连接技能2事件 (Leash)
            inputs.OnSkill2Pressed += () => skillManager.TryActivateSkill(1);
            
            // 连接技能3事件 (CalmingSpray)
            inputs.OnSkill3Pressed += () => skillManager.TryActivateSkill(2);
            
            // 连接捕获事件
            inputs.OnCapturePressed += () => groomer.TryCapturePet();
            
            // 连接挣扎事件（用于宠物逃脱，暂时不处理）
            // inputs.OnStrugglePressed += () => { };
            
            Debug.Log("[Phase2SceneInitializer] Skill input events connected to GroomerSkillManager.");
        }

        private GroomerController CreateGroomer()
        {
            GameObject groomerObj = new GameObject("Groomer");
            groomerObj.transform.position = _groomerSpawnPosition;
            groomerObj.tag = "Player";

            // Add CharacterController
            CharacterController charController = groomerObj.AddComponent<CharacterController>();
            charController.center = new Vector3(0f, 1f, 0f);
            charController.radius = 0.5f;
            charController.height = 2f;

            // Add visual representation
            CreateGroomerVisual(groomerObj.transform);

            // Add GroomerController
            GroomerController controller = groomerObj.AddComponent<GroomerController>();

            return controller;
        }

        private void CreateGroomerVisual(Transform parent)
        {
            // Simple capsule body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(parent);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);
            body.transform.localScale = new Vector3(0.6f, 0.8f, 0.4f);
            Destroy(body.GetComponent<Collider>());

            // Head
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(parent);
            head.transform.localPosition = new Vector3(0f, 1.9f, 0f);
            head.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            Destroy(head.GetComponent<Collider>());

            // Set color
            Renderer bodyRenderer = body.GetComponent<Renderer>();
            Renderer headRenderer = head.GetComponent<Renderer>();
            if (bodyRenderer != null) bodyRenderer.material.color = new Color(0.2f, 0.6f, 0.9f);
            if (headRenderer != null) headRenderer.material.color = new Color(0.9f, 0.7f, 0.6f);
        }

        private void SetupCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("[Phase2SceneInitializer] No main camera found!");
                return;
            }

            // Find the player/groomer
            GroomerController groomer = FindObjectOfType<GroomerController>();
            if (groomer == null)
            {
                Debug.LogWarning("[Phase2SceneInitializer] No GroomerController found for camera target!");
                return;
            }

            // Check if ThirdPersonController exists and needs camera target setup
            var thirdPersonController = groomer.GetComponent("ThirdPersonController");
            if (thirdPersonController != null)
            {
                // Find or create PlayerCameraRoot
                Transform cameraRoot = groomer.transform.Find("PlayerCameraRoot");
                if (cameraRoot == null)
                {
                    GameObject rootObj = new GameObject("PlayerCameraRoot");
                    rootObj.transform.SetParent(groomer.transform);
                    rootObj.transform.localPosition = new Vector3(0f, 1.5f, 0f);
                    cameraRoot = rootObj.transform;
                    Debug.Log("[Phase2SceneInitializer] Created PlayerCameraRoot");
                }

                // Set the camera target via reflection
                var field = thirdPersonController.GetType().GetField("CinemachineCameraTarget", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(thirdPersonController, cameraRoot.gameObject);
                    Debug.Log("[Phase2SceneInitializer] Set CinemachineCameraTarget on ThirdPersonController");
                }
            }

            // Ensure CameraController exists for our custom camera follow
            CameraController cameraController = mainCamera.GetComponent<CameraController>();
            if (cameraController == null)
            {
                Debug.Log("[Phase2SceneInitializer] Adding CameraController to Main Camera...");
                cameraController = mainCamera.gameObject.AddComponent<CameraController>();
            }

            // Set target
            cameraController.Target = groomer.transform;
            
            // Position camera immediately
            Vector3 offset = cameraController.DefaultOffset;
            mainCamera.transform.position = groomer.transform.position + offset;
            mainCamera.transform.LookAt(groomer.transform.position + Vector3.up * 1.5f);
            
            Debug.Log($"[Phase2SceneInitializer] Camera positioned at offset {offset}");

            // Set scene bounds
            cameraController.SetSceneBounds(new Bounds(Vector3.zero, new Vector3(50f, 30f, 50f)));

            Debug.Log("[Phase2SceneInitializer] Camera setup complete.");
        }

        private void SetupGameSystems()
        {
            // GameManager
            if (GameManager.Instance == null && FindObjectOfType<GameManager>() == null)
            {
                Debug.Log("[Phase2SceneInitializer] Creating GameManager...");
                GameObject gmObj = new GameObject("GameManager");
                gmObj.AddComponent<GameManager>();
            }

            // PetSpawnManager
            if (FindObjectOfType<PetSpawnManager>() == null)
            {
                Debug.Log("[Phase2SceneInitializer] Creating PetSpawnManager...");
                GameObject spawnObj = new GameObject("PetSpawnManager");
                spawnObj.AddComponent<PetSpawnManager>();
            }

            // AlertSystem
            if (AlertSystem.Instance == null && FindObjectOfType<AlertSystem>() == null)
            {
                Debug.Log("[Phase2SceneInitializer] Creating AlertSystem...");
                GameObject alertObj = new GameObject("AlertSystem");
                alertObj.AddComponent<AlertSystem>();
            }

            Debug.Log("[Phase2SceneInitializer] Game systems setup complete.");
        }

        private void SetupUI()
        {
            // Find or create EventSystem (required for UI input)
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                Debug.Log("[Phase2SceneInitializer] Creating EventSystem...");
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                // Use InputSystemUIInputModule for new Input System compatibility
                eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
            
            // Find or create Canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.Log("[Phase2SceneInitializer] Creating UI Canvas...");
                GameObject canvasObj = new GameObject("GameCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // Create Skill Bar UI
            SkillBarUI skillBar = FindObjectOfType<SkillBarUI>();
            if (skillBar == null)
            {
                Debug.Log("[Phase2SceneInitializer] Creating Skill Bar UI...");
                CreateSkillBarUI(canvas.transform);
            }

            // Create GameHUD if not exists
            GameHUD gameHUD = FindObjectOfType<GameHUD>();
            if (gameHUD == null)
            {
                Debug.Log("[Phase2SceneInitializer] Creating GameHUD...");
                canvas.gameObject.AddComponent<GameHUD>();
            }

            Debug.Log("[Phase2SceneInitializer] UI setup complete.");
        }

        /// <summary>
        /// Sets up the Mobile HUD for touch-enabled devices.
        /// Requirement 5.1: Enable Mobile_HUD on touch device.
        /// Requirement 1.8: Connect joystick input to character movement.
        /// </summary>
        private void SetupMobileHUD()
        {
            // Check if MobileHUDManager already exists
            MobileHUDManager existingManager = FindObjectOfType<MobileHUDManager>();
            if (existingManager != null)
            {
                Debug.Log("[Phase2SceneInitializer] MobileHUDManager already exists, connecting references...");
                ConnectMobileHUDReferences(existingManager);
                ConfigurePlayerInputForOnScreenControls();
                return;
            }

            // Find or create Canvas for Mobile HUD
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[Phase2SceneInitializer] No Canvas found for Mobile HUD!");
                return;
            }

            // Try to instantiate from prefab
            if (_mobileHUDPrefab != null)
            {
                Debug.Log("[Phase2SceneInitializer] Instantiating MobileHUD from prefab...");
                // MobileHUD prefab has its own Canvas, so instantiate at root level
                GameObject mobileHUDObj = Instantiate(_mobileHUDPrefab);
                mobileHUDObj.name = "MobileHUD";
                
                // 关键：激活 MobileHUD GameObject（prefab 默认是禁用的）
                mobileHUDObj.SetActive(true);
                
                MobileHUDManager manager = mobileHUDObj.GetComponent<MobileHUDManager>();
                if (manager != null)
                {
                    ConnectMobileHUDReferences(manager);
                    
                    // Apply settings if available
                    if (_mobileHUDSettings != null)
                    {
                        manager.SetSettings(_mobileHUDSettings);
                    }
                }
                
                // 配置 PlayerInput 以接收 OnScreenControl 的虚拟 Gamepad 输入
                ConfigurePlayerInputForOnScreenControls();
            }
            else
            {
                // Create MobileHUD programmatically
                Debug.Log("[Phase2SceneInitializer] Creating MobileHUD programmatically...");
                CreateMobileHUDProgrammatically(canvas.transform);
            }

            Debug.Log("[Phase2SceneInitializer] Mobile HUD setup complete.");
        }
        
        /// <summary>
        /// 配置 PlayerInput 组件以正确接收 OnScreenControl 的虚拟 Gamepad 输入。
        /// OnScreenStick 和 OnScreenButton 通过模拟 Gamepad 输入工作，
        /// 需要确保 PlayerInput 能够接收这些输入。
        /// </summary>
        private void ConfigurePlayerInputForOnScreenControls()
        {
            // 查找 Groomer 上的 PlayerInput 组件
            GroomerController groomer = FindObjectOfType<GroomerController>();
            if (groomer == null)
            {
                Debug.LogWarning("[Phase2SceneInitializer] No GroomerController found for PlayerInput configuration!");
                return;
            }
            
            var playerInput = groomer.GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if (playerInput == null)
            {
                Debug.LogWarning("[Phase2SceneInitializer] No PlayerInput component found on Groomer!");
                return;
            }
            
            // 关键修复：确保 PlayerInput 使用 "Invoke Unity Events" 或 "Send Messages" 行为
            // 并且能够接收来自虚拟 Gamepad 的输入
            // OnScreenControl 组件会创建一个虚拟 Gamepad 设备，PlayerInput 需要能够接收它
            
            // 检查当前的通知行为
            var behavior = playerInput.notificationBehavior;
            Debug.Log($"[Phase2SceneInitializer] PlayerInput notification behavior: {behavior}");
            
            // 如果使用 SendMessages，确保 StarterAssetsInputs 在同一个 GameObject 上
            var starterInputs = groomer.GetComponent<StarterAssets.StarterAssetsInputs>();
            if (starterInputs == null)
            {
                Debug.LogWarning("[Phase2SceneInitializer] StarterAssetsInputs not found on Groomer!");
                return;
            }
            
            // 强制刷新设备配对，确保虚拟 Gamepad 被识别
            // 这是解决 OnScreenControl 不工作的关键步骤
            try
            {
                // 获取所有 Gamepad 设备（包括虚拟的）
                var gamepads = UnityEngine.InputSystem.Gamepad.all;
                Debug.Log($"[Phase2SceneInitializer] Found {gamepads.Count} Gamepad devices (including virtual).");
                
                // 如果 PlayerInput 没有配对设备，尝试自动配对
                if (playerInput.devices.Count == 0 || !HasGamepadDevice(playerInput))
                {
                    // 切换到自动控制方案切换，这样可以自动检测虚拟 Gamepad
                    playerInput.SwitchCurrentControlScheme("Gamepad");
                    Debug.Log("[Phase2SceneInitializer] Switched PlayerInput to Gamepad control scheme for OnScreenControls.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Phase2SceneInitializer] Failed to configure PlayerInput for OnScreenControls: {e.Message}");
            }
            
            Debug.Log("[Phase2SceneInitializer] PlayerInput configured for OnScreenControl virtual Gamepad input.");
        }
        
        /// <summary>
        /// 检查 PlayerInput 是否已配对 Gamepad 设备。
        /// </summary>
        private bool HasGamepadDevice(UnityEngine.InputSystem.PlayerInput playerInput)
        {
            foreach (var device in playerInput.devices)
            {
                if (device is UnityEngine.InputSystem.Gamepad)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Connects MobileHUDManager to Groomer controller and movement components.
        /// Requirement 1.8: Joystick input equivalent to keyboard/gamepad.
        /// Requirement 5.4: 配置 OnScreenStick 和 OnScreenButton，绑定 SkillButtonVisual 到技能系统。
        /// </summary>
        private void ConnectMobileHUDReferences(MobileHUDManager manager)
        {
            // Find Groomer
            GroomerController groomer = FindObjectOfType<GroomerController>();
            if (groomer != null)
            {
                // Set groomer controller reference
                manager.SetGroomerController(groomer);
                
                // Subscribe groomer to capture button events
                // Requirement 2.4: Capture button triggers capture attempt
                if (manager.SkillWheel != null)
                {
                    manager.SkillWheel.OnCapturePressed += groomer.OnCaptureButtonPressed;
                    groomer.EnableMobileInput();
                    Debug.Log("[Phase2SceneInitializer] Bound GroomerController to MobileHUD capture button.");
                }
                
                // 输入现在由 OnScreenStick 通过 Input System 直接发送到 StarterAssetsInputs
                // 不再需要 PlayerMovement 引用
                
                // Bind skill wheel to groomer skills
                GroomerSkillManager skillManager = groomer.GetComponent<GroomerSkillManager>();
                if (skillManager != null)
                {
                    // 绑定 SkillWheel（旧版 UI）
                    if (manager.SkillWheel != null)
                    {
                        manager.SkillWheel.BindToGroomerSkills(skillManager);
                        Debug.Log("[Phase2SceneInitializer] Bound SkillWheel to GroomerSkillManager.");
                    }
                    
                    // 绑定 SkillButtonVisual 组件到技能系统
                    // Requirement 5.4: 绑定 SkillButtonVisual 到技能系统
                    BindSkillButtonVisualsToSkills(manager, skillManager);
                }
            }
            else
            {
                Debug.LogWarning("[Phase2SceneInitializer] No GroomerController found for MobileHUD connection!");
            }

            // Find desktop UI (skill bar) to toggle visibility
            SkillBarUI skillBar = FindObjectOfType<SkillBarUI>();
            if (skillBar != null)
            {
                // The MobileHUDManager will handle visibility toggling
                Debug.Log("[Phase2SceneInitializer] Found SkillBarUI for desktop/mobile toggle.");
            }
        }
        
        /// <summary>
        /// 绑定 SkillButtonVisual 组件到 GroomerSkillManager 的技能。
        /// Requirement 5.4: 绑定 SkillButtonVisual 到技能系统。
        /// Requirement 7.1, 7.2, 7.5: 技能按钮显示冷却和就绪动画。
        /// </summary>
        private void BindSkillButtonVisualsToSkills(MobileHUDManager manager, GroomerSkillManager skillManager)
        {
            var skillButtonVisuals = manager.SkillButtonVisuals;
            if (skillButtonVisuals == null || skillButtonVisuals.Length == 0)
            {
                Debug.LogWarning("[Phase2SceneInitializer] No SkillButtonVisual components found in MobileHUD.");
                return;
            }
            
            // 绑定每个 SkillButtonVisual 到对应的技能
            // 按钮顺序：Skill1 (CaptureNet), Skill2 (Leash), Skill3 (CalmingSpray)
            for (int i = 0; i < skillButtonVisuals.Length && i < skillManager.SkillCount; i++)
            {
                var visual = skillButtonVisuals[i];
                if (visual != null)
                {
                    var skill = skillManager.GetSkill(i);
                    if (skill != null)
                    {
                        visual.BindToSkill(skill);
                        Debug.Log($"[Phase2SceneInitializer] Bound SkillButtonVisual[{i}] to {skill.SkillName}.");
                    }
                }
            }
            
            Debug.Log("[Phase2SceneInitializer] SkillButtonVisual components bound to skills.");
        }

        /// <summary>
        /// Creates MobileHUD components programmatically when no prefab is available.
        /// </summary>
        private void CreateMobileHUDProgrammatically(Transform canvasTransform)
        {
            // Create MobileHUD container
            GameObject mobileHUDObj = new GameObject("MobileHUD");
            mobileHUDObj.transform.SetParent(canvasTransform, false);
            
            RectTransform hudRect = mobileHUDObj.AddComponent<RectTransform>();
            hudRect.anchorMin = Vector2.zero;
            hudRect.anchorMax = Vector2.one;
            hudRect.sizeDelta = Vector2.zero;
            hudRect.anchoredPosition = Vector2.zero;
            
            // Add MobileHUDManager
            MobileHUDManager manager = mobileHUDObj.AddComponent<MobileHUDManager>();
            
            // Apply settings if available
            if (_mobileHUDSettings != null)
            {
                manager.SetSettings(_mobileHUDSettings);
            }
            
            // Connect references
            ConnectMobileHUDReferences(manager);
            
            Debug.Log("[Phase2SceneInitializer] Created MobileHUD programmatically.");
        }

        private void CreateSkillBarUI(Transform canvasTransform)
        {
            // Create skill bar container
            GameObject skillBarObj = new GameObject("SkillBar");
            skillBarObj.transform.SetParent(canvasTransform, false);
            
            RectTransform skillBarRect = skillBarObj.AddComponent<RectTransform>();
            skillBarRect.anchorMin = new Vector2(0.5f, 0f);
            skillBarRect.anchorMax = new Vector2(0.5f, 0f);
            skillBarRect.pivot = new Vector2(0.5f, 0f);
            skillBarRect.anchoredPosition = new Vector2(0f, 50f);
            skillBarRect.sizeDelta = new Vector2(250f, 80f);

            SkillBarUI skillBar = skillBarObj.AddComponent<SkillBarUI>();

            // Create 3 skill slots
            SkillCooldownUI[] slots = new SkillCooldownUI[3];
            string[] skillNames = { "捕宠网", "牵引绳", "镇静喷雾" };
            string[] keyLabels = { "1", "2", "3" };

            for (int i = 0; i < 3; i++)
            {
                slots[i] = CreateSkillSlot(skillBarObj.transform, i, skillNames[i], keyLabels[i]);
            }

            // Configure skill bar
#if UNITY_EDITOR
            var serializedObj = new UnityEditor.SerializedObject(skillBar);
            var slotsProperty = serializedObj.FindProperty("_skillSlots");
            if (slotsProperty != null)
            {
                slotsProperty.arraySize = 3;
                for (int i = 0; i < 3; i++)
                {
                    slotsProperty.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
                }
                serializedObj.ApplyModifiedProperties();
            }
#endif

            Debug.Log("[Phase2SceneInitializer] Skill Bar UI created with 3 skill slots.");
        }

        private SkillCooldownUI CreateSkillSlot(Transform parent, int index, string skillName, string keyLabel)
        {
            // Create slot container
            GameObject slotObj = new GameObject($"SkillSlot_{index}");
            slotObj.transform.SetParent(parent, false);

            RectTransform slotRect = slotObj.AddComponent<RectTransform>();
            float xPos = -80f + (index * 80f); // Space slots horizontally
            slotRect.anchoredPosition = new Vector2(xPos, 0f);
            slotRect.sizeDelta = new Vector2(64f, 64f);

            // Create background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(slotObj.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Create skill icon
            GameObject iconObj = new GameObject("SkillIcon");
            iconObj.transform.SetParent(slotObj.transform, false);
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.1f, 0.1f);
            iconRect.anchorMax = new Vector2(0.9f, 0.9f);
            iconRect.sizeDelta = Vector2.zero;
            Image skillIcon = iconObj.AddComponent<Image>();
            skillIcon.color = Color.white;

            // Create cooldown overlay
            GameObject overlayObj = new GameObject("CooldownOverlay");
            overlayObj.transform.SetParent(slotObj.transform, false);
            RectTransform overlayRect = overlayObj.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;
            Image cooldownOverlay = overlayObj.AddComponent<Image>();
            cooldownOverlay.color = new Color(0f, 0f, 0f, 0.7f);
            cooldownOverlay.type = Image.Type.Filled;
            cooldownOverlay.fillMethod = Image.FillMethod.Radial360;
            cooldownOverlay.fillOrigin = (int)Image.Origin360.Top;
            cooldownOverlay.fillClockwise = false;
            overlayObj.SetActive(false);

            // Create cooldown text
            GameObject textObj = new GameObject("CooldownText");
            textObj.transform.SetParent(slotObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            // Use legacy Text instead of TMP to avoid dependency issues
            Text cooldownText = textObj.AddComponent<Text>();
            cooldownText.text = "";
            cooldownText.alignment = TextAnchor.MiddleCenter;
            cooldownText.fontSize = 20;
            cooldownText.fontStyle = FontStyle.Bold;
            cooldownText.color = Color.white;
            textObj.SetActive(false);

            // Create key label
            GameObject keyObj = new GameObject("KeyLabel");
            keyObj.transform.SetParent(slotObj.transform, false);
            RectTransform keyRect = keyObj.AddComponent<RectTransform>();
            keyRect.anchorMin = new Vector2(0.5f, 0f);
            keyRect.anchorMax = new Vector2(0.5f, 0f);
            keyRect.pivot = new Vector2(0.5f, 1f);
            keyRect.anchoredPosition = new Vector2(0f, -5f);
            keyRect.sizeDelta = new Vector2(30f, 20f);
            
            Text keyText = keyObj.AddComponent<Text>();
            keyText.text = keyLabel;
            keyText.alignment = TextAnchor.MiddleCenter;
            keyText.fontSize = 14;
            keyText.color = Color.yellow;

            // Add SkillCooldownUI component
            SkillCooldownUI skillCooldown = slotObj.AddComponent<SkillCooldownUI>();

#if UNITY_EDITOR
            var serializedObj = new UnityEditor.SerializedObject(skillCooldown);
            
            var iconProp = serializedObj.FindProperty("_skillIcon");
            if (iconProp != null) iconProp.objectReferenceValue = skillIcon;
            
            var overlayProp = serializedObj.FindProperty("_cooldownOverlay");
            if (overlayProp != null) overlayProp.objectReferenceValue = cooldownOverlay;
            
            serializedObj.ApplyModifiedProperties();
#endif

            return skillCooldown;
        }

        [ContextMenu("Re-Initialize Scene")]
        public void ReInitialize()
        {
            Initialize();
        }
    }
}
