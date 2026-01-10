using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using PetGrooming.UI.MobileUI;
using PetGrooming.Core;

namespace PetGrooming.Setup.Editor
{
    /// <summary>
    /// Editor utility to create the SkillWheelUI prefab.
    /// Creates the skill wheel with capture button and 3 skill buttons arranged in an arc.
    /// Requirements: 2.1, 3.1
    /// </summary>
    public static class SkillWheelPrefabCreator
    {
        private const string PrefabPath = "Assets/UI/MobileUI/SkillWheel.prefab";
        private const string FolderPath = "Assets/UI/MobileUI";
        private const string SkillButtonPrefabPath = "Assets/UI/MobileUI/MobileSkillButton.prefab";
        private const string CaptureButtonPrefabPath = "Assets/UI/MobileUI/CaptureButton.prefab";
        
        // Default layout settings
        private const float DefaultArcRadius = 150f;
        private const float DefaultArcStartAngle = 135f;
        private const float DefaultArcSpan = 90f;
        private const float DefaultCaptureButtonSize = 140f;
        private const float DefaultSkillButtonSize = 100f;
        
        [MenuItem("PetGrooming/Create Mobile UI/Skill Wheel Prefab")]
        public static void CreateSkillWheelPrefab()
        {
            // Ensure folder exists
            EnsureFolderExists();
            
            // Create root GameObject
            GameObject root = new GameObject("SkillWheel");
            RectTransform rootRect = root.AddComponent<RectTransform>();
            
            // Configure root RectTransform - anchor to bottom-right
            rootRect.anchorMin = new Vector2(1f, 0f);
            rootRect.anchorMax = new Vector2(1f, 0f);
            rootRect.pivot = new Vector2(1f, 0f);
            rootRect.anchoredPosition = new Vector2(-100f, 100f);
            rootRect.sizeDelta = new Vector2(400f, 400f); // Large enough to contain all buttons
            
            // Add SkillWheelUI component
            SkillWheelUI skillWheel = root.AddComponent<SkillWheelUI>();
            
            // Create capture button
            GameObject captureButton = CreateCaptureButton(root.transform);
            
            // Create skill buttons
            GameObject[] skillButtons = CreateSkillButtons(root.transform, 3);
            
            // Get component references
            MobileSkillButton captureButtonComponent = captureButton.GetComponent<MobileSkillButton>();
            MobileSkillButton[] skillButtonComponents = new MobileSkillButton[skillButtons.Length];
            for (int i = 0; i < skillButtons.Length; i++)
            {
                skillButtonComponents[i] = skillButtons[i].GetComponent<MobileSkillButton>();
            }
            
            // Set references on SkillWheelUI component using SerializedObject
            SerializedObject serializedWheel = new SerializedObject(skillWheel);
            serializedWheel.FindProperty("_captureButton").objectReferenceValue = captureButtonComponent;
            
            // Set skill buttons array
            SerializedProperty skillButtonsProperty = serializedWheel.FindProperty("_skillButtons");
            skillButtonsProperty.arraySize = skillButtonComponents.Length;
            for (int i = 0; i < skillButtonComponents.Length; i++)
            {
                skillButtonsProperty.GetArrayElementAtIndex(i).objectReferenceValue = skillButtonComponents[i];
            }
            
            // Set layout parameters
            serializedWheel.FindProperty("_arcRadius").floatValue = DefaultArcRadius;
            serializedWheel.FindProperty("_arcStartAngle").floatValue = DefaultArcStartAngle;
            serializedWheel.FindProperty("_arcSpan").floatValue = DefaultArcSpan;
            serializedWheel.FindProperty("_minButtonSpacing").floatValue = 20f;
            
            serializedWheel.ApplyModifiedPropertiesWithoutUndo();
            
            // Position buttons in arc
            PositionButtonsInArc(skillButtons, DefaultArcRadius, DefaultArcStartAngle, DefaultArcSpan);
            
            // Save as prefab
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            
            // Clean up scene object
            Object.DestroyImmediate(root);
            
            // Select the created prefab
            Selection.activeObject = prefab;
            
            Debug.Log($"SkillWheel prefab created at: {PrefabPath}");
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
        
        private static GameObject CreateCaptureButton(Transform parent)
        {
            // Try to load existing prefab
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CaptureButtonPrefabPath);
            
            GameObject captureButton;
            if (prefab != null)
            {
                captureButton = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            }
            else
            {
                // Create from scratch if prefab doesn't exist
                captureButton = CreateButtonFromScratch("CaptureButton", DefaultCaptureButtonSize, parent);
            }
            
            // Position at bottom-right (origin of skill wheel)
            RectTransform rect = captureButton.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(-DefaultCaptureButtonSize / 2f - 20f, DefaultCaptureButtonSize / 2f + 20f);
            
            return captureButton;
        }
        
