using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem.OnScreen;
using UnityEditor;
using PetGrooming.UI.MobileUI;

namespace PetGrooming.Setup.Editor
{
    /// <summary>
    /// Editor utility to migrate MobileHUD Prefab to use Input System OnScreen controls.
    /// 将 VirtualJoystick 替换为 OnScreenStick，将 MobileSkillButton 替换为 OnScreenButton + SkillButtonVisual。
    /// 
    /// Requirements: 5.4
    /// - 5.4: Mobile HUD Canvas 包含 OnScreenStick 和 OnScreenButton 组件
    /// </summary>
    public static class MobileHUDMigrationEditor
    {
        private const string MobileHUDPrefabPath = "Assets/UI/MobileUI/MobileHUD.prefab";
        private const string OnScreenStickPrefabPath = "Assets/UI/MobileUI/OnScreenStick.prefab";
        
        // OnScreenButton Prefab 路径
        private static readonly string[] OnScreenButtonPrefabPaths = new string[]
        {
            "Assets/UI/MobileUI/OnScreenButton_Skill1.prefab",
            "Assets/UI/MobileUI/OnScreenButton_Skill2.prefab",
            "Assets/UI/MobileUI/OnScreenButton_Skill3.prefab",
            "Assets/UI/MobileUI/OnScreenButton_Capture.prefab",
            "Assets/UI/MobileUI/OnScreenButton_Struggle.prefab",
        };
        
        [MenuItem("PetGrooming/Migration/Update MobileHUD with OnScreen Controls")]
        public static void UpdateMobileHUDWithOnScreenControls()
        {
            // 加载 MobileHUD Prefab
            GameObject mobileHUDPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MobileHUDPrefabPath);
            if (mobileHUDPrefab == null)
            {
                Debug.LogError($"[MobileHUDMigration] MobileHUD prefab 未找到: {MobileHUDPrefabPath}");
                return;
            }
            
            // 实例化 Prefab 进行编辑
            GameObject instance = PrefabUtility.InstantiatePrefab(mobileHUDPrefab) as GameObject;
            if (instance == null)
            {
                Debug.LogError("[MobileHUDMigration] 无法实例化 MobileHUD prefab");
                return;
            }
            
            try
            {
                // 获取 MobileHUDManager 组件
                MobileHUDManager hudManager = instance.GetComponent<MobileHUDManager>();
                if (hudManager == null)
                {
                    Debug.LogError("[MobileHUDMigration] MobileHUDManager 组件未找到");
                    return;
                }
                
                // 1. 添加 OnScreenStick 到现有的 VirtualJoystick 对象
                AddOnScreenStickToJoystick(instance, hudManager);
                
                // 2. 添加 OnScreenButton 和 SkillButtonVisual 到技能按钮
                AddOnScreenButtonsToSkillButtons(instance, hudManager);
                
                // 3. 更新 MobileHUDManager 引用
                UpdateMobileHUDManagerReferences(hudManager);
                
                // 保存修改到 Prefab
                PrefabUtility.SaveAsPrefabAsset(instance, MobileHUDPrefabPath);
                
                Debug.Log("[MobileHUDMigration] MobileHUD prefab 更新成功!");
            }
            finally
            {
                // 清理实例
                Object.DestroyImmediate(instance);
            }
        }
        
        private static void AddOnScreenStickToJoystick(GameObject hudInstance, MobileHUDManager hudManager)
        {
            // 查找 VirtualJoystick 对象
            VirtualJoystick joystick = hudInstance.GetComponentInChildren<VirtualJoystick>(true);
            if (joystick == null)
            {
                Debug.LogWarning("[MobileHUDMigration] VirtualJoystick 未找到，跳过 OnScreenStick 添加");
                return;
            }
            
            // 查找 Handle 对象
            Transform background = joystick.transform.Find("Background");
            if (background == null)
            {
                Debug.LogWarning("[MobileHUDMigration] VirtualJoystick/Background 未找到");
                return;
            }
            
            Transform handle = background.Find("Handle");
            if (handle == null)
            {
                Debug.LogWarning("[MobileHUDMigration] VirtualJoystick/Background/Handle 未找到");
                return;
            }
            
            // 检查是否已有 OnScreenStick
            OnScreenStick existingStick = handle.GetComponent<OnScreenStick>();
            if (existingStick != null)
            {
                Debug.Log("[MobileHUDMigration] OnScreenStick 已存在，跳过添加");
                return;
            }
            
            // 添加 OnScreenStick 组件
            OnScreenStick onScreenStick = handle.gameObject.AddComponent<OnScreenStick>();
            
            // 配置 OnScreenStick
            SerializedObject serializedStick = new SerializedObject(onScreenStick);
            serializedStick.FindProperty("m_ControlPath").stringValue = "<Gamepad>/leftStick";
            serializedStick.FindProperty("m_MovementRange").floatValue = 55f;
            serializedStick.FindProperty("m_Behaviour").intValue = 0; // RelativePositionWithStaticOrigin
            serializedStick.ApplyModifiedPropertiesWithoutUndo();
            
            // 更新 MobileHUDManager 引用
            SerializedObject serializedManager = new SerializedObject(hudManager);
            serializedManager.FindProperty("_onScreenStick").objectReferenceValue = onScreenStick;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();
            
            Debug.Log("[MobileHUDMigration] OnScreenStick 添加成功");
        }
        
