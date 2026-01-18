using System;
using UnityEngine;
using UnityEngine.InputSystem;
using PetGrooming.Core;
using PetGrooming.AI;

namespace PetGrooming.Systems
{
    /// <summary>
    /// Groomer 角色控制器，扩展 ThirdPersonController 功能。
    /// 处理捕捉机制以及在抱起宠物时的移动速度调整。
    /// 需求：1.1, 1.2, 1.3, 3.1, 3.2
    /// </summary>
    public class GroomerController : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Configuration")]
        [SerializeField] private GameConfig _gameConfig;
        
        [Header("Capture Settings")]
        [Tooltip("Key to press for capture/interact")]
        [SerializeField] private KeyCode _captureKey = KeyCode.E;
        
        [Header("Cage Interaction")]
        [Tooltip("Key to press for cage interaction (store/release)")]
        [SerializeField] private KeyCode _cageInteractKey = KeyCode.F;
        
        [Header("References")]
        [SerializeField] private Transform _petHoldPoint;
        
        #endregion

        #region Private Fields
        
        private CharacterController _characterController;
        private float _baseSpeed;
        private PetAI _nearbyPet;
        private PetCage _nearbyCage;
        private bool _useMobileInput;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// Groomer 当前是否正抱着一只被捕获的宠物。
        /// </summary>
        public bool IsCarryingPet { get; private set; }
        
        /// <summary>
        /// 当前被捕获的宠物引用。
        /// </summary>
        public PetAI CapturedPet { get; private set; }
        
        /// <summary>
        /// 配置中的捕捉范围。
        /// 需求 3.1：在 1.5 单位内可以捕捉。
        /// </summary>
        public float CaptureRange => _gameConfig != null ? _gameConfig.CaptureRange : 1.5f;
        
        /// <summary>
        /// 抱着宠物时的速度倍率。
        /// 需求 1.3：移动速度降低 15%（倍率 0.85）。
        /// </summary>
        public float CarrySpeedMultiplier => _gameConfig != null ? _gameConfig.CarrySpeedMultiplier : 0.85f;
        
        /// <summary>
        /// 基础移动速度。
        /// 需求 1.1：每秒 5 个单位。
        /// </summary>
        public float BaseMoveSpeed => _gameConfig != null ? _gameConfig.GroomerMoveSpeed : 5f;
        
        /// <summary>
        /// 当前实际移动速度（考虑抱宠减速和警戒加速）。
        /// 需求 6.5：警戒状态下获得 10% 速度加成。
        /// </summary>
        public float CurrentMoveSpeed => CalculateFullEffectiveSpeed(
            BaseMoveSpeed, 
            CarrySpeedMultiplier, 
            IsCarryingPet,
            AlertSystem.Instance != null && AlertSystem.Instance.IsAlertActive,
            AlertSystem.Instance != null ? AlertSystem.Instance.GroomerSpeedBonus : 0.1f);
        
        /// <summary>
        /// 游戏配置引用。
        /// </summary>
        public GameConfig Config => _gameConfig;
        
        /// <summary>
        /// 是否启用了移动端输入模式。
        /// 需求 1.8：支持虚拟摇杆输入。
        /// </summary>
        public bool UseMobileInput => _useMobileInput;
        
        #endregion

        #region Events
        
        /// <summary>
        /// 成功捕获宠物时触发。
        /// </summary>
        public event Action<PetAI> OnPetCaptured;
        
        /// <summary>
        /// 被捕获的宠物逃脱时触发。
        /// </summary>
        public event Action OnPetEscaped;
        
        /// <summary>
        /// 由于距离原因导致捕捉失败时触发。
        /// </summary>
        public event Action OnCaptureFailed;
        
        /// <summary>
        /// 当已经抱着宠物而再次尝试捕捉被拒绝时触发。
        /// 需求 1.3：Groomer 同时只能携带一只宠物。
        /// </summary>
        public event Action OnCaptureRejectedAlreadyCarrying;
        
        /// <summary>
        /// 把宠物存入笼子时触发。
        /// </summary>
        public event Action<PetCage> OnPetStoredInCage;
        
        /// <summary>
        /// 从笼子中手动放出宠物时触发。
        /// 需求 8.5：Groomer 可以手动从笼子释放宠物。
        /// </summary>
        public event Action<PetCage> OnPetReleasedFromCage;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            
            if (_gameConfig == null)
            {
                Debug.LogWarning("[GroomerController] GameConfig is not assigned, using default values.");
            }
            
