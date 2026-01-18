using System;
using UnityEngine;
using PetGrooming.Core;

namespace PetGrooming.Systems
{
    /// <summary>
    /// 管理恶作剧值的累积与阈值检测。
    /// 需求：5.1, 5.2, 5.3, 5.5, 6.1, 6.2, 6.6
    /// </summary>
    public class MischiefSystem : MonoBehaviour
    {
        #region Singleton
        
        private static MischiefSystem _instance;
        
        public static MischiefSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<MischiefSystem>();
                    if (_instance == null)
                    {
                        Debug.LogError("[MischiefSystem] No MischiefSystem instance found in scene!");
                    }
                }
                return _instance;
            }
        }
        
        #endregion

        #region Serialized Fields
        
        [Header("Configuration")]
        [SerializeField] private GameConfig _gameConfig;
        
        [Header("Phase 2 Configuration")]
        [SerializeField] private Phase2GameConfig _phase2Config;
        
        #endregion

        #region Private Fields
        
        private int _dynamicThreshold;
        private bool _useDynamicThreshold;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// 当前累积的恶作剧值。
        /// 需求 5.1：从 0 开始累积恶作剧值。
        /// </summary>
        public int CurrentMischiefValue { get; private set; }
        
        /// <summary>
        /// 宠物获胜所需的恶作剧阈值。
        /// 属性 15：恶作剧阈值与游戏模式匹配。
        /// 需求 6.1, 6.2：2 宠模式 800，3 宠模式 1000。
        /// </summary>
        public int MischiefThreshold
        {
            get
            {
                if (_useDynamicThreshold)
                {
                    return _dynamicThreshold;
                }
                return _gameConfig != null ? _gameConfig.MischiefThreshold : 500;
            }
        }
        
        /// <summary>
        /// 宠物技能击中 Groomer 时增加的恶作剧值。
        /// 需求 6.6：每次命中增加 30 分。
        /// </summary>
        public int PetSkillHitMischief => _phase2Config != null ? _phase2Config.PetSkillHitMischief : 30;
        
        /// <summary>
        /// 第二阶段游戏配置引用。
        /// </summary>
        public Phase2GameConfig Phase2Config => _phase2Config;
        
        /// <summary>
        /// 货架物品被破坏时增加的恶作剧值。
        /// 需求 5.2：每件货架物品增加 50 分。
        /// </summary>
        public int ShelfItemMischief => _gameConfig != null ? _gameConfig.ShelfItemMischief : 50;
        
        /// <summary>
        /// 清洁车被破坏时增加的恶作剧值。
        /// 需求 5.3：每辆清洁车增加 80 分。
        /// </summary>
        public int CleaningCartMischief => _gameConfig != null ? _gameConfig.CleaningCartMischief : 80;
        
        /// <summary>
        /// 游戏配置引用。
        /// </summary>
        public GameConfig Config => _gameConfig;
        
        #endregion

        #region Events
        
        /// <summary>
        /// 恶作剧值变化时触发。
        /// </summary>
        public event Action<int> OnMischiefValueChanged;
        
        /// <summary>
        /// 当恶作剧值达到阈值时触发。
        /// 需求 5.5：达到阈值后触发宠物胜利。
        /// </summary>
        public event Action OnThresholdReached;
        
        /// <summary>
        /// 当宠物技能击中 Groomer 时触发。
        /// 需求 6.6：命中时增加 30 分恶作剧值。
        /// </summary>
        public event Action<int> OnPetSkillHitMischief;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            // 单例初始化
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[MischiefSystem] Duplicate MischiefSystem detected, destroying this instance.");
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            
            // 校验配置
            if (_gameConfig == null)
            {
                Debug.LogError("[MischiefSystem] GameConfig is not assigned!");
            }
            
            if (_phase2Config == null)
            {
                Debug.LogWarning("[MischiefSystem] Phase2GameConfig is not assigned, using default values for Phase 2 features.");
            }
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// 根据游戏模式设置动态恶作剧阈值。
        /// 属性 15：恶作剧阈值与游戏模式匹配。
        /// 需求 6.1, 6.2：2 宠模式 800，3 宠模式 1000。
        /// </summary>
        /// <param name="gameMode">当前游戏模式。</param>
        public void SetDynamicThreshold(PetSpawnManager.GameMode gameMode)
        {
            _useDynamicThreshold = true;
            _dynamicThreshold = GetThresholdForGameMode(gameMode);
            
            Debug.Log($"[MischiefSystem] Dynamic threshold set to {_dynamicThreshold} for {gameMode} mode");
        }
        
        /// <summary>
        /// 根据宠物数量设置动态恶作剧阈值。
        /// 属性 15：恶作剧阈值与游戏模式匹配。
        /// 需求 6.1, 6.2：2 宠模式 800，3 宠模式 1000。
        /// </summary>
        /// <param name="petCount">当前对局中的宠物数量。</param>
        public void SetDynamicThresholdByPetCount(int petCount)
        {
            _useDynamicThreshold = true;
            _dynamicThreshold = GetThresholdForPetCount(petCount);
            
            Debug.Log($"[MischiefSystem] Dynamic threshold set to {_dynamicThreshold} for {petCount} pets");
        }
        
        /// <summary>
        /// 向当前恶作剧值中增加一定数值。
        /// 相关需求：5.2, 5.3, 5.5。
        /// </summary>
        /// <param name="amount">要增加的恶作剧值。</param>
        public void AddMischief(int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[MischiefSystem] Attempted to add non-positive mischief: {amount}");
                return;
            }
            
            int previousValue = CurrentMischiefValue;
            CurrentMischiefValue += amount;
            
            Debug.Log($"[MischiefSystem] Mischief added: {amount}. Total: {CurrentMischiefValue}/{MischiefThreshold}");
            
            OnMischiefValueChanged?.Invoke(CurrentMischiefValue);
            
            // 通知 GameManager 恶作剧值已变化
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnMischiefValueChanged(CurrentMischiefValue);
            }
            
            // 检查是否到达阈值
            // 需求 5.5：当恶作剧值达到阈值时，宠物获胜
            if (previousValue < MischiefThreshold && CurrentMischiefValue >= MischiefThreshold)
            {
                Debug.Log($"[MischiefSystem] Threshold reached! ({CurrentMischiefValue} >= {MischiefThreshold})");
                OnThresholdReached?.Invoke();
            }
        }
        
        /// <summary>
        /// 货架物品碰撞时增加恶作剧值。
        /// 需求 5.2：每件货架物品增加 50 分。
        /// </summary>
        public void AddShelfItemMischief()
        {
            AddMischief(ShelfItemMischief);
        }
        
        /// <summary>
        /// 清洁车碰撞时增加恶作剧值。
        /// 需求 5.3：每辆清洁车增加 80 分。
        /// </summary>
        public void AddCleaningCartMischief()
        {
            AddMischief(CleaningCartMischief);
        }
        
        /// <summary>
        /// 将恶作剧值重置为 0。
        /// </summary>
        public void Reset()
        {
            CurrentMischiefValue = 0;
            OnMischiefValueChanged?.Invoke(CurrentMischiefValue);
            Debug.Log("[MischiefSystem] Mischief value reset to 0.");
        }
        
        /// <summary>
        /// 当宠物技能击中 Groomer 时增加恶作剧值。
        /// 属性 18：宠物技能命中恶作剧加成。
        /// 需求 6.6：每次命中增加 30 分。
        /// </summary>
        public void AddPetSkillHitMischief()
        {
            int mischiefToAdd = PetSkillHitMischief;
            AddMischief(mischiefToAdd);
            OnPetSkillHitMischief?.Invoke(mischiefToAdd);
            
            Debug.Log($"[MischiefSystem] Pet skill hit Groomer! Added {mischiefToAdd} mischief points.");
        }
        
        #endregion

        #region Static Calculation Methods (Testable)
        
        /// <summary>
        /// 获取指定游戏模式下的恶作剧阈值。
        /// 属性 15：恶作剧阈值与游戏模式匹配。
        /// 需求 6.1, 6.2：2 宠模式 800，3 宠模式 1000。
        /// </summary>
        /// <param name="gameMode">游戏模式。</param>
        /// <returns>对应游戏模式下的恶作剧阈值。</returns>
        public static int GetThresholdForGameMode(PetSpawnManager.GameMode gameMode)
        {
            return gameMode switch
            {
                PetSpawnManager.GameMode.TwoPets => 800,
                PetSpawnManager.GameMode.ThreePets => 1000,
                _ => 800
            };
        }
        
        /// <summary>
        /// 根据宠物数量获取恶作剧阈值。
        /// 属性 15：恶作剧阈值与游戏模式匹配。
        /// 需求 6.1, 6.2：2 宠模式 800，3 宠模式 1000。
        /// </summary>
        /// <param name="petCount">对局中宠物数量。</param>
        /// <returns>对应宠物数量的恶作剧阈值。</returns>
        public static int GetThresholdForPetCount(int petCount)
        {
            return petCount >= 3 ? 1000 : 800;
        }
        
        /// <summary>
        /// 获取一次宠物技能命中 Groomer 时的恶作剧增量。
        /// 属性 18：宠物技能命中恶作剧加成。
        /// 需求 6.6：命中增加 30 分。
        /// </summary>
        /// <returns>宠物技能命中时增加的恶作剧值（30 分）。</returns>
        public static int GetPetSkillHitMischiefValue()
        {
            return 30;
        }
        
        /// <summary>
        /// 计算在增加一定数值后的新恶作剧值。
        /// 该方法用于性质测试，便于单元测试验证。
        /// </summary>
        /// <param name="currentValue">当前恶作剧值。</param>
        /// <param name="amountToAdd">要增加的数值。</param>
        /// <returns>增加后的恶作剧值。</returns>
        public static int CalculateMischiefValue(int currentValue, int amountToAdd)
        {
            if (amountToAdd <= 0)
            {
                return currentValue;
            }
            return currentValue + amountToAdd;
        }
        
        /// <summary>
        /// 获取某个可破坏物体类型对应的恶作剧值。
        /// 属性 5：恶作剧值计算。
        /// </summary>
        /// <param name="objectType">可破坏物体的类型。</param>
        /// <param name="shelfItemValue">货架物品的恶作剧值。</param>
        /// <param name="cleaningCartValue">清洁车的恶作剧值。</param>
        /// <returns>该物体类型对应的恶作剧值。</returns>
        public static int GetMischiefValueForObjectType(
            DestructibleObjectType objectType, 
            int shelfItemValue = 50, 
            int cleaningCartValue = 80)
        {
            return objectType switch
            {
                DestructibleObjectType.ShelfItem => shelfItemValue,
                DestructibleObjectType.CleaningCart => cleaningCartValue,
                _ => 0
            };
        }
        
        /// <summary>
        /// 检查当前恶作剧值是否达到阈值。
        /// </summary>
        /// <param name="currentValue">当前恶作剧值。</param>
        /// <param name="threshold">阈值。</param>
        /// <returns>达到或超过阈值时返回 true。</returns>
        public static bool IsThresholdReached(int currentValue, int threshold)
        {
            return currentValue >= threshold;
        }
        
        #endregion

        #region Editor Support
        
