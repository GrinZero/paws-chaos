using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using PetGrooming.Core;

namespace PetGrooming.UI.MobileUI
{
    /// <summary>
    /// 多点触控处理器，确保同时在摇杆和技能按钮上的触摸输入都能正确处理。
    /// 
    /// 需求：6.5
    /// 属性 12: 多点触控输入处理
    /// </summary>
    public class MultiTouchHandler : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("UI引用")]
        [Tooltip("对虚拟摇杆的引用")]
        [SerializeField] private VirtualJoystick _joystick;
        
        [Tooltip("对技能轮的引用")]
        [SerializeField] private SkillWheelUI _skillWheel;
        
        [Tooltip("对挣扎按钮的引用")]
        [SerializeField] private StruggleButtonUI _struggleButton;
        
        [Header("设置")]
        [Tooltip("要跟踪的最大同时触摸数")]
        [SerializeField] private int _maxTouches = 10;
        
        [Tooltip("启用调试日志")]
        [SerializeField] private bool _debugMode = false;
        
        #endregion

        #region Private Fields
        
        private Dictionary<int, TouchInfo> _activeTouches = new Dictionary<int, TouchInfo>();
        private int _joystickTouchId = -1;
        private HashSet<int> _skillButtonTouchIds = new HashSet<int>();
        
        #endregion

        #region Nested Types
        
        /// <summary>
        /// 关于活动触摸的信息。
        /// </summary>
        public struct TouchInfo
        {
            public int PointerId;
            public Vector2 Position;
            public TouchTarget Target;
            public float StartTime;
            
            public TouchInfo(int pointerId, Vector2 position, TouchTarget target)
            {
                PointerId = pointerId;
                Position = position;
                Target = target;
                StartTime = Time.time;
            }
        }
        
        /// <summary>
        /// 正在触摸的UI元素类型。
        /// </summary>
        public enum TouchTarget
        {
            None,
            Joystick,
            SkillButton,
            StruggleButton,
            Other
        }
        
        /// <summary>
        /// 多点触控输入处理的结果。
        /// </summary>
        public struct MultiTouchResult
        {
            public bool JoystickActive;
            public Vector2 JoystickDirection;
            public bool SkillButtonPressed;
            public int SkillButtonIndex;
            public bool StruggleButtonPressed;
            
            public static MultiTouchResult Empty => new MultiTouchResult
            {
                JoystickActive = false,
                JoystickDirection = Vector2.zero,
                SkillButtonPressed = false,
                SkillButtonIndex = -1,
                StruggleButtonPressed = false
            };
        }
        
        #endregion

        #region Properties
        
        /// <summary>
        /// 当前活动触摸的数量。
        /// </summary>
        public int ActiveTouchCount => _activeTouches.Count;
        
        /// <summary>
        /// 摇杆当前是否被触摸。
        /// </summary>
        public bool IsJoystickActive => _joystickTouchId >= 0;
        
        /// <summary>
        /// 是否有任何技能按钮当前被触摸。
        /// </summary>
        public bool IsAnySkillButtonActive => _skillButtonTouchIds.Count > 0;
        
        /// <summary>
        /// 当前摇杆方向（如果活动）。
        /// </summary>
        public Vector2 JoystickDirection => _joystick != null ? _joystick.Direction : Vector2.zero;
        
        #endregion

        #region Events
        
        /// <summary>
        /// 当多点触控状态改变时触发。
        /// </summary>
        public event Action<MultiTouchResult> OnMultiTouchStateChanged;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            _activeTouches = new Dictionary<int, TouchInfo>(_maxTouches);
            _skillButtonTouchIds = new HashSet<int>();
        }
        
        private void Update()
        {
            // Process touch input
            ProcessTouchInput();
        }
        
        private void OnDisable()
        {
            // Clear all active touches
            ClearAllTouches();
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// 设置UI引用。
        /// </summary>
        public void SetReferences(VirtualJoystick joystick, SkillWheelUI skillWheel, StruggleButtonUI struggleButton)
        {
            _joystick = joystick;
            _skillWheel = skillWheel;
            _struggleButton = struggleButton;
        }
        
        /// <summary>
        /// 清除所有活动触摸跟踪。
        /// </summary>
        public void ClearAllTouches()
        {
            _activeTouches.Clear();
            _joystickTouchId = -1;
            _skillButtonTouchIds.Clear();
        }
        
        /// <summary>
        /// 获取当前多点触控结果。
        /// </summary>
        public MultiTouchResult GetCurrentState()
        {
            return new MultiTouchResult
            {
                JoystickActive = IsJoystickActive,
                JoystickDirection = JoystickDirection,
                SkillButtonPressed = IsAnySkillButtonActive,
                SkillButtonIndex = -1, // Would need to track which button
                StruggleButtonPressed = false // Would need to track
            };
        }
        
        #endregion

        #region Static Methods (for testing)
        
        /// <summary>
        /// 验证同时输入是否被正确处理。
        /// 用于基于属性的测试。
        /// 
        /// 属性 12: 多点触控输入处理
        /// 验证: 需求 6.5
        /// </summary>
        /// <param name="joystickTouchId">摇杆上的触摸ID（无则为-1）</param>
        /// <param name="skillButtonTouchId">技能按钮上的触摸ID（无则为-1）</param>
        /// <returns>如果两个输入可以同时处理则为true</returns>
        public static bool ValidateSimultaneousInputs(int joystickTouchId, int skillButtonTouchId)
        {
            // Both inputs should be processable if they have different touch IDs
            // or if one of them is not active (-1)
            if (joystickTouchId == -1 || skillButtonTouchId == -1)
            {
                return true; // One or both not active, no conflict
            }
            
            // Both active - they should have different IDs
            return joystickTouchId != skillButtonTouchId;
        }
        
        /// <summary>
        /// 根据位置和UI布局确定触摸目标。
        /// 用于基于属性的测试。
        /// </summary>
        /// <param name="touchPosition">触摸的屏幕位置</param>
        /// <param name="joystickBounds">摇杆UI的边界</param>
        /// <param name="skillWheelBounds">技能轮UI的边界</param>
        /// <returns>触摸位置的目标类型</returns>
        public static TouchTarget DetermineTouchTarget(
            Vector2 touchPosition, 
            Rect joystickBounds, 
            Rect skillWheelBounds)
        {
            if (joystickBounds.Contains(touchPosition))
            {
                return TouchTarget.Joystick;
            }
            
            if (skillWheelBounds.Contains(touchPosition))
            {
                return TouchTarget.SkillButton;
            }
            
            return TouchTarget.None;
        }
        
        /// <summary>
        /// 验证不同UI元素之间的触摸ID是否唯一。
        /// 用于基于属性的测试。
        /// </summary>
        /// <param name="touchInfos">触摸信息数组</param>
        /// <returns>如果所有触摸ID都唯一则为true</returns>
        public static bool ValidateTouchIdUniqueness(TouchInfo[] touchInfos)
        {
            if (touchInfos == null || touchInfos.Length <= 1)
            {
                return true;
            }
            
            HashSet<int> seenIds = new HashSet<int>();
            foreach (var touch in touchInfos)
            {
                if (touch.PointerId >= 0)
                {
                    if (seenIds.Contains(touch.PointerId))
                    {
                        return false; // Duplicate ID found
                    }
                    seenIds.Add(touch.PointerId);
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 模拟同时触摸输入的处理。
        /// 用于基于属性的测试。
        /// </summary>
        /// <param name="joystickInput">摇杆触摸输入（未触摸则为null）</param>
        /// <param name="skillButtonInput">技能按钮触摸输入（未触摸则为null）</param>
        /// <returns>处理两个输入的结果</returns>
        public static MultiTouchResult ProcessSimultaneousInputs(
            TouchInfo? joystickInput,
            TouchInfo? skillButtonInput)
        {
            var result = MultiTouchResult.Empty;
            
            // Process joystick input
            if (joystickInput.HasValue && joystickInput.Value.Target == TouchTarget.Joystick)
            {
                result.JoystickActive = true;
                // Direction would be calculated from position
            }
            
            // Process skill button input
            if (skillButtonInput.HasValue && skillButtonInput.Value.Target == TouchTarget.SkillButton)
            {
                result.SkillButtonPressed = true;
            }
            
            return result;
        }
        
        /// <summary>
        /// 验证当技能按钮被按下时摇杆是否继续工作。
        /// 用于基于属性的测试。
        /// </summary>
        /// <param name="joystickActive">技能按下前摇杆是否活动</param>
        /// <param name="joystickDirection">技能按下前摇杆方向</param>
        /// <param name="skillButtonPressed">技能按钮是否被按下</param>
        /// <param name="joystickActiveAfter">技能按下后摇杆是否活动</param>
        /// <param name="joystickDirectionAfter">技能按下后摇杆方向</param>
        /// <returns>如果摇杆状态被保留则为true</returns>
        public static bool ValidateJoystickPreservedDuringSkillPress(
            bool joystickActive,
            Vector2 joystickDirection,
            bool skillButtonPressed,
            bool joystickActiveAfter,
            Vector2 joystickDirectionAfter)
        {
            // If joystick was active before, it should remain active after skill press
            if (joystickActive && skillButtonPressed)
            {
                if (!joystickActiveAfter)
                {
                    return false; // Joystick should not be deactivated by skill press
                }
                
                // Direction should be preserved (within tolerance)
                float directionDiff = Vector2.Distance(joystickDirection, joystickDirectionAfter);
                if (directionDiff > 0.01f)
                {
                    return false; // Direction changed unexpectedly
                }
            }
            
            return true;
        }
        
        #endregion

        #region Private Methods
        
        private void ProcessTouchInput()
        {
            // Process Unity's touch input
            for (int i = 0; i < Input.touchCount && i < _maxTouches; i++)
            {
                Touch touch = Input.GetTouch(i);
                
                switch (touch.phase)
                {
                    case UnityEngine.TouchPhase.Began:
                        HandleTouchBegan(touch);
                        break;
                        
                    case UnityEngine.TouchPhase.Moved:
                    case UnityEngine.TouchPhase.Stationary:
                        HandleTouchMoved(touch);
                        break;
                        
                    case UnityEngine.TouchPhase.Ended:
                    case UnityEngine.TouchPhase.Canceled:
                        HandleTouchEnded(touch);
                        break;
                }
            }
            
            // Also handle mouse input for editor testing
            #if UNITY_EDITOR
            ProcessMouseInput();
            #endif
        }
        
        private void HandleTouchBegan(Touch touch)
        {
            // Determine what UI element was touched
            TouchTarget target = DetermineTouchTargetFromRaycast(touch.position);
            
            if (target == TouchTarget.None)
            {
                return;
            }
            
            // Create touch info
            TouchInfo info = new TouchInfo(touch.fingerId, touch.position, target);
            _activeTouches[touch.fingerId] = info;
            
            // Track by target type
            switch (target)
            {
                case TouchTarget.Joystick:
                    if (_joystickTouchId < 0)
                    {
                        _joystickTouchId = touch.fingerId;
                    }
                    break;
                    
                case TouchTarget.SkillButton:
                    _skillButtonTouchIds.Add(touch.fingerId);
                    break;
            }
            
            if (_debugMode)
            {
                Debug.Log($"[MultiTouchHandler] Touch began: ID={touch.fingerId}, Target={target}");
            }
            
            NotifyStateChanged();
        }
        
        private void HandleTouchMoved(Touch touch)
        {
            if (!_activeTouches.ContainsKey(touch.fingerId))
            {
                return;
            }
            
            // Update position
            TouchInfo info = _activeTouches[touch.fingerId];
            info.Position = touch.position;
            _activeTouches[touch.fingerId] = info;
        }
        
        private void HandleTouchEnded(Touch touch)
        {
            if (!_activeTouches.ContainsKey(touch.fingerId))
            {
                return;
            }
            
            TouchInfo info = _activeTouches[touch.fingerId];
            
            // Remove from tracking
            _activeTouches.Remove(touch.fingerId);
            
            // Update target-specific tracking
            if (touch.fingerId == _joystickTouchId)
            {
                _joystickTouchId = -1;
            }
            
            _skillButtonTouchIds.Remove(touch.fingerId);
            
            if (_debugMode)
            {
                Debug.Log($"[MultiTouchHandler] Touch ended: ID={touch.fingerId}, Target={info.Target}");
            }
            
            NotifyStateChanged();
        }
        
        private TouchTarget DetermineTouchTargetFromRaycast(Vector2 screenPosition)
        {
            // Use EventSystem to determine what was touched
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = screenPosition;
            
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            
            foreach (var result in results)
            {
                // Check if it's the joystick
                if (_joystick != null && result.gameObject.transform.IsChildOf(_joystick.transform))
                {
                    return TouchTarget.Joystick;
                }
                
                // Check if it's a skill button
                if (_skillWheel != null && result.gameObject.transform.IsChildOf(_skillWheel.transform))
                {
                    return TouchTarget.SkillButton;
                }
                
                // Check if it's the struggle button
                if (_struggleButton != null && result.gameObject.transform.IsChildOf(_struggleButton.transform))
                {
                    return TouchTarget.StruggleButton;
                }
            }
            
            return TouchTarget.None;
        }
        
        #if UNITY_EDITOR
        private void ProcessMouseInput()
        {
            // Simulate touch with mouse for editor testing using new Input System
            var mouse = Mouse.current;
            if (mouse == null) return;
            
            Vector2 mousePosition = mouse.position.ReadValue();
            
            if (mouse.leftButton.wasPressedThisFrame)
            {
                Touch simulatedTouch = new Touch
                {
                    fingerId = 0,
                    position = mousePosition,
                    phase = UnityEngine.TouchPhase.Began
                };
                HandleTouchBegan(simulatedTouch);
            }
            else if (mouse.leftButton.isPressed)
            {
                Touch simulatedTouch = new Touch
                {
                    fingerId = 0,
                    position = mousePosition,
                    phase = UnityEngine.TouchPhase.Moved
                };
                HandleTouchMoved(simulatedTouch);
            }
            else if (mouse.leftButton.wasReleasedThisFrame)
            {
                Touch simulatedTouch = new Touch
                {
                    fingerId = 0,
                    position = mousePosition,
                    phase = UnityEngine.TouchPhase.Ended
                };
                HandleTouchEnded(simulatedTouch);
            }
        }
        #endif
        
        private void NotifyStateChanged()
        {
            OnMultiTouchStateChanged?.Invoke(GetCurrentState());
        }
        
        #endregion

        #region Editor Support
#if UNITY_EDITOR
        /// <summary>
        /// 获取用于测试的活动触摸。
        /// </summary>
        public Dictionary<int, TouchInfo> GetActiveTouchesForTesting()
        {
            return new Dictionary<int, TouchInfo>(_activeTouches);
        }
        
        /// <summary>
        /// 为测试模拟触摸。
        /// </summary>
        public void SimulateTouchForTesting(int fingerId, Vector2 position, TouchTarget target, UnityEngine.TouchPhase phase)
        {
            Touch touch = new Touch
            {
                fingerId = fingerId,
                position = position,
                phase = phase
            };
            
            switch (phase)
            {
                case UnityEngine.TouchPhase.Began:
                    TouchInfo info = new TouchInfo(fingerId, position, target);
                    _activeTouches[fingerId] = info;
                    if (target == TouchTarget.Joystick && _joystickTouchId < 0)
                    {
                        _joystickTouchId = fingerId;
                    }
                    else if (target == TouchTarget.SkillButton)
                    {
                        _skillButtonTouchIds.Add(fingerId);
                    }
                    break;
                    
                case UnityEngine.TouchPhase.Ended:
                case UnityEngine.TouchPhase.Canceled:
                    HandleTouchEnded(touch);
                    break;
            }
        }
        
        /// <summary>
        /// 获取用于测试的摇杆触摸ID。
        /// </summary>
        public int GetJoystickTouchIdForTesting()
        {
            return _joystickTouchId;
        }
        
        /// <summary>
        /// 获取用于测试的技能按钮触摸ID。
        /// </summary>
        public HashSet<int> GetSkillButtonTouchIdsForTesting()
        {
            return new HashSet<int>(_skillButtonTouchIds);
        }
#endif
        #endregion
    }
}
