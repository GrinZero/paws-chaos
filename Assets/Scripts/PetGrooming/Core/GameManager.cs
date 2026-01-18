using System;
using UnityEngine;

namespace PetGrooming.Core
{
    /// <summary>
    /// 控制游戏流程、计时和胜利条件的核心游戏管理器单例。
    /// 需求：6.1, 6.3, 6.4, 6.5
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region Singleton
        
        private static GameManager _instance;
        
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GameManager>();
                    if (_instance == null)
                    {
                        Debug.LogError("[GameManager] No GameManager instance found in scene!");
                    }
                }
                return _instance;
            }
        }
        
        #endregion

        #region Enums
        
        /// <summary>
        /// 表示游戏的当前状态。
        /// </summary>
        public enum GameState
        {
            NotStarted,
            Playing,
            Paused,
            GroomerWin,
            PetWin
        }
        
        #endregion

        #region Serialized Fields
        
        [Header("Configuration")]
        [SerializeField] private GameConfig _gameConfig;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// 游戏的当前状态。
        /// </summary>
        public GameState CurrentState { get; private set; } = GameState.NotStarted;
        
        /// <summary>
        /// 比赛中剩余的时间（秒）。
        /// </summary>
        public float RemainingTime { get; private set; }
        
        /// <summary>
        /// 比赛的持续时间（秒）。
        /// </summary>
        public float MatchDuration => _gameConfig != null ? _gameConfig.MatchDuration : 180f;
        
        /// <summary>
        /// 宠物胜利的恶作剧阈值。
        /// </summary>
        public int MischiefThreshold => _gameConfig != null ? _gameConfig.MischiefThreshold : 500;
        
        /// <summary>
        /// 对游戏配置的引用。
        /// </summary>
        public GameConfig Config => _gameConfig;
        
        /// <summary>
        /// 宠物是否已被梳理（遗留的单宠物支持）。
        /// </summary>
        public bool IsPetGroomed { get; private set; }
        
        /// <summary>
        /// 当前恶作剧值（用于胜利条件检查）。
        /// </summary>
        public int CurrentMischiefValue { get; private set; }
        
        /// <summary>
        /// 比赛中宠物的总数。
        /// </summary>
        public int TotalPetCount { get; private set; }
        
        /// <summary>
        /// 已被梳理的宠物数量。
        /// </summary>
        public int GroomedPetCount { get; private set; }
        
        /// <summary>
        /// 是否所有宠物都已被梳理。
        /// 需求 1.7：多宠物模式的胜利条件。
        /// </summary>
        public bool AllPetsGroomed => TotalPetCount > 0 && GroomedPetCount >= TotalPetCount;
        
        #endregion

        #region Events
        
        /// <summary>
        /// 当游戏状态改变时触发。
        /// </summary>
        public event Action<GameState> OnGameStateChanged;
        
        /// <summary>
        /// 当剩余时间更新时触发。
        /// </summary>
        public event Action<float> OnTimeUpdated;
        
        /// <summary>
        /// 当比赛开始时触发。
        /// </summary>
        public event Action OnMatchStarted;
        
        /// <summary>
        /// 当比赛结束时触发。
        /// </summary>
        public event Action<GameState> OnMatchEnded;
        
        #endregion

        #region Unity Lifecycle
        
        [Header("Auto Start")]
        [SerializeField] private bool _autoStartOnPlay = true;
        
        private void Awake()
        {
            // Singleton setup
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[GameManager] Duplicate GameManager detected, destroying this instance.");
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            
            // Validate configuration
            if (_gameConfig == null)
            {
                Debug.LogWarning("[GameManager] GameConfig is not assigned, using default values.");
            }
        }
        
        private void Start()
        {
            // Auto start the match when the game begins
            if (_autoStartOnPlay)
            {
                StartMatch();
            }
        }
        
        private void Update()
        {
            if (CurrentState == GameState.Playing)
            {
                UpdateTimer();
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
        /// 开始新的比赛。
        /// 需求 6.1：初始化 3 分钟倒计时器。
        /// </summary>
        public void StartMatch()
        {
            if (CurrentState == GameState.Playing)
            {
                Debug.LogWarning("[GameManager] Match already in progress.");
                return;
            }
            
            // Reset state
            RemainingTime = MatchDuration;
            IsPetGroomed = false;
            CurrentMischiefValue = 0;
            TotalPetCount = 0;
            GroomedPetCount = 0;
            
            // Change state
            SetGameState(GameState.Playing);
            
            OnMatchStarted?.Invoke();
            OnTimeUpdated?.Invoke(RemainingTime);
            
            Debug.Log($"[GameManager] Match started. Duration: {MatchDuration}s");
        }
        
        /// <summary>
        /// 以指定的结果结束比赛。
        /// </summary>
        /// <param name="result">最终游戏状态（GroomerWin 或 PetWin）。</param>
        public void EndMatch(GameState result)
        {
            if (CurrentState == GameState.GroomerWin || CurrentState == GameState.PetWin)
            {
                Debug.LogWarning("[GameManager] Match already ended.");
                return;
            }
            
            if (result != GameState.GroomerWin && result != GameState.PetWin)
            {
                Debug.LogError("[GameManager] Invalid end state. Must be GroomerWin or PetWin.");
                return;
            }
            
            SetGameState(result);
            OnMatchEnded?.Invoke(result);
            
            Debug.Log($"[GameManager] Match ended. Result: {result}");
        }
        
        /// <summary>
        /// 暂停当前比赛。
        /// </summary>
        public void PauseMatch()
        {
            if (CurrentState != GameState.Playing)
            {
                Debug.LogWarning("[GameManager] Cannot pause - match is not playing.");
                return;
            }
            
            SetGameState(GameState.Paused);
            Time.timeScale = 0f;
            
            Debug.Log("[GameManager] Match paused.");
        }
        
        /// <summary>
        /// 恢复暂停的比赛。
        /// </summary>
        public void ResumeMatch()
        {
            if (CurrentState != GameState.Paused)
            {
                Debug.LogWarning("[GameManager] Cannot resume - match is not paused.");
                return;
            }
            
            Time.timeScale = 1f;
            SetGameState(GameState.Playing);
            
            Debug.Log("[GameManager] Match resumed.");
        }
        
        /// <summary>
        /// 当宠物梳理完成时调用。
        /// 需求 6.4：当宠物成功梳理时，Groomer 获胜。
        /// 需求 1.7：当所有宠物都被梳理时，Groomer 获胜（多宠物模式）。
        /// </summary>
        public void OnPetGroomingComplete()
        {
            if (CurrentState != GameState.Playing)
            {
                return;
            }
            
            GroomedPetCount++;
            
            Debug.Log($"[GameManager] Pet groomed. Progress: {GroomedPetCount}/{TotalPetCount}");
            
            // Check multi-pet victory condition
            // Property 4: All Pets Groomed Victory Condition
            // Requirement 1.7: When all Pets are groomed, declare Groomer victory
            if (AllPetsGroomed)
            {
                IsPetGroomed = true;
                EndMatch(GameState.GroomerWin);
            }
            // Legacy single-pet mode support
            else if (TotalPetCount == 0)
            {
                IsPetGroomed = true;
                EndMatch(GameState.GroomerWin);
            }
        }
        
        /// <summary>
        /// 设置比赛中宠物的总数。
        /// 应在生成宠物后调用。
        /// </summary>
        /// <param name="count">宠物总数</param>
        public void SetTotalPetCount(int count)
        {
            TotalPetCount = count;
            Debug.Log($"[GameManager] Total pet count set to {count}");
        }
        
        /// <summary>
        /// 注册宠物梳理完成。
        /// 需求 1.7：跟踪已梳理的宠物以满足胜利条件。
        /// </summary>
        public void RegisterPetGroomed()
        {
            if (CurrentState != GameState.Playing)
            {
                return;
            }
            
            GroomedPetCount++;
            
            Debug.Log($"[GameManager] Pet groomed. Progress: {GroomedPetCount}/{TotalPetCount}");
            
            // Check victory condition
            CheckAllPetsGroomedVictory();
        }
        
        /// <summary>
        /// 检查是否所有宠物都已被梳理，如果是则触发胜利。
        /// 属性 4：所有宠物梳理胜利条件
        /// 需求 1.7：当所有宠物都被梳理时，宣布 Groomer 胜利。
        /// </summary>
        private void CheckAllPetsGroomedVictory()
        {
            if (AllPetsGroomed)
            {
                IsPetGroomed = true;
                EndMatch(GameState.GroomerWin);
            }
        }
        
        /// <summary>
        /// 当恶作剧值改变时调用。
        /// 需求 6.5：当恶作剧阈值达到时，宠物获胜。
        /// </summary>
        /// <param name="newMischiefValue">新的恶作剧值。</param>
        public void OnMischiefValueChanged(int newMischiefValue)
        {
            CurrentMischiefValue = newMischiefValue;
            
            if (CurrentState == GameState.Playing)
            {
                CheckMischiefVictory();
            }
        }
        
        /// <summary>
        /// 将游戏重置为初始状态以开始新的比赛。
        /// </summary>
        public void ResetGame()
        {
            Time.timeScale = 1f;
            RemainingTime = MatchDuration;
            IsPetGroomed = false;
            CurrentMischiefValue = 0;
            TotalPetCount = 0;
            GroomedPetCount = 0;
            SetGameState(GameState.NotStarted);
            
            Debug.Log("[GameManager] Game reset.");
        }
        
        #endregion

        #region Victory Condition Methods (Testable)
        
        /// <summary>
        /// 根据当前条件确定游戏状态。
        /// 此方法设计为可测试的，用于基于属性的测试。
        /// </summary>
        /// <param name="mischiefValue">当前恶作剧值。</param>
        /// <param name="mischiefThreshold">宠物胜利的阈值。</param>
        /// <param name="remainingTime">剩余时间（秒）。</param>
        /// <param name="isPetGroomed">宠物是否已被梳理（遗留的单宠物）。</param>
        /// <returns>确定的游戏状态。</returns>
        public static GameState DetermineVictoryCondition(
            int mischiefValue, 
            int mischiefThreshold, 
            float remainingTime, 
            bool isPetGroomed)
        {
            // Property 12: Grooming complete → GroomerWin
            // Requirement 6.4: When the Pet is successfully groomed, declare Groomer victory
            if (isPetGroomed)
            {
                return GameState.GroomerWin;
            }
            
            // Property 10: Mischief threshold reached → PetWin
            // Requirement 6.5: When mischief value reaches threshold, declare Pet victory immediately
            // Note: Mischief threshold takes priority over timer (per design doc edge cases)
            if (mischiefValue >= mischiefThreshold)
            {
                return GameState.PetWin;
            }
            
            // Property 11: Timer expired and pet not groomed → PetWin
            // Requirement 6.3: When timer reaches 0 and Pet is not groomed, declare Pet victory
            if (remainingTime <= 0f && !isPetGroomed)
            {
                return GameState.PetWin;
            }
            
            // No victory condition met, game continues
            return GameState.Playing;
        }
        
        /// <summary>
        /// 根据多宠物条件确定游戏状态。
        /// 属性 4：所有宠物梳理胜利条件
        /// 需求 1.7：当所有宠物都被梳理时，宣布 Groomer 胜利。
        /// </summary>
        /// <param name="mischiefValue">当前恶作剧值。</param>
        /// <param name="mischiefThreshold">宠物胜利的阈值。</param>
        /// <param name="remainingTime">剩余时间（秒）。</param>
        /// <param name="totalPets">比赛中宠物的总数。</param>
        /// <param name="groomedPets">已被梳理的宠物数量。</param>
        /// <returns>确定的游戏状态。</returns>
        public static GameState DetermineMultiPetVictoryCondition(
            int mischiefValue, 
            int mischiefThreshold, 
            float remainingTime, 
            int totalPets,
            int groomedPets)
        {
            // Property 4: All Pets Groomed Victory Condition
            // Requirement 1.7: When all Pets are groomed, declare Groomer victory
            bool allPetsGroomed = totalPets > 0 && groomedPets >= totalPets;
            if (allPetsGroomed)
            {
                return GameState.GroomerWin;
            }
            
            // Property 10: Mischief threshold reached → PetWin
            // Requirement 6.5: When mischief value reaches threshold, declare Pet victory immediately
            if (mischiefValue >= mischiefThreshold)
            {
                return GameState.PetWin;
            }
            
            // Property 11: Timer expired and not all pets groomed → PetWin
            // Requirement 6.3: When timer reaches 0 and not all Pets are groomed, declare Pet victory
            if (remainingTime <= 0f && !allPetsGroomed)
            {
                return GameState.PetWin;
            }
            
            // No victory condition met, game continues
            return GameState.Playing;
        }
        
        /// <summary>
        /// 根据计数检查是否所有宠物都已被梳理。
        /// 属性 4：所有宠物梳理胜利条件
        /// 需求 1.7
        /// </summary>
        /// <param name="totalPets">宠物总数</param>
        /// <param name="groomedPets">已梳理的宠物数量</param>
        /// <returns>如果所有宠物都已被梳理则返回 True</returns>
        public static bool AreAllPetsGroomed(int totalPets, int groomedPets)
        {
            return totalPets > 0 && groomedPets >= totalPets;
        }
        
        #endregion

        #region Private Methods
        
        /// <summary>
        /// 更新比赛计时器。
        /// </summary>
        private void UpdateTimer()
        {
            RemainingTime -= Time.deltaTime;
            OnTimeUpdated?.Invoke(RemainingTime);
            
            // Requirement 6.3: Timer expiry check
            if (RemainingTime <= 0f)
            {
                RemainingTime = 0f;
                CheckTimerVictory();
            }
        }
        
        /// <summary>
        /// 检查计时器到期是否触发宠物胜利。
        /// 需求 6.3：当计时器达到 0 且宠物未被梳理时，宠物获胜。
        /// </summary>
        private void CheckTimerVictory()
        {
            if (!IsPetGroomed)
            {
                EndMatch(GameState.PetWin);
            }
        }
        
        /// <summary>
        /// 检查恶作剧阈值是否触发宠物胜利。
        /// 需求 6.5：当恶作剧值达到阈值时，宠物立即获胜。
        /// </summary>
        private void CheckMischiefVictory()
        {
            if (CurrentMischiefValue >= MischiefThreshold)
            {
                EndMatch(GameState.PetWin);
            }
        }
        
        /// <summary>
        /// 设置游戏状态并触发状态改变事件。
        /// </summary>
        /// <param name="newState">新的游戏状态。</param>
        private void SetGameState(GameState newState)
        {
            if (CurrentState == newState)
            {
                return;
            }
            
            var previousState = CurrentState;
            CurrentState = newState;
            
            Debug.Log($"[GameManager] State changed: {previousState} → {newState}");
            OnGameStateChanged?.Invoke(newState);
        }
        
        #endregion

        #region Editor Support
        
#if UNITY_EDITOR
        /// <summary>
        /// 为测试目的设置游戏配置。
        /// </summary>
        public void SetConfigForTesting(GameConfig config)
        {
            _gameConfig = config;
        }
        
        /// <summary>
        /// 为测试目的设置宠物总数。
        /// </summary>
        public void SetTotalPetCountForTesting(int count)
        {
            TotalPetCount = count;
        }
        
        /// <summary>
        /// 为测试目的设置已梳理的宠物数量。
        /// </summary>
        public void SetGroomedPetCountForTesting(int count)
        {
            GroomedPetCount = count;
        }
        
        /// <summary>
        /// 为测试目的设置当前状态。
        /// </summary>
        public void SetStateForTesting(GameState state)
        {
            CurrentState = state;
        }
#endif
        
        #endregion
    }
}
