# Implementation Plan: Mobile Input Migration

## Overview

将现有自定义移动端控制逻辑迁移到 Unity 官方 Input System 方案。使用 `OnScreenStick` 和 `OnScreenButton` 替代自定义实现，确保与 `ThirdPersonController` 无缝集成。

## Tasks

- [x] 1. 扩展 Input Action Asset
  - [x] 1.1 修改 StarterAssets.inputactions 添加技能动作
    - 添加 Skill1, Skill2, Skill3, Capture, Struggle 动作
    - 配置键盘绑定 (1, 2, 3, E, Space)
    - 配置手柄绑定 (buttonWest, buttonNorth, buttonEast, buttonSouth, rightTrigger)
    - _Requirements: 6.1, 6.2, 6.3, 6.4_

  - [x] 1.2 扩展 StarterAssetsInputs.cs 支持技能输入
    - 添加 skill1, skill2, skill3, capture, struggle 字段
    - 添加 OnSkill1, OnSkill2, OnSkill3, OnCapture, OnStruggle 回调方法
    - 添加技能按下事件 (OnSkill1Pressed 等)
    - _Requirements: 6.6_

- [x] 2. 创建 SkillButtonVisual 组件
  - [x] 2.1 创建 SkillButtonVisual.cs 脚本
    - 实现冷却显示逻辑 (radial overlay, cooldown text)
    - 实现就绪动画 (glow/pulse)
    - 实现按下动画 (scale feedback)
    - 提供 BindToSkill 方法绑定技能
    - _Requirements: 7.1, 7.2, 7.4, 7.5_

  - [x] 2.2 编写 SkillButtonVisual 属性测试
    - **Property 4: 技能按钮冷却显示同步**
    - **Validates: Requirements 7.1, 7.2, 7.5**

- [x] 3. 重构 MobileHUDManager
  - [x] 3.1 移除输入发送逻辑
    - 删除 UpdateMovementInput() 方法
    - 删除对 StarterAssetsInputs.MoveInput() 的直接调用
    - 删除对 PlayerMovement 的引用
    - _Requirements: 4.1, 4.2, 4.3_

  - [x] 3.2 添加 OnScreenControl 管理
    - 添加 OnScreenStick 和 OnScreenButton 引用
    - 实现 EnableMobileControls() / DisableMobileControls()
    - 更新 UI 可见性管理逻辑
    - _Requirements: 4.4_

  - [x] 3.3 编写 MobileHUDManager 属性测试
    - **Property 3: 控件启用/禁用状态同步**
    - **Validates: Requirements 4.4**

- [x] 4. 创建移动端 UI Prefab
  - [x] 4.1 创建 OnScreenStick Prefab
    - 创建 UI Image 作为摇杆背景
    - 创建子 UI Image 作为摇杆手柄
    - 在手柄上添加 OnScreenStick 组件
    - 配置 controlPath 为 `<Gamepad>/leftStick`
    - 配置 movementRange 为背景半径
    - _Requirements: 1.1, 1.2, 1.4, 1.5_

  - [x] 4.2 创建 OnScreenButton Prefabs
    - 为每个技能创建按钮 Prefab
    - 添加 OnScreenButton 组件并配置 controlPath
    - 添加 SkillButtonVisual 组件
    - 配置视觉元素 (icon, cooldown overlay, glow)
    - _Requirements: 2.1, 2.2, 2.5, 2.6_

  - [x] 4.3 更新 MobileHUD Prefab
    - 替换 VirtualJoystick 为 OnScreenStick
    - 替换 MobileSkillButton 为 OnScreenButton + SkillButtonVisual
    - 更新 MobileHUDManager 引用
    - _Requirements: 5.4_

- [x] 5. Checkpoint - 验证 Prefab 配置
  - 确保所有 Prefab 正确配置
  - 确保 OnScreenControl 组件的 controlPath 正确
  - 如有问题请询问用户

- [x] 6. 更新 Demo.unity 场景
  - [x] 6.1 配置玩家对象
    - 确保玩家有 ThirdPersonController 组件
    - 确保玩家有 PlayerInput 组件 (使用 StarterAssets.inputactions)
    - 确保玩家有 StarterAssetsInputs 组件
    - 移除 PlayerMovement 组件引用
    - _Requirements: 5.1, 5.2, 5.3_

  - [x] 6.2 更新 Mobile HUD Canvas
    - 实例化新的 MobileHUD Prefab
    - 配置 OnScreenStick 和 OnScreenButton
    - 绑定 SkillButtonVisual 到技能系统
    - _Requirements: 5.4_

  - [x] 6.3 更新 GroomerController 引用
    - 移除对 PlayerMovement 的引用
    - 添加对 ThirdPersonController 的引用（如需要）
    - 连接技能输入事件到技能管理器
    - _Requirements: 3.3_

- [x] 7. 清理废弃代码
  - [x] 7.1 标记废弃脚本
    - 在 VirtualJoystick.cs 添加 [Obsolete] 标记
    - 在 MobileSkillButton.cs 添加 [Obsolete] 标记
    - 在 PlayerMovement.cs 添加 [Obsolete] 标记
    - _Requirements: 3.1, 3.4_

  - [x] 7.2 更新相关脚本引用
    - 更新 GroomerController 移除 PlayerMovement 依赖
    - 更新其他引用废弃脚本的代码
    - _Requirements: 3.2, 3.3_

- [x] 8. Final Checkpoint - 集成测试
  - 在 Unity Editor 中运行 Demo 场景
  - 验证键盘/鼠标输入正常工作
  - 验证触摸输入正常工作（使用 Device Simulator）
  - 验证技能按钮冷却显示正常
  - 如有问题请询问用户

## Notes

- 所有任务都是必需的，包括测试任务
- 使用 Unity 6000.3.1f1 版本
- OnScreenStick 和 OnScreenButton 位于 `UnityEngine.InputSystem.OnScreen` 命名空间
- 需要确保项目已启用 Input System Package
