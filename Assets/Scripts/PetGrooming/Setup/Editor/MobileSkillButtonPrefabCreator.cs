using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using PetGrooming.UI.MobileUI;
using PetGrooming.Core;

namespace PetGrooming.Setup.Editor
{
    /// <summary>
    /// Editor utility to create the MobileSkillButton prefab.
    /// Creates the UI hierarchy with Background, Icon, CooldownOverlay, CooldownText, and Glow elements.
    /// Requirements: 2.9
    /// </summary>
    public static class MobileSkillButtonPrefabCreator
    {
        private const string PrefabPath = "Assets/UI/MobileUI/MobileSkillButton.prefab";
        private const string FolderPath = "Assets/UI/MobileUI";
        
        // Default sizes
        private const float DefaultButtonSize = 100f;
        private const float IconPadding = 10f;
        
        // Default colors
        private static readonly Color BackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.7f);
        private static readonly Color CooldownOverlayColor = new Color(0f, 0f, 0f, 0.6f);
        private static readonly Color GlowColor = new Color(1f, 1f, 0.5f, 0.8f);
        
        [MenuItem("PetGrooming/Create Mobile UI/Mobile Skill Button Prefab")]
        public static void CreateMobileSkillButtonPrefab()
        {
            // Ensure folder exists
            EnsureFolderExists();
            
            // Create root GameObject
            GameObject root = new GameObject("MobileSkillButton");
            RectTransform rootRect = root.AddComponent<RectTransform>();
            
            // Configure root RectTransform
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.sizeDelta = new Vector2(DefaultButtonSize, DefaultButtonSize);
            
            // Add MobileSkillButton component
            MobileSkillButton skillButton = root.AddComponent<MobileSkillButton>();
            
            // Create UI hierarchy
            GameObject background = CreateBackground(root.transform);
            GameObject icon = CreateIcon(root.transform);
            GameObject cooldownOverlay = CreateCooldownOverlay(root.transform);
            GameObject cooldownText = CreateCooldownText(root.transform);
            GameObject glow = CreateGlow(root.transform);
            
            // Get component references
            Image bgImage = background.GetComponent<Image>();
            Image iconImage = icon.GetComponent<Image>();
            Image overlayImage = cooldownOverlay.GetComponent<Image>();
            TextMeshProUGUI textComponent = cooldownText.GetComponent<TextMeshProUGUI>();
            Image glowImage = glow.GetComponent<Image>();
            
            // Set references on MobileSkillButton component using SerializedObject
            SerializedObject serializedButton = new SerializedObject(skillButton);
            serializedButton.FindProperty("_background").objectReferenceValue = bgImage;
            serializedButton.FindProperty("_iconImage").objectReferenceValue = iconImage;
            serializedButton.FindProperty("_cooldownOverlay").objectReferenceValue = overlayImage;
            serializedButton.FindProperty("_cooldownText").objectReferenceValue = textComponent;
            serializedButton.FindProperty("_glowEffect").objectReferenceValue = glowImage;
            serializedButton.FindProperty("_buttonSize").floatValue = DefaultButtonSize;
            serializedButton.FindProperty("_readyColor").colorValue = Color.white;
            serializedButton.FindProperty("_cooldownColor").colorValue = new Color(0.5f, 0.5f, 0.5f, 1f);
            serializedButton.ApplyModifiedPropertiesWithoutUndo();
            
            // Save as prefab
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            
            // Clean up scene object
            Object.DestroyImmediate(root);
            
            // Select the created prefab
            Selection.activeObject = prefab;
            
            Debug.Log($"MobileSkillButton prefab created at: {PrefabPath}");
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
        
        private static GameObject CreateBackground(Transform parent)
        {
            GameObject background = new GameObject("Background");
            background.transform.SetParent(parent, false);
            background.layer = LayerMask.NameToLayer("UI");
            
            // Add RectTransform - stretch to fill parent
            RectTransform rect = background.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            // Add CanvasRenderer
            background.AddComponent<CanvasRenderer>();
            
            // Add Image component
            Image image = background.AddComponent<Image>();
            image.color = BackgroundColor;
            image.raycastTarget = true;
            
            // Make it circular by using a sprite or setting image type
            // For now, use default sprite (can be replaced with circular sprite)
            image.type = Image.Type.Simple;
            
            return background;
        }
        
        private static GameObject CreateIcon(Transform parent)
        {
            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(parent, false);
            icon.layer = LayerMask.NameToLayer("UI");
            
            // Add RectTransform - centered with padding
            RectTransform rect = icon.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(IconPadding, IconPadding);
            rect.offsetMax = new Vector2(-IconPadding, -IconPadding);
            
            // Add CanvasRenderer
            icon.AddComponent<CanvasRenderer>();
            
            // Add Image component
            Image image = icon.AddComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = false;
            image.preserveAspect = true;
            
            return icon;
        }
        
        private static GameObject CreateCooldownOverlay(Transform parent)
        {
            GameObject overlay = new GameObject("CooldownOverlay");
            overlay.transform.SetParent(parent, false);
            overlay.layer = LayerMask.NameToLayer("UI");
            
            // Add RectTransform - stretch to fill parent
            RectTransform rect = overlay.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            // Add CanvasRenderer
            overlay.AddComponent<CanvasRenderer>();
            
            // Add Image component with radial fill
            Image image = overlay.AddComponent<Image>();
            image.color = CooldownOverlayColor;
            image.raycastTarget = false;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial360;
            image.fillOrigin = (int)Image.Origin360.Top;
            image.fillClockwise = false;
            image.fillAmount = 0f; // Start with no cooldown
            image.enabled = false; // Hidden by default
            
            return overlay;
        }
        
        private static GameObject CreateCooldownText(Transform parent)
        {
            GameObject textObj = new GameObject("CooldownText");
            textObj.transform.SetParent(parent, false);
            textObj.layer = LayerMask.NameToLayer("UI");
            
            // Add RectTransform - centered
            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(60, 40);
            
            // Add TextMeshProUGUI component
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "";
            text.fontSize = 24;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            text.enabled = false; // Hidden by default
            
            // Add outline for better visibility
            text.outlineWidth = 0.2f;
            text.outlineColor = Color.black;
            
            return textObj;
        }
        
        private static GameObject CreateGlow(Transform parent)
        {
            GameObject glow = new GameObject("Glow");
            glow.transform.SetParent(parent, false);
            glow.layer = LayerMask.NameToLayer("UI");
            
            // Set as first sibling so it renders behind other elements
            glow.transform.SetAsFirstSibling();
            
            // Add RectTransform - slightly larger than parent for glow effect
            RectTransform rect = glow.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(DefaultButtonSize + 20, DefaultButtonSize + 20); // 10px glow on each side
            
            // Add CanvasRenderer
            glow.AddComponent<CanvasRenderer>();
            
            // Add Image component
            Image image = glow.AddComponent<Image>();
            image.color = GlowColor;
            image.raycastTarget = false;
            image.enabled = false; // Hidden by default, shown when skill is ready
            
            return glow;
        }
        
        [MenuItem("PetGrooming/Create Mobile UI/Validate Mobile Skill Button Prefab")]
        public static void ValidateMobileSkillButtonPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            
            if (prefab == null)
            {
                Debug.LogError($"MobileSkillButton prefab not found at: {PrefabPath}");
                return;
            }
            
            MobileSkillButton skillButton = prefab.GetComponent<MobileSkillButton>();
            if (skillButton == null)
            {
                Debug.LogError("MobileSkillButton component not found on prefab!");
                return;
            }
            
            // Check hierarchy
            string[] requiredChildren = { "Background", "Icon", "CooldownOverlay", "CooldownText", "Glow" };
            bool allFound = true;
            
            foreach (string childName in requiredChildren)
            {
                Transform child = prefab.transform.Find(childName);
                if (child == null)
                {
                    Debug.LogError($"{childName} child not found!");
                    allFound = false;
                }
            }
            
            if (!allFound)
            {
                return;
            }
            
            // Check cooldown overlay configuration
            Transform overlayTransform = prefab.transform.Find("CooldownOverlay");
            Image overlayImage = overlayTransform.GetComponent<Image>();
            
            if (overlayImage.type != Image.Type.Filled)
            {
                Debug.LogWarning("CooldownOverlay should use Filled image type for radial fill");
            }
            
            if (overlayImage.fillMethod != Image.FillMethod.Radial360)
            {
                Debug.LogWarning("CooldownOverlay should use Radial360 fill method");
            }
            
            // Check cooldown text
            Transform textTransform = prefab.transform.Find("CooldownText");
            TextMeshProUGUI textComponent = textTransform.GetComponent<TextMeshProUGUI>();
            
            if (textComponent == null)
            {
                Debug.LogError("CooldownText should have TextMeshProUGUI component!");
                return;
            }
            
            Debug.Log("MobileSkillButton prefab validation passed!");
        }
        
