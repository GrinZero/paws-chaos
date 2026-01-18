using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using PetGrooming.Core;

namespace PetGrooming.UI
{
    /// <summary>
    /// 管理显示胜利/失败和重玩选项的游戏结果屏幕。
    /// 需求 8.6：显示带有最终统计数据的胜利/失败屏幕。
    /// </summary>
    public class ResultScreen : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Result Panel")]
        [Tooltip("主要结果面板容器")]
        [SerializeField] private GameObject _resultPanel;
        
        [Header("结果显示")]
        [Tooltip("结果标题文本（胜利/失败）")]
        [SerializeField] private TextMeshProUGUI _resultTitleText;
        [Tooltip("结果描述文本")]
        [SerializeField] private TextMeshProUGUI _resultDescriptionText;
        [Tooltip("结果图标图像")]
        [SerializeField] private Image _resultIcon;
        
        [Header("统计显示")]
        [Tooltip("最终时间文本")]
        [SerializeField] private TextMeshProUGUI _finalTimeText;
        [Tooltip("最终恶作剧值文本")]
        [SerializeField] private TextMeshProUGUI _finalMischiefText;
        
        [Header("按钮")]
        [Tooltip("重新游戏的按钮")]
        [SerializeField] private Button _replayButton;
        [Tooltip("退出到主菜单的按钮")]
        [SerializeField] private Button _quitButton;
        
        [Header("Visual Settings")]
        [SerializeField] private Color _victoryColor = new Color(0.2f, 0.8f, 0.3f);
        [SerializeField] private Color _defeatColor = new Color(0.8f, 0.2f, 0.2f);
        [SerializeField] private Sprite _victoryIcon;
        [SerializeField] private Sprite _defeatIcon;
        
        [Header("Messages")]
        [SerializeField] private string _groomerVictoryTitle = "胜利!";
        [SerializeField] private string _groomerVictoryDesc = "成功完成萌宠洗护!";
        [SerializeField] private string _petVictoryTitle = "失败!";
        [SerializeField] private string _petVictoryDescMischief = "萌宠捣乱值达到上限!";
        [SerializeField] private string _petVictoryDescTimeout = "时间耗尽!";
        
        #endregion

        #region Private Fields
        
        private GameManager.GameState _lastResult;
        
        #endregion

        #region Unity Lifecycle
        
        private void Start()
        {
            ValidateReferences();
            SetupButtons();
            SubscribeToEvents();
            HideResult();
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// 显示带有指定结果的结果屏幕。
        /// 需求 8.6：显示带有最终统计数据的胜利/失败屏幕。
        /// </summary>
        /// <param name="groomerWin">如果 groomer 获胜则为 True，如果宠物获胜则为 False。</param>
        public void ShowResult(bool groomerWin)
        {
            if (_resultPanel == null) return;
            
            _resultPanel.SetActive(true);
            
            if (groomerWin)
            {
                ShowGroomerVictory();
            }
            else
            {
                ShowPetVictory();
            }
            
            UpdateStats();
            
            // Pause the game
            Time.timeScale = 0f;
        }
        
        /// <summary>
        /// 根据游戏状态显示结果屏幕。
        /// </summary>
        /// <param name="result">最终游戏状态。</param>
        public void ShowResult(GameManager.GameState result)
        {
            _lastResult = result;
            ShowResult(result == GameManager.GameState.GroomerWin);
        }
        
        /// <summary>
        /// 隐藏结果屏幕。
        /// </summary>
        public void HideResult()
        {
            if (_resultPanel != null)
            {
                _resultPanel.SetActive(false);
            }
        }
        
        /// <summary>
        /// 重新开始当前游戏。
        /// </summary>
        public void OnReplayClicked()
        {
            Time.timeScale = 1f;
            
            // Reset game manager if available
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResetGame();
                GameManager.Instance.StartMatch();
            }
            
            HideResult();
            
            Debug.Log("[ResultScreen] Game replayed.");
        }
        
        /// <summary>
        /// 退出到主菜单或重新加载场景。
        /// </summary>
        public void OnQuitClicked()
        {
            Time.timeScale = 1f;
            
            // Reload current scene as simple restart
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            
            Debug.Log("[ResultScreen] Quit to menu.");
        }
        
        #endregion

        #region Private Methods
        
        private void ValidateReferences()
        {
            if (_resultPanel == null)
                Debug.LogWarning("[ResultScreen] Result panel is not assigned!");
            if (_resultTitleText == null)
                Debug.LogWarning("[ResultScreen] Result title text is not assigned!");
            if (_replayButton == null)
                Debug.LogWarning("[ResultScreen] Replay button is not assigned!");
        }
        
        private void SetupButtons()
        {
            if (_replayButton != null)
            {
                _replayButton.onClick.AddListener(OnReplayClicked);
            }
            
            if (_quitButton != null)
            {
                _quitButton.onClick.AddListener(OnQuitClicked);
            }
        }
        
        private void SubscribeToEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnMatchEnded += OnMatchEnded;
            }
        }
        
        private void UnsubscribeFromEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnMatchEnded -= OnMatchEnded;
            }
            
            if (_replayButton != null)
            {
                _replayButton.onClick.RemoveListener(OnReplayClicked);
            }
            
            if (_quitButton != null)
            {
                _quitButton.onClick.RemoveListener(OnQuitClicked);
            }
        }
        
        private void OnMatchEnded(GameManager.GameState result)
        {
            ShowResult(result);
        }
        
        private void ShowGroomerVictory()
        {
            if (_resultTitleText != null)
            {
                _resultTitleText.text = _groomerVictoryTitle;
                _resultTitleText.color = _victoryColor;
            }
            
            if (_resultDescriptionText != null)
            {
                _resultDescriptionText.text = _groomerVictoryDesc;
            }
            
            if (_resultIcon != null && _victoryIcon != null)
            {
                _resultIcon.sprite = _victoryIcon;
                _resultIcon.color = _victoryColor;
            }
        }
        
        private void ShowPetVictory()
        {
            if (_resultTitleText != null)
            {
                _resultTitleText.text = _petVictoryTitle;
                _resultTitleText.color = _defeatColor;
            }
            
            if (_resultDescriptionText != null)
            {
                // Determine reason for pet victory
                string reason = DeterminePetVictoryReason();
                _resultDescriptionText.text = reason;
            }
            
            if (_resultIcon != null && _defeatIcon != null)
            {
                _resultIcon.sprite = _defeatIcon;
                _resultIcon.color = _defeatColor;
            }
        }
        
        private string DeterminePetVictoryReason()
        {
            if (GameManager.Instance != null)
            {
                // Check if mischief threshold was reached
                if (GameManager.Instance.CurrentMischiefValue >= GameManager.Instance.MischiefThreshold)
                {
                    return _petVictoryDescMischief;
                }
                
                // Otherwise it was timeout
                if (GameManager.Instance.RemainingTime <= 0)
                {
                    return _petVictoryDescTimeout;
                }
            }
            
            return _petVictoryDescTimeout;
        }
        
        private void UpdateStats()
        {
            if (GameManager.Instance == null) return;
            
            // Update final time
            if (_finalTimeText != null)
            {
                float elapsedTime = GameManager.Instance.MatchDuration - GameManager.Instance.RemainingTime;
                _finalTimeText.text = $"用时: {GameHUD.FormatTime(elapsedTime)}";
            }
            
            // Update final mischief value
            if (_finalMischiefText != null)
            {
                _finalMischiefText.text = $"捣乱值: {GameManager.Instance.CurrentMischiefValue}/{GameManager.Instance.MischiefThreshold}";
            }
        }
        
        #endregion
    }
}
