# Design Document: Mobile UI Controls

## Overview

本设计文档描述宠物洗护游戏的移动端 UI 控制系统实现方案。系统采用王者荣耀风格的触屏操作界面，包括左侧虚拟摇杆和右侧技能轮盘，为移动端玩家提供流畅的游戏体验。

### Design Goals

1. **直观操作** - 符合移动端 MOBA 游戏的操作习惯
2. **响应迅速** - 输入延迟控制在 1 帧以内
3. **视觉清晰** - 技能图标可区分，状态一目了然
4. **适配灵活** - 支持不同屏幕尺寸和设备类型

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      MobileHUDManager                        │
│  (管理移动端 UI 的启用/禁用，协调各组件)                      │
└─────────────────────────────────────────────────────────────┘
                              │
          ┌───────────────────┼───────────────────┐
          ▼                   ▼                   ▼
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│ VirtualJoystick │  │  SkillWheel     │  │ StruggleButton  │
│ (虚拟摇杆)       │  │  (技能轮盘)     │  │ (挣扎按钮)      │
└─────────────────┘  └─────────────────┘  └─────────────────┘
         │                    │                    │
         ▼                    ▼                    ▼
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│ JoystickHandle  │  │ MobileSkillBtn  │  │ StruggleProgress│
│ JoystickBG      │  │ CaptureButton   │  │                 │
└─────────────────┘  └─────────────────┘  └─────────────────┘
```

## Components and Interfaces

### 1. MobileHUDManager

主控制器，管理移动端 UI 的生命周期。

```csharp
public class MobileHUDManager : MonoBehaviour
{
    // 单例访问
    public static MobileHUDManager Instance { get; private set; }
    
    // UI 组件引用
    [SerializeField] private VirtualJoystick _joystick;
    [SerializeField] private SkillWheelUI _skillWheel;
    [SerializeField] private StruggleButtonUI _struggleButton;
    [SerializeField] private GameObject _desktopUI;
    
    // 状态
    public bool IsMobileMode { get; private set; }
    
    // 公共方法
    public void EnableMobileHUD();
    public void DisableMobileHUD();
    public void SetControlledCharacter(CharacterType type); // Groomer or Pet
    public Vector2 GetMovementInput(); // 获取摇杆输入
}
```

### 2. VirtualJoystick

虚拟摇杆组件，处理触摸输入并输出移动向量。

```csharp
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    // 配置
    [SerializeField] private RectTransform _background;  // 外圈背景
    [SerializeField] private RectTransform _handle;      // 内圈手柄
    [SerializeField] private float _handleRange = 1f;    // 手柄移动范围系数
    [SerializeField] private bool _dynamicPosition;      // 是否动态定位
    
    // 输出
    public Vector2 Direction { get; private set; }       // 归一化方向 (-1 to 1)
    public float Magnitude { get; private set; }         // 力度 (0 to 1)
    
    // 事件
    public event Action<Vector2> OnJoystickMoved;
    public event Action OnJoystickReleased;
    
    // 接口实现
    public void OnPointerDown(PointerEventData eventData);
    public void OnDrag(PointerEventData eventData);
    public void OnPointerUp(PointerEventData eventData);
}
```

### 3. SkillWheelUI

技能轮盘组件，管理技能按钮的布局和交互。

```csharp
public class SkillWheelUI : MonoBehaviour
{
    // 按钮引用
    [SerializeField] private MobileSkillButton _captureButton;    // 抓取按钮
    [SerializeField] private MobileSkillButton[] _skillButtons;   // 技能按钮数组
    
    // 布局配置
    [SerializeField] private float _arcRadius = 150f;             // 弧形半径
    [SerializeField] private float _arcStartAngle = 135f;         // 起始角度
    [SerializeField] private float _arcSpan = 90f;                // 弧形跨度
    
    // 公共方法
    public void BindToGroomerSkills(GroomerSkillManager skillManager);
    public void SetButtonsInteractable(bool interactable);
    public void ShowCaptureButton(bool show);
}
```

### 4. MobileSkillButton

单个技能按钮组件，处理触摸和冷却显示。

```csharp
public class MobileSkillButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    // UI 元素
    [SerializeField] private Image _background;
    [SerializeField] private Image _iconImage;
    [SerializeField] private Image _cooldownOverlay;
    [SerializeField] private TextMeshProUGUI _cooldownText;
    [SerializeField] private Image _glowEffect;
    
    // 配置
    [SerializeField] private float _buttonSize = 100f;
    [SerializeField] private Color _readyColor = Color.white;
    [SerializeField] private Color _cooldownColor = Color.gray;
    
    // 状态
    public bool IsOnCooldown { get; private set; }
    public float RemainingCooldown { get; private set; }
    
    // 事件
    public event Action OnButtonPressed;
    public event Action OnButtonReleased;
    
    // 公共方法
    public void SetSkill(SkillBase skill);
    public void SetIcon(Sprite icon);
    public void UpdateCooldown(float remaining, float total);
    public void PlayReadyAnimation();
    public void PlayPressAnimation();
    public void PlayFailAnimation();
}
```

### 5. StruggleButtonUI

挣扎按钮组件，萌宠被抓时显示。

```csharp
public class StruggleButtonUI : MonoBehaviour, IPointerDownHandler
{
    // UI 元素
    [SerializeField] private Image _buttonImage;
    [SerializeField] private Image _progressRing;
    [SerializeField] private TextMeshProUGUI _promptText;
    
