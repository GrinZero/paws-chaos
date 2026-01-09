using System;
using UnityEngine;
using PetGrooming.Core;
using PetGrooming.AI;

namespace PetGrooming.Systems.Skills
{
    /// <summary>
    /// Manages all skills for Dog pets.
    /// Integrates Power Charge, Intimidating Bark, and Steal Tool skills.
    /// Implements AI decision logic for skill usage.
    /// Requirements: 5.1, 5.6
    /// </summary>
    public class DogSkillManager : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Skills")]
        [Tooltip("Power Charge skill component")]
        public PowerChargeSkill PowerCharge;
        
        [Tooltip("Intimidating Bark skill component")]
        public IntimidatingBarkSkill IntimidatingBark;
        
        [Tooltip("Steal Tool skill component")]
        public StealToolSkill StealTool;
        
        [Header("AI Decision Settings")]
        [Tooltip("Distance at which to consider using Power Charge")]
        public float PowerChargeTriggerDistance = 4f;
        
        [Tooltip("Distance at which to consider using Intimidating Bark")]
        public float IntimidatingBarkTriggerDistance = 5f;
        
        [Tooltip("Distance at which to consider using Steal Tool")]
        public float StealToolTriggerDistance = 6f;
        
        [Tooltip("Minimum time between skill usage attempts")]
        public float SkillDecisionInterval = 1f;
        
        [Tooltip("Random chance factor for skill usage (0-1)")]
        [Range(0f, 1f)]
        public float SkillUsageChance = 0.7f;
        
        [Header("Configuration")]
        [Tooltip("Phase 2 game configuration")]
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
        /// Array of all skills managed by this manager.
        /// Requirement 5.1: Dog has 3 skills.
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
        /// Number of skills available.
        /// </summary>
        public int SkillCount => 3;

        /// <summary>
        /// Reference to the owner pet.
        /// </summary>
        public PetAI OwnerPet => _ownerPet;
        #endregion

        #region Events
        /// <summary>
        /// Fired when any skill is activated.
        /// </summary>
        public event Action<int, SkillBase> OnSkillActivated;
        
        /// <summary>
        /// Fired when a skill activation fails (on cooldown).
        /// </summary>
        public event Action<int, SkillBase> OnSkillActivationFailed;
        
        /// <summary>
        /// Fired when the AI decides to use a skill.
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
        /// Sets the owner pet for this skill manager.
        /// </summary>
        /// <param name="pet">The pet that owns these skills</param>
        public void SetOwner(PetAI pet)
        {
            _ownerPet = pet;
            SetupSkillOwners();
        }

        /// <summary>
        /// Evaluates the current situation and uses skills strategically.
        /// Requirement 5.6: Dog AI uses skills strategically based on distance and state.
        /// </summary>
        /// <param name="pet">The pet using the skills</param>
        /// <param name="groomer">The groomer to evaluate against</param>
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
        /// Evaluates which skill to use based on current situation.
        /// Requirement 5.6: Strategic skill usage based on distance and state.
        /// </summary>
        private SkillBase EvaluateSkillChoice(float distanceToGroomer, PetAI.PetState state, GroomerController groomer, bool groomerCarryingPet)
        {
            // Priority-based skill selection
            
            // 1. HIGHEST PRIORITY: If groomer is carrying a pet, use Power Charge to free them
            // Requirement 5.3: Power Charge releases captured pets
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
        /// Determines if a skill should be used based on random chance.
        /// </summary>
        private bool ShouldUseSkill()
        {
            return UnityEngine.Random.value <= SkillUsageChance;
        }
        #endregion

        #region Static Methods (Testable)
        /// <summary>
        /// Validates that all required skills are present.
        /// Requirement 5.1: Dog has 3 skills.
        /// </summary>
        /// <param name="powerCharge">Power Charge skill</param>
        /// <param name="intimidatingBark">Intimidating Bark skill</param>
        /// <param name="stealTool">Steal Tool skill</param>
        /// <returns>True if all skills are present</returns>
        public static bool ValidateSkillsPresent(SkillBase powerCharge, SkillBase intimidatingBark, SkillBase stealTool)
        {
            return powerCharge != null && intimidatingBark != null && stealTool != null;
        }

        /// <summary>
        /// Gets the expected skill count for Dog.
        /// Requirement 5.1: Dog has 3 skills.
        /// </summary>
        /// <returns>Expected skill count (3)</returns>
        public static int GetExpectedSkillCount()
        {
            return 3;
        }

        /// <summary>
        /// Evaluates the best skill to use based on distance and state.
        /// Requirement 5.6: Strategic skill usage.
        /// </summary>
        /// <param name="distance">Distance to groomer</param>
        /// <param name="state">Current pet state</param>
        /// <param name="groomerCarryingPet">Whether groomer is carrying a pet</param>
        /// <param name="powerChargeReady">Whether Power Charge is ready</param>
        /// <param name="intimidatingBarkReady">Whether Intimidating Bark is ready</param>
        /// <param name="stealToolReady">Whether Steal Tool is ready</param>
        /// <param name="stealToolCanActivate">Whether Steal Tool can activate (station in range)</param>
        /// <param name="powerChargeDistance">Trigger distance for Power Charge</param>
        /// <param name="intimidatingBarkDistance">Trigger distance for Intimidating Bark</param>
        /// <returns>Index of recommended skill (0-2) or -1 if none</returns>
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
        /// Sets config for testing purposes.
        /// </summary>
        public void SetConfigForTesting(Phase2GameConfig config)
        {
            GameConfig = config;
            ApplyConfig();
        }
        
        /// <summary>
        /// Sets skills for testing purposes.
        /// </summary>
        public void SetSkillsForTesting(PowerChargeSkill powerCharge, IntimidatingBarkSkill intimidatingBark, StealToolSkill stealTool)
        {
            PowerCharge = powerCharge;
            IntimidatingBark = intimidatingBark;
            StealTool = stealTool;
            _allSkills = new SkillBase[] { PowerCharge, IntimidatingBark, StealTool };
        }

        /// <summary>
        /// Sets the owner pet for testing.
        /// </summary>
        public void SetOwnerForTesting(PetAI pet)
        {
            _ownerPet = pet;
        }
#endif
        #endregion
    }
}
