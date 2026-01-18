using System;
using UnityEngine;
using UnityEngine.UI;
using PetGrooming.Systems.Skills;

namespace PetGrooming.UI
{
    /// <summary>
    /// 管理多个技能冷却 UI 布局和显示的 UI 组件。
    /// 支持可配置的 HUD 定位和自动技能绑定。
    /// 需求 7.5：技能图标定位在角色附近或固定的 HUD 位置。
    /// </summary>
    public class SkillBarUI : MonoBehaviour
    {
        #region Enums
        
        /// <summary>
        /// 技能栏的位置预设。
        /// </summary>
        public enum SkillBarPosition
        {
            BottomCenter,
            BottomLeft,
            BottomRight,
            TopCenter,
            TopLeft,
            TopRight,
            LeftCenter,
            RightCenter,
            Custom
        }
        
        /// <summary>
        /// 技能图标的布局方向。
        /// </summary>
        public enum LayoutDirection
        {
            Horizontal,
            Vertical
        }
        
        #endregion

        #region Serialized Fields
        
        [Header("Skill UI References")]
        [Tooltip("技能冷却 UI 组件数组")]
        [SerializeField] private SkillCooldownUI[] _skillSlots;
        
        [Tooltip("技能图标的容器")]
        [SerializeField] private RectTransform _skillContainer;
        
        [Header("位置设置")]
        [Tooltip("技能栏的预设位置")]
        [SerializeField] private SkillBarPosition _position = SkillBarPosition.BottomCenter;
        
        [Tooltip("自定义锚点位置（当位置为 Custom 时使用）")]
        [SerializeField] private Vector2 _customAnchor = new Vector2(0.5f, 0f);
        
        [Tooltip("从锚点位置的偏移量")]
        [SerializeField] private Vector2 _offset = new Vector2(0f, 50f);
        
        [Header("布局设置")]
        [Tooltip("技能图标的布局方向")]
        [SerializeField] private LayoutDirection _layoutDirection = LayoutDirection.Horizontal;
        
        [Tooltip("技能图标之间的间距")]
        [SerializeField] private float _spacing = 10f;
        
        [Tooltip("每个技能图标的大小")]
        [SerializeField] private Vector2 _iconSize = new Vector2(64f, 64f);
        
        [Header("按键提示设置")]
        [Tooltip("在技能图标下方显示按键提示")]
        [SerializeField] private bool _showKeyPrompts = true;
        
        [Tooltip("按键提示标签")]
        [SerializeField] private string[] _keyPromptLabels = new string[] { "1", "2", "3" };
        
        [Header("自动绑定设置")]
        [Tooltip("自动查找并绑定到 GroomerSkillManager")]
        [SerializeField] private bool _autoBindToGroomer = true;
        
        [Tooltip("用于搜索 Groomer 的标签")]
        [SerializeField] private string _groomerTag = "Player";
        
        #endregion

        #region Private Fields
        
        private RectTransform _rectTransform;
        private GroomerSkillManager _boundSkillManager;
        private bool _isInitialized;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// 当前绑定的技能管理器。
        /// </summary>
        public GroomerSkillManager BoundSkillManager => _boundSkillManager;
        
        /// <summary>
        /// 可用的技能槽数量。
        /// </summary>
        public int SlotCount => _skillSlots != null ? _skillSlots.Length : 0;
        
        /// <summary>
        /// 当前位置设置。
        /// </summary>
        public SkillBarPosition Position
        {
            get => _position;
            set
            {
                _position = value;
                ApplyPosition();
            }
        }
        
        #endregion

        #region Events
        
        /// <summary>
        /// 当技能准备就绪时触发。
        /// </summary>
        public event Action<int> OnSkillReady;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            ValidateReferences();
        }
        
        private void Start()
        {
            Initialize();
        }
        
        private void OnDestroy()
        {
            UnbindFromSkillManager();
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// 初始化技能栏 UI。
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;
            
            ApplyPosition();
            ApplyLayout();
            
            if (_autoBindToGroomer)
            {
                TryAutoBindToGroomer();
            }
            
            _isInitialized = true;
        }
        
