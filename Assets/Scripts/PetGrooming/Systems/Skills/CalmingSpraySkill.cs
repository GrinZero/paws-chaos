using System;
using UnityEngine;
using PetGrooming.Core;
using PetGrooming.AI;

namespace PetGrooming.Systems.Skills
{
    /// <summary>
    /// 美容师的镇静喷雾技能。
    /// 创建一个区域效果，使宠物眩晕 1 秒。
    /// 需求：3.7, 3.8
    /// </summary>
    public class CalmingSpraySkill : SkillBase
    {
        #region Serialized Fields
        [Header("镇静喷雾设置")]
        [Tooltip("喷雾效果的半径")]
        public float EffectRadius = 3f;
        
        [Tooltip("眩晕效果的持续时间（秒）")]
        public float StunDuration = 1f;
        
        [Header("视觉效果")]
        [Tooltip("喷雾效果的粒子系统")]
        public ParticleSystem SprayEffect;
        
        [Tooltip("喷雾视觉效果的持续时间")]
        public float SprayVisualDuration = 0.5f;
        
        [Header("配置")]
        [Tooltip("第二阶段游戏配置")]
        public Phase2GameConfig GameConfig;
        
        [Header("层级设置")]
        [Tooltip("宠物检测的层级掩码")]
        public LayerMask PetLayerMask = -1;
        #endregion

        #region Private Fields
        private Transform _ownerTransform;
        #endregion

        #region Events
        /// <summary>
        /// 当喷雾激活时触发。
        /// </summary>
        public event Action OnSprayActivated;
        
        /// <summary>
        /// 当宠物被喷雾眩晕时触发。
        /// </summary>
        public event Action<PetAI> OnPetStunned;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            
            SkillName = "镇静喷雾";
            
            // Apply config values if available
            if (GameConfig != null)
            {
                Cooldown = GameConfig.CalmingSprayCooldown;
                EffectRadius = GameConfig.CalmingSprayRadius;
                StunDuration = GameConfig.CalmingSprayStunDuration;
            }
            else
            {
                // Default cooldown: 13 seconds (Requirement 3.8)
                Cooldown = 13f;
            }
            
            _ownerTransform = transform;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 设置效果原点的所有者变换组件。
        /// </summary>
        public void SetOwner(Transform owner)
        {
            _ownerTransform = owner;
        }

        /// <summary>
        /// 激活镇静喷雾技能。
        /// 需求 3.7：创建一个区域效果，使宠物眩晕 1 秒。
        /// </summary>
        public override void Activate()
        {
            base.Activate();
            ReleaseSpray();
        }

        /// <summary>
        /// 获取效果半径内的所有宠物。
        /// </summary>
        /// <returns>范围内的宠物数组</returns>
        public PetAI[] GetPetsInRange()
        {
            Vector3 center = _ownerTransform != null ? _ownerTransform.position : transform.position;
            return FindPetsInRadius(center, EffectRadius, PetLayerMask);
        }
        #endregion

        #region Private Methods
        private void ReleaseSpray()
        {
            Vector3 center = _ownerTransform != null ? _ownerTransform.position : transform.position;
            
            // Play visual effect
            if (SprayEffect != null)
            {
                SprayEffect.transform.position = center;
                SprayEffect.Play();
            }
            
            OnSprayActivated?.Invoke();
            
            // Find and stun all pets in range
            PetAI[] petsInRange = FindPetsInRadius(center, EffectRadius, PetLayerMask);
            ApplyStunToAllPets(petsInRange, StunDuration);
            
            Debug.Log($"[镇静喷雾] 喷雾释放，使 {petsInRange.Length} 只宠物眩晕 {StunDuration} 秒");
        }

        private void ApplyStunToAllPets(PetAI[] pets, float duration)
        {
            foreach (PetAI pet in pets)
            {
                if (pet != null)
                {
                    ApplyStunEffect(pet, duration);
                    OnPetStunned?.Invoke(pet);
                }
            }
        }
        #endregion

