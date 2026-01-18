using System;
using UnityEngine;
using PetGrooming.Core;
using PetGrooming.AI;

namespace PetGrooming.Systems.Skills
{
    /// <summary>
    /// 管理猫咪宠物的所有技能。
    /// 集成敏捷跳跃、毛发干扰和躲入缝隙技能。
    /// 实现技能使用的 AI 决策逻辑。
    /// 需求：4.1, 4.6
    /// </summary>
    public class CatSkillManager : MonoBehaviour
    {
        #region Serialized Fields
        [Header("技能")]
        [Tooltip("敏捷跳跃技能组件")]
        public AgileJumpSkill AgileJump;
        
        [Tooltip("毛发干扰技能组件")]
        public FurDistractionSkill FurDistraction;
        
        [Tooltip("躲入缝隙技能组件")]
        public HideInGapSkill HideInGap;
        
        [Header("AI 决策设置")]
        [Tooltip("考虑使用敏捷跳跃的距离")]
        public float AgileJumpTriggerDistance = 5f;
        
        [Tooltip("考虑使用毛发干扰的距离")]
        public float FurDistractionTriggerDistance = 8f;
        
        [Tooltip("考虑使用躲入缝隙的距离")]
        public float HideInGapTriggerDistance = 6f;
        
        [Tooltip("技能使用尝试之间的最小时间间隔")]
        public float SkillDecisionInterval = 1f;
        
        [Tooltip("技能使用的随机机会因子 (0-1)")]
        [Range(0f, 1f)]
        public float SkillUsageChance = 0.7f;
        
        [Header("配置")]
        [Tooltip("阶段 2 游戏配置")]
        public Phase2GameConfig GameConfig;
        #endregion

        #region Private Fields
        private SkillBase[] _allSkills;
        private PetAI _ownerPet;
        private float _lastDecisionTime;
        private bool _isInitialized;
        #endregion

        #region Properties
        /// <summary>
        /// 由此管理器管理的所有技能的数组。
        /// 需求 4.1: 猫咪有 3 个技能。
        /// </summary>
        public SkillBase[] AllSkills
        {
            get
            {
                if (_allSkills == null || _allSkills.Length == 0)
                {
                    _allSkills = new SkillBase[] { AgileJump, FurDistraction, HideInGap };
                }
                return _allSkills;
            }
        }

        /// <summary>
        /// 可用技能的数量。
        /// </summary>
        public int SkillCount => 3;

        /// <summary>
        /// 对所有者宠物的引用。
        /// </summary>
        public PetAI OwnerPet => _ownerPet;
        #endregion

        #region Events
        /// <summary>
        /// 当任何技能激活时触发。
        /// </summary>
        public event Action<int, SkillBase> OnSkillActivated;
        
        /// <summary>
        /// 当技能激活失败（冷却中）时触发。
        /// </summary>
        public event Action<int, SkillBase> OnSkillActivationFailed;
        
        /// <summary>
        /// 当 AI 决定使用技能时触发。
        /// </summary>
        public event Action<SkillBase> OnAISkillDecision;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            InitializeSkills();
        }