        private static void AddOnScreenButtonsToSkillButtons(GameObject hudInstance, MobileHUDManager hudManager)
        {
            // 查找所有 MobileSkillButton
            MobileSkillButton[] skillButtons = hudInstance.GetComponentsInChildren<MobileSkillButton>(true);
            
            if (skillButtons.Length == 0)
            {
                Debug.LogWarning("[MobileHUDMigration] 未找到 MobileSkillButton");
                return;
            }
            
            // 定义按钮名称到 controlPath 的映射
            var buttonMappings = new System.Collections.Generic.Dictionary<string, string>
            {
                { "CaptureButton", "<Gamepad>/buttonSouth" },
                { "SkillButton_CaptureNet", "<Gamepad>/buttonWest" },
                { "SkillButton_Leash", "<Gamepad>/buttonNorth" },
                { "SkillButton_CalmingSpray", "<Gamepad>/buttonEast" },
            };
            
            var onScreenButtons = new System.Collections.Generic.List<OnScreenButton>();
            var skillButtonVisuals = new System.Collections.Generic.List<SkillButtonVisual>();
            
            foreach (var skillButton in skillButtons)
            {
                string buttonName = skillButton.gameObject.name;
                string controlPath = null;
                
                // 查找匹配的 controlPath
                foreach (var mapping in buttonMappings)
                {
                    if (buttonName.Contains(mapping.Key))
                    {
                        controlPath = mapping.Value;
                        break;
                    }
                }
                
                if (controlPath == null)
                {
                    Debug.LogWarning($"[MobileHUDMigration] 未找到 {buttonName} 的 controlPath 映射");
                    continue;
                }
                
                // 检查是否已有 OnScreenButton
                OnScreenButton existingButton = skillButton.GetComponent<OnScreenButton>();
                if (existingButton == null)
                {
                    // 添加 OnScreenButton 组件
                    existingButton = skillButton.gameObject.AddComponent<OnScreenButton>();
                    
                    // 配置 OnScreenButton
                    SerializedObject serializedButton = new SerializedObject(existingButton);
                    serializedButton.FindProperty("m_ControlPath").stringValue = controlPath;
                    serializedButton.ApplyModifiedPropertiesWithoutUndo();
                    
                    Debug.Log($"[MobileHUDMigration] OnScreenButton 添加到 {buttonName}");
                }
                
                onScreenButtons.Add(existingButton);
                
                // 检查是否已有 SkillButtonVisual
                SkillButtonVisual existingVisual = skillButton.GetComponent<SkillButtonVisual>();
                if (existingVisual == null)
                {
                    // 添加 SkillButtonVisual 组件
                    existingVisual = skillButton.gameObject.AddComponent<SkillButtonVisual>();
                    
                    // 尝试从 MobileSkillButton 获取引用并设置到 SkillButtonVisual
                    SerializedObject serializedSkillButton = new SerializedObject(skillButton);
                    SerializedObject serializedVisual = new SerializedObject(existingVisual);
                    
                    // 复制引用
                    var iconImage = serializedSkillButton.FindProperty("_iconImage").objectReferenceValue;
                    var cooldownOverlay = serializedSkillButton.FindProperty("_cooldownOverlay").objectReferenceValue;
                    var cooldownText = serializedSkillButton.FindProperty("_cooldownText").objectReferenceValue;
                    var glowEffect = serializedSkillButton.FindProperty("_glowEffect").objectReferenceValue;
                    
                    serializedVisual.FindProperty("_iconImage").objectReferenceValue = iconImage;
                    serializedVisual.FindProperty("_cooldownOverlay").objectReferenceValue = cooldownOverlay;
                    serializedVisual.FindProperty("_cooldownText").objectReferenceValue = cooldownText;
                    serializedVisual.FindProperty("_glowEffect").objectReferenceValue = glowEffect;
                    serializedVisual.FindProperty("_readyColor").colorValue = Color.white;
                    serializedVisual.FindProperty("_cooldownColor").colorValue = new Color(0.5f, 0.5f, 0.5f, 1f);
                    serializedVisual.ApplyModifiedPropertiesWithoutUndo();
                    
                    Debug.Log($"[MobileHUDMigration] SkillButtonVisual 添加到 {buttonName}");
                }
                
                skillButtonVisuals.Add(existingVisual);
            }
            
            // 更新 MobileHUDManager 引用
            SerializedObject serializedManager = new SerializedObject(hudManager);
            
            // 设置 OnScreenButton 数组
            SerializedProperty skillButtonsProperty = serializedManager.FindProperty("_skillButtons");
            skillButtonsProperty.arraySize = onScreenButtons.Count;
            for (int i = 0; i < onScreenButtons.Count; i++)
            {
                skillButtonsProperty.GetArrayElementAtIndex(i).objectReferenceValue = onScreenButtons[i];
            }
            
            // 设置 SkillButtonVisual 数组
            SerializedProperty skillButtonVisualsProperty = serializedManager.FindProperty("_skillButtonVisuals");
            skillButtonVisualsProperty.arraySize = skillButtonVisuals.Count;
            for (int i = 0; i < skillButtonVisuals.Count; i++)
            {
                skillButtonVisualsProperty.GetArrayElementAtIndex(i).objectReferenceValue = skillButtonVisuals[i];
            }
            
            serializedManager.ApplyModifiedPropertiesWithoutUndo();
        }
        
