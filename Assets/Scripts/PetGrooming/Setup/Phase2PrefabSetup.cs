using UnityEngine;
using UnityEngine.AI;
using PetGrooming.Systems;
using PetGrooming.Systems.Skills;
using PetGrooming.AI;
using PetGrooming.Core;

namespace PetGrooming.Setup
{
    /// <summary>
    /// Utility class for creating Phase 2 Pet Grooming game prefabs.
    /// Requirements: 1.4, 2.1, 2.2, 3.1, 4.1, 8.1
    /// 
    /// Usage: Attach to a GameObject in the scene and use context menu options
    /// or use the Unity Editor menu: PetGrooming > Create Phase 2 Prefabs
    /// </summary>
    public class Phase2PrefabSetup : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private Phase2GameConfig _phase2Config;
        [SerializeField] private GameConfig _gameConfig;
        
        [Header("Materials (Optional)")]
        [SerializeField] private Material _cageMaterial;
        [SerializeField] private Material _catMaterial;
        [SerializeField] private Material _dogMaterial;
        [SerializeField] private Material _groomerMaterial;
        
        [Header("Prefab Output Path")]
        [SerializeField] private string _prefabOutputPath = "Assets/Prefabs/PetGrooming";

        #region Pet Cage Prefab - Task 17.1
        
        /// <summary>
        /// Creates a Pet Cage prefab template.
        /// Requirements: 1.4, 8.1
        /// - Configures PetCage component
        /// - Adds visual indicators
        /// </summary>
        [ContextMenu("Create Pet Cage Template")]
        public GameObject CreatePetCageTemplate()
        {
            // Create main cage object
            GameObject cage = new GameObject("PetCage_Template");
            cage.transform.position = Vector3.zero;
            cage.tag = "PetCage";
            
            // Create cage visual structure (wire cage appearance)
            CreateCageVisuals(cage.transform);
            
            // Create storage position marker
            GameObject storagePos = new GameObject("StoragePosition");
            storagePos.transform.SetParent(cage.transform);
            storagePos.transform.localPosition = Vector3.zero;
            
            // Create release position marker
            GameObject releasePos = new GameObject("ReleasePosition");
            releasePos.transform.SetParent(cage.transform);
            releasePos.transform.localPosition = new Vector3(0f, 0f, 1.5f);
            
            // Create visual indicator (light or glow)
            GameObject indicator = CreateCageIndicator(cage.transform);
            
            // Add PetCage component
            PetCage petCage = cage.AddComponent<PetCage>();
            
            // Configure PetCage via reflection or serialized fields
#if UNITY_EDITOR
            var serializedObj = new UnityEditor.SerializedObject(petCage);
            
            var storagePosProp = serializedObj.FindProperty("_storagePosition");
            if (storagePosProp != null)
                storagePosProp.objectReferenceValue = storagePos.transform;
            
            var releasePosProp = serializedObj.FindProperty("_releasePosition");
            if (releasePosProp != null)
                releasePosProp.objectReferenceValue = releasePos.transform;
            
            var configProp = serializedObj.FindProperty("_phase2Config");
            if (configProp != null && _phase2Config != null)
                configProp.objectReferenceValue = _phase2Config;
            
            // Get the cage renderer for visual state indication
            var rendererProp = serializedObj.FindProperty("_cageRenderer");
            if (rendererProp != null)
            {
                Renderer cageRenderer = cage.GetComponentInChildren<Renderer>();
                if (cageRenderer != null)
                    rendererProp.objectReferenceValue = cageRenderer;
            }
            
            serializedObj.ApplyModifiedProperties();
#endif
            
            // Add collider for interaction detection
            BoxCollider cageCollider = cage.AddComponent<BoxCollider>();
            cageCollider.center = new Vector3(0f, 0.5f, 0f);
            cageCollider.size = new Vector3(1.5f, 1.2f, 1.5f);
            cageCollider.isTrigger = true;
            
            Debug.Log("[Phase2PrefabSetup] Pet Cage template created.");
            return cage;
        }
        
