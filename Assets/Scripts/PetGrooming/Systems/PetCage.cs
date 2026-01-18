using System;
using UnityEngine;
using PetGrooming.AI;
using PetGrooming.Core;

namespace PetGrooming.Systems
{
    /// <summary>
    /// 用于临时关押已捕获宠物的宠物笼系统。
    /// 需求：1.4, 1.5, 1.6, 8.1, 8.2, 8.3, 8.4, 8.5, 8.6
    /// </summary>
    public class PetCage : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Configuration")]
        [SerializeField] private Phase2GameConfig _phase2Config;
        
        [Header("Cage Settings")]
        [Tooltip("Maximum time a pet can be stored (seconds). Overridden by Phase2Config if assigned.")]
        [SerializeField] private float _maxStorageTime = 60f;
        
        [Tooltip("Time remaining when warning indicator appears (seconds). Overridden by Phase2Config if assigned.")]
        [SerializeField] private float _warningTime = 10f;
        
        [Tooltip("Invulnerability duration after release (seconds). Overridden by Phase2Config if assigned.")]
        [SerializeField] private float _releaseInvulnerabilityTime = 3f;
        
        [Header("Positions")]
        [Tooltip("Position where the pet is stored inside the cage")]
        [SerializeField] private Transform _storagePosition;
        
        [Tooltip("Position where the pet spawns when released")]
        [SerializeField] private Transform _releasePosition;
        
        [Header("Interaction")]
        [Tooltip("Distance within which groomer can interact with the cage")]
        [SerializeField] private float _interactionRange = 2f;
        
        [Header("Visual Indicators")]
        [Tooltip("Renderer for visual state indication")]
        [SerializeField] private Renderer _cageRenderer;
        
        [Tooltip("Color when cage is empty")]
        [SerializeField] private Color _emptyColor = Color.green;
        
        [Tooltip("Color when cage is occupied")]
        [SerializeField] private Color _occupiedColor = Color.yellow;
        
        [Tooltip("Color during warning state")]
        [SerializeField] private Color _warningColor = Color.red;
        
        #endregion

        #region Private Fields
        
        private float _currentStorageTime;
        private bool _isWarningActive;
        private Material _cageMaterial;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// 笼子当前是否关着宠物。
        /// 需求 8.1：通过可视化指示笼子是空的还是被占用。
        /// </summary>
        public bool IsOccupied { get; private set; }
        
        /// <summary>
        /// 当前存放在笼子里的宠物。
        /// </summary>
        public PetAI StoredPet { get; private set; }
        
        /// <summary>
        /// 距离自动释放剩余的时间。
        /// 需求 8.2：显示倒计时计时器。
        /// </summary>
        public float RemainingTime => Mathf.Max(0f, MaxStorageTime - _currentStorageTime);
        
        /// <summary>
        /// 配置中的最大关押时间（或默认值）。
        /// 需求 1.5：宠物最多可被关押 60 秒。
        /// </summary>
        public float MaxStorageTime => _phase2Config != null ? _phase2Config.CageStorageTime : _maxStorageTime;
        
        /// <summary>
        /// 配置中的警告时间阈值（或默认值）。
        /// 需求 8.3：剩余 10 秒时触发警告。
        /// </summary>
        public float WarningTime => _phase2Config != null ? _phase2Config.CageWarningTime : _warningTime;
        
        /// <summary>
        /// 配置中的释放后无敌持续时间（或默认值）。
        /// 需求 8.4：释放后 3 秒无敌。
        /// </summary>
        public float ReleaseInvulnerabilityDuration => _phase2Config != null ? _phase2Config.ReleaseInvulnerabilityTime : _releaseInvulnerabilityTime;
        
        /// <summary>
        /// Groomer 可以与笼子交互的距离。
        /// </summary>
        public float InteractionRange => _interactionRange;
        
        /// <summary>
        /// 当前是否处于警告状态。
        /// </summary>
        public bool IsWarningActive => _isWarningActive;
        
        /// <summary>
        /// 第二阶段配置引用。
        /// </summary>
        public Phase2GameConfig Phase2Config => _phase2Config;
        
        #endregion

        #region Events
        
        /// <summary>
        /// 当宠物被存入笼子时触发。
        /// </summary>
        public event Action<PetAI> OnPetStored;
        