        private static void UpdateMobileHUDManagerReferences(MobileHUDManager hudManager)
        {
            // 查找并设置 OnScreenStick 引用
            OnScreenStick onScreenStick = hudManager.GetComponentInChildren<OnScreenStick>(true);
            if (onScreenStick != null)
            {
                SerializedObject serializedManager = new SerializedObject(hudManager);
                serializedManager.FindProperty("_onScreenStick").objectReferenceValue = onScreenStick;
                serializedManager.ApplyModifiedPropertiesWithoutUndo();
            }
            
            Debug.Log("[MobileHUDMigration] MobileHUDManager 引用更新完成");
        }
        
        [MenuItem("PetGrooming/Migration/Validate MobileHUD Migration")]
        public static void ValidateMobileHUDMigration()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MobileHUDPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[MobileHUDMigration] MobileHUD prefab 未找到: {MobileHUDPrefabPath}");
                return;
            }
            
            bool isValid = true;
            
            // 检查 MobileHUDManager
            MobileHUDManager hudManager = prefab.GetComponent<MobileHUDManager>();
            if (hudManager == null)
            {
                Debug.LogError("[MobileHUDMigration] MobileHUDManager 组件未找到");
                isValid = false;
            }
            
            // 检查 OnScreenStick
            OnScreenStick onScreenStick = prefab.GetComponentInChildren<OnScreenStick>(true);
            if (onScreenStick == null)
            {
                Debug.LogWarning("[MobileHUDMigration] OnScreenStick 组件未找到");
            }
            else
            {
                SerializedObject serializedStick = new SerializedObject(onScreenStick);
                string controlPath = serializedStick.FindProperty("m_ControlPath").stringValue;
                if (controlPath != "<Gamepad>/leftStick")
                {
                    Debug.LogWarning($"[MobileHUDMigration] OnScreenStick controlPath 不正确: {controlPath}");
                }
                else
                {
                    Debug.Log("[MobileHUDMigration] OnScreenStick 配置正确");
                }
            }
            
            // 检查 OnScreenButton
            OnScreenButton[] onScreenButtons = prefab.GetComponentsInChildren<OnScreenButton>(true);
            if (onScreenButtons.Length == 0)
            {
                Debug.LogWarning("[MobileHUDMigration] 未找到 OnScreenButton 组件");
            }
            else
            {
                Debug.Log($"[MobileHUDMigration] 找到 {onScreenButtons.Length} 个 OnScreenButton");
            }
            
            // 检查 SkillButtonVisual
            SkillButtonVisual[] skillButtonVisuals = prefab.GetComponentsInChildren<SkillButtonVisual>(true);
            if (skillButtonVisuals.Length == 0)
            {
                Debug.LogWarning("[MobileHUDMigration] 未找到 SkillButtonVisual 组件");
            }
            else
            {
                Debug.Log($"[MobileHUDMigration] 找到 {skillButtonVisuals.Length} 个 SkillButtonVisual");
            }
            
            // 检查 MobileHUDManager 引用
            if (hudManager != null)
            {
                SerializedObject serializedManager = new SerializedObject(hudManager);
                
                var stickRef = serializedManager.FindProperty("_onScreenStick").objectReferenceValue;
                if (stickRef == null)
                {
                    Debug.LogWarning("[MobileHUDMigration] MobileHUDManager._onScreenStick 引用为空");
                }
                
                var buttonsArray = serializedManager.FindProperty("_skillButtons");
                if (buttonsArray.arraySize == 0)
                {
                    Debug.LogWarning("[MobileHUDMigration] MobileHUDManager._skillButtons 数组为空");
                }
                
                var visualsArray = serializedManager.FindProperty("_skillButtonVisuals");
                if (visualsArray.arraySize == 0)
                {
                    Debug.LogWarning("[MobileHUDMigration] MobileHUDManager._skillButtonVisuals 数组为空");
                }
            }
            
            if (isValid)
            {
                Debug.Log("[MobileHUDMigration] MobileHUD 迁移验证完成");
            }
        }
    }
}