        /// <summary>
        /// Creates the visual representation of the pet cage.
        /// Requirement 8.1: Visual indicator showing whether cage is empty or occupied.
        /// </summary>
        private void CreateCageVisuals(Transform parent)
        {
            // Create cage base (floor)
            GameObject cageBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cageBase.name = "CageBase";
            cageBase.transform.SetParent(parent);
            cageBase.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            cageBase.transform.localScale = new Vector3(1.2f, 0.1f, 1.2f);
            ApplyMaterial(cageBase, _cageMaterial);
            DestroyImmediate(cageBase.GetComponent<Collider>());
            
            // Create cage bars (vertical)
            float barRadius = 0.03f;
            float barHeight = 1.0f;
            int barsPerSide = 5;
            float cageWidth = 1.0f;
            
            for (int side = 0; side < 4; side++)
            {
                for (int i = 0; i < barsPerSide; i++)
                {
                    GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    bar.name = $"Bar_Side{side}_{i}";
                    bar.transform.SetParent(parent);
                    
                    float offset = -cageWidth / 2f + (cageWidth / (barsPerSide - 1)) * i;
                    
                    switch (side)
                    {
                        case 0: // Front
                            bar.transform.localPosition = new Vector3(offset, barHeight / 2f + 0.1f, -cageWidth / 2f);
                            break;
                        case 1: // Back
                            bar.transform.localPosition = new Vector3(offset, barHeight / 2f + 0.1f, cageWidth / 2f);
                            break;
                        case 2: // Left
                            bar.transform.localPosition = new Vector3(-cageWidth / 2f, barHeight / 2f + 0.1f, offset);
                            break;
                        case 3: // Right
                            bar.transform.localPosition = new Vector3(cageWidth / 2f, barHeight / 2f + 0.1f, offset);
                            break;
                    }
                    
                    bar.transform.localScale = new Vector3(barRadius * 2f, barHeight / 2f, barRadius * 2f);
                    ApplyMaterial(bar, _cageMaterial);
                    DestroyImmediate(bar.GetComponent<Collider>());
                }
            }
            
            // Create cage top (horizontal bars)
            GameObject cageTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cageTop.name = "CageTop";
            cageTop.transform.SetParent(parent);
            cageTop.transform.localPosition = new Vector3(0f, barHeight + 0.1f, 0f);
            cageTop.transform.localScale = new Vector3(1.1f, 0.05f, 1.1f);
            ApplyMaterial(cageTop, _cageMaterial);
            DestroyImmediate(cageTop.GetComponent<Collider>());
            
            // Create door frame (front opening)
            GameObject doorFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorFrame.name = "DoorFrame";
            doorFrame.transform.SetParent(parent);
            doorFrame.transform.localPosition = new Vector3(0f, barHeight / 2f + 0.1f, -cageWidth / 2f - 0.05f);
            doorFrame.transform.localScale = new Vector3(0.4f, barHeight, 0.02f);
            ApplyMaterial(doorFrame, _cageMaterial);
            DestroyImmediate(doorFrame.GetComponent<Collider>());
        }
        
        /// <summary>
        /// Creates the visual indicator for cage state.
        /// Requirement 8.1: Visual indicator showing whether cage is empty or occupied.
        /// </summary>
        private GameObject CreateCageIndicator(Transform parent)
        {
            // Create indicator light
            GameObject indicator = new GameObject("StateIndicator");
            indicator.transform.SetParent(parent);
            indicator.transform.localPosition = new Vector3(0f, 1.3f, 0f);
            
            // Add point light for visual feedback
            Light indicatorLight = indicator.AddComponent<Light>();
            indicatorLight.type = LightType.Point;
            indicatorLight.range = 2f;
            indicatorLight.intensity = 1f;
            indicatorLight.color = Color.green; // Green = empty
            
            // Add small sphere as visual marker
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "IndicatorSphere";
            sphere.transform.SetParent(indicator.transform);
            sphere.transform.localPosition = Vector3.zero;
            sphere.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
            DestroyImmediate(sphere.GetComponent<Collider>());
            
            return indicator;
        }
        
        #endregion


        #region Dog Prefab - Task 17.2
        
        /// <summary>
        /// Creates a Dog pet prefab template.
        /// Requirements: 2.1, 2.2
        /// - Configures DogAI component (PetAI with Dog type)
        /// - Configures DogSkillManager
        /// - Configures collision radius (1.0)
        /// </summary>
        [ContextMenu("Create Dog Template")]
        public GameObject CreateDogTemplate()
        {
            // Create dog object
            GameObject dog = new GameObject("Pet_Dog_Template");
            dog.transform.position = Vector3.zero;
            dog.tag = "Pet";
            dog.layer = LayerMask.NameToLayer("Default");
            
            // Create dog visual representation
            CreateDogVisuals(dog.transform);
            
            // Add main collider - Requirement 2.2: Dog collision radius = 1.0
            CapsuleCollider collider = dog.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 0.5f, 0f);
            collider.radius = 1.0f; // Dog collision radius
            collider.height = 1.2f;
            collider.direction = 1; // Y-axis
            
            // Add Rigidbody
            Rigidbody rb = dog.AddComponent<Rigidbody>();
            rb.mass = 8f; // Dogs are heavier
            rb.linearDamping = 2f;
            rb.angularDamping = 2f;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.isKinematic = true; // NavMeshAgent controls movement
            
            // Add NavMeshAgent
            NavMeshAgent navAgent = dog.AddComponent<NavMeshAgent>();
            ConfigureDogNavMeshAgent(navAgent);
            
            // Add PetAI component with Dog type
            PetAI petAI = dog.AddComponent<PetAI>();
            ConfigureDogPetAI(petAI);
            
            // Add DogSkillManager
            DogSkillManager skillManager = dog.AddComponent<DogSkillManager>();
            ConfigureDogSkillManager(skillManager, dog);
            
