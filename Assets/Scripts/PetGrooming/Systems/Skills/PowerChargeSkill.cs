using System;
using System.Collections;
using UnityEngine;
using PetGrooming.Core;
using PetGrooming.AI;

namespace PetGrooming.Systems.Skills
{
    /// <summary>
    /// 狗狗宠物的蓄力冲锋技能。
    /// 向前冲刺，击退美容师和障碍物。
    /// 如果美容师抱着被捕获的宠物，宠物将被释放。
    /// 需求：5.2, 5.3
    /// </summary>
    public class PowerChargeSkill : SkillBase
    {
        #region Serialized Fields
        [Header("蓄力冲锋设置")]
        [Tooltip("冲刺距离")]
        public float DashDistance = 5f;
        
        [Tooltip("冲刺速度")]
        public float DashSpeed = 15f;
        
        [Tooltip("施加给美容师和障碍物的击退力")]
        public float KnockbackForce = 10f;
        
        [Tooltip("冲锋期间检测目标的半径")]
        public float ChargeRadius = 1.5f;
        
        [Tooltip("用于检测目标的层掩码")]
        public LayerMask TargetLayers;
        
        [Header("配置")]
        [Tooltip("第二阶段游戏配置")]
        public Phase2GameConfig GameConfig;
        #endregion

        #region Private Fields
        private PetAI _ownerPet;
        private bool _isCharging;
        private Vector3 _chargeDirection;
        private float _chargeDistanceTraveled;
        #endregion

        #region Properties
        /// <summary>
        /// 狗狗当前是否正在冲锋。
        /// </summary>
        public bool IsCharging => _isCharging;
        #endregion

        #region Events
        /// <summary>
        /// 当冲锋撞击到美容师时触发。
        /// </summary>
        public event Action<GroomerController> OnGroomerHit;
        
        /// <summary>
        /// 当冲锋释放了被捕获的宠物时触发。
        /// </summary>
        public event Action<PetAI> OnCapturedPetReleased;
        
        /// <summary>
        /// 当冲锋撞击到障碍物时触发。
        /// </summary>
        public event Action<GameObject> OnObstacleHit;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            
            SkillName = "蓄力冲锋";
            
            // 如果有配置则应用配置值
            if (GameConfig != null)
            {
                Cooldown = GameConfig.PowerChargeCooldown;
                DashDistance = GameConfig.PowerChargeDashDistance;
                KnockbackForce = GameConfig.PowerChargeKnockbackForce;
            }
            else
            {
                // 默认冷却时间：8 秒 (需求 5.2)
                Cooldown = 8f;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 为该技能设置所有者宠物。
        /// </summary>
        public void SetOwner(PetAI owner)
        {
            _ownerPet = owner;
        }

        /// <summary>
        /// 检查技能是否可以激活。
        /// 已经在冲锋时不能激活。
        /// </summary>
        public override bool CanActivate()
        {
            return base.CanActivate() && !_isCharging;
        }

        /// <summary>
        /// 激活蓄力冲锋技能。
        /// 需求 5.2：狗狗向前冲刺，击退美容师和障碍物。
        /// </summary>
        public override void Activate()
        {
            base.Activate();
            StartCharge();
        }

        /// <summary>
        /// 当冲锋撞击到目标时调用。
        /// </summary>
        public void OnChargeHit(Collider other)
        {
            if (!_isCharging) return;
            
            // 检查美容师
            GroomerController groomer = other.GetComponent<GroomerController>();
            if (groomer == null)
            {
                groomer = other.GetComponentInParent<GroomerController>();
            }
            
            if (groomer != null)
            {
                HandleGroomerHit(groomer);
                return;
            }
            
            // 检查可破坏物体
            DestructibleObject destructible = other.GetComponent<DestructibleObject>();
            if (destructible != null)
            {
                HandleObstacleHit(other.gameObject, destructible);
            }
        }
        #endregion

        #region Private Methods
        private void StartCharge()
        {
            if (_ownerPet == null)
            {
                Debug.LogWarning("[蓄力冲锋] 未分配所有者宠物！");
                return;
            }
            
            _isCharging = true;
            _chargeDistanceTraveled = 0f;
            _chargeDirection = _ownerPet.transform.forward;
            
            StartCoroutine(ChargeCoroutine());
            
            Debug.Log("[蓄力冲锋] 冲锋开始！");
        }

        private IEnumerator ChargeCoroutine()
        {
            while (_isCharging && _chargeDistanceTraveled < DashDistance)
            {
                float moveDistance = DashSpeed * Time.deltaTime;
                Vector3 movement = _chargeDirection * moveDistance;
                
                // 移动宠物
                if (_ownerPet != null)
                {
                    _ownerPet.transform.position += movement;
                }
                
                _chargeDistanceTraveled += moveDistance;
                
                // 冲锋期间检查碰撞
                CheckChargeCollisions();
                
                yield return null;
            }
            
            EndCharge();
        }

        private void CheckChargeCollisions()
        {
            if (_ownerPet == null) return;
            
            Collider[] hits = Physics.OverlapSphere(_ownerPet.transform.position, ChargeRadius, TargetLayers);
            
            foreach (Collider hit in hits)
            {
                // 跳过自身
                if (hit.transform == _ownerPet.transform || hit.transform.IsChildOf(_ownerPet.transform))
                {
                    continue;
                }
                
                OnChargeHit(hit);
            }
        }

        private void HandleGroomerHit(GroomerController groomer)
        {
            // 对美容师施加击退
            ApplyKnockback(groomer.transform, KnockbackForce);
            
            // 检查美容师是否正抱着宠物，如果是则释放它
            // 需求 5.3：如果美容师抱着被捕获的宠物，宠物将被释放
            if (groomer.IsCarryingPet && groomer.CapturedPet != null)
            {
                PetAI releasedPet = groomer.CapturedPet;
                ReleaseCapturedPet(groomer);
                OnCapturedPetReleased?.Invoke(releasedPet);
                
                Debug.Log("[蓄力冲锋] 从美容师处释放了被捕获的宠物！");
            }
            
            // 增加宠物技能撞击美容师的恶作剧值
            // 属性 18：宠物技能撞击恶作剧值
            // 需求 6.6：宠物技能撞击增加 30 分
            if (MischiefSystem.Instance != null)
            {
                MischiefSystem.Instance.AddPetSkillHitMischief();
            }
            
            OnGroomerHit?.Invoke(groomer);
            
            Debug.Log("[蓄力冲锋] 撞击了美容师！");
        }

        private void HandleObstacleHit(GameObject obstacle, DestructibleObject destructible)
        {
            // 对障碍物施加击退
            Rigidbody rb = obstacle.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 knockbackDir = (obstacle.transform.position - _ownerPet.transform.position).normalized;
                rb.AddForce(knockbackDir * KnockbackForce, ForceMode.Impulse);
            }
            
            OnObstacleHit?.Invoke(obstacle);
            
            Debug.Log($"[蓄力冲锋] 撞击了障碍物：{obstacle.name}");
        }

