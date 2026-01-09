#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace PetGrooming.Setup.Editor
{
    /// <summary>
    /// Custom editor for Phase2PrefabSetup providing menu items and inspector buttons.
    /// </summary>
    [CustomEditor(typeof(Phase2PrefabSetup))]
    public class Phase2PrefabSetupEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            Phase2PrefabSetup setup = (Phase2PrefabSetup)target;
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Prefab Creation", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Pet Cage"))
            {
                setup.CreatePetCageTemplate();
            }
            if (GUILayout.Button("Create Dog"))
            {
                setup.CreateDogTemplate();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Cat"))
            {
                setup.CreateCatTemplate();
            }
            if (GUILayout.Button("Create Groomer"))
            {
                setup.CreateGroomerTemplate();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            if (GUILayout.Button("Create All Phase 2 Templates", GUILayout.Height(30)))
            {
                setup.CreateAllPhase2Templates();
            }
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Scene Configuration", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Pet Cages"))
            {
                setup.CreatePetCages();
            }
            if (GUILayout.Button("Create Spawn Points"))
            {
                setup.CreateSpawnPoints();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Configure Camera"))
            {
                setup.ConfigureCameraBoundaries();
            }
            if (GUILayout.Button("Create Game Systems"))
            {
                setup.CreatePhase2GameSystems();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            if (GUILayout.Button("Configure Complete Phase 2 Scene", GUILayout.Height(30)))
            {
                setup.ConfigurePhase2Scene();
            }
        }
        
        /// <summary>
        /// Menu item to create Phase 2 prefabs.
        /// </summary>
        [MenuItem("PetGrooming/Phase 2/Create All Prefab Templates")]
        public static void CreateAllPrefabTemplates()
        {
            Phase2PrefabSetup setup = FindOrCreateSetup();
            setup.CreateAllPhase2Templates();
        }
        
        /// <summary>
        /// Menu item to create Pet Cage template.
        /// </summary>
        [MenuItem("PetGrooming/Phase 2/Create Pet Cage Template")]
        public static void CreatePetCageTemplate()
        {
            Phase2PrefabSetup setup = FindOrCreateSetup();
            setup.CreatePetCageTemplate();
        }
        
        /// <summary>
        /// Menu item to create Dog template.
        /// </summary>
        [MenuItem("PetGrooming/Phase 2/Create Dog Template")]
        public static void CreateDogTemplate()
        {
            Phase2PrefabSetup setup = FindOrCreateSetup();
            setup.CreateDogTemplate();
        }
        
        /// <summary>
        /// Menu item to create Cat template.
        /// </summary>
        [MenuItem("PetGrooming/Phase 2/Create Cat Template")]
        public static void CreateCatTemplate()
        {
            Phase2PrefabSetup setup = FindOrCreateSetup();
            setup.CreateCatTemplate();
        }
        
        /// <summary>
        /// Menu item to create Groomer template.
        /// </summary>
        [MenuItem("PetGrooming/Phase 2/Create Groomer Template")]
        public static void CreateGroomerTemplate()
        {
            Phase2PrefabSetup setup = FindOrCreateSetup();
            setup.CreateGroomerTemplate();
        }
        
        /// <summary>
        /// Menu item to configure Phase 2 scene.
        /// </summary>
        [MenuItem("PetGrooming/Phase 2/Configure Phase 2 Scene")]
        public static void ConfigurePhase2Scene()
        {
            Phase2PrefabSetup setup = FindOrCreateSetup();
            setup.ConfigurePhase2Scene();
        }
        
        /// <summary>
        /// Finds existing Phase2PrefabSetup or creates a temporary one.
        /// </summary>
        private static Phase2PrefabSetup FindOrCreateSetup()
        {
            Phase2PrefabSetup setup = Object.FindObjectOfType<Phase2PrefabSetup>();
            if (setup == null)
            {
                GameObject setupObj = new GameObject("Phase2PrefabSetup_Temp");
                setup = setupObj.AddComponent<Phase2PrefabSetup>();
                
                // Try to find and assign configs
                string[] configGuids = AssetDatabase.FindAssets("t:Phase2GameConfig");
                if (configGuids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(configGuids[0]);
                    var config = AssetDatabase.LoadAssetAtPath<Core.Phase2GameConfig>(path);
                    if (config != null)
                    {
                        var serializedObj = new SerializedObject(setup);
                        var configProp = serializedObj.FindProperty("_phase2Config");
                        if (configProp != null)
                        {
                            configProp.objectReferenceValue = config;
                            serializedObj.ApplyModifiedProperties();
                        }
                    }
                }
                
                string[] gameConfigGuids = AssetDatabase.FindAssets("t:GameConfig");
                if (gameConfigGuids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(gameConfigGuids[0]);
                    var config = AssetDatabase.LoadAssetAtPath<Core.GameConfig>(path);
                    if (config != null)
                    {
                        var serializedObj = new SerializedObject(setup);
                        var configProp = serializedObj.FindProperty("_gameConfig");
                        if (configProp != null)
                        {
                            configProp.objectReferenceValue = config;
                            serializedObj.ApplyModifiedProperties();
                        }
                    }
                }
                
                Debug.Log("[Phase2PrefabSetupEditor] Created temporary Phase2PrefabSetup. Delete after use.");
            }
            return setup;
        }
    }
}
#endif