            Debug.Log("[Phase2PrefabSetup] Dog template created with collision radius 1.0.");
            return dog;
        }
        
        /// <summary>
        /// Creates the visual representation of the dog.
        /// </summary>
        private void CreateDogVisuals(Transform parent)
        {
            // Body (larger capsule for dog)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(parent);
            body.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            body.transform.localScale = new Vector3(0.6f, 0.4f, 0.9f);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            DestroyImmediate(body.GetComponent<Collider>());
            ApplyMaterial(body, _dogMaterial);
            
            // Head (larger sphere for dog)
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(parent);
            head.transform.localPosition = new Vector3(0f, 0.55f, 0.5f);
            head.transform.localScale = new Vector3(0.4f, 0.35f, 0.4f);
            DestroyImmediate(head.GetComponent<Collider>());
            ApplyMaterial(head, _dogMaterial);
            
            // Snout (elongated for dog)
            GameObject snout = GameObject.CreatePrimitive(PrimitiveType.Cube);
            snout.name = "Snout";
            snout.transform.SetParent(parent);
            snout.transform.localPosition = new Vector3(0f, 0.45f, 0.7f);
            snout.transform.localScale = new Vector3(0.15f, 0.12f, 0.2f);
            DestroyImmediate(snout.GetComponent<Collider>());
            ApplyMaterial(snout, _dogMaterial);
            
            // Floppy ears (dog style)
            for (int i = 0; i < 2; i++)
            {
                GameObject ear = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ear.name = $"Ear_{(i == 0 ? "Left" : "Right")}";
                ear.transform.SetParent(parent);
                ear.transform.localPosition = new Vector3((i == 0 ? -0.18f : 0.18f), 0.55f, 0.45f);
                ear.transform.localScale = new Vector3(0.08f, 0.2f, 0.1f);
                ear.transform.localRotation = Quaternion.Euler(0f, 0f, (i == 0 ? -20f : 20f));
                DestroyImmediate(ear.GetComponent<Collider>());
                ApplyMaterial(ear, _dogMaterial);
            }
            
            // Tail (wagging style)
            GameObject tail = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            tail.name = "Tail";
            tail.transform.SetParent(parent);
            tail.transform.localPosition = new Vector3(0f, 0.6f, -0.5f);
            tail.transform.localScale = new Vector3(0.08f, 0.2f, 0.08f);
            tail.transform.localRotation = Quaternion.Euler(-45f, 0f, 0f);
            DestroyImmediate(tail.GetComponent<Collider>());
            ApplyMaterial(tail, _dogMaterial);
            
            // Legs (4 legs for dog)
            Vector3[] legPositions = new Vector3[]
            {
                new Vector3(-0.2f, 0.2f, 0.25f),  // Front left
                new Vector3(0.2f, 0.2f, 0.25f),   // Front right
                new Vector3(-0.2f, 0.2f, -0.25f), // Back left
                new Vector3(0.2f, 0.2f, -0.25f)   // Back right
            };
            
            for (int i = 0; i < 4; i++)
            {
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                leg.name = $"Leg_{i}";
                leg.transform.SetParent(parent);
                leg.transform.localPosition = legPositions[i];
                leg.transform.localScale = new Vector3(0.1f, 0.2f, 0.1f);
                DestroyImmediate(leg.GetComponent<Collider>());
                ApplyMaterial(leg, _dogMaterial);
            }
            
            // Eyes
            for (int i = 0; i < 2; i++)
            {
                GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eye.name = $"Eye_{(i == 0 ? "Left" : "Right")}";
                eye.transform.SetParent(parent);
                eye.transform.localPosition = new Vector3((i == 0 ? -0.1f : 0.1f), 0.6f, 0.65f);
                eye.transform.localScale = new Vector3(0.06f, 0.06f, 0.03f);
                DestroyImmediate(eye.GetComponent<Collider>());
                Renderer eyeRenderer = eye.GetComponent<Renderer>();
                if (eyeRenderer != null)
                    eyeRenderer.material.color = Color.black;
            }
            
            // Nose
            GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            nose.name = "Nose";
            nose.transform.SetParent(parent);
            nose.transform.localPosition = new Vector3(0f, 0.45f, 0.8f);
            nose.transform.localScale = new Vector3(0.08f, 0.06f, 0.06f);
            DestroyImmediate(nose.GetComponent<Collider>());
            Renderer noseRenderer = nose.GetComponent<Renderer>();
            if (noseRenderer != null)
                noseRenderer.material.color = new Color(0.2f, 0.1f, 0.1f);
        }
        
        /// <summary>
        /// Configures the NavMeshAgent for dog movement.
        /// Requirement 2.1: Dog base movement speed of 5 units per second.
        /// </summary>
        private void ConfigureDogNavMeshAgent(NavMeshAgent agent)
        {
            float dogSpeed = _phase2Config != null ? _phase2Config.DogMoveSpeed : 5f;
            agent.speed = dogSpeed;
            agent.angularSpeed = 300f;
            agent.acceleration = 6f;
            agent.stoppingDistance = 0.5f;
            agent.radius = 0.5f; // NavMesh radius
            agent.height = 1.0f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agent.autoTraverseOffMeshLink = false; // Dogs can't climb
            agent.autoBraking = true;
        }
        
        /// <summary>
        /// Configures the PetAI component for dog behavior.
        /// Requirements: 2.2, 2.4
        /// </summary>
        private void ConfigureDogPetAI(PetAI petAI)
        {
#if UNITY_EDITOR
            var serializedObj = new UnityEditor.SerializedObject(petAI);
            
            // Set pet type to Dog
            var petTypeProp = serializedObj.FindProperty("_petType");
            if (petTypeProp != null)
                petTypeProp.enumValueIndex = (int)PetAI.PetType.Dog;
            
            // Set game config
            var gameConfigProp = serializedObj.FindProperty("_gameConfig");
            if (gameConfigProp != null && _gameConfig != null)
                gameConfigProp.objectReferenceValue = _gameConfig;
            
            // Set phase 2 config
            var phase2ConfigProp = serializedObj.FindProperty("_phase2Config");
            if (phase2ConfigProp != null && _phase2Config != null)
                phase2ConfigProp.objectReferenceValue = _phase2Config;
            
            // Get renderer for visual effects
            var rendererProp = serializedObj.FindProperty("_petRenderer");
            if (rendererProp != null)
            {
                Renderer petRenderer = petAI.GetComponentInChildren<Renderer>();
                if (petRenderer != null)
                    rendererProp.objectReferenceValue = petRenderer;
            }
            
            serializedObj.ApplyModifiedProperties();
#endif
        }
        
        /// <summary>
        /// Configures the DogSkillManager with all three dog skills.
        /// Requirement 5.1: Dog has 3 skills.
        /// </summary>
        private void ConfigureDogSkillManager(DogSkillManager skillManager, GameObject dog)
        {
#if UNITY_EDITOR
            var serializedObj = new UnityEditor.SerializedObject(skillManager);
            
            // Set game config
            var configProp = serializedObj.FindProperty("GameConfig");
            if (configProp != null && _phase2Config != null)
                configProp.objectReferenceValue = _phase2Config;
            
            serializedObj.ApplyModifiedProperties();
#endif
            
            // Skills will be auto-created by DogSkillManager.InitializeSkills()
        }
        
        #endregion


        #region Cat Prefab Update - Task 17.3
        
        /// <summary>
        /// Creates/updates a Cat pet prefab template with CatSkillManager.
        /// Requirements: 4.1
        /// - Adds CatSkillManager
        /// - Confirms collision radius (0.5)
        /// </summary>
        [ContextMenu("Create Cat Template")]
        public GameObject CreateCatTemplate()
        {
            // Create cat object
            GameObject cat = new GameObject("Pet_Cat_Template");
            cat.transform.position = Vector3.zero;
            cat.tag = "Pet";
            cat.layer = LayerMask.NameToLayer("Default");
            
            // Create cat visual representation
            CreateCatVisuals(cat.transform);
            
            // Add main collider - Requirement: Cat collision radius = 0.5
            CapsuleCollider collider = cat.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 0.3f, 0f);
            collider.radius = 0.5f; // Cat collision radius (confirmed)
            collider.height = 0.6f;
            collider.direction = 2; // Z-axis
            
            // Add Rigidbody
            Rigidbody rb = cat.AddComponent<Rigidbody>();
            rb.mass = 3f;
            rb.linearDamping = 2f;
            rb.angularDamping = 2f;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.isKinematic = true; // NavMeshAgent controls movement
            
            // Add NavMeshAgent
            NavMeshAgent navAgent = cat.AddComponent<NavMeshAgent>();
            ConfigureCatNavMeshAgent(navAgent);
            
            // Add PetAI component with Cat type
            PetAI petAI = cat.AddComponent<PetAI>();
            ConfigureCatPetAI(petAI);
            
            // Add CatSkillManager - Requirement 4.1
            CatSkillManager skillManager = cat.AddComponent<CatSkillManager>();
            ConfigureCatSkillManager(skillManager, cat);
            
            Debug.Log("[Phase2PrefabSetup] Cat template created with CatSkillManager and collision radius 0.5.");
            return cat;
        }
        
        /// <summary>
        /// Creates the visual representation of the cat.
        /// </summary>
        private void CreateCatVisuals(Transform parent)
        {
            // Body (capsule rotated horizontally)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(parent);
            body.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            body.transform.localScale = new Vector3(0.3f, 0.25f, 0.5f);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            DestroyImmediate(body.GetComponent<Collider>());
            ApplyMaterial(body, _catMaterial);
            
            // Head (sphere)
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(parent);
            head.transform.localPosition = new Vector3(0f, 0.35f, 0.3f);
            head.transform.localScale = new Vector3(0.25f, 0.2f, 0.2f);
            DestroyImmediate(head.GetComponent<Collider>());
            ApplyMaterial(head, _catMaterial);
            
            // Pointy ears (cat style)
            for (int i = 0; i < 2; i++)
            {
                GameObject ear = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ear.name = $"Ear_{(i == 0 ? "Left" : "Right")}";
                ear.transform.SetParent(parent);
                ear.transform.localPosition = new Vector3((i == 0 ? -0.08f : 0.08f), 0.48f, 0.3f);
                ear.transform.localScale = new Vector3(0.05f, 0.1f, 0.03f);
                ear.transform.localRotation = Quaternion.Euler(0f, 0f, (i == 0 ? 15f : -15f));
                DestroyImmediate(ear.GetComponent<Collider>());
                ApplyMaterial(ear, _catMaterial);
            }
            
            // Tail (curved up)
            GameObject tail = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            tail.name = "Tail";
            tail.transform.SetParent(parent);
            tail.transform.localPosition = new Vector3(0f, 0.35f, -0.35f);
            tail.transform.localScale = new Vector3(0.05f, 0.15f, 0.05f);
            tail.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
            DestroyImmediate(tail.GetComponent<Collider>());
            ApplyMaterial(tail, _catMaterial);
            
            // Eyes
            for (int i = 0; i < 2; i++)
            {
                GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eye.name = $"Eye_{(i == 0 ? "Left" : "Right")}";
                eye.transform.SetParent(parent);
                eye.transform.localPosition = new Vector3((i == 0 ? -0.06f : 0.06f), 0.38f, 0.38f);
                eye.transform.localScale = new Vector3(0.04f, 0.04f, 0.02f);
                DestroyImmediate(eye.GetComponent<Collider>());
                Renderer eyeRenderer = eye.GetComponent<Renderer>();
                if (eyeRenderer != null)
                    eyeRenderer.material.color = Color.black;
            }
            
            // Nose
            GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "Nose";
            nose.transform.SetParent(parent);
            nose.transform.localPosition = new Vector3(0f, 0.33f, 0.4f);
            nose.transform.localScale = new Vector3(0.03f, 0.02f, 0.02f);
            DestroyImmediate(nose.GetComponent<Collider>());
            Renderer noseRenderer = nose.GetComponent<Renderer>();
            if (noseRenderer != null)
                noseRenderer.material.color = new Color(0.3f, 0.2f, 0.2f);
        }
        
        /// <summary>
        /// Configures the NavMeshAgent for cat movement.
        /// Cat moves at 6 units per second and can climb.
        /// </summary>
        private void ConfigureCatNavMeshAgent(NavMeshAgent agent)
        {
            float catSpeed = _gameConfig != null ? _gameConfig.PetMoveSpeed : 6f;
            agent.speed = catSpeed;
            agent.angularSpeed = 360f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 0.5f;
            agent.radius = 0.3f;
            agent.height = 0.6f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agent.autoTraverseOffMeshLink = true; // Cats can climb
            agent.autoBraking = true;
        }
        
        /// <summary>
        /// Configures the PetAI component for cat behavior.
        /// </summary>
        private void ConfigureCatPetAI(PetAI petAI)
        {
#if UNITY_EDITOR
            var serializedObj = new UnityEditor.SerializedObject(petAI);
            
            // Set pet type to Cat
            var petTypeProp = serializedObj.FindProperty("_petType");
            if (petTypeProp != null)
                petTypeProp.enumValueIndex = (int)PetAI.PetType.Cat;
            
            // Set game config
            var gameConfigProp = serializedObj.FindProperty("_gameConfig");
            if (gameConfigProp != null && _gameConfig != null)
                gameConfigProp.objectReferenceValue = _gameConfig;
            
            // Set phase 2 config
            var phase2ConfigProp = serializedObj.FindProperty("_phase2Config");
            if (phase2ConfigProp != null && _phase2Config != null)
                phase2ConfigProp.objectReferenceValue = _phase2Config;
            
            // Get renderer for visual effects
            var rendererProp = serializedObj.FindProperty("_petRenderer");
            if (rendererProp != null)
            {
                Renderer petRenderer = petAI.GetComponentInChildren<Renderer>();
                if (petRenderer != null)
                    rendererProp.objectReferenceValue = petRenderer;
            }
            
            serializedObj.ApplyModifiedProperties();
#endif
        }
        
        /// <summary>
        /// Configures the CatSkillManager with all three cat skills.
        /// Requirement 4.1: Cat has 3 skills.
        /// </summary>
        private void ConfigureCatSkillManager(CatSkillManager skillManager, GameObject cat)
        {
#if UNITY_EDITOR
            var serializedObj = new UnityEditor.SerializedObject(skillManager);
            
            // Set game config
            var configProp = serializedObj.FindProperty("GameConfig");
            if (configProp != null && _phase2Config != null)
                configProp.objectReferenceValue = _phase2Config;
            
            serializedObj.ApplyModifiedProperties();
#endif
            
            // Skills will be auto-created by CatSkillManager.InitializeSkills()
        }
        
        #endregion


        #region Groomer Prefab Update - Task 17.4
        
        /// <summary>
        /// Creates/updates a Groomer prefab template with GroomerSkillManager.
        /// Requirements: 3.1
        /// - Adds GroomerSkillManager
        /// - Configures skill prefabs
        /// </summary>
        [ContextMenu("Create Groomer Template")]
        public GameObject CreateGroomerTemplate()
        {
            // Create groomer object
            GameObject groomer = new GameObject("Groomer_Template");
            groomer.transform.position = Vector3.zero;
            groomer.tag = "Player";
            groomer.layer = LayerMask.NameToLayer("Default");
            
            // Create groomer visual representation
            CreateGroomerVisuals(groomer.transform);
            
            // Add CharacterController
            CharacterController charController = groomer.AddComponent<CharacterController>();
            charController.center = new Vector3(0f, 1f, 0f);
            charController.radius = 0.5f;
            charController.height = 2f;
            
            // Add GroomerController
            GroomerController groomerController = groomer.AddComponent<GroomerController>();
            ConfigureGroomerController(groomerController);
            
            // Add PlayerMovement for input handling
            PlayerMovement playerMovement = groomer.AddComponent<PlayerMovement>();
            ConfigurePlayerMovement(playerMovement);
            
            // Add GroomerSkillManager - Requirement 3.1
            GroomerSkillManager skillManager = groomer.AddComponent<GroomerSkillManager>();
            ConfigureGroomerSkillManager(skillManager, groomer);
            
            // Create pet hold point
            GameObject holdPoint = new GameObject("PetHoldPoint");
            holdPoint.transform.SetParent(groomer.transform);
            holdPoint.transform.localPosition = new Vector3(0f, 1f, 0.5f);
            
            Debug.Log("[Phase2PrefabSetup] Groomer template created with PlayerMovement and GroomerSkillManager.");
            return groomer;
        }
        
        /// <summary>
        /// Creates the visual representation of the groomer.
        /// </summary>
        private void CreateGroomerVisuals(Transform parent)
        {
            // Body (capsule)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(parent);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);
            body.transform.localScale = new Vector3(0.6f, 0.8f, 0.4f);
            DestroyImmediate(body.GetComponent<Collider>());
            ApplyMaterial(body, _groomerMaterial);
            
            // Head (sphere)
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(parent);
            head.transform.localPosition = new Vector3(0f, 1.9f, 0f);
            head.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            DestroyImmediate(head.GetComponent<Collider>());
            ApplyMaterial(head, _groomerMaterial);
            
            // Arms
            for (int i = 0; i < 2; i++)
            {
                GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                arm.name = $"Arm_{(i == 0 ? "Left" : "Right")}";
                arm.transform.SetParent(parent);
                arm.transform.localPosition = new Vector3((i == 0 ? -0.45f : 0.45f), 1.2f, 0f);
                arm.transform.localScale = new Vector3(0.15f, 0.4f, 0.15f);
                arm.transform.localRotation = Quaternion.Euler(0f, 0f, (i == 0 ? 15f : -15f));
                DestroyImmediate(arm.GetComponent<Collider>());
                ApplyMaterial(arm, _groomerMaterial);
            }
            
            // Legs
            for (int i = 0; i < 2; i++)
            {
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                leg.name = $"Leg_{(i == 0 ? "Left" : "Right")}";
                leg.transform.SetParent(parent);
                leg.transform.localPosition = new Vector3((i == 0 ? -0.15f : 0.15f), 0.4f, 0f);
                leg.transform.localScale = new Vector3(0.18f, 0.4f, 0.18f);
                DestroyImmediate(leg.GetComponent<Collider>());
                ApplyMaterial(leg, _groomerMaterial);
            }
            
            // Apron (groomer uniform)
            GameObject apron = GameObject.CreatePrimitive(PrimitiveType.Cube);
            apron.name = "Apron";
            apron.transform.SetParent(parent);
            apron.transform.localPosition = new Vector3(0f, 1f, 0.22f);
            apron.transform.localScale = new Vector3(0.5f, 0.8f, 0.05f);
            DestroyImmediate(apron.GetComponent<Collider>());
            Renderer apronRenderer = apron.GetComponent<Renderer>();
            if (apronRenderer != null)
                apronRenderer.material.color = Color.white;
        }
        
        /// <summary>
        /// Configures the GroomerController component.
        /// </summary>
        private void ConfigureGroomerController(GroomerController controller)
        {
#if UNITY_EDITOR
            var serializedObj = new UnityEditor.SerializedObject(controller);
            
            // Set game config
            var configProp = serializedObj.FindProperty("_gameConfig");
            if (configProp != null && _gameConfig != null)
                configProp.objectReferenceValue = _gameConfig;
            
            serializedObj.ApplyModifiedProperties();
#endif
        }
        
        /// <summary>
        /// Configures the PlayerMovement component.
        /// </summary>
        private void ConfigurePlayerMovement(PlayerMovement playerMovement)
        {
#if UNITY_EDITOR
            var serializedObj = new UnityEditor.SerializedObject(playerMovement);
            
            // Set game config
            var configProp = serializedObj.FindProperty("_gameConfig");
            if (configProp != null && _gameConfig != null)
                configProp.objectReferenceValue = _gameConfig;
            
            // Set camera reference
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                var cameraProp = serializedObj.FindProperty("_cameraTransform");
                if (cameraProp != null)
                    cameraProp.objectReferenceValue = mainCamera.transform;
            }
            
            serializedObj.ApplyModifiedProperties();
