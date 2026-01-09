using UnityEngine;
using PetGrooming.Systems;
using PetGrooming.AI;

namespace PetGrooming.Setup
{
    /// <summary>
    /// Utility class for creating Pet Grooming game prefabs.
    /// Requirements: 7.1, 7.2, 7.3, 2.1
    /// 
    /// Usage: Attach to a GameObject in the scene and call CreateAllPrefabs() from the Inspector
    /// or use the Unity Editor menu: PetGrooming > Create Prefabs
    /// </summary>
    public class PrefabSetup : MonoBehaviour
    {
        [Header("Prefab Output Path")]
        [SerializeField] private string _prefabOutputPath = "Assets/Prefabs/PetGrooming";
        
        [Header("Materials (Optional)")]
        [SerializeField] private Material _groomingStationMaterial;
        [SerializeField] private Material _shelfMaterial;
        [SerializeField] private Material _cartMaterial;
        [SerializeField] private Material _petMaterial;
        
        /// <summary>
        /// Creates all game prefabs in the scene for manual prefab saving.
        /// Call this method to generate prefab templates.
        /// </summary>
        [ContextMenu("Create All Prefab Templates")]
        public void CreateAllPrefabTemplates()
        {
            CreateGroomingStationTemplate();
            CreateShelfItemTemplate();
            CreateCleaningCartTemplate();
            CreatePetTemplate();
            
            Debug.Log("[PrefabSetup] All prefab templates created in scene. Save them as prefabs manually.");
        }
        
        /// <summary>
        /// Creates a grooming station template GameObject.
        /// Requirement 7.1: Scene contains at least 1 Grooming_Station.
        /// </summary>
        [ContextMenu("Create Grooming Station Template")]
        public GameObject CreateGroomingStationTemplate()
        {
            // Create main station object
            GameObject station = new GameObject("GroomingStation_Template");
            station.transform.position = Vector3.zero;
            
            // Add visual representation (table-like structure)
            GameObject tableTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tableTop.name = "TableTop";
            tableTop.transform.SetParent(station.transform);
            tableTop.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            tableTop.transform.localScale = new Vector3(2f, 0.1f, 1.5f);
            
            if (_groomingStationMaterial != null)
            {
                tableTop.GetComponent<Renderer>().material = _groomingStationMaterial;
            }
            
            // Add legs
            for (int i = 0; i < 4; i++)
            {
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leg.name = $"Leg_{i}";
                leg.transform.SetParent(station.transform);
                
                float xOffset = (i % 2 == 0) ? -0.8f : 0.8f;
                float zOffset = (i < 2) ? -0.6f : 0.6f;
                
                leg.transform.localPosition = new Vector3(xOffset, 0.4f, zOffset);
                leg.transform.localScale = new Vector3(0.1f, 0.8f, 0.1f);
                
                if (_groomingStationMaterial != null)
                {
                    leg.GetComponent<Renderer>().material = _groomingStationMaterial;
                }
            }
            
            // Create grooming position marker
            GameObject groomingPos = new GameObject("GroomingPosition");
            groomingPos.transform.SetParent(station.transform);
            groomingPos.transform.localPosition = new Vector3(0f, 1f, 0f);
            
            // Create groomer position marker
            GameObject groomerPos = new GameObject("GroomerPosition");
            groomerPos.transform.SetParent(station.transform);
            groomerPos.transform.localPosition = new Vector3(0f, 0f, -1.5f);
            
            // Add GroomingStation component
            GroomingStation stationComponent = station.AddComponent<GroomingStation>();
            
            // Add GroomingSystem component
            station.AddComponent<GroomingSystem>();
            
            // Add collider for interaction detection
            BoxCollider stationCollider = station.AddComponent<BoxCollider>();
            stationCollider.center = new Vector3(0f, 0.5f, 0f);
            stationCollider.size = new Vector3(2.5f, 1f, 2f);
            stationCollider.isTrigger = true;
            
            Debug.Log("[PrefabSetup] Grooming Station template created.");
            return station;
        }
        