        /// <summary>
        /// 将技能栏绑定到技能管理器。
        /// </summary>
        /// <param name="skillManager">要绑定的技能管理器</param>
        public void BindToSkillManager(GroomerSkillManager skillManager)
        {
            UnbindFromSkillManager();
            
            _boundSkillManager = skillManager;
            
            if (_boundSkillManager == null) return;
            
            // Bind each skill slot to the corresponding skill
            for (int i = 0; i < _skillSlots.Length && i < _boundSkillManager.SkillCount; i++)
            {
                if (_skillSlots[i] != null)
                {
                    SkillBase skill = _boundSkillManager.GetSkill(i);
                    _skillSlots[i].SetSkill(skill);
                    
                    // Subscribe to ready events
                    int slotIndex = i;
                    _skillSlots[i].OnSkillReady += () => OnSkillReady?.Invoke(slotIndex);
                }
            }
        }
        
        /// <summary>
        /// 从当前技能管理器解绑。
        /// </summary>
        public void UnbindFromSkillManager()
        {
            if (_boundSkillManager == null) return;
            
            foreach (var slot in _skillSlots)
            {
                if (slot != null)
                {
                    slot.SetSkill(null);
                }
            }
            
            _boundSkillManager = null;
        }
        
        /// <summary>
        /// 设置技能栏的位置。
        /// 需求 7.5：技能图标根据设置定位。
        /// </summary>
        /// <param name="position">位置预设</param>
        public void SetPosition(SkillBarPosition position)
        {
            _position = position;
            ApplyPosition();
        }
        
        /// <summary>
        /// 为技能栏设置自定义位置。
        /// </summary>
        /// <param name="anchor">锚点位置 (0-1)</param>
        /// <param name="offset">从锚点的偏移量</param>
        public void SetCustomPosition(Vector2 anchor, Vector2 offset)
        {
            _position = SkillBarPosition.Custom;
            _customAnchor = anchor;
            _offset = offset;
            ApplyPosition();
        }
        
        /// <summary>
        /// 通过索引获取技能槽。
        /// </summary>
        /// <param name="index">槽位索引</param>
        /// <returns>索引处的技能冷却 UI，如果不存在则返回 null</returns>
        public SkillCooldownUI GetSlot(int index)
        {
            if (index < 0 || index >= _skillSlots.Length) return null;
            return _skillSlots[index];
        }
        
        /// <summary>
        /// 显示或隐藏技能栏。
        /// </summary>
        /// <param name="visible">是否显示技能栏</param>
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
        
        #endregion

        #region Private Methods
        
        private void ValidateReferences()
        {
            if (_skillSlots == null || _skillSlots.Length == 0)
            {
                Debug.LogWarning("[SkillBarUI] No skill slots assigned!");
            }
            
            if (_skillContainer == null)
            {
                _skillContainer = _rectTransform;
            }
        }
        
        private void TryAutoBindToGroomer()
        {
            // Try to find Groomer by tag
            GameObject groomerObj = GameObject.FindGameObjectWithTag(_groomerTag);
            if (groomerObj != null)
            {
                GroomerSkillManager skillManager = groomerObj.GetComponent<GroomerSkillManager>();
                if (skillManager == null)
                {
                    skillManager = groomerObj.GetComponentInChildren<GroomerSkillManager>();
                }
                
                if (skillManager != null)
                {
                    BindToSkillManager(skillManager);
                    return;
                }
            }
            
            // Try to find GroomerSkillManager directly
            GroomerSkillManager foundManager = FindObjectOfType<GroomerSkillManager>();
            if (foundManager != null)
            {
                BindToSkillManager(foundManager);
            }
        }
        
        private void ApplyPosition()
        {
            if (_rectTransform == null) return;
            
            Vector2 anchor = GetAnchorForPosition(_position);
            Vector2 pivot = GetPivotForPosition(_position);
            
            _rectTransform.anchorMin = anchor;
            _rectTransform.anchorMax = anchor;
            _rectTransform.pivot = pivot;
            _rectTransform.anchoredPosition = _offset;
        }
        
        private void ApplyLayout()
        {
            if (_skillContainer == null || _skillSlots == null) return;
            
            // Calculate total size
            int slotCount = _skillSlots.Length;
            float totalWidth, totalHeight;
            
            if (_layoutDirection == LayoutDirection.Horizontal)
            {
                totalWidth = slotCount * _iconSize.x + (slotCount - 1) * _spacing;
                totalHeight = _iconSize.y;
            }
            else
            {
                totalWidth = _iconSize.x;
                totalHeight = slotCount * _iconSize.y + (slotCount - 1) * _spacing;
            }
            
            // Set container size
            _skillContainer.sizeDelta = new Vector2(totalWidth, totalHeight);
            
            // Position each slot
            for (int i = 0; i < _skillSlots.Length; i++)
            {
                if (_skillSlots[i] == null) continue;
                
                RectTransform slotRect = _skillSlots[i].GetComponent<RectTransform>();
                if (slotRect == null) continue;
                
                // Set size
                slotRect.sizeDelta = _iconSize;
                
                // Calculate position
                Vector2 position;
                if (_layoutDirection == LayoutDirection.Horizontal)
                {
                    float x = -totalWidth / 2f + _iconSize.x / 2f + i * (_iconSize.x + _spacing);
                    position = new Vector2(x, 0f);
                }
                else
                {
                    float y = totalHeight / 2f - _iconSize.y / 2f - i * (_iconSize.y + _spacing);
                    position = new Vector2(0f, y);
                }
                
                slotRect.anchoredPosition = position;
            }
        }
        
