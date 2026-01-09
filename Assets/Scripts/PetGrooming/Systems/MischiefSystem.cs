using System;
using UnityEngine;
using PetGrooming.Core;

namespace PetGrooming.Systems
{
    /// <summary>
    /// Manages the mischief value accumulation and threshold detection.
    /// Requirements: 5.1, 5.2, 5.3, 5.5, 6.1, 6.2, 6.6
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
        /// Current accumulated mischief value.
        /// Requirement 5.1: Track cumulative mischief value starting at 0.
        /// </summary>
        public int CurrentMischiefValue { get; private set; }
        
        /// <summary>
        /// Mischief threshold for Pet victory.
        /// Property 15: Mischief Threshold Matches Game Mode
        /// Requirements 6.1, 6.2: Dynamic threshold based on game mode (2-pet: 800, 3-pet: 1000)
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
        /// Mischief points added when a pet skill hits the Groomer.
        /// Requirement 6.6: Pet skill hit adds 30 points.
        /// </summary>
        public int PetSkillHitMischief => _phase2Config != null ? _phase2Config.PetSkillHitMischief : 30;
        
        /// <summary>
        /// Reference to the Phase 2 game configuration.
        /// </summary>
        public Phase2GameConfig Phase2Config => _phase2Config;
        
        /// <summary>
        /// Mischief value for shelf items.
        /// Requirement 5.2: Shelf item adds 50 points.
        /// </summary>
        public int ShelfItemMischief => _gameConfig != null ? _gameConfig.ShelfItemMischief : 50;
        
        /// <summary>
        /// Mischief value for cleaning carts.
        /// Requirement 5.3: Cleaning cart adds 80 points.
        /// </summary>
        public int CleaningCartMischief => _gameConfig != null ? _gameConfig.CleaningCartMischief : 80;
        
        /// <summary>
        /// Reference to the game configuration.
        /// </summary>
        public GameConfig Config => _gameConfig;
        
        #endregion

        #region Events
        
        /// <summary>
        /// Fired when the mischief value changes.
        /// </summary>
        public event Action<int> OnMischiefValueChanged;
        
        /// <summary>
        /// Fired when the mischief threshold is reached.
        /// Requirement 5.5: When mischief value reaches threshold, trigger Pet victory.
        /// </summary>
        public event Action OnThresholdReached;
        
        /// <summary>
        /// Fired when a pet skill hits the Groomer.
        /// Requirement 6.6: Pet skill hit adds 30 points.
        /// </summary>
        public event Action<int> OnPetSkillHitMischief;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            // Singleton setup
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[MischiefSystem] Duplicate MischiefSystem detected, destroying this instance.");
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            
            // Validate configuration
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
        /// Sets the dynamic mischief threshold based on game mode.
        /// Property 15: Mischief Threshold Matches Game Mode
        /// Requirements 6.1, 6.2: 2-pet mode = 800, 3-pet mode = 1000
        /// </summary>
        /// <param name="gameMode">The current game mode</param>
        public void SetDynamicThreshold(PetSpawnManager.GameMode gameMode)
        {
            _useDynamicThreshold = true;
            _dynamicThreshold = GetThresholdForGameMode(gameMode);
            
            Debug.Log($"[MischiefSystem] Dynamic threshold set to {_dynamicThreshold} for {gameMode} mode");
        }
        
        /// <summary>
        /// Sets the dynamic mischief threshold based on pet count.
        /// Property 15: Mischief Threshold Matches Game Mode
        /// Requirements 6.1, 6.2: 2-pet mode = 800, 3-pet mode = 1000
        /// </summary>
        /// <param name="petCount">Number of pets in the match</param>
        public void SetDynamicThresholdByPetCount(int petCount)
        {
            _useDynamicThreshold = true;
            _dynamicThreshold = GetThresholdForPetCount(petCount);
            
            Debug.Log($"[MischiefSystem] Dynamic threshold set to {_dynamicThreshold} for {petCount} pets");
        }
        
        /// <summary>
        /// Adds mischief points to the current value.
        /// Requirements: 5.2, 5.3, 5.5
        /// </summary>
        /// <param name="amount">The amount of mischief to add.</param>
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
            