        [MenuItem("PetGrooming/Create Mobile UI/Create Capture Button Prefab (Large)")]
        public static void CreateCaptureButtonPrefab()
        {
            // Ensure folder exists
            EnsureFolderExists();
            
            // Create root GameObject
            GameObject root = new GameObject("CaptureButton");
            RectTransform rootRect = root.AddComponent<RectTransform>();
            
            // Configure root RectTransform - larger size for capture button (140px)
            float captureButtonSize = 140f;
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.sizeDelta = new Vector2(captureButtonSize, captureButtonSize);
            
            // Add MobileSkillButton component
            MobileSkillButton skillButton = root.AddComponent<MobileSkillButton>();
            
            // Create UI hierarchy
            GameObject background = CreateBackground(root.transform);
            GameObject icon = CreateIcon(root.transform);
            GameObject cooldownOverlay = CreateCooldownOverlay(root.transform);
            GameObject cooldownText = CreateCooldownText(root.transform);
            GameObject glow = CreateGlow(root.transform);
            
            // Adjust glow size for larger button
            RectTransform glowRect = glow.GetComponent<RectTransform>();
            glowRect.sizeDelta = new Vector2(captureButtonSize + 30, captureButtonSize + 30);
            
            // Get component references
            Image bgImage = background.GetComponent<Image>();
            Image iconImage = icon.GetComponent<Image>();
            Image overlayImage = cooldownOverlay.GetComponent<Image>();
            TextMeshProUGUI textComponent = cooldownText.GetComponent<TextMeshProUGUI>();
            Image glowImage = glow.GetComponent<Image>();
            
            // Set gold/yellow theme for capture button
            glowImage.color = new Color(0.96f, 0.65f, 0.14f, 0.8f); // Gold glow
            
            // Set references on MobileSkillButton component
            SerializedObject serializedButton = new SerializedObject(skillButton);
            serializedButton.FindProperty("_background").objectReferenceValue = bgImage;
            serializedButton.FindProperty("_iconImage").objectReferenceValue = iconImage;
            serializedButton.FindProperty("_cooldownOverlay").objectReferenceValue = overlayImage;
            serializedButton.FindProperty("_cooldownText").objectReferenceValue = textComponent;
            serializedButton.FindProperty("_glowEffect").objectReferenceValue = glowImage;
            serializedButton.FindProperty("_buttonSize").floatValue = captureButtonSize;
            serializedButton.FindProperty("_readyColor").colorValue = Color.white;
            serializedButton.FindProperty("_cooldownColor").colorValue = new Color(0.5f, 0.5f, 0.5f, 1f);
            serializedButton.ApplyModifiedPropertiesWithoutUndo();
            
            // Save as prefab
            string capturePrefabPath = "Assets/UI/MobileUI/CaptureButton.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, capturePrefabPath);
            
            // Clean up scene object
            Object.DestroyImmediate(root);
            
            // Select the created prefab
            Selection.activeObject = prefab;
            
            Debug.Log($"CaptureButton prefab created at: {capturePrefabPath}");
        }
    }
}
