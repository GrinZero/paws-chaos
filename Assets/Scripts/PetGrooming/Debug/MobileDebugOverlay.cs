using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace PetGrooming.Utils
{
    /// <summary>
    /// 移动端调试信息覆盖层。
    /// 在屏幕上显示输入状态、FPS、设备信息等。
    /// </summary>
    public class MobileDebugOverlay : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool _showDebugInfo = true;
        [SerializeField] private int _fontSize = 24;
        [SerializeField] private Color _textColor = Color.green;
        [SerializeField] private Color _backgroundColor = new Color(0, 0, 0, 0.7f);
        
        private GUIStyle _textStyle;
        private GUIStyle _boxStyle;
        private float _fps;
        private float _fpsUpdateInterval = 0.5f;
        private float _fpsAccumulator;
        private int _fpsFrames;
        private float _fpsTimeLeft;
        
        // 输入引用
        private StarterAssets.StarterAssetsInputs _starterInputs;
        private PlayerInput _playerInput;
        
        private void Start()
        {
            // 查找输入组件
            _starterInputs = FindObjectOfType<StarterAssets.StarterAssetsInputs>();
            _playerInput = FindObjectOfType<PlayerInput>();
            
            _fpsTimeLeft = _fpsUpdateInterval;
            
            UnityEngine.Debug.Log("[MobileDebugOverlay] 调试覆盖层已启动");
        }
        
        private void Update()
        {
            // 计算 FPS
            _fpsTimeLeft -= Time.deltaTime;
            _fpsAccumulator += Time.timeScale / Time.deltaTime;
            _fpsFrames++;
            
            if (_fpsTimeLeft <= 0f)
            {
                _fps = _fpsAccumulator / _fpsFrames;
                _fpsTimeLeft = _fpsUpdateInterval;
                _fpsAccumulator = 0f;
                _fpsFrames = 0;
            }
        }
        
        private void OnGUI()
        {
            if (!_showDebugInfo) return;
            
            // 初始化样式
            if (_textStyle == null)
            {
                _textStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = _fontSize,
                    normal = { textColor = _textColor }
                };
                
                _boxStyle = new GUIStyle(GUI.skin.box)
                {
                    normal = { background = MakeTexture(2, 2, _backgroundColor) }
                };
            }
            
            // 构建调试信息
            string debugText = BuildDebugText();
            
            // 计算文本大小
            GUIContent content = new GUIContent(debugText);
            Vector2 size = _textStyle.CalcSize(content);
            
            // 绘制背景和文本（左上角）
            float padding = 10f;
            Rect boxRect = new Rect(padding, padding, size.x + 20, size.y + 20);
            GUI.Box(boxRect, "", _boxStyle);
            GUI.Label(new Rect(padding + 10, padding + 10, size.x, size.y), debugText, _textStyle);
        }
        
        private string BuildDebugText()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            // 基本信息
            sb.AppendLine($"FPS: {_fps:F1}");
            sb.AppendLine($"分辨率: {Screen.width}x{Screen.height}");
            sb.AppendLine($"屏幕方向: {Screen.orientation}");
            sb.AppendLine($"触摸支持: {Input.touchSupported}");
            sb.AppendLine($"触摸数量: {Input.touchCount}");
            
            // PlayerInput 状态
            if (_playerInput != null)
            {
                sb.AppendLine($"控制方案: {_playerInput.currentControlScheme}");
                sb.AppendLine($"设备: {_playerInput.devices.Count}");
            }
            else
            {
                sb.AppendLine("PlayerInput: 未找到!");
            }
            
            // Gamepad 设备
            var gamepads = Gamepad.all;
            sb.AppendLine($"Gamepad 数量: {gamepads.Count}");
            
            // StarterAssetsInputs 状态
            if (_starterInputs != null)
            {
                sb.AppendLine($"Move: {_starterInputs.move}");
                sb.AppendLine($"Look: {_starterInputs.look}");
                sb.AppendLine($"Jump: {_starterInputs.jump}");
                sb.AppendLine($"Sprint: {_starterInputs.sprint}");
            }
            else
            {
                sb.AppendLine("StarterAssetsInputs: 未找到!");
            }
            
            // 触摸信息
            for (int i = 0; i < Input.touchCount && i < 3; i++)
            {
                Touch touch = Input.GetTouch(i);
                sb.AppendLine($"Touch{i}: {touch.position} ({touch.phase})");
            }
            
            return sb.ToString();
        }
        
        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            
            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
        
        /// <summary>
        /// 切换调试信息显示
        /// </summary>
        public void ToggleDebugInfo()
        {
            _showDebugInfo = !_showDebugInfo;
        }
    }
}
