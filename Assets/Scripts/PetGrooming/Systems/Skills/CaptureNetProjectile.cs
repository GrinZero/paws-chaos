using UnityEngine;
using PetGrooming.AI;

namespace PetGrooming.Systems.Skills
{
    /// <summary>
    /// Projectile component for the Capture Net skill.
    /// Handles movement and collision detection with pets.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CaptureNetProjectile : MonoBehaviour
    {
        #region Private Fields
        private CaptureNetSkill _ownerSkill;
        private Vector3 _direction;
        private float _speed;
        private float _maxRange;
        private Vector3 _startPosition;
        private bool _hasHit;
        private Rigidbody _rigidbody;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody != null)
            {
                _rigidbody.useGravity = false;
                _rigidbody.isKinematic = false;
                _rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }
        }

        private void Update()
        {
            if (_hasHit) return;
            
            // Check if exceeded max range
            float distanceTraveled = Vector3.Distance(_startPosition, transform.position);
            if (distanceTraveled >= _maxRange)
            {
                DestroyProjectile();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hasHit) return;
            
            HandleCollision(other.gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_hasHit) return;
            
            HandleCollision(collision.gameObject);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initializes the projectile with movement parameters.
        /// </summary>
        /// <param name="ownerSkill">The skill that created this projectile</param>
        /// <param name="direction">Direction of travel</param>
        /// <param name="speed">Speed of the projectile</param>
        /// <param name="maxRange">Maximum travel distance</param>
        public void Initialize(CaptureNetSkill ownerSkill, Vector3 direction, float speed, float maxRange)
        {
            _ownerSkill = ownerSkill;
            _direction = direction.normalized;
            _speed = speed;
            _maxRange = maxRange;
            _startPosition = transform.position;
            _hasHit = false;
            
            // Set velocity
            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = _direction * _speed;
            }
        }
        #endregion

        #region Private Methods
        private void HandleCollision(GameObject hitObject)
        {
            // Try to find PetAI on the hit object
            PetAI pet = hitObject.GetComponent<PetAI>();
            if (pet == null)
            {
                pet = hitObject.GetComponentInParent<PetAI>();
            }
            
            if (pet != null)
            {
                _hasHit = true;
                
                // Notify the skill of the hit
                if (_ownerSkill != null)
                {
                    _ownerSkill.OnProjectileHit(pet);
                }
                
                DestroyProjectile();
            }
            else
            {
                // Hit something else (wall, obstacle) - destroy projectile
                DestroyProjectile();
            }
        }

        private void DestroyProjectile()
        {
            // Could add visual effects here before destroying
            Destroy(gameObject);
        }
        #endregion
    }
}
