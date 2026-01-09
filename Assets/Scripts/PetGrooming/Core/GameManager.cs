using System;
using UnityEngine;

namespace PetGrooming.Core
{
    /// <summary>
    /// Core game manager singleton that controls game flow, timing, and victory conditions.
    /// Requirements: 6.1, 6.3, 6.4, 6.5
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
        /// Represents the current state of the game.
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
        /// Current state of the game.
        /// </summary>
        public GameState CurrentState { get; private set; } = GameState.NotStarted;
        
        /// <summary>
        /// Remaining time in the match in seconds.
        /// </summary>
        public float RemainingTime { get; private set; }
        
        /// <summary>
        /// Duration of the match in seconds.
        /// </summary>
        public float MatchDuration => _gameConfig != null ? _gameConfig.MatchDuration : 180f;
        
        /// <summary>
        /// Mischief threshold for Pet victory.
        /// </summary>
        public int MischiefThreshold => _gameConfig != null ? _gameConfig.MischiefThreshold : 500;
        
        /// <summary>
        /// Reference to the game configuration.
        /// </summary>
        public GameConfig Config => _gameConfig;
        
        /// <summary>
        /// Whether the pet has been groomed (legacy single-pet support).
        /// </summary>
        public bool IsPetGroomed { get; private set; }
        
        /// <summary>
        /// Current mischief value (tracked for victory condition checks).
        /// </summary>
        public int CurrentMischiefValue { get; private set; }
        
        /// <summary>
        /// Total number of pets in the match.
        /// </summary>
        public int TotalPetCount { get; private set; }
        
        /// <summary>
        /// Number of pets that have been groomed.
        /// </summary>
        public int GroomedPetCount { get; private set; }
        
        /// <summary>
        /// Whether all pets have been groomed.
        /// Requirement 1.7: Victory condition for multi-pet mode.
        /// </summary>
        public bool AllPetsGroomed => TotalPetCount > 0 && GroomedPetCount >= TotalPetCount;
        
        #endregion

        #region Events
        
        /// <summary>
        /// Fired when the game state changes.
        /// </summary>
        public event Action<GameState> OnGameStateChanged;
        
        /// <summary>
        /// Fired when the remaining time is updated.
        /// </summary>
        public event Action<float> OnTimeUpdated;
        
        /// <summary>
        /// Fired when the match starts.
        /// </summary>
        public event Action OnMatchStarted;
        
        /// <summary>
        /// Fired when the match ends.
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
        /// Starts a new match.
        /// Requirement 6.1: Initialize a 3-minute countdown timer.
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
        /// Ends the match with the specified result.
        /// </summary>
        /// <param name="result">The final game state (GroomerWin or PetWin).</param>
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
        /// Pauses the current match.
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
        /// Resumes a paused match.
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
        /// Called when the pet grooming is completed.
        /// Requirement 6.4: Groomer wins when pet is successfully groomed.
        /// Requirement 1.7: Groomer wins when ALL pets are groomed (multi-pet mode).
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
        /// Sets the total number of pets for the match.
        /// Should be called after spawning pets.
        /// </summary>
        /// <param name="count">Total number of pets</param>
        public void SetTotalPetCount(int count)
        {
            TotalPetCount = count;
            Debug.Log($"[GameManager] Total pet count set to {count}");
        }
        
        /// <summary>
        /// Registers a pet grooming completion.
        /// Requirement 1.7: Track groomed pets for victory condition.
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
        /// Checks if all pets have been groomed and triggers victory if so.
        /// Property 4: All Pets Groomed Victory Condition
        /// Requirement 1.7: When all Pets are groomed, declare Groomer victory.
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
        /// Called when mischief value changes.
        /// Requirement 6.5: Pet wins when mischief threshold is reached.
        /// </summary>
        /// <param name="newMischiefValue">The new mischief value.</param>
        public void OnMischiefValueChanged(int newMischiefValue)
        {
            CurrentMischiefValue = newMischiefValue;
            
            if (CurrentState == GameState.Playing)
            {
                CheckMischiefVictory();
            }
        }
        
        /// <summary>
        /// Resets the game to initial state for a new match.
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
        /// Determines the game state based on current conditions.
        /// This method is designed to be testable for property-based testing.
        /// </summary>
        /// <param name="mischiefValue">Current mischief value.</param>
        /// <param name="mischiefThreshold">Threshold for pet victory.</param>
        /// <param name="remainingTime">Remaining time in seconds.</param>
        /// <param name="isPetGroomed">Whether the pet has been groomed (legacy single-pet).</param>
        /// <returns>The determined game state.</returns>
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
        /// Determines the game state based on multi-pet conditions.
        /// Property 4: All Pets Groomed Victory Condition
        /// Requirement 1.7: When all Pets are groomed, declare Groomer victory.
        /// </summary>
        /// <param name="mischiefValue">Current mischief value.</param>
        /// <param name="mischiefThreshold">Threshold for pet victory.</param>
        /// <param name="remainingTime">Remaining time in seconds.</param>
        /// <param name="totalPets">Total number of pets in the match.</param>
        /// <param name="groomedPets">Number of pets that have been groomed.</param>
        /// <returns>The determined game state.</returns>
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
        /// Checks if all pets are groomed based on counts.
        /// Property 4: All Pets Groomed Victory Condition
        /// Requirement 1.7
        /// </summary>
        /// <param name="totalPets">Total number of pets</param>
        /// <param name="groomedPets">Number of groomed pets</param>
        /// <returns>True if all pets are groomed</returns>
        public static bool AreAllPetsGroomed(int totalPets, int groomedPets)
        {
            return totalPets > 0 && groomedPets >= totalPets;
        }
        
        #endregion

        #region Private Methods
        
        /// <summary>
        /// Updates the match timer.
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
        /// Checks if timer expiry triggers Pet victory.
        /// Requirement 6.3: When timer reaches 0 and Pet is not groomed, Pet wins.
        /// </summary>
        private void CheckTimerVictory()
        {
            if (!IsPetGroomed)
            {
                EndMatch(GameState.PetWin);
            }
        }
        
        /// <summary>
        /// Checks if mischief threshold triggers Pet victory.
        /// Requirement 6.5: When mischief value reaches threshold, Pet wins immediately.
        /// </summary>
        private void CheckMischiefVictory()
        {
            if (CurrentMischiefValue >= MischiefThreshold)
            {
                EndMatch(GameState.PetWin);
            }
        }
        
        /// <summary>
        /// Sets the game state and fires the state changed event.
        /// </summary>
        /// <param name="newState">The new game state.</param>
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
        /// Sets the game config for testing purposes.
        /// </summary>
        public void SetConfigForTesting(GameConfig config)
        {
            _gameConfig = config;
        }
        
        /// <summary>
        /// Sets the total pet count for testing purposes.
        /// </summary>
        public void SetTotalPetCountForTesting(int count)
        {
            TotalPetCount = count;
        }
        
        /// <summary>
        /// Sets the groomed pet count for testing purposes.
        /// </summary>
        public void SetGroomedPetCountForTesting(int count)
        {
            GroomedPetCount = count;
        }
        
        /// <summary>
        /// Sets the current state for testing purposes.
        /// </summary>
        public void SetStateForTesting(GameState state)
        {
            CurrentState = state;
        }
#endif
        
        #endregion
    }
}
