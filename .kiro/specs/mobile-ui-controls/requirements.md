# Requirements Document

## Introduction

本文档定义了宠物洗护游戏移动端 UI 控制系统的需求。目标是实现类似王者荣耀风格的移动端操作界面，包括左侧虚拟摇杆和右侧大型圆形技能按钮，提供流畅的触屏游戏体验。

## Glossary

- **Virtual_Joystick（虚拟摇杆）**: 屏幕左下角的触摸控制区域，用于控制角色移动方向和速度
- **Skill_Button（技能按钮）**: 屏幕右下角的圆形触摸按钮，用于释放技能
- **Capture_Button（抓取按钮）**: 洗护师的主要交互按钮，用于抓取萌宠
- **Struggle_Button（挣扎按钮）**: 萌宠被抓取时显示的按钮，快速点击可增加逃脱几率
- **Touch_Area（触摸区域）**: 可响应触摸输入的 UI 区域
- **Joystick_Handle（摇杆手柄）**: 虚拟摇杆中可拖动的内圈部分
- **Joystick_Background（摇杆背景）**: 虚拟摇杆的外圈背景，定义可移动范围
- **Skill_Wheel（技能轮盘）**: 右下角技能按钮的扇形布局容器
- **Cooldown_Indicator（冷却指示器）**: 技能按钮上显示冷却进度的视觉效果
- **Mobile_HUD（移动端界面）**: 针对触屏设备优化的游戏界面布局
- **Skill_Icon（技能图标）**: 技能按钮上显示的可视化图标，用于区分不同技能

## Requirements

### Requirement 1: 虚拟摇杆控制

**User Story:** As a mobile player, I want a virtual joystick on the left side of the screen, so that I can control my character's movement with touch input.

#### Acceptance Criteria

1. THE Virtual_Joystick SHALL be positioned in the bottom-left corner of the screen with configurable offset
2. THE Virtual_Joystick SHALL consist of a circular background (150-200 pixels diameter) and a draggable handle (60-80 pixels diameter)
3. WHEN the player touches within the Joystick_Background, THE Joystick_Handle SHALL move to the touch position
4. WHEN the player drags the Joystick_Handle, THE system SHALL output a normalized direction vector (-1 to 1 on both axes)
5. WHEN the Joystick_Handle is dragged beyond the background radius, THE handle SHALL be clamped to the edge of the background
6. WHEN the player releases the touch, THE Joystick_Handle SHALL return to center position with smooth animation
7. THE Virtual_Joystick SHALL support dynamic repositioning (joystick appears where player first touches within designated zone)
8. THE movement input from Virtual_Joystick SHALL be equivalent to keyboard/gamepad input for character movement

### Requirement 2: 洗护师技能按钮布局

**User Story:** As a mobile player controlling the Groomer, I want large circular skill buttons arranged like Honor of Kings, so that I can easily activate skills during fast-paced gameplay.

#### Acceptance Criteria

1. THE Groomer Skill_Button layout SHALL be positioned in the bottom-right corner of the screen
2. THE primary Capture_Button (抓取按钮) SHALL be the largest button (140 pixels diameter) at the bottom-right corner
3. THE three skill buttons (Capture_Net, Leash, Calming_Spray) SHALL be arranged in an arc above the Capture_Button (100 pixels diameter each)
4. WHEN the Capture_Button is pressed, THE Groomer SHALL attempt to capture a nearby pet
5. WHEN a Skill_Button is pressed, THE corresponding skill SHALL be activated immediately
6. THE Skill_Button SHALL display a radial cooldown overlay when the skill is on cooldown
7. WHEN a skill is on cooldown, THE Skill_Button SHALL show remaining cooldown time in seconds at the center
8. WHEN a skill becomes ready, THE Skill_Button SHALL play a glow/pulse animation
9. THE Skill_Button SHALL have a semi-transparent background with skill icon clearly visible

### Requirement 2.5: 萌宠挣扎按钮

**User Story:** As a player controlling a pet (in multiplayer mode), I want a struggle button when captured, so that I can attempt to escape from the Groomer.

#### Acceptance Criteria

