using System;
using UnityEngine;
using UnityEngine.InputSystem;
using PetGrooming.Core;
using PetGrooming.AI;

namespace PetGrooming.Systems
{
    /// <summary>
    /// Controller for the Groomer character, extending ThirdPersonController functionality.
    /// Handles capture mechanics and movement speed adjustments when carrying a pet.
    /// Requirements: 1.1, 1.2, 1.3, 3.1, 3.2
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
        
        #endregion

        #region Properties
        
        /// <summary>
        /// Whether the groomer is currently carrying a captured pet.
        /// </summary>
        public bool IsCarryingPet { get; private set; }
        
        /// <summary>
        /// Reference to the currently captured pet.
        /// </summary>
        public PetAI CapturedPet { get; private set; }
        
        /// <summary>
        /// Capture range from config.
        /// Requirement 3.1: Capture within 1.5 units.
        /// </summary>
        public float CaptureRange => _gameConfig != null ? _gameConfig.CaptureRange : 1.5f;
        
        /// <summary>
        /// Speed multiplier when carrying a pet.
        /// Requirement 1.3: 15% speed reduction (0.85 multiplier).
        /// </summary>
        public float CarrySpeedMultiplier => _gameConfig != null ? _gameConfig.CarrySpeedMultiplier : 0.85f;
        
        /// <summary>
        /// Base movement speed.
        /// Requirement 1.1: 5 units per second.
        /// </summary>
        public float BaseMoveSpeed => _gameConfig != null ? _gameConfig.GroomerMoveSpeed : 5f;
        
        /// <summary>
        /// Current effective movement speed (accounting for carry state and alert bonus).
        /// Requirement 6.5: 10% speed bonus during alert state.
        /// </summary>
        public float CurrentMoveSpeed => CalculateFullEffectiveSpeed(
            BaseMoveSpeed, 
            CarrySpeedMultiplier, 
            IsCarryingPet,
            AlertSystem.Instance != null && AlertSystem.Instance.IsAlertActive,
            AlertSystem.Instance != null ? AlertSystem.Instance.GroomerSpeedBonus : 0.1f);
        
        /// <summary>
        /// Reference to the game configuration.
        /// </summary>
        public GameConfig Config => _gameConfig;
        
        #endregion

        #region Events
        
        /// <summary>
        /// Fired when a pet is successfully captured.
        /// </summary>
        public event Action<PetAI> OnPetCaptured;
        
        /// <summary>
        /// Fired when the captured pet escapes.
        /// </summary>
        public event Action OnPetEscaped;
        
        /// <summary>
        /// Fired when a capture attempt fails due to distance.
        /// </summary>
        public event Action OnCaptureFailed;
        
        /// <summary>
        /// Fired when a capture attempt is rejected because already carrying a pet.
        /// Requirement 1.3: Groomer can only carry one pet at a time.
        /// </summary>
        public event Action OnCaptureRejectedAlreadyCarrying;
        
        /// <summary>
        /// Fired when a pet is stored in a cage.
        /// </summary>
        public event Action<PetCage> OnPetStoredInCage;
        
        /// <summary>
        /// Fired when a pet is manually released from a cage.
        /// Requirement 8.5: Groomer can manually release pet from cage.
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
            
            // Create pet hold point if not assigned
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
        /// Attempts to capture a nearby pet.
        /// Requirement 1.3: Groomer can only carry one pet at a time.
        /// Requirement 3.1: Capture within 1.5 units.
        /// Requirement 3.2: Pet enters captured state on success.
        /// </summary>
        /// <returns>True if capture was successful.</returns>
        public bool TryCapturePet()
        {
            // Property 2: Single Pet Carry Constraint
            // Requirement 1.3: Reject capture if already carrying a pet
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
            
            // Check if pet is invulnerable
            if (nearestPet.IsInvulnerable)
            {
                Debug.Log("[GroomerController] Pet is invulnerable - capture rejected.");
                OnCaptureFailed?.Invoke();
                return false;
            }
            
            float distance = CalculateDistanceToPet(transform.position, nearestPet.transform.position);
            
            // Property 6: Capture Distance Validation
            if (!IsWithinCaptureRange(distance, CaptureRange))
            {
                Debug.Log($"[GroomerController] Pet too far. Distance: {distance}, Range: {CaptureRange}");
                OnCaptureFailed?.Invoke();
                return false;
            }
            
            // Capture successful
            CapturePet(nearestPet);
            return true;
        }
        
