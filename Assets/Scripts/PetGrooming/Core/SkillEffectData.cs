using System;
using UnityEngine;

namespace PetGrooming.Core
{
    /// <summary>
    /// Enum defining all possible skill effect types.
    /// Requirements: 3.2, 3.7, 4.4, 5.4
    /// </summary>
    public enum SkillEffectType
    {
        /// <summary>
        /// Reduces movement speed by a percentage.
        /// Used by: Capture Net (50%), Intimidating Bark (20%)
        /// </summary>
        Slow,
        
        /// <summary>
        /// Completely prevents movement and actions.
        /// Used by: Calming Spray (1 second)
        /// </summary>
        Stun,
        
        /// <summary>
        /// Makes the target invisible or semi-transparent.
        /// Used by: Hide In Gap (Cat skill)
        /// </summary>
        Invisible,
        
        /// <summary>
        /// Increases movement speed by a percentage.
        /// Used by: Alert state (Groomer 10% bonus)
        /// </summary>
        SpeedBoost,
        
        /// <summary>
        /// Prevents the target from being captured or affected by skills.
        /// Used by: Pet Cage release (3 seconds)
        /// </summary>
        Invulnerable
    }

    /// <summary>
    /// Data structure representing an active skill effect on a character.
    /// Tracks effect type, value, and remaining duration.
    /// Requirements: 3.2, 3.7, 4.4, 5.4
    /// </summary>
    [Serializable]
    public class SkillEffectData
    {
        #region Fields
        /// <summary>
        /// The type of effect being applied.
        /// </summary>
        public SkillEffectType Type;
        
        /// <summary>
        /// The magnitude of the effect (e.g., 0.5 for 50% slow).
        /// </summary>
        public float Value;
        
        /// <summary>
        /// Total duration of the effect in seconds.
        /// </summary>
        public float Duration;
        
        /// <summary>
        /// Remaining time for the effect in seconds.
        /// </summary>
        public float RemainingTime;
        
        /// <summary>
        /// Optional source identifier for tracking which skill applied this effect.
        /// </summary>
        public string SourceSkillName;
        #endregion

        #region Properties
        /// <summary>
        /// Whether the effect has expired.
        /// </summary>
        public bool IsExpired => RemainingTime <= 0f;
        
        /// <summary>
        /// Normalized progress of the effect (0 = just started, 1 = expired).
        /// </summary>
        public float Progress => Duration > 0f ? 1f - (RemainingTime / Duration) : 1f;
        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor for serialization.
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
        /// Creates a new skill effect with the specified parameters.
        /// </summary>
        /// <param name="type">The type of effect</param>
        /// <param name="value">The magnitude of the effect</param>
        /// <param name="duration">Duration in seconds</param>
        /// <param name="sourceSkillName">Optional name of the skill that created this effect</param>
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
        /// Updates the remaining time by subtracting delta time.
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update</param>
        /// <returns>True if the effect is still active, false if expired</returns>
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
        /// Resets the effect duration to its original value.
        /// </summary>
        public void ResetDuration()
        {
            RemainingTime = Duration;
        }

        /// <summary>
        /// Extends the effect duration by the specified amount.
        /// </summary>
        /// <param name="additionalTime">Time to add in seconds</param>
        public void ExtendDuration(float additionalTime)
        {
            RemainingTime += additionalTime;
            Duration += additionalTime;
        }

        /// <summary>
        /// Creates a copy of this effect data.
        /// </summary>
        /// <returns>A new SkillEffectData with the same values</returns>
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
        /// Creates a slow effect.
        /// </summary>
        /// <param name="slowAmount">Speed reduction (0.5 = 50% slower)</param>
        /// <param name="duration">Duration in seconds</param>
        /// <param name="sourceName">Name of the source skill</param>
        public static SkillEffectData CreateSlow(float slowAmount, float duration, string sourceName = "")
        {
            return new SkillEffectData(SkillEffectType.Slow, slowAmount, duration, sourceName);
        }

        /// <summary>
        /// Creates a stun effect.
        /// </summary>
        /// <param name="duration">Duration in seconds</param>
        /// <param name="sourceName">Name of the source skill</param>
        public static SkillEffectData CreateStun(float duration, string sourceName = "")
        {
            return new SkillEffectData(SkillEffectType.Stun, 1f, duration, sourceName);
        }

        /// <summary>
        /// Creates an invisibility effect.
        /// </summary>
        /// <param name="opacity">Opacity level (0 = fully invisible, 1 = fully visible)</param>
        /// <param name="duration">Duration in seconds</param>
        /// <param name="sourceName">Name of the source skill</param>
        public static SkillEffectData CreateInvisible(float opacity, float duration, string sourceName = "")
        {
            return new SkillEffectData(SkillEffectType.Invisible, opacity, duration, sourceName);
        }

        /// <summary>
        /// Creates a speed boost effect.
        /// </summary>
        /// <param name="boostAmount">Speed increase (0.1 = 10% faster)</param>
        /// <param name="duration">Duration in seconds</param>
        /// <param name="sourceName">Name of the source skill</param>
        public static SkillEffectData CreateSpeedBoost(float boostAmount, float duration, string sourceName = "")
        {
            return new SkillEffectData(SkillEffectType.SpeedBoost, boostAmount, duration, sourceName);
        }

        /// <summary>
        /// Creates an invulnerability effect.
        /// </summary>
        /// <param name="duration">Duration in seconds</param>
        /// <param name="sourceName">Name of the source skill</param>
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
