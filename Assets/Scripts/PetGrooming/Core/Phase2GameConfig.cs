using UnityEngine;

namespace PetGrooming.Core
{
    /// <summary>
    /// 包含第二阶段游戏配置参数的 ScriptableObject。
    /// 通过多宠物支持、技能系统、宠物笼和相机设置扩展基础游戏。
    /// 需求：1.1, 1.2, 3.2-3.8, 4.2-4.4, 5.2-5.5, 6.1, 6.2
    /// </summary>
    [CreateAssetMenu(fileName = "Phase2GameConfig", menuName = "PetGrooming/Phase2GameConfig")]
    public class Phase2GameConfig : ScriptableObject
    {
        #region Match Settings
        [Header("Match Settings")]
        [Tooltip("比赛持续时间（秒）（第二阶段为 5 分钟）")]
        public float MatchDuration = 300f;
        
        [Tooltip("2 宠物模式的恶作剧阈值")]
        public int TwoPetMischiefThreshold = 800;
        
        [Tooltip("3 宠物模式的恶作剧阈值")]
        public int ThreePetMischiefThreshold = 1000;
        
        [Tooltip("触发警戒状态的阈值以下点数")]
        public int AlertThresholdOffset = 100;
        #endregion

        #region Pet Cage Settings
        [Header("Pet Cage Settings")]
        [Tooltip("宠物可在笼中存放的最长时间（秒）")]
        public float CageStorageTime = 60f;
        
        [Tooltip("警告指示器出现时的剩余时间（秒）")]
        public float CageWarningTime = 10f;
        
        [Tooltip("宠物从笼中释放后的无敌持续时间（秒）")]
        public float ReleaseInvulnerabilityTime = 3f;
        #endregion

        #region Groomer Skill Cooldowns
        [Header("Groomer Skill Cooldowns")]
        [Tooltip("捕获网技能的冷却时间（秒）")]
        public float CaptureNetCooldown = 8f;
        
        [Tooltip("牵引绳技能的冷却时间（秒）")]
        public float LeashCooldown = 12f;
        
        [Tooltip("镇静喷雾技能的冷却时间（秒）")]
        public float CalmingSprayCooldown = 13f;
        #endregion

        #region Cat Skill Cooldowns
        [Header("Cat Skill Cooldowns")]
        [Tooltip("敏捷跳跃技能的冷却时间（秒）")]
        public float AgileJumpCooldown = 6f;
        
        [Tooltip("毛发干扰技能的冷却时间（秒）")]
        public float FurDistractionCooldown = 10f;
        
        [Tooltip("躲在缝隙中技能的冷却时间（秒）")]
        public float HideInGapCooldown = 14f;
        #endregion

        #region Dog Skill Cooldowns
        [Header("Dog Skill Cooldowns")]
        [Tooltip("强力冲锋技能的冷却时间（秒）")]
        public float PowerChargeCooldown = 8f;
        
        [Tooltip("恐吓 bark 技能的冷却时间（秒）")]
        public float IntimidatingBarkCooldown = 12f;
        
        [Tooltip("偷工具技能的冷却时间（秒）")]
        public float StealToolCooldown = 12f;
        #endregion


        #region Capture Net Skill Effects
        [Header("Capture Net Skill Effects")]
        [Tooltip("被捕获网击中时的移动速度减少量（0.5 = 减少 50%）")]
        [Range(0f, 1f)]
        public float CaptureNetSlowAmount = 0.5f;
        
        [Tooltip("捕获网减速效果的持续时间（秒）")]
        public float CaptureNetSlowDuration = 3f;
        
        [Tooltip("捕获网的投射物速度")]
        public float CaptureNetProjectileSpeed = 15f;
        #endregion

        #region Leash Skill Effects
        [Header("Leash Skill Effects")]
        [Tooltip("牵引绳技能的最大范围")]
        public float LeashMaxRange = 10f;
        
