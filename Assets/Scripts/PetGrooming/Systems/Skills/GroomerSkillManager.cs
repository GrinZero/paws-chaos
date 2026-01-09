using System;
using UnityEngine;
using UnityEngine.InputSystem;
using PetGrooming.Core;

namespace PetGrooming.Systems.Skills
{
    /// <summary>
    /// Manages all skills for the Groomer character.
    /// Integrates Capture Net, Leash, and Calming Spray skills.
    /// Requirement: 3.1
    /// </summary>
    public class GroomerSkillManager : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Skills")]
        [Tooltip("Capture Net skill component")]
        public CaptureNetSkill CaptureNet;
        
        [Tooltip("Leash skill component")]
        public LeashSkill Leash;
        
        [Tooltip("Calming Spray skill component")]
        public CalmingSpraySkill CalmingSpray;
        
        [Header("Input Settings")]
        [Tooltip("Key to activate Capture Net (Skill 1)")]
        public KeyCode Skill1Key = KeyCode.Alpha1;
        
        [Tooltip("Key to activate Leash (Skill 2)")]
        public KeyCode Skill2Key = KeyCode.Alpha2;
        
        [Tooltip("Key to activate Calming Spray (Skill 3)")]
        public KeyCode Skill3Key = KeyCode.Alpha3;
        
        [Header("Configuration")]
        [Tooltip("Phase 2 game configuration")]
        public Phase2GameConfig GameConfig;
        #endregion

        #region Private Fields
        private SkillBase[] _allSkills;
        private Transform _ownerTransform;
        #endregion

        #region Properties
        /// <summary>
        /// Array of all skills managed by this manager.
        /// </summary>
        public SkillBase[] AllSkills
        {
            get
            {
                if (_allSkills == null || _allSkills.Length == 0)
                {
                    _allSkills = new SkillBase[] { CaptureNet, Leash, CalmingSpray };
                }
                return _allSkills;
            }
        }