        private static GameObject[] CreateSkillButtons(Transform parent, int count)
        {
            GameObject[] buttons = new GameObject[count];
            
            // Try to load existing prefab
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SkillButtonPrefabPath);
            
            for (int i = 0; i < count; i++)
            {
                GameObject button;
                if (prefab != null)
                {
                    button = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                    button.name = $"SkillButton_{i + 1}";
                }
                else
                {
                    // Create from scratch if prefab doesn't exist
                    button = CreateButtonFromScratch($"SkillButton_{i + 1}", DefaultSkillButtonSize, parent);
                }
                
                // Configure RectTransform
                RectTransform rect = button.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                
                buttons[i] = button;
            }
            
            return buttons;
        }
        
        private static GameObject CreateButtonFromScratch(string name, float size, Transform parent)
        {
            GameObject button = new GameObject(name);
            button.transform.SetParent(parent, false);
            button.layer = LayerMask.NameToLayer("UI");
            
            // Add RectTransform
            RectTransform rect = button.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(size, size);
            
            // Add MobileSkillButton component
            MobileSkillButton skillButton = button.AddComponent<MobileSkillButton>();
            
            // Create background
            GameObject background = new GameObject("Background");
            background.transform.SetParent(button.transform, false);
            background.layer = LayerMask.NameToLayer("UI");
            
            RectTransform bgRect = background.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            
            background.AddComponent<CanvasRenderer>();
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.7f);
            bgImage.raycastTarget = true;
            
            // Create icon
            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(button.transform, false);
            icon.layer = LayerMask.NameToLayer("UI");
            
            RectTransform iconRect = icon.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(10f, 10f);
            iconRect.offsetMax = new Vector2(-10f, -10f);
            
            icon.AddComponent<CanvasRenderer>();
            Image iconImage = icon.AddComponent<Image>();
            iconImage.color = Color.white;
            iconImage.raycastTarget = false;
            iconImage.preserveAspect = true;
            
            // Create cooldown overlay
            GameObject overlay = new GameObject("CooldownOverlay");
            overlay.transform.SetParent(button.transform, false);
            overlay.layer = LayerMask.NameToLayer("UI");
            