        #region Static Methods (Testable)
        /// <summary>
        /// 查找半径范围内的所有宠物。
        /// </summary>
        /// <param name="center">搜索区域的中心</param>
        /// <param name="radius">搜索半径</param>
        /// <param name="layerMask">用于过滤的层级掩码</param>
        /// <returns>找到的宠物数组</returns>
        public static PetAI[] FindPetsInRadius(Vector3 center, float radius, LayerMask layerMask)
        {
            Collider[] colliders = Physics.OverlapSphere(center, radius, layerMask);
            
            // 先统计宠物数量
            int petCount = 0;
            foreach (Collider col in colliders)
            {
                PetAI pet = col.GetComponent<PetAI>();
                if (pet == null)
                {
                    pet = col.GetComponentInParent<PetAI>();
                }
                if (pet != null)
                {
                    petCount++;
                }
            }
            
            // 创建数组并填充
            PetAI[] pets = new PetAI[petCount];
            int index = 0;
            foreach (Collider col in colliders)
            {
                PetAI pet = col.GetComponent<PetAI>();
                if (pet == null)
                {
                    pet = col.GetComponentInParent<PetAI>();
                }
                if (pet != null && index < pets.Length)
                {
                    // 避免重复
                    bool isDuplicate = false;
                    for (int i = 0; i < index; i++)
                    {
                        if (pets[i] == pet)
                        {
                            isDuplicate = true;
                            break;
                        }
                    }
                    
                    if (!isDuplicate)
                    {
                        pets[index++] = pet;
                    }
                }
            }
            
            // 如果有重复，调整数组大小
            if (index < pets.Length)
            {
                PetAI[] resized = new PetAI[index];
                Array.Copy(pets, resized, index);
                return resized;
            }
            
            return pets;
        }

        /// <summary>
        /// 对宠物应用眩晕效果。
        /// 属性 10: 镇静喷雾眩晕效果
        /// 需求 3.7: 使宠物眩晕 1 秒。
        /// </summary>
        /// <param name="pet">要眩晕的宠物</param>
        /// <param name="duration">眩晕持续时间（秒）</param>
        public static void ApplyStunEffect(PetAI pet, float duration)
        {
            if (pet == null) return;
            
            // 创建眩晕效果数据
            SkillEffectData stunEffect = SkillEffectData.CreateStun(duration, "镇静喷雾");
            
            // 应用到宠物
            IEffectReceiver effectReceiver = pet.GetComponent<IEffectReceiver>();
            if (effectReceiver != null)
            {
                effectReceiver.ApplyEffect(stunEffect);
            }
            else
            {
                Debug.Log($"[镇静喷雾] 应用了眩晕效果，持续 {duration} 秒");
            }
        }

        /// <summary>
        /// 检查位置是否在效果半径内。
        /// </summary>
        /// <param name="center">效果中心</param>
        /// <param name="targetPosition">要检查的位置</param>
        /// <param name="radius">效果半径</param>
        /// <returns>如果在半径内则为 True</returns>
        public static bool IsInEffectRadius(Vector3 center, Vector3 targetPosition, float radius)
        {
            float distance = Vector3.Distance(center, targetPosition);
            return distance <= radius;
        }

        /// <summary>
        /// 验证眩晕效果参数是否符合要求。
        /// 属性 10: 镇静喷雾眩晕效果
        /// </summary>
        /// <param name="stunDuration">要验证的眩晕持续时间</param>
        /// <returns>如果参数符合要求则为 True</returns>
        public static bool ValidateStunEffectParameters(float stunDuration)
        {
            // 需求 3.7: 使宠物眩晕 1 秒
            const float RequiredDuration = 1f;
            const float Tolerance = 0.001f;
            
            return Mathf.Abs(stunDuration - RequiredDuration) < Tolerance;
        }

        /// <summary>
        /// 计算会受到影响的宠物数量。
        /// </summary>
        /// <param name="petPositions">宠物位置数组</param>
        /// <param name="center">效果中心</param>
        /// <param name="radius">效果半径</param>
        /// <returns>范围内的宠物数量</returns>
        public static int CalculateAffectedPetCount(Vector3[] petPositions, Vector3 center, float radius)
        {
            int count = 0;
            foreach (Vector3 pos in petPositions)
            {
                if (IsInEffectRadius(center, pos, radius))
                {
                    count++;
                }
            }
            return count;
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
                Cooldown = config.CalmingSprayCooldown;
                EffectRadius = config.CalmingSprayRadius;
                StunDuration = config.CalmingSprayStunDuration;
            }
        }
        
        /// <summary>
        /// 设置用于测试的效果参数。
        /// </summary>
        public void SetEffectParametersForTesting(float radius, float duration)
        {
            EffectRadius = radius;
            StunDuration = duration;
        }
#endif

        private void OnDrawGizmosSelected()
        {
            // Draw effect radius
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Vector3 center = _ownerTransform != null ? _ownerTransform.position : transform.position;
            Gizmos.DrawSphere(center, EffectRadius);
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(center, EffectRadius);
        }
        #endregion
    }
}
