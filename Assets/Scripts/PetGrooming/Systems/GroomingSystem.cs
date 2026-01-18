using System;
using UnityEngine;
using PetGrooming.Core;

namespace PetGrooming.Systems
{
    /// <summary>
    /// 管理美容流程状态机以及宠物逃脱几率的计算。
    /// 需求：4.1, 4.2, 4.4, 4.5, 4.6
    /// </summary>
    public class GroomingSystem : MonoBehaviour
    {
        #region Enums
        
        /// <summary>
        /// 当前美容流程所处的步骤。
        /// 需求 4.2：美容包含 3 个连续步骤：梳理、清洗、吹干。
        /// </summary>
        public enum GroomingStep
        {
            None,
            Brush,
            Clean,
            Dry,
            Complete
        }
        
        #endregion

        #region Serialized Fields
        
        [Header("Configuration")]
        [SerializeField] private GameConfig _gameConfig;
        
        [Header("Key Bindings")]
        [SerializeField] private KeyCode _brushKey = KeyCode.Alpha1;
        [SerializeField] private KeyCode _cleanKey = KeyCode.Alpha2;
        [SerializeField] private KeyCode _dryKey = KeyCode.Alpha3;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// 当前美容步骤。
        /// </summary>
        public GroomingStep CurrentStep { get; private set; } = GroomingStep.None;
        
        /// <summary>
        /// 已完成的美容步骤数量（0-3）。
        /// </summary>
        public int CompletedStepsCount { get; private set; }
        
        /// <summary>
        /// 当前是否处于进行中的美容流程。
        /// </summary>
        public bool IsGrooming => CurrentStep != GroomingStep.None && CurrentStep != GroomingStep.Complete;
        
        /// <summary>
        /// 宠物的基础逃脱几率。
        /// 需求 4.5：基础逃脱几率为 40%。
        /// </summary>
        public float BaseEscapeChance => _gameConfig != null ? _gameConfig.BaseEscapeChance : 0.4f;
        
        /// <summary>
        /// 每完成一个步骤降低的逃脱几率。
        /// 需求 4.5：每完成一步降低 10%。
        /// </summary>
        public float EscapeChanceReductionPerStep => _gameConfig != null ? _gameConfig.EscapeChanceReductionPerStep : 0.1f;
        
        /// <summary>
        /// 游戏配置引用。
        /// </summary>
        public GameConfig Config => _gameConfig;
        
        /// <summary>
        /// 当前美容步骤所需按下的按键。
        /// </summary>
        public KeyCode CurrentRequiredKey => GetRequiredKeyForStep(CurrentStep);
        
        #endregion

        #region Events
        
        /// <summary>
        /// 当美容步骤变化时触发。
        /// </summary>
        public event Action<GroomingStep> OnStepChanged;
        
        /// <summary>
        /// 美容完成时触发。
        /// 需求 4.6：三个步骤全部完成后，宠物被标记为已美容。
        /// </summary>
        public event Action OnGroomingComplete;
        
        /// <summary>
        /// 美容被取消时触发（如宠物逃脱）。
        /// </summary>
        public event Action OnGroomingCancelled;
        
