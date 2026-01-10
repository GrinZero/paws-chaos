using UnityEngine;
using UnityEditor;
using PetGrooming.Utils;

namespace PetGrooming.Setup.Editor
{
    /// <summary>
    /// 移动端打包配置工具。
    /// 一键设置横屏、调试等配置。
    /// </summary>
    public class MobileBuildSetup : EditorWindow
    {
        [MenuItem("PetGrooming/Mobile Build Setup (移动端打包设置)")]
        public static void ShowWindow()
        {
            GetWindow<MobileBuildSetup>("Mobile Build Setup (移动端打包设置)");
        }
        
        [MenuItem("PetGrooming/Setup Landscape Mode (一键配置横屏)")]
        public static void SetupLandscapeMode()
        {
            // Android 设置
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            
            UnityEngine.Debug.Log("[MobileBuildSetup] 已设置为横屏模式 (Landscape)");
            EditorUtility.DisplayDialog("设置完成", "已配置为横屏模式！\n\n- 默认方向: Landscape Left\n- 允许自动旋转: 仅横屏", "确定");
        }
        
        [MenuItem("PetGrooming/Add Debug Overlay (添加调试覆盖层到场景)")]
        public static void AddDebugOverlay()
        {
            // 检查是否已存在
            var existing = Object.FindFirstObjectByType<MobileDebugOverlay>();
            if (existing != null)
            {
                UnityEngine.Debug.LogWarning("[MobileBuildSetup] 场景中已存在 MobileDebugOverlay!");
                Selection.activeGameObject = existing.gameObject;
                return;
            }
            
            // 创建新的 GameObject
            GameObject debugObj = new GameObject("MobileDebugOverlay");
            debugObj.AddComponent<MobileDebugOverlay>();
            
            // 标记为 DontDestroyOnLoad（可选）
            // 如果需要跨场景保持，取消下面的注释
            // debugObj.AddComponent<DontDestroyOnLoadHelper>();
            
            Undo.RegisterCreatedObjectUndo(debugObj, "Add Mobile Debug Overlay");
            Selection.activeGameObject = debugObj;
            
            UnityEngine.Debug.Log("[MobileBuildSetup] 已添加 MobileDebugOverlay 到场景");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Mobile Build Setup (移动端打包配置)", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            // 横屏设置
            GUILayout.Label("Screen Orientation (屏幕方向)", EditorStyles.boldLabel);
            if (GUILayout.Button("Setup Landscape Mode (设置为横屏模式)", GUILayout.Height(30)))
            {
                SetupLandscapeMode();
            }
            
            GUILayout.Space(10);
            
            // 当前设置显示
            EditorGUILayout.HelpBox(
                $"Current Settings (当前设置):\n" +
                $"Default Orientation (默认方向): {PlayerSettings.defaultInterfaceOrientation}\n" +
                $"Allow Portrait (允许竖屏): {PlayerSettings.allowedAutorotateToPortrait}\n" +
                $"Allow Landscape Left (允许横屏左): {PlayerSettings.allowedAutorotateToLandscapeLeft}\n" +
                $"Allow Landscape Right (允许横屏右): {PlayerSettings.allowedAutorotateToLandscapeRight}",
                MessageType.Info);
            
            GUILayout.Space(10);
            
            // 调试工具
            GUILayout.Label("Debug Tools (调试工具)", EditorStyles.boldLabel);
            if (GUILayout.Button("Add Debug Overlay to Scene (添加调试覆盖层到场景)", GUILayout.Height(30)))
            {
                AddDebugOverlay();
            }
            
            GUILayout.Space(10);
            
            // Development Build 设置
            GUILayout.Label("Build Settings (打包设置)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Recommended for testing (建议测试时启用):\n" +
                "Build Settings → Development Build ✓\n" +
                "This shows more debug info on device (这样可以在设备上看到更多调试信息)",
                MessageType.Info);
            
            if (GUILayout.Button("Open Build Settings (打开打包设置)", GUILayout.Height(30)))
            {
                EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
            }
        }
    }
}