        /// <summary>
        /// Creates a shelf item (destructible) template GameObject.
        /// Requirement 7.2: Scene contains at least 4 shelf items.
        /// </summary>
        [ContextMenu("Create Shelf Item Template")]
        public GameObject CreateShelfItemTemplate()
        {
            // Create shelf with items
            GameObject shelf = new GameObject("ShelfItem_Template");
            shelf.transform.position = Vector3.zero;
            shelf.tag = "Destructible";
            
            // Create shelf structure
            GameObject shelfBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shelfBase.name = "ShelfBase";
            shelfBase.transform.SetParent(shelf.transform);
            shelfBase.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            shelfBase.transform.localScale = new Vector3(1.5f, 1f, 0.4f);
            
            // Remove collider from visual (we'll add one to parent)
            DestroyImmediate(shelfBase.GetComponent<Collider>());
            
            if (_shelfMaterial != null)
            {
                shelfBase.GetComponent<Renderer>().material = _shelfMaterial;
            }
            
            // Add items on shelf (small boxes)
            for (int i = 0; i < 3; i++)
            {
                GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
                item.name = $"Item_{i}";
                item.transform.SetParent(shelf.transform);
                item.transform.localPosition = new Vector3(-0.4f + (i * 0.4f), 1.1f, 0f);
                item.transform.localScale = new Vector3(0.2f, 0.3f, 0.2f);
                
                // Remove individual colliders
                DestroyImmediate(item.GetComponent<Collider>());
            }
            
            // Add physics components
            Rigidbody rb = shelf.AddComponent<Rigidbody>();
            rb.mass = 5f;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 0.5f;
            
            BoxCollider collider = shelf.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.7f, 0f);
            collider.size = new Vector3(1.5f, 1.4f, 0.4f);
            
            // Add DestructibleObject component
            DestructibleObject destructible = shelf.AddComponent<DestructibleObject>();
            
            Debug.Log("[PrefabSetup] Shelf Item template created.");
            return shelf;
        }
        
        /// <summary>
        /// Creates a cleaning cart (destructible) template GameObject.
        /// Requirement 7.3: Scene contains at least 2 cleaning carts.
        /// </summary>
        [ContextMenu("Create Cleaning Cart Template")]
        public GameObject CreateCleaningCartTemplate()
        {
            // Create cart
            GameObject cart = new GameObject("CleaningCart_Template");
            cart.transform.position = Vector3.zero;
            cart.tag = "Destructible";
            
            // Create cart body
            GameObject cartBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cartBody.name = "CartBody";
            cartBody.transform.SetParent(cart.transform);
            cartBody.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            cartBody.transform.localScale = new Vector3(0.8f, 0.8f, 0.5f);
            
            // Remove collider from visual
            DestroyImmediate(cartBody.GetComponent<Collider>());
            
            if (_cartMaterial != null)
            {
                cartBody.GetComponent<Renderer>().material = _cartMaterial;
            }
            
            // Add handle
            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handle.name = "Handle";
            handle.transform.SetParent(cart.transform);
            handle.transform.localPosition = new Vector3(0f, 1.1f, -0.3f);
            handle.transform.localScale = new Vector3(0.6f, 0.05f, 0.05f);
            
            DestroyImmediate(handle.GetComponent<Collider>());
            
            // Add handle posts
            for (int i = 0; i < 2; i++)
            {
                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.name = $"HandlePost_{i}";
                post.transform.SetParent(cart.transform);
                post.transform.localPosition = new Vector3((i == 0 ? -0.3f : 0.3f), 0.9f, -0.3f);
                post.transform.localScale = new Vector3(0.05f, 0.4f, 0.05f);
                
                DestroyImmediate(post.GetComponent<Collider>());
            }
            
            // Add wheels (visual only)
            for (int i = 0; i < 4; i++)
            {
                GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.name = $"Wheel_{i}";
                wheel.transform.SetParent(cart.transform);
                
                float xOffset = (i % 2 == 0) ? -0.35f : 0.35f;
                float zOffset = (i < 2) ? -0.2f : 0.2f;
                
                wheel.transform.localPosition = new Vector3(xOffset, 0.1f, zOffset);
                wheel.transform.localScale = new Vector3(0.15f, 0.05f, 0.15f);
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                
                DestroyImmediate(wheel.GetComponent<Collider>());
            }
            
            // Add physics components
            Rigidbody rb = cart.AddComponent<Rigidbody>();
            rb.mass = 10f;
            rb.linearDamping = 1f;
            rb.angularDamping = 1f;
            
            BoxCollider collider = cart.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.6f, 0f);
            collider.size = new Vector3(0.9f, 1.2f, 0.6f);
            
