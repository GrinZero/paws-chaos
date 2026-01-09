#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using PetGrooming.Systems;
using PetGrooming.AI;

namespace PetGrooming.Setup.Editor
{
    /// <summary>
    /// Editor utilities for creating Pet Grooming prefabs.
    /// Requirements: 7.1, 7.2, 7.3, 2.1
    /// </summary>
    public static class PrefabSetupEditor
    {
        private const string PREFAB_PATH = "Assets/Prefabs/PetGrooming";
        
        [MenuItem("PetGrooming/Create All Prefabs")]
        public static void CreateAllPrefabs()
        {
            EnsurePrefabDirectory();
            
            CreateGroomingStationPrefab();
            CreateShelfItemPrefab();
            CreateCleaningCartPrefab();
            CreatePetPrefab();
            
            AssetDatabase.Refresh();
            Debug.Log("[PrefabSetupEditor] All prefabs created successfully!");
        }
        
        [MenuItem("PetGrooming/Create Grooming Station Prefab")]
        public static void CreateGroomingStationPrefab()
        {
            EnsurePrefabDirectory();
            
            GameObject station = CreateGroomingStationGameObject();
            string path = $"{PREFAB_PATH}/GroomingStation.prefab";
            
            SavePrefab(station, path);
            Object.DestroyImmediate(station);
            
            Debug.Log($"[PrefabSetupEditor] Grooming Station prefab created at: {path}");
        }
        
        [MenuItem("PetGrooming/Create Shelf Item Prefab")]
        public static void CreateShelfItemPrefab()
        {
            EnsurePrefabDirectory();
            
            GameObject shelf = CreateShelfItemGameObject();
            string path = $"{PREFAB_PATH}/ShelfItem.prefab";
            
            SavePrefab(shelf, path);
            Object.DestroyImmediate(shelf);
            
            Debug.Log($"[PrefabSetupEditor] Shelf Item prefab created at: {path}");
        }
        
        [MenuItem("PetGrooming/Create Cleaning Cart Prefab")]
        public static void CreateCleaningCartPrefab()
        {
            EnsurePrefabDirectory();
            
            GameObject cart = CreateCleaningCartGameObject();
            string path = $"{PREFAB_PATH}/CleaningCart.prefab";
            
            SavePrefab(cart, path);
            Object.DestroyImmediate(cart);
            
            Debug.Log($"[PrefabSetupEditor] Cleaning Cart prefab created at: {path}");
        }
        
        [MenuItem("PetGrooming/Create Pet Prefab")]
        public static void CreatePetPrefab()
        {
            EnsurePrefabDirectory();
            
            GameObject pet = CreatePetGameObject();
            string path = $"{PREFAB_PATH}/Pet_Cat.prefab";
            
            SavePrefab(pet, path);
            Object.DestroyImmediate(pet);
            
            Debug.Log($"[PrefabSetupEditor] Pet prefab created at: {path}");
        }
        
        private static void EnsurePrefabDirectory()
        {
            if (!AssetDatabase.IsValidFolder(PREFAB_PATH))
            {
                string[] folders = PREFAB_PATH.Split('/');
                string currentPath = folders[0];
                
                for (int i = 1; i < folders.Length; i++)
                {
                    string newPath = $"{currentPath}/{folders[i]}";
                    if (!AssetDatabase.IsValidFolder(newPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, folders[i]);
                    }
                    currentPath = newPath;
                }
            }
        }
        
        private static void SavePrefab(GameObject obj, string path)
        {
            // Check if prefab already exists
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existingPrefab != null)
            {
                // Replace existing prefab
                PrefabUtility.SaveAsPrefabAsset(obj, path);
            }
            else
            {
                // Create new prefab
                PrefabUtility.SaveAsPrefabAsset(obj, path);
            }
        }
        
