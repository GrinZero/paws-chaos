using System;
using UnityEngine;
using PetGrooming.Core;
using PetGrooming.AI;

namespace PetGrooming.Systems.Skills
{
    /// <summary>
    /// 狗狗宠物的威慑吠叫技能。
    /// 产生一个区域效果，使美容师速度降低 20%，持续 3 秒。
    /// 需求：5.4
    /// </summary>
    public class IntimidatingBarkSkill : SkillBase
    {
        #region Serialized Fields
        [Header("威慑吠叫设置")]
        [Tooltip("吠叫效果的半径")]
        public float EffectRadius = 4f;
        
        [Tooltip("移动速度降低量 (0.2 = 降低 20%)")]
        [Range(0f, 1f)]
        public float SlowAmount = 0.2f;
        
        [Tooltip("减速效果持续时间（秒）")]
        public float SlowDuration = 3f;
        
        [Tooltip("吠叫的视觉效果")]
        public ParticleSystem BarkEffect;
        
        [Tooltip("吠叫声音的音频源")]
        public AudioSource BarkSound;
        
        [Header("配置")]
        [Tooltip("阶段 2 游戏配置")]
        public Phase2GameConfig GameConfig;
        #endregion

        #region Private Fields
        private PetAI _ownerPet;
        #endregion

        #region Events
        /// <summary>
        /// 当吠叫影响美容师时触发。
        /// </summary>
        public event Action<GroomerController> OnGroomerAffected;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            
            SkillName = "威慑吠叫";
            
