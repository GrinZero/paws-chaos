using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem.OnScreen;
using UnityEditor;

namespace PetGrooming.Setup.Editor
{
    /// <summary>
    /// Editor utility to create the OnScreenStick prefab.
    /// 使用 Unity Input System 官方的 OnScreenStick 组件替代自定义 VirtualJoystick。
    /// 
    /// Requirements: 1.1, 1.2, 1.4, 1.5
    /// - 1.1: 使用 Unity 的 OnScreenStick 组件
    /// - 1.2: OnScreenStick 绑定到 StarterAssets.inputactions 的 "Move" 动作
    /// - 1.4: 支持可配置的移动范围 (movementRange)
    /// - 1.5: 视觉外观匹配原始设计 (背景圆 + 手柄)
    /// </summary>
    public static class OnScreenStickPrefabCreator
    {
        private const string PrefabPath = "Assets/UI/MobileUI/OnScreenStick.prefab";
        private const string FolderPath = "Assets/UI/MobileUI";
        
        // 默认配置
        private const float DefaultBackgroundSize = 180f;
        private const float DefaultHandleSize = 70f;
        private const float DefaultMovementRange = 55f; // 背景半径 - 手柄半径/2
        
        [MenuItem("PetGrooming/Create Mobile UI/OnScreenStick Prefab")]
        public static void CreateOnScreenStickPrefab()
        {
            // 确保文件夹存在
            EnsureFolderExists();
            
            // 创建根 GameObject
            GameObject root = new GameObject("OnScreenStick");
            RectTransform rootRect = root.AddComponent<RectTransform>();
            
            // 配置根 RectTransform - 左下角锚点
            rootRect.anchorMin = new Vector2(0, 0);
            rootRect.anchorMax = new Vector2(0, 0);
            rootRect.pivot = new Vector2(0, 0);
            rootRect.anchoredPosition = new Vector2(150, 100);
            rootRect.sizeDelta = new Vector2(DefaultBackgroundSize, DefaultBackgroundSize);
            
            // 创建背景
            GameObject background = CreateBackground(root.transform);
            
            // 创建手柄 (OnScreenStick 组件添加在手柄上)
            GameObject handle = CreateHandle(background.transform);
            
            // 保存为 Prefab
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            
            // 清理场景对象
            Object.DestroyImmediate(root);
            
            // 选中创建的 Prefab
            Selection.activeObject = prefab;
            
            Debug.Log($"[OnScreenStickPrefabCreator] OnScreenStick prefab 创建成功: {PrefabPath}");
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
            
            // 添加 RectTransform
            RectTransform rect = background.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(DefaultBackgroundSize, DefaultBackgroundSize);
            
            // 添加 CanvasRenderer
            background.AddComponent<CanvasRenderer>();
            
            // 添加 Image 组件 (Requirement 1.5: 背景圆)
            Image image = background.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.5f); // 半透明深色
            image.raycastTarget = true; // 背景需要接收触摸事件
            
            return background;
        }
        
        private static GameObject CreateHandle(Transform parent)
        {
            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(parent, false);
            handle.layer = LayerMask.NameToLayer("UI");
            
            // 添加 RectTransform
            RectTransform rect = handle.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(DefaultHandleSize, DefaultHandleSize);
            
            // 添加 CanvasRenderer
            handle.AddComponent<CanvasRenderer>();
            
            // 添加 Image 组件 (Requirement 1.5: 手柄)
            Image image = handle.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.6f); // 白色半透明
            image.raycastTarget = true; // 手柄需要接收触摸事件以驱动 OnScreenStick
            
            // 添加 OnScreenStick 组件 (Requirement 1.1)
            OnScreenStick onScreenStick = handle.AddComponent<OnScreenStick>();
            
            // 配置 OnScreenStick
            // Requirement 1.2: 绑定到 <Gamepad>/leftStick，这会映射到 StarterAssets 的 Move 动作
            SerializedObject serializedStick = new SerializedObject(onScreenStick);
            serializedStick.FindProperty("m_ControlPath").stringValue = "<Gamepad>/leftStick";
            
            // Requirement 1.4: 配置移动范围
            serializedStick.FindProperty("m_MovementRange").floatValue = DefaultMovementRange;
            
            // 设置行为模式为相对位置
            // 0 = RelativePositionWithStaticOrigin
            // 1 = ExactPositionWithStaticOrigin
            // 2 = ExactPositionWithDynamicOrigin
            serializedStick.FindProperty("m_Behaviour").intValue = 0;
            
            serializedStick.ApplyModifiedPropertiesWithoutUndo();
            
            return handle;
        }
        
        [MenuItem("PetGrooming/Create Mobile UI/Validate OnScreenStick Prefab")]
        public static void ValidateOnScreenStickPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            
            if (prefab == null)
            {
                Debug.LogError($"[OnScreenStickPrefabCreator] OnScreenStick prefab 未找到: {PrefabPath}");
                return;
            }
            
            // 检查层级结构
            Transform background = prefab.transform.Find("Background");
            if (background == null)
            {
                Debug.LogError("[OnScreenStickPrefabCreator] Background 子对象未找到!");
                return;
            }
            
            Transform handle = background.Find("Handle");
            if (handle == null)
            {
                Debug.LogError("[OnScreenStickPrefabCreator] Handle 子对象未找到!");
                return;
            }
            
            // 检查 OnScreenStick 组件
            OnScreenStick onScreenStick = handle.GetComponent<OnScreenStick>();
            if (onScreenStick == null)
            {
                Debug.LogError("[OnScreenStickPrefabCreator] Handle 上未找到 OnScreenStick 组件!");
                return;
            }
            
            // 检查 controlPath 配置
            SerializedObject serializedStick = new SerializedObject(onScreenStick);
            string controlPath = serializedStick.FindProperty("m_ControlPath").stringValue;
            if (controlPath != "<Gamepad>/leftStick")
            {
                Debug.LogWarning($"[OnScreenStickPrefabCreator] controlPath 配置不正确: {controlPath}，应为 <Gamepad>/leftStick");
            }
            
            // 检查尺寸
            RectTransform bgRect = background.GetComponent<RectTransform>();
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            
            if (bgRect.sizeDelta.x < 150 || bgRect.sizeDelta.x > 200)
            {
                Debug.LogWarning($"[OnScreenStickPrefabCreator] 背景尺寸 {bgRect.sizeDelta.x} 超出推荐范围 (150-200)");
            }
            
            if (handleRect.sizeDelta.x < 60 || handleRect.sizeDelta.x > 80)
            {
                Debug.LogWarning($"[OnScreenStickPrefabCreator] 手柄尺寸 {handleRect.sizeDelta.x} 超出推荐范围 (60-80)");
            }
            
            Debug.Log("[OnScreenStickPrefabCreator] OnScreenStick prefab 验证通过!");
        }
    }
}