            // Add DestructibleObject component
            DestructibleObject destructible = cart.AddComponent<DestructibleObject>();
            
            Debug.Log("[PrefabSetup] Cleaning Cart template created.");
            return cart;
        }
        
        /// <summary>
        /// Creates a pet (cat) template GameObject.
        /// Requirement 2.1: Pet spawns at random valid position.
        /// </summary>
        [ContextMenu("Create Pet Template")]
        public GameObject CreatePetTemplate()
        {
            // Create pet
            GameObject pet = new GameObject("Pet_Cat_Template");
            pet.transform.position = Vector3.zero;
            pet.tag = "Pet";
            pet.layer = LayerMask.NameToLayer("Default");
            
            // Create simple cat body (capsule)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(pet.transform);
            body.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            body.transform.localScale = new Vector3(0.3f, 0.25f, 0.5f);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            
            // Remove collider from visual
            DestroyImmediate(body.GetComponent<Collider>());
            
            if (_petMaterial != null)
            {
                body.GetComponent<Renderer>().material = _petMaterial;
            }
            
            // Create head
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(pet.transform);
            head.transform.localPosition = new Vector3(0f, 0.35f, 0.3f);
            head.transform.localScale = new Vector3(0.25f, 0.2f, 0.2f);
            
            DestroyImmediate(head.GetComponent<Collider>());
            
            if (_petMaterial != null)
            {
                head.GetComponent<Renderer>().material = _petMaterial;
            }
            
            // Create ears
            for (int i = 0; i < 2; i++)
            {
                GameObject ear = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ear.name = $"Ear_{i}";
                ear.transform.SetParent(pet.transform);
                ear.transform.localPosition = new Vector3((i == 0 ? -0.08f : 0.08f), 0.48f, 0.3f);
                ear.transform.localScale = new Vector3(0.05f, 0.1f, 0.03f);
                ear.transform.localRotation = Quaternion.Euler(0f, 0f, (i == 0 ? 15f : -15f));
                
                DestroyImmediate(ear.GetComponent<Collider>());
                
                if (_petMaterial != null)
                {
                    ear.GetComponent<Renderer>().material = _petMaterial;
                }
            }
            
            // Create tail
            GameObject tail = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            tail.name = "Tail";
            tail.transform.SetParent(pet.transform);
            tail.transform.localPosition = new Vector3(0f, 0.35f, -0.35f);
            tail.transform.localScale = new Vector3(0.05f, 0.15f, 0.05f);
            tail.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
            
            DestroyImmediate(tail.GetComponent<Collider>());
            
            if (_petMaterial != null)
            {
                tail.GetComponent<Renderer>().material = _petMaterial;
            }
            
            // Add main collider
            CapsuleCollider collider = pet.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 0.3f, 0f);
            collider.radius = 0.2f;
            collider.height = 0.6f;
            collider.direction = 2; // Z-axis
            
            // Add Rigidbody for physics interactions
            Rigidbody rb = pet.AddComponent<Rigidbody>();
            rb.mass = 3f;
            rb.linearDamping = 2f;
            rb.angularDamping = 2f;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.isKinematic = true; // NavMeshAgent will control movement
            
            // Add NavMeshAgent
            UnityEngine.AI.NavMeshAgent navAgent = pet.AddComponent<UnityEngine.AI.NavMeshAgent>();
            navAgent.speed = 6f;
            navAgent.angularSpeed = 360f;
            navAgent.acceleration = 8f;
            navAgent.stoppingDistance = 0.5f;
            navAgent.radius = 0.3f;
            navAgent.height = 0.6f;
            
            // Add PetAI component
            PetAI petAI = pet.AddComponent<PetAI>();
            
            Debug.Log("[PrefabSetup] Pet template created.");
            return pet;
        }
    }
}
