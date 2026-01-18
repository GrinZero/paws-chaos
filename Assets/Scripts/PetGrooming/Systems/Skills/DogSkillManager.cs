using System;
using UnityEngine;
using PetGrooming.Core;
using PetGrooming.AI;

namespace PetGrooming.Systems.Skills
{
    /// <summary>
    /// 管理狗狗宠物的所有技能。
    /// 集成强力冲锋、威吓吠叫和偷取工具技能。
    /// 实现技能使用的 AI 决策逻辑。
    /// 需求：5.1, 5.6
    /// </summary>
    public class DogSkillManager : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Skills")]
        [Tooltip("强力冲锋技能组件")]
        public PowerChargeSkill PowerCharge;
        
        [Tooltip("威吓吠叫技能组件")]
        public IntimidatingBarkSkill IntimidatingBark;
        
        [Tooltip("偷取工具技能组件")]
        public StealToolSkill StealTool;
        
        [Header("AI Decision Settings")]
        [Tooltip("考虑使用强力冲锋的距离")]
        public float PowerChargeTriggerDistance = 4f;
        
        [Tooltip("考虑使用威吓吠叫的距离")]
        public float IntimidatingBarkTriggerDistance = 5f;
        
        [Tooltip("考虑使用偷取工具的距离")]
        public float StealToolTriggerDistance = 6f;
        
        [Tooltip("技能使用尝试之间的最小时间间隔")]
        public float SkillDecisionInterval = 1f;
        
        [Tooltip("技能使用的随机机会因子 (0-1)")]
        [Range(0f, 1f)]
        public float SkillUsageChance = 0.7f;
        
