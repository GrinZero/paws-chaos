using System;
using UnityEngine;
using PetGrooming.Core;

namespace PetGrooming.Systems
{
    /// <summary>
    /// Manages the grooming process state machine and escape chance calculations.
    /// Requirements: 4.1, 4.2, 4.4, 4.5, 4.6
    /// </summary>
    public class GroomingSystem : MonoBehaviour
    {
        #region Enums
        
        /// <summary>
        /// Represents the current step in the grooming process.
        /// Requirement 4.2: Grooming consists of 3 sequential steps: Brush, Clean, Dry
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
        /// Current step in the grooming process.
        /// </summary>
        public GroomingStep CurrentStep { get; private set; } = GroomingStep.None;
        
        /// <summary>
        /// Number of completed grooming steps (0-3).
        /// </summary>
        public int CompletedStepsCount { get; private set; }
        
        /// <summary>
        /// Whether grooming is currently in progress.
        /// </summary>
        public bool IsGrooming => CurrentStep != GroomingStep.None && CurrentStep != GroomingStep.Complete;
        
        /// <summary>
        /// Base escape chance for the pet.
        /// Requirement 4.5: 40% base escape chance.
        /// </summary>
        public float BaseEscapeChance => _gameConfig != null ? _gameConfig.BaseEscapeChance : 0.4f;
        
        /// <summary>
        /// Escape chance reduction per completed step.
        /// Requirement 4.5: Reduced by 10% per completed step.
        /// </summary>
        public float EscapeChanceReductionPerStep => _gameConfig != null ? _gameConfig.EscapeChanceReductionPerStep : 0.1f;
        
        /// <summary>
        /// Reference to the game configuration.
        /// </summary>
        public GameConfig Config => _gameConfig;
        
        /// <summary>
        /// The key required for the current grooming step.
        /// </summary>
        public KeyCode CurrentRequiredKey => GetRequiredKeyForStep(CurrentStep);
        
        #endregion

        #region Events
        
        /// <summary>
        /// Fired when the grooming step changes.
        /// </summary>
        public event Action<GroomingStep> OnStepChanged;
        
        /// <summary>
        /// Fired when grooming is complete.
        /// Requirement 4.6: When all 3 steps are completed, Pet is marked as groomed.
        /// </summary>
        public event Action OnGroomingComplete;
        
        /// <summary>
        /// Fired when grooming is cancelled (e.g., pet escapes).
        /// </summary>
        public event Action OnGroomingCancelled;
        
        /// <summary>
        /// Fired when a step is successfully completed.
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
            
            // Check for correct key input
            // Requirement 4.4: When player presses correct key, advance to next step
            if (Input.GetKeyDown(CurrentRequiredKey))
            {
                AdvanceToNextStep();
            }
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Starts the grooming process.
        /// Requirement 4.1: Grooming begins when groomer brings captured pet to station.
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
        /// Attempts to advance to the next grooming step.
        /// Requirement 4.4: When player presses correct key, advance to next step.
        /// </summary>
        /// <param name="inputKey">The key that was pressed.</param>
        /// <returns>True if the step was advanced.</returns>
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
        /// Cancels the current grooming process.
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
        /// Gets the current escape chance based on completed steps.
        /// Requirement 4.5: Escape chance = base_chance - (completed_steps * 0.1)
        /// </summary>
        /// <returns>Current escape chance (0.0 to 1.0).</returns>
        public float GetCurrentEscapeChance()
        {
            return CalculateEscapeChance(CompletedStepsCount, BaseEscapeChance, EscapeChanceReductionPerStep);
        }
        
        /// <summary>
        /// Resets the grooming system to initial state.
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
            
            // Increment completed steps count
            if (CurrentStep != GroomingStep.None && CurrentStep != GroomingStep.Complete)
            {
                CompletedStepsCount++;
            }
            
            SetStep(nextStep);
            OnStepCompleted?.Invoke(completedStep);
            
            // Check for completion
            // Requirement 4.6: When all 3 steps are completed, Pet is marked as groomed
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
        /// Gets the next step in the grooming sequence.
        /// Property 8: Grooming Step Sequence
        /// Requirement 4.2: Steps progress None → Brush → Clean → Dry → Complete
        /// </summary>
        /// <param name="currentStep">The current grooming step.</param>
        /// <returns>The next grooming step.</returns>
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
        /// Gets the step index (0-3) for a given step.
        /// </summary>
        /// <param name="step">The grooming step.</param>
        /// <returns>The step index (0 for None, 1 for Brush, etc.).</returns>
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
        /// Calculates the escape chance based on completed grooming steps.
        /// Property 9: Escape Chance Reduction Formula
        /// Requirement 4.5: Escape chance = base_chance - (n * reduction_per_step)
        /// </summary>
        /// <param name="completedSteps">Number of completed grooming steps (0-3).</param>
        /// <param name="baseChance">Base escape chance (default 0.4).</param>
        /// <param name="reductionPerStep">Reduction per step (default 0.1).</param>
        /// <returns>The calculated escape chance (clamped to 0.0 minimum).</returns>
        public static float CalculateEscapeChance(int completedSteps, float baseChance, float reductionPerStep)
        {
            float reduction = completedSteps * reductionPerStep;
            return Mathf.Max(0f, baseChance - reduction);
        }
        
        /// <summary>
        /// Validates that a step sequence is correct.
        /// Property 8: Grooming Step Sequence
        /// </summary>
        /// <param name="steps">Array of steps to validate.</param>
        /// <returns>True if the sequence is valid.</returns>
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
        /// Gets the complete valid grooming sequence.
        /// Property 8: Grooming Step Sequence
        /// </summary>
        /// <returns>Array of steps in correct order.</returns>
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
        /// Checks if grooming is complete based on step.
        /// Requirement 4.6: All 3 steps completed marks pet as groomed.
        /// </summary>
        /// <param name="step">The current step.</param>
        /// <returns>True if grooming is complete.</returns>
        public static bool IsGroomingComplete(GroomingStep step)
        {
            return step == GroomingStep.Complete;
        }
        
        /// <summary>
        /// Gets the number of steps required to complete grooming.
        /// </summary>
        /// <returns>Number of grooming steps (3).</returns>
        public static int GetTotalStepsRequired()
        {
            return 3; // Brush, Clean, Dry
        }
        
        #endregion

        #region Editor Support
        
#if UNITY_EDITOR
        /// <summary>
        /// Sets the game config for testing purposes.
        /// </summary>
        public void SetConfigForTesting(GameConfig config)
        {
            _gameConfig = config;
        }
        
        /// <summary>
        /// Sets the current step for testing purposes.
        /// </summary>
        public void SetStepForTesting(GroomingStep step)
        {
            CurrentStep = step;
        }
        
        /// <summary>
        /// Sets the completed steps count for testing purposes.
        /// </summary>
        public void SetCompletedStepsForTesting(int count)
        {
            CompletedStepsCount = count;
        }
#endif
        
        #endregion
    }
}
