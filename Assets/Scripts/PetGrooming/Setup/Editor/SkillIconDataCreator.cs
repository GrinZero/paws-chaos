using UnityEngine;
using UnityEditor;

namespace PetGrooming.Setup.Editor
{
    /// <summary>
    /// Editor utility to create and configure SkillIconData ScriptableObject.
    /// Requirements: 3.5.7, 3.5.8
    /// </summary>
    public static class SkillIconDataCreator
    {
        private const string IconsPath = "Assets/UI/MobileUI/Icons";
        private const string OutputPath = "Assets/UI/MobileUI/SkillIconData.asset";

        [MenuItem("PetGrooming/Create Skill Icon Data Asset")]
        public static void CreateSkillIconDataAsset()
        {
            // Check if asset already exists
            var existingAsset = AssetDatabase.LoadAssetAtPath<Core.SkillIconData>(OutputPath);
            if (existingAsset != null)
            {
                Debug.Log("SkillIconData asset already exists. Updating icon references...");
                ConfigureSkillIconData(existingAsset);
                EditorUtility.SetDirty(existingAsset);
                AssetDatabase.SaveAssets();
                return;
            }

            // Create new SkillIconData instance
            var skillIconData = ScriptableObject.CreateInstance<Core.SkillIconData>();
            
            // Configure with icons
            ConfigureSkillIconData(skillIconData);

            // Save as asset
            AssetDatabase.CreateAsset(skillIconData, OutputPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"SkillIconData asset created at: {OutputPath}");
            
            // Select the created asset
            Selection.activeObject = skillIconData;
            EditorGUIUtility.PingObject(skillIconData);
        }

        private static void ConfigureSkillIconData(Core.SkillIconData data)
        {
            // Load and assign icons
            // Capture Net - Blue (#4A90D9)
            data.CaptureNet.SkillId = "capture_net";
            data.CaptureNet.Icon = LoadSprite("Icon_CaptureNet");
            data.CaptureNet.ThemeColor = new Color(0.29f, 0.56f, 0.85f, 1f);

            // Leash - Green (#5CB85C)
            data.Leash.SkillId = "leash";
            data.Leash.Icon = LoadSprite("Icon_Leash");
            data.Leash.ThemeColor = new Color(0.36f, 0.72f, 0.36f, 1f);

            // Calming Spray - Purple (#9B59B6)
            data.CalmingSpray.SkillId = "calming_spray";
            data.CalmingSpray.Icon = LoadSprite("Icon_CalmingSpray");
            data.CalmingSpray.ThemeColor = new Color(0.61f, 0.35f, 0.71f, 1f);

            // Capture Button - Gold (#F5A623)
            data.CaptureButton.SkillId = "capture";
            data.CaptureButton.Icon = LoadSprite("Icon_Capture");
            data.CaptureButton.ThemeColor = new Color(0.96f, 0.65f, 0.14f, 1f);

            // Struggle Button - Orange-Red (#E74C3C)
            data.StruggleButton.SkillId = "struggle";
            data.StruggleButton.Icon = LoadSprite("Icon_Struggle");
            data.StruggleButton.ThemeColor = new Color(0.91f, 0.30f, 0.24f, 1f);

            // Validate
            if (!data.ValidateIcons())
            {
                Debug.LogWarning("Some skill icons are missing. Please ensure all icons are generated first.");
            }
            else
            {
                Debug.Log("All skill icons configured successfully.");
            }
        }

        private static Sprite LoadSprite(string iconName)
        {
            string path = $"{IconsPath}/{iconName}.png";
            
            // Load all assets at path - sprites are sub-assets of the texture
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            
            foreach (Object asset in assets)
            {
                if (asset is Sprite sprite)
                {
                    return sprite;
                }
            }
            
            // If no sprite found, try loading as texture and check if it's imported as sprite
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null)
            {
                // The texture exists but may not be imported as sprite yet
                // Try to get the sprite from the texture
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                {
                    return sprite;
                }
                
                Debug.LogWarning($"Texture found but not imported as sprite: {path}. Please ensure TextureImporter is set to Sprite mode.");
            }
            else
            {
                Debug.LogWarning($"Could not load sprite at: {path}");
            }
            
            return null;
        }
    }
}
