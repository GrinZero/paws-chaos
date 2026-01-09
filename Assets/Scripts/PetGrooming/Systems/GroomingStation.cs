using System;
using UnityEngine;
using PetGrooming.AI;

namespace PetGrooming.Systems
{
    /// <summary>
    /// Represents a grooming station where pets can be groomed.
    /// Requirements: 4.1, 7.1
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
        /// Whether the station is currently occupied.
        /// </summary>
        public bool IsOccupied { get; private set; }
        
        /// <summary>
        /// The position where the pet is placed during grooming.
        /// </summary>
        public Transform GroomingPosition => _groomingPosition;
        
        /// <summary>
        /// The position where the groomer stands during grooming.
        /// </summary>
        public Transform GroomerPosition => _groomerPosition;
        
        /// <summary>
        /// Distance within which grooming can be initiated.
        /// </summary>
        public float InteractionRange => _interactionRange;
        
        /// <summary>
        /// Reference to the grooming system.
        /// </summary>
        public GroomingSystem GroomingSystem => _groomingSystem;
        
        /// <summary>
        /// The pet currently being groomed (if any).
        /// </summary>
        public PetAI CurrentPet { get; private set; }
        
        #endregion

        #region Events
        
        /// <summary>
        /// Fired when grooming starts at this station.
        /// </summary>
        public event Action OnGroomingStarted;
        
        /// <summary>
        /// Fired when grooming ends at this station.
        /// </summary>
        public event Action OnGroomingEnded;
        
        /// <summary>
        /// Fired when a groomer enters the interaction range.
        /// </summary>
        public event Action<Transform> OnGroomerInRange;
        
        /// <summary>
        /// Fired when a groomer exits the interaction range.
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
            // Subscribe to grooming system events
            if (_groomingSystem != null)
            {
                _groomingSystem.OnGroomingComplete += HandleGroomingComplete;
                _groomingSystem.OnGroomingCancelled += HandleGroomingCancelled;
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (_groomingSystem != null)
            {
                _groomingSystem.OnGroomingComplete -= HandleGroomingComplete;
                _groomingSystem.OnGroomingCancelled -= HandleGroomingCancelled;
            }
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Checks if grooming can be started by the given groomer.
        /// Requirement 4.1: Grooming begins when groomer brings captured pet to station.
        /// </summary>
        /// <param name="groomerPosition">Position of the groomer.</param>
        /// <param name="hasCapuredPet">Whether the groomer has a captured pet.</param>
        /// <returns>True if grooming can be started.</returns>
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
        /// Checks if the groomer is within interaction range.
        /// </summary>
        /// <param name="groomerPosition">Position of the groomer.</param>
        /// <returns>True if groomer is in range.</returns>
        public bool IsGroomerInRange(Vector3 groomerPosition)
        {
            return IsWithinRange(groomerPosition, transform.position, _interactionRange);
        }
        
        /// <summary>
        /// Starts grooming at this station.
        /// Requirement 4.1: Grooming begins automatically when conditions are met.
        /// </summary>
        /// <param name="pet">The pet to be groomed.</param>
        /// <returns>True if grooming was started successfully.</returns>
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
            
            // Position the pet at the grooming position
            if (_groomingPosition != null)
            {
                pet.transform.position = _groomingPosition.position;
                pet.transform.rotation = _groomingPosition.rotation;
            }
            
            // Notify the pet that grooming has started
            pet.OnGroomingStarted();
            
            // Start the grooming process
            if (_groomingSystem != null)
            {
                _groomingSystem.StartGrooming();
            }
            
            OnGroomingStarted?.Invoke();
            
            Debug.Log($"[GroomingStation] Grooming started for pet: {pet.name}");
            
            return true;
        }
        
        /// <summary>
        /// Ends grooming at this station.
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
        /// Gets the grooming position in world space.
        /// </summary>
        /// <returns>World position for pet during grooming.</returns>
        public Vector3 GetGroomingWorldPosition()
        {
            return _groomingPosition != null ? _groomingPosition.position : transform.position;
        }
        
        /// <summary>
        /// Gets the groomer position in world space.
        /// </summary>
        /// <returns>World position for groomer during grooming.</returns>
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
                Debug.LogWarning("[GroomingStation] Grooming position not assigned, using station position.");
                // Create a child transform for grooming position
                var groomingPosObj = new GameObject("GroomingPosition");
                groomingPosObj.transform.SetParent(transform);
                groomingPosObj.transform.localPosition = Vector3.zero;
                _groomingPosition = groomingPosObj.transform;
            }
            
            if (_groomerPosition == null)
            {
                Debug.LogWarning("[GroomingStation] Groomer position not assigned, using offset from station.");
                // Create a child transform for groomer position
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
                    Debug.LogWarning("[GroomingStation] GroomingSystem not found, creating one.");
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
        /// Calculates the distance between two positions.
        /// </summary>
        /// <param name="pos1">First position.</param>
        /// <param name="pos2">Second position.</param>
        /// <returns>Distance between positions.</returns>
        public static float CalculateDistance(Vector3 pos1, Vector3 pos2)
        {
            return Vector3.Distance(pos1, pos2);
        }
        
        #endregion

        #region Editor Support
        
#if UNITY_EDITOR
        /// <summary>
        /// Sets the grooming system for testing purposes.
        /// </summary>
        public void SetGroomingSystemForTesting(GroomingSystem system)
        {
            _groomingSystem = system;
        }
        
        /// <summary>
        /// Sets the interaction range for testing purposes.
        /// </summary>
        public void SetInteractionRangeForTesting(float range)
        {
            _interactionRange = range;
        }
        
        /// <summary>
        /// Sets the occupied state for testing purposes.
        /// </summary>
        public void SetOccupiedForTesting(bool occupied)
        {
            IsOccupied = occupied;
        }
#endif
        
        private void OnDrawGizmosSelected()
        {
            // Draw interaction range
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _interactionRange);
            
            // Draw grooming position
            if (_groomingPosition != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_groomingPosition.position, 0.3f);
            }
            
            // Draw groomer position
            if (_groomerPosition != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(_groomerPosition.position, 0.3f);
            }
        }
        
        #endregion
    }
}
