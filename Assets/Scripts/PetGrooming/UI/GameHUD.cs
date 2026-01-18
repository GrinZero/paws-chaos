using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PetGrooming.Core;
using PetGrooming.Systems;

namespace PetGrooming.UI
{
    /// <summary>
    /// 显示计时器、恶作剧值和洗护步骤的主要游戏 HUD 组件。
    /// 需求：6.2, 5.4, 8.1, 8.2, 8.5
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Timer Display")]
        [Tooltip("显示剩余比赛时间的文本组件")]
        [SerializeField] private TextMeshProUGUI _timerText;
        
        [Header("恶作剧显示")]
        [Tooltip("恶作剧进度条的滑块")]
        [SerializeField] private Slider _mischiefBar;
        [Tooltip("显示恶作剧数值的文本组件")]
        [SerializeField] private TextMeshProUGUI _mischiefValueText;
        [Tooltip("恶作剧条颜色变化的填充图像")]
        [SerializeField] private Image _mischiefBarFill;
        
        [Header("洗护显示")]
        [Tooltip("包含洗护步骤 UI 的面板")]
        [SerializeField] private GameObject _groomingPanel;
        [Tooltip("显示当前洗护步骤的文本组件")]
        [SerializeField] private TextMeshProUGUI _groomingStepText;
        [Tooltip("显示洗护按键提示的文本组件")]
        [SerializeField] private TextMeshProUGUI _groomingKeyPromptText;
        
        [Header("Colors")]
        [SerializeField] private Color _normalMischiefColor = new Color(0.2f, 0.6f, 1f);
        [SerializeField] private Color _warningMischiefColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color _dangerMischiefColor = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private float _warningThreshold = 0.5f;
        [SerializeField] private float _dangerThreshold = 0.8f;
        
        #endregion

        #region Private Fields
        
        private int _maxMischiefValue = 500;
        
        #endregion

        #region Unity Lifecycle
        
        private void Start()
        {
            ValidateReferences();
            SubscribeToEvents();
            InitializeUI();
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// 更新计时器显示。
        /// 需求 8.1：以 MM:SS 格式显示剩余比赛时间。
        /// </summary>
        /// <param name="remainingTime">剩余时间（秒）。</param>
        public void UpdateTimer(float remainingTime)
        {
            if (_timerText == null) return;
            
            _timerText.text = FormatTime(remainingTime);
        }
        
        /// <summary>
        /// 更新恶作剧值显示。
        /// 需求 8.2：将恶作剧值显示为带数值的填充进度条。
        /// </summary>
        /// <param name="current">当前恶作剧值。</param>
        /// <param name="max">最大恶作剧值（阈值）。</param>
        public void UpdateMischiefValue(int current, int max)
        {
            _maxMischiefValue = max;
            
            if (_mischiefBar != null)
            {
                _mischiefBar.maxValue = max;
                _mischiefBar.value = current;
            }
            
            if (_mischiefValueText != null)
            {
                _mischiefValueText.text = $"{current}/{max}";
            }
            
            UpdateMischiefBarColor(current, max);
        }
        
        /// <summary>
        /// Shows the current grooming step.
        /// Requirement 8.5: Display current grooming step during grooming process.
        /// </summary>
        /// <param name="step">The current grooming step.</param>
        public void ShowGroomingStep(GroomingSystem.GroomingStep step)
        {
            if (_groomingPanel == null) return;
            
            bool showPanel = step != GroomingSystem.GroomingStep.None && 
                            step != GroomingSystem.GroomingStep.Complete;
            
            _groomingPanel.SetActive(showPanel);
            
            if (showPanel)
            {
                UpdateGroomingStepDisplay(step);
            }
        }
        
        /// <summary>
        /// Hides the grooming panel.
        /// </summary>
        public void HideGroomingPanel()
        {
            if (_groomingPanel != null)
            {
                _groomingPanel.SetActive(false);
            }
        }
        
        #endregion

        #region Private Methods
        
        private void ValidateReferences()
        {
            if (_timerText == null)
                Debug.LogWarning("[GameHUD] Timer text is not assigned!");
            if (_mischiefBar == null)
                Debug.LogWarning("[GameHUD] Mischief bar is not assigned!");
            if (_mischiefValueText == null)
                Debug.LogWarning("[GameHUD] Mischief value text is not assigned!");
            if (_groomingPanel == null)
                Debug.LogWarning("[GameHUD] Grooming panel is not assigned!");
        }
        
        private void SubscribeToEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnTimeUpdated += UpdateTimer;
            }
            
