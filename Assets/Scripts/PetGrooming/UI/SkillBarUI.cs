using System;
using UnityEngine;
using UnityEngine.UI;
using PetGrooming.Systems.Skills;

namespace PetGrooming.UI
{
    /// <summary>
    /// UI component that manages the layout and display of multiple skill cooldown UIs.
    /// Supports configurable HUD positioning and automatic skill binding.
    /// Requirement 7.5: Skill icons positioned near character or in fixed HUD location.
    /// </summary>
    public class SkillBarUI : MonoBehaviour
    {
        #region Enums
        
        /// <summary>
        /// Position presets for the skill bar.
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
        /// Layout direction for skill icons.
        /// </summary>
        public enum LayoutDirection
        {
            Horizontal,
            Vertical
        }
        
        #endregion

        #region Serialized Fields
        
        [Header("Skill UI References")]
        [Tooltip("Array of skill cooldown UI components")]
        [SerializeField] private SkillCooldownUI[] _skillSlots;
        
        [Tooltip("Container for skill icons")]
        [SerializeField] private RectTransform _skillContainer;
        
        [Header("Position Settings")]
        [Tooltip("Preset position for the skill bar")]
        [SerializeField] private SkillBarPosition _position = SkillBarPosition.BottomCenter;
        
        [Tooltip("Custom anchor position (used when position is Custom)")]
        [SerializeField] private Vector2 _customAnchor = new Vector2(0.5f, 0f);
        
        [Tooltip("Offset from anchor position")]
        [SerializeField] private Vector2 _offset = new Vector2(0f, 50f);
        
        [Header("Layout Settings")]
        [Tooltip("Layout direction for skill icons")]
        [SerializeField] private LayoutDirection _layoutDirection = LayoutDirection.Horizontal;
        
        [Tooltip("Spacing between skill icons")]
        [SerializeField] private float _spacing = 10f;
        
        [Tooltip("Size of each skill icon")]
        [SerializeField] private Vector2 _iconSize = new Vector2(64f, 64f);
        
        [Header("Key Prompt Settings")]
        [Tooltip("Show key prompts below skill icons")]
        [SerializeField] private bool _showKeyPrompts = true;
        
        [Tooltip("Key prompt labels")]
        [SerializeField] private string[] _keyPromptLabels = new string[] { "1", "2", "3" };
        
        [Header("Auto-Bind Settings")]
        [Tooltip("Automatically find and bind to GroomerSkillManager")]
        [SerializeField] private bool _autoBindToGroomer = true;
        
        [Tooltip("Tag to search for Groomer")]
        [SerializeField] private string _groomerTag = "Player";
        
        #endregion

        #region Private Fields
        
        private RectTransform _rectTransform;
        private GroomerSkillManager _boundSkillManager;
        private bool _isInitialized;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// The currently bound skill manager.
        /// </summary>
        public GroomerSkillManager BoundSkillManager => _boundSkillManager;
        
        /// <summary>
        /// Number of skill slots available.
        /// </summary>
        public int SlotCount => _skillSlots != null ? _skillSlots.Length : 0;
        
        /// <summary>
        /// Current position setting.
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
        /// Fired when a skill becomes ready.
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
        /// Initializes the skill bar UI.
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
        /// Binds the skill bar to a skill manager.
        /// </summary>
        /// <param name="skillManager">The skill manager to bind to</param>
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
        /// Unbinds from the current skill manager.
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
        /// Sets the position of the skill bar.
        /// Requirement 7.5: Skill icons positioned based on settings.
        /// </summary>
        /// <param name="position">Position preset</param>
        public void SetPosition(SkillBarPosition position)
        {
            _position = position;
            ApplyPosition();
        }
        
        /// <summary>
        /// Sets a custom position for the skill bar.
        /// </summary>
        /// <param name="anchor">Anchor position (0-1)</param>
        /// <param name="offset">Offset from anchor</param>
        public void SetCustomPosition(Vector2 anchor, Vector2 offset)
        {
            _position = SkillBarPosition.Custom;
            _customAnchor = anchor;
            _offset = offset;
            ApplyPosition();
        }
        
        /// <summary>
        /// Gets a skill slot by index.
        /// </summary>
        /// <param name="index">Slot index</param>
        /// <returns>The skill cooldown UI at the index, or null</returns>
        public SkillCooldownUI GetSlot(int index)
        {
            if (index < 0 || index >= _skillSlots.Length) return null;
            return _skillSlots[index];
        }
        
        /// <summary>
        /// Shows or hides the skill bar.
        /// </summary>
        /// <param name="visible">Whether to show the skill bar</param>
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
        /// Calculates the total width of a horizontal skill bar.
        /// </summary>
        /// <param name="slotCount">Number of slots</param>
        /// <param name="iconWidth">Width of each icon</param>
        /// <param name="spacing">Spacing between icons</param>
        /// <returns>Total width</returns>
        public static float CalculateBarWidth(int slotCount, float iconWidth, float spacing)
        {
            if (slotCount <= 0) return 0f;
            return slotCount * iconWidth + (slotCount - 1) * spacing;
        }
        
        /// <summary>
        /// Calculates the position of a slot in a horizontal layout.
        /// </summary>
        /// <param name="slotIndex">Index of the slot</param>
        /// <param name="totalSlots">Total number of slots</param>
        /// <param name="iconWidth">Width of each icon</param>
        /// <param name="spacing">Spacing between icons</param>
        /// <returns>X position of the slot center</returns>
        public static float CalculateSlotPosition(int slotIndex, int totalSlots, float iconWidth, float spacing)
        {
            float totalWidth = CalculateBarWidth(totalSlots, iconWidth, spacing);
            return -totalWidth / 2f + iconWidth / 2f + slotIndex * (iconWidth + spacing);
        }
        
        #endregion

        #region Editor Support
#if UNITY_EDITOR
        /// <summary>
        /// Sets skill slots for testing purposes.
        /// </summary>
        public void SetSlotsForTesting(SkillCooldownUI[] slots)
        {
            _skillSlots = slots;
        }
        
        /// <summary>
        /// Sets layout settings for testing purposes.
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