        /// <summary>
        /// Number of skills available.
        /// </summary>
        public int SkillCount => 3;
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
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _ownerTransform = transform;
            InitializeSkills();
        }

        private void Start()
        {
            SetupSkillOwners();
        }

        private void Update()
        {
            HandleSkillInput();
        }
        #endregion

        #region Public Methods
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
                Debug.LogWarning($"[GroomerSkillManager] Invalid skill index: {skillIndex}");
                return false;
            }
            
            if (skill.TryActivate())
            {
                OnSkillActivated?.Invoke(skillIndex, skill);
                Debug.Log($"[GroomerSkillManager] Activated skill {skillIndex}: {skill.SkillName}");
                return true;
            }
            else
            {
                OnSkillActivationFailed?.Invoke(skillIndex, skill);
                Debug.Log($"[GroomerSkillManager] Skill {skillIndex} ({skill.SkillName}) on cooldown: {skill.RemainingCooldown:F1}s");
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
                case 0: return CaptureNet;
                case 1: return Leash;
                case 2: return CalmingSpray;
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
            if (skill == CaptureNet) return 0;
            if (skill == Leash) return 1;
            if (skill == CalmingSpray) return 2;
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

        /// <summary>
        /// Sets the owner transform for all skills.
        /// </summary>
        /// <param name="owner">The owner transform</param>
        public void SetOwner(Transform owner)
        {
            _ownerTransform = owner;
            SetupSkillOwners();
        }
        #endregion

        #region Private Methods
        private void InitializeSkills()
        {
            // Create skill components if not assigned
            if (CaptureNet == null)
            {
                CaptureNet = GetComponentInChildren<CaptureNetSkill>();
                if (CaptureNet == null)
                {
                    CaptureNet = gameObject.AddComponent<CaptureNetSkill>();
                }
            }
            
            if (Leash == null)
            {
                Leash = GetComponentInChildren<LeashSkill>();
                if (Leash == null)
                {
                    Leash = gameObject.AddComponent<LeashSkill>();
                }
            }
            
            if (CalmingSpray == null)
            {
                CalmingSpray = GetComponentInChildren<CalmingSpraySkill>();
                if (CalmingSpray == null)
                {
                    CalmingSpray = gameObject.AddComponent<CalmingSpraySkill>();
                }
            }
            
            // Apply config if available
            if (GameConfig != null)
            {
                ApplyConfig();
            }
            
            // Rebuild skills array
            _allSkills = new SkillBase[] { CaptureNet, Leash, CalmingSpray };
        }

        private void ApplyConfig()
        {
            if (GameConfig == null) return;
            
#if UNITY_EDITOR
            if (CaptureNet != null)
            {
                CaptureNet.SetConfigForTesting(GameConfig);
            }
            
            if (Leash != null)
            {
                Leash.SetConfigForTesting(GameConfig);
            }
            
            if (CalmingSpray != null)
            {
                CalmingSpray.SetConfigForTesting(GameConfig);
            }
#endif
        }

        private void SetupSkillOwners()
        {
            if (CaptureNet != null)
            {
                CaptureNet.SetOwner(_ownerTransform);
            }
            
            if (Leash != null)
            {
                Leash.SetOwner(_ownerTransform);
            }
            
            if (CalmingSpray != null)
            {
                CalmingSpray.SetOwner(_ownerTransform);
            }
        }

        private void HandleSkillInput()
        {
            // Check for game state if GameManager exists
            if (GameManager.Instance != null && 
                GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            {
                return;
            }
            
            if (WasKeyPressedThisFrame(Skill1Key))
            {
                TryActivateSkill(0);
            }
            else if (WasKeyPressedThisFrame(Skill2Key))
            {
                TryActivateSkill(1);
            }
            else if (WasKeyPressedThisFrame(Skill3Key))
            {
                TryActivateSkill(2);
            }
        }
        
        /// <summary>
        /// Checks if a key was pressed this frame using the new Input System.
        /// </summary>
        private bool WasKeyPressedThisFrame(KeyCode keyCode)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return false;
            
            Key key = KeyCodeToKey(keyCode);
            return key != Key.None && keyboard[key].wasPressedThisFrame;
        }
        
        /// <summary>
        /// Converts legacy KeyCode to new Input System Key.
        /// </summary>
        private static Key KeyCodeToKey(KeyCode keyCode)
        {
            return keyCode switch
            {
                KeyCode.E => Key.E,
                KeyCode.F => Key.F,
                KeyCode.Alpha1 => Key.Digit1,
                KeyCode.Alpha2 => Key.Digit2,
                KeyCode.Alpha3 => Key.Digit3,
                KeyCode.Space => Key.Space,
                KeyCode.LeftShift => Key.LeftShift,
                KeyCode.RightShift => Key.RightShift,
                _ => Key.None
            };
        }
        #endregion

        #region Static Methods (Testable)
        /// <summary>
        /// Validates that all required skills are present.
        /// </summary>
        /// <param name="captureNet">Capture Net skill</param>
        /// <param name="leash">Leash skill</param>
        /// <param name="calmingSpray">Calming Spray skill</param>
        /// <returns>True if all skills are present</returns>
        public static bool ValidateSkillsPresent(SkillBase captureNet, SkillBase leash, SkillBase calmingSpray)
        {
            return captureNet != null && leash != null && calmingSpray != null;
        }

        /// <summary>
        /// Gets the expected skill count for the Groomer.
        /// Requirement 3.1: Groomer has 3 skills.
        /// </summary>
        /// <returns>Expected skill count (3)</returns>
        public static int GetExpectedSkillCount()
        {
            return 3;
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
        public void SetSkillsForTesting(CaptureNetSkill captureNet, LeashSkill leash, CalmingSpraySkill calmingSpray)
        {
            CaptureNet = captureNet;
            Leash = leash;
            CalmingSpray = calmingSpray;
            _allSkills = new SkillBase[] { CaptureNet, Leash, CalmingSpray };
        }
#endif
        #endregion
    }
}
