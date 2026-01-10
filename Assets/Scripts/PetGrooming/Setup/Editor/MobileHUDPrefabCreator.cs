using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using PetGrooming.UI.MobileUI;
using PetGrooming.Core;

namespace PetGrooming.Setup.Editor
{
    /// <summary>
    /// Editor utility to create the MobileHUD prefab.
    /// Assembles all mobile UI components (joystick, skill wheel, struggle button) into a single prefab.
    /// Requirements: 5.1
    /// </summary>
    public static class MobileHUDPrefabCreator
    {
        private const string PrefabPath = "Assets/UI/MobileUI/MobileHUD.prefab";
        private const string FolderPath = "Assets/UI/MobileUI";
        
        // Paths to existing prefabs
        private const string JoystickPrefabPath = "Assets/UI/MobileUI/VirtualJoystick.prefab";
        private const string SkillWheelPrefabPath = "Assets/UI/MobileUI/SkillWheel.prefab";
        private const string StruggleButtonPrefabPath = "Assets/UI/MobileUI/StruggleButton.prefab";
        
        [MenuItem("PetGrooming/Create Mobile UI/Mobile HUD Prefab")]
        public static void CreateMobileHUDPrefab()
        {
            // Ensure folder exists
            EnsureFolderExists();
            
            // Create Canvas root
            GameObject canvasRoot = CreateCanvas();
            
            // Add MobileHUDManager component
            MobileHUDManager hudManager = canvasRoot.AddComponent<MobileHUDManager>();
            
            // Create or instantiate joystick
            GameObject joystick = CreateOrInstantiateJoystick(canvasRoot.transform);
            VirtualJoystick joystickComponent = joystick.GetComponent<VirtualJoystick>();
            
            // Create or instantiate skill wheel
            GameObject skillWheel = CreateOrInstantiateSkillWheel(canvasRoot.transform);
            SkillWheelUI skillWheelComponent = skillWheel.GetComponent<SkillWheelUI>();
            
            // Create or instantiate struggle button
            GameObject struggleButton = CreateOrInstantiateStruggleButton(canvasRoot.transform);
            StruggleButtonUI struggleButtonComponent = struggleButton.GetComponent<StruggleButtonUI>();
            
            // Set references on MobileHUDManager
            SerializedObject serializedHUD = new SerializedObject(hudManager);
            serializedHUD.FindProperty("_joystick").objectReferenceValue = joystickComponent;
            serializedHUD.FindProperty("_skillWheel").objectReferenceValue = skillWheelComponent;
            serializedHUD.FindProperty("_struggleButton").objectReferenceValue = struggleButtonComponent;
            serializedHUD.FindProperty("_autoDetectDevice").boolValue = true;
            serializedHUD.FindProperty("_uiModePrefsKey").stringValue = "MobileUIMode";
            serializedHUD.ApplyModifiedPropertiesWithoutUndo();
            
            // Save as prefab
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(canvasRoot, PrefabPath);
            
            // Clean up scene object
            Object.DestroyImmediate(canvasRoot);
            
            // Select the created prefab
            Selection.activeObject = prefab;
            
            Debug.Log($"MobileHUD prefab created at: {PrefabPath}");
        }
        
        private static void EnsureFolderExists()
        {
            if (!AssetDatabase.IsValidFolder(FolderPath))
            {
                if (!AssetDatabase.IsValidFolder("Assets/UI"))
                {
                    AssetDatabase.CreateFolder("Assets", "UI");
                }
                AssetDatabase.CreateFolder("Assets/UI", "MobileUI");
            }
        }
        
        private static GameObject CreateCanvas()
        {
            GameObject canvasObj = new GameObject("MobileHUD");
            
            // Add Canvas component
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // Ensure mobile UI is on top
            
            // Add CanvasScaler for responsive UI
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            
            // Add GraphicRaycaster for touch input
            canvasObj.AddComponent<GraphicRaycaster>();
            
            return canvasObj;
        }
        
