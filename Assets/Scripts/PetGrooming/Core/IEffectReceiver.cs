namespace PetGrooming.Core
{
    /// <summary>
    /// 可以接收技能效果的对象接口。
    /// 由 PetAI 和 GroomerController 实现，用于处理减速、眩晕等效果。
    /// </summary>
    public interface IEffectReceiver
    {
        /// <summary>
        /// 向此接收者应用技能效果。
        /// </summary>
        /// <param name="effect">要应用的效果数据</param>
        void ApplyEffect(SkillEffectData effect);
        
        /// <summary>
        /// 从此接收者移除特定类型的效果。
        /// </summary>
        /// <param name="effectType">要移除的效果类型</param>
        void RemoveEffect(SkillEffectType effectType);
        
        /// <summary>
        /// 检查此接收者当前是否有特定类型的效果。
        /// </summary>
        /// <param name="effectType">要检查的效果类型</param>
        /// <returns>如果效果处于活动状态则返回 True</returns>
        bool HasEffect(SkillEffectType effectType);
        
        /// <summary>
        /// 清除此接收者的所有活动效果。
        /// </summary>
        void ClearAllEffects();
    }
}
