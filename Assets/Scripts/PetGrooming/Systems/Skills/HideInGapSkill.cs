using System;
using UnityEngine;
using PetGrooming.Core;
using PetGrooming.AI;

namespace PetGrooming.Systems.Skills
{
    /// <summary>
    /// 猫咪宠物的钻空躲藏技能。
    /// 在静止时使猫咪隐身，在移动时半透明。
    /// 需求 4.4：静止时隐身 3 秒，冷却时间 14 秒。
    /// 需求 4.5：使用技能移动时半透明（50% 不透明度）。
    /// </summary>
    public class HideInGapSkill : SkillBase
    {
        #region Serialized Fields
        [Header("钻空躲藏设置")]
        [Tooltip("隐身效果的持续时间（秒）")]
        public float InvisibilityDuration = 3f;
        
        [Tooltip("静止时的不透明度 (0 = 完全隐身)")]
        [Range(0f, 1f)]
        public float StationaryOpacity = 0f;
        
        [Tooltip("移动时的不透明度 (0.5 = 50% 可见)")]
        [Range(0f, 1f)]
        public float MovingOpacity = 0.5f;
        
        [Tooltip("用于判断猫咪是否正在移动的移动阈值")]
        public float MovementThreshold = 0.1f;
        
        [Header("配置")]
        [Tooltip("第二阶段游戏配置")]
        public Phase2GameConfig GameConfig;
        #endregion

        #region Private Fields
        private PetAI _ownerPet;
        private Transform _ownerTransform;
        private bool _isHiding;
        private float _hideTimer;
        private Vector3 _lastPosition;
        private float _currentOpacity;
        #endregion

        #region Properties
        /// <summary>
        /// 猫咪当前是否正在躲藏。
        /// </summary>
        public bool IsHiding => _isHiding;
        
        /// <summary>
        /// 猫咪当前的不透明度 (0 = 隐身, 1 = 完全可见)。
        /// </summary>
        public float CurrentOpacity => _currentOpacity;
        
        /// <summary>
        /// 躲藏效果的剩余时间。
        /// </summary>
        public float RemainingHideTime => _hideTimer;
        #endregion

        #region Events
        /// <summary>
        /// 当躲藏开始时触发。
        /// </summary>
        public event Action OnHideStarted;
        
        /// <summary>
        /// 当躲藏结束时触发。
        /// </summary>
        public event Action OnHideEnded;
        
        /// <summary>
        /// 当不透明度改变时触发。参数是新的不透明度值。
        /// </summary>
        public event Action<float> OnOpacityChanged;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            
            SkillName = "钻空躲藏";
            
            // 如果有配置则应用配置值
            if (GameConfig != null)
            {
                Cooldown = GameConfig.HideInGapCooldown;
                InvisibilityDuration = GameConfig.HideInGapDuration;
                MovingOpacity = GameConfig.HideInGapMovingOpacity;
            }
            else
            {
                // 默认冷却时间：14 秒 (需求 4.4)
                Cooldown = 14f;
                InvisibilityDuration = 3f;
                MovingOpacity = 0.5f;
            }
            
            _ownerTransform = transform;
            _currentOpacity = 1f;
        }