        private void EndCharge()
        {
            _isCharging = false;
            Debug.Log("[蓄力冲锋] 冲锋结束。");
        }
        #endregion

        #region Static Methods (Testable)
        /// <summary>
        /// 向目标施加击退力。
        /// </summary>
        /// <param name="target">目标变换组件</param>
        /// <param name="force">击退力</param>
        public static void ApplyKnockback(Transform target, float force)
        {
            if (target == null) return;
            
            Rigidbody rb = target.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // 向远离冲锋的方向施加击退
                Vector3 knockbackDir = target.forward * -1f;
                rb.AddForce(knockbackDir * force, ForceMode.Impulse);
            }
            
            // 同时尝试为美容师使用 CharacterController
            CharacterController cc = target.GetComponent<CharacterController>();
            if (cc != null)
            {
                // CharacterController 不直接支持物理力
                // 击退效果需要由 GroomerController 处理
            }
        }

        /// <summary>
        /// 从美容师处释放被捕获的宠物。
        /// 属性 12：蓄力冲锋释放被捕获的宠物
        /// 需求 5.3：如果美容师抱着被捕获的宠物，宠物将被释放。
        /// </summary>
        /// <param name="groomer">抱着宠物的美容师</param>
        /// <returns>如果释放了宠物则为 True</returns>
        public static bool ReleaseCapturedPet(GroomerController groomer)
        {
            if (groomer == null) return false;
            if (!groomer.IsCarryingPet || groomer.CapturedPet == null) return false;
            
            // 释放宠物
            groomer.ReleasePet();
            return true;
        }

        /// <summary>
        /// 检查蓄力冲锋撞击是否应该释放被捕获的宠物。
        /// 属性 12：蓄力冲锋释放被捕获的宠物
        /// </summary>
        /// <param name="isCarryingPet">美容师是否正抱着宠物</param>
        /// <returns>如果应该释放宠物则为 True</returns>
        public static bool ShouldReleaseCapturedPet(bool isCarryingPet)
        {
            return isCarryingPet;
        }

        /// <summary>
        /// 计算冲锋结束位置。
        /// </summary>
        /// <param name="startPosition">起始位置</param>
        /// <param name="direction">冲锋方向</param>
        /// <param name="distance">冲锋距离</param>
        /// <returns>冲锋结束位置</returns>
        public static Vector3 CalculateChargeEndPosition(Vector3 startPosition, Vector3 direction, float distance)
        {
            return startPosition + direction.normalized * distance;
        }

        /// <summary>
        /// 验证蓄力冲锋冷却时间是否符合要求。
        /// 需求 5.2：8 秒冷却时间
        /// </summary>
        /// <param name="cooldown">要验证的冷却时间</param>
        /// <returns>如果冷却时间符合要求则为 True</returns>
        public static bool ValidateCooldown(float cooldown)
        {
            const float RequiredCooldown = 8f;
            const float Tolerance = 0.001f;
            return Mathf.Abs(cooldown - RequiredCooldown) < Tolerance;
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
                Cooldown = config.PowerChargeCooldown;
                DashDistance = config.PowerChargeDashDistance;
                KnockbackForce = config.PowerChargeKnockbackForce;
            }
        }

        /// <summary>
        /// 设置用于测试的所有者。
        /// </summary>
        public void SetOwnerForTesting(PetAI owner)
        {
            _ownerPet = owner;
        }

        /// <summary>
        /// 设置用于测试的冲锋状态。
        /// </summary>
        public void SetChargingStateForTesting(bool isCharging)
        {
            _isCharging = isCharging;
        }
#endif

        private void OnDrawGizmosSelected()
        {
            // 绘制冲锋半径
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, ChargeRadius);
            
            // 绘制冲锋方向和距离
            if (_ownerPet != null || Application.isPlaying)
            {
                Gizmos.color = Color.yellow;
                Vector3 start = transform.position;
                Vector3 end = start + transform.forward * DashDistance;
                Gizmos.DrawLine(start, end);
                Gizmos.DrawWireSphere(end, 0.3f);
            }
        }
        #endregion
    }
}
