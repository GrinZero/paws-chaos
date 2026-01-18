using UnityEngine;

namespace PetGrooming.Core
{
    /// <summary>
    /// 包含移动 HUD 配置参数的 ScriptableObject。
    /// 定义虚拟摇杆、技能轮盘、挣扎按钮和动画的设置。
    /// 需求：1.1, 1.2, 2.2, 3.1
    /// </summary>
    [CreateAssetMenu(fileName = "MobileHUDSettings", menuName = "PetGrooming/MobileHUDSettings")]
    public class MobileHUDSettings : ScriptableObject
    {
        #region Joystick Settings
        [Header("Joystick Settings")]
        [Tooltip("摇杆背景的直径（以像素为单位，推荐 150-200）")]
        [Range(100f, 250f)]
        public float JoystickSize = 180f;
        
        [Tooltip("摇杆手柄的直径（以像素为单位，推荐 60-80）")]
        [Range(40f, 100f)]
        public float HandleSize = 70f;
        
        [Tooltip("从屏幕左下角的偏移量")]
        public Vector2 JoystickOffset = new Vector2(150f, 150f);
        
        [Tooltip("启用动态摇杆定位（出现在玩家首次触摸的位置）")]
        public bool DynamicJoystick = true;
        
        [Tooltip("手柄移动范围作为背景半径的倍数")]
        [Range(0.5f, 1.5f)]
        public float HandleRange = 1f;
        
        [Tooltip("释放时手柄返回中心的持续时间（秒）")]
        [Range(0.05f, 0.3f)]
        public float HandleReturnDuration = 0.1f;
        
        [Tooltip("摇杆手柄在空闲状态下的不透明度")]
        [Range(0.3f, 1f)]
        public float HandleIdleOpacity = 0.6f;
        
        [Tooltip("摇杆手柄在活动状态下的不透明度")]
        [Range(0.5f, 1f)]
        public float HandleActiveOpacity = 1f;
        #endregion

        #region Skill Wheel Settings
        [Header("Skill Wheel Settings")]
        [Tooltip("捕获按钮的直径（以像素为单位，最大的按钮）")]
        [Range(100f, 180f)]
        public float CaptureButtonSize = 140f;
        
        [Tooltip("技能按钮的直径（以像素为单位）")]
        [Range(60f, 140f)]
        public float SkillButtonSize = 100f;
        
        [Tooltip("技能按钮排列的弧形半径")]
        [Range(100f, 250f)]
        public float ArcRadius = 150f;
        
        [Tooltip("技能按钮弧形的起始角度（度，0 = 右，90 = 上）")]
        [Range(90f, 180f)]
        public float ArcStartAngle = 135f;
        
        [Tooltip("技能按钮弧形的总角度跨度（度）")]
        [Range(45f, 120f)]
        public float ArcSpan = 90f;
        
        [Tooltip("从屏幕右下角的偏移量")]
        public Vector2 SkillWheelOffset = new Vector2(-100f, 100f);
        
        [Tooltip("技能按钮之间的最小间距（以像素为单位）")]
        [Range(10f, 40f)]
        public float ButtonSpacing = 20f;
        #endregion

        #region Struggle Button Settings
        [Header("Struggle Button Settings")]
        [Tooltip("挣扎按钮的直径（以像素为单位）")]
        [Range(120f, 200f)]
        public float StruggleButtonSize = 160f;
        
        [Tooltip("完成挣扎所需的点击次数")]
        [Range(5, 20)]
        public int StruggleTapsRequired = 10;
        
        [Tooltip("挣扎点击的时间窗口（秒）")]
        [Range(1f, 5f)]
        public float StruggleTapWindow = 3f;
        
        [Tooltip("从屏幕中右侧的偏移量")]
        public Vector2 StruggleButtonOffset = new Vector2(-150f, 0f);
        #endregion

        #region Animation Settings
        [Header("Animation Settings")]
        [Tooltip("按钮按下时的缩放倍数")]
        [Range(0.85f, 0.99f)]
        public float PressScale = 0.95f;
        
        [Tooltip("按下缩放动画的持续时间（秒）")]
        [Range(0.05f, 0.2f)]
        public float PressAnimationDuration = 0.1f;
        
        [Tooltip("就绪脉冲动画的持续时间（秒）")]
        [Range(0.2f, 0.5f)]
        public float ReadyPulseDuration = 0.3f;
        
        [Tooltip("发光脉冲效果的速度")]
        [Range(1f, 4f)]
        public float GlowPulseSpeed = 2f;
        
        [Tooltip("失败摇晃动画的持续时间（秒）")]
        [Range(0.1f, 0.5f)]
        public float FailShakeDuration = 0.2f;
        
        [Tooltip("失败摇晃动画的强度")]
        [Range(5f, 20f)]
        public float FailShakeIntensity = 10f;
        #endregion

        #region Visual Settings
        [Header("Visual Settings")]
        [Tooltip("技能按钮的背景颜色")]
        public Color ButtonBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.7f);
        
        [Tooltip("冷却覆盖层的颜色")]
        public Color CooldownOverlayColor = new Color(0f, 0f, 0f, 0.6f);
        
        [Tooltip("就绪发光效果的颜色")]
        public Color ReadyGlowColor = new Color(1f, 1f, 0.5f, 0.8f);
        
        [Tooltip("挣扎按钮的颜色（橙红色主题）")]
        public Color StruggleButtonColor = new Color(0.9f, 0.3f, 0.2f, 1f);
        #endregion

        #region Input Settings
        [Header("Input Settings")]
        [Tooltip("按钮点击去抖动的最小间隔（秒）")]
        [Range(0.03f, 0.1f)]
        public float TapDebounceInterval = 0.05f;
        
        [Tooltip("技能激活时启用触觉反馈")]
        public bool EnableHapticFeedback = true;
        #endregion
    }
}