    // 配置
    [SerializeField] private float _buttonSize = 160f;
    [SerializeField] private int _tapsRequired = 10;
    [SerializeField] private float _tapWindow = 3f;
    
    // 状态
    public int CurrentTaps { get; private set; }
    public float Progress => (float)CurrentTaps / _tapsRequired;
    
    // 事件
    public event Action OnStruggleComplete;
    public event Action<float> OnProgressChanged;
    
    // 公共方法
    public void Show();
    public void Hide();
    public void ResetProgress();
}
```

## Data Models

### SkillIconData

技能图标配置数据。

```csharp
[CreateAssetMenu(fileName = "SkillIconData", menuName = "PetGrooming/Skill Icon Data")]
public class SkillIconData : ScriptableObject
{
    [System.Serializable]
    public class SkillIconEntry
    {
        public string skillId;
        public Sprite icon;
        public Color themeColor;
        public Sprite glowSprite;
    }
    
    public SkillIconEntry captureNet;      // 捕获网 - 蓝色
    public SkillIconEntry leash;           // 牵引绳 - 绿色
    public SkillIconEntry calmingSpray;    // 镇静喷雾 - 紫色
    public SkillIconEntry captureButton;   // 抓取 - 金黄色
    public SkillIconEntry struggleButton;  // 挣扎 - 橙红色
    
    public SkillIconEntry GetIconForSkill(string skillId);
}
```

### MobileHUDSettings

移动端 UI 配置。

```csharp
[CreateAssetMenu(fileName = "MobileHUDSettings", menuName = "PetGrooming/Mobile HUD Settings")]
public class MobileHUDSettings : ScriptableObject
{
    [Header("Joystick Settings")]
    public float joystickSize = 180f;
    public float handleSize = 70f;
    public Vector2 joystickOffset = new Vector2(150f, 150f);
    public bool dynamicJoystick = true;
    
    [Header("Skill Wheel Settings")]
    public float captureButtonSize = 140f;
    public float skillButtonSize = 100f;
    public float arcRadius = 150f;
    public Vector2 skillWheelOffset = new Vector2(-100f, 100f);
    
    [Header("Struggle Button Settings")]
    public float struggleButtonSize = 160f;
    public int struggleTapsRequired = 10;
    
    [Header("Animation Settings")]
    public float pressScale = 0.95f;
    public float readyPulseDuration = 0.3f;
    public float glowPulseSpeed = 2f;
}
```

## UI Layout Diagram

```
┌────────────────────────────────────────────────────────────────┐
│                         Game View                               │
│                                                                 │
│                                                                 │
│                                                                 │
│                                                                 │
│                                                                 │
│                                                                 │
│                                                                 │
│                                                                 │
│  ┌─────────┐                                      ┌───┐        │
│  │ ┌─────┐ │                                    ┌─┤ 3 │        │
│  │ │  ○  │ │  Joystick                       ┌──┤ └───┘        │
│  │ │     │ │  (Left)                       ┌─┤2 │              │
│  │ └─────┘ │                             ┌─┤1 │──┘              │
│  └─────────┘                             │ └──┘                 │
│                                          │  ┌─────┐             │
│                                          └──┤  ●  │ Capture    │
│                                             │     │ Button     │
│                                             └─────┘             │
└────────────────────────────────────────────────────────────────┘

