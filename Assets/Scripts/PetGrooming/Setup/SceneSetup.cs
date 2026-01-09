using UnityEngine;
using UnityEngine.AI;
using PetGrooming.Core;
using PetGrooming.Systems;
using PetGrooming.AI;
using PetGrooming.UI;

namespace PetGrooming.Setup
{
    /// <summary>
    /// Scene setup utility for configuring the Pet Grooming game scene.
    /// Requirements: 7.1, 7.2, 7.3, 7.5
    /// 
    /// This script sets up:
    /// - 1 Grooming Station (Requirement 7.1)
    /// - 4+ Shelf Items (Requirement 7.2)
    /// - 2+ Cleaning Carts (Requirement 7.3)
    /// - NavMesh configuration (Requirement 7.5)
    /// - Game Manager and UI systems
    /// </summary>
    public class SceneSetup : MonoBehaviour
    {
        [Header("Scene Configuration")]
        [SerializeField] private Vector3 _playAreaCenter = Vector3.zero;
        [SerializeField] private Vector3 _playAreaSize = new Vector3(30f, 0f, 30f);
        
        [Header("Grooming Station")]
        [SerializeField] private Vector3 _groomingStationPosition = new Vector3(0f, 0f, 10f);
        
        [Header("Destructible Objects")]
        [SerializeField] private int _shelfItemCount = 4;
        [SerializeField] private int _cleaningCartCount = 2;
        
        [Header("References (Optional)")]
        [SerializeField] private GameConfig _gameConfig;
        [SerializeField] private Material _floorMaterial;
        [SerializeField] private Material _wallMaterial;
        
        /// <summary>
        /// Sets up the complete game scene.
        /// </summary>
        [ContextMenu("Setup Complete Scene")]
        public void SetupCompleteScene()
        {
            CreateFloor();
            CreateWalls();
            CreateGroomingStation();
            CreateDestructibleObjects();
            CreateGameSystems();
            CreateUI();
            
            Debug.Log("[SceneSetup] Complete scene setup finished. Remember to bake NavMesh!");
        }
        
        /// <summary>
        /// Creates the floor for the play area.
        /// </summary>
        [ContextMenu("Create Floor")]
        public void CreateFloor()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.position = _playAreaCenter;
            floor.transform.localScale = new Vector3(_playAreaSize.x / 10f, 1f, _playAreaSize.z / 10f);
            floor.isStatic = true;
            
            // Mark as NavMesh walkable
            GameObjectUtility.SetStaticEditorFlags(floor, GameObjectUtility.NavigationStatic);
            
            if (_floorMaterial != null)
            {
                floor.GetComponent<Renderer>().material = _floorMaterial;
            }
            
            Debug.Log("[SceneSetup] Floor created.");
        }
        
        /// <summary>
        /// Creates walls around the play area.
        /// </summary>
        [ContextMenu("Create Walls")]
        public void CreateWalls()
        {
            GameObject wallsParent = new GameObject("Walls");
            wallsParent.transform.position = _playAreaCenter;
            wallsParent.isStatic = true;
            
            float halfWidth = _playAreaSize.x / 2f;
            float halfDepth = _playAreaSize.z / 2f;
            float wallHeight = 3f;
            float wallThickness = 0.5f;
            
            // Create 4 walls
            CreateWall(wallsParent.transform, "Wall_North", 
                new Vector3(0f, wallHeight / 2f, halfDepth), 
                new Vector3(_playAreaSize.x, wallHeight, wallThickness));
            
            CreateWall(wallsParent.transform, "Wall_South", 
                new Vector3(0f, wallHeight / 2f, -halfDepth), 
                new Vector3(_playAreaSize.x, wallHeight, wallThickness));
            
            CreateWall(wallsParent.transform, "Wall_East", 
                new Vector3(halfWidth, wallHeight / 2f, 0f), 
                new Vector3(wallThickness, wallHeight, _playAreaSize.z));
            
            CreateWall(wallsParent.transform, "Wall_West", 
                new Vector3(-halfWidth, wallHeight / 2f, 0f), 
                new Vector3(wallThickness, wallHeight, _playAreaSize.z));
            
            Debug.Log("[SceneSetup] Walls created.");
        }
        
        private void CreateWall(Transform parent, string name, Vector3 localPosition, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent);
            wall.transform.localPosition = localPosition;
            wall.transform.localScale = scale;
            wall.isStatic = true;
            
            // Mark as NavMesh obstacle
            GameObjectUtility.SetStaticEditorFlags(wall, GameObjectUtility.NavigationStatic);
            
