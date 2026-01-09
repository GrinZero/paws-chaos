namespace PetGrooming.Core
{
    /// <summary>
    /// Interface for objects that can receive skill effects.
    /// Implemented by PetAI and GroomerController to handle effects like slow, stun, etc.
    /// </summary>
    public interface IEffectReceiver
    {
        /// <summary>
        /// Applies a skill effect to this receiver.
        /// </summary>
        /// <param name="effect">The effect data to apply</param>
        void ApplyEffect(SkillEffectData effect);
        
        /// <summary>
        /// Removes a specific effect type from this receiver.
        /// </summary>
        /// <param name="effectType">The type of effect to remove</param>
        void RemoveEffect(SkillEffectType effectType);
        
        /// <summary>
        /// Checks if this receiver currently has a specific effect type.
        /// </summary>
        /// <param name="effectType">The type of effect to check</param>
        /// <returns>True if the effect is active</returns>
        bool HasEffect(SkillEffectType effectType);
        
        /// <summary>
        /// Clears all active effects from this receiver.
        /// </summary>
        void ClearAllEffects();
    }
}
