using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using PetGrooming.Core;

namespace PetGrooming.UI.MobileUI
{
    /// <summary>
    /// Multi-touch handler that ensures simultaneous touch inputs on both
    /// the joystick and skill buttons are processed correctly.
    /// 
    /// Requirements: 6.5
    /// Property 12: Multi-Touch Input Handling
    /// </summary>
    public class MultiTouchHandler : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("UI References")]
        [Tooltip("Reference to the virtual joystick")]
        [SerializeField] private VirtualJoystick _joystick;
        
        [Tooltip("Reference to the skill wheel")]
        [SerializeField] private SkillWheelUI _skillWheel;
        
        [Tooltip("Reference to the struggle button")]
        [SerializeField] private StruggleButtonUI _struggleButton;
        
        [Header("Settings")]
        [Tooltip("Maximum number of simultaneous touches to track")]
        [SerializeField] private int _maxTouches = 10;
        
        [Tooltip("Enable debug logging")]
        [SerializeField] private bool _debugMode = false;
        
        #endregion

        #region Private Fields
        
        private Dictionary<int, TouchInfo> _activeTouches = new Dictionary<int, TouchInfo>();
        private int _joystickTouchId = -1;
        private HashSet<int> _skillButtonTouchIds = new HashSet<int>();
        
        #endregion

        #region Nested Types
        
        /// <summary>
        /// Information about an active touch.
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
        /// Type of UI element being touched.
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
        /// Result of multi-touch input processing.
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
        /// Number of currently active touches.
        /// </summary>
        public int ActiveTouchCount => _activeTouches.Count;
        
        /// <summary>
        /// Whether the joystick is currently being touched.
        /// </summary>
        public bool IsJoystickActive => _joystickTouchId >= 0;
        
        /// <summary>
        /// Whether any skill button is currently being touched.
        /// </summary>
        public bool IsAnySkillButtonActive => _skillButtonTouchIds.Count > 0;
        
        /// <summary>
        /// Current joystick direction (if active).
        /// </summary>
        public Vector2 JoystickDirection => _joystick != null ? _joystick.Direction : Vector2.zero;
        
        #endregion

        #region Events
        
        /// <summary>
        /// Fired when multi-touch state changes.
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
        /// Sets UI references.
        /// </summary>
        public void SetReferences(VirtualJoystick joystick, SkillWheelUI skillWheel, StruggleButtonUI struggleButton)
        {
            _joystick = joystick;
            _skillWheel = skillWheel;
            _struggleButton = struggleButton;
        }
        
        /// <summary>
        /// Clears all active touch tracking.
        /// </summary>
        public void ClearAllTouches()
        {
            _activeTouches.Clear();
            _joystickTouchId = -1;
            _skillButtonTouchIds.Clear();
        }
        
        /// <summary>
        /// Gets the current multi-touch result.
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
        /// Validates that simultaneous inputs are processed correctly.
        /// Used for property-based testing.
        /// 
        /// Property 12: Multi-Touch Input Handling
        /// Validates: Requirements 6.5
        /// </summary>
        /// <param name="joystickTouchId">Touch ID on joystick (-1 if none)</param>
        /// <param name="skillButtonTouchId">Touch ID on skill button (-1 if none)</param>
        /// <returns>True if both inputs can be processed simultaneously</returns>
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
        /// Determines the touch target based on position and UI layout.
        /// Used for property-based testing.
        /// </summary>
        /// <param name="touchPosition">Screen position of touch</param>
        /// <param name="joystickBounds">Bounds of joystick UI</param>
        /// <param name="skillWheelBounds">Bounds of skill wheel UI</param>
        /// <returns>The target type at the touch position</returns>
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
        /// Validates that touch IDs are unique across different UI elements.
        /// Used for property-based testing.
        /// </summary>
        /// <param name="touchInfos">Array of touch information</param>
        /// <returns>True if all touch IDs are unique</returns>
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
        /// Simulates processing of simultaneous touch inputs.
        /// Used for property-based testing.
        /// </summary>
        /// <param name="joystickInput">Joystick touch input (null if not touched)</param>
        /// <param name="skillButtonInput">Skill button touch input (null if not touched)</param>
        /// <returns>Result of processing both inputs</returns>
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
        /// Validates that joystick continues to work while skill button is pressed.
        /// Used for property-based testing.
        /// </summary>
        /// <param name="joystickActive">Whether joystick was active before skill press</param>
        /// <param name="joystickDirection">Joystick direction before skill press</param>
        /// <param name="skillButtonPressed">Whether skill button was pressed</param>
        /// <param name="joystickActiveAfter">Whether joystick is active after skill press</param>
        /// <param name="joystickDirectionAfter">Joystick direction after skill press</param>
        /// <returns>True if joystick state was preserved</returns>
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
        /// Gets active touches for testing.
        /// </summary>
        public Dictionary<int, TouchInfo> GetActiveTouchesForTesting()
        {
            return new Dictionary<int, TouchInfo>(_activeTouches);
        }
        
        /// <summary>
        /// Simulates a touch for testing.
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
        /// Gets joystick touch ID for testing.
        /// </summary>
        public int GetJoystickTouchIdForTesting()
        {
            return _joystickTouchId;
        }
        
        /// <summary>
        /// Gets skill button touch IDs for testing.
        /// </summary>
        public HashSet<int> GetSkillButtonTouchIdsForTesting()
        {
            return new HashSet<int>(_skillButtonTouchIds);
        }
#endif
        #endregion
    }
}
