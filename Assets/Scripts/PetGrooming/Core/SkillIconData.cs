using UnityEngine;

namespace PetGrooming.Core
{
    /// <summary>
    /// ScriptableObject containing skill icon configuration data.
    /// Defines icons, theme colors, and glow effects for each skill.
    /// Requirements: 3.5.1-3.5.8
    /// </summary>
    [CreateAssetMenu(fileName = "SkillIconData", menuName = "PetGrooming/SkillIconData")]
    public class SkillIconData : ScriptableObject
    {
        #region Skill Icon Entry
        /// <summary>
        /// Configuration for a single skill icon.
        /// </summary>
        [System.Serializable]
        public class SkillIconEntry
        {
            [Tooltip("Unique identifier for the skill")]
            public string SkillId;
            
            [Tooltip("Main icon sprite for the skill")]
            public Sprite Icon;
            
            [Tooltip("Theme color for the skill")]
            public Color ThemeColor = Color.white;
            
            [Tooltip("Glow effect sprite (optional)")]
            public Sprite GlowSprite;
            
            [Tooltip("Desaturated icon for cooldown state (auto-generated if null)")]
            public Sprite CooldownIcon;
        }
        #endregion

        #region Groomer Skills
        [Header("Groomer Skills")]
        [Tooltip("Capture Net skill - Blue net/mesh visual")]
        public SkillIconEntry CaptureNet = new SkillIconEntry
        {
            SkillId = "capture_net",
            ThemeColor = new Color(0.29f, 0.56f, 0.85f, 1f) // #4A90D9 Blue
        };
        
        [Tooltip("Leash skill - Green rope/hook visual")]
        public SkillIconEntry Leash = new SkillIconEntry
        {
            SkillId = "leash",
            ThemeColor = new Color(0.36f, 0.72f, 0.36f, 1f) // #5CB85C Green
        };
        
        [Tooltip("Calming Spray skill - Purple spray/mist visual")]
        public SkillIconEntry CalmingSpray = new SkillIconEntry
        {
            SkillId = "calming_spray",
            ThemeColor = new Color(0.61f, 0.35f, 0.71f, 1f) // #9B59B6 Purple
        };
        
        [Tooltip("Capture/Grab button - Gold/yellow hand visual")]
        public SkillIconEntry CaptureButton = new SkillIconEntry
        {
            SkillId = "capture",
            ThemeColor = new Color(0.96f, 0.65f, 0.14f, 1f) // #F5A623 Gold
        };
        #endregion

        #region Pet Skills
        [Header("Pet Skills")]
        [Tooltip("Struggle button - Orange/red breaking chains visual")]
        public SkillIconEntry StruggleButton = new SkillIconEntry
        {
            SkillId = "struggle",
            ThemeColor = new Color(0.91f, 0.30f, 0.24f, 1f) // #E74C3C Orange-Red
        };
        #endregion

        #region Helper Methods
        /// <summary>
        /// Gets the icon entry for a skill by its ID.
        /// </summary>
        /// <param name="skillId">The skill identifier</param>
        /// <returns>The SkillIconEntry for the skill, or null if not found</returns>
        public SkillIconEntry GetIconForSkill(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
                return null;

            switch (skillId.ToLower())
            {
                case "capture_net":
                case "capturenet":
                    return CaptureNet;
                case "leash":
                    return Leash;
                case "calming_spray":
                case "calmingspray":
                    return CalmingSpray;
                case "capture":
                case "grab":
                    return CaptureButton;
                case "struggle":
                    return StruggleButton;
                default:
                    Debug.LogWarning($"SkillIconData: Unknown skill ID '{skillId}'");
                    return null;
            }
        }

        /// <summary>
        /// Gets all groomer skill icon entries.
        /// </summary>
        /// <returns>Array of groomer skill icon entries</returns>
        public SkillIconEntry[] GetGroomerSkillIcons()
        {
            return new SkillIconEntry[] { CaptureNet, Leash, CalmingSpray };
        }

        /// <summary>
        /// Validates that all required icons are assigned.
        /// </summary>
        /// <returns>True if all icons are valid</returns>
        public bool ValidateIcons()
        {
            bool isValid = true;
            
            if (CaptureNet.Icon == null)
            {
                Debug.LogWarning("SkillIconData: CaptureNet icon is not assigned");
                isValid = false;
            }
            if (Leash.Icon == null)
            {
                Debug.LogWarning("SkillIconData: Leash icon is not assigned");
                isValid = false;
            }
            if (CalmingSpray.Icon == null)
            {
                Debug.LogWarning("SkillIconData: CalmingSpray icon is not assigned");
                isValid = false;
            }
            if (CaptureButton.Icon == null)
            {
                Debug.LogWarning("SkillIconData: CaptureButton icon is not assigned");
                isValid = false;
            }
            if (StruggleButton.Icon == null)
            {
                Debug.LogWarning("SkillIconData: StruggleButton icon is not assigned");
                isValid = false;
            }
            
            return isValid;
        }
        #endregion
    }
}
