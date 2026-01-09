using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using PetGrooming.UI.MobileUI;
using PetGrooming.Core;

namespace PetGrooming.Setup.Editor
{
    /// <summary>
    /// Editor utility to create the VirtualJoystick prefab.
    /// Creates the UI hierarchy with Background and Handle elements.
    /// Requirements: 1.1, 1.2
    /// </summary>
    public static class VirtualJoystickPrefabCreator
    {
        private const string PrefabPath = "Assets/UI/MobileUI/VirtualJoystick.prefab";
        private const string FolderPath = "Assets/UI/MobileUI";
        
        [MenuItem("PetGrooming/Create Mobile UI/Virtual Joystick Prefab")]
        public static void CreateVirtualJoystickPrefab()
        {
            // Ensure folder exists
            if (!AssetDatabase.IsValidFolder(FolderPath))
            {
                if (!AssetDatabase.IsValidFolder("Assets/UI"))
                {
                    AssetDatabase.CreateFolder("Assets", "UI");
                }
                AssetDatabase.CreateFolder("Assets/UI", "MobileUI");
            }
            
            // Create root GameObject
            GameObject root = new GameObject("VirtualJoystick");
            RectTransform rootRect = root.AddComponent<RectTransform>();
            
            // Configure root RectTransform - bottom-left anchor
            rootRect.anchorMin = new Vector2(0, 0);
            rootRect.anchorMax = new Vector2(0, 0);
            rootRect.pivot = new Vector2(0, 0);
            rootRect.anchoredPosition = new Vector2(150, 150); // Default offset
            rootRect.sizeDelta = new Vector2(180, 180); // Default joystick size
            
            // Add VirtualJoystick component
            VirtualJoystick joystick = root.AddComponent<VirtualJoystick>();
            
            // Create Background
            GameObject background = CreateBackground(root.transform);
            RectTransform bgRect = background.GetComponent<RectTransform>();
            
            // Create Handle
            GameObject handle = CreateHandle(background.transform);
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            Image handleImage = handle.GetComponent<Image>();
            
            // Set references on VirtualJoystick component using SerializedObject
            SerializedObject serializedJoystick = new SerializedObject(joystick);
            serializedJoystick.FindProperty("_background").objectReferenceValue = bgRect;
            serializedJoystick.FindProperty("_handle").objectReferenceValue = handleRect;
            serializedJoystick.FindProperty("_handleImage").objectReferenceValue = handleImage;
            serializedJoystick.FindProperty("_handleRange").floatValue = 1f;
            serializedJoystick.FindProperty("_dynamicPosition").boolValue = true;
            serializedJoystick.FindProperty("_returnDuration").floatValue = 0.1f;
            serializedJoystick.FindProperty("_idleOpacity").floatValue = 0.6f;
            serializedJoystick.FindProperty("_activeOpacity").floatValue = 1f;
            serializedJoystick.ApplyModifiedPropertiesWithoutUndo();
            
            // Save as prefab
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            
            // Clean up scene object
            Object.DestroyImmediate(root);
            
            // Select the created prefab
            Selection.activeObject = prefab;
            
            Debug.Log($"VirtualJoystick prefab created at: {PrefabPath}");
        }
        
        private static GameObject CreateBackground(Transform parent)
        {
            GameObject background = new GameObject("Background");
            background.transform.SetParent(parent, false);
            background.layer = LayerMask.NameToLayer("UI");
            
            // Add RectTransform
            RectTransform rect = background.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(180, 180); // Joystick background size (Requirement 1.2)
            
            // Add CanvasRenderer
            background.AddComponent<CanvasRenderer>();
            
            // Add Image component
            Image image = background.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.5f); // Semi-transparent dark
            image.raycastTarget = true;
            
            return background;
        }
        
        private static GameObject CreateHandle(Transform parent)
        {
            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(parent, false);
            handle.layer = LayerMask.NameToLayer("UI");
            
            // Add RectTransform
            RectTransform rect = handle.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(70, 70); // Handle size (Requirement 1.2: 60-80 pixels)
            
            // Add CanvasRenderer
            handle.AddComponent<CanvasRenderer>();
            
            // Add Image component
            Image image = handle.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.6f); // White with idle opacity
            image.raycastTarget = false; // Handle doesn't need to receive raycasts
            
            return handle;
        }
        
        [MenuItem("PetGrooming/Create Mobile UI/Validate Virtual Joystick Prefab")]
        public static void ValidateVirtualJoystickPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            
            if (prefab == null)
            {
                Debug.LogError($"VirtualJoystick prefab not found at: {PrefabPath}");
                return;
            }
            
            VirtualJoystick joystick = prefab.GetComponent<VirtualJoystick>();
            if (joystick == null)
            {
                Debug.LogError("VirtualJoystick component not found on prefab!");
                return;
            }
            
            // Check hierarchy
            Transform background = prefab.transform.Find("Background");
            if (background == null)
            {
                Debug.LogError("Background child not found!");
                return;
            }
            
            Transform handle = background.Find("Handle");
            if (handle == null)
            {
                Debug.LogError("Handle child not found!");
                return;
            }
            
            // Check sizes (Requirement 1.2)
            RectTransform bgRect = background.GetComponent<RectTransform>();
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            
            if (bgRect.sizeDelta.x < 150 || bgRect.sizeDelta.x > 200)
            {
                Debug.LogWarning($"Background size {bgRect.sizeDelta.x} is outside recommended range (150-200)");
            }
            
            if (handleRect.sizeDelta.x < 60 || handleRect.sizeDelta.x > 80)
            {
                Debug.LogWarning($"Handle size {handleRect.sizeDelta.x} is outside recommended range (60-80)");
            }
            
            Debug.Log("VirtualJoystick prefab validation passed!");
        }
    }
}