        /// <summary>
        /// 某个美容步骤成功完成时触发。
        /// </summary>
        public event Action<GroomingStep> OnStepCompleted;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_gameConfig == null)
            {
                Debug.LogError("[GroomingSystem] GameConfig is not assigned!");
            }
        }
        
        private void Update()
        {
            if (!IsGrooming) return;
            
            // 检查当前步骤要求的按键是否被按下
            // 需求 4.4：按下正确按键后进入下一步
            if (Input.GetKeyDown(CurrentRequiredKey))
            {
                AdvanceToNextStep();
            }
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// 开始美容流程。
        /// 需求 4.1：当 Groomer 把被捕获的宠物带到美容台时开始美容。
        /// </summary>
        public void StartGrooming()
        {
            if (IsGrooming)
            {
                Debug.LogWarning("[GroomingSystem] Grooming already in progress!");
                return;
            }
            
            CompletedStepsCount = 0;
            SetStep(GroomingStep.Brush);
            
            Debug.Log("[GroomingSystem] Grooming started - Step 1: Brush");
        }
        
        /// <summary>
        /// 尝试进入下一个美容步骤。
        /// 需求 4.4：玩家按下正确按键时进入下一步。
        /// </summary>
        /// <param name="inputKey">本次按下的按键。</param>
        /// <returns>如果成功进入下一步则返回 true。</returns>
        public bool TryAdvanceStep(KeyCode inputKey)
        {
            if (!IsGrooming)
            {
                return false;
            }
            
            if (inputKey != CurrentRequiredKey)
            {
                return false;
            }
            
            AdvanceToNextStep();
            return true;
        }
        
        /// <summary>
        /// 取消当前美容流程。
        /// </summary>
        public void CancelGrooming()
        {
            if (!IsGrooming && CurrentStep != GroomingStep.Complete)
            {
                return;
            }
            
            var previousStep = CurrentStep;
            SetStep(GroomingStep.None);
            CompletedStepsCount = 0;
            
            OnGroomingCancelled?.Invoke();
            
            Debug.Log($"[GroomingSystem] Grooming cancelled at step: {previousStep}");
        }
        
        /// <summary>
        /// 根据已完成步骤数获取当前逃脱几率。
        /// 需求 4.5：逃脱几率 = 基础几率 - (完成步骤数 × 0.1)。
        /// </summary>
        /// <returns>当前逃脱几率（0.0 到 1.0）。</returns>
        public float GetCurrentEscapeChance()
        {
            return CalculateEscapeChance(CompletedStepsCount, BaseEscapeChance, EscapeChanceReductionPerStep);
        }
        
        /// <summary>
        /// 将美容系统重置为初始状态。
        /// </summary>
        public void Reset()
        {
            CurrentStep = GroomingStep.None;
            CompletedStepsCount = 0;
        }
        
        #endregion

        #region Private Methods
        
        private void SetStep(GroomingStep step)
        {
            if (CurrentStep == step) return;
            
            var previousStep = CurrentStep;
            CurrentStep = step;
            
            OnStepChanged?.Invoke(step);
            
            Debug.Log($"[GroomingSystem] Step changed: {previousStep} → {step}");
        }
        
        private void AdvanceToNextStep()
        {
            var completedStep = CurrentStep;
            var nextStep = GetNextStep(CurrentStep);
            
            // 增加已完成步骤计数
            if (CurrentStep != GroomingStep.None && CurrentStep != GroomingStep.Complete)
            {
                CompletedStepsCount++;
            }
            
            SetStep(nextStep);
            OnStepCompleted?.Invoke(completedStep);
            
            // 检查是否已完成全部步骤
            // 需求 4.6：三个步骤全部完成后，宠物被视为已美容
            if (nextStep == GroomingStep.Complete)
            {
                OnGroomingComplete?.Invoke();
                Debug.Log("[GroomingSystem] Grooming complete!");
            }
            else
            {
                Debug.Log($"[GroomingSystem] Step completed: {completedStep}. Next: {nextStep}. Escape chance: {GetCurrentEscapeChance():P0}");
            }
        }
        
        private KeyCode GetRequiredKeyForStep(GroomingStep step)
        {
            return step switch
            {
                GroomingStep.Brush => _brushKey,
                GroomingStep.Clean => _cleanKey,
                GroomingStep.Dry => _dryKey,
                _ => KeyCode.None
            };
        }
        
        #endregion

        #region Static Calculation Methods (Testable)
        
        /// <summary>
        /// 获取美容流程中的下一个步骤。
        /// 属性 8：美容步骤顺序。
        /// 需求 4.2：步骤按 None → Brush → Clean → Dry → Complete 依次进行。
        /// </summary>
        /// <param name="currentStep">当前的美容步骤。</param>
        /// <returns>下一个美容步骤。</returns>
        public static GroomingStep GetNextStep(GroomingStep currentStep)
        {
            return currentStep switch
            {
                GroomingStep.None => GroomingStep.Brush,
                GroomingStep.Brush => GroomingStep.Clean,
                GroomingStep.Clean => GroomingStep.Dry,
                GroomingStep.Dry => GroomingStep.Complete,
                GroomingStep.Complete => GroomingStep.Complete,
                _ => GroomingStep.None
            };
        }
        
        /// <summary>
        /// 获取指定美容步骤对应的索引（0-3）。
        /// </summary>
        /// <param name="step">美容步骤。</param>
        /// <returns>步骤索引（None 为 0，Brush 为 1，依此类推）。</returns>
        public static int GetStepIndex(GroomingStep step)
        {
            return step switch
            {
                GroomingStep.None => 0,
                GroomingStep.Brush => 1,
                GroomingStep.Clean => 2,
                GroomingStep.Dry => 3,
                GroomingStep.Complete => 4,
                _ => 0
            };
        }
        
        /// <summary>
        /// 根据已完成的美容步骤计算逃脱几率。
        /// 属性 9：逃脱几率衰减公式。
        /// 需求 4.5：逃脱几率 = 基础几率 - (完成步数 × 每步衰减)。
        /// </summary>
        /// <param name="completedSteps">已完成的美容步骤数量（0-3）。</param>
        /// <param name="baseChance">基础逃脱几率（默认 0.4）。</param>
        /// <param name="reductionPerStep">每一步降低的几率（默认 0.1）。</param>
        /// <returns>计算得到的逃脱几率（下限为 0.0）。</returns>
        public static float CalculateEscapeChance(int completedSteps, float baseChance, float reductionPerStep)
        {
            float reduction = completedSteps * reductionPerStep;
            return Mathf.Max(0f, baseChance - reduction);
        }
        
        /// <summary>
        /// 校验给定的步骤序列是否正确。
        /// 属性 8：美容步骤顺序。
        /// </summary>
        /// <param name="steps">要校验的步骤数组。</param>
        /// <returns>如果序列合法则返回 true。</returns>
        public static bool IsValidStepSequence(GroomingStep[] steps)
        {
            if (steps == null || steps.Length == 0) return true;
            
            for (int i = 0; i < steps.Length - 1; i++)
            {
                if (GetNextStep(steps[i]) != steps[i + 1])
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 获取完整的合法美容步骤序列。
        /// 属性 8：美容步骤顺序。
        /// </summary>
        /// <returns>按正确顺序排列的步骤数组。</returns>
        public static GroomingStep[] GetCompleteSequence()
        {
            return new[]
            {
                GroomingStep.None,
                GroomingStep.Brush,
                GroomingStep.Clean,
                GroomingStep.Dry,
                GroomingStep.Complete
            };
        }
        
        /// <summary>
        /// 根据当前步骤判断美容是否已经完成。
        /// 需求 4.6：三个步骤全部完成后宠物被视为已美容。
        /// </summary>
        /// <param name="step">当前步骤。</param>
        /// <returns>如果美容已完成则返回 true。</returns>
        public static bool IsGroomingComplete(GroomingStep step)
        {
            return step == GroomingStep.Complete;
        }
        
        /// <summary>
        /// 获取完成一次完整美容所需的步骤数。
        /// </summary>
        /// <returns>美容步骤数量（3）。</returns>
        public static int GetTotalStepsRequired()
        {
            return 3; // Brush, Clean, Dry
        }
        
        #endregion

        #region Editor Support
        
#if UNITY_EDITOR
        /// <summary>
        /// 设置 GameConfig（测试用）。
        /// </summary>
        public void SetConfigForTesting(GameConfig config)
        {
            _gameConfig = config;
        }
        
        /// <summary>
        /// 设置当前步骤（测试用）。
        /// </summary>
        public void SetStepForTesting(GroomingStep step)
        {
            CurrentStep = step;
        }
        
        /// <summary>
        /// 设置已完成步骤数（测试用）。
        /// </summary>
        public void SetCompletedStepsForTesting(int count)
        {
            CompletedStepsCount = count;
        }
#endif
        
        #endregion
    }
}
