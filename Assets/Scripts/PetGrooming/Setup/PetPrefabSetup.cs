using UnityEngine;
using UnityEngine.AI;
using PetGrooming.AI;
using PetGrooming.Core;

namespace PetGrooming.Setup
{
    /// <summary>
    /// Utility class for creating and configuring Pet prefabs.
    /// Requirement 2.1: Pet spawns at random valid position.
    /// </summary>
    public class PetPrefabSetup : MonoBehaviour
    {
        [Header("Pet Configuration")]
        [SerializeField] private GameConfig _gameConfig;
        [SerializeField] private Material _petMaterial;
        
        [Header("NavMeshAgent Settings")]
        [SerializeField] private float _speed = 6f;
        [SerializeField] private float _angularSpeed = 360f;
        [SerializeField] private float _acceleration = 8f;
        [SerializeField] private float _stoppingDistance = 0.5f;
        [SerializeField] private float _radius = 0.3f;
        [SerializeField] private float _height = 0.6f;
        
        [Header("Collider Settings")]
        [SerializeField] private Vector3 _colliderCenter = new Vector3(0f, 0.3f, 0f);
        [SerializeField] private float _colliderRadius = 0.2f;
        [SerializeField] private float _colliderHeight = 0.6f;
        
        [Header("Play Area Bounds")]
        [SerializeField] private Vector3 _playAreaMin = new Vector3(-15f, 0f, -15f);
        [SerializeField] private Vector3 _playAreaMax = new Vector3(15f, 0f, 15f);
        
        /// <summary>
        /// Creates a pet GameObject with all required components.
        /// </summary>
        [ContextMenu("Create Pet Template")]
        public GameObject CreatePetTemplate()
        {
            GameObject pet = new GameObject("Pet_Cat_Template");
            pet.transform.position = Vector3.zero;
            pet.tag = "Pet";
            
            // Create visual representation
            CreatePetVisuals(pet.transform);
            
            // Add collider
            CapsuleCollider collider = pet.AddComponent<CapsuleCollider>();
            collider.center = _colliderCenter;
            collider.radius = _colliderRadius;
            collider.height = _colliderHeight;
            collider.direction = 2; // Z-axis
            
            // Add Rigidbody
            Rigidbody rb = pet.AddComponent<Rigidbody>();
            rb.mass = 3f;
            rb.linearDamping = 2f;
            rb.angularDamping = 2f;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.isKinematic = true; // NavMeshAgent controls movement
            
            // Add NavMeshAgent
            NavMeshAgent navAgent = pet.AddComponent<NavMeshAgent>();
            ConfigureNavMeshAgent(navAgent);
            
            // Add PetAI
            PetAI petAI = pet.AddComponent<PetAI>();
            ConfigurePetAI(petAI);
            
            Debug.Log("[PetPrefabSetup] Pet template created with all components.");
            return pet;
        }
        
