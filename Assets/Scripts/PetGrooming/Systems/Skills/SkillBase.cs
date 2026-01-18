using System;
using UnityEngine;

namespace PetGrooming.Systems.Skills
{
    /// <summary>
    /// 游戏中所有技能的抽象基类。
    /// 管理冷却计时、就绪状态检测和事件回调。
    /// 需求：3.3, 3.6, 3.8, 4.2, 4.3, 4.4, 5.2, 5.4, 5.5
    /// </summary>
    public abstract class SkillBase : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Skill Settings")]
        [Tooltip("技能的显示名称")]
        public string SkillName;
        
        [Tooltip("冷却持续时间（秒）")]
        public float Cooldown;
        
        [Tooltip("用于 UI 显示的图标精灵")]
        public Sprite Icon;
        #endregion

        #region Properties
        /// <summary>
        /// 剩余冷却时间（秒）。
        /// </summary>
        public float RemainingCooldown { get; protected set; }
        
        /// <summary>
        /// 技能是否准备好激活（冷却完成）。
        /// </summary>
        public bool IsReady => RemainingCooldown <= 0f;
        
        /// <summary>
        /// 归一化冷却进度（0 = 就绪，1 = 刚激活）。
        /// </summary>
        public float CooldownProgress => Cooldown > 0f ? RemainingCooldown / Cooldown : 0f;
        #endregion

        #region Events
        /// <summary>
        /// 当冷却值改变时触发。参数是剩余冷却时间。
        /// </summary>
        public event Action<float> OnCooldownChanged;
        
        /// <summary>
        /// 当技能成功激活时触发。
        /// </summary>
        public event Action OnSkillActivated;
        
        /// <summary>
        /// 当技能冷却完成变回就绪状态时触发。
        /// </summary>
        public event Action OnSkillReady;
        #endregion

        #region Private Fields
        private bool _wasOnCooldown;
        #endregion

        #region Unity Lifecycle
        protected virtual void Awake()
        {
            // 开始时技能处于就绪状态
            RemainingCooldown = 0f;
            _wasOnCooldown = false;
        }

        protected virtual void Update()
        {
            UpdateCooldown();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 检查技能是否可以激活。
        /// 在派生类中重写以添加额外条件。
        /// </summary>
        /// <returns>如果技能可以激活则为 True</returns>
        public virtual bool CanActivate()
        {
            return IsReady;
        }

        /// <summary>
        /// 尝试激活技能。
        /// </summary>
        /// <returns>如果激活成功则为 True</returns>
        public bool TryActivate()
        {
            if (!CanActivate())
            {
                return false;
            }
            
            Activate();
            return true;
        }

        /// <summary>
        /// 激活技能并开始冷却。
        /// 在派生类中重写以实现特定于技能的行为。
        /// </summary>
        public virtual void Activate()
        {
            StartCooldown();
            OnSkillActivated?.Invoke();
        }

        /// <summary>
        /// 将冷却时间重置为零，使技能立即就绪。
        /// </summary>
        public void ResetCooldown()
        {
            float previousCooldown = RemainingCooldown;
            RemainingCooldown = 0f;
            
            if (previousCooldown > 0f)
            {
                OnCooldownChanged?.Invoke(RemainingCooldown);
                OnSkillReady?.Invoke();
            }
            
            _wasOnCooldown = false;
        }

        /// <summary>
        /// 将冷却时间设置为特定值。
        /// </summary>
        /// <param name="cooldownTime">要设置的冷却时间</param>
        public void SetCooldown(float cooldownTime)
        {
            RemainingCooldown = Mathf.Max(0f, cooldownTime);
            _wasOnCooldown = RemainingCooldown > 0f;
            OnCooldownChanged?.Invoke(RemainingCooldown);
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// 启动冷却计时器。
        /// </summary>
        protected void StartCooldown()
        {
            RemainingCooldown = Cooldown;
            _wasOnCooldown = true;
            OnCooldownChanged?.Invoke(RemainingCooldown);
        }

        /// <summary>
        /// 每帧更新冷却计时器。
        /// </summary>
        protected virtual void UpdateCooldown()
        {
            if (RemainingCooldown <= 0f)
            {
                return;
            }
            
            RemainingCooldown -= Time.deltaTime;
            
            if (RemainingCooldown <= 0f)
            {
                RemainingCooldown = 0f;
                
                if (_wasOnCooldown)
                {
                    _wasOnCooldown = false;
                    OnSkillReady?.Invoke();
                }
            }
            
            OnCooldownChanged?.Invoke(RemainingCooldown);
        }
        #endregion
    }
}
