# Requirements Document

## Introduction

本文档定义了将现有自定义移动端控制逻辑迁移到 Unity 官方 Input System 方案的需求。目标是使用 Unity 官方提供的 `OnScreenStick` 和 `OnScreenButton` 组件替代自定义的 `VirtualJoystick`、`MobileSkillButton` 等实现，确保与 `ThirdPersonController` 和 `StarterAssetsInputs` 的无缝集成。

## Glossary

- **OnScreenStick**: Unity Input System 官方提供的屏幕虚拟摇杆组件，继承自 `OnScreenControl`
- **OnScreenButton**: Unity Input System 官方提供的屏幕虚拟按钮组件，继承自 `OnScreenControl`
- **StarterAssetsInputs**: Unity Starter Assets 提供的输入处理组件，管理 move、look、jump、sprint 等输入
- **ThirdPersonController**: Unity Starter Assets 提供的第三人称角色控制器，读取 StarterAssetsInputs 进行移动
- **PlayerInput**: Unity Input System 的核心组件，管理输入动作映射
- **InputActionAsset**: 定义输入动作和绑定的资产文件（如 StarterAssets.inputactions）
- **VirtualJoystick**: 当前自定义的虚拟摇杆实现（待废弃）
- **MobileSkillButton**: 当前自定义的技能按钮实现（待废弃）
- **MobileHUDManager**: 当前自定义的移动端 HUD 管理器（待重构）

## Requirements

### Requirement 1: 虚拟摇杆迁移到 OnScreenStick

**User Story:** As a developer, I want to replace the custom VirtualJoystick with Unity's OnScreenStick, so that the joystick input integrates seamlessly with the official Input System.

#### Acceptance Criteria

1. THE system SHALL use Unity's `OnScreenStick` component instead of custom `VirtualJoystick`
2. THE OnScreenStick SHALL bind to the "Move" action in StarterAssets.inputactions
3. WHEN the player drags the OnScreenStick, THE StarterAssetsInputs.move SHALL receive the normalized input vector
4. THE OnScreenStick SHALL support configurable movement range (movementRange property)
5. THE OnScreenStick visual appearance SHALL match the original design (background circle + handle)
6. WHEN the player releases the stick, THE handle SHALL return to center position
7. THE OnScreenStick SHALL work correctly with ThirdPersonController without additional code

### Requirement 2: 技能按钮迁移到 OnScreenButton

**User Story:** As a developer, I want to use Unity's OnScreenButton for skill activation, so that skill inputs integrate with the Input System action map.

#### Acceptance Criteria

1. THE system SHALL use Unity's `OnScreenButton` component for skill buttons
2. EACH OnScreenButton SHALL bind to a corresponding action in the Input Action Asset
3. THE StarterAssets.inputactions SHALL be extended with skill actions (Skill1, Skill2, Skill3, Capture)
4. WHEN a skill button is pressed, THE corresponding Input Action SHALL be triggered
5. THE skill button visual feedback (cooldown overlay, glow) SHALL be handled by a separate UI component
6. THE OnScreenButton SHALL support configurable control path binding

### Requirement 3: 删除自定义移动逻辑

**User Story:** As a developer, I want to remove the custom PlayerMovement script, so that all movement is handled by the official ThirdPersonController.

#### Acceptance Criteria

1. THE custom `PlayerMovement` script SHALL be removed or deprecated
2. THE `ThirdPersonController` SHALL be the sole movement controller for the player character
3. THE `GroomerController` SHALL reference `ThirdPersonController` instead of `PlayerMovement`
4. ALL movement-related code in custom scripts SHALL be removed
5. THE Demo.unity scene SHALL use `ThirdPersonController` for player movement

### Requirement 4: MobileHUDManager 重构

**User Story:** As a developer, I want to refactor MobileHUDManager to work with official Input System components, so that mobile UI management is simplified.

#### Acceptance Criteria

1. THE MobileHUDManager SHALL no longer directly send input to StarterAssetsInputs
2. THE MobileHUDManager SHALL manage UI visibility and layout only
3. THE joystick-to-movement integration SHALL be handled entirely by OnScreenStick binding
4. THE MobileHUDManager SHALL support enabling/disabling OnScreenControl components
5. THE skill cooldown display SHALL be decoupled from input handling

### Requirement 5: Demo.unity 场景更新

**User Story:** As a developer, I want the Demo.unity scene to use the official Input System setup, so that the game works correctly with the migrated controls.

#### Acceptance Criteria

1. THE player GameObject in Demo.unity SHALL have ThirdPersonController component
2. THE player GameObject SHALL have PlayerInput component with StarterAssets.inputactions
3. THE player GameObject SHALL have StarterAssetsInputs component
4. THE Mobile HUD Canvas SHALL contain OnScreenStick and OnScreenButton components
5. THE scene SHALL NOT reference deprecated custom movement scripts
6. THE scene SHALL work correctly on both desktop (keyboard/mouse) and mobile (touch)

### Requirement 6: Input Action Asset 扩展

**User Story:** As a developer, I want to extend the StarterAssets.inputactions with game-specific actions, so that all inputs are managed through the Input System.

#### Acceptance Criteria

1. THE StarterAssets.inputactions SHALL include Skill1, Skill2, Skill3 actions (Button type)
2. THE StarterAssets.inputactions SHALL include Capture action (Button type)
3. THE StarterAssets.inputactions SHALL include Struggle action (Button type)
4. EACH new action SHALL have keyboard bindings for desktop play
5. THE new actions SHALL be bindable to OnScreenButton components
6. THE StarterAssetsInputs script SHALL be extended to handle new skill actions

### Requirement 7: 保留技能 UI 视觉效果

**User Story:** As a player, I want skill buttons to still show cooldown and ready animations, so that I have visual feedback during gameplay.

#### Acceptance Criteria

1. THE skill button UI SHALL display radial cooldown overlay when skill is on cooldown
2. THE skill button UI SHALL show remaining cooldown time in seconds
3. WHEN a skill becomes ready, THE button SHALL play a glow/pulse animation
4. THE visual feedback components SHALL be separate from OnScreenButton input handling
5. THE SkillButtonVisual component SHALL observe skill state and update UI accordingly

