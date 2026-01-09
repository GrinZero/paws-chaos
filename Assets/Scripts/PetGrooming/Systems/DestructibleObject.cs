using System;
using UnityEngine;
using PetGrooming.Core;
using PetGrooming.AI;

namespace PetGrooming.Systems
{
    /// <summary>
    /// Component for destructible objects that can be knocked over by the pet.
    /// Requirements: 5.2, 5.3, 7.2, 7.3, 7.4
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class DestructibleObject : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Configuration")]
        [SerializeField] private DestructibleObjectType _objectType = DestructibleObjectType.ShelfItem;
        
        [Header("Physics Settings")]
        [SerializeField] private float _knockOverForceThreshold = 2f;
        [SerializeField] private float _knockOverAngleThreshold = 45f;
        
        [Header("Audio (Optional)")]
        [SerializeField] private AudioClip _knockOverSound;
        [SerializeField] private float _knockOverSoundVolume = 1f;
        
        #endregion

        #region Private Fields
        
        private Rigidbody _rigidbody;
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private bool _isKnockedOver;
        private bool _hasTriggeredMischief;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// Type of destructible object.
        /// </summary>
        public DestructibleObjectType Type => _objectType;
        
        /// <summary>
        /// Mischief value for this object type.
        /// Requirement 5.2: Shelf item adds 50 points.
        /// Requirement 5.3: Cleaning cart adds 80 points.
        /// </summary>
        public int MischiefValue => GetMischiefValueForType(_objectType);
        
        /// <summary>
        /// Whether the object has been knocked over.
        /// </summary>
        public bool IsKnockedOver => _isKnockedOver;
        
        /// <summary>
        /// Whether mischief has already been triggered for this object.
        /// </summary>
        public bool HasTriggeredMischief => _hasTriggeredMischief;
        
        #endregion

        #region Events
        
