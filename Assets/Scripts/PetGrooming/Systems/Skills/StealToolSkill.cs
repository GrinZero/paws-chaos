using System;
using UnityEngine;
using PetGrooming.Core;
using PetGrooming.AI;

namespace PetGrooming.Systems.Skills
{
    /// <summary>
    /// 狗狗宠物的偷窃工具技能。
    /// 从最近的美容站移除一个工具，使美容过程增加 1 步。
    /// 需求：5.5
    /// </summary>
    public class StealToolSkill : SkillBase
    {
        #region Serialized Fields
        [Header("偷窃工具设置")]
        [Tooltip("检测最近美容站的范围")]
        public float DetectionRange = 5f;
        
        [Tooltip("偷窃工具时增加的额外美容步骤")]
        public int ExtraStepsAdded = 1;
        
        [Tooltip("偷窃工具时的视觉效果")]
        public ParticleSystem StealEffect;
        
        [Header("配置")]
        [Tooltip("阶段 2 游戏配置")]
        public Phase2GameConfig GameConfig;
        #endregion

        #region Private Fields
        private PetAI _ownerPet;
        #endregion

        #region Events
        /// <summary>
        /// 当工具成功被盗时触发。
        /// </summary>
        public event Action<GroomingStation, int> OnToolStolen;
        
        /// <summary>
        /// 当没有美容站在范围内时触发。
        /// </summary>
        public event Action OnNoStationInRange;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            
            SkillName = "偷窃工具";
            
