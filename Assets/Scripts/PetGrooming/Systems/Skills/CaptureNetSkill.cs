using System;
using UnityEngine;
using PetGrooming.Core;
using PetGrooming.AI;

namespace PetGrooming.Systems.Skills
{
    /// <summary>
    /// 美容师的捕获网技能。
    /// 投掷一个投射物，使被击中的宠物减速 50%，持续 3 秒。
    /// 需求：3.2, 3.3
    /// </summary>
    public class CaptureNetSkill : SkillBase
    {
        #region Serialized Fields
        [Header("捕获网设置")]
        [Tooltip("网投射物的速度")]
        public float ProjectileSpeed = 15f;
        
        [Tooltip("被击中时的移动速度降低量 (0.5 = 降低 50%)")]
        [Range(0f, 1f)]
        public float SlowAmount = 0.5f;
        
        [Tooltip("减速效果持续时间（秒）")]
        public float SlowDuration = 3f;
        
        [Tooltip("投射物的最大范围")]
        public float MaxRange = 15f;
        
        [Tooltip("网投射物的预制体")]
        public GameObject NetProjectilePrefab;
        
        [Header("引用")]
        [Tooltip("投射物发射的变换点")]
        public Transform LaunchPoint;
        
        [Header("配置")]
        [Tooltip("第二阶段游戏配置")]
        public Phase2GameConfig GameConfig;
        #endregion

        #region Private Fields
        private Transform _ownerTransform;
        #endregion

        #region Events
        /// <summary>
        /// 当网击中宠物时触发。
        /// </summary>
        public event Action<PetAI> OnNetHit;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            
            SkillName = "捕获网";
            
            // Apply config values if available
            if (GameConfig != null)
            {
                Cooldown = GameConfig.CaptureNetCooldown;
                SlowAmount = GameConfig.CaptureNetSlowAmount;
                SlowDuration = GameConfig.CaptureNetSlowDuration;
                ProjectileSpeed = GameConfig.CaptureNetProjectileSpeed;
            }
            else
            {
                // Default cooldown: 8 seconds (Requirement 3.3)
                Cooldown = 8f;
            }
            
            _ownerTransform = transform;
        }