#endif
        }
        
        /// <summary>
        /// Configures the GroomerSkillManager with all three groomer skills.
        /// Requirement 3.1: Groomer has 3 skills.
        /// </summary>
        private void ConfigureGroomerSkillManager(GroomerSkillManager skillManager, GameObject groomer)
        {
#if UNITY_EDITOR
            var serializedObj = new UnityEditor.SerializedObject(skillManager);
            
            // Set game config
            var configProp = serializedObj.FindProperty("GameConfig");
            if (configProp != null && _phase2Config != null)
                configProp.objectReferenceValue = _phase2Config;
            
            serializedObj.ApplyModifiedProperties();
#endif
            
            // Skills will be auto-created by GroomerSkillManager.InitializeSkills()
        }
        
        #endregion


        #region Scene Configuration - Task 17.5
        
        /// <summary>
        /// Configures the game scene with all Phase 2 elements.
        /// Requirements: 1.4, 9.3
        /// - Places pet cages
        /// - Configures multi-pet spawn points
        /// - Configures camera boundaries
        /// </summary>
        [ContextMenu("Configure Phase 2 Scene")]
        public void ConfigurePhase2Scene()
        {
            CreatePetCages();
            CreateSpawnPoints();
            ConfigureCameraBoundaries();
            CreatePhase2GameSystems();
            
            Debug.Log("[Phase2PrefabSetup] Phase 2 scene configuration complete.");
        }
        
        /// <summary>
        /// Creates pet cages in the scene.
        /// Requirement 1.4: Scene contains at least 1 Pet_Cage.
        /// </summary>
        [ContextMenu("Create Pet Cages")]
        public void CreatePetCages()
        {
            // Create parent object for cages
            GameObject cagesParent = GameObject.Find("PetCages");
            if (cagesParent == null)
            {
                cagesParent = new GameObject("PetCages");
            }
            
            // Create 2 pet cages at strategic positions
            Vector3[] cagePositions = new Vector3[]
            {
                new Vector3(-8f, 0f, 5f),
                new Vector3(8f, 0f, 5f)
            };
            
            for (int i = 0; i < cagePositions.Length; i++)
            {
                GameObject cage = CreatePetCageTemplate();
                cage.name = $"PetCage_{i}";
                cage.transform.SetParent(cagesParent.transform);
                cage.transform.position = cagePositions[i];
            }
            
            Debug.Log($"[Phase2PrefabSetup] Created {cagePositions.Length} pet cages.");
        }
        
        /// <summary>
        /// Creates spawn points for multi-pet mode.
        /// Requirements: 1.1, 1.2
        /// </summary>
        [ContextMenu("Create Spawn Points")]
        public void CreateSpawnPoints()
        {
            // Create parent object for spawn points
            GameObject spawnParent = GameObject.Find("PetSpawnPoints");
            if (spawnParent == null)
            {
                spawnParent = new GameObject("PetSpawnPoints");
            }
            
            // Create spawn points for up to 3 pets
            Vector3[] spawnPositions = new Vector3[]
            {
                new Vector3(-10f, 0f, -8f),
                new Vector3(10f, 0f, -8f),
                new Vector3(0f, 0f, -12f)
            };
            
            Transform[] spawnTransforms = new Transform[spawnPositions.Length];
            
            for (int i = 0; i < spawnPositions.Length; i++)
            {
                GameObject spawnPoint = new GameObject($"SpawnPoint_{i}");
                spawnPoint.transform.SetParent(spawnParent.transform);
                spawnPoint.transform.position = spawnPositions[i];
                spawnTransforms[i] = spawnPoint.transform;
                
                // Add visual marker (editor only)
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = "Marker";
                marker.transform.SetParent(spawnPoint.transform);
                marker.transform.localPosition = Vector3.zero;
                marker.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                DestroyImmediate(marker.GetComponent<Collider>());
                Renderer markerRenderer = marker.GetComponent<Renderer>();
                if (markerRenderer != null)
                    markerRenderer.material.color = Color.cyan;
            }
            
            // Configure PetSpawnManager if it exists
            PetSpawnManager spawnManager = FindObjectOfType<PetSpawnManager>();
            if (spawnManager != null)
            {
#if UNITY_EDITOR
                var serializedObj = new UnityEditor.SerializedObject(spawnManager);
                var spawnPointsProp = serializedObj.FindProperty("_spawnPoints");
                if (spawnPointsProp != null)
                {
                    spawnPointsProp.arraySize = spawnTransforms.Length;
                    for (int i = 0; i < spawnTransforms.Length; i++)
                    {
                        spawnPointsProp.GetArrayElementAtIndex(i).objectReferenceValue = spawnTransforms[i];
                    }
                    serializedObj.ApplyModifiedProperties();
                }
#endif
            }
            
            Debug.Log($"[Phase2PrefabSetup] Created {spawnPositions.Length} spawn points.");
        }
        
        /// <summary>
        /// Configures camera boundaries for the scene.
        /// Requirement 9.3: Camera clamps to prevent showing out-of-bounds areas.
        /// </summary>
        [ContextMenu("Configure Camera Boundaries")]
        public void ConfigureCameraBoundaries()
        {
            CameraController cameraController = FindObjectOfType<CameraController>();
            if (cameraController == null)
            {
                // Create camera controller on main camera
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    cameraController = mainCamera.gameObject.AddComponent<CameraController>();
                }
                else
                {
                    Debug.LogWarning("[Phase2PrefabSetup] No main camera found to configure.");
                    return;
                }
            }
            
            // Set scene bounds
            Bounds sceneBounds = new Bounds(
                Vector3.zero,
                new Vector3(40f, 20f, 40f)
            );
            
            cameraController.SetSceneBounds(sceneBounds);
            