            // 如果可用，应用配置值
            if (GameConfig != null)
            {
                Cooldown = GameConfig.StealToolCooldown;
                DetectionRange = GameConfig.StealToolRange;
                ExtraStepsAdded = GameConfig.StealToolExtraSteps;
            }
            else
            {
                // 默认冷却时间：12 秒 (需求 5.5)
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
        /// 检查技能是否可以激活。
        /// 需要美容站在范围内。
        /// </summary>
        public override bool CanActivate()
        {
            if (!base.CanActivate()) return false;
            
            // 检查是否有美容站在范围内
            GroomingStation nearestStation = FindNearestGroomingStation();
            return nearestStation != null;
        }

        /// <summary>
        /// 激活偷窃工具技能。
        /// 需求 5.5: 从最近的美容站移除一个工具，使美容过程增加 1 步。
        /// </summary>
        public override void Activate()
        {
            base.Activate();
            PerformSteal();
        }
        #endregion

        #region Private Methods
        private void PerformSteal()
        {
            GroomingStation nearestStation = FindNearestGroomingStation();
            
            if (nearestStation == null)
            {
                OnNoStationInRange?.Invoke();
                Debug.Log("[StealToolSkill] No Grooming Station in range!");
                return;
            }
            
            // 播放视觉效果
            if (StealEffect != null)
            {
                StealEffect.transform.position = nearestStation.transform.position;
                StealEffect.Play();
            }
            
            // 为美容站添加额外的美容步骤
            AddExtraGroomingSteps(nearestStation, ExtraStepsAdded);
            
            OnToolStolen?.Invoke(nearestStation, ExtraStepsAdded);
            
            Debug.Log($"[StealToolSkill] Stole tool from {nearestStation.name}! Added {ExtraStepsAdded} extra step(s).");
        }

        private GroomingStation FindNearestGroomingStation()
        {
            Vector3 searchOrigin = _ownerPet != null ? _ownerPet.transform.position : transform.position;
            
            GroomingStation[] stations = FindObjectsOfType<GroomingStation>();
            GroomingStation nearest = null;
            float nearestDistance = float.MaxValue;
            
            foreach (GroomingStation station in stations)
            {
                float distance = Vector3.Distance(searchOrigin, station.transform.position);
                
                if (distance <= DetectionRange && distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = station;
                }
            }
            
            return nearest;
        }

        private void AddExtraGroomingSteps(GroomingStation station, int extraSteps)
        {
            if (station == null) return;
            
            // Get the grooming system from the station
            GroomingSystem groomingSystem = station.GroomingSystem;
            if (groomingSystem != null)
            {
                // 美容系统跟踪步骤 - 我们需要添加额外的所需步骤
            // 这由 GroomingStation 的扩展功能处理
                AddExtraStepsToStation(station, extraSteps);
            }
        }

        /// <summary>
        /// 为美容站添加额外的美容步骤。
        /// 这会修改美容站的美容要求。
        /// </summary>
        private void AddExtraStepsToStation(GroomingStation station, int extraSteps)
        {
            // 美容站需要跟踪所需的额外步骤
            // 这通常在 GroomingStation 类中实现
            // 目前，我们将使用基于组件的方法
            
            StationExtraSteps extraStepsComponent = station.GetComponent<StationExtraSteps>();
            if (extraStepsComponent == null)
            {
                extraStepsComponent = station.gameObject.AddComponent<StationExtraSteps>();
            }
            
            extraStepsComponent.AddExtraSteps(extraSteps);
        }
        #endregion

        #region Static Methods (Testable)
        /// <summary>
        /// 计算偷窃后的总美容步骤。
        /// 属性 14: 偷窃工具增加美容步骤
        /// 需求 5.5: 美容过程增加 1 步
        /// </summary>
        /// <param name="baseSteps">基础美容步骤数 (3)</param>
        /// <param name="extraSteps">偷窃增加的额外步骤</param>
        /// <returns>所需的总美容步骤</returns>
        public static int CalculateTotalGroomingSteps(int baseSteps, int extraSteps)
        {
            return baseSteps + extraSteps;
        }

        /// <summary>
        /// 验证偷窃是否增加了正确数量的步骤。
        /// 属性 14: 偷窃工具增加美容步骤
        /// 需求 5.5: 美容过程增加 1 步
        /// </summary>
        /// <param name="stepsAdded">增加的步骤数</param>
        /// <returns>如果增加了正确数量的步骤则为 True</returns>
        public static bool ValidateStepsAdded(int stepsAdded)
        {
            const int RequiredStepsAdded = 1;
            return stepsAdded == RequiredStepsAdded;
        }

        /// <summary>
        /// 验证偷窃工具冷却时间是否符合要求。
        /// 需求 5.5: 12 秒冷却时间
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
        /// 检查美容站是否在偷窃范围内。
        /// </summary>
        /// <param name="petPosition">宠物的位置</param>
        /// <param name="stationPosition">美容站的位置</param>
        /// <param name="range">检测范围</param>
        /// <returns>如果在范围内则为 True</returns>
        public static bool IsStationInRange(Vector3 petPosition, Vector3 stationPosition, float range)
        {
            float distance = Vector3.Distance(petPosition, stationPosition);
            return distance <= range;
        }

        /// <summary>
        /// 在范围内从列表中找到最近的美容站。
        /// </summary>
        /// <param name="petPosition">宠物的位置</param>
        /// <param name="stationPositions">美容站位置数组</param>
        /// <param name="range">检测范围</param>
        /// <returns>最近美容站的索引，如果不在范围内则为 -1</returns>
        public static int FindNearestStationIndex(Vector3 petPosition, Vector3[] stationPositions, float range)
        {
            int nearestIndex = -1;
            float nearestDistance = float.MaxValue;
            
            for (int i = 0; i < stationPositions.Length; i++)
            {
                float distance = Vector3.Distance(petPosition, stationPositions[i]);
                
                if (distance <= range && distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestIndex = i;
                }
            }
            
            return nearestIndex;
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
                Cooldown = config.StealToolCooldown;
                DetectionRange = config.StealToolRange;
                ExtraStepsAdded = config.StealToolExtraSteps;
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
            // 绘制检测范围
            Gizmos.color = new Color(0.5f, 0f, 1f, 0.3f);
            Gizmos.DrawSphere(transform.position, DetectionRange);
            
            Gizmos.color = new Color(0.5f, 0f, 1f, 1f);
            Gizmos.DrawWireSphere(transform.position, DetectionRange);
        }
        #endregion
    }

    /// <summary>
    /// 辅助组件，用于跟踪添加到美容站的额外美容步骤。
    /// </summary>
    public class StationExtraSteps : MonoBehaviour
    {
        /// <summary>
        /// 添加到此美容站的额外步骤数。
        /// </summary>
        public int ExtraSteps { get; private set; }

        /// <summary>
        /// 添加额外步骤时触发的事件。
        /// </summary>
        public event Action<int> OnExtraStepsAdded;

        /// <summary>
        /// 为此美容站添加额外的美容步骤。
        /// </summary>
        /// <param name="steps">要添加的步骤数</param>
        public void AddExtraSteps(int steps)
        {
            ExtraSteps += steps;
            OnExtraStepsAdded?.Invoke(steps);
            Debug.Log($"[StationExtraSteps] Added {steps} extra steps. Total extra: {ExtraSteps}");
        }

        /// <summary>
        /// 重置额外步骤计数。
        /// </summary>
        public void Reset()
        {
            ExtraSteps = 0;
        }

        /// <summary>
        /// 获取所需的总美容步骤（基础 + 额外）。
        /// </summary>
        /// <param name="baseSteps">基础步骤数（默认为 3）</param>
        /// <returns>所需的总步骤</returns>
        public int GetTotalStepsRequired(int baseSteps = 3)
        {
            return baseSteps + ExtraSteps;
        }
    }
}