        /// <summary>
        /// Releases the currently captured pet.
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
        /// Called when the captured pet escapes.
        /// </summary>
        public void OnPetEscape()
        {
            if (!IsCarryingPet || CapturedPet == null)
            {
                return;
            }
            
            PetAI escapedPet = CapturedPet;
            
            // Clear capture state
            CapturedPet.transform.SetParent(null);
            CapturedPet = null;
            IsCarryingPet = false;
            
            OnPetEscaped?.Invoke();
            
            Debug.Log("[GroomerController] Pet escaped!");
        }
        
        /// <summary>
        /// Sets a reference to a nearby pet for capture detection.
        /// </summary>
        public void SetNearbyPet(PetAI pet)
        {
            _nearbyPet = pet;
        }
        
        /// <summary>
        /// Clears the nearby pet reference.
        /// </summary>
        public void ClearNearbyPet()
        {
            _nearbyPet = null;
        }
        
        /// <summary>
        /// Sets a reference to a nearby cage for interaction.
        /// </summary>
        public void SetNearbyCage(PetCage cage)
        {
            _nearbyCage = cage;
        }
        
        /// <summary>
        /// Clears the nearby cage reference.
        /// </summary>
        public void ClearNearbyCage()
        {
            _nearbyCage = null;
        }
        
        /// <summary>
        /// Attempts to store the carried pet in a nearby cage.
        /// Requirements: 1.4, 1.5
        /// </summary>
        /// <returns>True if storage was successful.</returns>
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
            
            // Store the pet
            PetAI petToStore = CapturedPet;
            
            // Unsubscribe from escape event
            petToStore.OnEscaped -= OnPetEscape;
            
            // Clear carry state
            petToStore.transform.SetParent(null);
            CapturedPet = null;
            IsCarryingPet = false;
            
            // Store in cage
            if (nearestCage.StorePet(petToStore))
            {
                OnPetStoredInCage?.Invoke(nearestCage);
                Debug.Log("[GroomerController] Pet stored in cage successfully.");
                return true;
            }
            
