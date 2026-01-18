using System;
using UnityEngine;
using PetGrooming.Core;
using PetGrooming.AI;

namespace PetGrooming.Systems.Skills
{
    /// <summary>
    /// 猫咪宠物的毛球干扰技能。
    /// 投掷一个毛球，阻挡美容师视线 2 秒。
    /// 需求 4.3：毛球阻挡视线 2 秒，冷却时间 10 秒。
    /// </summary>
    public class FurDistractionSkill : SkillBase
    {
        #region Serialized Fields
        [Header("毛球干扰设置")]
        [Tooltip("毛球投射物的速度")]
        public float ProjectileSpeed = 10f;
        
        [Tooltip("视线阻挡效果的持续时间（秒）")]
        public float VisionBlockDuration = 2f;
        
        [Tooltip("投射物的最大范围")]
        public float MaxRange = 12f;
        
        [Tooltip("毛球投射物的预制体")]
        public GameObject FurBallPrefab;
        
        [Header("视觉效果设置")]
        [Tooltip("美容师身上视线阻挡效果的预制体")]
        public GameObject VisionBlockEffectPrefab;
        
        [Header("配置")]
        [Tooltip("第二阶段游戏配置")]
        public Phase2GameConfig GameConfig;
        #endregion

        #region Private Fields
        private PetAI _ownerPet;
        private Transform _ownerTransform;
        #endregion

        #region Events
        /// <summary>
        /// 投掷毛球时触发。
        /// </summary>
        public event Action OnFurBallThrown;
        
        /// <summary>
        /// 当毛球撞击到美容师时触发。
        /// </summary>
        public event Action<GroomerController> OnGroomerHit;
        
        /// <summary>
        /// 当视线阻挡效果结束时触发。
        /// </summary>
        public event Action OnVisionBlockEnded;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            
            SkillName = "毛球干扰";
            
            // 如果有配置则应用配置值
            if (GameConfig != null)
            {
                Cooldown = GameConfig.FurDistractionCooldown;
                VisionBlockDuration = GameConfig.FurDistractionDuration;
            }
            else
            {
                // 默认冷却时间：10 秒 (需求 4.3)
                Cooldown = 10f;
                VisionBlockDuration = 2f;
            }
            
            _ownerTransform = transform;
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
        }

        /// <summary>
        /// 激活毛球干扰技能。
        /// 需求 4.3：投掷一个毛球，阻挡美容师视线 2 秒。
        /// </summary>
        public override void Activate()
        {
            base.Activate();
            ThrowFurBall();
        }

        /// <summary>
        /// 当毛球撞击到美容师时调用。
        /// 应用视线阻挡效果。
        /// </summary>
        /// <param name="groomer">被撞击的美容师</param>
        public void OnProjectileHit(GroomerController groomer)
        {
            if (groomer == null) return;
            
            ApplyVisionBlockEffect(groomer, VisionBlockDuration);
            
            // 增加宠物技能撞击美容师的恶作剧值
            // 属性 18：宠物技能撞击恶作剧值
            // 需求 6.6：宠物技能撞击增加 30 分
            if (MischiefSystem.Instance != null)
            {
                MischiefSystem.Instance.AddPetSkillHitMischief();
            }
            
            OnGroomerHit?.Invoke(groomer);
            
            Debug.Log($"[毛球干扰] 撞击了美容师，阻挡视线 {VisionBlockDuration} 秒");
        }
        #endregion

        #region Private Methods
        private void ThrowFurBall()
        {
            Vector3 launchPosition = _ownerTransform.position + Vector3.up * 0.5f;
            
            // 寻找要瞄准的美容师
            GroomerController groomer = FindObjectOfType<GroomerController>();
            Vector3 targetDirection;
            
            if (groomer != null)
            {
                targetDirection = (groomer.transform.position - launchPosition).normalized;
            }
            else
            {
                targetDirection = _ownerTransform.forward;
            }
            
            if (FurBallPrefab != null)
            {
                GameObject projectile = Instantiate(FurBallPrefab, launchPosition, Quaternion.LookRotation(targetDirection));
                
                // 初始化投射物
                FurBallProjectile furBall = projectile.GetComponent<FurBallProjectile>();
                if (furBall != null)
                {
                    furBall.Initialize(this, targetDirection, ProjectileSpeed, MaxRange);
                }
                else
                {
                    // 备选方案：添加基础移动
                    Rigidbody rb = projectile.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.linearVelocity = targetDirection * ProjectileSpeed;
                    }
                    
                    // 在达到最大范围时间后销毁
                    Destroy(projectile, MaxRange / ProjectileSpeed);
                }
            }
            else
            {
                // 没有预制体 - 执行基于射线的命中检测
                PerformRaycastHit(launchPosition, targetDirection);
            }
            
