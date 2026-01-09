# Implementation Plan: Mobile UI Controls

## Overview

本实现计划将移动端 UI 控制系统分解为可执行的编码任务。采用增量开发方式，从核心组件开始，逐步构建完整的移动端操作界面。

## Tasks

- [x] 1. 创建基础配置和数据结构
  - [x] 1.1 创建 MobileHUDSettings ScriptableObject
    - 定义摇杆、技能轮盘、挣扎按钮的配置参数
    - 包含尺寸、位置、动画参数
    - _Requirements: 1.1, 1.2, 2.2, 3.1_
  - [x] 1.2 创建 SkillIconData ScriptableObject
    - 定义技能图标配置结构
    - 包含图标 Sprite、主题色、发光效果
    - _Requirements: 3.5.1-3.5.8_

- [x] 2. 实现虚拟摇杆组件
  - [x] 2.1 创建 VirtualJoystick 脚本
    - 实现 IPointerDownHandler, IDragHandler, IPointerUpHandler 接口
    - 处理触摸输入，输出归一化方向向量
    - 支持动态定位模式
    - _Requirements: 1.1, 1.3, 1.4, 1.5, 1.6, 1.7_
  - [x] 2.2 编写虚拟摇杆属性测试
    - **Property 1: Joystick Direction Normalization**
    - **Property 2: Joystick Handle Clamping**
    - **Validates: Requirements 1.4, 1.5**
  - [x] 2.3 创建虚拟摇杆 Prefab
    - 设置 UI 层级结构（Background, Handle）
    - 配置 RectTransform 锚点和尺寸
    - _Requirements: 1.1, 1.2_

- [ ] 3. 实现移动端技能按钮组件
  - [ ] 3.1 创建 MobileSkillButton 脚本
    - 实现触摸交互和视觉反馈
    - 实现冷却显示（径向填充、倒计时文本）
    - 实现就绪动画（发光、脉冲）
    - _Requirements: 2.5, 2.6, 2.7, 2.8, 4.1, 4.2, 4.3, 4.4_
  - [ ] 3.2 编写技能按钮属性测试
    - **Property 4: Cooldown Display State Consistency**
    - **Property 9: Press Scale Feedback**
    - **Validates: Requirements 2.6, 2.7, 4.1**
  - [ ] 3.3 创建技能按钮 Prefab
    - 设置 UI 层级（Background, Icon, CooldownOverlay, CooldownText, Glow）
    - 配置视觉样式
    - _Requirements: 2.9_

- [ ] 4. 实现技能轮盘布局
  - [ ] 4.1 创建 SkillWheelUI 脚本
    - 实现弧形布局算法
    - 管理抓取按钮和技能按钮
    - 绑定到 GroomerSkillManager
    - _Requirements: 2.1, 2.2, 2.3, 3.2, 3.3, 3.4_
  - [ ] 4.2 编写技能轮盘属性测试
    - **Property 3: Skill Button Arc Arrangement**
    - **Property 7: Button Spacing Minimum**
    - **Validates: Requirements 2.3, 3.2, 3.3**
  - [ ] 4.3 创建技能轮盘 Prefab
    - 组装抓取按钮和 3 个技能按钮
    - 配置布局参数
    - _Requirements: 2.1, 3.1_

- [ ] 5. Checkpoint - 验证核心 UI 组件
  - 确保摇杆和技能轮盘基础功能正常
  - 运行已有测试，确保通过

- [ ] 6. 实现挣扎按钮组件
  - [ ] 6.1 创建 StruggleButtonUI 脚本
    - 实现快速点击检测和进度累积
    - 实现进度环显示
    - 实现显示/隐藏逻辑
    - _Requirements: 2.5.1, 2.5.3, 2.5.4, 2.5.5_
  - [ ] 6.2 编写挣扎按钮属性测试
    - **Property 5: Struggle Button Visibility**
    - **Property 6: Struggle Progress Accumulation**
    - **Validates: Requirements 2.5.1, 2.5.3, 2.5.4, 2.5.5**
  - [ ] 6.3 创建挣扎按钮 Prefab
    - 设置 UI 层级（Button, ProgressRing, PromptText）
    - 配置橙红色主题
    - _Requirements: 2.5.2, 2.5.6_

- [ ] 7. 实现 MobileHUDManager 主控制器
  - [ ] 7.1 创建 MobileHUDManager 脚本
    - 实现单例模式
    - 管理移动端 UI 组件的启用/禁用
    - 处理设备检测和 UI 模式切换
    - 集成摇杆输入到角色移动
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 1.8_
  - [ ] 7.2 编写 MobileHUDManager 属性测试
    - **Property 11: UI Mode Visibility Toggle**
    - **Validates: Requirements 5.4, 5.5**
  - [ ] 7.3 创建 MobileHUD Prefab
    - 组装所有移动端 UI 组件
    - 配置 Canvas 和布局
    - _Requirements: 5.1_

- [ ] 8. Checkpoint - 验证完整移动端 UI
  - 确保所有组件协同工作
  - 运行所有测试，确保通过

- [ ] 9. 实现屏幕适配和多点触控
  - [ ] 9.1 添加屏幕尺寸适配逻辑
    - 根据屏幕尺寸调整 UI 缩放
    - 确保 UI 元素不超出屏幕边界
    - _Requirements: 3.6_
  - [ ] 9.2 编写屏幕适配属性测试
    - **Property 8: Screen Size Adaptation**
    - **Validates: Requirements 3.6**
  - [ ] 9.3 实现多点触控处理
    - 确保摇杆和技能按钮可同时操作
    - _Requirements: 6.5_
  - [ ] 9.4 编写多点触控属性测试
    - **Property 12: Multi-Touch Input Handling**
    - **Validates: Requirements 6.5**

- [ ] 10. 创建技能图标资源
  - [ ] 10.1 创建技能图标 Sprite
    - 捕获网图标（蓝色网状）
    - 牵引绳图标（绿色绳索）
    - 镇静喷雾图标（紫色喷雾）
    - 抓取图标（金黄色手掌）
    - 挣扎图标（橙红色锁链）
    - _Requirements: 3.5.1-3.5.6_
  - [ ] 10.2 配置 SkillIconData 资源
    - 创建 ScriptableObject 实例
    - 关联图标和颜色配置
    - _Requirements: 3.5.7, 3.5.8_

- [ ] 11. 集成到游戏场景
  - [ ] 11.1 更新 Phase2SceneInitializer
    - 添加 MobileHUD 初始化逻辑
    - 连接摇杆输入到角色控制器
    - _Requirements: 1.8, 5.1_
  - [ ] 11.2 更新 GroomerController
    - 支持从 MobileHUDManager 获取移动输入
    - 添加抓取按钮回调
    - _Requirements: 1.8, 2.4_

- [ ] 12. 实现 UI 偏好设置持久化
  - [ ] 12.1 添加 UI 模式设置保存/加载
    - 使用 PlayerPrefs 存储偏好
    - 启动时恢复上次设置
    - _Requirements: 5.6_

- [ ] 13. Final Checkpoint - 完整功能验证
  - 确保所有测试通过
  - 验证移动端和桌面端 UI 切换正常
  - 如有问题，询问用户

## Notes

- 所有任务均为必做任务
- 每个任务引用具体需求以确保可追溯性
- Checkpoint 任务用于阶段性验证
- 属性测试验证通用正确性属性
- 单元测试验证具体示例和边界情况