            // 如果可用，应用配置值
            if (GameConfig != null)
            {
                Cooldown = GameConfig.IntimidatingBarkCooldown;
                EffectRadius = GameConfig.IntimidatingBarkRadius;
                SlowAmount = GameConfig.IntimidatingBarkSlowAmount;
                SlowDuration = GameConfig.IntimidatingBarkDuration;
            }
            else
            {
                // 默认冷却时间：12 秒 (需求 5.4)
                Cooldown = 12f;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 设置此技能的所有者宠物。
        /// </summary>
        public void SetOwner(PetAI owner)
        {
            _ownerPet = owner;
        }

        /// <summary>
        /// 激活威慑吠叫技能。
        /// 需求 5.4: 产生一个区域效果，使美容师速度降低 20%，持续 3 秒。
        /// </summary>
        public override void Activate()
        {
            base.Activate();
            PerformBark();
        }
        #endregion

        #region Private Methods
        private void PerformBark()
        {
            Vector3 barkOrigin = _ownerPet != null ? _ownerPet.transform.position : transform.position;
            
            // 播放视觉效果
            if (BarkEffect != null)
            {
                BarkEffect.Play();
            }
            
            // 播放音效
            if (BarkSound != null)
            {
                BarkSound.Play();
            }
            
            // 查找范围内所有目标
            Collider[] hits = Physics.OverlapSphere(barkOrigin, EffectRadius);
            
            foreach (Collider hit in hits)
            {
                // 检查美容师
                GroomerController groomer = hit.GetComponent<GroomerController>();
                if (groomer == null)
                {
                    groomer = hit.GetComponentInParent<GroomerController>();
                }
                
                if (groomer != null)
                {
                    ApplySlowToGroomer(groomer);
                }
            }
            
            Debug.Log($"[威慑吠叫] 吠叫释放！半径: {EffectRadius}, 减速: {SlowAmount * 100}% 持续 {SlowDuration} 秒");
        }

        private void ApplySlowToGroomer(GroomerController groomer)
        {
            if (groomer == null) return;
            
            // 对美容师施加减速效果
            IEffectReceiver effectReceiver = groomer.GetComponent<IEffectReceiver>();
            if (effectReceiver != null)
            {
                SkillEffectData slowEffect = SkillEffectData.CreateSlow(SlowAmount, SlowDuration, "威慑吠叫");
                effectReceiver.ApplyEffect(slowEffect);
            }
            
            // 为宠物技能击中美容师添加恶作剧值
            // 属性 18: 宠物技能击中恶作剧值
            // 需求 6.6: 宠物技能击中增加 30 点
            if (MischiefSystem.Instance != null)
            {
                MischiefSystem.Instance.AddPetSkillHitMischief();
            }
            
            OnGroomerAffected?.Invoke(groomer);
            
            Debug.Log($"[威慑吠叫] 对美容师应用了 {SlowAmount * 100}% 的减速效果，持续 {SlowDuration} 秒");
        }
        #endregion

        #region Static Methods (Testable)
        /// <summary>
        /// 计算减速效果参数。
        /// 属性 13: 威慑吠叫减速效果
        /// 需求 5.4: 减速 20%，持续 3 秒
        /// </summary>
        /// <param name="baseSlowAmount">配置中的基础减速量</param>
        /// <param name="baseDuration">配置中的基础持续时间</param>
        /// <returns>减速量和持续时间的元组</returns>
        public static (float slowAmount, float duration) CalculateSlowEffect(float baseSlowAmount, float baseDuration)
        {
            return (baseSlowAmount, baseDuration);
        }

        /// <summary>
        /// 根据要求验证减速效果参数是否正确。
        /// 属性 13: 威慑吠叫减速效果
        /// 需求 5.4: 减速 20%，持续 3 秒
        /// </summary>
        /// <param name="slowAmount">要验证的减速量</param>
        /// <param name="duration">要验证的持续时间</param>
        /// <returns>如果参数符合要求则为 True</returns>
        public static bool ValidateSlowEffectParameters(float slowAmount, float duration)
        {
            const float RequiredSlowAmount = 0.2f;
            const float RequiredDuration = 3f;
            const float Tolerance = 0.001f;
            
            return Mathf.Abs(slowAmount - RequiredSlowAmount) < Tolerance &&
                   Mathf.Abs(duration - RequiredDuration) < Tolerance;
        }

        /// <summary>
        /// 验证威慑吠叫冷却时间是否符合要求。
        /// 需求 5.4: 12 秒冷却时间
        /// </summary>
        /// <param name="cooldown">要验证的冷却时间</param>
        /// <returns>如果冷却时间符合要求则为 True</returns>
        public static bool ValidateCooldown(float cooldown)
        {
            const float RequiredCooldown = 12f;
            const float Tolerance = 0.001f;
            return Mathf.Abs(cooldown - RequiredCooldown) < Tolerance;
        }

        /// <summary>
        /// 检查位置是否在吠叫效果半径内。
        /// </summary>
        /// <param name="barkOrigin">吠叫的起源</param>
        /// <param name="targetPosition">要检查的位置</param>
        /// <param name="effectRadius">效果半径</param>
        /// <returns>如果在范围内则为 True</returns>
        public static bool IsWithinEffectRadius(Vector3 barkOrigin, Vector3 targetPosition, float effectRadius)
        {
            float distance = Vector3.Distance(barkOrigin, targetPosition);
            return distance <= effectRadius;
        }

        /// <summary>
        /// 为威慑吠叫创建减速效果数据。
        /// </summary>
        /// <param name="slowAmount">减速量 (0.2 = 20%)</param>
        /// <param name="duration">持续时间（秒）</param>
        /// <returns>减速效果的 SkillEffectData</returns>
        public static SkillEffectData CreateBarkSlowEffect(float slowAmount, float duration)
        {
            return SkillEffectData.CreateSlow(slowAmount, duration, "Intimidating Bark");
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
                Cooldown = config.IntimidatingBarkCooldown;
                EffectRadius = config.IntimidatingBarkRadius;
                SlowAmount = config.IntimidatingBarkSlowAmount;
                SlowDuration = config.IntimidatingBarkDuration;
            }
        }

        /// <summary>
        /// 设置用于测试的所有者。
        /// </summary>
        public void SetOwnerForTesting(PetAI owner)
        {
            _ownerPet = owner;
        }
#endif

        private void OnDrawGizmosSelected()
        {
            // 绘制效果半径
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, EffectRadius);
            
            Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
            Gizmos.DrawWireSphere(transform.position, EffectRadius);
        }
        #endregion
    }
}