            // 如果未指定宠物挂点，则创建一个默认挂点
            if (_petHoldPoint == null)
            {
                GameObject holdPoint = new GameObject("PetHoldPoint");
                holdPoint.transform.SetParent(transform);
                holdPoint.transform.localPosition = new Vector3(0f, 1f, 0.5f);
                _petHoldPoint = holdPoint.transform;
            }
        }
        
        private void Start()
        {
            _baseSpeed = BaseMoveSpeed;
        }
        
        private void Update()
        {
            if (GameManager.Instance != null && 
                GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            {
                return;
            }
            
            HandleCaptureInput();
            HandleCageInteraction();
            UpdateCapturedPetPosition();
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// 尝试捕捉附近的宠物。
        /// 需求 1.3：Groomer 同时只能抱一只宠物。
        /// 需求 3.1：在 1.5 单位内可以捕捉。
        /// 需求 3.2：成功后宠物进入被捕获状态。
        /// </summary>
        /// <returns>如果捕捉成功返回 true。</returns>
        public bool TryCapturePet()
        {
            // 属性 2：单宠物携带约束
            // 需求 1.3：已经抱着宠物时应拒绝新的捕捉尝试
            if (IsCarryingPet)
            {
                Debug.Log("[GroomerController] Already carrying a pet - capture rejected.");
                OnCaptureRejectedAlreadyCarrying?.Invoke();
                return false;
            }
            
            PetAI nearestPet = FindNearestPet();
            if (nearestPet == null)
            {
                Debug.Log("[GroomerController] No pet found nearby.");
                OnCaptureFailed?.Invoke();
                return false;
            }
            
            // 检查宠物是否处于无敌状态
            if (nearestPet.IsInvulnerable)
            {
                Debug.Log("[GroomerController] Pet is invulnerable - capture rejected.");
                OnCaptureFailed?.Invoke();
                return false;
            }
            
            float distance = CalculateDistanceToPet(transform.position, nearestPet.transform.position);
            
            // 属性 6：捕捉距离校验
            if (!IsWithinCaptureRange(distance, CaptureRange))
            {
                Debug.Log($"[GroomerController] Pet too far. Distance: {distance}, Range: {CaptureRange}");
                OnCaptureFailed?.Invoke();
                return false;
            }
            
            // 捕捉成功
            CapturePet(nearestPet);
            return true;
        }
        
        /// <summary>
        /// 放下当前被捕获的宠物。
        /// </summary>
        public void ReleasePet()
        {
            if (!IsCarryingPet || CapturedPet == null)
            {
                return;
            }
            
            CapturedPet.transform.SetParent(null);
            CapturedPet = null;
            IsCarryingPet = false;
            
            Debug.Log("[GroomerController] Pet released.");
        }
        
        /// <summary>
        /// 被抱着的宠物逃脱时调用。
        /// </summary>
        public void OnPetEscape()
        {
            if (!IsCarryingPet || CapturedPet == null)
            {
                return;
            }
            
            PetAI escapedPet = CapturedPet;
            
            // 清理本地的捕捉状态
            CapturedPet.transform.SetParent(null);
            CapturedPet = null;
            IsCarryingPet = false;
            
            OnPetEscaped?.Invoke();
            
            Debug.Log("[GroomerController] Pet escaped!");
        }
        
        /// <summary>
        /// 设置附近宠物引用，用于捕捉检测。
        /// </summary>
        public void SetNearbyPet(PetAI pet)
        {
            _nearbyPet = pet;
        }
        
        /// <summary>
        /// 清除附近宠物引用。
        /// </summary>
        public void ClearNearbyPet()
        {
            _nearbyPet = null;
        }
        
        /// <summary>
        /// 设置附近笼子的引用，用于交互。
        /// </summary>
        public void SetNearbyCage(PetCage cage)
        {
            _nearbyCage = cage;
        }
        
        /// <summary>
        /// 清除附近笼子引用。
        /// </summary>
        public void ClearNearbyCage()
        {
            _nearbyCage = null;
        }
        
        /// <summary>
        /// 尝试将怀中的宠物存入附近笼子。
        /// 需求：1.4, 1.5
        /// </summary>
        /// <returns>如果存入成功返回 true。</returns>
        public bool TryStorePetInCage()
        {
            if (!IsCarryingPet || CapturedPet == null)
            {
                Debug.Log("[GroomerController] No pet to store in cage.");
                return false;
            }
            
            PetCage nearestCage = FindNearestCage();
            if (nearestCage == null)
            {
                Debug.Log("[GroomerController] No cage found nearby.");
                return false;
            }
            
            if (!nearestCage.CanStorePet())
            {
                Debug.Log("[GroomerController] Cage is already occupied.");
                return false;
            }
            
            if (!nearestCage.IsGroomerInRange(transform.position))
            {
                Debug.Log("[GroomerController] Cage is out of range.");
                return false;
            }
            
            // 将宠物存入笼子
            PetAI petToStore = CapturedPet;
            
            // 取消订阅宠物逃脱事件
            petToStore.OnEscaped -= OnPetEscape;
            
            // 清除携带状态
            petToStore.transform.SetParent(null);
            CapturedPet = null;
            IsCarryingPet = false;
            
            // 存入笼子
            if (nearestCage.StorePet(petToStore))
            {
                OnPetStoredInCage?.Invoke(nearestCage);
                Debug.Log("[GroomerController] Pet stored in cage successfully.");
                return true;
            }
            
            // 如果存储失败，恢复携带状态
            CapturePet(petToStore);
            return false;
        }
        
        /// <summary>
        /// 尝试从附近笼子中手动释放宠物。
        /// 需求 8.5：Groomer 可以手动从笼子释放宠物。
        /// </summary>
        /// <returns>如果释放成功返回 true。</returns>
        public bool TryReleasePetFromCage()
        {
            PetCage nearestCage = FindNearestCage();
            if (nearestCage == null)
            {
                Debug.Log("[GroomerController] No cage found nearby.");
                return false;
            }
            
            if (!nearestCage.CanManualRelease(transform.position))
            {
                Debug.Log("[GroomerController] Cannot release pet from cage (empty or out of range).");
                return false;
            }
            
            nearestCage.ManualRelease();
            OnPetReleasedFromCage?.Invoke(nearestCage);
            Debug.Log("[GroomerController] Pet manually released from cage.");
            return true;
        }
        
        /// <summary>
        /// 获取宠物挂点的 Transform。
        /// </summary>
        public Transform GetPetHoldPoint()
        {
            return _petHoldPoint;
        }
        
        /// <summary>
        /// 启用移动端输入模式。
        /// 需求 1.8：支持虚拟摇杆输入。
        /// </summary>
        public void EnableMobileInput()
        {
            _useMobileInput = true;
            Debug.Log("[GroomerController] Mobile input enabled.");
        }
        
        /// <summary>
        /// 禁用移动端输入模式。
        /// </summary>
        public void DisableMobileInput()
        {
            _useMobileInput = false;
            Debug.Log("[GroomerController] Mobile input disabled.");
        }
        
        /// <summary>
        /// 当移动端 UI 的捕捉按钮被按下时调用。
        /// 需求 2.4：捕捉按钮应触发一次捕捉尝试。
        /// </summary>
        public void OnCaptureButtonPressed()
        {
            TryCapturePet();
        }
        
        #endregion

        #region Private Methods
        
        private void HandleCaptureInput()
        {
            if (WasKeyPressedThisFrame(_captureKey))
            {
                TryCapturePet();
            }
        }
        
        private void HandleCageInteraction()
        {
            if (WasKeyPressedThisFrame(_cageInteractKey))
            {
                // If carrying a pet, try to store it
                if (IsCarryingPet)
                {
                    TryStorePetInCage();
                }
                // Otherwise, try to release a pet from cage
                else
                {
                    TryReleasePetFromCage();
                }
            }
        }
        
        /// <summary>
        /// 使用新输入系统检查某个按键在本帧是否被按下。
        /// </summary>
        private bool WasKeyPressedThisFrame(KeyCode keyCode)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return false;
            
            Key key = KeyCodeToKey(keyCode);
            return key != Key.None && keyboard[key].wasPressedThisFrame;
        }
        
        /// <summary>
        /// 将旧版 KeyCode 转换为新输入系统中的 Key。
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
        
        private void UpdateCapturedPetPosition()
        {
            if (IsCarryingPet && CapturedPet != null && _petHoldPoint != null)
            {
                CapturedPet.transform.position = _petHoldPoint.position;
                CapturedPet.transform.rotation = _petHoldPoint.rotation;
            }
        }
        
        private PetAI FindNearestPet()
        {
            // 优先使用已有的附近宠物引用
            if (_nearbyPet != null)
            {
                return _nearbyPet;
            }
            
            // 否则在场景中搜索所有宠物
            PetAI[] pets = FindObjectsOfType<PetAI>();
            PetAI nearest = null;
            float nearestDistance = float.MaxValue;
            
            foreach (PetAI pet in pets)
            {
                // 跳过已经被捕获或正在被美容的宠物
                if (pet.CurrentState == PetAI.PetState.Captured || 
                    pet.CurrentState == PetAI.PetState.BeingGroomed)
                {
                    continue;
                }
                
                float distance = Vector3.Distance(transform.position, pet.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = pet;
                }
            }
            
            return nearest;
        }
        
        private PetCage FindNearestCage()
        {
            // 优先使用已有的附近笼子引用
            if (_nearbyCage != null)
            {
                return _nearbyCage;
            }
            
            // 否则在场景中搜索所有笼子
            PetCage[] cages = FindObjectsOfType<PetCage>();
            PetCage nearest = null;
            float nearestDistance = float.MaxValue;
            
            foreach (PetCage cage in cages)
            {
                float distance = Vector3.Distance(transform.position, cage.transform.position);
                if (distance < nearestDistance && cage.IsGroomerInRange(transform.position))
                {
                    nearestDistance = distance;
                    nearest = cage;
                }
            }
            
            return nearest;
        }
        
        private void CapturePet(PetAI pet)
        {
            CapturedPet = pet;
            IsCarryingPet = true;
            
            // 将宠物作为子物体挂到挂点上
            pet.transform.SetParent(_petHoldPoint);
            pet.transform.localPosition = Vector3.zero;
            pet.transform.localRotation = Quaternion.identity;
            
            // 通知宠物已被捕获
            pet.OnCaptured(transform);
            
            // 订阅宠物逃脱事件
            pet.OnEscaped += OnPetEscape;
            
            OnPetCaptured?.Invoke(pet);
            
            Debug.Log("[GroomerController] Pet captured!");
        }
        
        #endregion

        #region Static Calculation Methods (Testable)
        
        /// <summary>
        /// 根据是否抱宠物计算有效移动速度。
        /// 属性 1：抱宠物速度衰减。
        /// 需求 1.3：抱宠物时速度降低 15%。
        /// </summary>
        /// <param name="baseSpeed">基础移动速度。</param>
        /// <param name="carryMultiplier">抱宠物时的速度倍率（0.85 代表降低 15%）。</param>
        /// <param name="isCarrying">当前是否正在抱宠物。</param>
        /// <returns>计算后的有效移动速度。</returns>
        public static float CalculateEffectiveSpeed(float baseSpeed, float carryMultiplier, bool isCarrying)
        {
            if (isCarrying)
            {
                return baseSpeed * carryMultiplier;
            }
            return baseSpeed;
        }
        
        /// <summary>
        /// 计算包含抱宠减速与警戒加速在内的最终移动速度。
        /// 属性 17：警戒状态速度加成。
        /// 需求 6.5：警戒状态下获得 10% 速度加成。
        /// </summary>
        /// <param name="baseSpeed">基础移动速度。</param>
        /// <param name="carryMultiplier">抱宠物时的速度倍率（0.85 代表降低 15%）。</param>
        /// <param name="isCarrying">当前是否正在抱宠物。</param>
        /// <param name="isAlertActive">当前是否处于警戒状态。</param>
        /// <param name="alertSpeedBonus">警戒状态下的速度加成（0.1 = 10%）。</param>
        /// <returns>最终有效移动速度。</returns>
        public static float CalculateFullEffectiveSpeed(
            float baseSpeed, 
            float carryMultiplier, 
            bool isCarrying,
            bool isAlertActive,
            float alertSpeedBonus)
        {
            float speed = CalculateEffectiveSpeed(baseSpeed, carryMultiplier, isCarrying);
            
            // Apply alert speed bonus
            if (isAlertActive)
            {
                speed *= (1.0f + alertSpeedBonus);
            }
            
            return speed;
        }
        
        /// <summary>
        /// 计算 Groomer 与宠物之间的距离。
        /// </summary>
        /// <param name="groomerPosition">Groomer 位置。</param>
        /// <param name="petPosition">宠物位置。</param>
        /// <returns>两者之间的距离。</returns>
        public static float CalculateDistanceToPet(Vector3 groomerPosition, Vector3 petPosition)
        {
            // 只计算水平距离（忽略 Y 轴，用于地面捕捉）
            Vector3 diff = groomerPosition - petPosition;
            diff.y = 0f;
            return diff.magnitude;
        }
        
        /// <summary>
        /// 根据距离判断捕捉尝试是否应该成功。
        /// 属性 6：捕捉距离校验。
        /// 需求 3.1：在 1.5 单位内可以捕捉。
        /// </summary>
        /// <param name="distance">与宠物的距离。</param>
        /// <param name="captureRange">最大捕捉范围。</param>
        /// <returns>在捕捉范围内返回 true。</returns>
        public static bool IsWithinCaptureRange(float distance, float captureRange)
        {
            return distance <= captureRange;
        }
        
        /// <summary>
        /// 根据距离和宠物状态判断捕捉是否应成功。
        /// 属性 6：捕捉距离校验。
        /// </summary>
        /// <param name="distance">与宠物的距离。</param>
        /// <param name="captureRange">最大捕捉范围。</param>
        /// <param name="petState">当前宠物状态。</param>
        /// <returns>捕捉应成功则返回 true。</returns>
        public static bool ShouldCaptureSucceed(float distance, float captureRange, PetAI.PetState petState)
        {
            // 不能捕捉已经被捕获或正在美容的宠物
            if (petState == PetAI.PetState.Captured || petState == PetAI.PetState.BeingGroomed)
            {
                return false;
            }
            
            return IsWithinCaptureRange(distance, captureRange);
        }
        
        /// <summary>
        /// 判断由于已在抱宠物，捕捉尝试是否应该被拒绝。
        /// 属性 2：单宠物携带约束。
        /// 需求 1.3：Groomer 同时只能携带一只宠物。
        /// </summary>
        /// <param name="isCurrentlyCarrying">当前是否正在抱宠物。</param>
        /// <param name="currentPet">当前抱着的宠物（可为 null）。</param>
        /// <returns>如果因已抱宠物而应拒绝捕捉，返回 true。</returns>
        public static bool ShouldRejectCaptureAlreadyCarrying(bool isCurrentlyCarrying, PetAI currentPet)
        {
            return isCurrentlyCarrying && currentPet != null;
        }
        
        /// <summary>
        /// 校验“单宠物携带”约束是否被满足。
        /// 属性 2：单宠物携带约束。
        /// 需求 1.3：当 Groomer 已经抱着一只宠物时，再次捕捉必须被系统拒绝。
        /// </summary>
        /// <param name="isCarryingBefore">捕捉尝试前是否在抱宠物。</param>
        /// <param name="capturedPetBefore">尝试前抱着的宠物。</param>
        /// <param name="captureAttemptResult">本次捕捉尝试结果。</param>
        /// <param name="isCarryingAfter">尝试后是否在抱宠物。</param>
        /// <param name="capturedPetAfter">尝试后抱着的宠物。</param>
        /// <returns>如果该约束被满足则返回 true。</returns>
        public static bool ValidateSinglePetCarryConstraint(
            bool isCarryingBefore, 
            PetAI capturedPetBefore,
            bool captureAttemptResult,
            bool isCarryingAfter,
            PetAI capturedPetAfter)
        {
            // If already carrying a pet before the attempt
            if (isCarryingBefore && capturedPetBefore != null)
            {
                // Capture attempt must fail
                if (captureAttemptResult)
                {
                    return false;
                }
                
                // The carried pet must remain unchanged
                if (!isCarryingAfter || capturedPetAfter != capturedPetBefore)
                {
                    return false;
                }
            }
            
            return true;
        }
        
        #endregion

        #region Editor Support
        
#if UNITY_EDITOR
        /// <summary>
        /// 设置用于测试的 GameConfig。
        /// </summary>
        public void SetConfigForTesting(GameConfig config)
        {
            _gameConfig = config;
        }
        
        /// <summary>
        /// 设置携带状态（测试用）。
        /// </summary>
        public void SetCarryingStateForTesting(bool isCarrying, PetAI pet = null)
        {
            IsCarryingPet = isCarrying;
            CapturedPet = pet;
        }
#endif
        
        private void OnDrawGizmosSelected()
        {
            // 绘制捕捉范围
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, CaptureRange);
            
            // 绘制宠物挂点位置
            if (_petHoldPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(_petHoldPoint.position, 0.2f);
            }
        }
        
        #endregion
    }
}