            RectTransform overlayRect = overlay.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            
            overlay.AddComponent<CanvasRenderer>();
            Image overlayImage = overlay.AddComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.6f);
            overlayImage.raycastTarget = false;
            overlayImage.type = Image.Type.Filled;
            overlayImage.fillMethod = Image.FillMethod.Radial360;
            overlayImage.fillOrigin = (int)Image.Origin360.Top;
            overlayImage.fillClockwise = false;
            overlayImage.fillAmount = 0f;
            overlayImage.enabled = false;
            
            // Create cooldown text
            GameObject textObj = new GameObject("CooldownText");
            textObj.transform.SetParent(button.transform, false);
            textObj.layer = LayerMask.NameToLayer("UI");
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(60, 40);
            
            TMPro.TextMeshProUGUI text = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            text.text = "";
            text.fontSize = 24;
            text.fontStyle = TMPro.FontStyles.Bold;
            text.alignment = TMPro.TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            text.enabled = false;
            
            // Create glow
            GameObject glow = new GameObject("Glow");
            glow.transform.SetParent(button.transform, false);
            glow.transform.SetAsFirstSibling();
            glow.layer = LayerMask.NameToLayer("UI");
            
            RectTransform glowRect = glow.AddComponent<RectTransform>();
            glowRect.anchorMin = new Vector2(0.5f, 0.5f);
            glowRect.anchorMax = new Vector2(0.5f, 0.5f);
            glowRect.pivot = new Vector2(0.5f, 0.5f);
            glowRect.anchoredPosition = Vector2.zero;
            glowRect.sizeDelta = new Vector2(size + 20, size + 20);
            
            glow.AddComponent<CanvasRenderer>();
            Image glowImage = glow.AddComponent<Image>();
            glowImage.color = new Color(1f, 1f, 0.5f, 0.8f);
            glowImage.raycastTarget = false;
            glowImage.enabled = false;
            
            // Set references using SerializedObject
            SerializedObject serializedButton = new SerializedObject(skillButton);
            serializedButton.FindProperty("_background").objectReferenceValue = bgImage;
            serializedButton.FindProperty("_iconImage").objectReferenceValue = iconImage;
            serializedButton.FindProperty("_cooldownOverlay").objectReferenceValue = overlayImage;
            serializedButton.FindProperty("_cooldownText").objectReferenceValue = text;
            serializedButton.FindProperty("_glowEffect").objectReferenceValue = glowImage;
            serializedButton.FindProperty("_buttonSize").floatValue = size;
            serializedButton.ApplyModifiedPropertiesWithoutUndo();
            
            return button;
        }
        
        private static void PositionButtonsInArc(GameObject[] buttons, float arcRadius, float arcStartAngle, float arcSpan)
        {
            if (buttons == null || buttons.Length == 0) return;
            
            Vector2[] positions = SkillWheelUI.CalculateArcPositions(
                buttons.Length, arcRadius, arcStartAngle, arcSpan
            );
            
            // Calculate offset from capture button position
            Vector2 captureButtonOffset = new Vector2(
                -DefaultCaptureButtonSize / 2f - 20f,
                DefaultCaptureButtonSize / 2f + 20f
            );
            
            for (int i = 0; i < buttons.Length && i < positions.Length; i++)
            {
                RectTransform rect = buttons[i].GetComponent<RectTransform>();
                if (rect != null)
                {
                    // Position relative to capture button
                    rect.anchoredPosition = captureButtonOffset + positions[i];
                }
            }
        }
        
        [MenuItem("PetGrooming/Create Mobile UI/Validate Skill Wheel Prefab")]
        public static void ValidateSkillWheelPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            
            if (prefab == null)
            {
                Debug.LogError($"SkillWheel prefab not found at: {PrefabPath}");
                return;
            }
            
            SkillWheelUI skillWheel = prefab.GetComponent<SkillWheelUI>();
            if (skillWheel == null)
            {
                Debug.LogError("SkillWheelUI component not found on prefab!");
                return;
            }
            
            // Check capture button
            if (skillWheel.CaptureButton == null)
            {
                Debug.LogError("CaptureButton reference is not set!");
                return;
            }
            
            // Check skill buttons
            if (skillWheel.SkillButtons == null || skillWheel.SkillButtons.Length != 3)
            {
                Debug.LogError($"Expected 3 skill buttons, found {skillWheel.SkillButtonCount}");
                return;
            }
            
            foreach (var button in skillWheel.SkillButtons)
            {
                if (button == null)
                {
                    Debug.LogError("One or more skill button references are null!");
                    return;
                }
            }
            
            // Check layout parameters
            if (skillWheel.ArcRadius <= 0)
            {
                Debug.LogWarning("ArcRadius should be positive");
            }
            
            if (skillWheel.ArcSpan <= 0)
            {
                Debug.LogWarning("ArcSpan should be positive");
            }
            
            Debug.Log("SkillWheel prefab validation passed!");
            Debug.Log($"  - Capture Button: {skillWheel.CaptureButton.name}");
            Debug.Log($"  - Skill Buttons: {skillWheel.SkillButtonCount}");
            Debug.Log($"  - Arc Radius: {skillWheel.ArcRadius}");
            Debug.Log($"  - Arc Start Angle: {skillWheel.ArcStartAngle}");
            Debug.Log($"  - Arc Span: {skillWheel.ArcSpan}");
        }
    }
}