        /// <summary>
        /// Creates the visual representation of the pet (simple cat shape).
        /// </summary>
        private void CreatePetVisuals(Transform parent)
        {
            // Body (capsule rotated horizontally)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(parent);
            body.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            body.transform.localScale = new Vector3(0.3f, 0.25f, 0.5f);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            DestroyImmediate(body.GetComponent<Collider>());
            ApplyMaterial(body);
            
            // Head (sphere)
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(parent);
            head.transform.localPosition = new Vector3(0f, 0.35f, 0.3f);
            head.transform.localScale = new Vector3(0.25f, 0.2f, 0.2f);
            DestroyImmediate(head.GetComponent<Collider>());
            ApplyMaterial(head);
            
            // Left Ear
            GameObject leftEar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftEar.name = "Ear_Left";
            leftEar.transform.SetParent(parent);
            leftEar.transform.localPosition = new Vector3(-0.08f, 0.48f, 0.3f);
            leftEar.transform.localScale = new Vector3(0.05f, 0.1f, 0.03f);
            leftEar.transform.localRotation = Quaternion.Euler(0f, 0f, 15f);
            DestroyImmediate(leftEar.GetComponent<Collider>());
            ApplyMaterial(leftEar);
            
            // Right Ear
            GameObject rightEar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightEar.name = "Ear_Right";
            rightEar.transform.SetParent(parent);
            rightEar.transform.localPosition = new Vector3(0.08f, 0.48f, 0.3f);
            rightEar.transform.localScale = new Vector3(0.05f, 0.1f, 0.03f);
            rightEar.transform.localRotation = Quaternion.Euler(0f, 0f, -15f);
            DestroyImmediate(rightEar.GetComponent<Collider>());
            ApplyMaterial(rightEar);
            
            // Tail
            GameObject tail = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            tail.name = "Tail";
            tail.transform.SetParent(parent);
            tail.transform.localPosition = new Vector3(0f, 0.35f, -0.35f);
            tail.transform.localScale = new Vector3(0.05f, 0.15f, 0.05f);
            tail.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
            DestroyImmediate(tail.GetComponent<Collider>());
            ApplyMaterial(tail);
            
            // Eyes (small spheres)
            CreateEye(parent, "Eye_Left", new Vector3(-0.06f, 0.38f, 0.38f));
            CreateEye(parent, "Eye_Right", new Vector3(0.06f, 0.38f, 0.38f));
            
            // Nose (small cube)
            GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "Nose";
            nose.transform.SetParent(parent);
            nose.transform.localPosition = new Vector3(0f, 0.33f, 0.4f);
            nose.transform.localScale = new Vector3(0.03f, 0.02f, 0.02f);
            DestroyImmediate(nose.GetComponent<Collider>());
            // Nose is pink/dark
            Renderer noseRenderer = nose.GetComponent<Renderer>();
            if (noseRenderer != null)
            {
                noseRenderer.material.color = new Color(0.3f, 0.2f, 0.2f);
            }
        }
        
        private void CreateEye(Transform parent, string name, Vector3 position)
        {
            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = name;
            eye.transform.SetParent(parent);
            eye.transform.localPosition = position;
            eye.transform.localScale = new Vector3(0.04f, 0.04f, 0.02f);
            DestroyImmediate(eye.GetComponent<Collider>());
            
            // Eyes are dark
            Renderer eyeRenderer = eye.GetComponent<Renderer>();
            if (eyeRenderer != null)
            {
                eyeRenderer.material.color = Color.black;
            }
        }
        
        private void ApplyMaterial(GameObject obj)
        {
            if (_petMaterial != null)
            {
                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = _petMaterial;
                }
            }
        }
        
        /// <summary>
        /// Configures the NavMeshAgent with appropriate settings.
        /// </summary>
        private void ConfigureNavMeshAgent(NavMeshAgent agent)
        {
            agent.speed = _speed;
            agent.angularSpeed = _angularSpeed;
            agent.acceleration = _acceleration;
            agent.stoppingDistance = _stoppingDistance;
            agent.radius = _radius;
            agent.height = _height;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agent.autoTraverseOffMeshLink = true;
            agent.autoBraking = true;
        }
        
        /// <summary>
        /// Configures the PetAI component with game config and bounds.
        /// </summary>
        private void ConfigurePetAI(PetAI petAI)
        {
#if UNITY_EDITOR
            if (_gameConfig != null)
            {
                petAI.SetConfigForTesting(_gameConfig);
            }
            
            petAI.SetPlayAreaBoundsForTesting(_playAreaMin, _playAreaMax);
#endif
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw play area bounds
            Gizmos.color = Color.yellow;
            Vector3 center = (_playAreaMin + _playAreaMax) / 2f;
            Vector3 size = _playAreaMax - _playAreaMin;
            size.y = 2f;
            Gizmos.DrawWireCube(center, size);
            
            // Draw collider preview
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + _colliderCenter, _colliderRadius);
        }
    }
}
