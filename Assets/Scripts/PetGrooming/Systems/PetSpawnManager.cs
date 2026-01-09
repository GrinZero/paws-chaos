using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PetGrooming.Core;
using PetGrooming.AI;

namespace PetGrooming.Systems
{
    /// <summary>
    /// Manages multi-pet spawning and tracking for Phase 2.
    /// Requirements: 1.1, 1.2
    /// </summary>
    public class PetSpawnManager : MonoBehaviour
    {
        #region Singleton
        
        private static PetSpawnManager _instance;
        
        public static PetSpawnManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<PetSpawnManager>();
                    if (_instance == null)
                    {
                        Debug.LogError("[PetSpawnManager] No PetSpawnManager instance found in scene!");
                    }
                }
                return _instance;
            }
        }
        
        #endregion

        #region Enums
        
        /// <summary>
        /// Game mode determining the number of pets to spawn.
        /// Requirements: 1.1, 1.2
        /// </summary>
        public enum GameMode
        {
            TwoPets = 2,
            ThreePets = 3
        }
        
        #endregion

        #region Serialized Fields
        
        [Header("Configuration")]
        [SerializeField] private Phase2GameConfig _phase2Config;
        
        [Header("Spawn Settings")]
        [Tooltip("Current game mode determining pet count")]
        [SerializeField] private GameMode _currentMode = GameMode.TwoPets;
        
        [Tooltip("Spawn points for pets")]
        [SerializeField] private Transform[] _spawnPoints;
        
        [Header("Pet Prefabs")]
        [Tooltip("Prefab for Cat pets")]
        [SerializeField] private GameObject _catPrefab;
        
        [Tooltip("Prefab for Dog pets")]
        [SerializeField] private GameObject _dogPrefab;
        
        [Header("Play Area Bounds")]
        [SerializeField] private Vector3 _playAreaMin = new Vector3(-20f, 0f, -20f);
        [SerializeField] private Vector3 _playAreaMax = new Vector3(20f, 0f, 20f);
        
        #endregion

        #region Private Fields
        
        private List<PetAI> _activePets = new List<PetAI>();
        
        #endregion

        #region Properties
        
        /// <summary>
        /// Current game mode.
        /// </summary>
        public GameMode CurrentMode => _currentMode;
        
        /// <summary>
        /// List of all active pets in the scene.
        /// </summary>
        public List<PetAI> ActivePets => _activePets;
        
        /// <summary>
        /// Number of pets that have not been groomed yet.
        /// </summary>
        public int RemainingPets => _activePets.Count(p => p != null && !p.IsGroomed);
        
        /// <summary>
        /// Total number of pets spawned.
        /// </summary>
        public int TotalPets => _activePets.Count;
        
        /// <summary>
        /// Number of pets that have been groomed.
        /// </summary>
        public int GroomedPets => _activePets.Count(p => p != null && p.IsGroomed);
        
        /// <summary>
        /// Whether all pets have been groomed.
        /// Requirement 1.7: Victory condition check.
        /// </summary>
        public bool AllPetsGroomed => _activePets.Count > 0 && _activePets.All(p => p != null && p.IsGroomed);
        
        /// <summary>
        /// Reference to the Phase 2 configuration.
        /// </summary>
        public Phase2GameConfig Phase2Config => _phase2Config;
        
        /// <summary>
        /// Play area minimum bounds.
        /// </summary>
        public Vector3 PlayAreaMin => _playAreaMin;
        
        /// <summary>
        /// Play area maximum bounds.
        /// </summary>
        public Vector3 PlayAreaMax => _playAreaMax;
        
        #endregion

        #region Events
        
        /// <summary>
        /// Fired when a pet is spawned.
        /// </summary>
        public event Action<PetAI> OnPetSpawned;
        
        /// <summary>
        /// Fired when a pet completes grooming.
        /// </summary>
        public event Action<PetAI> OnPetGroomed;
        
        /// <summary>
        /// Fired when all pets have been groomed.
        /// Requirement 1.7: Victory condition.
        /// </summary>
        public event Action OnAllPetsGroomed;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            // Singleton setup
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[PetSpawnManager] Duplicate PetSpawnManager detected, destroying this instance.");
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            
            if (_phase2Config == null)
            {
                Debug.LogWarning("[PetSpawnManager] Phase2GameConfig is not assigned.");
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
        /// Spawns pets based on the current game mode.
        /// Requirements: 1.1, 1.2
        /// </summary>
        public void SpawnPets()
        {
            // Clear any existing pets
            ClearPets();
            
            int petCount = GetPetCountForMode(_currentMode);
            
            Debug.Log($"[PetSpawnManager] Spawning {petCount} pets for {_currentMode} mode");
            
            // Set dynamic mischief threshold based on game mode
            // Property 15: Mischief Threshold Matches Game Mode
            // Requirements 6.1, 6.2: 2-pet mode = 800, 3-pet mode = 1000
            if (MischiefSystem.Instance != null)
            {
                MischiefSystem.Instance.SetDynamicThreshold(_currentMode);
            }
            
            for (int i = 0; i < petCount; i++)
            {
                SpawnPet(i);
            }
            
            // Set total pet count in GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetTotalPetCount(petCount);
            }
        }
        
        /// <summary>
        /// Sets the game mode and optionally respawns pets.
        /// </summary>
        /// <param name="mode">The new game mode</param>
        /// <param name="respawn">Whether to respawn pets immediately</param>
        public void SetGameMode(GameMode mode, bool respawn = false)
        {
            _currentMode = mode;
            
            if (respawn)
            {
                SpawnPets();
            }
            
            Debug.Log($"[PetSpawnManager] Game mode set to {mode}");
        }
        
        /// <summary>
        /// Called when a pet completes grooming.
        /// Requirement 1.7: Check for all pets groomed victory condition.
        /// </summary>
        /// <param name="pet">The pet that completed grooming</param>
        public void OnPetGroomingComplete(PetAI pet)
        {
            if (pet == null) return;
            
            pet.MarkAsGroomed();
            OnPetGroomed?.Invoke(pet);
            
            Debug.Log($"[PetSpawnManager] Pet groomed. Remaining: {RemainingPets}/{TotalPets}");
            
            // Check victory condition
            if (AllPetsGroomed)
            {
                Debug.Log("[PetSpawnManager] All pets groomed! Triggering victory.");
                OnAllPetsGroomed?.Invoke();
            }
        }
        
        /// <summary>
        /// Gets the nearest pet to a position that is not groomed or captured.
        /// </summary>
        /// <param name="position">Position to check from</param>
        /// <returns>The nearest available pet, or null if none found</returns>
        public PetAI GetNearestPet(Vector3 position)
        {
            PetAI nearest = null;
            float nearestDistance = float.MaxValue;
            
            foreach (var pet in _activePets)
            {
                if (pet == null || pet.IsGroomed) continue;
                if (pet.CurrentState == PetAI.PetState.Captured || 
                    pet.CurrentState == PetAI.PetState.BeingGroomed) continue;
                
                float distance = Vector3.Distance(position, pet.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = pet;
                }
            }
            
            return nearest;
        }
        
        /// <summary>
        /// Gets all pets that are not groomed.
        /// </summary>
        /// <returns>List of ungroomed pets</returns>
        public List<PetAI> GetUngroomedPets()
        {
            return _activePets.Where(p => p != null && !p.IsGroomed).ToList();
        }
        
        /// <summary>
        /// Clears all spawned pets.
        /// </summary>
        public void ClearPets()
        {
            foreach (var pet in _activePets)
            {
                if (pet != null)
                {
                    Destroy(pet.gameObject);
                }
            }
            _activePets.Clear();
        }
        
        #endregion

        #region Private Methods
        
        /// <summary>
        /// Spawns a single pet at the specified index.
        /// </summary>
        private void SpawnPet(int index)
        {
            // Determine pet type (alternate between cats and dogs)
            bool isCat = index % 2 == 0;
            GameObject prefab = isCat ? _catPrefab : _dogPrefab;
            
            if (prefab == null)
            {
                Debug.LogWarning($"[PetSpawnManager] {(isCat ? "Cat" : "Dog")} prefab is not assigned!");
                return;
            }
            
            // Get spawn position
            Vector3 spawnPosition = GetSpawnPosition(index);
            
            // Instantiate pet
            GameObject petObj = Instantiate(prefab, spawnPosition, Quaternion.identity);
            petObj.name = $"Pet_{(isCat ? "Cat" : "Dog")}_{index}";
            
            PetAI petAI = petObj.GetComponent<PetAI>();
            if (petAI != null)
            {
                _activePets.Add(petAI);
                
                // Subscribe to grooming complete event
                petAI.OnGroomingComplete += () => OnPetGroomingComplete(petAI);
                
                OnPetSpawned?.Invoke(petAI);
                
                Debug.Log($"[PetSpawnManager] Spawned {petObj.name} at {spawnPosition}");
            }
            else
            {
                Debug.LogError($"[PetSpawnManager] Spawned pet prefab does not have PetAI component!");
                Destroy(petObj);
            }
        }
        
        /// <summary>
        /// Gets the spawn position for a pet at the specified index.
        /// </summary>
        private Vector3 GetSpawnPosition(int index)
        {
            // Use spawn points if available
            if (_spawnPoints != null && _spawnPoints.Length > 0)
            {
                int spawnIndex = index % _spawnPoints.Length;
                if (_spawnPoints[spawnIndex] != null)
                {
                    return _spawnPoints[spawnIndex].position;
                }
            }
            
            // Generate random position within bounds
            return GenerateRandomSpawnPosition();
        }
        
        /// <summary>
        /// Generates a random spawn position within the play area bounds.
        /// </summary>
        private Vector3 GenerateRandomSpawnPosition()
        {
            return GenerateRandomPositionInBounds(_playAreaMin, _playAreaMax);
        }
        
        #endregion

        #region Static Methods (Testable)
        
        /// <summary>
        /// Gets the number of pets for a given game mode.
        /// Property 1: Pet Spawn Count Matches Game Mode
        /// Requirements: 1.1, 1.2
        /// </summary>
        /// <param name="mode">The game mode</param>
        /// <returns>Number of pets to spawn</returns>
        public static int GetPetCountForMode(GameMode mode)
        {
            return (int)mode;
        }
        
        /// <summary>
        /// Generates a random position within the specified bounds.
        /// </summary>
        /// <param name="min">Minimum bounds</param>
        /// <param name="max">Maximum bounds</param>
        /// <returns>Random position within bounds</returns>
        public static Vector3 GenerateRandomPositionInBounds(Vector3 min, Vector3 max)
        {
            return new Vector3(
                UnityEngine.Random.Range(min.x, max.x),
                (min.y + max.y) / 2f,
                UnityEngine.Random.Range(min.z, max.z)
            );
        }
        
        /// <summary>
        /// Checks if a position is within the specified bounds.
        /// </summary>
        /// <param name="position">Position to check</param>
        /// <param name="min">Minimum bounds</param>
        /// <param name="max">Maximum bounds</param>
        /// <returns>True if position is within bounds</returns>
        public static bool IsPositionInBounds(Vector3 position, Vector3 min, Vector3 max)
        {
            return position.x >= min.x && position.x <= max.x &&
                   position.z >= min.z && position.z <= max.z;
        }
        
        /// <summary>
        /// Checks if all pets in a list are groomed.
        /// Property 4: All Pets Groomed Victory Condition
        /// Requirement 1.7
        /// </summary>
        /// <param name="pets">List of pets to check</param>
        /// <returns>True if all pets are groomed</returns>
        public static bool AreAllPetsGroomed(IEnumerable<PetAI> pets)
        {
            if (pets == null) return false;
            
            var petList = pets.ToList();
            if (petList.Count == 0) return false;
            
            return petList.All(p => p != null && p.IsGroomed);
        }
        
        #endregion

        #region Editor Support
        
