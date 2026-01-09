# Pet Grooming Game - Unity 使用指南

## Unity 环境

- Unity 地址: `/Applications/Unity/6000.3.1f1/Unity.app/Contents/MacOS/Unity`
- Unity 版本: 6000.3.1f1
- 平台: macOS

## Phase 2 快速开始

### 方法一：使用 Phase2SceneInitializer（推荐）

1. 在 Unity 中打开你的场景
2. 创建空 GameObject，命名为 `Phase2Initializer`
3. 添加组件 `Phase2SceneInitializer`（在 PetGrooming > Setup 下）
4. 运行游戏 - 会自动创建缺失的组件

### 方法二：手动配置

1. **创建配置文件**
   - Project 窗口右键 → Create → PetGrooming → Phase2GameConfig
   - 同样创建 GameConfig（如果没有）

2. **配置场景**
   - 菜单栏 → PetGrooming → Phase 2 → Configure Phase 2 Scene

3. **创建 Groomer**
   - 菜单栏 → PetGrooming → Phase 2 → Create Groomer Template

## 游戏控制

### 移动
| 按键 | 动作 |
|------|------|
| W / ↑ | 向前移动 |
| S / ↓ | 向后移动 |
| A / ← | 向左移动 |
| D / → | 向右移动 |

> 移动方向相对于摄像头视角

### 交互
| 按键 | 动作 |
|------|------|
| E | 抓捕宠物 |
| F | 存放/释放宠物（笼子交互） |

### 技能
| 按键 | 技能 | 冷却 |
|------|------|------|
| 1 | 捕宠网（减速 50%，3秒） | 8秒 |
| 2 | 牵引绳（拉拽宠物） | 12秒 |
| 3 | 镇静喷雾（眩晕 1秒） | 13秒 |

## 游戏规则

### 胜利条件
- **洗护师胜利**: 完成所有宠物的洗护
- **宠物胜利**: 捣乱值达到阈值（2宠模式 800，3宠模式 1000）

### 宠物笼
- 最多存放 60 秒
- 10 秒时显示警告
- 释放后宠物有 3 秒无敌

### 警报状态
- 捣乱值接近阈值时触发（阈值 - 100）
- 洗护师获得 10% 移速加成
- 屏幕轻微震动

## 运行测试

```bash
/Applications/Unity/6000.3.1f1/Unity.app/Contents/MacOS/Unity -runTests -batchmode -projectPath . -testResults ./TestResults.xml -testPlatform EditMode
```

## 常见问题

### Q: 移动方向不对？
确保 CameraController 已添加到 Main Camera，并且 Target 设置为 Groomer。

### Q: 技能按键没反应？
确保 Groomer 上有 GroomerSkillManager 组件。

### Q: 摄像头不跟随？
检查 Main Camera 上是否有 CameraController，Target 是否正确设置。
