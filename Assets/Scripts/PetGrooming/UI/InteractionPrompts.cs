using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PetGrooming.Core;

namespace PetGrooming.UI
{
    /// <summary>
    /// Manages interaction prompt UI elements for capture, grooming, and distance feedback.
    /// Requirements: 3.5, 8.3, 8.4
    /// </summary>
    public class InteractionPrompts : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Capture Prompt")]
        [Tooltip("Panel for capture interaction prompt")]
        [SerializeField] private GameObject _capturePromptPanel;
        [Tooltip("Text for capture prompt message")]
        [SerializeField] private TextMeshProUGUI _capturePromptText;
        
        [Header("Groom Prompt")]
        [Tooltip("Panel for grooming interaction prompt")]
        [SerializeField] private GameObject _groomPromptPanel;
        [Tooltip("Text for groom prompt message")]
        [SerializeField] private TextMeshProUGUI _groomPromptText;
        
        [Header("Distance Warning")]
        [Tooltip("Panel for 'too far' warning")]
        [SerializeField] private GameObject _tooFarPanel;
        [Tooltip("Text for distance warning message")]
        [SerializeField] private TextMeshProUGUI _tooFarText;
        [Tooltip("Duration to show the 'too far' warning")]
        [SerializeField] private float _tooFarDisplayDuration = 1.5f;
        
        [Header("Struggle Prompt")]
        [Tooltip("Panel for pet struggle indicator")]
        [SerializeField] private GameObject _strugglePromptPanel;
        [Tooltip("Text for struggle prompt message")]
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
        /// Shows or hides the capture prompt.
        /// Requirement 8.3: Display capture prompts when pet is captured.
        /// </summary>
        /// <param name="show">Whether to show the prompt.</param>
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
        /// Shows or hides the groom prompt.
        /// Requirement 8.4: Display "Press E to start grooming" near grooming station.
        /// </summary>
        /// <param name="show">Whether to show the prompt.</param>
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
        /// Shows the "too far" warning.
        /// Requirement 3.5: Display "Too far" indicator when capture fails due to distance.
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
        /// Hides the "too far" warning.
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
        /// Shows or hides the struggle prompt.
        /// Requirement 8.3: Display struggle prompts for the Pet AI.
        /// </summary>
        /// <param name="show">Whether to show the prompt.</param>
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
        /// Hides all interaction prompts.
        /// </summary>
        public void HideAllPrompts()
        {
            ShowCapturePrompt(false);
            ShowGroomPrompt(false);
            HideTooFarPrompt();
            ShowStrugglePrompt(false);
        }
        
        /// <summary>
        /// Updates the capture prompt with custom message.
        /// </summary>
        /// <param name="message">Custom message to display.</param>
        public void SetCapturePromptMessage(string message)
        {
            _captureMessage = message;
            if (_capturePromptPanel != null && _capturePromptPanel.activeSelf && _capturePromptText != null)
            {
                _capturePromptText.text = message;
            }
        }
        
        /// <summary>
        /// Updates the groom prompt with custom message.
        /// </summary>
        /// <param name="message">Custom message to display.</param>
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
