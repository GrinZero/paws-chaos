using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PetGrooming.Core;

namespace PetGrooming.UI
{
    /// <summary>
    /// 管理捕获、洗护和距离反馈的交互提示 UI 元素。
    /// 需求：3.5, 8.3, 8.4
    /// </summary>
    public class InteractionPrompts : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Capture Prompt")]
        [Tooltip("捕获交互提示的面板")]
        [SerializeField] private GameObject _capturePromptPanel;
        [Tooltip("捕获提示消息的文本")]
        [SerializeField] private TextMeshProUGUI _capturePromptText;
        
        [Header("洗护提示")]
        [Tooltip("洗护交互提示的面板")]
        [SerializeField] private GameObject _groomPromptPanel;
        [Tooltip("洗护提示消息的文本")]
        [SerializeField] private TextMeshProUGUI _groomPromptText;
        
        [Header("距离警告")]
        [Tooltip("'太远'警告的面板")]
        [SerializeField] private GameObject _tooFarPanel;
        [Tooltip("距离警告消息的文本")]
        [SerializeField] private TextMeshProUGUI _tooFarText;
        [Tooltip("显示'太远'警告的持续时间")]
        [SerializeField] private float _tooFarDisplayDuration = 1.5f;
        
        [Header("挣扎提示")]
        [Tooltip("宠物挣扎指示器的面板")]
        [SerializeField] private GameObject _strugglePromptPanel;
        [Tooltip("挣扎提示消息的文本")]
        [SerializeField] private TextMeshProUGUI _strugglePromptText;
        
        [Header("Prompt Messages")]
        [SerializeField] private string _captureMessage = "按 [E] 抓捕";
        [SerializeField] private string _groomMessage = "按 [E] 开始洗护";
        [SerializeField] private string _tooFarMessage = "距离太远!";
        [SerializeField] private string _struggleMessage = "萌宠正在挣扎!";
        
        #endregion

        #region Private Fields
        
        private float _tooFarHideTime;
        private bool _isTooFarVisible;
        
        #endregion

        #region Unity Lifecycle
        
        private void Start()
        {
            ValidateReferences();
            HideAllPrompts();
        }
        
        private void Update()
        {
            // Auto-hide "too far" warning after duration
            if (_isTooFarVisible && Time.time >= _tooFarHideTime)
            {
                HideTooFarPrompt();
            }
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// 显示或隐藏捕获提示。
        /// 需求 8.3：当宠物被捕获时显示捕获提示。
        /// </summary>
        /// <param name="show">是否显示提示。</param>
        public void ShowCapturePrompt(bool show)
        {
            if (_capturePromptPanel != null)
            {
                _capturePromptPanel.SetActive(show);
            }
            
            if (show && _capturePromptText != null)
            {
                _capturePromptText.text = _captureMessage;
            }
        }
        
        /// <summary>
        /// 显示或隐藏洗护提示。
        /// 需求 8.4：在洗护站附近显示"按 E 开始洗护"。
        /// </summary>
        /// <param name="show">是否显示提示。</param>
        public void ShowGroomPrompt(bool show)
        {
            if (_groomPromptPanel != null)
            {
                _groomPromptPanel.SetActive(show);
            }
            
            if (show && _groomPromptText != null)
            {
                _groomPromptText.text = _groomMessage;
            }
        }
        
        /// <summary>
        /// 显示"太远"警告。
        /// 需求 3.5：当由于距离原因捕获失败时显示"太远"指示器。
        /// </summary>
        public void ShowTooFarPrompt()
        {
            if (_tooFarPanel != null)
            {
                _tooFarPanel.SetActive(true);
            }
            
            if (_tooFarText != null)
            {
                _tooFarText.text = _tooFarMessage;
            }
            
            _isTooFarVisible = true;
            _tooFarHideTime = Time.time + _tooFarDisplayDuration;
        }
        
        /// <summary>
        /// 隐藏"太远"警告。
        /// </summary>
        public void HideTooFarPrompt()
        {
            if (_tooFarPanel != null)
            {
                _tooFarPanel.SetActive(false);
            }
            
            _isTooFarVisible = false;
        }
        
        /// <summary>
        /// 显示或隐藏挣扎提示。
        /// 需求 8.3：为 Pet AI 显示挣扎提示。
        /// </summary>
        /// <param name="show">是否显示提示。</param>
        public void ShowStrugglePrompt(bool show)
        {
            if (_strugglePromptPanel != null)
            {
                _strugglePromptPanel.SetActive(show);
            }
            
            if (show && _strugglePromptText != null)
            {
                _strugglePromptText.text = _struggleMessage;
            }
        }
        
        /// <summary>
        /// 隐藏所有交互提示。
        /// </summary>
        public void HideAllPrompts()
        {
            ShowCapturePrompt(false);
            ShowGroomPrompt(false);
            HideTooFarPrompt();
            ShowStrugglePrompt(false);
        }
        
        /// <summary>
        /// 使用自定义消息更新捕获提示。
        /// </summary>
        /// <param name="message">要显示的自定义消息。</param>
        public void SetCapturePromptMessage(string message)
        {
            _captureMessage = message;
            if (_capturePromptPanel != null && _capturePromptPanel.activeSelf && _capturePromptText != null)
            {
                _capturePromptText.text = message;
            }
        }
        
        /// <summary>
        /// 使用自定义消息更新洗护提示。
        /// </summary>
        /// <param name="message">要显示的自定义消息。</param>
        public void SetGroomPromptMessage(string message)
        {
            _groomMessage = message;
            if (_groomPromptPanel != null && _groomPromptPanel.activeSelf && _groomPromptText != null)
            {
                _groomPromptText.text = message;
            }
        }
        
        #endregion

        #region Private Methods
        
        private void ValidateReferences()
        {
            if (_capturePromptPanel == null)
                Debug.LogWarning("[InteractionPrompts] Capture prompt panel is not assigned!");
            if (_groomPromptPanel == null)
                Debug.LogWarning("[InteractionPrompts] Groom prompt panel is not assigned!");
            if (_tooFarPanel == null)
                Debug.LogWarning("[InteractionPrompts] Too far panel is not assigned!");
            if (_strugglePromptPanel == null)
                Debug.LogWarning("[InteractionPrompts] Struggle prompt panel is not assigned!");
        }
        
        #endregion
    }
}
