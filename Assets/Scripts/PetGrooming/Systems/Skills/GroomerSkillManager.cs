using System;
using UnityEngine;
using UnityEngine.InputSystem;
using PetGrooming.Core;
using PetGrooming.Systems;
using StarterAssets;

namespace PetGrooming.Systems.Skills
{
    /// <summary>
    /// 管理美容师角色的所有技能。
    /// 集成捕获网、牵引绳和镇静喷雾技能。
    /// 需求：3.1
    /// </summary>
    public class GroomerSkillManager : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Skills")]
        [Tooltip("捕获网技能组件")]
        public CaptureNetSkill CaptureNet;
        
        [Tooltip("牵引绳技能组件")]
        public LeashSkill Leash;
        
        [Tooltip("镇静喷雾技能组件")]
        public CalmingSpraySkill CalmingSpray;
        
        [Header("Input Settings")]
        [Tooltip("激活捕获网的按键（技能 1）")]
        public KeyCode Skill1Key = KeyCode.Alpha1;
        
        [Tooltip("激活牵引绳的按键（技能 2）")]
        public KeyCode Skill2Key = KeyCode.Alpha2;
        
        [Tooltip("激活镇静喷雾的按键（技能 3）")]
        public KeyCode Skill3Key = KeyCode.Alpha3;
        
        [Header("Configuration")]
        [Tooltip("阶段 2 游戏配置")]
        public Phase2GameConfig GameConfig;
        #endregion

        #region Private Fields
        private SkillBase[] _allSkills;
        private Transform _ownerTransform;
        private StarterAssetsInputs _starterAssetsInputs;
        #endregion

        #region Properties
        /// <summary>
        /// 由此管理器管理的所有技能的数组。
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
        /// 可用技能的数量。
        /// </summary>
        public int SkillCount => 3;
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
            SubscribeToInputEvents();
        }

        private void Update()
        {
            HandleSkillInput();
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromInputEvents();
        }
        #endregion

        #region Public Methods
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
        /// 按索引获取技能。
        /// </summary>
        /// <param name="index">技能索引 (0-2)</param>
        /// <returns>给定索引处的技能，如果无效则为 null</returns>
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
        /// 获取技能的索引。
        /// </summary>
        /// <param name="skill">要查找的技能</param>
        /// <returns>技能索引，如果未找到则为 -1</returns>
        public int GetSkillIndex(SkillBase skill)
        {
            if (skill == CaptureNet) return 0;
            if (skill == Leash) return 1;
            if (skill == CalmingSpray) return 2;
            return -1;
        }

        /// <summary>
        /// 按索引检查技能是否就绪。
        /// </summary>
        /// <param name="skillIndex">技能索引</param>
        /// <returns>如果技能就绪则为 True</returns>
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

        /// <summary>
        /// 设置所有技能的所有者变换组件。
        /// </summary>
        /// <param name="owner">所有者变换组件</param>
        public void SetOwner(Transform owner)
        {
            _ownerTransform = owner;
            SetupSkillOwners();
        }
        #endregion

        #region Private Methods
        private void InitializeSkills()
        {
            // 如果未分配则创建技能组件
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
            
            // 如果可用则应用配置
            if (GameConfig != null)
            {
                ApplyConfig();
            }
            
            // 重建技能数组
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
        
        /// <summary>
        /// 订阅 StarterAssetsInputs 的技能事件。
        /// 这样 OnScreenButton 触发的输入也能激活技能。
        /// </summary>
        private void SubscribeToInputEvents()
        {
            _starterAssetsInputs = GetComponent<StarterAssetsInputs>();
            if (_starterAssetsInputs == null)
            {
                _starterAssetsInputs = FindObjectOfType<StarterAssetsInputs>();
            }
            
            if (_starterAssetsInputs != null)
            {
                _starterAssetsInputs.OnSkill1Pressed += OnSkill1Pressed;
                _starterAssetsInputs.OnSkill2Pressed += OnSkill2Pressed;
                _starterAssetsInputs.OnSkill3Pressed += OnSkill3Pressed;
                _starterAssetsInputs.OnCapturePressed += OnCapturePressed;
                Debug.Log("[GroomerSkillManager] 已订阅 StarterAssetsInputs 技能事件");
            }
            else
            {
                Debug.LogWarning("[GroomerSkillManager] 未找到 StarterAssetsInputs，移动端技能按钮可能无法工作");
            }
        }
        
        /// <summary>
        /// 取消订阅事件。
        /// </summary>
        private void UnsubscribeFromInputEvents()
        {
            if (_starterAssetsInputs != null)
            {
                _starterAssetsInputs.OnSkill1Pressed -= OnSkill1Pressed;
                _starterAssetsInputs.OnSkill2Pressed -= OnSkill2Pressed;
                _starterAssetsInputs.OnSkill3Pressed -= OnSkill3Pressed;
                _starterAssetsInputs.OnCapturePressed -= OnCapturePressed;
            }
        }
        
        // Input System 事件回调
        private void OnSkill1Pressed() => TryActivateSkill(0);
        private void OnSkill2Pressed() => TryActivateSkill(1);
        private void OnSkill3Pressed() => TryActivateSkill(2);
        private void OnCapturePressed()
        {
            // Capture 使用 GroomerController 的捕获逻辑
            var groomerController = GetComponent<GroomerController>();
            if (groomerController != null)
            {
                groomerController.TryCapturePet();
                Debug.Log("[美容师技能管理] 触发捕获");
            }
        }

        private void HandleSkillInput()
        {
            // 如果 GameManager 存在，检查游戏状态
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
        /// 使用新输入系统检查本帧是否按下了某个键。
        /// </summary>
        private bool WasKeyPressedThisFrame(KeyCode keyCode)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return false;
            
            Key key = KeyCodeToKey(keyCode);
            return key != Key.None && keyboard[key].wasPressedThisFrame;
        }
        
        /// <summary>
        /// 将旧版 KeyCode 转换为新输入系统 Key。
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
        /// 验证所有必需的技能是否存在。
        /// </summary>
        /// <param name="captureNet">捕获网技能</param>
        /// <param name="leash">牵引绳技能</param>
        /// <param name="calmingSpray">镇静喷雾技能</param>
        /// <returns>如果所有技能都存在则为 True</returns>
        public static bool ValidateSkillsPresent(SkillBase captureNet, SkillBase leash, SkillBase calmingSpray)
        {
            return captureNet != null && leash != null && calmingSpray != null;
        }

        /// <summary>
        /// 获取美容师的预期技能数量。
        /// 需求 3.1: 美容师有 3 个技能。
        /// </summary>
        /// <returns>预期技能数量 (3)</returns>
        public static int GetExpectedSkillCount()
        {
            return 3;
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
