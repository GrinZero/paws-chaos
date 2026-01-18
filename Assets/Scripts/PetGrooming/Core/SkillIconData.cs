using UnityEngine;

namespace PetGrooming.Core
{
    /// <summary>
/// 包含技能图标配置数据的 ScriptableObject。
/// 为每个技能定义图标、主题颜色和发光效果。
/// 需求：3.5.1-3.5.8
/// </summary>
    [CreateAssetMenu(fileName = "SkillIconData", menuName = "PetGrooming/SkillIconData")]
    public class SkillIconData : ScriptableObject
    {
        #region Skill Icon Entry
        /// <summary>
        /// 单个技能图标的配置。
        /// </summary>
        [System.Serializable]
        public class SkillIconEntry
        {
            [Tooltip("技能的唯一标识符")]
            public string SkillId;
            
            [Tooltip("技能的主图标精灵")]
            public Sprite Icon;
            
            [Tooltip("技能的主题颜色")]
            public Color ThemeColor = Color.white;
            
            [Tooltip("发光效果精灵（可选）")]
            public Sprite GlowSprite;
            
            [Tooltip("冷却状态的去饱和图标（如果为 null 则自动生成）")]
            public Sprite CooldownIcon;
        }
        #endregion

        #region Groomer Skills
        [Header("Groomer Skills")]
        [Tooltip("捕获网技能 - 蓝色网/网格视觉效果")]
        public SkillIconEntry CaptureNet = new SkillIconEntry
        {
            SkillId = "capture_net",
            ThemeColor = new Color(0.29f, 0.56f, 0.85f, 1f) // #4A90D9 Blue
        };
        
        [Tooltip("牵引绳技能 - 绿色绳索/钩子视觉效果")]
        public SkillIconEntry Leash = new SkillIconEntry
        {
            SkillId = "leash",
            ThemeColor = new Color(0.36f, 0.72f, 0.36f, 1f) // #5CB85C Green
        };
        
        [Tooltip("镇静喷雾技能 - 紫色喷雾/雾气视觉效果")]
        public SkillIconEntry CalmingSpray = new SkillIconEntry
        {
            SkillId = "calming_spray",
            ThemeColor = new Color(0.61f, 0.35f, 0.71f, 1f) // #9B59B6 Purple
        };
        
        [Tooltip("捕获/抓取按钮 - 金色/黄色手形视觉效果")]
        public SkillIconEntry CaptureButton = new SkillIconEntry
        {
            SkillId = "capture",
            ThemeColor = new Color(0.96f, 0.65f, 0.14f, 1f) // #F5A623 Gold
        };
        #endregion

        #region Pet Skills
        [Header("Pet Skills")]
        [Tooltip("挣扎按钮 - 橙红色断裂链条视觉效果")]
        public SkillIconEntry StruggleButton = new SkillIconEntry
        {
            SkillId = "struggle",
            ThemeColor = new Color(0.91f, 0.30f, 0.24f, 1f) // #E74C3C Orange-Red
        };
        #endregion

        #region Helper Methods
        /// <summary>
        /// 通过技能 ID 获取技能图标条目。
        /// </summary>
        /// <param name="skillId">技能标识符</param>
        /// <returns>技能的 SkillIconEntry，如果未找到则返回 null</returns>
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
        /// 获取所有 groomer 技能图标条目。
        /// </summary>
        /// <returns>Groomer 技能图标条目的数组</returns>
        public SkillIconEntry[] GetGroomerSkillIcons()
        {
            return new SkillIconEntry[] { CaptureNet, Leash, CalmingSpray };
        }

        /// <summary>
        /// 验证所有必需的图标是否已分配。
        /// </summary>
        /// <returns>如果所有图标都有效则返回 True</returns>
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