        protected override void Update()
        {
            base.Update();
            
            if (_isHiding)
            {
                UpdateHideState();
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 为该技能设置所有者宠物。
        /// </summary>
        public void SetOwner(PetAI pet)
        {
            _ownerPet = pet;
            _ownerTransform = pet != null ? pet.transform : transform;
            _lastPosition = _ownerTransform.position;
        }

        /// <summary>
        /// 检查技能是否可以激活。
        /// </summary>
        public override bool CanActivate()
        {
            // 如果准备就绪且当前未在躲藏，则可以激活
            return base.CanActivate() && !_isHiding;
        }

        /// <summary>
        /// 激活钻空躲藏技能。
        /// 需求 4.4：在静止时隐身 3 秒。
        /// </summary>
        public override void Activate()
        {
            base.Activate();
            StartHiding();
        }

        /// <summary>
        /// 提前取消躲藏效果。
        /// </summary>
        public void CancelHide()
        {
            if (_isHiding)
            {
                EndHiding();
            }
        }

        /// <summary>
        /// 根据移动情况获取当前的可见性状态。
        /// </summary>
        /// <returns>如果猫咪当前可见（躲藏时移动），则为 True</returns>
        public bool IsCurrentlyVisible()
        {
            if (!_isHiding) return true;
            return _currentOpacity > 0f;
        }
        #endregion

        #region Private Methods
        private void StartHiding()
        {
            _isHiding = true;
            _hideTimer = InvisibilityDuration;
            _lastPosition = _ownerTransform.position;
            
            // 开始时完全隐身（静止）
            SetOpacity(StationaryOpacity);
            
            // 对宠物应用隐身效果
            if (_ownerPet != null)
            {
                _ownerPet.SetInvisible(true, StationaryOpacity, InvisibilityDuration);
            }
            
            OnHideStarted?.Invoke();
            Debug.Log("[钻空躲藏] 躲藏开始");
        }

        private void UpdateHideState()
        {
            _hideTimer -= Time.deltaTime;
            
            // 检查猫咪是否正在移动
            bool isMoving = IsMoving(_ownerTransform.position, _lastPosition, MovementThreshold, Time.deltaTime);
            _lastPosition = _ownerTransform.position;
            
            // 根据移动情况更新不透明度
            // 需求 4.4：静止时完全隐身
            // 需求 4.5：移动时半透明 (50%)
            float targetOpacity = CalculateOpacity(isMoving, StationaryOpacity, MovingOpacity);
            
            if (!Mathf.Approximately(_currentOpacity, targetOpacity))
            {
                SetOpacity(targetOpacity);
                
                // 更新宠物的隐身状态
                if (_ownerPet != null)
                {
                    _ownerPet.UpdateInvisibilityOpacity(isMoving);
                }
            }
            
            // 检查躲藏持续时间是否已结束
            if (_hideTimer <= 0f)
            {
                EndHiding();
            }
        }

        private void EndHiding()
        {
            _isHiding = false;
            _hideTimer = 0f;
            
            // 恢复完全可见
            SetOpacity(1f);
            
            // 移除宠物的隐身效果
            if (_ownerPet != null)
            {
                _ownerPet.SetInvisible(false);
            }
            
            OnHideEnded?.Invoke();
            Debug.Log("[钻空躲藏] 躲藏结束");
        }

        private void SetOpacity(float opacity)
        {
            _currentOpacity = opacity;
            OnOpacityChanged?.Invoke(opacity);
        }
        #endregion

        #region Static Methods (Testable)
        /// <summary>
        /// 根据位置变化判断猫咪是否正在移动。
        /// </summary>
        /// <param name="currentPosition">当前位置</param>
        /// <param name="lastPosition">上一个位置</param>
        /// <param name="threshold">移动阈值</param>
        /// <param name="deltaTime">自上次检查以来的时间</param>
        /// <returns>如果正在移动则为 True</returns>
        public static bool IsMoving(Vector3 currentPosition, Vector3 lastPosition, float threshold, float deltaTime)
        {
            if (deltaTime <= 0f) return false;
            
            float distance = Vector3.Distance(currentPosition, lastPosition);
            float speed = distance / deltaTime;
            
            return speed > threshold;
        }

        /// <summary>
        /// 根据移动状态计算不透明度。
        /// 属性 11：钻空躲藏可见性状态
        /// 需求 4.4：静止时隐身 (0%)
        /// 需求 4.5：移动时半透明 (50%)
        /// </summary>
        /// <param name="isMoving">猫咪是否正在移动</param>
        /// <param name="stationaryOpacity">静止时的不透明度</param>
        /// <param name="movingOpacity">移动时的不透明度</param>
        /// <returns>目标不透明度值</returns>
        public static float CalculateOpacity(bool isMoving, float stationaryOpacity, float movingOpacity)
        {
            return isMoving ? movingOpacity : stationaryOpacity;
        }

        /// <summary>
        /// 验证可见性状态是否符合要求。
        /// 属性 11：钻空躲藏可见性状态
        /// </summary>
        /// <param name="isMoving">猫咪是否正在移动</param>
        /// <param name="opacity">当前不透明度</param>
        /// <returns>如果不透明度符合预期状态则为 True</returns>
        public static bool ValidateVisibilityState(bool isMoving, float opacity)
        {
            const float Tolerance = 0.001f;
            
            if (isMoving)
            {
                // 需求 4.5：移动时 50% 不透明度
                return Mathf.Abs(opacity - 0.5f) < Tolerance;
            }
            else
            {
                // 需求 4.4：静止时 0% 不透明度（隐身）
                return Mathf.Abs(opacity - 0f) < Tolerance;
            }
        }

        /// <summary>
        /// 验证冷却时间是否符合要求。
        /// 需求 4.4：14 秒冷却时间。
        /// </summary>
        /// <param name="cooldown">要验证的冷却时间</param>
        /// <returns>如果冷却时间符合要求则为 True</returns>
        public static bool ValidateCooldown(float cooldown)
        {
            const float RequiredCooldown = 14f;
            const float Tolerance = 0.001f;
            return Mathf.Abs(cooldown - RequiredCooldown) < Tolerance;
        }

        /// <summary>
        /// 验证隐身持续时间是否符合要求。
        /// 需求 4.4：3 秒持续时间。
        /// </summary>
        /// <param name="duration">要验证的持续时间</param>
        /// <returns>如果持续时间符合要求则为 True</returns>
        public static bool ValidateDuration(float duration)
        {
            const float RequiredDuration = 3f;
            const float Tolerance = 0.001f;
            return Mathf.Abs(duration - RequiredDuration) < Tolerance;
        }

        /// <summary>
        /// 获取给定移动状态下的预期不透明度。
        /// 用于属性测试。
        /// </summary>
        /// <param name="isMoving">猫咪是否正在移动</param>
        /// <returns>预期不透明度值</returns>
        public static float GetExpectedOpacity(bool isMoving)
        {
            // 需求 4.4：静止时 0%
            // 需求 4.5：移动时 50%
            return isMoving ? 0.5f : 0f;
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
            if (config != null)
            {
                Cooldown = config.HideInGapCooldown;
                InvisibilityDuration = config.HideInGapDuration;
                MovingOpacity = config.HideInGapMovingOpacity;
            }
        }

        /// <summary>
        /// 设置用于测试的躲藏状态。
        /// </summary>
        public void SetHidingStateForTesting(bool isHiding, float remainingTime = 0f)
        {
            _isHiding = isHiding;
            _hideTimer = remainingTime;
        }

        /// <summary>
        /// 设置用于测试的不透明度。
        /// </summary>
        public void SetOpacityForTesting(float opacity)
        {
            _currentOpacity = opacity;
        }

        /// <summary>
        /// 设置用于测试移动检测的上一个位置。
        /// </summary>
        public void SetLastPositionForTesting(Vector3 position)
        {
            _lastPosition = position;
        }
#endif

        private void OnDrawGizmosSelected()
        {
            // 绘制可见性指示器
            if (_isHiding)
            {
                Gizmos.color = new Color(0f, 1f, 1f, _currentOpacity);
                Gizmos.DrawWireSphere(transform.position, 0.5f);
            }
        }
        #endregion
    }
}
