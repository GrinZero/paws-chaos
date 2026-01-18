using System;
using UnityEngine;

namespace PetGrooming.Core
{
    /// <summary>
    /// 定义所有可能的技能效果类型的枚举。
    /// 需求：3.2, 3.7, 4.4, 5.4
    /// </summary>
    public enum SkillEffectType
    {
        /// <summary>
        /// 按百分比减少移动速度。
        /// 使用技能：捕获网（50%），恐吓 bark（20%）
        /// </summary>
        Slow,
        
        /// <summary>
        /// 完全阻止移动和行动。
        /// 使用技能：镇静喷雾（1 秒）
        /// </summary>
        Stun,
        
        /// <summary>
        /// 使目标不可见或半透明。
        /// 使用技能：躲在缝隙中（猫技能）
        /// </summary>
        Invisible,
        
        /// <summary>
        /// 按百分比增加移动速度。
        /// 使用技能：警戒状态（Groomer 10% 加成）
        /// </summary>
        SpeedBoost,
        
        /// <summary>
        /// 防止目标被捕获或受到技能影响。
        /// 使用技能：宠物笼释放（3 秒）
        /// </summary>
        Invulnerable
    }

    /// <summary>
    /// 表示角色身上活动技能效果的数据结构。
    /// 跟踪效果类型、值和剩余持续时间。
    /// 需求：3.2, 3.7, 4.4, 5.4
    /// </summary>
    [Serializable]
    public class SkillEffectData
    {
        #region Fields
        /// <summary>
        /// 正在应用的效果类型。
        /// </summary>
        public SkillEffectType Type;
        
        /// <summary>
        /// 效果的强度（例如，0.5 表示 50% 减速）。
        /// </summary>
        public float Value;
        
        /// <summary>
        /// 效果的总持续时间（秒）。
        /// </summary>
        public float Duration;
        
        /// <summary>
        /// 效果的剩余时间（秒）。
        /// </summary>
        public float RemainingTime;
        
        /// <summary>
        /// 可选的源标识符，用于跟踪哪个技能应用了此效果。
        /// </summary>
        public string SourceSkillName;
        #endregion

        #region Properties
        /// <summary>
        /// 效果是否已过期。
        /// </summary>
        public bool IsExpired => RemainingTime <= 0f;
        
        /// <summary>
        /// 效果的标准化进度（0 = 刚刚开始，1 = 已过期）。
        /// </summary>
        public float Progress => Duration > 0f ? 1f - (RemainingTime / Duration) : 1f;
        #endregion

        #region Constructors
        /// <summary>
        /// 用于序列化的默认构造函数。
        /// </summary>
        public SkillEffectData()
        {
            Type = SkillEffectType.Slow;
            Value = 0f;
            Duration = 0f;
            RemainingTime = 0f;
            SourceSkillName = string.Empty;
        }

        /// <summary>
        /// 使用指定的参数创建新的技能效果。
        /// </summary>
        /// <param name="type">效果的类型</param>
        /// <param name="value">效果的强度</param>
        /// <param name="duration">持续时间（秒）</param>
        /// <param name="sourceSkillName">创建此效果的技能的可选名称</param>
        public SkillEffectData(SkillEffectType type, float value, float duration, string sourceSkillName = "")
        {
            Type = type;
            Value = value;
            Duration = duration;
            RemainingTime = duration;
            SourceSkillName = sourceSkillName;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 通过减去增量时间来更新剩余时间。
        /// </summary>
        /// <param name="deltaTime">自上次更新以来经过的时间</param>
        /// <returns>如果效果仍然活动则返回 True，如果过期则返回 False</returns>
        public bool UpdateTime(float deltaTime)
        {
            RemainingTime -= deltaTime;
            
            if (RemainingTime < 0f)
            {
                RemainingTime = 0f;
            }
            
            return !IsExpired;
        }

        /// <summary>
        /// 将效果持续时间重置为其原始值。
        /// </summary>
        public void ResetDuration()
        {
            RemainingTime = Duration;
        }

        /// <summary>
        /// 将效果持续时间延长指定的量。
        /// </summary>
        /// <param name="additionalTime">要添加的时间（秒）</param>
        public void ExtendDuration(float additionalTime)
        {
            RemainingTime += additionalTime;
            Duration += additionalTime;
        }

        /// <summary>
        /// 创建此效果数据的副本。
        /// </summary>
        /// <returns>具有相同值的新 SkillEffectData</returns>
        public SkillEffectData Clone()
        {
            return new SkillEffectData
            {
                Type = Type,
                Value = Value,
                Duration = Duration,
                RemainingTime = RemainingTime,
                SourceSkillName = SourceSkillName
            };
        }
        #endregion

        #region Static Factory Methods
        /// <summary>
        /// 创建减速效果。
        /// </summary>
        /// <param name="slowAmount">速度减少量（0.5 = 慢 50%）</param>
        /// <param name="duration">持续时间（秒）</param>
        /// <param name="sourceName">源技能的名称</param>
        public static SkillEffectData CreateSlow(float slowAmount, float duration, string sourceName = "")
        {
            return new SkillEffectData(SkillEffectType.Slow, slowAmount, duration, sourceName);
        }

        /// <summary>
        /// 创建眩晕效果。
        /// </summary>
        /// <param name="duration">持续时间（秒）</param>
        /// <param name="sourceName">源技能的名称</param>
        public static SkillEffectData CreateStun(float duration, string sourceName = "")
        {
            return new SkillEffectData(SkillEffectType.Stun, 1f, duration, sourceName);
        }

        /// <summary>
        /// 创建隐身效果。
        /// </summary>
        /// <param name="opacity">不透明度级别（0 = 完全不可见，1 = 完全可见）</param>
        /// <param name="duration">持续时间（秒）</param>
        /// <param name="sourceName">源技能的名称</param>
        public static SkillEffectData CreateInvisible(float opacity, float duration, string sourceName = "")
        {
            return new SkillEffectData(SkillEffectType.Invisible, opacity, duration, sourceName);
        }

        /// <summary>
        /// 创建速度提升效果。
        /// </summary>
        /// <param name="boostAmount">速度提升量（0.1 = 快 10%）</param>
        /// <param name="duration">持续时间（秒）</param>
        /// <param name="sourceName">源技能的名称</param>
        public static SkillEffectData CreateSpeedBoost(float boostAmount, float duration, string sourceName = "")
        {
            return new SkillEffectData(SkillEffectType.SpeedBoost, boostAmount, duration, sourceName);
        }

        /// <summary>
        /// 创建无敌效果。
        /// </summary>
        /// <param name="duration">持续时间（秒）</param>
        /// <param name="sourceName">源技能的名称</param>
        public static SkillEffectData CreateInvulnerable(float duration, string sourceName = "")
        {
            return new SkillEffectData(SkillEffectType.Invulnerable, 1f, duration, sourceName);
        }
        #endregion

        #region Overrides
        public override string ToString()
        {
            return $"{Type} (Value: {Value:F2}, Remaining: {RemainingTime:F1}s/{Duration:F1}s)";
        }
        #endregion
    }
}
