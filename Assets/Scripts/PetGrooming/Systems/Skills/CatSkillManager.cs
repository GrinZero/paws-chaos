using System;
using UnityEngine;
using PetGrooming.Core;
using PetGrooming.AI;

namespace PetGrooming.Systems.Skills
{
    /// <summary>
    /// Manages all skills for Cat pets.
    /// Integrates Agile Jump, Fur Distraction, and Hide In Gap skills.
    /// Implements AI decision logic for skill usage.
    /// Requirements: 4.1, 4.6
    /// </summary>
    public class CatSkillManager : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Skills")]
        [Tooltip("Agile Jump skill component")]
        public AgileJumpSkill AgileJump;
        
        [Tooltip("Fur Distraction skill component")]
        public FurDistractionSkill FurDistraction;
        
        [Tooltip("Hide In Gap skill component")]
        public HideInGapSkill HideInGap;
        
        [Header("AI Decision Settings")]
        [Tooltip("Distance at which to consider using Agile Jump")]
        public float AgileJumpTriggerDistance = 5f;
        
        [Tooltip("Distance at which to consider using Fur Distraction")]
        public float FurDistractionTriggerDistance = 8f;
        
        [Tooltip("Distance at which to consider using Hide In Gap")]
        public float HideInGapTriggerDistance = 6f;
        
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
        /// Requirement 4.1: Cat has 3 skills.
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
        /// Requirement 4.6: Cat AI uses skills strategically based on distance and state.
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
            
            // Evaluate and potentially use a skill
            SkillBase skillToUse = EvaluateSkillChoice(distance, state, groomer);
            
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
        /// Gets a skill by index.
        /// </summary>
        /// <param name="index">Index of the skill (0-2)</param>
        /// <returns>The skill at the given index, or null if invalid</returns>
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
        /// Gets the index of a skill.
        /// </summary>
        /// <param name="skill">The skill to find</param>
        /// <returns>Index of the skill, or -1 if not found</returns>
        public int GetSkillIndex(SkillBase skill)
        {
            if (skill == AgileJump) return 0;
            if (skill == FurDistraction) return 1;
            if (skill == HideInGap) return 2;
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
            
            // Apply config if available
            if (GameConfig != null)
            {
                ApplyConfig();
            }
            
            // Rebuild skills array
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
        /// Evaluates which skill to use based on current situation.
        /// Requirement 4.6: Strategic skill usage based on distance and state.
        /// </summary>
        private SkillBase EvaluateSkillChoice(float distanceToGroomer, PetAI.PetState state, GroomerController groomer)
        {
            // Priority-based skill selection
            
            // 1. If groomer is very close and we're fleeing, try to hide
            if (state == PetAI.PetState.Fleeing && distanceToGroomer <= HideInGapTriggerDistance)
            {
                if (HideInGap != null && HideInGap.IsReady && !HideInGap.IsHiding)
                {
                    return HideInGap;
                }
            }
            
            // 2. If groomer is at medium range, throw fur distraction
            if (distanceToGroomer <= FurDistractionTriggerDistance && distanceToGroomer > AgileJumpTriggerDistance)
            {
                if (FurDistraction != null && FurDistraction.IsReady)
                {
                    return FurDistraction;
                }
            }
            
            // 3. If groomer is close and we need to escape, use agile jump
            if (state == PetAI.PetState.Fleeing && distanceToGroomer <= AgileJumpTriggerDistance)
            {
                if (AgileJump != null && AgileJump.IsReady && !AgileJump.IsJumping)
                {
                    return AgileJump;
                }
            }
            
            // 4. Fallback: If any skill is ready and groomer is in range, consider using it
            if (distanceToGroomer <= FurDistractionTriggerDistance)
            {
                // Check skills in priority order
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
        /// Requirement 4.1: Cat has 3 skills.
        /// </summary>
        /// <param name="agileJump">Agile Jump skill</param>
        /// <param name="furDistraction">Fur Distraction skill</param>
        /// <param name="hideInGap">Hide In Gap skill</param>
        /// <returns>True if all skills are present</returns>
        public static bool ValidateSkillsPresent(SkillBase agileJump, SkillBase furDistraction, SkillBase hideInGap)
        {
            return agileJump != null && furDistraction != null && hideInGap != null;
        }

        /// <summary>
        /// Gets the expected skill count for Cat.
        /// Requirement 4.1: Cat has 3 skills.
        /// </summary>
        /// <returns>Expected skill count (3)</returns>
        public static int GetExpectedSkillCount()
        {
            return 3;
        }

        /// <summary>
        /// Evaluates the best skill to use based on distance and state.
        /// Requirement 4.6: Strategic skill usage.
        /// </summary>
        /// <param name="distance">Distance to groomer</param>
        /// <param name="state">Current pet state</param>
        /// <param name="agileJumpReady">Whether Agile Jump is ready</param>
        /// <param name="furDistractionReady">Whether Fur Distraction is ready</param>
        /// <param name="hideInGapReady">Whether Hide In Gap is ready</param>
        /// <param name="agileJumpDistance">Trigger distance for Agile Jump</param>
        /// <param name="furDistractionDistance">Trigger distance for Fur Distraction</param>
        /// <param name="hideInGapDistance">Trigger distance for Hide In Gap</param>
        /// <returns>Index of recommended skill (0-2) or -1 if none</returns>
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
        public void SetSkillsForTesting(AgileJumpSkill agileJump, FurDistractionSkill furDistraction, HideInGapSkill hideInGap)
        {
            AgileJump = agileJump;
            FurDistraction = furDistraction;
            HideInGap = hideInGap;
            _allSkills = new SkillBase[] { AgileJump, FurDistraction, HideInGap };
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