        private Vector2 GetAnchorForPosition(SkillBarPosition position)
        {
            return position switch
            {
                SkillBarPosition.BottomCenter => new Vector2(0.5f, 0f),
                SkillBarPosition.BottomLeft => new Vector2(0f, 0f),
                SkillBarPosition.BottomRight => new Vector2(1f, 0f),
                SkillBarPosition.TopCenter => new Vector2(0.5f, 1f),
                SkillBarPosition.TopLeft => new Vector2(0f, 1f),
                SkillBarPosition.TopRight => new Vector2(1f, 1f),
                SkillBarPosition.LeftCenter => new Vector2(0f, 0.5f),
                SkillBarPosition.RightCenter => new Vector2(1f, 0.5f),
                SkillBarPosition.Custom => _customAnchor,
                _ => new Vector2(0.5f, 0f)
            };
        }
        
        private Vector2 GetPivotForPosition(SkillBarPosition position)
        {
            return position switch
            {
                SkillBarPosition.BottomCenter => new Vector2(0.5f, 0f),
                SkillBarPosition.BottomLeft => new Vector2(0f, 0f),
                SkillBarPosition.BottomRight => new Vector2(1f, 0f),
                SkillBarPosition.TopCenter => new Vector2(0.5f, 1f),
                SkillBarPosition.TopLeft => new Vector2(0f, 1f),
                SkillBarPosition.TopRight => new Vector2(1f, 1f),
                SkillBarPosition.LeftCenter => new Vector2(0f, 0.5f),
                SkillBarPosition.RightCenter => new Vector2(1f, 0.5f),
                SkillBarPosition.Custom => _customAnchor,
                _ => new Vector2(0.5f, 0f)
            };
        }
        
        #endregion

        #region Static Helper Methods
        
        /// <summary>
        /// 计算水平技能栏的总宽度。
        /// </summary>
        /// <param name="slotCount">槽位数量</param>
        /// <param name="iconWidth">每个图标的宽度</param>
        /// <param name="spacing">图标之间的间距</param>
        /// <returns>总宽度</returns>
        public static float CalculateBarWidth(int slotCount, float iconWidth, float spacing)
        {
            if (slotCount <= 0) return 0f;
            return slotCount * iconWidth + (slotCount - 1) * spacing;
        }
        
        /// <summary>
        /// 计算水平布局中槽位的位置。
        /// </summary>
        /// <param name="slotIndex">槽位索引</param>
        /// <param name="totalSlots">总槽位数量</param>
        /// <param name="iconWidth">每个图标的宽度</param>
        /// <param name="spacing">图标之间的间距</param>
        /// <returns>槽位中心的 X 位置</returns>
        public static float CalculateSlotPosition(int slotIndex, int totalSlots, float iconWidth, float spacing)
        {
            float totalWidth = CalculateBarWidth(totalSlots, iconWidth, spacing);
            return -totalWidth / 2f + iconWidth / 2f + slotIndex * (iconWidth + spacing);
        }
        
        #endregion

        #region Editor Support
#if UNITY_EDITOR
        /// <summary>
        /// 为测试目的设置技能槽。
        /// </summary>
        public void SetSlotsForTesting(SkillCooldownUI[] slots)
        {
            _skillSlots = slots;
        }
        
        /// <summary>
        /// 为测试目的设置布局设置。
        /// </summary>
        public void SetLayoutForTesting(LayoutDirection direction, float spacing, Vector2 iconSize)
        {
            _layoutDirection = direction;
            _spacing = spacing;
            _iconSize = iconSize;
        }
        
        private void OnValidate()
        {
            if (Application.isPlaying && _isInitialized)
            {
                ApplyPosition();
                ApplyLayout();
            }
        }
#endif
        #endregion
    }
}
