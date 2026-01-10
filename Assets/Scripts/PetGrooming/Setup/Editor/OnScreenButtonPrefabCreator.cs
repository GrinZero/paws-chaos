using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem.OnScreen;
using UnityEditor;
using TMPro;
using PetGrooming.UI.MobileUI;

namespace PetGrooming.Setup.Editor
{
    /// <summary>
    /// Editor utility to create OnScreenButton prefabs for skill buttons.
    /// 使用 Unity Input System 官方的 OnScreenButton 组件替代自定义 MobileSkillButton。
    /// 
    /// Requirements: 2.1, 2.2, 2.5, 2.6
    /// - 2.1: 使用 Unity 的 OnScreenButton 组件
    /// - 2.2: 每个 OnScreenButton 绑定到对应的 Input Action
    /// - 2.5: 技能按钮视觉反馈由单独的 UI 组件处理
    /// - 2.6: OnScreenButton 支持可配置的 control path 绑定
    /// </summary>
    public static class OnScreenButtonPrefabCreator
    {
        private const string FolderPath = "Assets/UI/MobileUI";
        
        // 默认配置
        private const float DefaultButtonSize = 100f;
        private const float DefaultCaptureButtonSize = 140f;
        
        // 技能按钮配置
        private static readonly SkillButtonConfig[] SkillConfigs = new SkillButtonConfig[]
        {
            new SkillButtonConfig("OnScreenButton_Skill1", "<Gamepad>/buttonWest", DefaultButtonSize, "Skill1 (CaptureNet)"),
            new SkillButtonConfig("OnScreenButton_Skill2", "<Gamepad>/buttonNorth", DefaultButtonSize, "Skill2 (Leash)"),
            new SkillButtonConfig("OnScreenButton_Skill3", "<Gamepad>/buttonEast", DefaultButtonSize, "Skill3 (CalmingSpray)"),
            new SkillButtonConfig("OnScreenButton_Capture", "<Gamepad>/buttonSouth", DefaultCaptureButtonSize, "Capture"),
            new SkillButtonConfig("OnScreenButton_Struggle", "<Gamepad>/rightTrigger", DefaultButtonSize, "Struggle"),
        };
        
        private struct SkillButtonConfig
        {
            public string PrefabName;
            public string ControlPath;
            public float Size;
            public string Description;
            
            public SkillButtonConfig(string name, string path, float size, string desc)
            {
                PrefabName = name;
                ControlPath = path;
                Size = size;
                Description = desc;
            }
        }
        
        [MenuItem("PetGrooming/Create Mobile UI/OnScreenButton Prefabs/Create All")]
        public static void CreateAllOnScreenButtonPrefabs()
        {
            EnsureFolderExists();
            
            foreach (var config in SkillConfigs)
            {
                CreateOnScreenButtonPrefab(config);
            }
            
            Debug.Log("[OnScreenButtonPrefabCreator] 所有 OnScreenButton prefabs 创建完成!");
        }
        
        [MenuItem("PetGrooming/Create Mobile UI/OnScreenButton Prefabs/Skill1 (CaptureNet)")]
        public static void CreateSkill1Button() => CreateOnScreenButtonPrefab(SkillConfigs[0]);
        
        [MenuItem("PetGrooming/Create Mobile UI/OnScreenButton Prefabs/Skill2 (Leash)")]
        public static void CreateSkill2Button() => CreateOnScreenButtonPrefab(SkillConfigs[1]);
        
        [MenuItem("PetGrooming/Create Mobile UI/OnScreenButton Prefabs/Skill3 (CalmingSpray)")]
        public static void CreateSkill3Button() => CreateOnScreenButtonPrefab(SkillConfigs[2]);
        
        [MenuItem("PetGrooming/Create Mobile UI/OnScreenButton Prefabs/Capture")]
        public static void CreateCaptureButton() => CreateOnScreenButtonPrefab(SkillConfigs[3]);
        
