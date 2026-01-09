using UnityEngine;
using UnityEngine.UI;
using PetGrooming.Core;
using PetGrooming.Systems;
using PetGrooming.Systems.Skills;
using PetGrooming.AI;
using PetGrooming.UI;

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

        [Header("Groomer Settings")]
        [SerializeField] private Vector3 _groomerSpawnPosition = new Vector3(0f, 0f, 5f);

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

            // Disable ThirdPersonController if it exists (conflicts with our system)
            // var thirdPersonController = groomer.GetComponent("ThirdPersonController") as MonoBehaviour;
            // if (thirdPersonController != null && thirdPersonController.enabled)
            // {
            //     thirdPersonController.enabled = false;
            //     Debug.Log("[Phase2SceneInitializer] Disabled ThirdPersonController to use PlayerMovement instead.");
            // }

            // Ensure PlayerMovement component exists
            PlayerMovement playerMovement = groomer.GetComponent<PlayerMovement>();
            if (playerMovement == null)
            {
                Debug.Log("[Phase2SceneInitializer] Adding PlayerMovement to Groomer...");
                playerMovement = groomer.gameObject.AddComponent<PlayerMovement>();
            }

            // Ensure GroomerSkillManager exists
            GroomerSkillManager skillManager = groomer.GetComponent<GroomerSkillManager>();
            if (skillManager == null)
            {
                Debug.Log("[Phase2SceneInitializer] Adding GroomerSkillManager to Groomer...");
                skillManager = groomer.gameObject.AddComponent<GroomerSkillManager>();
            }

            // Set camera reference for PlayerMovement
            if (Camera.main != null)
            {
                playerMovement.SetCameraTransform(Camera.main.transform);
            }

            Debug.Log("[Phase2SceneInitializer] Groomer setup complete.");
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