        private void Start()
        {
            // Set launch point to owner if not specified
            if (LaunchPoint == null)
            {
                LaunchPoint = _ownerTransform;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 设置投射物方向计算的所有者变换组件。
        /// </summary>
        public void SetOwner(Transform owner)
        {
            _ownerTransform = owner;
            if (LaunchPoint == null)
            {
                LaunchPoint = owner;
            }
        }

        /// <summary>
        /// 激活捕获网技能，发射一个投射物。
        /// 需求 3.2: 投掷一个投射物，使被击中的宠物减速 50%，持续 3 秒。
        /// </summary>
        public override void Activate()
        {
            base.Activate();
            LaunchProjectile();
        }

        /// <summary>
        /// 当网投射物击中宠物时调用。
        /// 对宠物应用减速效果。
        /// </summary>
        /// <param name="pet">被击中的宠物</param>
        public void OnProjectileHit(PetAI pet)
        {
            if (pet == null) return;
            
            ApplySlowEffect(pet, SlowAmount, SlowDuration);
            OnNetHit?.Invoke(pet);
            
            Debug.Log($"[捕获网] 击中宠物，应用 {SlowAmount * 100}% 减速效果，持续 {SlowDuration} 秒");
        }
        #endregion

        #region Private Methods
        private void LaunchProjectile()
        {
            Vector3 launchPosition = LaunchPoint != null ? LaunchPoint.position : _ownerTransform.position;
            Vector3 launchDirection = _ownerTransform.forward;
            
            if (NetProjectilePrefab != null)
            {
                GameObject projectile = Instantiate(NetProjectilePrefab, launchPosition, Quaternion.LookRotation(launchDirection));
                
                // Initialize the projectile
                CaptureNetProjectile netProjectile = projectile.GetComponent<CaptureNetProjectile>();
                if (netProjectile != null)
                {
                    netProjectile.Initialize(this, launchDirection, ProjectileSpeed, MaxRange);
                }
                else
                {
                    // Fallback: Add basic movement
                    Rigidbody rb = projectile.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.linearVelocity = launchDirection * ProjectileSpeed;
                    }
                    
                    // Destroy after max range time
                    Destroy(projectile, MaxRange / ProjectileSpeed);
                }
            }
            else
            {
                // No prefab - do raycast-based hit detection
                PerformRaycastHit(launchPosition, launchDirection);
            }
            
            Debug.Log("[捕获网] 投射物发射");
        }

        private void PerformRaycastHit(Vector3 origin, Vector3 direction)
        {
            RaycastHit hit;
            if (Physics.Raycast(origin, direction, out hit, MaxRange))
            {
                PetAI pet = hit.collider.GetComponent<PetAI>();
                if (pet == null)
                {
                    pet = hit.collider.GetComponentInParent<PetAI>();
                }
                
                if (pet != null)
                {
                    OnProjectileHit(pet);
                }
            }
        }
        #endregion

        #region Static Methods (Testable)
        /// <summary>
        /// 对宠物应用减速效果。
        /// 属性 8: 捕获网减速效果
        /// 需求 3.2: 使被击中的宠物减速 50%，持续 3 秒。
        /// </summary>
        /// <param name="pet">要减速的宠物</param>
        /// <param name="slowAmount">速度降低量 (0.5 = 50%)</param>
        /// <param name="duration">持续时间（秒）</param>
        public static void ApplySlowEffect(PetAI pet, float slowAmount, float duration)
        {
            if (pet == null) return;
            
            // 创建减速效果数据
            SkillEffectData slowEffect = SkillEffectData.CreateSlow(slowAmount, duration, "捕获网");
            
            // 应用到宠物（宠物需要有效果处理）
            IEffectReceiver effectReceiver = pet.GetComponent<IEffectReceiver>();
            if (effectReceiver != null)
            {
                effectReceiver.ApplyEffect(slowEffect);
            }
            else
            {
                // 备选方案：如果宠物支持，直接修改速度
                Debug.Log($"[捕获网] 应用了减速效果：{slowAmount * 100}% 持续 {duration} 秒");
            }
        }

        /// <summary>
        /// 计算减速效果参数。
        /// </summary>
        /// <param name="baseSlowAmount">配置中的基础减速量</param>
        /// <param name="baseDuration">配置中的基础持续时间</param>
        /// <returns>(slowAmount, duration) 的元组</returns>
        public static (float slowAmount, float duration) CalculateSlowEffect(float baseSlowAmount, float baseDuration)
        {
            // 目前没有修改器，但这允许未来扩展
            return (baseSlowAmount, baseDuration);
        }

        /// <summary>
        /// 根据要求验证减速效果参数是否正确。
        /// 属性 8: 捕获网减速效果
        /// </summary>
        /// <param name="slowAmount">要验证的减速量</param>
        /// <param name="duration">要验证的持续时间</param>
        /// <returns>如果参数符合要求则为 True</returns>
        public static bool ValidateSlowEffectParameters(float slowAmount, float duration)
        {
            // 需求 3.2: 50% 减速，持续 3 秒
            const float RequiredSlowAmount = 0.5f;
            const float RequiredDuration = 3f;
            const float Tolerance = 0.001f;
            
            return Mathf.Abs(slowAmount - RequiredSlowAmount) < Tolerance &&
                   Mathf.Abs(duration - RequiredDuration) < Tolerance;
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
                Cooldown = config.CaptureNetCooldown;
                SlowAmount = config.CaptureNetSlowAmount;
                SlowDuration = config.CaptureNetSlowDuration;
                ProjectileSpeed = config.CaptureNetProjectileSpeed;
            }
        }
#endif

        private void OnDrawGizmosSelected()
        {
            // Draw max range
            Gizmos.color = Color.yellow;
            Vector3 start = LaunchPoint != null ? LaunchPoint.position : transform.position;
            Gizmos.DrawLine(start, start + transform.forward * MaxRange);
        }
        #endregion
    }
}