        [Tooltip("牵引绳连接时的拉动速度")]
        public float LeashPullSpeed = 8f;
        
        [Tooltip("猫从牵引绳中挣脱的几率（0.6 = 60%）")]
        [Range(0f, 1f)]
        public float LeashCatBreakFreeChance = 0.6f;
        
        [Tooltip("狗从牵引绳中挣脱的几率（0.4 = 40%）")]
        [Range(0f, 1f)]
        public float LeashDogBreakFreeChance = 0.4f;
        #endregion

        #region Calming Spray Skill Effects
        [Header("Calming Spray Skill Effects")]
        [Tooltip("镇静喷雾的效果半径")]
        public float CalmingSprayRadius = 3f;
        
        [Tooltip("被镇静喷雾击中时的眩晕持续时间（秒）")]
        public float CalmingSprayStunDuration = 1f;
        #endregion

        #region Fur Distraction Skill Effects
        [Header("Fur Distraction Skill Effects")]
        [Tooltip("毛发干扰导致视野阻挡的持续时间（秒）")]
        public float FurDistractionDuration = 2f;
        #endregion

        #region Hide In Gap Skill Effects
        [Header("Hide In Gap Skill Effects")]
        [Tooltip("躲在缝隙中导致隐身的持续时间（秒）")]
        public float HideInGapDuration = 3f;
        
        [Tooltip("使用躲在缝隙中移动时的不透明度（0.5 = 50%）")]
        [Range(0f, 1f)]
        public float HideInGapMovingOpacity = 0.5f;
        #endregion

        #region Power Charge Skill Effects
        [Header("Power Charge Skill Effects")]
        [Tooltip("强力冲锋的冲刺距离")]
        public float PowerChargeDashDistance = 5f;
        
        [Tooltip("强力冲锋施加的击退力")]
        public float PowerChargeKnockbackForce = 10f;
        #endregion

        #region Intimidating Bark Skill Effects
        [Header("Intimidating Bark Skill Effects")]
        [Tooltip("恐吓 bark 的效果半径")]
        public float IntimidatingBarkRadius = 4f;
        
        [Tooltip("恐吓 bark 导致的移动速度减少量（0.2 = 减少 20%）")]
        [Range(0f, 1f)]
        public float IntimidatingBarkSlowAmount = 0.2f;
        
        [Tooltip("恐吓 bark 减速效果的持续时间（秒）")]
        public float IntimidatingBarkDuration = 3f;
        #endregion

        #region Steal Tool Skill Effects
        [Header("Steal Tool Skill Effects")]
        [Tooltip("偷工具技能检测最近的梳理站的范围")]
        public float StealToolRange = 5f;
        
        [Tooltip("工具被偷时增加的额外梳理步骤")]
        public int StealToolExtraSteps = 1;
        #endregion


        #region Alert System Settings
        [Header("Alert System Settings")]
        [Tooltip("警戒状态下 Groomer 的速度加成（0.1 = 增加 10%）")]
        [Range(0f, 0.5f)]
        public float AlertGroomerSpeedBonus = 0.1f;
        
        [Tooltip("警戒灯闪烁的间隔（秒）")]
        public float AlertFlashInterval = 0.5f;
        
        [Tooltip("宠物技能击中 Groomer 时增加的恶作剧点数")]
        public int PetSkillHitMischief = 30;
        #endregion

        #region Camera Settings
        [Header("Camera Settings")]
        [Tooltip("相机跟随插值速度")]
        public float CameraFollowSpeed = 5f;
        
        [Tooltip("相机与目标的默认偏移量")]
        public Vector3 CameraDefaultOffset = new Vector3(0, 8, -6);
        
        [Tooltip("相机默认视野")]
        public float CameraDefaultFOV = 60f;
        
        [Tooltip("Groomer 捕获宠物时的缩放倍数")]
        public float CameraCaptureZoomMultiplier = 1.2f;
        