            if (_wallMaterial != null)
            {
                wall.GetComponent<Renderer>().material = _wallMaterial;
            }
        }
        
        /// <summary>
        /// Creates the grooming station.
        /// Requirement 7.1: Scene contains at least 1 Grooming_Station.
        /// </summary>
        [ContextMenu("Create Grooming Station")]
        public void CreateGroomingStation()
        {
            GameObject station = new GameObject("GroomingStation");
            station.transform.position = _groomingStationPosition;
            
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
            
            Debug.Log("[SceneSetup] Grooming Station created.");
        }
        
        /// <summary>
        /// Creates destructible objects (shelves and carts).
        /// Requirements 7.2, 7.3: Scene contains shelf items and cleaning carts.
        /// </summary>
        [ContextMenu("Create Destructible Objects")]
        public void CreateDestructibleObjects()
        {
            GameObject destructiblesParent = new GameObject("Destructibles");
            destructiblesParent.transform.position = _playAreaCenter;
            
            // Create shelf items (Requirement 7.2: at least 4)
            float halfWidth = _playAreaSize.x / 2f - 2f;
            float halfDepth = _playAreaSize.z / 2f - 2f;
            
            Vector3[] shelfPositions = new Vector3[]
            {
                new Vector3(-halfWidth + 2f, 0f, halfDepth - 2f),
                new Vector3(halfWidth - 2f, 0f, halfDepth - 2f),
                new Vector3(-halfWidth + 2f, 0f, -halfDepth + 2f),
                new Vector3(halfWidth - 2f, 0f, -halfDepth + 2f),
                new Vector3(0f, 0f, halfDepth - 2f),
                new Vector3(0f, 0f, -halfDepth + 2f)
            };
            
            for (int i = 0; i < Mathf.Min(_shelfItemCount, shelfPositions.Length); i++)
            {
                CreateShelfItem(destructiblesParent.transform, $"ShelfItem_{i}", shelfPositions[i]);
            }
            
            // Create cleaning carts (Requirement 7.3: at least 2)
            Vector3[] cartPositions = new Vector3[]
            {
                new Vector3(-halfWidth / 2f, 0f, 0f),
                new Vector3(halfWidth / 2f, 0f, 0f),
                new Vector3(0f, 0f, halfDepth / 2f),
                new Vector3(0f, 0f, -halfDepth / 2f)
            };
            
            for (int i = 0; i < Mathf.Min(_cleaningCartCount, cartPositions.Length); i++)
            {
                CreateCleaningCart(destructiblesParent.transform, $"CleaningCart_{i}", cartPositions[i]);
            }
            
            Debug.Log($"[SceneSetup] Created {_shelfItemCount} shelf items and {_cleaningCartCount} cleaning carts.");
        }
        
        private void CreateShelfItem(Transform parent, string name, Vector3 position)
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
            DestroyImmediate(shelfBase.GetComponent<Collider>());
            
            // Create items
            for (int i = 0; i < 3; i++)
            {
                GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
                item.name = $"Item_{i}";
                item.transform.SetParent(shelf.transform);
                item.transform.localPosition = new Vector3(-0.4f + (i * 0.4f), 1.1f, 0f);
                item.transform.localScale = new Vector3(0.2f, 0.3f, 0.2f);
                DestroyImmediate(item.GetComponent<Collider>());
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
        
        private void CreateCleaningCart(Transform parent, string name, Vector3 position)
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
            DestroyImmediate(cartBody.GetComponent<Collider>());
            
            // Create handle
            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handle.name = "Handle";
            handle.transform.SetParent(cart.transform);
            handle.transform.localPosition = new Vector3(0f, 1.1f, -0.3f);
            handle.transform.localScale = new Vector3(0.6f, 0.05f, 0.05f);
            DestroyImmediate(handle.GetComponent<Collider>());
            
            // Create handle posts
            for (int i = 0; i < 2; i++)
            {
                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.name = $"HandlePost_{i}";
                post.transform.SetParent(cart.transform);
                post.transform.localPosition = new Vector3((i == 0 ? -0.3f : 0.3f), 0.9f, -0.3f);
                post.transform.localScale = new Vector3(0.05f, 0.4f, 0.05f);
                DestroyImmediate(post.GetComponent<Collider>());
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
                DestroyImmediate(wheel.GetComponent<Collider>());
            }
            
            // Add physics
            Rigidbody rb = cart.AddComponent<Rigidbody>();
            rb.mass = 10f;
            rb.linearDamping = 1f;
            rb.angularDamping = 1f;
            
            BoxCollider collider = cart.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.6f, 0f);
            collider.size = new Vector3(0.9f, 1.2f, 0.6f);
            
            DestructibleObject destructible = cart.AddComponent<DestructibleObject>();
        }
        
        /// <summary>
        /// Creates game management systems.
        /// </summary>
        [ContextMenu("Create Game Systems")]
        public void CreateGameSystems()
        {
            // Create GameManager
            GameObject gameManagerObj = new GameObject("GameManager");
            GameManager gameManager = gameManagerObj.AddComponent<GameManager>();
            
            if (_gameConfig != null)
            {
                // GameManager will use the config
            }
            
            // Create MischiefSystem
            GameObject mischiefObj = new GameObject("MischiefSystem");
            mischiefObj.AddComponent<MischiefSystem>();
            
            Debug.Log("[SceneSetup] Game systems created.");
        }
        
        /// <summary>
        /// Creates UI elements.
        /// </summary>
        [ContextMenu("Create UI")]
        public void CreateUI()
        {
            // Create Canvas
            GameObject canvasObj = new GameObject("GameCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            // Add GameHUD
            canvasObj.AddComponent<GameHUD>();
            
            // Add InteractionPrompts
            canvasObj.AddComponent<InteractionPrompts>();
            
            // Add ResultScreen
            canvasObj.AddComponent<ResultScreen>();
            
            Debug.Log("[SceneSetup] UI created.");
        }
        
        /// <summary>
        /// Gets the play area bounds for PetAI configuration.
        /// </summary>
        public (Vector3 min, Vector3 max) GetPlayAreaBounds()
        {
            Vector3 halfSize = _playAreaSize / 2f;
            Vector3 min = _playAreaCenter - halfSize;
            Vector3 max = _playAreaCenter + halfSize;
            return (min, max);
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw play area
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(_playAreaCenter, _playAreaSize + Vector3.up * 3f);
            
            // Draw grooming station position
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_groomingStationPosition, 1f);
        }
    }
    
    /// <summary>
    /// Helper class for setting static editor flags.
    /// </summary>
    public static class GameObjectUtility
    {
        public static void SetStaticEditorFlags(GameObject obj, int flags)
        {
#if UNITY_EDITOR
            UnityEditor.GameObjectUtility.SetStaticEditorFlags(obj, (UnityEditor.StaticEditorFlags)flags);
#endif
        }
        
        // Navigation static flag value
        public const int NavigationStatic = 8;
    }
}