        /// <summary>
        /// 当宠物从笼子中被释放时触发（自动或手动）。
        /// </summary>
        public event Action<PetAI> OnPetReleased;
        
        /// <summary>
        /// 当进入警告状态时触发（剩余 10 秒）。
        /// 需求 8.3：在剩余 10 秒时显示警告指示。
        /// </summary>
        public event Action OnWarningStarted;
        
        /// <summary>
        /// 当剩余时间变化时触发（用于 UI 更新）。
        /// </summary>
        public event Action<float> OnRemainingTimeChanged;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            ValidateReferences();
            
            // 缓存材质以便后续修改颜色
            if (_cageRenderer != null)
            {
                _cageMaterial = _cageRenderer.material;
            }
        }
        
        private void Start()
        {
            UpdateVisualState();
        }
        
        private void Update()
        {
            if (!IsOccupied || StoredPet == null)
            {
                return;
            }
            
            // Skip timer update if game is not playing
            if (GameManager.Instance != null && 
                GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            {
                return;
            }
            
            UpdateStorageTimer();
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// 检查笼子当前是否可以存放宠物。
        /// </summary>
        /// <returns>如果笼子为空且可以存宠物则返回 true。</returns>
        public bool CanStorePet()
        {
            return !IsOccupied;
        }
        
        /// <summary>
        /// 将一只宠物存入笼子。
        /// 需求：1.4, 1.5, 8.1, 8.6。
        /// </summary>
        /// <param name="pet">要存入的宠物。</param>
        /// <returns>存入成功返回 true。</returns>
        public bool StorePet(PetAI pet)
        {
            if (pet == null)
            {
                Debug.LogError("[PetCage] Cannot store null pet!");
                return false;
            }
            
            if (IsOccupied)
            {
                Debug.LogWarning("[PetCage] Cage is already occupied!");
                return false;
            }
            
            // 存入宠物
            StoredPet = pet;
            IsOccupied = true;
            _currentStorageTime = 0f;
            _isWarningActive = false;
            
            // 把宠物摆放到笼子内部位置
            if (_storagePosition != null)
            {
                pet.transform.SetParent(_storagePosition);
                pet.transform.localPosition = Vector3.zero;
                pet.transform.localRotation = Quaternion.identity;
            }
            else
            {
                pet.transform.SetParent(transform);
                pet.transform.localPosition = Vector3.zero;
            }
            
            // 被关押期间禁用宠物 AI
            pet.SetState(PetAI.PetState.Captured);
            
            // 需求 8.6：标记宠物为已关押，使其不再产生恶作剧值
            pet.SetCaged(true);
            
            UpdateVisualState();
            
            OnPetStored?.Invoke(pet);
            
            Debug.Log($"[PetCage] Pet stored: {pet.name}. Timer started for {MaxStorageTime} seconds.");
            
            return true;
        }
        
        /// <summary>
        /// 释放宠物（计时结束后的自动释放）。
        /// 需求：1.6, 8.4。
        /// </summary>
        public void ReleasePet()
        {
            ReleasePetInternal(isManual: false);
        }
        
        /// <summary>
        /// 手动释放笼子中的宠物。
        /// 需求 8.5：Groomer 可以手动释放宠物。
        /// </summary>
        public void ManualRelease()
        {
            ReleasePetInternal(isManual: true);
        }
        
        /// <summary>
        /// 检查 Groomer 是否在笼子的交互范围内。
        /// </summary>
        /// <param name="groomerPosition">Groomer 的位置。</param>
        /// <returns>在范围内则返回 true。</returns>
        public bool IsGroomerInRange(Vector3 groomerPosition)
        {
            return IsWithinRange(groomerPosition, transform.position, _interactionRange);
        }
        
        /// <summary>
        /// 检查是否可以手动释放宠物。
        /// 需求 8.5：Groomer 可与笼子交互来释放宠物。
        /// </summary>
        /// <param name="groomerPosition">Groomer 的位置。</param>
        /// <returns>如果可以手动释放则返回 true。</returns>
        public bool CanManualRelease(Vector3 groomerPosition)
        {
            return IsOccupied && StoredPet != null && IsGroomerInRange(groomerPosition);
        }
        
        /// <summary>
        /// 获取释放宠物的世界坐标位置。
        /// </summary>
        /// <returns>宠物被释放时将被放置的世界坐标。</returns>
        public Vector3 GetReleaseWorldPosition()
        {
            return _releasePosition != null ? _releasePosition.position : transform.position + Vector3.forward;
        }
        
        #endregion

        #region Private Methods
        
        private void ValidateReferences()
        {
            if (_storagePosition == null)
            {
                Debug.LogWarning("[PetCage] 未指定存放位置，正在创建默认位置。");
                var storagePosObj = new GameObject("StoragePosition");
                storagePosObj.transform.SetParent(transform);
                storagePosObj.transform.localPosition = Vector3.zero;
                _storagePosition = storagePosObj.transform;
            }
            
            if (_releasePosition == null)
            {
                Debug.LogWarning("[PetCage] 未指定释放位置，正在创建默认位置。");
                var releasePosObj = new GameObject("ReleasePosition");
                releasePosObj.transform.SetParent(transform);
                releasePosObj.transform.localPosition = Vector3.forward * 1.5f;
                _releasePosition = releasePosObj.transform;
            }
        }
        
        private void UpdateStorageTimer()
        {
            float previousTime = _currentStorageTime;
            _currentStorageTime += Time.deltaTime;
            
            // 通知剩余时间发生变化
            OnRemainingTimeChanged?.Invoke(RemainingTime);
            
            // 检查是否进入警告状态
            // 需求 8.3：剩余 10 秒时进入警告状态
            if (!_isWarningActive && RemainingTime <= WarningTime)
            {
                _isWarningActive = true;
                UpdateVisualState();
                OnWarningStarted?.Invoke();
                Debug.Log($"[PetCage] Warning! {RemainingTime:F1} seconds remaining.");
            }
            
            // 检查是否需要自动释放
            // 需求 1.6：在 60 秒后自动释放
            if (_currentStorageTime >= MaxStorageTime)
            {
                Debug.Log("[PetCage] Storage time expired - auto-releasing pet.");
                ReleasePet();
            }
        }
        
        private void ReleasePetInternal(bool isManual)
        {
            if (!IsOccupied || StoredPet == null)
            {
                Debug.LogWarning("[PetCage] No pet to release!");
                return;
            }
            
            PetAI releasedPet = StoredPet;
            
            // 清空笼子状态
            StoredPet = null;
            IsOccupied = false;
            _currentStorageTime = 0f;
            _isWarningActive = false;
            
            // 移除父子关系并设置宠物位置
            releasedPet.transform.SetParent(null);
            releasedPet.transform.position = GetReleaseWorldPosition();
            
            // 需求 8.6：清除关押标记，使宠物重新可以制造恶作剧
            releasedPet.SetCaged(false);
            
            // 需求 8.4：释放后赋予宠物一段时间无敌
            releasedPet.SetInvulnerable(ReleaseInvulnerabilityDuration);
            
            // 将宠物状态恢复为 Idle
            releasedPet.SetState(PetAI.PetState.Idle);
            
            UpdateVisualState();
            
            OnPetReleased?.Invoke(releasedPet);
            
            string releaseType = isManual ? "manually" : "automatically";
            Debug.Log($"[PetCage] Pet {releaseType} released: {releasedPet.name} with {ReleaseInvulnerabilityDuration}s invulnerability.");
        }
        
        private void UpdateVisualState()
        {
            if (_cageMaterial == null) return;
            
            Color targetColor;
            
            if (!IsOccupied)
            {
                targetColor = _emptyColor;
            }
            else if (_isWarningActive)
            {
                targetColor = _warningColor;
            }
            else
            {
                targetColor = _occupiedColor;
            }
            
            _cageMaterial.color = targetColor;
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
        /// 计算还剩多少关押时间。
        /// 属性 3：宠物笼关押时长。
        /// </summary>
        /// <param name="currentTime">已关押的时间。</param>
        /// <param name="maxTime">最大关押时间。</param>
        /// <returns>剩余时间（秒）。</returns>
        public static float CalculateRemainingTime(float currentTime, float maxTime)
        {
            return Mathf.Max(0f, maxTime - currentTime);
        }
        
        /// <summary>
        /// 判断是否应该进入警告状态。
        /// 需求 8.3：剩余 10 秒时进入警告状态。
        /// </summary>
        /// <param name="remainingTime">剩余关押时间。</param>
        /// <param name="warningThreshold">警告阈值时间。</param>
        /// <returns>应处于警告状态时返回 true。</returns>
        public static bool ShouldShowWarning(float remainingTime, float warningThreshold)
        {
            return remainingTime <= warningThreshold && remainingTime > 0f;
        }
        
        /// <summary>
        /// 判断是否应触发自动释放。
        /// 需求 1.6：在 60 秒后自动释放。
        /// </summary>
        /// <param name="currentTime">当前已关押时间。</param>
        /// <param name="maxTime">最大关押时间。</param>
        /// <returns>应自动释放时返回 true。</returns>
        public static bool ShouldAutoRelease(float currentTime, float maxTime)
        {
            return currentTime >= maxTime;
        }
        
        /// <summary>
        /// 校验关押时长是否符合预期。
        /// 属性 3：宠物笼关押时长——精确 60 秒。
        /// </summary>
        /// <param name="storageDuration">实际关押时间。</param>
        /// <param name="expectedDuration">期望关押时间（60 秒）。</param>
        /// <param name="tolerance">允许误差范围。</param>
        /// <returns>在误差范围内返回 true。</returns>
        public static bool ValidateStorageDuration(float storageDuration, float expectedDuration, float tolerance = 0.1f)
        {
            return Mathf.Abs(storageDuration - expectedDuration) <= tolerance;
        }
        
        /// <summary>
        /// 校验无敌时长是否符合预期。
        /// 属性 19：被关押宠物释放后的无敌时间——精确 3 秒。
        /// </summary>
        /// <param name="invulnerabilityDuration">实际无敌时长。</param>
        /// <param name="expectedDuration">期望时长（3 秒）。</param>
        /// <param name="tolerance">允许误差。</param>
        /// <returns>在误差范围内返回 true。</returns>
        public static bool ValidateInvulnerabilityDuration(float invulnerabilityDuration, float expectedDuration, float tolerance = 0.1f)
        {
            return Mathf.Abs(invulnerabilityDuration - expectedDuration) <= tolerance;
        }
        
        #endregion

        #region Editor Support
        
#if UNITY_EDITOR
        /// <summary>
        /// 设置 Phase2GameConfig（测试用）。
        /// </summary>
        public void SetPhase2ConfigForTesting(Phase2GameConfig config)
        {
            _phase2Config = config;
        }
        
        /// <summary>
        /// 设置当前关押时间（测试用）。
        /// </summary>
        public void SetStorageTimeForTesting(float time)
        {
            _currentStorageTime = time;
        }
        
        /// <summary>
        /// 设置笼子占用状态（测试用）。
        /// </summary>
        public void SetOccupiedForTesting(bool occupied, PetAI pet = null)
        {
            IsOccupied = occupied;
            StoredPet = pet;
        }
        
        /// <summary>
        /// 获取当前关押时间（测试用）。
        /// </summary>
        public float GetCurrentStorageTimeForTesting()
        {
            return _currentStorageTime;
        }
        
        /// <summary>
        /// 直接设置最大关押时间（测试用，忽略配置）。
        /// </summary>
        public void SetMaxStorageTimeForTesting(float time)
        {
            _maxStorageTime = time;
        }
        
        /// <summary>
        /// 直接设置警告时间（测试用，忽略配置）。
        /// </summary>
        public void SetWarningTimeForTesting(float time)
        {
            _warningTime = time;
        }
        
        /// <summary>
        /// 直接设置无敌时间（测试用，忽略配置）。
        /// </summary>
        public void SetInvulnerabilityTimeForTesting(float time)
        {
            _releaseInvulnerabilityTime = time;
        }
#endif
        
        private void OnDrawGizmosSelected()
        {
            // 绘制交互范围
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _interactionRange);
            
            // 绘制存放位置
            if (_storagePosition != null)
            {
                Gizmos.color = IsOccupied ? Color.yellow : Color.green;
                Gizmos.DrawWireSphere(_storagePosition.position, 0.3f);
            }
            
            // 绘制释放位置
            if (_releasePosition != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(_releasePosition.position, 0.3f);
                
                // 从存放位置画一条线到释放位置
                if (_storagePosition != null)
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawLine(_storagePosition.position, _releasePosition.position);
                }
            }
        }
        
        #endregion
    }
}
