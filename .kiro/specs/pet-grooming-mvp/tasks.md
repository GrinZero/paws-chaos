# Implementation Plan: Pet Grooming MVP

## Overview

本任务列表将设计文档转化为可执行的编码任务。任务按依赖顺序排列，从核心系统到具体功能逐步构建。所有脚本放置在 `Assets/Scripts/PetGrooming/` 目录下。

## Tasks

- [x] 1. 创建项目结构和核心配置
  - [x] 1.1 创建 `Assets/Scripts/PetGrooming/` 目录结构
    - 创建 Core、AI、Systems、UI 子目录
    - _Requirements: 项目组织_
  - [x] 1.2 创建 GameConfig ScriptableObject
    - 实现所有配置参数（移速、距离、阈值等）
    - _Requirements: 1.1, 2.5, 3.1, 5.5, 6.1_

- [x] 2. 实现游戏管理核心系统
  - [x] 2.1 创建 GameManager 单例
    - 实现游戏状态枚举和状态管理
    - 实现计时器逻辑
    - 实现胜负判定方法
    - _Requirements: 6.1, 6.3, 6.4, 6.5_
  - [x] 2.2 编写 GameManager 属性测试
    - **Property 10: Victory Condition - Mischief Threshold**
    - **Property 11: Victory Condition - Timer Expiry**
    - **Property 12: Victory Condition - Grooming Complete**
    - **Validates: Requirements 5.5, 6.3, 6.4, 6.5**

- [x] 3. 实现捣乱值系统
  - [x] 3.1 创建 MischiefSystem 单例
    - 实现捣乱值累积逻辑
    - 实现阈值检测和事件触发
    - _Requirements: 5.1, 5.2, 5.3, 5.5_
  - [x] 3.2 编写 MischiefSystem 属性测试
    - **Property 5: Mischief Value Calculation**
    - **Validates: Requirements 2.4, 5.2, 5.3**

- [x] 4. 实现洗护师控制器
  - [x] 4.1 创建 GroomerController 组件
    - 扩展现有 ThirdPersonController 功能
    - 实现抓捕检测和执行逻辑
    - 实现携带状态下的移速调整
    - _Requirements: 1.1, 1.2, 1.3, 3.1, 3.2_
  - [x] 4.2 编写 GroomerController 属性测试
    - **Property 1: Carry Speed Reduction**
    - **Property 6: Capture Distance Validation**
    - **Validates: Requirements 1.3, 3.1, 3.2**

- [x] 5. Checkpoint - 核心系统验证
  - 确保所有测试通过，如有问题请询问用户

- [x] 6. 实现萌宠 AI 系统
  - [x] 6.1 创建 PetAI 状态机
    - 实现 Idle、Wandering、Fleeing、Captured、BeingGroomed 状态
    - 实现状态转换逻辑
    - _Requirements: 2.1, 2.2, 2.3_
  - [x] 6.2 实现萌宠移动和逃跑行为
    - 实现基于 NavMeshAgent 的移动
    - 实现逃跑方向计算
    - _Requirements: 2.2, 2.5_
  - [x] 6.3 实现挣扎和逃脱系统
    - 实现挣扎概率计算
    - 实现逃脱后的瞬移逻辑
    - _Requirements: 3.3, 3.4_
  - [x] 6.4 编写 PetAI 属性测试
    - **Property 2: Pet Spawn Position Validity**
    - **Property 3: Flee State Trigger Distance**
    - **Property 4: Wander Target Bounds**
    - **Property 7: Escape Teleport Distance**
    - **Validates: Requirements 2.1, 2.2, 2.3, 3.4**

- [x] 7. 实现洗护流程系统
  - [x] 7.1 创建 GroomingSystem 组件
    - 实现三步洗护流程状态机
    - 实现按键输入检测
    - 实现逃脱概率递减逻辑
    - _Requirements: 4.1, 4.2, 4.4, 4.5, 4.6_
  - [x] 7.2 创建 GroomingStation 组件
    - 实现洗护台交互检测
    - 实现洗护位置定位
    - _Requirements: 4.1, 7.1_
  - [x] 7.3 编写 GroomingSystem 属性测试
    - **Property 8: Grooming Step Sequence**
    - **Property 9: Escape Chance Reduction Formula**
    - **Validates: Requirements 4.2, 4.4, 4.5, 4.6**

- [x] 8. Checkpoint - 游戏逻辑验证
  - 确保所有测试通过，如有问题请询问用户

- [x] 9. 实现可破坏物体系统
  - [x] 9.1 创建 DestructibleObject 组件
    - 实现物体类型和捣乱值配置
    - 实现碰撞检测和物理响应
    - 实现与 MischiefSystem 的集成
    - _Requirements: 5.2, 5.3, 7.2, 7.3, 7.4_

- [x] 10. 实现游戏 UI 系统
  - [x] 10.1 创建 GameHUD 组件
    - 实现计时器显示
    - 实现捣乱值进度条
    - 实现洗护步骤提示
    - _Requirements: 6.2, 5.4, 8.1, 8.2, 8.5_
  - [x] 10.2 创建交互提示 UI
    - 实现抓捕提示
    - 实现洗护提示
    - 实现距离过远提示
    - _Requirements: 3.5, 8.3, 8.4_
  - [x] 10.3 创建结果界面
    - 实现胜利/失败显示
    - 实现重玩按钮
    - _Requirements: 8.6_

- [x] 11. 场景集成和配置
  - [x] 11.1 创建游戏场景 Prefabs
    - 创建洗护台 Prefab
    - 创建可破坏货架 Prefab
    - 创建可破坏推车 Prefab
    - _Requirements: 7.1, 7.2, 7.3_
  - [x] 11.2 配置游戏场景
    - 放置洗护台
    - 放置可破坏物体
    - 配置 NavMesh
    - _Requirements: 7.1, 7.2, 7.3, 7.5_
  - [x] 11.3 创建萌宠 Prefab
    - 配置 PetAI 组件
    - 配置 NavMeshAgent
    - 配置碰撞体
    - _Requirements: 2.1_

- [x] 12. Final Checkpoint - 完整游戏测试
  - 确保所有测试通过
  - 验证完整游戏循环
  - 如有问题请询问用户

## Notes

- 所有任务均为必需，包括测试任务
- 每个任务引用具体需求以确保可追溯性
- Checkpoint 任务用于阶段性验证
- 属性测试使用 NUnit + FsCheck 框架