            OnFurBallThrown?.Invoke();
            Debug.Log("[毛球干扰] 投掷了毛球");
        }

        private void PerformRaycastHit(Vector3 origin, Vector3 direction)
        {
            RaycastHit hit;
            if (Physics.Raycast(origin, direction, out hit, MaxRange))
            {
                GroomerController groomer = hit.collider.GetComponent<GroomerController>();
                if (groomer == null)
                {
                    groomer = hit.collider.GetComponentInParent<GroomerController>();
                }
                
                if (groomer != null)
                {
                    OnProjectileHit(groomer);
                }
            }
        }
        #endregion

        #region Static Methods (Testable)
        /// <summary>
        /// 向美容师应用视线阻挡效果。
        /// 需求 4.3：阻挡美容师视线 2 秒。
        /// </summary>
        /// <param name="groomer">受影响的美容师</param>
        /// <param name="duration">效果持续时间（秒）</param>
        public static void ApplyVisionBlockEffect(GroomerController groomer, float duration)
        {
            if (groomer == null) return;
            
            // 创建视线阻挡效果数据
            // 这可以实现为 UI 叠加层或后期处理效果
            // 目前，如果可用，我们将使用效果接收器接口
            IEffectReceiver effectReceiver = groomer.GetComponent<IEffectReceiver>();
            if (effectReceiver != null)
            {
                // 使用自定义效果类型或类似眩晕的效果来实现视线阻挡
                var effect = new SkillEffectData(SkillEffectType.Stun, 0.5f, duration, "Fur Distraction");
                effectReceiver.ApplyEffect(effect);
            }
            
            Debug.Log($"[毛球干扰] 应用了视线阻挡效果，持续 {duration} 秒");
        }

        /// <summary>
        /// 验证视线阻挡持续时间是否符合要求。
        /// 需求 4.3：2 秒视线阻挡。
        /// </summary>
        /// <param name="duration">要验证的持续时间</param>
        /// <returns>如果持续时间符合要求则为 True</returns>
        public static bool ValidateVisionBlockDuration(float duration)
        {
            const float RequiredDuration = 2f;
            const float Tolerance = 0.001f;
            return Mathf.Abs(duration - RequiredDuration) < Tolerance;
        }

        /// <summary>
        /// 验证冷却时间是否符合要求。
        /// 需求 4.3：10 秒冷却时间。
        /// </summary>
        /// <param name="cooldown">要验证的冷却时间</param>
        /// <returns>如果冷却时间符合要求则为 True</returns>
        public static bool ValidateCooldown(float cooldown)
        {
            const float RequiredCooldown = 10f;
            const float Tolerance = 0.001f;
            return Mathf.Abs(cooldown - RequiredCooldown) < Tolerance;
        }

        /// <summary>
        /// 计算投射物飞行时间。
        /// </summary>
        /// <param name="distance">到目标的距离</param>
        /// <param name="speed">投射物速度</param>
        /// <returns>飞行时间（秒）</returns>
        public static float CalculateTravelTime(float distance, float speed)
        {
            if (speed <= 0f) return float.MaxValue;
            return distance / speed;
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
                Cooldown = config.FurDistractionCooldown;
                VisionBlockDuration = config.FurDistractionDuration;
            }
        }
#endif

        private void OnDrawGizmosSelected()
        {
            // 绘制最大范围
            Gizmos.color = Color.magenta;
            Vector3 start = transform.position + Vector3.up * 0.5f;
            Gizmos.DrawLine(start, start + transform.forward * MaxRange);
            Gizmos.DrawWireSphere(start + transform.forward * MaxRange, 0.3f);
        }
        #endregion
    }

    /// <summary>
    /// 毛球的投射物组件。
    /// </summary>
    public class FurBallProjectile : MonoBehaviour
    {
        private FurDistractionSkill _sourceSkill;
        private Vector3 _direction;
        private float _speed;
        private float _maxRange;
        private Vector3 _startPosition;
        private bool _hasHit;

        public void Initialize(FurDistractionSkill skill, Vector3 direction, float speed, float maxRange)
        {
            _sourceSkill = skill;
            _direction = direction.normalized;
            _speed = speed;
            _maxRange = maxRange;
            _startPosition = transform.position;
            _hasHit = false;
        }

        private void Update()
        {
            if (_hasHit) return;
            
            // 移动投射物
            transform.position += _direction * _speed * Time.deltaTime;
            
            // 检查是否超过了最大范围
            float distanceTraveled = Vector3.Distance(_startPosition, transform.position);
            if (distanceTraveled >= _maxRange)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hasHit) return;
            
            GroomerController groomer = other.GetComponent<GroomerController>();
            if (groomer == null)
            {
                groomer = other.GetComponentInParent<GroomerController>();
            }
            
            if (groomer != null)
            {
                _hasHit = true;
                _sourceSkill?.OnProjectileHit(groomer);
                Destroy(gameObject);
            }
        }
    }
}