        private static GameObject CreateOrInstantiateJoystick(Transform parent)
        {
            // Try to load existing prefab
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(JoystickPrefabPath);
            
            GameObject joystick;
            if (prefab != null)
            {
                joystick = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                PrefabUtility.UnpackPrefabInstance(joystick, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }
            else
            {
                // Create inline if prefab doesn't exist
                joystick = CreateJoystickInline(parent);
            }
            
            // Position in bottom-left
            RectTransform rect = joystick.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0, 0);
            rect.pivot = new Vector2(0, 0);
            rect.anchoredPosition = new Vector2(100, 100);
            
            return joystick;
        }
        
        private static GameObject CreateOrInstantiateSkillWheel(Transform parent)
        {
            // Try to load existing prefab
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SkillWheelPrefabPath);
            
            GameObject skillWheel;
            if (prefab != null)
            {
                skillWheel = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                PrefabUtility.UnpackPrefabInstance(skillWheel, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }
            else
            {
                // Create inline if prefab doesn't exist
                skillWheel = CreateSkillWheelInline(parent);
            }
            
            // Position in bottom-right
            RectTransform rect = skillWheel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(1, 0);
            rect.anchoredPosition = new Vector2(-100, 100);
            
            return skillWheel;
        }
        
        private static GameObject CreateOrInstantiateStruggleButton(Transform parent)
        {
            // Try to load existing prefab
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(StruggleButtonPrefabPath);
            
            GameObject struggleButton;
            if (prefab != null)
            {
                struggleButton = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                PrefabUtility.UnpackPrefabInstance(struggleButton, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }
            else
            {
                // Create inline if prefab doesn't exist
                struggleButton = CreateStruggleButtonInline(parent);
            }
            
            // Position in center-right
            RectTransform rect = struggleButton.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 0.5f);
            rect.anchorMax = new Vector2(1, 0.5f);
            rect.pivot = new Vector2(1, 0.5f);
            rect.anchoredPosition = new Vector2(-150, 0);
            
            // Start hidden (shown when pet is captured)
            struggleButton.SetActive(false);
            
            return struggleButton;
        }
        
        #region Inline Creation Methods
        
        private static GameObject CreateJoystickInline(Transform parent)
        {
            GameObject root = new GameObject("VirtualJoystick");
            root.transform.SetParent(parent, false);
            
            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(180, 180);
            
            VirtualJoystick joystick = root.AddComponent<VirtualJoystick>();
            
            // Create Background
            GameObject background = new GameObject("Background");
            background.transform.SetParent(root.transform, false);
            RectTransform bgRect = background.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.5f, 0.5f);
            bgRect.anchorMax = new Vector2(0.5f, 0.5f);
            bgRect.sizeDelta = new Vector2(180, 180);
            background.AddComponent<CanvasRenderer>();
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            
            // Create Handle
            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(background.transform, false);
            RectTransform handleRect = handle.AddComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(70, 70);
            handle.AddComponent<CanvasRenderer>();
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = new Color(1f, 1f, 1f, 0.6f);
            handleImage.raycastTarget = false;
            
            // Set references
            SerializedObject serialized = new SerializedObject(joystick);
            serialized.FindProperty("_background").objectReferenceValue = bgRect;
            serialized.FindProperty("_handle").objectReferenceValue = handleRect;
            serialized.FindProperty("_handleImage").objectReferenceValue = handleImage;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            
            return root;
        }
        