#if UNITY_EDITOR
            var serializedObj = new UnityEditor.SerializedObject(cameraController);
            
            // Set Phase 2 config
            var configProp = serializedObj.FindProperty("_phase2Config");
            if (configProp != null && _phase2Config != null)
                configProp.objectReferenceValue = _phase2Config;
            
            // Find groomer as target
            GroomerController groomer = FindObjectOfType<GroomerController>();
            if (groomer != null)
            {
                var targetProp = serializedObj.FindProperty("_target");
                if (targetProp != null)
                    targetProp.objectReferenceValue = groomer.transform;
            }
            
            serializedObj.ApplyModifiedProperties();
#endif
            
            Debug.Log("[Phase2PrefabSetup] Camera boundaries configured.");
        }
        
        /// <summary>
        /// Creates Phase 2 game systems (PetSpawnManager, AlertSystem, etc.).
        /// </summary>
        [ContextMenu("Create Phase 2 Game Systems")]
        public void CreatePhase2GameSystems()
        {
            // Create PetSpawnManager if not exists
            if (FindObjectOfType<PetSpawnManager>() == null)
            {
                GameObject spawnManagerObj = new GameObject("PetSpawnManager");
                PetSpawnManager spawnManager = spawnManagerObj.AddComponent<PetSpawnManager>();
                
#if UNITY_EDITOR
                var serializedObj = new UnityEditor.SerializedObject(spawnManager);
                var configProp = serializedObj.FindProperty("_phase2Config");
                if (configProp != null && _phase2Config != null)
                    configProp.objectReferenceValue = _phase2Config;
                serializedObj.ApplyModifiedProperties();
#endif
            }
            
            // Create AlertSystem if not exists
            if (FindObjectOfType<AlertSystem>() == null)
            {
                GameObject alertSystemObj = new GameObject("AlertSystem");
                AlertSystem alertSystem = alertSystemObj.AddComponent<AlertSystem>();
                
#if UNITY_EDITOR
                var serializedObj = new UnityEditor.SerializedObject(alertSystem);
                var configProp = serializedObj.FindProperty("_phase2Config");
                if (configProp != null && _phase2Config != null)
                    configProp.objectReferenceValue = _phase2Config;
                serializedObj.ApplyModifiedProperties();
#endif
            }
            
            Debug.Log("[Phase2PrefabSetup] Phase 2 game systems created.");
        }
        
        #endregion

        #region Utility Methods
        
        /// <summary>
        /// Creates all Phase 2 prefab templates.
        /// </summary>
        [ContextMenu("Create All Phase 2 Templates")]
        public void CreateAllPhase2Templates()
        {
            CreatePetCageTemplate();
            CreateDogTemplate();
            CreateCatTemplate();
            CreateGroomerTemplate();
            
            Debug.Log("[Phase2PrefabSetup] All Phase 2 prefab templates created.");
        }
        
        /// <summary>
        /// Applies material to a game object if available.
        /// </summary>
        private void ApplyMaterial(GameObject obj, Material material)
        {
            if (material != null)
            {
                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = material;
                }
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw scene bounds
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(40f, 20f, 40f));
            
            // Draw cage positions
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(new Vector3(-8f, 0f, 5f), 1f);
            Gizmos.DrawWireSphere(new Vector3(8f, 0f, 5f), 1f);
            
            // Draw spawn positions
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(new Vector3(-10f, 0f, -8f), 0.5f);
            Gizmos.DrawWireSphere(new Vector3(10f, 0f, -8f), 0.5f);
            Gizmos.DrawWireSphere(new Vector3(0f, 0f, -12f), 0.5f);
        }
        
        #endregion
    }
}
