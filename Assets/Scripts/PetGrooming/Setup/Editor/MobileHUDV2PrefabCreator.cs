using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem.OnScreen;
using UnityEditor;
using TMPro;
using PetGrooming.UI.MobileUI;

namespace PetGrooming.Setup.Editor
{
    /// <summary>
    /// 创建使用新 Input System 组件的 MobileHUD V2 Prefab。
    /// 替换旧的 VirtualJoystick 和 SkillWheelUI，使用 OnScreenStick 和 OnScreenButton。
    /// </summary>
    public static class MobileHUDV2PrefabCreator
    {
        private const string OutputPath = "Assets/UI/MobileUI/MobileHUD_V2.prefab";
        
        [MenuItem("PetGrooming/Create MobileHUD V2 Prefab")]
        public static void CreateMobileHUDV2Prefab()
        {
            // 创建根 Canvas
            GameObject canvasObj = new GameObject("MobileHUD_V2");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            
            // 确保 RectTransform 正确设置
            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.localScale = Vector3.one; // 关键：Scale = 1
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100; // 修复：使用标准值
            
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // 添加 MobileHUDManager
            MobileHUDManager manager = canvasObj.AddComponent<MobileHUDManager>();
            
            // 创建 OnScreenStick（左下角）
            GameObject stickObj = CreateOnScreenStick(canvasObj.transform);
            OnScreenStick onScreenStick = stickObj.GetComponentInChildren<OnScreenStick>();
            
            // 创建技能按钮容器（右下角）- 王者荣耀风格布局
            GameObject skillButtonsContainer = CreateSkillButtonsContainer(canvasObj.transform);
            
            // 创建主按钮（大按钮，捕获/普攻）- 中心位置，放大
            var (captureBtn, captureVisual) = CreateMainSkillButton(skillButtonsContainer.transform, "Capture", "<Gamepad>/buttonSouth", Vector2.zero);
            
            // 创建 3 个技能按钮（小按钮，环绕主按钮）- 放大并调整位置
            var (skill1Btn, skill1Visual) = CreateSkillButton(skillButtonsContainer.transform, "Skill1", "<Gamepad>/buttonWest", new Vector2(-130, 90), 85);
            var (skill2Btn, skill2Visual) = CreateSkillButton(skillButtonsContainer.transform, "Skill2", "<Gamepad>/buttonNorth", new Vector2(0, 150), 85);
            var (skill3Btn, skill3Visual) = CreateSkillButton(skillButtonsContainer.transform, "Skill3", "<Gamepad>/buttonEast", new Vector2(130, 90), 85);
            
            // 创建跳跃按钮（左侧，摇杆上方）
            CreateJumpButton(canvasObj.transform);
            
            // 设置 MobileHUDManager 引用（注意顺序：Skill1, Skill2, Skill3, Capture）
            SetMobileHUDManagerReferences(manager, onScreenStick, 
                new OnScreenButton[] { skill1Btn, skill2Btn, skill3Btn, captureBtn },
                new SkillButtonVisual[] { skill1Visual, skill2Visual, skill3Visual, captureVisual });
            
            // 保存 Prefab
            string directory = System.IO.Path.GetDirectoryName(OutputPath);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            
            PrefabUtility.SaveAsPrefabAsset(canvasObj, OutputPath);
            Object.DestroyImmediate(canvasObj);
            
            Debug.Log($"[MobileHUDV2PrefabCreator] MobileHUD V2 Prefab 已创建: {OutputPath}");
            AssetDatabase.Refresh();
        }
        
        private static GameObject CreateOnScreenStick(Transform parent)
        {
            // 创建容器 - 向右移动避开刘海屏
            GameObject container = new GameObject("OnScreenStick");
            container.transform.SetParent(parent, false);
            
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 0);
            containerRect.anchorMax = new Vector2(0, 0);
            containerRect.pivot = new Vector2(0, 0);
            containerRect.anchoredPosition = new Vector2(120, 60); // 向右移动避开刘海
            containerRect.sizeDelta = new Vector2(280, 280); // 放大容器
            
            // 创建背景 - 放大
            GameObject background = new GameObject("Background");
            background.transform.SetParent(container.transform, false);
            background.layer = 5; // UI layer
            
            RectTransform bgRect = background.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.5f, 0.5f);
            bgRect.anchorMax = new Vector2(0.5f, 0.5f);
            bgRect.anchoredPosition = Vector2.zero;
            bgRect.sizeDelta = new Vector2(240, 240); // 放大背景
            
