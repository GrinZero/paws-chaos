using System;
using UnityEngine;
using PetGrooming.AI;
using PetGrooming.Core;

namespace PetGrooming.Systems
{
    /// <summary>
    /// Pet cage system for temporarily storing captured pets.
    /// Requirements: 1.4, 1.5, 1.6, 8.1, 8.2, 8.3, 8.4, 8.5, 8.6
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
        /// Whether the cage currently contains a pet.
        /// Requirement 8.1: Visual indicator showing whether cage is empty or occupied.
        /// </summary>
        public bool IsOccupied { get; private set; }
        
        /// <summary>
        /// The pet currently stored in the cage.
        /// </summary>
        public PetAI StoredPet { get; private set; }
        
        /// <summary>
        /// Remaining time before automatic release.
        /// Requirement 8.2: Display countdown timer.
        /// </summary>
        public float RemainingTime => Mathf.Max(0f, MaxStorageTime - _currentStorageTime);
        
        /// <summary>
        /// Maximum storage time from config or default.
        /// Requirement 1.5: Pet stored for up to 60 seconds.
        /// </summary>
        public float MaxStorageTime => _phase2Config != null ? _phase2Config.CageStorageTime : _maxStorageTime;
        
        /// <summary>
        /// Warning time threshold from config or default.
        /// Requirement 8.3: Warning at 10 seconds remaining.
        /// </summary>
        public float WarningTime => _phase2Config != null ? _phase2Config.CageWarningTime : _warningTime;
        
        /// <summary>
        /// Invulnerability duration from config or default.
        /// Requirement 8.4: 3 seconds invulnerability on release.
        /// </summary>
        public float ReleaseInvulnerabilityDuration => _phase2Config != null ? _phase2Config.ReleaseInvulnerabilityTime : _releaseInvulnerabilityTime;
        
        /// <summary>
        /// Distance within which groomer can interact.
        /// </summary>
        public float InteractionRange => _interactionRange;
        
        /// <summary>
        /// Whether the warning state is currently active.
        /// </summary>
        public bool IsWarningActive => _isWarningActive;
        
        /// <summary>
        /// Reference to the Phase 2 configuration.
        /// </summary>
        public Phase2GameConfig Phase2Config => _phase2Config;
        
        #endregion

        #region Events
        
        /// <summary>
        /// Fired when a pet is stored in the cage.
        /// </summary>
        public event Action<PetAI> OnPetStored;
        
        /// <summary>
        /// Fired when a pet is released from the cage (automatic or manual).
        /// </summary>
        public event Action<PetAI> OnPetReleased;
        
        /// <summary>
        /// Fired when the warning state begins (10 seconds remaining).
        /// Requirement 8.3: Warning indicator at 10 seconds.
        /// </summary>
        public event Action OnWarningStarted;
        
        /// <summary>
        /// Fired when the remaining time changes (for UI updates).
        /// </summary>
        public event Action<float> OnRemainingTimeChanged;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            ValidateReferences();
            
            // Cache material for color changes
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
        /// Checks if a pet can be stored in this cage.
        /// </summary>
        /// <returns>True if the cage is empty and can accept a pet.</returns>
        public bool CanStorePet()
        {
            return !IsOccupied;
        }
        
        /// <summary>
        /// Stores a pet in the cage.
        /// Requirements: 1.4, 1.5, 8.1, 8.6
        /// </summary>
        /// <param name="pet">The pet to store.</param>
        /// <returns>True if storage was successful.</returns>
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
            
            // Store the pet
            StoredPet = pet;
            IsOccupied = true;
            _currentStorageTime = 0f;
            _isWarningActive = false;
            
            // Position the pet in the cage
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
            
            // Disable pet AI while caged
            pet.SetState(PetAI.PetState.Captured);
            
            // Requirement 8.6: Mark pet as caged so it doesn't contribute to mischief
            pet.SetCaged(true);
            
            UpdateVisualState();
            
            OnPetStored?.Invoke(pet);
            
            Debug.Log($"[PetCage] Pet stored: {pet.name}. Timer started for {MaxStorageTime} seconds.");
            
