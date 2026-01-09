# Requirements Document

## Introduction

本文档定义了宠物洗护对抗游戏的 MVP（最小可行产品）版本需求。MVP 聚焦于单人模式下的核心玩法循环：一名洗护师对抗一只 AI 控制的萌宠（猫咪），在限时内完成抓捕与洗护流程。

## Glossary

- **Groomer（洗护师）**: 玩家控制的主角色，目标是抓捕并完成萌宠的洗护流程
- **Pet（萌宠）**: AI 控制的猫咪角色，目标是躲避抓捕并积累捣乱值
- **Grooming_Station（洗护台）**: 场景中的核心交互点，洗护师需将萌宠带至此处完成洗护
- **Mischief_Value（捣乱值）**: 萌宠通过破坏行为积累的分数，达到阈值则萌宠获胜
- **Grooming_Process（洗护流程）**: 固定3步流程（梳毛→清洁→烘干）
- **Struggle_System（挣扎系统）**: 萌宠被抓取后可通过挣扎尝试逃脱的机制
- **Game_Manager（游戏管理器）**: 控制游戏状态、计时和胜负判定的核心系统

## Requirements

### Requirement 1: 角色移动控制

**User Story:** As a player, I want to control the Groomer character with smooth movement, so that I can chase and catch pets in the store.

#### Acceptance Criteria

1. WHEN the player presses movement keys (WASD or arrow keys), THE Groomer SHALL move in the corresponding direction at a base speed of 5 units per second
2. WHEN the Groomer collides with scene obstacles, THE Movement_System SHALL prevent the character from passing through
3. WHEN the Groomer is carrying a captured pet, THE Movement_System SHALL reduce movement speed by 15%
4. THE Groomer SHALL have a third-person camera that follows the character with smooth interpolation

### Requirement 2: AI 萌宠行为

**User Story:** As a player, I want the AI pet to exhibit realistic evasion behavior, so that the chase feels engaging and challenging.

#### Acceptance Criteria

1. WHEN the game starts, THE Pet SHALL spawn at a random valid position in the scene
2. WHEN the Groomer approaches within 8 units, THE Pet SHALL enter flee state and move away from the Groomer
3. WHEN the Pet is in idle state, THE Pet SHALL wander randomly within the play area
4. WHEN the Pet collides with destructible objects, THE Mischief_System SHALL add corresponding points to the mischief value
5. THE Pet SHALL move at a base speed of 6 units per second (faster than Groomer)

### Requirement 3: 抓捕系统

**User Story:** As a player, I want to catch pets when close enough, so that I can bring them to the grooming station.

#### Acceptance Criteria

1. WHEN the Groomer is within 1.5 units of the Pet and presses the interact key, THE Capture_System SHALL initiate a capture attempt
2. WHEN a capture is initiated, THE Pet SHALL enter a captured state and be attached to the Groomer
3. WHILE the Pet is captured, THE Struggle_System SHALL allow the Pet to attempt escape with a 40% base success rate
4. WHEN the Pet successfully escapes, THE Pet SHALL teleport 3 units away from the Groomer
5. IF the capture attempt fails due to distance, THEN THE UI_System SHALL display a "Too far" indicator

### Requirement 4: 洗护流程

**User Story:** As a player, I want to complete a grooming process on captured pets, so that I can win the game.

#### Acceptance Criteria

1. WHEN the Groomer brings a captured Pet to the Grooming_Station, THE Grooming_Process SHALL begin automatically
2. THE Grooming_Process SHALL consist of 3 sequential steps: Brush, Clean, Dry
3. WHEN each grooming step is active, THE UI_System SHALL display the required key prompt
4. WHEN the player presses the correct key, THE Grooming_Process SHALL advance to the next step
5. WHILE grooming is in progress, THE Pet SHALL continue to struggle with decreasing success rate (reduced by 10% per completed step)
6. WHEN all 3 steps are completed, THE Pet SHALL be marked as groomed and removed from play

### Requirement 5: 捣乱值系统

**User Story:** As a player, I want to see the pet's mischief progress, so that I understand the urgency of catching them.

#### Acceptance Criteria

1. THE Mischief_System SHALL track a cumulative mischief value starting at 0
2. WHEN the Pet knocks over a shelf item, THE Mischief_System SHALL add 50 points
3. WHEN the Pet knocks over a cleaning cart, THE Mischief_System SHALL add 80 points
4. THE UI_System SHALL display the current mischief value as a progress bar
5. WHEN the mischief value reaches 500 points (MVP threshold), THE Game_Manager SHALL declare Pet victory

### Requirement 6: 游戏计时与胜负判定

**User Story:** As a player, I want clear win/lose conditions with a time limit, so that each match has defined stakes.

#### Acceptance Criteria

1. WHEN a match starts, THE Game_Manager SHALL initialize a 3-minute countdown timer (MVP shortened from 5 minutes)
2. THE UI_System SHALL display the remaining time prominently on screen
3. WHEN the timer reaches 0 and the Pet is not groomed, THE Game_Manager SHALL declare Pet victory
4. WHEN the Pet is successfully groomed, THE Game_Manager SHALL declare Groomer victory
5. WHEN the mischief value reaches the threshold, THE Game_Manager SHALL declare Pet victory immediately
6. WHEN a victory condition is met, THE UI_System SHALL display the result screen with replay option

### Requirement 7: 场景与可破坏物体

**User Story:** As a player, I want an interactive environment with destructible objects, so that the chase feels dynamic.

#### Acceptance Criteria

1. THE Scene SHALL contain at least 1 Grooming_Station positioned accessibly
2. THE Scene SHALL contain at least 4 shelf items that can be knocked over
3. THE Scene SHALL contain at least 2 cleaning carts that can be pushed and knocked over
4. WHEN a destructible object is knocked over, THE Physics_System SHALL apply realistic physics simulation
5. WHEN a knocked-over object blocks a path, THE Navigation_System SHALL update pathfinding accordingly

### Requirement 8: 用户界面

**User Story:** As a player, I want clear visual feedback on game state, so that I can make informed decisions during gameplay.

#### Acceptance Criteria

1. THE UI_System SHALL display the remaining match time in MM:SS format
2. THE UI_System SHALL display the mischief value as a filled progress bar with numeric value
3. WHEN the Pet is captured, THE UI_System SHALL display struggle prompts for the Pet AI
4. WHEN near the Grooming_Station with a captured Pet, THE UI_System SHALL display "Press E to start grooming"
5. THE UI_System SHALL display the current grooming step during the grooming process
6. WHEN the match ends, THE UI_System SHALL display victory/defeat screen with final stats
