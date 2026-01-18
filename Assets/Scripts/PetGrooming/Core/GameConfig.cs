using UnityEngine;

namespace PetGrooming.Core
{
    /// <summary>
    /// 包含所有游戏配置参数的 ScriptableObject。
    /// 需求：1.1, 2.5, 3.1, 5.5, 6.1
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "PetGrooming/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Match Settings")]
        [Tooltip("比赛持续时间（秒）（MVP 为 3 分钟）")]
        public float MatchDuration = 180f;
        
        [Tooltip("宠物胜利的恶作剧值阈值")]
        public int MischiefThreshold = 500;

        [Header("Groomer Settings")]
        [Tooltip("Groomer 的基础移动速度（单位/秒）")]
        public float GroomerMoveSpeed = 5f;
        
        [Tooltip("可以启动捕获的距离")]
        public float CaptureRange = 1.5f;
        
        [Tooltip("携带宠物时的速度倍数（减少 15% = 0.85）")]
        public float CarrySpeedMultiplier = 0.85f;

        [Header("Pet Settings")]
        [Tooltip("宠物的基础移动速度（单位/秒）")]
        public float PetMoveSpeed = 6f;
        
        [Tooltip("宠物检测到 Groomer 并进入逃离状态的距离")]
        public float FleeDetectionRange = 8f;
        
        [Tooltip("宠物被捕获时的基础逃脱几率（40%）")]
        public float BaseEscapeChance = 0.4f;
        
        [Tooltip("宠物成功逃脱时 teleport 离开的距离")]
        public float EscapeTeleportDistance = 3f;
        
        [Tooltip("每完成一个梳理步骤，逃脱几率的减少量")]
        public float EscapeChanceReductionPerStep = 0.1f;
        
        [Tooltip("挣扎尝试之间的间隔（秒）")]
        public float StruggleInterval = 1f;

        [Header("Mischief Values")]
        [Tooltip("宠物撞倒货架物品时增加的恶作剧点数")]
        public int ShelfItemMischief = 50;
        
        [Tooltip("宠物撞倒清洁车时增加的恶作剧点数")]
        public int CleaningCartMischief = 80;
    }
}