        [MenuItem("PetGrooming/Create Mobile UI/OnScreenButton Prefabs/Struggle")]
        public static void CreateStruggleButton() => CreateOnScreenButtonPrefab(SkillConfigs[4]);
        
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
        
        private static void CreateOnScreenButtonPrefab(SkillButtonConfig config)
        {
            string prefabPath = $"{FolderPath}/{config.PrefabName}.prefab";
            
            // 创建根 GameObject
            GameObject root = new GameObject(config.PrefabName);
            RectTransform rootRect = root.AddComponent<RectTransform>();
            
            // 配置根 RectTransform
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.sizeDelta = new Vector2(config.Size, config.Size);
            
            // 添加 CanvasRenderer
            root.AddComponent<CanvasRenderer>();
            
            // 添加背景 Image (Requirement 2.5: 视觉元素)
            Image backgroundImage = root.AddComponent<Image>();
            backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 0.7f);
            backgroundImage.raycastTarget = true;
            
            // 添加 OnScreenButton 组件 (Requirement 2.1)
            OnScreenButton onScreenButton = root.AddComponent<OnScreenButton>();
            
            // 配置 OnScreenButton (Requirement 2.2, 2.6)
            SerializedObject serializedButton = new SerializedObject(onScreenButton);
            serializedButton.FindProperty("m_ControlPath").stringValue = config.ControlPath;
            serializedButton.ApplyModifiedPropertiesWithoutUndo();
            
            // 添加 SkillButtonVisual 组件 (Requirement 2.5)
            SkillButtonVisual skillButtonVisual = root.AddComponent<SkillButtonVisual>();
            
            // 创建子元素
            GameObject glowObj = CreateGlowEffect(root.transform, config.Size);
            GameObject iconObj = CreateIcon(root.transform);
            GameObject cooldownOverlayObj = CreateCooldownOverlay(root.transform);
            GameObject cooldownTextObj = CreateCooldownText(root.transform);
            
            // 设置 SkillButtonVisual 引用
            SerializedObject serializedVisual = new SerializedObject(skillButtonVisual);
            serializedVisual.FindProperty("_iconImage").objectReferenceValue = iconObj.GetComponent<Image>();
            serializedVisual.FindProperty("_cooldownOverlay").objectReferenceValue = cooldownOverlayObj.GetComponent<Image>();
            serializedVisual.FindProperty("_cooldownText").objectReferenceValue = cooldownTextObj.GetComponent<TextMeshProUGUI>();
            serializedVisual.FindProperty("_glowEffect").objectReferenceValue = glowObj.GetComponent<Image>();
            serializedVisual.FindProperty("_readyColor").colorValue = Color.white;
            serializedVisual.FindProperty("_cooldownColor").colorValue = new Color(0.5f, 0.5f, 0.5f, 1f);
            serializedVisual.ApplyModifiedPropertiesWithoutUndo();
            
            // 保存为 Prefab
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            
            // 清理场景对象
            Object.DestroyImmediate(root);
            
            Debug.Log($"[OnScreenButtonPrefabCreator] {config.Description} prefab 创建成功: {prefabPath}");
        }
        
        private static GameObject CreateGlowEffect(Transform parent, float buttonSize)
        {
            GameObject glow = new GameObject("Glow");
            glow.transform.SetParent(parent, false);
            glow.transform.SetAsFirstSibling(); // 放在最底层
            glow.layer = LayerMask.NameToLayer("UI");
            
            RectTransform rect = glow.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(buttonSize * 1.2f, buttonSize * 1.2f); // 比按钮大 20%
            
            glow.AddComponent<CanvasRenderer>();
            
            Image image = glow.AddComponent<Image>();
            image.color = new Color(1f, 1f, 0.5f, 0.8f); // 黄色发光
            image.raycastTarget = false;
            image.enabled = false; // 默认隐藏
            
            return glow;
        }
        