        private static GameObject CreateSkillWheelInline(Transform parent)
        {
            GameObject root = new GameObject("SkillWheel");
            root.transform.SetParent(parent, false);
            
            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(300, 300);
            
            SkillWheelUI skillWheel = root.AddComponent<SkillWheelUI>();
            
            // Create Capture Button (largest, at center-bottom)
            GameObject captureBtn = CreateSkillButtonInline(root.transform, "CaptureButton", 140f);
            RectTransform captureRect = captureBtn.GetComponent<RectTransform>();
            captureRect.anchoredPosition = Vector2.zero;
            MobileSkillButton captureComponent = captureBtn.GetComponent<MobileSkillButton>();
            
            // Create 3 skill buttons in arc
            MobileSkillButton[] skillButtons = new MobileSkillButton[3];
            string[] skillNames = { "SkillButton_CaptureNet", "SkillButton_Leash", "SkillButton_CalmingSpray" };
            
            for (int i = 0; i < 3; i++)
            {
                GameObject skillBtn = CreateSkillButtonInline(root.transform, skillNames[i], 100f);
                skillButtons[i] = skillBtn.GetComponent<MobileSkillButton>();
                
                // Position in arc (will be adjusted by SkillWheelUI)
                float angle = 135f + (i * 45f); // Arc from 135 to 225 degrees
                float radius = 150f;
                float x = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
                float y = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;
                
                RectTransform btnRect = skillBtn.GetComponent<RectTransform>();
                btnRect.anchoredPosition = new Vector2(x, y);
            }
            
            // Set references
            SerializedObject serialized = new SerializedObject(skillWheel);
            serialized.FindProperty("_captureButton").objectReferenceValue = captureComponent;
            
            SerializedProperty skillButtonsArray = serialized.FindProperty("_skillButtons");
            skillButtonsArray.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                skillButtonsArray.GetArrayElementAtIndex(i).objectReferenceValue = skillButtons[i];
            }
            