        /// <summary>
        /// Creates a grooming station GameObject.
        /// Requirement 7.1: Scene contains at least 1 Grooming_Station.
        /// </summary>
        private static GameObject CreateGroomingStationGameObject()
        {
            GameObject station = new GameObject("GroomingStation");
            
            // Create table top
            GameObject tableTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tableTop.name = "TableTop";
            tableTop.transform.SetParent(station.transform);
            tableTop.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            tableTop.transform.localScale = new Vector3(2f, 0.1f, 1.5f);
            
            // Create legs
            for (int i = 0; i < 4; i++)
            {
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leg.name = $"Leg_{i}";
                leg.transform.SetParent(station.transform);
                
                float xOffset = (i % 2 == 0) ? -0.8f : 0.8f;
                float zOffset = (i < 2) ? -0.6f : 0.6f;
                
                leg.transform.localPosition = new Vector3(xOffset, 0.4f, zOffset);
                leg.transform.localScale = new Vector3(0.1f, 0.8f, 0.1f);
            }
            
            // Create position markers
            GameObject groomingPos = new GameObject("GroomingPosition");
            groomingPos.transform.SetParent(station.transform);
            groomingPos.transform.localPosition = new Vector3(0f, 1f, 0f);
            
            GameObject groomerPos = new GameObject("GroomerPosition");
            groomerPos.transform.SetParent(station.transform);
            groomerPos.transform.localPosition = new Vector3(0f, 0f, -1.5f);
            
            // Add components
            station.AddComponent<GroomingStation>();
            station.AddComponent<GroomingSystem>();
            
            BoxCollider collider = station.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.5f, 0f);
            collider.size = new Vector3(2.5f, 1f, 2f);
            collider.isTrigger = true;
            