        private static GameObject CreateIcon(Transform parent)
        {
            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(parent, false);
            icon.layer = LayerMask.NameToLayer("UI");
            
            RectTransform rect = icon.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.1f);
            rect.anchorMax = new Vector2(0.9f, 0.9f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            
            icon.AddComponent<CanvasRenderer>();
            
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
            
            RectTransform rect = overlay.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            
            overlay.AddComponent<CanvasRenderer>();
            
            Image image = overlay.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.6f);
            image.raycastTarget = false;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial360;
            image.fillOrigin = (int)Image.Origin360.Top;
            image.fillClockwise = false;
            image.fillAmount = 0f;
            image.enabled = false; // 默认隐藏
            
            return overlay;
        }
        
        private static GameObject CreateCooldownText(Transform parent)
        {
            GameObject textObj = new GameObject("CooldownText");
            textObj.transform.SetParent(parent, false);
            textObj.layer = LayerMask.NameToLayer("UI");
            
            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(60, 40);
            
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "";
            text.fontSize = 24;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            text.enabled = false; // 默认隐藏
            
            return textObj;
        }
        
        [MenuItem("PetGrooming/Create Mobile UI/OnScreenButton Prefabs/Validate All")]
        public static void ValidateAllOnScreenButtonPrefabs()
        {
            bool allValid = true;
            
            foreach (var config in SkillConfigs)
            {
                string prefabPath = $"{FolderPath}/{config.PrefabName}.prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                
                if (prefab == null)
                {
                    Debug.LogError($"[OnScreenButtonPrefabCreator] {config.PrefabName} prefab 未找到: {prefabPath}");
                    allValid = false;
                    continue;
                }
                
                // 检查 OnScreenButton 组件
                OnScreenButton button = prefab.GetComponent<OnScreenButton>();
                if (button == null)
                {
                    Debug.LogError($"[OnScreenButtonPrefabCreator] {config.PrefabName} 缺少 OnScreenButton 组件!");
                    allValid = false;
                    continue;
                }
                
                // 检查 controlPath
                SerializedObject serializedButton = new SerializedObject(button);
                string controlPath = serializedButton.FindProperty("m_ControlPath").stringValue;
                if (controlPath != config.ControlPath)
                {
                    Debug.LogWarning($"[OnScreenButtonPrefabCreator] {config.PrefabName} controlPath 不匹配: {controlPath} != {config.ControlPath}");
                }
                
                // 检查 SkillButtonVisual 组件
                SkillButtonVisual visual = prefab.GetComponent<SkillButtonVisual>();
                if (visual == null)
                {
                    Debug.LogError($"[OnScreenButtonPrefabCreator] {config.PrefabName} 缺少 SkillButtonVisual 组件!");
                    allValid = false;
                    continue;
                }
                
                // 检查子元素
                if (prefab.transform.Find("Icon") == null)
                {
                    Debug.LogWarning($"[OnScreenButtonPrefabCreator] {config.PrefabName} 缺少 Icon 子对象");
                }
                if (prefab.transform.Find("CooldownOverlay") == null)
                {
                    Debug.LogWarning($"[OnScreenButtonPrefabCreator] {config.PrefabName} 缺少 CooldownOverlay 子对象");
                }
                if (prefab.transform.Find("CooldownText") == null)
                {
                    Debug.LogWarning($"[OnScreenButtonPrefabCreator] {config.PrefabName} 缺少 CooldownText 子对象");
                }
                if (prefab.transform.Find("Glow") == null)
                {
                    Debug.LogWarning($"[OnScreenButtonPrefabCreator] {config.PrefabName} 缺少 Glow 子对象");
                }
                
                Debug.Log($"[OnScreenButtonPrefabCreator] {config.PrefabName} 验证通过");
            }
            
            if (allValid)
            {
                Debug.Log("[OnScreenButtonPrefabCreator] 所有 OnScreenButton prefabs 验证通过!");
            }
        }
    }
}