        [Tooltip("相机缩放插值速度")]
        public float CameraZoomSpeed = 2f;
        
        [Tooltip("相机与目标的最小距离（碰撞避免）")]
        public float CameraMinDistance = 2f;
        
        [Tooltip("警戒状态下的屏幕抖动强度")]
        public float AlertShakeIntensity = 0.1f;
        
        [Tooltip("警戒状态下的屏幕抖动持续时间")]
        public float AlertShakeDuration = 0.5f;
        #endregion

        #region Pet Type Settings
        [Header("Pet Type Settings")]
        [Tooltip("猫宠物的碰撞半径")]
        public float CatCollisionRadius = 0.5f;
        
        [Tooltip("狗宠物的碰撞半径")]
        public float DogCollisionRadius = 1.0f;
        
        [Tooltip("猫宠物的基础逃脱几率")]
        [Range(0f, 1f)]
        public float CatBaseEscapeChance = 0.4f;
        
        [Tooltip("狗宠物的基础逃脱几率")]
        [Range(0f, 1f)]
        public float DogBaseEscapeChance = 0.3f;
        
        [Tooltip("猫碰撞的击退力")]
        public float CatKnockbackForce = 5f;
        
        [Tooltip("狗碰撞的击退力")]
        public float DogKnockbackForce = 10f;
        
        [Tooltip("狗宠物的基础移动速度（与 Groomer 相同）")]
        public float DogMoveSpeed = 5f;
        #endregion

        #region Helper Methods
        /// <summary>
        /// 根据游戏模式获取恶作剧阈值。
        /// </summary>
        /// <param name="petCount">比赛中的宠物数量（2 或 3）</param>
        /// <returns>给定宠物数量的恶作剧阈值</returns>
        public int GetMischiefThreshold(int petCount)
        {
            return petCount >= 3 ? ThreePetMischiefThreshold : TwoPetMischiefThreshold;
        }

        /// <summary>
        /// 根据游戏模式获取警戒触发阈值。
        /// </summary>
        /// <param name="petCount">比赛中的宠物数量（2 或 3）</param>
        /// <returns>触发警戒状态的恶作剧值</returns>
        public int GetAlertThreshold(int petCount)
        {
            return GetMischiefThreshold(petCount) - AlertThresholdOffset;
        }

        /// <summary>
        /// 根据宠物类型获取牵引绳技能的挣脱几率。
        /// </summary>
        /// <param name="isCat">如果宠物是猫则为 True，如果是狗则为 False</param>
        /// <returns>挣脱几率（0-1）</returns>
        public float GetLeashBreakFreeChance(bool isCat)
        {
            return isCat ? LeashCatBreakFreeChance : LeashDogBreakFreeChance;
        }

        /// <summary>
        /// 根据宠物类型获取碰撞半径。
        /// </summary>
        /// <param name="isCat">如果宠物是猫则为 True，如果是狗则为 False</param>
        /// <returns>碰撞半径</returns>
        public float GetCollisionRadius(bool isCat)
        {
            return isCat ? CatCollisionRadius : DogCollisionRadius;
        }

        /// <summary>
        /// 根据宠物类型获取基础逃脱几率。
        /// </summary>
        /// <param name="isCat">如果宠物是猫则为 True，如果是狗则为 False</param>
        /// <returns>基础逃脱几率（0-1）</returns>
        public float GetBaseEscapeChance(bool isCat)
        {
            return isCat ? CatBaseEscapeChance : DogBaseEscapeChance;
        }

        /// <summary>
        /// 根据宠物类型获取击退力。
        /// </summary>
        /// <param name="isCat">如果宠物是猫则为 True，如果是狗则为 False</param>
        /// <returns>击退力</returns>
        public float GetKnockbackForce(bool isCat)
        {
            return isCat ? CatKnockbackForce : DogKnockbackForce;
        }
        #endregion
    }
}