            serialized.FindProperty("_arcRadius").floatValue = 150f;
            serialized.FindProperty("_arcStartAngle").floatValue = 135f;
            serialized.FindProperty("_arcSpan").floatValue = 90f;
            serialized.FindProperty("_minButtonSpacing").floatValue = 20f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            
            return root;
        }
        
        private static GameObject CreateSkillButtonInline(Transform parent, string name, float size)
        {
            GameObject button = new GameObject(name);
            button.transform.SetParent(parent, false);
            
            RectTransform rect = button.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            
            button.AddComponent<CanvasRenderer>();
            
            // Background
            Image bgImage = button.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.7f);
            
            MobileSkillButton skillButton = button.AddComponent<MobileSkillButton>();
            
            // Create Icon child
            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(button.transform, false);
            RectTransform iconRect = icon.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.1f, 0.1f);
            iconRect.anchorMax = new Vector2(0.9f, 0.9f);
            iconRect.sizeDelta = Vector2.zero;
            icon.AddComponent<CanvasRenderer>();
            Image iconImage = icon.AddComponent<Image>();
            iconImage.color = Color.white;
            iconImage.raycastTarget = false;
            
            // Create Cooldown Overlay child
            GameObject cooldown = new GameObject("CooldownOverlay");
            cooldown.transform.SetParent(button.transform, false);
            RectTransform cooldownRect = cooldown.AddComponent<RectTransform>();
            cooldownRect.anchorMin = Vector2.zero;
            cooldownRect.anchorMax = Vector2.one;
            cooldownRect.sizeDelta = Vector2.zero;
            cooldown.AddComponent<CanvasRenderer>();
            Image cooldownImage = cooldown.AddComponent<Image>();
            cooldownImage.color = new Color(0f, 0f, 0f, 0.6f);
            cooldownImage.type = Image.Type.Filled;
            cooldownImage.fillMethod = Image.FillMethod.Radial360;
            cooldownImage.fillOrigin = (int)Image.Origin360.Top;
            cooldownImage.fillClockwise = false;
            cooldownImage.raycastTarget = false;
            cooldown.SetActive(false);
            
            // Set references
            SerializedObject serialized = new SerializedObject(skillButton);
            serialized.FindProperty("_background").objectReferenceValue = bgImage;
            serialized.FindProperty("_iconImage").objectReferenceValue = iconImage;
            serialized.FindProperty("_cooldownOverlay").objectReferenceValue = cooldownImage;
            serialized.FindProperty("_buttonSize").floatValue = size;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            
            return button;
        }
        
        private static GameObject CreateStruggleButtonInline(Transform parent)
        {
            GameObject root = new GameObject("StruggleButton");
            root.transform.SetParent(parent, false);
            
            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(160, 160);
            
            root.AddComponent<CanvasRenderer>();
            
            // Button Image
            Image buttonImage = root.AddComponent<Image>();
            buttonImage.color = new Color(0.9f, 0.3f, 0.2f, 1f); // Orange-red
            
            StruggleButtonUI struggleButton = root.AddComponent<StruggleButtonUI>();
            
            // Create Progress Ring
            GameObject progressRing = new GameObject("ProgressRing");
            progressRing.transform.SetParent(root.transform, false);
            RectTransform progressRect = progressRing.AddComponent<RectTransform>();
            progressRect.anchorMin = Vector2.zero;
            progressRect.anchorMax = Vector2.one;
            progressRect.sizeDelta = new Vector2(-10, -10);
            progressRing.AddComponent<CanvasRenderer>();
            Image progressImage = progressRing.AddComponent<Image>();
            progressImage.color = new Color(1f, 0.8f, 0.2f, 0.8f);
            progressImage.type = Image.Type.Filled;
            progressImage.fillMethod = Image.FillMethod.Radial360;
            progressImage.fillOrigin = (int)Image.Origin360.Top;
            progressImage.fillAmount = 0f;
            progressImage.raycastTarget = false;
            
            // Create Prompt Text
            GameObject textObj = new GameObject("PromptText");
            textObj.transform.SetParent(root.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            // Use legacy Text for simplicity
            Text promptText = textObj.AddComponent<Text>();
            promptText.text = "挣扎";
            promptText.alignment = TextAnchor.MiddleCenter;
            promptText.fontSize = 24;
            promptText.fontStyle = FontStyle.Bold;
            promptText.color = Color.white;
            promptText.raycastTarget = false;
            
            // Set references
            SerializedObject serialized = new SerializedObject(struggleButton);
            serialized.FindProperty("_buttonImage").objectReferenceValue = buttonImage;
            serialized.FindProperty("_progressRing").objectReferenceValue = progressImage;
            serialized.FindProperty("_buttonSize").floatValue = 160f;
            serialized.FindProperty("_tapsRequired").intValue = 10;
            serialized.FindProperty("_tapWindow").floatValue = 3f;
            serialized.FindProperty("_buttonColor").colorValue = new Color(0.9f, 0.3f, 0.2f, 1f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            
            return root;
        }
        
        #endregion
        
        [MenuItem("PetGrooming/Create Mobile UI/Validate Mobile HUD Prefab")]
        public static void ValidateMobileHUDPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            
            if (prefab == null)
            {
                Debug.LogError($"MobileHUD prefab not found at: {PrefabPath}");
                return;
            }
            
            // Check MobileHUDManager
            MobileHUDManager hudManager = prefab.GetComponent<MobileHUDManager>();
            if (hudManager == null)
            {
                Debug.LogError("MobileHUDManager component not found on prefab!");
                return;
            }
            
            // Check Canvas
            Canvas canvas = prefab.GetComponent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Canvas component not found on prefab!");
                return;
            }
            
            // Check child components
            VirtualJoystick joystick = prefab.GetComponentInChildren<VirtualJoystick>(true);
            if (joystick == null)
            {
                Debug.LogWarning("VirtualJoystick not found in prefab hierarchy!");
            }
            
            SkillWheelUI skillWheel = prefab.GetComponentInChildren<SkillWheelUI>(true);
            if (skillWheel == null)
            {
                Debug.LogWarning("SkillWheelUI not found in prefab hierarchy!");
            }
            
            StruggleButtonUI struggleButton = prefab.GetComponentInChildren<StruggleButtonUI>(true);
            if (struggleButton == null)
            {
                Debug.LogWarning("StruggleButtonUI not found in prefab hierarchy!");
            }
            
            Debug.Log("MobileHUD prefab validation passed!");
        }
    }
}