            // If storage failed, restore carry state
            CapturePet(petToStore);
            return false;
        }
        
        /// <summary>
        /// Attempts to manually release a pet from a nearby cage.
        /// Requirement 8.5: Groomer can manually release pet from cage.
        /// </summary>
        /// <returns>True if release was successful.</returns>
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
        /// Gets the pet hold point transform.
        /// </summary>
        public Transform GetPetHoldPoint()
        {
            return _petHoldPoint;
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
        /// Checks if a key was pressed this frame using the new Input System.
        /// </summary>
        private bool WasKeyPressedThisFrame(KeyCode keyCode)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return false;
            
            Key key = KeyCodeToKey(keyCode);
            return key != Key.None && keyboard[key].wasPressedThisFrame;
        }
        
        /// <summary>
        /// Converts legacy KeyCode to new Input System Key.
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
            // First check if we have a nearby pet reference
            if (_nearbyPet != null)
            {
                return _nearbyPet;
            }
            
            // Otherwise search for pets in the scene
            PetAI[] pets = FindObjectsOfType<PetAI>();
            PetAI nearest = null;
            float nearestDistance = float.MaxValue;
            
            foreach (PetAI pet in pets)
            {
                // Skip pets that are already captured or being groomed
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
            // First check if we have a nearby cage reference
            if (_nearbyCage != null)
            {
                return _nearbyCage;
            }
            
            // Otherwise search for cages in the scene
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
            
            // Parent pet to hold point
            pet.transform.SetParent(_petHoldPoint);
            pet.transform.localPosition = Vector3.zero;
            pet.transform.localRotation = Quaternion.identity;
            
            // Notify pet of capture
            pet.OnCaptured(transform);
            
            // Subscribe to escape event
            pet.OnEscaped += OnPetEscape;
            
            OnPetCaptured?.Invoke(pet);
            
            Debug.Log("[GroomerController] Pet captured!");
        }
        
        #endregion

        #region Static Calculation Methods (Testable)
        
        /// <summary>
        /// Calculates the effective movement speed based on carry state.
        /// Property 1: Carry Speed Reduction
        /// Requirement 1.3: 15% speed reduction when carrying pet.
        /// </summary>
        /// <param name="baseSpeed">Base movement speed.</param>
        /// <param name="carryMultiplier">Speed multiplier when carrying (0.85 for 15% reduction).</param>
        /// <param name="isCarrying">Whether currently carrying a pet.</param>
        /// <returns>Effective movement speed.</returns>
        public static float CalculateEffectiveSpeed(float baseSpeed, float carryMultiplier, bool isCarrying)
        {
            if (isCarrying)
            {
                return baseSpeed * carryMultiplier;
            }
            return baseSpeed;
        }
        
        /// <summary>
        /// Calculates the full effective movement speed including carry state and alert bonus.
        /// Property 17: Alert State Speed Bonus
        /// Requirement 6.5: 10% speed bonus during alert state.
        /// </summary>
        /// <param name="baseSpeed">Base movement speed.</param>
        /// <param name="carryMultiplier">Speed multiplier when carrying (0.85 for 15% reduction).</param>
        /// <param name="isCarrying">Whether currently carrying a pet.</param>
        /// <param name="isAlertActive">Whether alert state is active.</param>
        /// <param name="alertSpeedBonus">Speed bonus during alert (0.1 = 10%).</param>
        /// <returns>Full effective movement speed.</returns>
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
        /// Calculates the distance between groomer and pet positions.
        /// </summary>
        /// <param name="groomerPosition">Groomer's position.</param>
        /// <param name="petPosition">Pet's position.</param>
        /// <returns>Distance between positions.</returns>
        public static float CalculateDistanceToPet(Vector3 groomerPosition, Vector3 petPosition)
        {
            // Calculate horizontal distance (ignoring Y for ground-based capture)
            Vector3 diff = groomerPosition - petPosition;
            diff.y = 0f;
            return diff.magnitude;
        }
        
        /// <summary>
        /// Determines if a capture attempt should succeed based on distance.
        /// Property 6: Capture Distance Validation
        /// Requirement 3.1: Capture within 1.5 units.
        /// </summary>
        /// <param name="distance">Distance to pet.</param>
        /// <param name="captureRange">Maximum capture range.</param>
        /// <returns>True if within capture range.</returns>
        public static bool IsWithinCaptureRange(float distance, float captureRange)
        {
            return distance <= captureRange;
        }
        
        /// <summary>
        /// Determines the capture result based on distance and state.
        /// Property 6: Capture Distance Validation
        /// </summary>
        /// <param name="distance">Distance to pet.</param>
        /// <param name="captureRange">Maximum capture range.</param>
        /// <param name="petState">Current state of the pet.</param>
        /// <returns>True if capture should succeed.</returns>
        public static bool ShouldCaptureSucceed(float distance, float captureRange, PetAI.PetState petState)
        {
            // Cannot capture already captured or grooming pets
            if (petState == PetAI.PetState.Captured || petState == PetAI.PetState.BeingGroomed)
            {
                return false;
            }
            
            return IsWithinCaptureRange(distance, captureRange);
        }
        
        /// <summary>
        /// Determines if a capture attempt should be rejected due to already carrying a pet.
        /// Property 2: Single Pet Carry Constraint
        /// Requirement 1.3: Groomer can only carry one pet at a time.
        /// </summary>
        /// <param name="isCurrentlyCarrying">Whether the groomer is currently carrying a pet.</param>
        /// <param name="currentPet">The currently carried pet (can be null).</param>
        /// <returns>True if capture should be rejected (already carrying).</returns>
        public static bool ShouldRejectCaptureAlreadyCarrying(bool isCurrentlyCarrying, PetAI currentPet)
        {
            return isCurrentlyCarrying && currentPet != null;
        }
        
        /// <summary>
        /// Validates the single pet carry constraint.
        /// Property 2: Single Pet Carry Constraint
        /// Requirement 1.3: When Groomer captures a Pet while already carrying one, 
        /// the Capture_System SHALL reject the capture attempt.
        /// </summary>
        /// <param name="isCarryingBefore">Whether carrying a pet before capture attempt.</param>
        /// <param name="capturedPetBefore">The pet being carried before capture attempt.</param>
        /// <param name="captureAttemptResult">Result of the capture attempt.</param>
        /// <param name="isCarryingAfter">Whether carrying a pet after capture attempt.</param>
        /// <param name="capturedPetAfter">The pet being carried after capture attempt.</param>
        /// <returns>True if the constraint is satisfied.</returns>
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
        /// Sets the game config for testing purposes.
        /// </summary>
        public void SetConfigForTesting(GameConfig config)
        {
            _gameConfig = config;
        }
        
        /// <summary>
        /// Sets the carrying state for testing purposes.
        /// </summary>
        public void SetCarryingStateForTesting(bool isCarrying, PetAI pet = null)
        {
            IsCarryingPet = isCarrying;
            CapturedPet = pet;
        }
#endif
        
        private void OnDrawGizmosSelected()
        {
            // Draw capture range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, CaptureRange);
            
            // Draw pet hold point
            if (_petHoldPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(_petHoldPoint.position, 0.2f);
            }
        }
        
        #endregion
    }
}