1. WHEN a Pet is captured, THE Mobile_HUD SHALL display a large Struggle_Button (160 pixels diameter) at the center-right of the screen
2. THE Struggle_Button SHALL show a "挣扎" (Struggle) icon with clear visual design
3. WHEN the player rapidly taps the Struggle_Button, THE Pet's escape chance SHALL increase
4. THE Struggle_Button SHALL display a progress indicator showing escape attempt progress
5. WHEN the Pet is not captured, THE Struggle_Button SHALL be hidden
6. THE Struggle_Button SHALL have a distinct color (orange/red) to indicate urgency

### Requirement 3: 技能按钮布局

**User Story:** As a mobile player, I want skill buttons arranged ergonomically, so that I can reach all skills comfortably with my right thumb.

#### Acceptance Criteria

1. THE Capture_Button (抓取) SHALL be positioned at the bottom-right corner (largest button, 140 pixels)
2. THE three skill buttons SHALL be arranged in an arc above and to the left of the Capture_Button
3. THE spacing between Skill_Buttons SHALL be at least 20 pixels to prevent accidental touches
4. THE Skill_Wheel layout SHALL support 3-4 skill buttons in the arc arrangement
5. EACH Skill_Button position SHALL be configurable through settings
6. THE layout SHALL adapt to different screen sizes and aspect ratios

### Requirement 3.5: 技能图标设计

**User Story:** As a player, I want distinct skill icons for each ability, so that I can quickly identify and use the correct skill.

#### Acceptance Criteria

1. THE Capture_Net skill icon SHALL display a net/mesh visual with blue color theme
2. THE Leash skill icon SHALL display a rope/hook visual with green color theme
3. THE Calming_Spray skill icon SHALL display a spray bottle/mist visual with purple color theme
4. THE Capture_Button icon SHALL display a hand/grab visual with yellow/gold color theme
5. THE Struggle_Button icon SHALL display breaking chains/escape visual with orange/red color theme
6. EACH skill icon SHALL be clearly distinguishable at 100 pixel size
7. THE icons SHALL use consistent art style matching the game's visual theme
8. WHEN a skill is on cooldown, THE icon SHALL be desaturated (grayscale) with cooldown overlay

### Requirement 4: 触摸反馈

**User Story:** As a mobile player, I want visual and haptic feedback when I interact with controls, so that I know my inputs are registered.

#### Acceptance Criteria

1. WHEN the player touches a Skill_Button, THE button SHALL scale down slightly (95% scale) as press feedback
2. WHEN the player releases a Skill_Button, THE button SHALL return to normal scale with bounce animation
3. WHEN a skill is successfully activated, THE Skill_Button SHALL flash briefly
4. WHEN a skill activation fails (on cooldown), THE Skill_Button SHALL shake briefly and show "冷却中" text
5. WHEN the Virtual_Joystick is active, THE Joystick_Handle SHALL have increased opacity
6. THE system SHALL support optional haptic feedback on skill activation (device permitting)

### Requirement 5: UI 适配与切换

**User Story:** As a player, I want the game to automatically switch between mobile and desktop UI, so that I get the best experience on any device.

#### Acceptance Criteria

1. WHEN running on a touch-enabled device, THE system SHALL automatically enable Mobile_HUD
2. WHEN running on desktop without touch, THE system SHALL use the standard keyboard/mouse UI
3. THE player SHALL be able to manually toggle between Mobile_HUD and desktop UI in settings
4. WHEN Mobile_HUD is enabled, THE standard skill bar at bottom-center SHALL be hidden
5. WHEN Mobile_HUD is disabled, THE Virtual_Joystick and Skill_Wheel SHALL be hidden
6. THE UI mode preference SHALL be saved and restored between sessions

### Requirement 6: 性能优化

**User Story:** As a mobile player, I want smooth UI performance, so that controls remain responsive during gameplay.

#### Acceptance Criteria

1. THE Virtual_Joystick input processing SHALL complete within 1 frame (no input lag)
2. THE Skill_Button touch detection SHALL use efficient raycasting with minimal overhead
3. THE cooldown animations SHALL use optimized shaders or UI components
4. THE Mobile_HUD SHALL maintain 60 FPS on target mobile devices
5. WHEN multiple touches are detected, THE system SHALL correctly handle simultaneous joystick and skill inputs