        private void Start()
        {
            SetupSkillOwners();
            _isInitialized = true;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 设置此技能管理器的所有者宠物。
        /// </summary>
        /// <param name="pet">拥有这些技能的宠物</param>
        public void SetOwner(PetAI pet)
        {
            _ownerPet = pet;
            SetupSkillOwners();
        }

        /// <summary>
        /// 评估当前情况并策略性地使用技能。
        /// 需求 4.6: 猫咪 AI 根据距离和状态策略性地使用技能。
        /// </summary>
        /// <param name="pet">使用技能的宠物</param>
        /// <param name="groomer">用于评估的美容师</param>
        public void EvaluateAndUseSkills(PetAI pet, GroomerController groomer)
        {
            if (pet == null || groomer == null) return;
            
            // 检查决策间隔
            if (Time.time - _lastDecisionTime < SkillDecisionInterval) return;
            _lastDecisionTime = Time.time;
            
            // 计算到美容师的距离
            float distance = Vector3.Distance(pet.transform.position, groomer.transform.position);
            
            // 获取宠物状态
            PetAI.PetState state = pet.CurrentState;
            
            // 评估并可能使用技能
            SkillBase skillToUse = EvaluateSkillChoice(distance, state, groomer);
            
            if (skillToUse != null && ShouldUseSkill())
            {
                TryActivateSkill(GetSkillIndex(skillToUse));
                OnAISkillDecision?.Invoke(skillToUse);
            }
        }

        /// <summary>
        /// 尝试按索引激活技能。
        /// </summary>
        /// <param name="skillIndex">技能索引 (0-2)</param>
        /// <returns>如果激活成功则为 True</returns>
        public bool TryActivateSkill(int skillIndex)
        {
            SkillBase skill = GetSkill(skillIndex);
            if (skill == null)
            {
                Debug.LogWarning($"[CatSkillManager] Invalid skill index: {skillIndex}");
                return false;
            }
            
            if (skill.TryActivate())
            {
                OnSkillActivated?.Invoke(skillIndex, skill);
                Debug.Log($"[CatSkillManager] Activated skill {skillIndex}: {skill.SkillName}");
                return true;
            }
            else
            {
                OnSkillActivationFailed?.Invoke(skillIndex, skill);
                Debug.Log($"[CatSkillManager] Skill {skillIndex} ({skill.SkillName}) on cooldown: {skill.RemainingCooldown:F1}s");
                return false;
            }
        }

        /// <summary>
        /// 按索引获取技能。
        /// </summary>
        /// <param name="index">技能索引 (0-2)</param>
        /// <returns>给定索引处的技能，如果无效则为 null</returns>
        public SkillBase GetSkill(int index)
        {
            switch (index)
            {
                case 0: return AgileJump;
                case 1: return FurDistraction;
                case 2: return HideInGap;
                default: return null;
            }
        }

        /// <summary>
        /// 获取技能的索引。
        /// </summary>
        /// <param name="skill">要查找的技能</param>
        /// <returns>技能的索引，如果未找到则为 -1</returns>
        public int GetSkillIndex(SkillBase skill)
        {
            if (skill == AgileJump) return 0;
            if (skill == FurDistraction) return 1;
            if (skill == HideInGap) return 2;
            return -1;
        }

        /// <summary>
        /// 检查技能是否已就绪。
        /// </summary>
        /// <param name="skillIndex">技能索引</param>
        /// <returns>如果技能已就绪则为 True</returns>
        public bool IsSkillReady(int skillIndex)
        {
            SkillBase skill = GetSkill(skillIndex);
            return skill != null && skill.IsReady;
        }

        /// <summary>
        /// 获取技能的剩余冷却时间。
        /// </summary>
        /// <param name="skillIndex">技能索引</param>
        /// <returns>剩余冷却时间（秒）</returns>
        public float GetSkillCooldown(int skillIndex)
        {
            SkillBase skill = GetSkill(skillIndex);
            return skill != null ? skill.RemainingCooldown : 0f;
        }

        /// <summary>
        /// 重置所有技能冷却时间。
        /// </summary>
        public void ResetAllCooldowns()
        {
            foreach (SkillBase skill in AllSkills)
            {
                if (skill != null)
                {
                    skill.ResetCooldown();
                }
            }
        }
        #endregion

        #region Private Methods
        private void InitializeSkills()
        {
            // 如果未分配则创建技能组件
            if (AgileJump == null)
            {
                AgileJump = GetComponentInChildren<AgileJumpSkill>();
                if (AgileJump == null)
                {
                    AgileJump = gameObject.AddComponent<AgileJumpSkill>();
                }
            }
            
            if (FurDistraction == null)
            {
                FurDistraction = GetComponentInChildren<FurDistractionSkill>();
                if (FurDistraction == null)
                {
                    FurDistraction = gameObject.AddComponent<FurDistractionSkill>();
                }
            }
            
            if (HideInGap == null)
            {
                HideInGap = GetComponentInChildren<HideInGapSkill>();
                if (HideInGap == null)
                {
                    HideInGap = gameObject.AddComponent<HideInGapSkill>();
                }
            }
            
            // 如果可用则应用配置
            if (GameConfig != null)
            {
                ApplyConfig();
            }
            
            // 重建技能数组
            _allSkills = new SkillBase[] { AgileJump, FurDistraction, HideInGap };
        }

        private void ApplyConfig()
        {
            if (GameConfig == null) return;
            
#if UNITY_EDITOR
            if (AgileJump != null)
            {
                AgileJump.SetConfigForTesting(GameConfig);
            }
            
            if (FurDistraction != null)
            {
                FurDistraction.SetConfigForTesting(GameConfig);
            }
            
            if (HideInGap != null)
            {
                HideInGap.SetConfigForTesting(GameConfig);
            }
#endif
        }

        private void SetupSkillOwners()
        {
            if (AgileJump != null)
            {
                AgileJump.SetOwner(_ownerPet);
            }
            
            if (FurDistraction != null)
            {
                FurDistraction.SetOwner(_ownerPet);
            }
            
            if (HideInGap != null)
            {
                HideInGap.SetOwner(_ownerPet);
            }
        }

        /// <summary>
        /// 根据当前情况评估要使用的技能。
        /// 需求 4.6: 基于距离和状态的策略性技能使用。
        /// </summary>
        private SkillBase EvaluateSkillChoice(float distanceToGroomer, PetAI.PetState state, GroomerController groomer)
        {
            // 基于优先级的技能选择
            
            // 1. 如果美容师非常近且我们正在逃跑，尝试躲藏
            if (state == PetAI.PetState.Fleeing && distanceToGroomer <= HideInGapTriggerDistance)
            {
                if (HideInGap != null && HideInGap.IsReady && !HideInGap.IsHiding)
                {
                    return HideInGap;
                }
            }
            
            // 2. 如果美容师在中等距离，投掷毛发干扰
            if (distanceToGroomer <= FurDistractionTriggerDistance && distanceToGroomer > AgileJumpTriggerDistance)
            {
                if (FurDistraction != null && FurDistraction.IsReady)
                {
                    return FurDistraction;
                }
            }
            
            // 3. 如果美容师很近且我们需要逃跑，使用敏捷跳跃
            if (state == PetAI.PetState.Fleeing && distanceToGroomer <= AgileJumpTriggerDistance)
            {
                if (AgileJump != null && AgileJump.IsReady && !AgileJump.IsJumping)
                {
                    return AgileJump;
                }
            }
            
            // 4. 后备：如果任何技能已就绪且美容师在范围内，考虑使用它
            if (distanceToGroomer <= FurDistractionTriggerDistance)
            {
                // 按优先级顺序检查技能
                if (HideInGap != null && HideInGap.IsReady && !HideInGap.IsHiding)
                {
                    return HideInGap;
                }
                if (FurDistraction != null && FurDistraction.IsReady)
                {
                    return FurDistraction;
                }
                if (AgileJump != null && AgileJump.IsReady && !AgileJump.IsJumping)
                {
                    return AgileJump;
                }
            }
            
            return null;
        }

        /// <summary>
        /// 根据随机机会确定是否应使用技能。
        /// </summary>
        private bool ShouldUseSkill()
        {
            return UnityEngine.Random.value <= SkillUsageChance;
        }
        #endregion

        #region Static Methods (Testable)
        /// <summary>
        /// 验证所有必需的技能是否存在。
        /// 需求 4.1: 猫咪有 3 个技能。
        /// </summary>
        /// <param name="agileJump">敏捷跳跃技能</param>
        /// <param name="furDistraction">毛发干扰技能</param>
        /// <param name="hideInGap">躲入缝隙技能</param>
        /// <returns>如果所有技能都存在则为 True</returns>
        public static bool ValidateSkillsPresent(SkillBase agileJump, SkillBase furDistraction, SkillBase hideInGap)
        {
            return agileJump != null && furDistraction != null && hideInGap != null;
        }

        /// <summary>
        /// 获取猫咪的预期技能数量。
        /// 需求 4.1: 猫咪有 3 个技能。
        /// </summary>
        /// <returns>预期技能数量 (3)</returns>
        public static int GetExpectedSkillCount()
        {
            return 3;
        }

        /// <summary>
        /// 根据距离和状态评估要使用的最佳技能。
        /// 需求 4.6: 策略性技能使用。
        /// </summary>
        /// <param name="distance">到美容师的距离</param>
        /// <param name="state">当前宠物状态</param>
        /// <param name="agileJumpReady">敏捷跳跃是否就绪</param>
        /// <param name="furDistractionReady">毛发干扰是否就绪</param>
        /// <param name="hideInGapReady">躲入缝隙是否就绪</param>
        /// <param name="agileJumpDistance">敏捷跳跃的触发距离</param>
        /// <param name="furDistractionDistance">毛发干扰的触发距离</param>
        /// <param name="hideInGapDistance">躲入缝隙的触发距离</param>
        /// <returns>推荐技能的索引 (0-2)，如果没有则为 -1</returns>
        public static int EvaluateBestSkill(
            float distance, 
            PetAI.PetState state,
            bool agileJumpReady,
            bool furDistractionReady,
            bool hideInGapReady,
            float agileJumpDistance = 5f,
            float furDistractionDistance = 8f,
            float hideInGapDistance = 6f)
        {
            // Priority 1: Hide when fleeing and close
            if (state == PetAI.PetState.Fleeing && distance <= hideInGapDistance && hideInGapReady)
            {
                return 2; // HideInGap
            }
            
            // Priority 2: Fur distraction at medium range
            if (distance <= furDistractionDistance && distance > agileJumpDistance && furDistractionReady)
            {
                return 1; // FurDistraction
            }
            
            // Priority 3: Agile jump when close and fleeing
            if (state == PetAI.PetState.Fleeing && distance <= agileJumpDistance && agileJumpReady)
            {
                return 0; // AgileJump
            }
            
            // Fallback: Any ready skill in range
            if (distance <= furDistractionDistance)
            {
                if (hideInGapReady) return 2;
                if (furDistractionReady) return 1;
                if (agileJumpReady) return 0;
            }
            
            return -1; // 不推荐任何技能
        }
        #endregion

        #region Editor Support
#if UNITY_EDITOR
        /// <summary>
        /// 设置用于测试的配置。
        /// </summary>

        public void SetConfigForTesting(Phase2GameConfig config)
        {
            GameConfig = config;
            ApplyConfig();
        }
        
        /// <summary>
        /// 设置用于测试的技能。
        /// </summary>
        public void SetSkillsForTesting(AgileJumpSkill agileJump, FurDistractionSkill furDistraction, HideInGapSkill hideInGap)
        {
            AgileJump = agileJump;
            FurDistraction = furDistraction;
            HideInGap = hideInGap;
            _allSkills = new SkillBase[] { AgileJump, FurDistraction, HideInGap };
        }

        /// <summary>
        /// 设置用于测试的所有者宠物。
        /// </summary>
        public void SetOwnerForTesting(PetAI pet)
        {
            _ownerPet = pet;
        }
#endif
        #endregion
    }
}
