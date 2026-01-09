using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using PetGrooming.Core;

namespace PetGrooming.UI
{
    /// <summary>
    /// Manages the game result screen displaying victory/defeat and replay option.
    /// Requirement 8.6: Display victory/defeat screen with final stats.
    /// </summary>
    public class ResultScreen : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Result Panel")]
        [Tooltip("Main result panel container")]
        [SerializeField] private GameObject _resultPanel;
        
        [Header("Result Display")]
        [Tooltip("Text for result title (Victory/Defeat)")]
        [SerializeField] private TextMeshProUGUI _resultTitleText;
        [Tooltip("Text for result description")]
        [SerializeField] private TextMeshProUGUI _resultDescriptionText;
        [Tooltip("Image for result icon")]
        [SerializeField] private Image _resultIcon;
        
        [Header("Stats Display")]
        [Tooltip("Text for final time")]
        [SerializeField] private TextMeshProUGUI _finalTimeText;
        [Tooltip("Text for final mischief value")]
        [SerializeField] private TextMeshProUGUI _finalMischiefText;
        
        [Header("Buttons")]
        [Tooltip("Button to replay the game")]
        [SerializeField] private Button _replayButton;
        [Tooltip("Button to quit to main menu")]
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
        /// Shows the result screen with the specified outcome.
        /// Requirement 8.6: Display victory/defeat screen with final stats.
        /// </summary>
        /// <param name="groomerWin">True if groomer won, false if pet won.</param>
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
        /// Shows the result screen based on game state.
        /// </summary>
        /// <param name="result">The final game state.</param>
        public void ShowResult(GameManager.GameState result)
        {
            _lastResult = result;
            ShowResult(result == GameManager.GameState.GroomerWin);
        }
        
        /// <summary>
        /// Hides the result screen.
        /// </summary>
        public void HideResult()
        {
            if (_resultPanel != null)
            {
                _resultPanel.SetActive(false);
            }
        }
        
        /// <summary>
        /// Replays the current game.
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
        /// Quits to main menu or reloads scene.
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
