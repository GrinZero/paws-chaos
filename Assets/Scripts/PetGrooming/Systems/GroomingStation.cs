using System;
using UnityEngine;
using PetGrooming.AI;

namespace PetGrooming.Systems
{
    /// <summary>
    /// 宠物美容台，用于对宠物进行美容的站点。
    /// 需求：4.1, 7.1
    /// </summary>
    public class GroomingStation : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Station Settings")]
        [Tooltip("The position where the pet will be placed during grooming")]
        [SerializeField] private Transform _groomingPosition;
        
        [Tooltip("The position where the groomer stands during grooming")]
        [SerializeField] private Transform _groomerPosition;
        
        [Tooltip("Distance within which grooming can be initiated")]
        [SerializeField] private float _interactionRange = 2f;
        
        [Header("References")]
        [SerializeField] private GroomingSystem _groomingSystem;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// 该美容台当前是否被占用。
        /// </summary>
        public bool IsOccupied { get; private set; }
        
        /// <summary>
        /// 宠物在美容时被放置的位置。
        /// </summary>
        public Transform GroomingPosition => _groomingPosition;
        
        /// <summary>
        /// Groomer 在美容时站立的位置。
        /// </summary>
        public Transform GroomerPosition => _groomerPosition;
        
        /// <summary>
        /// 可以开始美容的交互距离。
        /// </summary>
        public float InteractionRange => _interactionRange;
        
        /// <summary>
        /// GroomingSystem 引用。
        /// </summary>
        public GroomingSystem GroomingSystem => _groomingSystem;
        
        /// <summary>
        /// 当前正在被美容的宠物（如有）。
        /// </summary>
        public PetAI CurrentPet { get; private set; }
        
        #endregion

        #region Events
        
        /// <summary>
        /// 在该美容台开始美容时触发。
        /// </summary>
        public event Action OnGroomingStarted;
        
        /// <summary>
        /// 在该美容台结束美容时触发。
        /// </summary>
        public event Action OnGroomingEnded;
        
        /// <summary>
        /// 当 Groomer 进入交互范围时触发。
        /// </summary>
        public event Action<Transform> OnGroomerInRange;
        