#if UNITY_EDITOR
        /// <summary>
        /// 设置 GameConfig（测试用）。
        /// </summary>
        public void SetConfigForTesting(GameConfig config)
        {
            _gameConfig = config;
        }
        
        /// <summary>
        /// 设置 Phase2GameConfig（测试用）。
        /// </summary>
        public void SetPhase2ConfigForTesting(Phase2GameConfig config)
        {
            _phase2Config = config;
        }
        
        /// <summary>
        /// 直接设置恶作剧值（测试用）。
        /// </summary>
        public void SetMischiefValueForTesting(int value)
        {
            CurrentMischiefValue = value;
        }
        
        /// <summary>
        /// 直接设置动态阈值（测试用）。
        /// </summary>
        public void SetDynamicThresholdForTesting(int threshold)
        {
            _useDynamicThreshold = true;
            _dynamicThreshold = threshold;
        }
        
        /// <summary>
        /// 重置动态阈值（测试用）。
        /// </summary>
        public void ResetDynamicThresholdForTesting()
        {
            _useDynamicThreshold = false;
            _dynamicThreshold = 0;
        }
        
        /// <summary>
        /// 获取当前是否使用动态阈值（测试用）。
        /// </summary>
        public bool IsUsingDynamicThreshold => _useDynamicThreshold;
#endif
        
        #endregion
    }
    
    /// <summary>
    /// 场景中可破坏物体的类型。
    /// </summary>
    public enum DestructibleObjectType
    {
        ShelfItem,
        CleaningCart
    }
}