#if UNITY_EDITOR
        /// <summary>
        /// Sets the game mode for testing purposes.
        /// </summary>
        public void SetGameModeForTesting(GameMode mode)
        {
            _currentMode = mode;
        }
        
        /// <summary>
        /// Sets the Phase 2 config for testing purposes.
        /// </summary>
        public void SetPhase2ConfigForTesting(Phase2GameConfig config)
        {
            _phase2Config = config;
        }
        
        /// <summary>
        /// Sets the play area bounds for testing.
        /// </summary>
        public void SetPlayAreaBoundsForTesting(Vector3 min, Vector3 max)
        {
            _playAreaMin = min;
            _playAreaMax = max;
        }
        
        /// <summary>
        /// Adds a pet to the active pets list for testing.
        /// </summary>
        public void AddPetForTesting(PetAI pet)
        {
            if (pet != null && !_activePets.Contains(pet))
            {
                _activePets.Add(pet);
            }
        }
        
        /// <summary>
        /// Clears active pets for testing.
        /// </summary>
        public void ClearPetsForTesting()
        {
            _activePets.Clear();
        }
#endif
        
        private void OnDrawGizmosSelected()
        {
            // Draw play area bounds
            Gizmos.color = Color.cyan;
            Vector3 center = (_playAreaMin + _playAreaMax) / 2f;
            Vector3 size = _playAreaMax - _playAreaMin;
            Gizmos.DrawWireCube(center, size);
            
            // Draw spawn points
            if (_spawnPoints != null)
            {
                Gizmos.color = Color.green;
                foreach (var point in _spawnPoints)
                {
                    if (point != null)
                    {
                        Gizmos.DrawWireSphere(point.position, 0.5f);
                    }
                }
            }
        }
        
        #endregion
    }
}