            background.AddComponent<CanvasRenderer>();
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.15f, 0.15f, 0.15f, 0.6f);
            
            // 创建手柄 - 放大
            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(background.transform, false);
            handle.layer = 5;
            
            RectTransform handleRect = handle.AddComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.anchoredPosition = Vector2.zero;
            handleRect.sizeDelta = new Vector2(100, 100); // 放大手柄
            
            handle.AddComponent<CanvasRenderer>();
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = new Color(0.3f, 0.6f, 1f, 0.9f);
            
            // 添加 OnScreenStick 组件到手柄
            OnScreenStick stick = handle.AddComponent<OnScreenStick>();
            var stickSO = new SerializedObject(stick);
            stickSO.FindProperty("m_ControlPath").stringValue = "<Gamepad>/leftStick";
            stickSO.FindProperty("m_MovementRange").floatValue = 70f; // 增大移动范围
            stickSO.ApplyModifiedPropertiesWithoutUndo();
            
            return container;
        }
        
        private static GameObject CreateSkillButtonsContainer(Transform parent)
        {
            // 技能按钮容器 - 向左移动避开刘海屏
            GameObject container = new GameObject("SkillButtons");
            container.transform.SetParent(parent, false);
            
            RectTransform rect = container.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(1, 0);
            rect.anchoredPosition = new Vector2(-120, 60); // 向左移动避开刘海
            rect.sizeDelta = new Vector2(350, 280); // 放大容器
            
            return container;
        }
        
        private static (OnScreenButton, SkillButtonVisual) CreateSkillButton(
            Transform parent, string name, string controlPath, Vector2 position, float size = 90)
        {
            // 创建按钮容器
            GameObject buttonObj = new GameObject($"SkillButton_{name}");
            buttonObj.transform.SetParent(parent, false);
            buttonObj.layer = 5;
            
            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(size, size);
            
            buttonObj.AddComponent<CanvasRenderer>();
            
            // 背景 Image
            Image bgImage = buttonObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.85f);
            
            // OnScreenButton 组件
            OnScreenButton onScreenButton = buttonObj.AddComponent<OnScreenButton>();
            // 使用 SerializedObject 设置 controlPath（确保序列化正确保存）
            var buttonSO = new SerializedObject(onScreenButton);
            buttonSO.FindProperty("m_ControlPath").stringValue = controlPath;
            buttonSO.ApplyModifiedPropertiesWithoutUndo();
            
            // 创建发光效果
            GameObject glowObj = new GameObject("Glow");
            glowObj.transform.SetParent(buttonObj.transform, false);
            glowObj.layer = 5;
            
            RectTransform glowRect = glowObj.AddComponent<RectTransform>();
            glowRect.anchorMin = new Vector2(0.5f, 0.5f);
            glowRect.anchorMax = new Vector2(0.5f, 0.5f);
            glowRect.anchoredPosition = Vector2.zero;
            glowRect.sizeDelta = new Vector2(size + 20, size + 20);
            
            glowObj.AddComponent<CanvasRenderer>();
            Image glowImage = glowObj.AddComponent<Image>();
            glowImage.color = new Color(1f, 1f, 0.5f, 0.8f);
            glowImage.enabled = false;
            
            // 创建图标
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(buttonObj.transform, false);
            iconObj.layer = 5;
            
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.15f, 0.15f);
            iconRect.anchorMax = new Vector2(0.85f, 0.85f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = Vector2.zero;
            
            iconObj.AddComponent<CanvasRenderer>();
            Image iconImage = iconObj.AddComponent<Image>();
            iconImage.color = Color.white;
            iconImage.raycastTarget = false;
            iconImage.preserveAspect = true;
            
            // 创建冷却遮罩
            GameObject overlayObj = new GameObject("CooldownOverlay");
            overlayObj.transform.SetParent(buttonObj.transform, false);
            overlayObj.layer = 5;
            
            RectTransform overlayRect = overlayObj.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.anchoredPosition = Vector2.zero;
            overlayRect.sizeDelta = Vector2.zero;
            
            overlayObj.AddComponent<CanvasRenderer>();
            Image overlayImage = overlayObj.AddComponent<Image>();
            overlayImage.color = new Color(0, 0, 0, 0.7f);
            overlayImage.type = Image.Type.Filled;
            overlayImage.fillMethod = Image.FillMethod.Radial360;
            overlayImage.fillOrigin = (int)Image.Origin360.Top;
            overlayImage.fillClockwise = false;
            overlayImage.fillAmount = 0;
            overlayImage.raycastTarget = false;
            overlayImage.enabled = false;
            
            // 创建冷却文本
            GameObject textObj = new GameObject("CooldownText");
            textObj.transform.SetParent(buttonObj.transform, false);
            textObj.layer = 5;
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(60, 40);
            
            textObj.AddComponent<CanvasRenderer>();
            TextMeshProUGUI cooldownText = textObj.AddComponent<TextMeshProUGUI>();
            cooldownText.text = "";
            cooldownText.fontSize = size > 80 ? 28 : 20; // 根据按钮大小调整字体
            cooldownText.fontStyle = FontStyles.Bold;
            cooldownText.alignment = TextAlignmentOptions.Center;
            cooldownText.color = Color.white;
            cooldownText.raycastTarget = false;
            cooldownText.enabled = false;
            
            // 添加 SkillButtonVisual 组件
            SkillButtonVisual visual = buttonObj.AddComponent<SkillButtonVisual>();
            
            // 使用 SerializedObject 设置私有字段（确保序列化正确保存）
            var visualSO = new SerializedObject(visual);
            visualSO.FindProperty("_iconImage").objectReferenceValue = iconImage;
            visualSO.FindProperty("_cooldownOverlay").objectReferenceValue = overlayImage;
            visualSO.FindProperty("_cooldownText").objectReferenceValue = cooldownText;
            visualSO.FindProperty("_glowEffect").objectReferenceValue = glowImage;
            visualSO.ApplyModifiedPropertiesWithoutUndo();
            
            return (onScreenButton, visual);
        }
        
        /// <summary>
        /// 创建主技能按钮（大按钮，王者荣耀风格的普攻/捕获按钮）
        /// </summary>
        private static (OnScreenButton, SkillButtonVisual) CreateMainSkillButton(
            Transform parent, string name, string controlPath, Vector2 position)
        {
            float mainSize = 130; // 主按钮放大
            
            // 创建按钮容器
            GameObject buttonObj = new GameObject($"MainButton_{name}");
            buttonObj.transform.SetParent(parent, false);
            buttonObj.layer = 5;
            
            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(mainSize, mainSize);
            
            buttonObj.AddComponent<CanvasRenderer>();
            
            // 背景 Image - 主按钮用醒目的红色
            Image bgImage = buttonObj.AddComponent<Image>();
            bgImage.color = new Color(0.85f, 0.25f, 0.25f, 0.9f);
            
            // OnScreenButton 组件
            OnScreenButton onScreenButton = buttonObj.AddComponent<OnScreenButton>();
            var buttonSO = new SerializedObject(onScreenButton);
            buttonSO.FindProperty("m_ControlPath").stringValue = controlPath;
            buttonSO.ApplyModifiedPropertiesWithoutUndo();
            
            // 创建发光效果
            GameObject glowObj = new GameObject("Glow");
            glowObj.transform.SetParent(buttonObj.transform, false);
            glowObj.layer = 5;
            
            RectTransform glowRect = glowObj.AddComponent<RectTransform>();
            glowRect.anchorMin = new Vector2(0.5f, 0.5f);
            glowRect.anchorMax = new Vector2(0.5f, 0.5f);
            glowRect.anchoredPosition = Vector2.zero;
            glowRect.sizeDelta = new Vector2(mainSize + 30, mainSize + 30);
            
            glowObj.AddComponent<CanvasRenderer>();
            Image glowImage = glowObj.AddComponent<Image>();
            glowImage.color = new Color(1f, 0.5f, 0.3f, 0.8f); // 橙红色发光
            glowImage.enabled = false;
            
            // 创建图标
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(buttonObj.transform, false);
            iconObj.layer = 5;
            
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.15f, 0.15f);
            iconRect.anchorMax = new Vector2(0.85f, 0.85f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = Vector2.zero;
            
            iconObj.AddComponent<CanvasRenderer>();
            Image iconImage = iconObj.AddComponent<Image>();
            iconImage.color = Color.white;
            iconImage.raycastTarget = false;
            iconImage.preserveAspect = true;
            
            // 创建冷却遮罩
            GameObject overlayObj = new GameObject("CooldownOverlay");
            overlayObj.transform.SetParent(buttonObj.transform, false);
            overlayObj.layer = 5;
            
            RectTransform overlayRect = overlayObj.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.anchoredPosition = Vector2.zero;
            overlayRect.sizeDelta = Vector2.zero;
            
            overlayObj.AddComponent<CanvasRenderer>();
            Image overlayImage = overlayObj.AddComponent<Image>();
            overlayImage.color = new Color(0, 0, 0, 0.7f);
            overlayImage.type = Image.Type.Filled;
            overlayImage.fillMethod = Image.FillMethod.Radial360;
            overlayImage.fillOrigin = (int)Image.Origin360.Top;
            overlayImage.fillClockwise = false;
            overlayImage.fillAmount = 0;
            overlayImage.raycastTarget = false;
            overlayImage.enabled = false;
            
            // 创建冷却文本
            GameObject textObj = new GameObject("CooldownText");
            textObj.transform.SetParent(buttonObj.transform, false);
            textObj.layer = 5;
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(70, 50);
            
            textObj.AddComponent<CanvasRenderer>();
            TextMeshProUGUI cooldownText = textObj.AddComponent<TextMeshProUGUI>();
            cooldownText.text = "";
            cooldownText.fontSize = 32;
            cooldownText.fontStyle = FontStyles.Bold;
            cooldownText.alignment = TextAlignmentOptions.Center;
            cooldownText.color = Color.white;
            cooldownText.raycastTarget = false;
            cooldownText.enabled = false;
            
            // 添加 SkillButtonVisual 组件
            SkillButtonVisual visual = buttonObj.AddComponent<SkillButtonVisual>();
            
            var visualSO = new SerializedObject(visual);
            visualSO.FindProperty("_iconImage").objectReferenceValue = iconImage;
            visualSO.FindProperty("_cooldownOverlay").objectReferenceValue = overlayImage;
            visualSO.FindProperty("_cooldownText").objectReferenceValue = cooldownText;
            visualSO.FindProperty("_glowEffect").objectReferenceValue = glowImage;
            visualSO.ApplyModifiedPropertiesWithoutUndo();
            
            return (onScreenButton, visual);
        }
        
        /// <summary>
        /// 创建跳跃按钮（位于右下角，技能按钮左下方）
        /// </summary>
        private static void CreateJumpButton(Transform parent)
        {
            float size = 70;
            
            GameObject buttonObj = new GameObject("JumpButton");
            buttonObj.transform.SetParent(parent, false);
            buttonObj.layer = 5;
            
            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 0);  // 右下角锚点
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(-420, 60); // 技能按钮左下方，不重叠
            rect.sizeDelta = new Vector2(size, size);
            
            buttonObj.AddComponent<CanvasRenderer>();
            
            // 背景 - 蓝色跳跃按钮
            Image bgImage = buttonObj.AddComponent<Image>();
            bgImage.color = new Color(0.3f, 0.6f, 0.9f, 0.85f);
            
            // OnScreenButton - 绑定到 rightShoulder
            OnScreenButton onScreenButton = buttonObj.AddComponent<OnScreenButton>();
            var buttonSO = new SerializedObject(onScreenButton);
            buttonSO.FindProperty("m_ControlPath").stringValue = "<Gamepad>/rightShoulder";
            buttonSO.ApplyModifiedPropertiesWithoutUndo();
            
            // 跳跃图标文字
            GameObject textObj = new GameObject("JumpText");
            textObj.transform.SetParent(buttonObj.transform, false);
            textObj.layer = 5;
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = Vector2.zero;
            
            textObj.AddComponent<CanvasRenderer>();
            TextMeshProUGUI jumpText = textObj.AddComponent<TextMeshProUGUI>();
            jumpText.text = "跳";
            jumpText.fontSize = 28;
            jumpText.fontStyle = FontStyles.Bold;
            jumpText.alignment = TextAlignmentOptions.Center;
            jumpText.color = Color.white;
            jumpText.raycastTarget = false;
        }
        
        private static void SetMobileHUDManagerReferences(
            MobileHUDManager manager, 
            OnScreenStick stick,
            OnScreenButton[] buttons,
            SkillButtonVisual[] visuals)
        {
            // 使用 SerializedObject 设置私有字段（确保序列化正确保存）
            var managerSO = new SerializedObject(manager);
            managerSO.FindProperty("_onScreenStick").objectReferenceValue = stick;
            
            // 设置数组
            var buttonsProperty = managerSO.FindProperty("_skillButtons");
            buttonsProperty.arraySize = buttons.Length;
            for (int i = 0; i < buttons.Length; i++)
            {
                buttonsProperty.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
            }
            
            var visualsProperty = managerSO.FindProperty("_skillButtonVisuals");
            visualsProperty.arraySize = visuals.Length;
            for (int i = 0; i < visuals.Length; i++)
            {
                visualsProperty.GetArrayElementAtIndex(i).objectReferenceValue = visuals[i];
            }
            
            managerSO.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}