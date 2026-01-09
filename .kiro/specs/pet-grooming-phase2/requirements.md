# Requirements Document

## Introduction

本文档定义了宠物洗护对抗游戏第二阶段的需求。在 MVP 基础上，本阶段聚焦于：多萌宠支持、狗狗角色差异化、角色技能系统，以及捣乱值系统的动态调整。目标是实现完整的单人模式核心玩法循环。

## Glossary

- **Groomer（洗护师）**: 玩家控制的主角色，目标是抓捕并完成所有萌宠的洗护流程
- **Pet（萌宠）**: AI 控制的角色，包括 Cat（猫咪）和 Dog（狗狗）两种类型
- **Cat（猫咪）**: 移速快、体型小、可攀爬的萌宠类型
- **Dog（狗狗）**: 移速中等、体型大、冲撞力强的萌宠类型
- **Skill_System（技能系统）**: 管理角色技能的冷却、释放和效果的系统
- **Mischief_Value（捣乱值）**: 萌宠通过破坏行为积累的分数，达到阈值则萌宠获胜
- **Pet_Cage（宠物笼）**: 场景中可暂存已抓捕萌宠的设施
- **Grooming_Station（洗护台）**: 场景中的核心交互点，洗护师需将萌宠带至此处完成洗护
- **Grooming_Process（洗护流程）**: 固定3步流程（梳毛→清洁→烘干）
- **Struggle_System（挣扎系统）**: 萌宠被抓取后可通过挣扎尝试逃脱的机制
- **Game_Manager（游戏管理器）**: 控制游戏状态、计时和胜负判定的核心系统
- **Cooldown（冷却时间）**: 技能使用后需等待的时间间隔

## Requirements

### Requirement 1: 多萌宠支持

**User Story:** As a player, I want to face multiple pets in a single match, so that the gameplay is more challenging and strategic.

#### Acceptance Criteria

1. WHEN a match starts in 2-pet mode, THE Game_Manager SHALL spawn 2 Pet instances at random valid positions
2. WHEN a match starts in 3-pet mode, THE Game_Manager SHALL spawn 3 Pet instances at random valid positions
3. WHEN the Groomer captures a Pet while already carrying one, THE Capture_System SHALL reject the capture attempt
4. THE Scene SHALL contain at least 1 Pet_Cage for temporary pet storage
5. WHEN the Groomer brings a captured Pet to a Pet_Cage, THE Cage_System SHALL store the Pet for up to 60 seconds
6. WHEN a Pet has been stored in Pet_Cage for 60 seconds, THE Cage_System SHALL automatically release the Pet
7. WHEN all Pets are groomed, THE Game_Manager SHALL declare Groomer victory

### Requirement 2: 狗狗角色

**User Story:** As a player, I want to face dog pets with different behaviors than cats, so that each match feels varied and interesting.

#### Acceptance Criteria

