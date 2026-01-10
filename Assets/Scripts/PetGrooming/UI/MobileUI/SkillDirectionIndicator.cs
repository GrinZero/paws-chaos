using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace PetGrooming.UI.MobileUI
{
    /// <summary>
    /// 技能方向指示器，支持拖动释放方向（类似王者荣耀）。
    /// 当玩家按住技能按钮并拖动时，显示方向指示器。
    /// 释放时触发技能并传递方向。
    /// </summary>
    public class SkillDirectionIndicator : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        #region Serialized Fields
        
        [Header("UI 引用")]
        [Tooltip("方向指示器箭头")]
        [SerializeField] private RectTransform _arrowIndicator;
        
        [Tooltip("范围圆圈")]
        [SerializeField] private RectTransform _rangeCircle;
        
        [Tooltip("方向线")]
        [SerializeField] private Image _directionLine;
        
        [Header("设置")]
        [Tooltip("最大拖动距离")]
        [SerializeField] private float _maxDragDistance = 100f;
        
        [Tooltip("最小触发距离（低于此距离不显示方向）")]
        [SerializeField] private float _minTriggerDistance = 20f;
        
        [Tooltip("是否需要方向（false = 点击即释放）")]
        [SerializeField] private bool _requiresDirection = true;
        
        [Tooltip("指示器颜色")]
        [SerializeField] private Color _indicatorColor = new Color(1f, 0.8f, 0.2f, 0.8f);
        
        [Tooltip("取消区域颜色")]
        [SerializeField] private Color _cancelColor = new Color(1f, 0.2f, 0.2f, 0.8f);
        
        #endregion

        #region Private Fields
        
        private RectTransform _rectTransform;
        private Canvas _canvas;
        private bool _isDragging;
        private Vector2 _startPosition;
        private Vector2 _currentDirection;
        private float _currentMagnitude;
        private int _currentPointerId = -1;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// 当前拖动方向（归一化）。
        /// </summary>
        public Vector2 Direction => _currentDirection;
        
        /// <summary>
        /// 当前拖动距离（0-1）。
        /// </summary>
        public float Magnitude => _currentMagnitude;
        
        /// <summary>
        /// 是否正在拖动。
        /// </summary>
        public bool IsDragging => _isDragging;
        
        /// <summary>
        /// 是否在取消区域（拖回按钮中心）。
        /// </summary>
        public bool IsInCancelZone => _currentMagnitude < _minTriggerDistance / _maxDragDistance;
        
        #endregion

        #region Events
        
        /// <summary>
        /// 技能释放事件，参数为方向向量。
        /// </summary>
        public event Action<Vector2> OnSkillReleased;
        
        /// <summary>
        /// 技能取消事件。
        /// </summary>
        public event Action OnSkillCancelled;
        
        /// <summary>
        /// 开始拖动事件。
        /// </summary>
        public event Action OnDragStarted;
        
        /// <summary>
        /// 方向更新事件。
        /// </summary>
        public event Action<Vector2> OnDirectionChanged;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
            
            HideIndicator();
        }
        
        private void OnDisable()
        {
            if (_isDragging)
            {
                CancelDrag();
            }
        }
        
        #endregion

        #region IPointerDownHandler
        
        public void OnPointerDown(PointerEventData eventData)
        {
            if (_isDragging) return;
            
            _isDragging = true;
            _currentPointerId = eventData.pointerId;
            _startPosition = eventData.position;
            
            if (_requiresDirection)
            {
                ShowIndicator();
                OnDragStarted?.Invoke();
            }
        }
        
        #endregion

        #region IDragHandler
        
        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging || eventData.pointerId != _currentPointerId) return;
            
            Vector2 delta = eventData.position - _startPosition;
            float distance = delta.magnitude;
            
            // 计算方向和距离
            if (distance > 0.001f)
            {
                _currentDirection = delta.normalized;
                _currentMagnitude = Mathf.Clamp01(distance / _maxDragDistance);
            }
            else
            {
                _currentDirection = Vector2.zero;
                _currentMagnitude = 0f;
            }
            
            // 更新指示器
            if (_requiresDirection)
            {
                UpdateIndicator(delta, distance);
            }
            
            OnDirectionChanged?.Invoke(_currentDirection);
        }
        
        #endregion

        #region IPointerUpHandler
        
        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isDragging || eventData.pointerId != _currentPointerId) return;
            
            _isDragging = false;
            _currentPointerId = -1;
            
            HideIndicator();
            
            // 判断是释放还是取消
            if (!_requiresDirection)
            {
                // 不需要方向，直接释放
                OnSkillReleased?.Invoke(Vector2.zero);
            }
            else if (IsInCancelZone)
            {
                // 在取消区域
                OnSkillCancelled?.Invoke();
            }
            else
            {
                // 正常释放
                OnSkillReleased?.Invoke(_currentDirection);
            }
            
            // 重置状态
            _currentDirection = Vector2.zero;
            _currentMagnitude = 0f;
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// 设置是否需要方向。
        /// </summary>
        public void SetRequiresDirection(bool requires)
        {
            _requiresDirection = requires;
        }
        
        /// <summary>
        /// 强制取消当前拖动。
        /// </summary>
        public void CancelDrag()
        {
            if (!_isDragging) return;
            
            _isDragging = false;
            _currentPointerId = -1;
            _currentDirection = Vector2.zero;
            _currentMagnitude = 0f;
            
            HideIndicator();
            OnSkillCancelled?.Invoke();
        }
        
        #endregion

        #region Private Methods
        
        private void ShowIndicator()
        {
            if (_arrowIndicator != null)
            {
                _arrowIndicator.gameObject.SetActive(true);
            }
            
            if (_rangeCircle != null)
            {
                _rangeCircle.gameObject.SetActive(true);
            }
            
            if (_directionLine != null)
            {
                _directionLine.gameObject.SetActive(true);
            }
        }
        
        private void HideIndicator()
        {
            if (_arrowIndicator != null)
            {
                _arrowIndicator.gameObject.SetActive(false);
            }
            
            if (_rangeCircle != null)
            {
                _rangeCircle.gameObject.SetActive(false);
            }
            
            if (_directionLine != null)
            {
                _directionLine.gameObject.SetActive(false);
            }
        }
        
        private void UpdateIndicator(Vector2 delta, float distance)
        {
            bool inCancelZone = distance < _minTriggerDistance;
            Color currentColor = inCancelZone ? _cancelColor : _indicatorColor;
            
            // 更新箭头位置和旋转
            if (_arrowIndicator != null && distance > _minTriggerDistance)
            {
                // 限制在最大距离内
                Vector2 clampedDelta = delta.normalized * Mathf.Min(distance, _maxDragDistance);
                _arrowIndicator.anchoredPosition = clampedDelta;
                
                // 旋转箭头指向方向
                float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                _arrowIndicator.rotation = Quaternion.Euler(0, 0, angle - 90);
                
                // 更新颜色
                var arrowImage = _arrowIndicator.GetComponent<Image>();
                if (arrowImage != null)
                {
                    arrowImage.color = currentColor;
                }
            }
            
            // 更新方向线
            if (_directionLine != null && distance > _minTriggerDistance)
            {
                _directionLine.color = currentColor;
                
                // 设置线的长度和旋转
                RectTransform lineRect = _directionLine.rectTransform;
                lineRect.sizeDelta = new Vector2(Mathf.Min(distance, _maxDragDistance), lineRect.sizeDelta.y);
                
                float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                lineRect.rotation = Quaternion.Euler(0, 0, angle);
            }
            
            // 更新范围圆圈颜色
            if (_rangeCircle != null)
            {
                var circleImage = _rangeCircle.GetComponent<Image>();
                if (circleImage != null)
                {
                    Color circleColor = currentColor;
                    circleColor.a = 0.3f;
                    circleImage.color = circleColor;
                }
            }
        }
        
        #endregion

        #region Static Methods (用于测试)
        
        /// <summary>
        /// 计算方向向量。
        /// </summary>
        public static Vector2 CalculateDirection(Vector2 startPos, Vector2 currentPos)
        {
            Vector2 delta = currentPos - startPos;
            return delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector2.zero;
        }
        
        /// <summary>
        /// 计算归一化距离。
        /// </summary>
        public static float CalculateMagnitude(Vector2 startPos, Vector2 currentPos, float maxDistance)
        {
            if (maxDistance <= 0) return 0f;
            float distance = Vector2.Distance(startPos, currentPos);
            return Mathf.Clamp01(distance / maxDistance);
        }
        
        /// <summary>
        /// 判断是否在取消区域。
        /// </summary>
        public static bool CheckIsInCancelZone(float magnitude, float minTriggerRatio)
        {
            return magnitude < minTriggerRatio;
        }
        
        #endregion

        #region Editor Support
#if UNITY_EDITOR
        public void SetReferencesForTesting(RectTransform arrow, RectTransform range, Image line)
        {
            _arrowIndicator = arrow;
            _rangeCircle = range;
            _directionLine = line;
        }
        
        public void SetConfigForTesting(float maxDrag, float minTrigger, bool requiresDir)
        {
            _maxDragDistance = maxDrag;
            _minTriggerDistance = minTrigger;
            _requiresDirection = requiresDir;
        }
#endif
        #endregion
    }
}
