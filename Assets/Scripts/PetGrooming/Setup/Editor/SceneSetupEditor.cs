#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using PetGrooming.Core;
using PetGrooming.Systems;
using PetGrooming.AI;
using PetGrooming.UI;

namespace PetGrooming.Setup.Editor
{
    /// <summary>
    /// Editor utilities for setting up the Pet Grooming game scene.
    /// Requirements: 7.1, 7.2, 7.3, 7.5
    /// </summary>
    public static class SceneSetupEditor
    {
        private static readonly Vector3 PlayAreaCenter = Vector3.zero;
        private static readonly Vector3 PlayAreaSize = new Vector3(30f, 0f, 30f);
        
        [MenuItem("PetGrooming/Setup Scene/Complete Scene Setup")]
        public static void SetupCompleteScene()
        {
            CreateFloor();
            CreateWalls();
            CreateGroomingStation();
            CreateDestructibleObjects();
            CreateGameSystems();
            CreateUI();
            CreateNavMeshSurface();
            
            Debug.Log("[SceneSetupEditor] Complete scene setup finished!");
            Debug.Log("[SceneSetupEditor] Remember to bake NavMesh: Window > AI > Navigation > Bake");
        }
        
        [MenuItem("PetGrooming/Setup Scene/Create Floor")]
        public static void CreateFloor()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.position = PlayAreaCenter;
            floor.transform.localScale = new Vector3(PlayAreaSize.x / 10f, 1f, PlayAreaSize.z / 10f);
            floor.isStatic = true;
            
            // Mark as NavMesh walkable
            UnityEditor.GameObjectUtility.SetStaticEditorFlags(floor, StaticEditorFlags.NavigationStatic);
            
            // Try to apply a material
            Material floorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Material_Checkerboard.mat");
            if (floorMat != null)
            {
                floor.GetComponent<Renderer>().material = floorMat;
            }
            
            Undo.RegisterCreatedObjectUndo(floor, "Create Floor");
            Debug.Log("[SceneSetupEditor] Floor created.");
        }
        
        [MenuItem("PetGrooming/Setup Scene/Create Walls")]
        public static void CreateWalls()
        {
            GameObject wallsParent = new GameObject("Walls");
            wallsParent.transform.position = PlayAreaCenter;
            wallsParent.isStatic = true;
            
            float halfWidth = PlayAreaSize.x / 2f;
            float halfDepth = PlayAreaSize.z / 2f;
            float wallHeight = 3f;
            float wallThickness = 0.5f;
            
            Material wallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Material_Simple_BlueDark.mat");
            
            CreateWall(wallsParent.transform, "Wall_North", 
                new Vector3(0f, wallHeight / 2f, halfDepth), 
                new Vector3(PlayAreaSize.x, wallHeight, wallThickness), wallMat);
            
            CreateWall(wallsParent.transform, "Wall_South", 
                new Vector3(0f, wallHeight / 2f, -halfDepth), 
                new Vector3(PlayAreaSize.x, wallHeight, wallThickness), wallMat);
            
            CreateWall(wallsParent.transform, "Wall_East", 
                new Vector3(halfWidth, wallHeight / 2f, 0f), 
                new Vector3(wallThickness, wallHeight, PlayAreaSize.z), wallMat);
            
            CreateWall(wallsParent.transform, "Wall_West", 
                new Vector3(-halfWidth, wallHeight / 2f, 0f), 
                new Vector3(wallThickness, wallHeight, PlayAreaSize.z), wallMat);
            
            Undo.RegisterCreatedObjectUndo(wallsParent, "Create Walls");
            Debug.Log("[SceneSetupEditor] Walls created.");
        }
        