Legend:
○ = Joystick Handle
● = Capture Button (largest)
1,2,3 = Skill Buttons (Capture Net, Leash, Calming Spray)
```

## Skill Icon Visual Design

| 技能 | 图标描述 | 主题色 | 视觉元素 |
|------|----------|--------|----------|
| 捕获网 (Capture Net) | 展开的网状图案 | #4A90D9 蓝色 | 网格线条，向外扩散 |
| 牵引绳 (Leash) | 绳索末端带钩子 | #5CB85C 绿色 | 弯曲绳索，金属钩 |
| 镇静喷雾 (Calming Spray) | 喷雾瓶喷出雾气 | #9B59B6 紫色 | 瓶身，雾气粒子 |
| 抓取 (Capture) | 张开的手掌 | #F5A623 金黄色 | 手掌轮廓，抓取姿势 |
| 挣扎 (Struggle) | 挣脱的锁链 | #E74C3C 橙红色 | 断裂锁链，动态线条 |



## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system—essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Joystick Direction Normalization

*For any* drag position on the virtual joystick, the output direction vector SHALL have components within the range [-1, 1] on both X and Y axes.

**Validates: Requirements 1.4**

### Property 2: Joystick Handle Clamping

*For any* touch position outside the joystick background radius, the handle position SHALL be clamped to the edge of the background circle (distance from center equals radius).

**Validates: Requirements 1.5**

### Property 3: Skill Button Arc Arrangement

*For any* set of 3 skill buttons in the skill wheel, the buttons SHALL be positioned along an arc with consistent angular spacing, and all buttons SHALL be within the specified arc span from the start angle.

**Validates: Requirements 2.3, 3.2**

### Property 4: Cooldown Display State Consistency

*For any* skill button bound to a skill, when the skill's remaining cooldown is greater than 0, the cooldown overlay SHALL be visible AND the cooldown text SHALL display the remaining time (rounded up to nearest second).

**Validates: Requirements 2.6, 2.7**

### Property 5: Struggle Button Visibility

*For any* pet character state, the struggle button SHALL be visible if and only if the pet is in the captured state.

**Validates: Requirements 2.5.1, 2.5.5**

### Property 6: Struggle Progress Accumulation

*For any* sequence of N taps on the struggle button within the tap window, the progress value SHALL equal N divided by the required tap count (clamped to [0, 1]).

**Validates: Requirements 2.5.3, 2.5.4**

### Property 7: Button Spacing Minimum

*For any* pair of adjacent skill buttons in the skill wheel, the distance between their edges SHALL be at least 20 pixels.

**Validates: Requirements 3.3**

### Property 8: Screen Size Adaptation

*For any* screen size and aspect ratio, the skill wheel and joystick SHALL remain fully visible within the screen bounds and maintain their relative positions (joystick in bottom-left quadrant, skill wheel in bottom-right quadrant).

**Validates: Requirements 3.6**

### Property 9: Press Scale Feedback

*For any* skill button, when a pointer down event occurs, the button's scale SHALL be reduced to 95% of its original size.

**Validates: Requirements 4.1**

### Property 10: Joystick Active Opacity

*For any* joystick state, when the joystick is being touched (active), the handle's alpha SHALL be greater than when the joystick is idle.

**Validates: Requirements 4.5**

### Property 11: UI Mode Visibility Toggle

*For any* UI mode setting, when mobile mode is enabled, the desktop skill bar SHALL be hidden AND the mobile UI (joystick, skill wheel) SHALL be visible; when mobile mode is disabled, the inverse SHALL be true.

**Validates: Requirements 5.4, 5.5**

### Property 12: Multi-Touch Input Handling

*For any* simultaneous touch inputs on both the joystick and a skill button, both inputs SHALL be processed correctly (joystick outputs direction AND skill button triggers activation).

**Validates: Requirements 6.5**

## Error Handling

### Touch Input Errors

| Error Condition | Handling Strategy |
|-----------------|-------------------|
| Touch outside UI bounds | Ignore touch, no action |
| Rapid repeated taps | Debounce with minimum interval (50ms) |
| Touch ID lost mid-drag | Reset joystick to center, cancel action |
| Null skill reference | Log warning, disable button |

### State Errors

| Error Condition | Handling Strategy |
|-----------------|-------------------|
| Skill manager not found | Disable skill wheel, log error |
| Invalid cooldown value | Clamp to [0, maxCooldown] |
| Missing icon sprite | Use placeholder icon |
| Screen size too small | Scale down UI proportionally |

## Testing Strategy

### Unit Tests

Unit tests will verify specific examples and edge cases:

1. **Joystick center position** - Verify handle returns to (0, 0) on release
2. **Capture button size** - Verify button is exactly 140 pixels
3. **Skill button count** - Verify 3 skill buttons are created
4. **Cooldown text format** - Verify "5" displays for 4.1-5.0 seconds remaining
5. **Struggle button color** - Verify orange/red color values

### Property-Based Tests

Property-based tests will use NUnit with FsCheck for C# to verify universal properties:

**Testing Framework**: NUnit + FsCheck
**Minimum Iterations**: 100 per property test

Each property test will be tagged with:
- **Feature: mobile-ui-controls, Property {number}: {property_text}**

**Test File Structure**:
```
Assets/Scripts/PetGrooming/Tests/
├── MobileUI/
│   ├── VirtualJoystickTests.cs
│   ├── SkillWheelTests.cs
│   ├── StruggleButtonTests.cs
│   └── MobileHUDManagerTests.cs
```

### Integration Tests

1. **Joystick to Character Movement** - Verify joystick input moves character
2. **Skill Button to Skill Activation** - Verify button press activates skill
3. **Struggle Button to Escape** - Verify tap progress triggers escape attempt
4. **UI Mode Switch** - Verify switching modes shows/hides correct UI elements