1. THE Dog SHALL have a base movement speed of 5 units per second (same as Groomer)
2. THE Dog SHALL have a larger collision radius (1.0 unit) compared to Cat (0.5 unit)
3. WHEN the Dog collides with destructible objects, THE Physics_System SHALL apply stronger knockback force than Cat
4. THE Dog SHALL have a base escape chance of 30% (lower than Cat's 40%)
5. WHEN the Dog is in flee state, THE Dog SHALL prefer open areas over tight spaces
6. THE Dog SHALL be unable to climb elevated surfaces that Cat can access

### Requirement 3: 洗护师技能系统

**User Story:** As a player, I want to use special skills to catch pets more effectively, so that I have tactical options during the chase.

#### Acceptance Criteria

1. THE Groomer SHALL have access to 3 skills: Capture_Net, Leash, and Calming_Spray
2. WHEN the Groomer activates Capture_Net, THE Skill_System SHALL throw a projectile that slows hit Pet by 50% for 3 seconds
3. THE Capture_Net skill SHALL have a cooldown of 8 seconds
4. WHEN the Groomer activates Leash, THE Skill_System SHALL fire a hook that pulls a hit Pet toward the Groomer
5. WHEN a Pet is hit by Leash, THE Pet SHALL have a chance to break free (Cat 60%, Dog 40%)
6. THE Leash skill SHALL have a cooldown of 12 seconds
7. WHEN the Groomer activates Calming_Spray, THE Skill_System SHALL create an area effect that stuns Pets for 1 second
8. THE Calming_Spray skill SHALL have a cooldown of 13 seconds
9. THE UI_System SHALL display skill cooldowns as visual indicators above the character

### Requirement 4: 猫咪技能系统

**User Story:** As a player, I want cat pets to have unique evasion abilities, so that catching them requires skill and timing.

#### Acceptance Criteria

1. THE Cat AI SHALL have access to 3 skills: Agile_Jump, Fur_Distraction, and Hide_In_Gap
2. WHEN the Cat activates Agile_Jump, THE Cat SHALL perform a double jump to cross obstacles, with cooldown of 6 seconds
3. WHEN the Cat activates Fur_Distraction, THE Cat SHALL throw a fur ball that blocks Groomer vision for 2 seconds, with cooldown of 10 seconds
4. WHEN the Cat activates Hide_In_Gap, THE Cat SHALL become invisible for 3 seconds while stationary, with cooldown of 14 seconds
5. WHILE the Cat is using Hide_In_Gap and moves, THE Cat SHALL become semi-transparent (50% opacity)
6. THE Cat AI SHALL use skills strategically based on distance to Groomer and current state

### Requirement 5: 狗狗技能系统

**User Story:** As a player, I want dog pets to have unique disruption abilities, so that they provide a different challenge than cats.

#### Acceptance Criteria

1. THE Dog AI SHALL have access to 3 skills: Power_Charge, Intimidating_Bark, and Steal_Tool
2. WHEN the Dog activates Power_Charge, THE Dog SHALL dash forward knocking back Groomer and obstacles, with cooldown of 8 seconds
3. IF the Groomer is holding a captured Pet when hit by Power_Charge, THEN THE captured Pet SHALL be released
4. WHEN the Dog activates Intimidating_Bark, THE Dog SHALL create an area effect reducing Groomer speed by 20% for 3 seconds, with cooldown of 12 seconds
5. WHEN the Dog activates Steal_Tool, THE Dog SHALL remove one tool from the nearest Grooming_Station, adding 1 step to grooming process, with cooldown of 12 seconds
6. THE Dog AI SHALL use skills strategically based on distance to Groomer and current state

### Requirement 6: 动态捣乱值阈值

**User Story:** As a player, I want the mischief threshold to scale with the number of pets, so that the game remains balanced.

#### Acceptance Criteria

1. WHEN playing in 2-pet mode, THE Mischief_System SHALL set threshold to 800 points
2. WHEN playing in 3-pet mode, THE Mischief_System SHALL set threshold to 1000 points
3. WHEN mischief value reaches (threshold - 100), THE Game_Manager SHALL trigger alert state
4. WHILE in alert state, THE Scene SHALL display flashing lights and play alert sound
5. WHILE in alert state, THE Groomer SHALL receive a 10% movement speed bonus
6. WHEN a Pet uses a skill that hits the Groomer, THE Mischief_System SHALL add 30 points

### Requirement 7: 技能冷却可视化

**User Story:** As a player, I want to see skill cooldowns clearly, so that I can plan my actions effectively.

#### Acceptance Criteria

1. THE UI_System SHALL display each skill as an icon with cooldown overlay
2. WHEN a skill is on cooldown, THE UI_System SHALL show a radial fill animation indicating remaining time
3. WHEN a skill is on cooldown, THE UI_System SHALL display remaining seconds as text
4. WHEN a skill becomes available, THE UI_System SHALL play a ready indicator animation
5. THE skill icons SHALL be positioned near the character or in a fixed HUD location based on settings

### Requirement 8: 宠物笼系统

**User Story:** As a player, I want to temporarily store captured pets, so that I can manage multiple pets strategically.

#### Acceptance Criteria

1. THE Pet_Cage SHALL have a visual indicator showing whether it is empty or occupied
2. WHEN a Pet is stored in Pet_Cage, THE UI_System SHALL display a countdown timer
3. WHEN the countdown reaches 10 seconds, THE UI_System SHALL display a warning indicator
4. WHEN a Pet is released from Pet_Cage, THE Pet SHALL spawn at the cage location with 3 seconds of invulnerability
5. THE Groomer SHALL be able to manually release a Pet from Pet_Cage by interacting with it
6. WHILE a Pet is in Pet_Cage, THE Pet SHALL not contribute to mischief value accumulation

### Requirement 9: 摄像头跟随系统

**User Story:** As a player, I want a smart camera that follows my character and provides good visibility, so that I can see the action clearly during gameplay.

#### Acceptance Criteria

1. THE Camera_System SHALL follow the Groomer with smooth interpolation at a configurable follow speed
2. THE Camera_System SHALL maintain a configurable offset distance and height from the Groomer
3. WHEN the Groomer is near scene boundaries, THE Camera_System SHALL clamp position to prevent showing out-of-bounds areas
4. WHEN the Groomer captures a Pet, THE Camera_System SHALL smoothly zoom out slightly to show both characters
5. WHEN the Groomer releases or loses a Pet, THE Camera_System SHALL smoothly return to default zoom level
6. WHEN the Groomer is in grooming state at Grooming_Station, THE Camera_System SHALL switch to a fixed grooming view angle
7. THE Camera_System SHALL avoid clipping through walls and obstacles using collision detection
8. WHEN an obstacle blocks the camera view, THE Camera_System SHALL move closer to the Groomer to maintain visibility
9. THE Camera_System SHALL support configurable field of view for different gameplay situations
10. WHEN in alert state (mischief near threshold), THE Camera_System SHALL apply subtle screen shake effect