        [Header("Configuration")]
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
        /// 需求 5.1: 狗狗有 3 个技能。
        /// </summary>
        public SkillBase[] AllSkills
        {
            get
            {
                if (_allSkills == null || _allSkills.Length == 0)
                {
                    _allSkills = new SkillBase[] { PowerCharge, IntimidatingBark, StealTool };
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
        /// 需求 5.6: 狗狗 AI 根据距离和状态策略性地使用技能。
        /// </summary>
        /// <param name="pet">使用技能的宠物</param>
        /// <param name="groomer">用于评估的美容师</param>
        public void EvaluateAndUseSkills(PetAI pet, GroomerController groomer)
        {
            if (pet == null || groomer == null) return;
            
            // Check decision interval
            if (Time.time - _lastDecisionTime < SkillDecisionInterval) return;
            _lastDecisionTime = Time.time;
            
            // Calculate distance to groomer
            float distance = Vector3.Distance(pet.transform.position, groomer.transform.position);
            
            // Get pet state
            PetAI.PetState state = pet.CurrentState;
            
            // Check if groomer is carrying a pet (priority target for Power Charge)
            bool groomerCarryingPet = groomer.IsCarryingPet;
            
            // Evaluate and potentially use a skill
            SkillBase skillToUse = EvaluateSkillChoice(distance, state, groomer, groomerCarryingPet);
            
            if (skillToUse != null && ShouldUseSkill())
            {
                TryActivateSkill(GetSkillIndex(skillToUse));
                OnAISkillDecision?.Invoke(skillToUse);
            }
        }

        /// <summary>
        /// Attempts to activate a skill by index.
        /// </summary>
        /// <param name="skillIndex">Index of the skill (0-2)</param>
        /// <returns>True if activation was successful</returns>
        public bool TryActivateSkill(int skillIndex)
        {
            SkillBase skill = GetSkill(skillIndex);
            if (skill == null)
            {
                Debug.LogWarning($"[DogSkillManager] Invalid skill index: {skillIndex}");
                return false;
            }
            
            if (skill.TryActivate())
            {
                OnSkillActivated?.Invoke(skillIndex, skill);
                Debug.Log($"[DogSkillManager] Activated skill {skillIndex}: {skill.SkillName}");
                return true;
            }
            else
            {
                OnSkillActivationFailed?.Invoke(skillIndex, skill);
                Debug.Log($"[DogSkillManager] Skill {skillIndex} ({skill.SkillName}) on cooldown: {skill.RemainingCooldown:F1}s");
                return false;
            }
        }

        /// <summary>
        /// Gets a skill by index.
        /// </summary>
        /// <param name="index">Index of the skill (0-2)</param>
        /// <returns>The skill at the given index, or null if invalid</returns>
        public SkillBase GetSkill(int index)
        {
            switch (index)
            {
                case 0: return PowerCharge;
                case 1: return IntimidatingBark;
                case 2: return StealTool;
                default: return null;
            }
        }

        /// <summary>
        /// Gets the index of a skill.
        /// </summary>
        /// <param name="skill">The skill to find</param>
        /// <returns>Index of the skill, or -1 if not found</returns>
        public int GetSkillIndex(SkillBase skill)
        {
            if (skill == PowerCharge) return 0;
            if (skill == IntimidatingBark) return 1;
            if (skill == StealTool) return 2;
            return -1;
        }

        /// <summary>
        /// Checks if a skill is ready by index.
        /// </summary>
        /// <param name="skillIndex">Index of the skill</param>
        /// <returns>True if the skill is ready</returns>
        public bool IsSkillReady(int skillIndex)
        {
            SkillBase skill = GetSkill(skillIndex);
            return skill != null && skill.IsReady;
        }

        /// <summary>
        /// Gets the remaining cooldown for a skill.
        /// </summary>
        /// <param name="skillIndex">Index of the skill</param>
        /// <returns>Remaining cooldown in seconds</returns>
        public float GetSkillCooldown(int skillIndex)
        {
            SkillBase skill = GetSkill(skillIndex);
            return skill != null ? skill.RemainingCooldown : 0f;
        }

        /// <summary>
        /// Resets all skill cooldowns.
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
            // Create skill components if not assigned
            if (PowerCharge == null)
            {
                PowerCharge = GetComponentInChildren<PowerChargeSkill>();
                if (PowerCharge == null)
                {
                    PowerCharge = gameObject.AddComponent<PowerChargeSkill>();
                }
            }
            
            if (IntimidatingBark == null)
            {
                IntimidatingBark = GetComponentInChildren<IntimidatingBarkSkill>();
                if (IntimidatingBark == null)
                {
                    IntimidatingBark = gameObject.AddComponent<IntimidatingBarkSkill>();
                }
            }
            
            if (StealTool == null)
            {
                StealTool = GetComponentInChildren<StealToolSkill>();
                if (StealTool == null)
                {
                    StealTool = gameObject.AddComponent<StealToolSkill>();
                }
            }
            
            // Apply config if available
            if (GameConfig != null)
            {
                ApplyConfig();
            }
            
            // Rebuild skills array
            _allSkills = new SkillBase[] { PowerCharge, IntimidatingBark, StealTool };
        }

        private void ApplyConfig()
        {
            if (GameConfig == null) return;
            
#if UNITY_EDITOR
            if (PowerCharge != null)
            {
                PowerCharge.SetConfigForTesting(GameConfig);
            }
            
            if (IntimidatingBark != null)
            {
                IntimidatingBark.SetConfigForTesting(GameConfig);
            }
            
            if (StealTool != null)
            {
                StealTool.SetConfigForTesting(GameConfig);
            }
#endif
        }

        private void SetupSkillOwners()
        {
            if (PowerCharge != null)
            {
                PowerCharge.SetOwner(_ownerPet);
            }
            
            if (IntimidatingBark != null)
            {
                IntimidatingBark.SetOwner(_ownerPet);
            }
            
            if (StealTool != null)
            {
                StealTool.SetOwner(_ownerPet);
            }
        }

        /// <summary>
        /// 根据当前情况评估要使用的技能。
        /// 需求 5.6: 基于距离和状态的策略性技能使用。
        /// </summary>
        private SkillBase EvaluateSkillChoice(float distanceToGroomer, PetAI.PetState state, GroomerController groomer, bool groomerCarryingPet)
        {
            // 基于优先级的技能选择
            
            // 1. HIGHEST PRIORITY: If groomer is carrying a pet, use Power Charge to free them
            // 需求 5.3: 强力冲锋释放被捕获的宠物
            if (groomerCarryingPet && distanceToGroomer <= PowerChargeTriggerDistance)
            {
                if (PowerCharge != null && PowerCharge.IsReady && !PowerCharge.IsCharging)
                {
                    return PowerCharge;
                }
            }
            
            // 2. If groomer is close, use Intimidating Bark to slow them down
            if (distanceToGroomer <= IntimidatingBarkTriggerDistance)
            {
                if (IntimidatingBark != null && IntimidatingBark.IsReady)
                {
                    return IntimidatingBark;
                }
            }
            
            // 3. If near a grooming station, try to steal tools
            if (StealTool != null && StealTool.IsReady && StealTool.CanActivate())
            {
                return StealTool;
            }
            
            // 4. If groomer is approaching and we're fleeing, use Power Charge to knock them back
            if (state == PetAI.PetState.Fleeing && distanceToGroomer <= PowerChargeTriggerDistance)
            {
                if (PowerCharge != null && PowerCharge.IsReady && !PowerCharge.IsCharging)
                {
                    return PowerCharge;
                }
            }
            
            // 5. Fallback: If any skill is ready and groomer is in range, consider using it
            if (distanceToGroomer <= IntimidatingBarkTriggerDistance)
            {
                // Check skills in priority order
                if (IntimidatingBark != null && IntimidatingBark.IsReady)
                {
                    return IntimidatingBark;
                }
                if (PowerCharge != null && PowerCharge.IsReady && !PowerCharge.IsCharging)
                {
                    return PowerCharge;
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
        /// 需求 5.1: 狗狗有 3 个技能。
        /// </summary>
        /// <param name="powerCharge">强力冲锋技能</param>
        /// <param name="intimidatingBark">威吓吠叫技能</param>
        /// <param name="stealTool">偷取工具技能</param>
        /// <returns>如果所有技能都存在则为 True</returns>
        public static bool ValidateSkillsPresent(SkillBase powerCharge, SkillBase intimidatingBark, SkillBase stealTool)
        {
            return powerCharge != null && intimidatingBark != null && stealTool != null;
        }

        /// <summary>
        /// 获取狗狗的预期技能数量。
        /// 需求 5.1: 狗狗有 3 个技能。
        /// </summary>
        /// <returns>预期技能数量 (3)</returns>
        public static int GetExpectedSkillCount()
        {
            return 3;
        }

        /// <summary>
        /// 根据距离和状态评估要使用的最佳技能。
        /// 需求 5.6: 策略性技能使用。
        /// </summary>
        /// <param name="distance">到美容师的距离</param>
        /// <param name="state">当前宠物状态</param>
        /// <param name="groomerCarryingPet">美容师是否正抱着宠物</param>
        /// <param name="powerChargeReady">强力冲锋是否就绪</param>
        /// <param name="intimidatingBarkReady">威吓吠叫是否就绪</param>
        /// <param name="stealToolReady">偷取工具是否就绪</param>
        /// <param name="stealToolCanActivate">偷取工具是否可以激活（工作台在范围内）</param>
        /// <param name="powerChargeDistance">强力冲锋的触发距离</param>
        /// <param name="intimidatingBarkDistance">威吓吠叫的触发距离</param>
        /// <returns>推荐技能的索引 (0-2)，如果没有则为 -1</returns>
        public static int EvaluateBestSkill(
            float distance, 
            PetAI.PetState state,
            bool groomerCarryingPet,
            bool powerChargeReady,
            bool intimidatingBarkReady,
            bool stealToolReady,
            bool stealToolCanActivate,
            float powerChargeDistance = 4f,
            float intimidatingBarkDistance = 5f)
        {
            // Priority 1: Free captured pet with Power Charge
            if (groomerCarryingPet && distance <= powerChargeDistance && powerChargeReady)
            {
                return 0; // PowerCharge
            }
            
            // Priority 2: Slow groomer with Intimidating Bark
            if (distance <= intimidatingBarkDistance && intimidatingBarkReady)
            {
                return 1; // IntimidatingBark
            }
            
            // Priority 3: Steal tool if possible
            if (stealToolReady && stealToolCanActivate)
            {
                return 2; // StealTool
            }
            
            // Priority 4: Power Charge when fleeing
            if (state == PetAI.PetState.Fleeing && distance <= powerChargeDistance && powerChargeReady)
            {
                return 0; // PowerCharge
            }
            
            // Fallback: Any ready skill in range
            if (distance <= intimidatingBarkDistance)
            {
                if (intimidatingBarkReady) return 1;
                if (powerChargeReady) return 0;
            }
            
            return -1; // No skill recommended
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
        public void SetSkillsForTesting(PowerChargeSkill powerCharge, IntimidatingBarkSkill intimidatingBark, StealToolSkill stealTool)
        {
            PowerCharge = powerCharge;
            IntimidatingBark = intimidatingBark;
            StealTool = stealTool;
            _allSkills = new SkillBase[] { PowerCharge, IntimidatingBark, StealTool };
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