        private static void CreateWall(Transform parent, string name, Vector3 localPosition, Vector3 scale, Material mat)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent);
            wall.transform.localPosition = localPosition;
            wall.transform.localScale = scale;
            wall.isStatic = true;
            
            UnityEditor.GameObjectUtility.SetStaticEditorFlags(wall, StaticEditorFlags.NavigationStatic);
            
            if (mat != null)
            {
                wall.GetComponent<Renderer>().material = mat;
            }
        }
        
        [MenuItem("PetGrooming/Setup Scene/Create Grooming Station")]
        public static void CreateGroomingStation()
        {
            Vector3 position = new Vector3(0f, 0f, 10f);
            
            GameObject station = new GameObject("GroomingStation");
            station.transform.position = position;
            
            Material stationMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Material_Simple_AquaMarine.mat");
            
            // Create table top
            GameObject tableTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tableTop.name = "TableTop";
            tableTop.transform.SetParent(station.transform);
            tableTop.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            tableTop.transform.localScale = new Vector3(2f, 0.1f, 1.5f);
            if (stationMat != null) tableTop.GetComponent<Renderer>().material = stationMat;
            
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
                if (stationMat != null) leg.GetComponent<Renderer>().material = stationMat;
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
            
            Undo.RegisterCreatedObjectUndo(station, "Create Grooming Station");
            Debug.Log("[SceneSetupEditor] Grooming Station created.");
        }
        
        [MenuItem("PetGrooming/Setup Scene/Create Destructible Objects")]
        public static void CreateDestructibleObjects()
        {
            GameObject destructiblesParent = new GameObject("Destructibles");
            destructiblesParent.transform.position = PlayAreaCenter;
            
            float halfWidth = PlayAreaSize.x / 2f - 2f;
            float halfDepth = PlayAreaSize.z / 2f - 2f;
            
            Material shelfMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Material_Simple_Yellow.mat");
            Material cartMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Material_Simple_Green.mat");
            
            // Create 4 shelf items (Requirement 7.2)
            Vector3[] shelfPositions = new Vector3[]
            {
                new Vector3(-halfWidth + 2f, 0f, halfDepth - 2f),
                new Vector3(halfWidth - 2f, 0f, halfDepth - 2f),
                new Vector3(-halfWidth + 2f, 0f, -halfDepth + 2f),
                new Vector3(halfWidth - 2f, 0f, -halfDepth + 2f)
            };
            
            for (int i = 0; i < shelfPositions.Length; i++)
            {
                CreateShelfItem(destructiblesParent.transform, $"ShelfItem_{i}", shelfPositions[i], shelfMat);
            }
            
            // Create 2 cleaning carts (Requirement 7.3)
            Vector3[] cartPositions = new Vector3[]
            {
                new Vector3(-halfWidth / 2f, 0f, 0f),
                new Vector3(halfWidth / 2f, 0f, 0f)
            };
            
            for (int i = 0; i < cartPositions.Length; i++)
            {
                CreateCleaningCart(destructiblesParent.transform, $"CleaningCart_{i}", cartPositions[i], cartMat);
            }
            
            Undo.RegisterCreatedObjectUndo(destructiblesParent, "Create Destructible Objects");
            Debug.Log("[SceneSetupEditor] Created 4 shelf items and 2 cleaning carts.");
        }
        
        private static void CreateShelfItem(Transform parent, string name, Vector3 position, Material mat)
        {
            GameObject shelf = new GameObject(name);
            shelf.transform.SetParent(parent);
            shelf.transform.position = position;
            shelf.tag = "Destructible";
            
            // Create shelf base
            GameObject shelfBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shelfBase.name = "ShelfBase";
            shelfBase.transform.SetParent(shelf.transform);
            shelfBase.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            shelfBase.transform.localScale = new Vector3(1.5f, 1f, 0.4f);
            Object.DestroyImmediate(shelfBase.GetComponent<Collider>());
            if (mat != null) shelfBase.GetComponent<Renderer>().material = mat;
            
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
        }
        
        private static void CreateCleaningCart(Transform parent, string name, Vector3 position, Material mat)
        {
            GameObject cart = new GameObject(name);
            cart.transform.SetParent(parent);
            cart.transform.position = position;
            cart.tag = "Destructible";
            
            // Create cart body
            GameObject cartBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cartBody.name = "CartBody";
            cartBody.transform.SetParent(cart.transform);
            cartBody.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            cartBody.transform.localScale = new Vector3(0.8f, 0.8f, 0.5f);
            Object.DestroyImmediate(cartBody.GetComponent<Collider>());
            if (mat != null) cartBody.GetComponent<Renderer>().material = mat;
            
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
        }
        
        [MenuItem("PetGrooming/Setup Scene/Create Game Systems")]
        public static void CreateGameSystems()
        {
            // Create GameManager
            if (Object.FindObjectOfType<GameManager>() == null)
            {
                GameObject gameManagerObj = new GameObject("GameManager");
                gameManagerObj.AddComponent<GameManager>();
                Undo.RegisterCreatedObjectUndo(gameManagerObj, "Create GameManager");
            }
            
            // Create MischiefSystem
            if (Object.FindObjectOfType<MischiefSystem>() == null)
            {
                GameObject mischiefObj = new GameObject("MischiefSystem");
                mischiefObj.AddComponent<MischiefSystem>();
                Undo.RegisterCreatedObjectUndo(mischiefObj, "Create MischiefSystem");
            }
            
            Debug.Log("[SceneSetupEditor] Game systems created.");
        }
        
        [MenuItem("PetGrooming/Setup Scene/Create UI")]
        public static void CreateUI()
        {
            // Check if Canvas already exists
            Canvas existingCanvas = Object.FindObjectOfType<Canvas>();
            GameObject canvasObj;
            
            if (existingCanvas != null)
            {
                canvasObj = existingCanvas.gameObject;
            }
            else
            {
                canvasObj = new GameObject("GameCanvas");
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
            
            // Add UI components if not present
            if (canvasObj.GetComponent<GameHUD>() == null)
                canvasObj.AddComponent<GameHUD>();
            
            if (canvasObj.GetComponent<InteractionPrompts>() == null)
                canvasObj.AddComponent<InteractionPrompts>();
            
            if (canvasObj.GetComponent<ResultScreen>() == null)
                canvasObj.AddComponent<ResultScreen>();
            
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create UI");
            Debug.Log("[SceneSetupEditor] UI created.");
        }
        
        [MenuItem("PetGrooming/Setup Scene/Create NavMesh Surface")]
        public static void CreateNavMeshSurface()
        {
            // NavMeshSurface requires the AI Navigation package
            // If not available, users should manually set up NavMesh via Window > AI > Navigation
            Debug.Log("[SceneSetupEditor] To set up NavMesh:");
            Debug.Log("  1. Ensure AI Navigation package is installed (Window > Package Manager)");
            Debug.Log("  2. Use Window > AI > Navigation to configure and bake NavMesh");
            Debug.Log("  3. Mark floor and obstacles as Navigation Static in Inspector");
        }
        
        [MenuItem("PetGrooming/Setup Scene/Create Pet Instance")]
        public static void CreatePetInstance()
        {
            Vector3 spawnPosition = new Vector3(0f, 0f, -5f);
            
            GameObject pet = new GameObject("Pet_Cat");
            pet.transform.position = spawnPosition;
            pet.tag = "Pet";
            
            Material petMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Material_Simple_Orange.mat");
            
            // Create body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(pet.transform);
            body.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            body.transform.localScale = new Vector3(0.3f, 0.25f, 0.5f);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            if (petMat != null) body.GetComponent<Renderer>().material = petMat;
            
            // Create head
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(pet.transform);
            head.transform.localPosition = new Vector3(0f, 0.35f, 0.3f);
            head.transform.localScale = new Vector3(0.25f, 0.2f, 0.2f);
            Object.DestroyImmediate(head.GetComponent<Collider>());
            if (petMat != null) head.GetComponent<Renderer>().material = petMat;
            
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
                if (petMat != null) ear.GetComponent<Renderer>().material = petMat;
            }
            
            // Create tail
            GameObject tail = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            tail.name = "Tail";
            tail.transform.SetParent(pet.transform);
            tail.transform.localPosition = new Vector3(0f, 0.35f, -0.35f);
            tail.transform.localScale = new Vector3(0.05f, 0.15f, 0.05f);
            tail.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
            Object.DestroyImmediate(tail.GetComponent<Collider>());
            if (petMat != null) tail.GetComponent<Renderer>().material = petMat;
            
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
            NavMeshAgent navAgent = pet.AddComponent<NavMeshAgent>();
            navAgent.speed = 6f;
            navAgent.angularSpeed = 360f;
            navAgent.acceleration = 8f;
            navAgent.stoppingDistance = 0.5f;
            navAgent.radius = 0.3f;
            navAgent.height = 0.6f;
            
            // Add PetAI
            pet.AddComponent<PetAI>();
            
            Undo.RegisterCreatedObjectUndo(pet, "Create Pet Instance");
            Debug.Log("[SceneSetupEditor] Pet instance created.");
        }
    }
}
#endif