        /// <summary>
        /// 当 Groomer 离开交互范围时触发。
        /// </summary>
        public event Action OnGroomerOutOfRange;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            ValidateReferences();
        }
        
        private void Start()
        {
            // 订阅 GroomingSystem 的事件
            if (_groomingSystem != null)
            {
                _groomingSystem.OnGroomingComplete += HandleGroomingComplete;
                _groomingSystem.OnGroomingCancelled += HandleGroomingCancelled;
            }
        }
        
        private void OnDestroy()
        {
            // 取消事件订阅
            if (_groomingSystem != null)
            {
                _groomingSystem.OnGroomingComplete -= HandleGroomingComplete;
                _groomingSystem.OnGroomingCancelled -= HandleGroomingCancelled;
            }
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// 检查给定的 Groomer 是否可以在此处开始美容。
        /// 需求 4.1：当 Groomer 把被捕获的宠物带到美容台时开始美容。
        /// </summary>
        /// <param name="groomerPosition">Groomer 的位置。</param>
        /// <param name="hasCapuredPet">Groomer 是否携带被捕获的宠物。</param>
        /// <returns>如果可以开始美容则返回 true。</returns>
        public bool CanStartGrooming(Vector3 groomerPosition, bool hasCapuredPet)
        {
            if (IsOccupied)
            {
                return false;
            }
            
            if (!hasCapuredPet)
            {
                return false;
            }
            
            if (!IsGroomerInRange(groomerPosition))
            {
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 检查 Groomer 是否在交互范围内。
        /// </summary>
        /// <param name="groomerPosition">Groomer 的位置。</param>
        /// <returns>在范围内则返回 true。</returns>
        public bool IsGroomerInRange(Vector3 groomerPosition)
        {
            return IsWithinRange(groomerPosition, transform.position, _interactionRange);
        }
        
        /// <summary>
        /// 在该美容台开始美容。
        /// 需求 4.1：当条件满足时自动开始美容。
        /// </summary>
        /// <param name="pet">要被美容的宠物。</param>
        /// <returns>如果成功开始美容则返回 true。</returns>
        public bool StartGrooming(PetAI pet)
        {
            if (IsOccupied)
            {
                Debug.LogWarning("[GroomingStation] Station is already occupied!");
                return false;
            }
            
            if (pet == null)
            {
                Debug.LogError("[GroomingStation] Cannot start grooming with null pet!");
                return false;
            }
            
            IsOccupied = true;
            CurrentPet = pet;
            
            // 将宠物放到美容位置
            if (_groomingPosition != null)
            {
                pet.transform.position = _groomingPosition.position;
                pet.transform.rotation = _groomingPosition.rotation;
            }
            
            // 通知宠物美容已开始
            pet.OnGroomingStarted();
            
            // 启动美容流程
            if (_groomingSystem != null)
            {
                _groomingSystem.StartGrooming();
            }
            
            OnGroomingStarted?.Invoke();
            
            Debug.Log($"[GroomingStation] Grooming started for pet: {pet.name}");
            
            return true;
        }
        
        /// <summary>
        /// 在该美容台结束美容。
        /// </summary>
        public void EndGrooming()
        {
            if (!IsOccupied)
            {
                return;
            }
            
            IsOccupied = false;
            CurrentPet = null;
            
            OnGroomingEnded?.Invoke();
            
            Debug.Log("[GroomingStation] Grooming ended.");
        }
        
        /// <summary>
        /// 获取世界坐标中的美容位置。
        /// </summary>
        /// <returns>宠物美容时所在的世界坐标。</returns>
        public Vector3 GetGroomingWorldPosition()
        {
            return _groomingPosition != null ? _groomingPosition.position : transform.position;
        }
        
        /// <summary>
        /// 获取世界坐标中的 Groomer 站位。
        /// </summary>
        /// <returns>美容时 Groomer 所在的世界坐标。</returns>
        public Vector3 GetGroomerWorldPosition()
        {
            return _groomerPosition != null ? _groomerPosition.position : transform.position + Vector3.forward;
        }
        
        #endregion

        #region Private Methods
        
        private void ValidateReferences()
        {
            if (_groomingPosition == null)
            {
                Debug.LogWarning("[GroomingStation] 未指定 GroomingPosition，将使用站点位置。");
                // 创建一个子物体作为默认的美容位置
                var groomingPosObj = new GameObject("GroomingPosition");
                groomingPosObj.transform.SetParent(transform);
                groomingPosObj.transform.localPosition = Vector3.zero;
                _groomingPosition = groomingPosObj.transform;
            }
            
            if (_groomerPosition == null)
            {
                Debug.LogWarning("[GroomingStation] 未指定 GroomerPosition，将使用站点前方偏移位置。");
                // 创建一个子物体作为默认的 Groomer 站位
                var groomerPosObj = new GameObject("GroomerPosition");
                groomerPosObj.transform.SetParent(transform);
                groomerPosObj.transform.localPosition = Vector3.forward * 1.5f;
                _groomerPosition = groomerPosObj.transform;
            }
            
            if (_groomingSystem == null)
            {
                _groomingSystem = GetComponent<GroomingSystem>();
                if (_groomingSystem == null)
                {
                    _groomingSystem = FindObjectOfType<GroomingSystem>();
                }
                
                if (_groomingSystem == null)
                {
                    Debug.LogWarning("[GroomingStation] 场景中未找到 GroomingSystem，自动创建一个。");
                    _groomingSystem = gameObject.AddComponent<GroomingSystem>();
                }
            }
        }
        
        private void HandleGroomingComplete()
        {
            Debug.Log("[GroomingStation] Grooming complete - releasing station.");
            EndGrooming();
        }
        
        private void HandleGroomingCancelled()
        {
            Debug.Log("[GroomingStation] Grooming cancelled - releasing station.");
            EndGrooming();
        }
        
        #endregion

        #region Static Calculation Methods (Testable)
        
        /// <summary>
        /// 检查某个位置是否在目标位置的一定范围内。
        /// </summary>
        /// <param name="position">要检查的位置。</param>
        /// <param name="target">目标位置。</param>
        /// <param name="range">最大范围。</param>
        /// <returns>在范围内则返回 true。</returns>
        public static bool IsWithinRange(Vector3 position, Vector3 target, float range)
        {
            float distance = Vector3.Distance(position, target);
            return distance <= range;
        }
        
        /// <summary>
        /// 计算两个位置之间的距离。
        /// </summary>
        /// <param name="pos1">第一个位置。</param>
        /// <param name="pos2">第二个位置。</param>
        /// <returns>两个位置之间的距离。</returns>
        public static float CalculateDistance(Vector3 pos1, Vector3 pos2)
        {
            return Vector3.Distance(pos1, pos2);
        }
        
        #endregion

        #region Editor Support
        
#if UNITY_EDITOR
        /// <summary>
        /// 设置 GroomingSystem（测试用）。
        /// </summary>
        public void SetGroomingSystemForTesting(GroomingSystem system)
        {
            _groomingSystem = system;
        }
        
        /// <summary>
        /// 设置交互距离（测试用）。
        /// </summary>
        public void SetInteractionRangeForTesting(float range)
        {
            _interactionRange = range;
        }
        
        /// <summary>
        /// 设置占用状态（测试用）。
        /// </summary>
        public void SetOccupiedForTesting(bool occupied)
        {
            IsOccupied = occupied;
        }
#endif
        
        private void OnDrawGizmosSelected()
        {
            // 绘制交互范围
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _interactionRange);
            
            // 绘制美容位置
            if (_groomingPosition != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_groomingPosition.position, 0.3f);
            }
            
            // 绘制 Groomer 站位
            if (_groomerPosition != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(_groomerPosition.position, 0.3f);
            }
        }
        
        #endregion
    }
}