            // Notify GameManager of mischief change
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnMischiefValueChanged(CurrentMischiefValue);
            }
            
            // Check threshold
            // Requirement 5.5: When mischief value reaches 500 points, Pet wins
            if (previousValue < MischiefThreshold && CurrentMischiefValue >= MischiefThreshold)
            {
                Debug.Log($"[MischiefSystem] Threshold reached! ({CurrentMischiefValue} >= {MischiefThreshold})");
                OnThresholdReached?.Invoke();
            }
        }
        
        /// <summary>
        /// Adds mischief for a shelf item collision.
        /// Requirement 5.2: Shelf item adds 50 points.
        /// </summary>
        public void AddShelfItemMischief()
        {
            AddMischief(ShelfItemMischief);
        }
        
        /// <summary>
        /// Adds mischief for a cleaning cart collision.
        /// Requirement 5.3: Cleaning cart adds 80 points.
        /// </summary>
        public void AddCleaningCartMischief()
        {
            AddMischief(CleaningCartMischief);
        }
        
        /// <summary>
        /// Resets the mischief value to 0.
        /// </summary>
        public void Reset()
        {
            CurrentMischiefValue = 0;
            OnMischiefValueChanged?.Invoke(CurrentMischiefValue);
            Debug.Log("[MischiefSystem] Mischief value reset to 0.");
        }
        
        /// <summary>
        /// Adds mischief when a pet skill hits the Groomer.
        /// Property 18: Pet Skill Hit Mischief Value
        /// Requirement 6.6: Pet skill hit adds 30 points.
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
        /// Gets the mischief threshold for a given game mode.
        /// Property 15: Mischief Threshold Matches Game Mode
        /// Requirements 6.1, 6.2: 2-pet mode = 800, 3-pet mode = 1000
        /// </summary>
        /// <param name="gameMode">The game mode</param>
        /// <returns>The mischief threshold for the game mode</returns>
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
        /// Gets the mischief threshold for a given pet count.
        /// Property 15: Mischief Threshold Matches Game Mode
        /// Requirements 6.1, 6.2: 2-pet mode = 800, 3-pet mode = 1000
        /// </summary>
        /// <param name="petCount">Number of pets in the match</param>
        /// <returns>The mischief threshold for the pet count</returns>
        public static int GetThresholdForPetCount(int petCount)
        {
            return petCount >= 3 ? 1000 : 800;
        }
        
        /// <summary>
        /// Gets the mischief value added when a pet skill hits the Groomer.
        /// Property 18: Pet Skill Hit Mischief Value
        /// Requirement 6.6: Pet skill hit adds 30 points.
        /// </summary>
        /// <returns>The mischief value for pet skill hit (30 points)</returns>
        public static int GetPetSkillHitMischiefValue()
        {
            return 30;
        }
        
        /// <summary>
        /// Calculates the new mischief value after adding an amount.
        /// This method is designed to be testable for property-based testing.
        /// </summary>
        /// <param name="currentValue">Current mischief value.</param>
        /// <param name="amountToAdd">Amount to add.</param>
        /// <returns>The new mischief value.</returns>
        public static int CalculateMischiefValue(int currentValue, int amountToAdd)
        {
            if (amountToAdd <= 0)
            {
                return currentValue;
            }
            return currentValue + amountToAdd;
        }
        
        /// <summary>
        /// Gets the mischief value for a specific object type.
        /// Property 5: Mischief Value Calculation
        /// </summary>
        /// <param name="objectType">Type of destructible object.</param>
        /// <param name="shelfItemValue">Configured shelf item mischief value.</param>
        /// <param name="cleaningCartValue">Configured cleaning cart mischief value.</param>
        /// <returns>The mischief value for the object type.</returns>
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
        /// Checks if the threshold has been reached.
        /// </summary>
        /// <param name="currentValue">Current mischief value.</param>
        /// <param name="threshold">Mischief threshold.</param>
        /// <returns>True if threshold is reached.</returns>
        public static bool IsThresholdReached(int currentValue, int threshold)
        {
            return currentValue >= threshold;
        }
        
        #endregion

        #region Editor Support
        
#if UNITY_EDITOR
        /// <summary>
        /// Sets the game config for testing purposes.
        /// </summary>
        public void SetConfigForTesting(GameConfig config)
        {
            _gameConfig = config;
        }
        
        /// <summary>
        /// Sets the Phase 2 config for testing purposes.
        /// </summary>
        public void SetPhase2ConfigForTesting(Phase2GameConfig config)
        {
            _phase2Config = config;
        }
        
        /// <summary>
        /// Sets the mischief value directly for testing purposes.
        /// </summary>
        public void SetMischiefValueForTesting(int value)
        {
            CurrentMischiefValue = value;
        }
        
        /// <summary>
        /// Sets the dynamic threshold directly for testing purposes.
        /// </summary>
        public void SetDynamicThresholdForTesting(int threshold)
        {
            _useDynamicThreshold = true;
            _dynamicThreshold = threshold;
        }
        
        /// <summary>
        /// Resets the dynamic threshold for testing purposes.
        /// </summary>
        public void ResetDynamicThresholdForTesting()
        {
            _useDynamicThreshold = false;
            _dynamicThreshold = 0;
        }
        
        /// <summary>
        /// Gets whether dynamic threshold is being used (for testing).
        /// </summary>
        public bool IsUsingDynamicThreshold => _useDynamicThreshold;
#endif
        
        #endregion
    }
    
    /// <summary>
    /// Types of destructible objects in the scene.
    /// </summary>
    public enum DestructibleObjectType
    {
        ShelfItem,
        CleaningCart
    }
}