            return station;
        }
        
        /// <summary>
        /// Creates a shelf item GameObject.
        /// Requirement 7.2: Scene contains at least 4 shelf items.
        /// </summary>
        private static GameObject CreateShelfItemGameObject()
        {
            GameObject shelf = new GameObject("ShelfItem");
            shelf.tag = "Destructible";
            
            // Create shelf base
            GameObject shelfBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shelfBase.name = "ShelfBase";
            shelfBase.transform.SetParent(shelf.transform);
            shelfBase.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            shelfBase.transform.localScale = new Vector3(1.5f, 1f, 0.4f);
            Object.DestroyImmediate(shelfBase.GetComponent<Collider>());
            
            // Create items
            for (int i = 0; i < 3; i++)
            {
                GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
                item.name = $"Item_{i}";
                item.transform.SetParent(shelf.transform);
                item.transform.localPosition = new Vector3(-0.4f + (i * 0.4f), 1.1f, 0f);
                item.transform.localScale = new Vector3(0.2f, 0.3f, 0.2f);
                Object.DestroyImmediate(item.GetComponent<Collider>());
            }
            
            // Add physics
            Rigidbody rb = shelf.AddComponent<Rigidbody>();
            rb.mass = 5f;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 0.5f;
            
            BoxCollider collider = shelf.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.7f, 0f);
            collider.size = new Vector3(1.5f, 1.4f, 0.4f);
            
            shelf.AddComponent<DestructibleObject>();
            
            return shelf;
        }
        
        /// <summary>
        /// Creates a cleaning cart GameObject.
        /// Requirement 7.3: Scene contains at least 2 cleaning carts.
        /// </summary>
        private static GameObject CreateCleaningCartGameObject()
        {
            GameObject cart = new GameObject("CleaningCart");
            cart.tag = "Destructible";
            
            // Create cart body
            GameObject cartBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cartBody.name = "CartBody";
            cartBody.transform.SetParent(cart.transform);
            cartBody.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            cartBody.transform.localScale = new Vector3(0.8f, 0.8f, 0.5f);
            Object.DestroyImmediate(cartBody.GetComponent<Collider>());
            
            // Create handle
            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handle.name = "Handle";
            handle.transform.SetParent(cart.transform);
            handle.transform.localPosition = new Vector3(0f, 1.1f, -0.3f);
            handle.transform.localScale = new Vector3(0.6f, 0.05f, 0.05f);
            Object.DestroyImmediate(handle.GetComponent<Collider>());
            
            // Create handle posts
            for (int i = 0; i < 2; i++)
            {
                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.name = $"HandlePost_{i}";
                post.transform.SetParent(cart.transform);
                post.transform.localPosition = new Vector3((i == 0 ? -0.3f : 0.3f), 0.9f, -0.3f);
                post.transform.localScale = new Vector3(0.05f, 0.4f, 0.05f);
                Object.DestroyImmediate(post.GetComponent<Collider>());
            }
            
            // Create wheels
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
                Object.DestroyImmediate(wheel.GetComponent<Collider>());
            }
            
            // Add physics
            Rigidbody rb = cart.AddComponent<Rigidbody>();
            rb.mass = 10f;
            rb.linearDamping = 1f;
            rb.angularDamping = 1f;
            
            BoxCollider collider = cart.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.6f, 0f);
            collider.size = new Vector3(0.9f, 1.2f, 0.6f);
            
            cart.AddComponent<DestructibleObject>();
            
            return cart;
        }
        
        /// <summary>
        /// Creates a pet GameObject.
        /// Requirement 2.1: Pet spawns at random valid position.
        /// </summary>
        private static GameObject CreatePetGameObject()
        {
            GameObject pet = new GameObject("Pet_Cat");
            pet.tag = "Pet";
            
            // Create body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(pet.transform);
            body.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            body.transform.localScale = new Vector3(0.3f, 0.25f, 0.5f);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            
            // Create head
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(pet.transform);
            head.transform.localPosition = new Vector3(0f, 0.35f, 0.3f);
            head.transform.localScale = new Vector3(0.25f, 0.2f, 0.2f);
            Object.DestroyImmediate(head.GetComponent<Collider>());
            
            // Create ears
            for (int i = 0; i < 2; i++)
            {
                GameObject ear = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ear.name = $"Ear_{i}";
                ear.transform.SetParent(pet.transform);
                ear.transform.localPosition = new Vector3((i == 0 ? -0.08f : 0.08f), 0.48f, 0.3f);
                ear.transform.localScale = new Vector3(0.05f, 0.1f, 0.03f);
                ear.transform.localRotation = Quaternion.Euler(0f, 0f, (i == 0 ? 15f : -15f));
                Object.DestroyImmediate(ear.GetComponent<Collider>());
            }
            
            // Create tail
            GameObject tail = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            tail.name = "Tail";
            tail.transform.SetParent(pet.transform);
            tail.transform.localPosition = new Vector3(0f, 0.35f, -0.35f);
            tail.transform.localScale = new Vector3(0.05f, 0.15f, 0.05f);
            tail.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
            Object.DestroyImmediate(tail.GetComponent<Collider>());
            
            // Add collider
            CapsuleCollider collider = pet.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 0.3f, 0f);
            collider.radius = 0.2f;
            collider.height = 0.6f;
            collider.direction = 2;
            
            // Add Rigidbody
            Rigidbody rb = pet.AddComponent<Rigidbody>();
            rb.mass = 3f;
            rb.linearDamping = 2f;
            rb.angularDamping = 2f;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.isKinematic = true;
            
            // Add NavMeshAgent
            UnityEngine.AI.NavMeshAgent navAgent = pet.AddComponent<UnityEngine.AI.NavMeshAgent>();
            navAgent.speed = 6f;
            navAgent.angularSpeed = 360f;
            navAgent.acceleration = 8f;
            navAgent.stoppingDistance = 0.5f;
            navAgent.radius = 0.3f;
            navAgent.height = 0.6f;
            
            // Add PetAI
            pet.AddComponent<PetAI>();
            
            return pet;
        }
    }
}
#endif
