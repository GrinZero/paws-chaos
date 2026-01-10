using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using PetGrooming.UI.MobileUI;
using PetGrooming.Core;

namespace PetGrooming.Setup.Editor
{
    /// <summary>
    /// Editor utility to create the StruggleButton prefab.
    /// Creates the UI hierarchy with Button, ProgressRing, and PromptText elements.
    /// Requirements: 2.5.2, 2.5.6
    /// </summary>
    public static class StruggleButtonPrefabCreator
    {
        private const string PrefabPath = "Assets/UI/MobileUI/StruggleButton.prefab";
        private const string FolderPath = "Assets/UI/MobileUI";
        
        // Default sizes (Requirement 2.5.1: 160 pixels diameter)
        private const float DefaultButtonSize = 160f;
        private const float ProgressRingThickness = 8f;
        
        // Default colors (Requirement 2.5.6: orange/red theme)
        private static readonly Color ButtonColor = new Color(0.9f, 0.3f, 0.2f, 1f);
        private static readonly Color ProgressRingColor = new Color(1f, 0.5f, 0.2f, 1f);
        private static readonly Color ProgressRingBackgroundColor = new Color(0.3f, 0.1f, 0.1f, 0.5f);
        
        [MenuItem("PetGrooming/Create Mobile UI/Struggle Button Prefab")]
        public static void CreateStruggleButtonPrefab()
        {
            // Ensure folder exists
            EnsureFolderExists();
            
            // Create root GameObject
            GameObject root = new GameObject("StruggleButton");
            RectTransform rootRect = root.AddComponent<RectTransform>();
            
            // Configure root RectTransform
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.sizeDelta = new Vector2(DefaultButtonSize, DefaultButtonSize);
            
            // Add StruggleButtonUI component
            StruggleButtonUI struggleButton = root.AddComponent<StruggleButtonUI>();
            
            // Create UI hierarchy
            GameObject progressRingBg = CreateProgressRingBackground(root.transform);
            GameObject progressRing = CreateProgressRing(root.transform);
            GameObject buttonImage = CreateButtonImage(root.transform);
            GameObject promptText = CreatePromptText(root.transform);
            
            // Get component references
            Image buttonImg = buttonImage.GetComponent<Image>();
            Image progressRingImg = progressRing.GetComponent<Image>();
            TextMeshProUGUI textComponent = promptText.GetComponent<TextMeshProUGUI>();
            
            // Set references on StruggleButtonUI component using SerializedObject
            SerializedObject serializedButton = new SerializedObject(struggleButton);
            serializedButton.FindProperty("_buttonImage").objectReferenceValue = buttonImg;
            serializedButton.FindProperty("_progressRing").objectReferenceValue = progressRingImg;
            serializedButton.FindProperty("_promptText").objectReferenceValue = textComponent;
            serializedButton.FindProperty("_buttonSize").floatValue = DefaultButtonSize;
            serializedButton.FindProperty("_tapsRequired").intValue = 10;
            serializedButton.FindProperty("_tapWindow").floatValue = 3f;
            serializedButton.FindProperty("_buttonColor").colorValue = ButtonColor;
            serializedButton.ApplyModifiedPropertiesWithoutUndo();
            
            // Save as prefab
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            
            // Clean up scene object
            Object.DestroyImmediate(root);
            
            // Select the created prefab
            Selection.activeObject = prefab;
            
            Debug.Log($"StruggleButton prefab created at: {PrefabPath}");
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
        
        private static GameObject CreateProgressRingBackground(Transform parent)
        {
            GameObject ringBg = new GameObject("ProgressRingBackground");
            ringBg.transform.SetParent(parent, false);
            ringBg.layer = LayerMask.NameToLayer("UI");
            
            // Add RectTransform - stretch to fill parent
            RectTransform rect = ringBg.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            // Add CanvasRenderer
            ringBg.AddComponent<CanvasRenderer>();
            
            // Add Image component - full ring background
            Image image = ringBg.AddComponent<Image>();
            image.color = ProgressRingBackgroundColor;
            image.raycastTarget = false;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial360;
            image.fillOrigin = (int)Image.Origin360.Top;
            image.fillClockwise = true;
            image.fillAmount = 1f;
            
            return ringBg;
        }
        
        private static GameObject CreateProgressRing(Transform parent)
        {
            GameObject ring = new GameObject("ProgressRing");
            ring.transform.SetParent(parent, false);
            ring.layer = LayerMask.NameToLayer("UI");
            
            // Add RectTransform - stretch to fill parent
            RectTransform rect = ring.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            // Add CanvasRenderer
            ring.AddComponent<CanvasRenderer>();
            
            // Add Image component with radial fill for progress
            Image image = ring.AddComponent<Image>();
            image.color = ProgressRingColor;
            image.raycastTarget = false;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial360;
            image.fillOrigin = (int)Image.Origin360.Top;
            image.fillClockwise = true;
            image.fillAmount = 0f; // Start with no progress
            
            return ring;
        }
        
        private static GameObject CreateButtonImage(Transform parent)
        {
            GameObject button = new GameObject("ButtonImage");
            button.transform.SetParent(parent, false);
            button.layer = LayerMask.NameToLayer("UI");
            
            // Add RectTransform - slightly smaller than parent to show progress ring
            RectTransform rect = button.AddComponent<RectTransform>();
            float inset = ProgressRingThickness + 4f;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
            
            // Add CanvasRenderer
            button.AddComponent<CanvasRenderer>();
            
            // Add Image component
            Image image = button.AddComponent<Image>();
            image.color = ButtonColor;
            image.raycastTarget = true; // This is the touch target
            
            return button;
        }
        
        private static GameObject CreatePromptText(Transform parent)
        {
            GameObject textObj = new GameObject("PromptText");
            textObj.transform.SetParent(parent, false);
            textObj.layer = LayerMask.NameToLayer("UI");
            
            // Add RectTransform - centered
            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(120, 60);
            
            // Add TextMeshProUGUI component
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "挣扎"; // "Struggle" in Chinese
            text.fontSize = 28;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            
            // Add outline for better visibility
            text.outlineWidth = 0.2f;
            text.outlineColor = new Color(0.3f, 0.1f, 0.1f, 1f);
            
            return textObj;
        }
        
        [MenuItem("PetGrooming/Create Mobile UI/Validate Struggle Button Prefab")]
        public static void ValidateStruggleButtonPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            
            if (prefab == null)
            {
                Debug.LogError($"StruggleButton prefab not found at: {PrefabPath}");
                return;
            }
            
            StruggleButtonUI struggleButton = prefab.GetComponent<StruggleButtonUI>();
            if (struggleButton == null)
            {
                Debug.LogError("StruggleButtonUI component not found on prefab!");
                return;
            }
            
            // Check hierarchy
            string[] requiredChildren = { "ProgressRingBackground", "ProgressRing", "ButtonImage", "PromptText" };
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
            
            // Check progress ring configuration
            Transform ringTransform = prefab.transform.Find("ProgressRing");
            Image ringImage = ringTransform.GetComponent<Image>();
            
            if (ringImage.type != Image.Type.Filled)
            {
                Debug.LogWarning("ProgressRing should use Filled image type for radial fill");
            }
            
            if (ringImage.fillMethod != Image.FillMethod.Radial360)
            {
                Debug.LogWarning("ProgressRing should use Radial360 fill method");
            }
            
            // Check prompt text
            Transform textTransform = prefab.transform.Find("PromptText");
            TextMeshProUGUI textComponent = textTransform.GetComponent<TextMeshProUGUI>();
            
            if (textComponent == null)
            {
                Debug.LogError("PromptText should have TextMeshProUGUI component!");
                return;
            }
            
            if (textComponent.text != "挣扎")
            {
                Debug.LogWarning("PromptText should display '挣扎' (Struggle)");
            }
            
            // Check button color (orange/red theme)
            Transform buttonTransform = prefab.transform.Find("ButtonImage");
            Image buttonImage = buttonTransform.GetComponent<Image>();
            
            // Verify it's in the orange/red range
            Color color = buttonImage.color;
            if (color.r < 0.7f || color.g > 0.5f)
            {
                Debug.LogWarning("ButtonImage should have orange/red color theme (Requirement 2.5.6)");
            }
            
            // Check button size (Requirement 2.5.1: 160 pixels)
            RectTransform rootRect = prefab.GetComponent<RectTransform>();
            if (Mathf.Abs(rootRect.sizeDelta.x - 160f) > 1f || Mathf.Abs(rootRect.sizeDelta.y - 160f) > 1f)
            {
                Debug.LogWarning("StruggleButton should be 160 pixels diameter (Requirement 2.5.1)");
            }
            
            Debug.Log("StruggleButton prefab validation passed!");
        }
    }
}