            if (MischiefSystem.Instance != null)
            {
                MischiefSystem.Instance.OnMischiefValueChanged += OnMischiefChanged;
            }
        }
        
        private void UnsubscribeFromEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnTimeUpdated -= UpdateTimer;
            }
            
            if (MischiefSystem.Instance != null)
            {
                MischiefSystem.Instance.OnMischiefValueChanged -= OnMischiefChanged;
            }
        }
        
        private void InitializeUI()
        {
            // Initialize timer
            if (GameManager.Instance != null)
            {
                UpdateTimer(GameManager.Instance.RemainingTime);
                _maxMischiefValue = GameManager.Instance.MischiefThreshold;
            }
            
            // Initialize mischief display
            if (MischiefSystem.Instance != null)
            {
                UpdateMischiefValue(MischiefSystem.Instance.CurrentMischiefValue, _maxMischiefValue);
            }
            else
            {
                UpdateMischiefValue(0, _maxMischiefValue);
            }
            
            // Hide grooming panel initially
            HideGroomingPanel();
        }
        
        private void OnMischiefChanged(int newValue)
        {
            UpdateMischiefValue(newValue, _maxMischiefValue);
        }
        
        private void UpdateMischiefBarColor(int current, int max)
        {
            if (_mischiefBarFill == null) return;
            
            float ratio = max > 0 ? (float)current / max : 0f;
            
            if (ratio >= _dangerThreshold)
            {
                _mischiefBarFill.color = _dangerMischiefColor;
            }
            else if (ratio >= _warningThreshold)
            {
                _mischiefBarFill.color = _warningMischiefColor;
            }
            else
            {
                _mischiefBarFill.color = _normalMischiefColor;
            }
        }
        
        private void UpdateGroomingStepDisplay(GroomingSystem.GroomingStep step)
        {
            if (_groomingStepText != null)
            {
                _groomingStepText.text = GetStepDisplayName(step);
            }
            
            if (_groomingKeyPromptText != null)
            {
                _groomingKeyPromptText.text = GetStepKeyPrompt(step);
            }
        }
        
        #endregion

        #region Static Helper Methods
        
        /// <summary>
        /// Formats time in MM:SS format.
        /// Requirement 8.1: Display remaining match time in MM:SS format.
        /// </summary>
        /// <param name="timeInSeconds">Time in seconds.</param>
        /// <returns>Formatted time string.</returns>
        public static string FormatTime(float timeInSeconds)
        {
            if (timeInSeconds < 0) timeInSeconds = 0;
            
            int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
            int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
            
            return $"{minutes:00}:{seconds:00}";
        }
        
        /// <summary>
        /// Gets the display name for a grooming step.
        /// </summary>
        /// <param name="step">The grooming step.</param>
        /// <returns>Localized display name.</returns>
        public static string GetStepDisplayName(GroomingSystem.GroomingStep step)
        {
            return step switch
            {
                GroomingSystem.GroomingStep.Brush => "梳毛",
                GroomingSystem.GroomingStep.Clean => "清洁",
                GroomingSystem.GroomingStep.Dry => "烘干",
                GroomingSystem.GroomingStep.Complete => "完成",
                _ => ""
            };
        }
        
        /// <summary>
        /// Gets the key prompt for a grooming step.
        /// </summary>
        /// <param name="step">The grooming step.</param>
        /// <returns>Key prompt string.</returns>
        public static string GetStepKeyPrompt(GroomingSystem.GroomingStep step)
        {
            return step switch
            {
                GroomingSystem.GroomingStep.Brush => "按 [1] 梳毛",
                GroomingSystem.GroomingStep.Clean => "按 [2] 清洁",
                GroomingSystem.GroomingStep.Dry => "按 [3] 烘干",
                _ => ""
            };
        }
        
        #endregion
    }
}