        /// <summary>
        /// Fired when the object is knocked over.
        /// </summary>
        public event Action<DestructibleObject> OnKnockedOver;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            
            // Store initial transform for reset
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
        }
        
        private void Start()
        {
            // Ensure rigidbody is configured properly
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = false;
                _rigidbody.useGravity = true;
            }
        }
        
        private void Update()
        {
            // Check if object has been knocked over based on rotation
            if (!_isKnockedOver)
            {
                CheckKnockedOverState();
            }
        }
        
        private void OnCollisionEnter(Collision collision)
        {
            // Check if collision is from a pet
            if (collision.gameObject.CompareTag("Pet"))
            {
                // Requirement 8.6: Check if pet is caged - caged pets don't trigger mischief
                PetAI pet = collision.gameObject.GetComponent<PetAI>();
                if (pet != null && pet.IsCaged)
                {
                    Debug.Log($"[DestructibleObject] Collision from caged pet ignored for mischief.");
                    return;
                }
                
                HandlePetCollision(collision);
            }
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Applies a force to knock over the object.
        /// Requirement 7.4: Apply realistic physics simulation.
        /// </summary>
        /// <param name="force">Force vector to apply.</param>
        public void KnockOver(Vector3 force)
        {
            if (_rigidbody == null) return;
            
            _rigidbody.AddForce(force, ForceMode.Impulse);
            
            Debug.Log($"[DestructibleObject] {gameObject.name} received knock force: {force.magnitude}");
        }
        
        /// <summary>
        /// Resets the object to its initial state.
        /// </summary>
        public void Reset()
        {
            transform.position = _initialPosition;
            transform.rotation = _initialRotation;
            
            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }
            
            _isKnockedOver = false;
            _hasTriggeredMischief = false;
            
            Debug.Log($"[DestructibleObject] {gameObject.name} reset to initial state.");
        }
        
        /// <summary>
        /// Manually triggers the knocked over state (for testing or scripted events).
        /// </summary>
        public void TriggerKnockedOver()
        {
            if (_isKnockedOver) return;
            
            SetKnockedOver();
        }
        
        #endregion

        #region Private Methods
        
        private void HandlePetCollision(Collision collision)
        {
            // Calculate collision force
            float collisionForce = collision.relativeVelocity.magnitude;
            
            if (collisionForce >= _knockOverForceThreshold)
            {
                // Apply physics force from collision
                Vector3 knockForce = collision.relativeVelocity.normalized * collisionForce;
                KnockOver(knockForce);
                
                Debug.Log($"[DestructibleObject] {gameObject.name} hit by pet with force: {collisionForce}");
            }
        }
        
        private void CheckKnockedOverState()
        {
            // Check if object has tilted beyond threshold
            float angle = Quaternion.Angle(transform.rotation, _initialRotation);
            
            if (angle >= _knockOverAngleThreshold)
            {
                SetKnockedOver();
            }
        }
        
        private void SetKnockedOver()
        {
            if (_isKnockedOver) return;
            
            _isKnockedOver = true;
            
            // Play sound if configured
            if (_knockOverSound != null)
            {
                AudioSource.PlayClipAtPoint(_knockOverSound, transform.position, _knockOverSoundVolume);
            }
            
            // Trigger mischief only once
            if (!_hasTriggeredMischief)
            {
                _hasTriggeredMischief = true;
                TriggerMischief();
            }
            
            OnKnockedOver?.Invoke(this);
            
            Debug.Log($"[DestructibleObject] {gameObject.name} knocked over! Type: {_objectType}, Mischief: {MischiefValue}");
        }
        
        private void TriggerMischief()
        {
            // Integrate with MischiefSystem
            if (MischiefSystem.Instance != null)
            {
                switch (_objectType)
                {
                    case DestructibleObjectType.ShelfItem:
                        MischiefSystem.Instance.AddShelfItemMischief();
                        break;
                    case DestructibleObjectType.CleaningCart:
                        MischiefSystem.Instance.AddCleaningCartMischief();
                        break;
                }
            }
            else
            {
                Debug.LogWarning("[DestructibleObject] MischiefSystem not found!");
            }
        }
        
        #endregion

        #region Static Calculation Methods (Testable)
        
        /// <summary>
        /// Gets the mischief value for a specific object type.
        /// Property 5: Mischief Value Calculation
        /// Requirement 5.2: Shelf item adds 50 points.
        /// Requirement 5.3: Cleaning cart adds 80 points.
        /// </summary>
        /// <param name="objectType">Type of destructible object.</param>
        /// <param name="shelfItemValue">Configured shelf item value (default 50).</param>
        /// <param name="cleaningCartValue">Configured cleaning cart value (default 80).</param>
        /// <returns>The mischief value for the object type.</returns>
        public static int GetMischiefValueForType(
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
        /// Determines if a collision force is strong enough to knock over the object.
        /// </summary>
        /// <param name="collisionForce">The force of the collision.</param>
        /// <param name="threshold">The minimum force required to knock over.</param>
        /// <returns>True if the force exceeds the threshold.</returns>
        public static bool IsForceEnoughToKnockOver(float collisionForce, float threshold)
        {
            return collisionForce >= threshold;
        }
        
        /// <summary>
        /// Determines if an object is knocked over based on rotation angle.
        /// </summary>
        /// <param name="currentRotation">Current rotation of the object.</param>
        /// <param name="initialRotation">Initial rotation of the object.</param>
        /// <param name="angleThreshold">Angle threshold for knocked over state.</param>
        /// <returns>True if the object is considered knocked over.</returns>
        public static bool IsKnockedOverByAngle(Quaternion currentRotation, Quaternion initialRotation, float angleThreshold)
        {
            float angle = Quaternion.Angle(currentRotation, initialRotation);
            return angle >= angleThreshold;
        }
        
        #endregion

        #region Editor Support
        
#if UNITY_EDITOR
        /// <summary>
        /// Sets the object type for testing purposes.
        /// </summary>
        public void SetObjectTypeForTesting(DestructibleObjectType type)
        {
            _objectType = type;
        }
        
        /// <summary>
        /// Sets the knocked over state for testing purposes.
        /// </summary>
        public void SetKnockedOverForTesting(bool knockedOver)
        {
            _isKnockedOver = knockedOver;
        }
        
        /// <summary>
        /// Sets the mischief triggered state for testing purposes.
        /// </summary>
        public void SetMischiefTriggeredForTesting(bool triggered)
        {
            _hasTriggeredMischief = triggered;
        }
        
        /// <summary>
        /// Gets the force threshold for testing.
        /// </summary>
        public float GetForceThresholdForTesting()
        {
            return _knockOverForceThreshold;
        }
        
        /// <summary>
        /// Gets the angle threshold for testing.
        /// </summary>
        public float GetAngleThresholdForTesting()
        {
            return _knockOverAngleThreshold;
        }
#endif
        
        private void OnDrawGizmosSelected()
        {
            // Draw object type indicator
            Gizmos.color = _objectType == DestructibleObjectType.ShelfItem ? Color.yellow : Color.cyan;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
        }
        
        #endregion
    }
}