            return true;
        }
        
        /// <summary>
        /// Releases the pet from the cage (automatic release after timer expires).
        /// Requirements: 1.6, 8.4
        /// </summary>
        public void ReleasePet()
        {
            ReleasePetInternal(isManual: false);
        }
        
        /// <summary>
        /// Manually releases the pet from the cage.
        /// Requirement 8.5: Groomer can manually release pet.
        /// </summary>
        public void ManualRelease()
        {
            ReleasePetInternal(isManual: true);
        }
        
        /// <summary>
        /// Checks if the groomer is within interaction range.
        /// </summary>
        /// <param name="groomerPosition">Position of the groomer.</param>
        /// <returns>True if groomer is in range.</returns>
        public bool IsGroomerInRange(Vector3 groomerPosition)
        {
            return IsWithinRange(groomerPosition, transform.position, _interactionRange);
        }
        
        /// <summary>
        /// Checks if manual release can be performed.
        /// Requirement 8.5: Groomer can interact with cage to release pet.
        /// </summary>
        /// <param name="groomerPosition">Position of the groomer.</param>
        /// <returns>True if manual release is possible.</returns>
        public bool CanManualRelease(Vector3 groomerPosition)
        {
            return IsOccupied && StoredPet != null && IsGroomerInRange(groomerPosition);
        }
        
        /// <summary>
        /// Gets the release position in world space.
        /// </summary>
        /// <returns>World position where pet will be released.</returns>
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
                Debug.LogWarning("[PetCage] Storage position not assigned, creating default.");
                var storagePosObj = new GameObject("StoragePosition");
                storagePosObj.transform.SetParent(transform);
                storagePosObj.transform.localPosition = Vector3.zero;
                _storagePosition = storagePosObj.transform;
            }
            
            if (_releasePosition == null)
            {
                Debug.LogWarning("[PetCage] Release position not assigned, creating default.");
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
            
            // Notify of time change
            OnRemainingTimeChanged?.Invoke(RemainingTime);
            
            // Check for warning state
            // Requirement 8.3: Warning at 10 seconds remaining
            if (!_isWarningActive && RemainingTime <= WarningTime)
            {
                _isWarningActive = true;
                UpdateVisualState();
                OnWarningStarted?.Invoke();
                Debug.Log($"[PetCage] Warning! {RemainingTime:F1} seconds remaining.");
            }
            
            // Check for automatic release
            // Requirement 1.6: Auto-release after 60 seconds
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
            
            // Clear cage state
            StoredPet = null;
            IsOccupied = false;
            _currentStorageTime = 0f;
            _isWarningActive = false;
            
            // Unparent and position the pet
            releasedPet.transform.SetParent(null);
            releasedPet.transform.position = GetReleaseWorldPosition();
            
            // Requirement 8.6: Clear caged state so pet can contribute to mischief again
            releasedPet.SetCaged(false);
            
            // Requirement 8.4: Apply invulnerability on release
            releasedPet.SetInvulnerable(ReleaseInvulnerabilityDuration);
            
            // Return pet to idle state
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
        /// Checks if a position is within range of a target.
        /// </summary>
        /// <param name="position">Position to check.</param>
        /// <param name="target">Target position.</param>
        /// <param name="range">Maximum range.</param>
        /// <returns>True if within range.</returns>
        public static bool IsWithinRange(Vector3 position, Vector3 target, float range)
        {
            float distance = Vector3.Distance(position, target);
            return distance <= range;
        }
        
        /// <summary>
        /// Calculates remaining storage time.
        /// Property 3: Pet Cage Storage Duration
        /// </summary>
        /// <param name="currentTime">Current elapsed storage time.</param>
        /// <param name="maxTime">Maximum storage time.</param>
        /// <returns>Remaining time in seconds.</returns>
        public static float CalculateRemainingTime(float currentTime, float maxTime)
        {
            return Mathf.Max(0f, maxTime - currentTime);
        }
        
        /// <summary>
        /// Determines if warning state should be active.
        /// Requirement 8.3: Warning at 10 seconds remaining.
        /// </summary>
        /// <param name="remainingTime">Remaining storage time.</param>
        /// <param name="warningThreshold">Warning threshold time.</param>
        /// <returns>True if warning should be active.</returns>
        public static bool ShouldShowWarning(float remainingTime, float warningThreshold)
        {
            return remainingTime <= warningThreshold && remainingTime > 0f;
        }
        
        /// <summary>
        /// Determines if automatic release should occur.
        /// Requirement 1.6: Auto-release after 60 seconds.
        /// </summary>
        /// <param name="currentTime">Current elapsed storage time.</param>
        /// <param name="maxTime">Maximum storage time.</param>
        /// <returns>True if auto-release should occur.</returns>
        public static bool ShouldAutoRelease(float currentTime, float maxTime)
        {
            return currentTime >= maxTime;
        }
        
        /// <summary>
        /// Validates that storage duration matches the expected value.
        /// Property 3: Pet Cage Storage Duration - exactly 60 seconds.
        /// </summary>
        /// <param name="storageDuration">Actual storage duration.</param>
        /// <param name="expectedDuration">Expected storage duration (60 seconds).</param>
        /// <param name="tolerance">Acceptable tolerance for timing.</param>
        /// <returns>True if duration is within tolerance.</returns>
        public static bool ValidateStorageDuration(float storageDuration, float expectedDuration, float tolerance = 0.1f)
        {
            return Mathf.Abs(storageDuration - expectedDuration) <= tolerance;
        }
        
        /// <summary>
        /// Validates that invulnerability duration matches the expected value.
        /// Property 19: Caged Pet Release Invulnerability - exactly 3 seconds.
        /// </summary>
        /// <param name="invulnerabilityDuration">Actual invulnerability duration.</param>
        /// <param name="expectedDuration">Expected duration (3 seconds).</param>
        /// <param name="tolerance">Acceptable tolerance.</param>
        /// <returns>True if duration is within tolerance.</returns>
        public static bool ValidateInvulnerabilityDuration(float invulnerabilityDuration, float expectedDuration, float tolerance = 0.1f)
        {
            return Mathf.Abs(invulnerabilityDuration - expectedDuration) <= tolerance;
        }
        
        #endregion

        #region Editor Support
        
#if UNITY_EDITOR
        /// <summary>
        /// Sets the Phase 2 config for testing purposes.
        /// </summary>
        public void SetPhase2ConfigForTesting(Phase2GameConfig config)
        {
            _phase2Config = config;
        }
        
        /// <summary>
        /// Sets the storage time for testing purposes.
        /// </summary>
        public void SetStorageTimeForTesting(float time)
        {
            _currentStorageTime = time;
        }
        
        /// <summary>
        /// Sets the occupied state for testing purposes.
        /// </summary>
        public void SetOccupiedForTesting(bool occupied, PetAI pet = null)
        {
            IsOccupied = occupied;
            StoredPet = pet;
        }
        
        /// <summary>
        /// Gets the current storage time for testing.
        /// </summary>
        public float GetCurrentStorageTimeForTesting()
        {
            return _currentStorageTime;
        }
        
        /// <summary>
        /// Sets the max storage time directly for testing (bypasses config).
        /// </summary>
        public void SetMaxStorageTimeForTesting(float time)
        {
            _maxStorageTime = time;
        }
        
        /// <summary>
        /// Sets the warning time directly for testing (bypasses config).
        /// </summary>
        public void SetWarningTimeForTesting(float time)
        {
            _warningTime = time;
        }
        
        /// <summary>
        /// Sets the invulnerability time directly for testing (bypasses config).
        /// </summary>
        public void SetInvulnerabilityTimeForTesting(float time)
        {
            _releaseInvulnerabilityTime = time;
        }
#endif
        
        private void OnDrawGizmosSelected()
        {
            // Draw interaction range
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _interactionRange);
            
            // Draw storage position
            if (_storagePosition != null)
            {
                Gizmos.color = IsOccupied ? Color.yellow : Color.green;
                Gizmos.DrawWireSphere(_storagePosition.position, 0.3f);
            }
            
            // Draw release position
            if (_releasePosition != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(_releasePosition.position, 0.3f);
                
                // Draw line from storage to release
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
