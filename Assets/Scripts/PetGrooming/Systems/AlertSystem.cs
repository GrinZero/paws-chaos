using System;
using System.Collections;
using UnityEngine;
using PetGrooming.Core;

namespace PetGrooming.Systems
{
    /// <summary>
    /// 当恶作剧值接近阈值时管理警戒状态的系统。
    /// 需求：6.3, 6.4, 6.5
    /// 属性 16：警戒状态触发条件
    /// 属性 17：警戒状态速度加成
    /// </summary>
    public class AlertSystem : MonoBehaviour
    {
        #region Singleton
        
        private static AlertSystem _instance;
        
        public static AlertSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<AlertSystem>();
                    if (_instance == null)
                    {
                        Debug.LogError("[AlertSystem] No AlertSystem instance found in scene!");
                    }
                }
                return _instance;
            }
        }
        
        #endregion

        #region Serialized Fields
        
        [Header("Configuration")]
        [SerializeField] private Phase2GameConfig _phase2Config;
        
        [Header("Alert Lights")]
        [Tooltip("Lights that flash during alert state")]
        [SerializeField] private Light[] _alertLights;
        
        [Header("Alert Sound")]
        [Tooltip("Audio source for alert sound")]
        [SerializeField] private AudioSource _alertSound;
        
        [Header("Visual Settings")]
        [Tooltip("Color of alert lights when on")]
        [SerializeField] private Color _alertLightColor = Color.red;
        
        [Tooltip("Original color of alert lights")]
        [SerializeField] private Color _normalLightColor = Color.white;
        
        #endregion

        #region Private Fields
        
        private Coroutine _flashCoroutine;
        private bool _lightsOn;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// Whether the alert state is currently active.
        /// Property 16: Alert State Trigger Condition
        /// Requirement 6.3: Alert triggers at (threshold - 100)
        /// </summary>
        public bool IsAlertActive { get; private set; }
        
        /// <summary>
        /// Speed bonus applied to Groomer during alert state.
        /// Property 17: Alert State Speed Bonus
        /// Requirement 6.5: 10% movement speed bonus
        /// </summary>
        public float GroomerSpeedBonus => _phase2Config != null ? _phase2Config.AlertGroomerSpeedBonus : 0.1f;
        
        /// <summary>
        /// Interval between light flashes in seconds.
        /// Requirement 6.4: Flashing lights
        /// </summary>
        public float FlashInterval => _phase2Config != null ? _phase2Config.AlertFlashInterval : 0.5f;
        
        /// <summary>
        /// Reference to the Phase 2 game configuration.
        /// </summary>
        public Phase2GameConfig Phase2Config => _phase2Config;
        
        #endregion

        #region Events
        
        /// <summary>
        /// Fired when alert state starts.
        /// </summary>
        public event Action OnAlertStarted;
        
        /// <summary>
        /// Fired when alert state ends.
        /// </summary>
        public event Action OnAlertEnded;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            // 单例初始化
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[AlertSystem] Duplicate AlertSystem detected, destroying this instance.");
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            
            if (_phase2Config == null)
            {
                Debug.LogWarning("[AlertSystem] Phase2GameConfig is not assigned, using default values.");
            }
        }
        
        private void Start()
        {
            // 订阅恶作剧值变化事件
            if (MischiefSystem.Instance != null)
            {
                MischiefSystem.Instance.OnMischiefValueChanged += OnMischiefValueChanged;
            }
            
            // 初始化为关闭灯光状态
            SetLightsState(false);
        }
        
        private void OnDestroy()
        {
            // 取消事件订阅
            if (MischiefSystem.Instance != null)
            {
                MischiefSystem.Instance.OnMischiefValueChanged -= OnMischiefValueChanged;
            }
            
            if (_instance == this)
            {
                _instance = null;
            }
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// 开启警戒状态。
        /// 需求 6.4：显示闪烁警示灯并播放警报音效。
        /// 需求 6.5：Groomer 获得 10% 移动速度加成。
        /// </summary>
        public void StartAlert()
        {
            if (IsAlertActive)
            {
                Debug.Log("[AlertSystem] Alert already active.");
                return;
            }
            
            IsAlertActive = true;
            
            // 开始切换警报灯闪烁协程
            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
            }
            _flashCoroutine = StartCoroutine(FlashLightsCoroutine());
            
            // 播放警报音效
            PlayAlertSound();
            
            OnAlertStarted?.Invoke();
            
            Debug.Log("[AlertSystem] Alert state started!");
        }
        
        /// <summary>
        /// 关闭警戒状态。
        /// </summary>
        public void StopAlert()
        {
            if (!IsAlertActive)
            {
                Debug.Log("[AlertSystem] Alert not active.");
                return;
            }
            
            IsAlertActive = false;
            
            // 停止灯光闪烁
            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
                _flashCoroutine = null;
            }
            
            // 将灯光恢复为正常状态
            SetLightsState(false);
            
            // 停止警报音效
            StopAlertSound();
            
            OnAlertEnded?.Invoke();
            
            Debug.Log("[AlertSystem] Alert state ended.");
        }
        
        /// <summary>
        /// 根据当前恶作剧值检查是否应进入警戒状态。
        /// 属性 16：警戒状态触发条件。
        /// 需求 6.3：在（阈值 - 100）时触发警戒。
        /// </summary>
        /// <param name="currentMischief">当前恶作剧值。</param>
        public void CheckAlertCondition(int currentMischief)
        {
            if (MischiefSystem.Instance == null)
            {
                return;
            }
            
            int threshold = MischiefSystem.Instance.MischiefThreshold;
            int alertThreshold = GetAlertTriggerThreshold(threshold);
            
            bool shouldBeAlert = ShouldTriggerAlert(currentMischief, alertThreshold);
            
            if (shouldBeAlert && !IsAlertActive)
            {
                StartAlert();
            }
            // 设计说明：一旦进入警戒状态不会自动结束，将持续到游戏结束
        }
        
        /// <summary>
        /// 获取 Groomer 在当前警戒状态下的速度倍率。
        /// 属性 17：警戒状态速度加成。
        /// 需求 6.5：警戒状态下移动速度提升 10%。
        /// </summary>
        /// <returns>速度倍率（非警戒为 1.0，警戒为 1.1）。</returns>
        public float GetGroomerSpeedMultiplier()
        {
            return CalculateGroomerSpeedMultiplier(IsAlertActive, GroomerSpeedBonus);
        }
        
        #endregion

        #region Private Methods
        
        private void OnMischiefValueChanged(int newValue)
        {
            CheckAlertCondition(newValue);
        }
        
        private IEnumerator FlashLightsCoroutine()
        {
            while (IsAlertActive)
            {
                // 切换灯光开关状态
                _lightsOn = !_lightsOn;
                SetLightsState(_lightsOn);
                
                yield return new WaitForSeconds(FlashInterval);
            }
        }
        
        private void SetLightsState(bool on)
        {
            if (_alertLights == null || _alertLights.Length == 0)
            {
                return;
            }
            
            foreach (Light light in _alertLights)
            {
                if (light != null)
                {
                    light.enabled = on;
                    light.color = on ? _alertLightColor : _normalLightColor;
                }
            }
        }
        
        private void PlayAlertSound()
        {
            if (_alertSound != null && !_alertSound.isPlaying)
            {
                _alertSound.loop = true;
                _alertSound.Play();
            }
        }
        
        private void StopAlertSound()
        {
            if (_alertSound != null && _alertSound.isPlaying)
            {
                _alertSound.Stop();
            }
        }
        
        #endregion

        #region Static Calculation Methods (Testable)
        
        /// <summary>
        /// 计算警戒状态的触发阈值。
        /// 属性 16：警戒状态触发条件。
        /// 需求 6.3：在（恶作剧阈值 - 100）时触发警戒。
        /// </summary>
        /// <param name="mischiefThreshold">宠物获胜所需的恶作剧阈值。</param>
        /// <param name="alertOffset">距离阈值的偏移量（默认 100）。</param>
        /// <returns>警戒触发时的恶作剧值。</returns>
        public static int GetAlertTriggerThreshold(int mischiefThreshold, int alertOffset = 100)
        {
            return mischiefThreshold - alertOffset;
        }
        
        /// <summary>
        /// 根据恶作剧值判断是否应触发警戒。
        /// 属性 16：警戒状态触发条件。
        /// 需求 6.3：当恶作剧值达到（阈值 - 100）时进入警戒状态。
        /// </summary>
        /// <param name="currentMischief">当前恶作剧值。</param>
        /// <param name="alertTriggerThreshold">警戒触发阈值。</param>
        /// <returns>应处于警戒状态时返回 true。</returns>
        public static bool ShouldTriggerAlert(int currentMischief, int alertTriggerThreshold)
        {
            return currentMischief >= alertTriggerThreshold;
        }
        
        /// <summary>
        /// 计算警戒状态下 Groomer 的速度倍率。
        /// 属性 17：警戒状态速度加成。
        /// 需求 6.5：警戒状态下移动速度提高 10%。
        /// </summary>
        /// <param name="isAlertActive">当前是否处于警戒状态。</param>
        /// <param name="speedBonus">速度加成比例（0.1 表示 10%）。</param>
        /// <returns>速度倍率（警戒时为 1.0 + 加成，否则为 1.0）。</returns>
        public static float CalculateGroomerSpeedMultiplier(bool isAlertActive, float speedBonus)
        {
            if (isAlertActive)
            {
                return 1.0f + speedBonus;
            }
            return 1.0f;
        }
        
        /// <summary>
        /// 校验警戒状态触发条件是否满足。
        /// 属性 16：警戒状态触发条件。
        /// 需求 6.3：当恶作剧值达到（阈值 - 100）时应处于警戒状态。
        /// </summary>
        /// <param name="mischiefValue">当前恶作剧值。</param>
        /// <param name="mischiefThreshold">宠物获胜所需的恶作剧阈值。</param>
        /// <param name="alertOffset">距离阈值的偏移量。</param>
        /// <param name="isAlertActive">当前是否处于警戒状态。</param>
        /// <returns>在给定恶作剧值下，警戒状态是否合理。</returns>
        public static bool ValidateAlertTriggerCondition(
            int mischiefValue, 
            int mischiefThreshold, 
            int alertOffset, 
            bool isAlertActive)
        {
            int alertTriggerThreshold = GetAlertTriggerThreshold(mischiefThreshold, alertOffset);
            bool shouldBeAlert = ShouldTriggerAlert(mischiefValue, alertTriggerThreshold);
            
            // If mischief is at or above alert threshold, alert should be active
            if (shouldBeAlert)
            {
                return isAlertActive;
            }
            
            // If mischief is below alert threshold, alert can be either state
            // (once triggered, alert stays on until game ends)
            return true;
        }
        
        /// <summary>
        /// 校验警戒状态下的速度加成是否正确。
        /// 属性 17：警戒状态速度加成。
        /// 需求 6.5：警戒状态下 Groomer 获得 10% 速度加成。
        /// </summary>
        /// <param name="isAlertActive">当前是否处于警戒状态。</param>
        /// <param name="expectedBonus">期望的速度加成（0.1 表示 10%）。</param>
        /// <param name="actualMultiplier">实际应用的速度倍率。</param>
        /// <returns>如果加成正确应用则返回 true。</returns>
        public static bool ValidateAlertSpeedBonus(
            bool isAlertActive, 
            float expectedBonus, 
            float actualMultiplier)
        {
            float expectedMultiplier = CalculateGroomerSpeedMultiplier(isAlertActive, expectedBonus);
            return Mathf.Approximately(actualMultiplier, expectedMultiplier);
        }
        
        #endregion

        #region Editor Support
        
#if UNITY_EDITOR
        /// <summary>
        /// 设置 Phase2GameConfig（测试用）。
        /// </summary>
        public void SetPhase2ConfigForTesting(Phase2GameConfig config)
        {
            _phase2Config = config;
        }
        
        /// <summary>
        /// 直接设置警戒状态（测试用）。
        /// </summary>
        public void SetAlertStateForTesting(bool isActive)
        {
            IsAlertActive = isActive;
        }
        
        /// <summary>
        /// 获取警报灯数组（测试用）。
        /// </summary>
        public Light[] GetAlertLightsForTesting()
        {
            return _alertLights;
        }
        
        /// <summary>
        /// 设置警报灯数组（测试用）。
        /// </summary>
        public void SetAlertLightsForTesting(Light[] lights)
        {
            _alertLights = lights;
        }
        
        /// <summary>
        /// 获取警报音效 AudioSource（测试用）。
        /// </summary>
        public AudioSource GetAlertSoundForTesting()
        {
            return _alertSound;
        }
        
        /// <summary>
        /// 设置警报音效 AudioSource（测试用）。
        /// </summary>
        public void SetAlertSoundForTesting(AudioSource sound)
        {
            _alertSound = sound;
        }
#endif
        
        #endregion
    }
}
