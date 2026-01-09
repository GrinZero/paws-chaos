using System;
using NUnit.Framework;
using UnityEngine;
using PetGrooming.Systems;

namespace PetGrooming.Tests.Editor
{
    /// <summary>
    /// Property-based tests for GroomingSystem behavior.
    /// Uses NUnit with manual random input generation (100+ iterations per property).
    /// 
    /// Feature: pet-grooming-mvp
    /// Validates: Requirements 4.2, 4.4, 4.5, 4.6
    /// </summary>
    [TestFixture]
    public class GroomingSystemPropertyTests
    {
        private const int PropertyTestIterations = 100;
        private System.Random _random;
        
        // Default config values
        private const float DefaultBaseEscapeChance = 0.4f;
        private const float DefaultEscapeChanceReductionPerStep = 0.1f;
        
        [SetUp]
        public void SetUp()
        {
            _random = new System.Random(42); // Fixed seed for reproducibility
        }
        
        #region Property 8: Grooming Step Sequence
        
        /// <summary>
        /// Feature: pet-grooming-mvp, Property 8: Grooming Step Sequence
        /// 
        /// *For any* grooming process, the steps shall always progress in the order:
        /// None → Brush → Clean → Dry → Complete, and completing all steps shall 
        /// mark the Pet as groomed.
        /// 
        /// Validates: Requirements 4.2, 4.4, 4.6
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property8_StepSequence_ShouldFollowCorrectOrder()
        {
            // The complete sequence should always be: None → Brush → Clean → Dry → Complete
            var expectedSequence = GroomingSystem.GetCompleteSequence();
            
            for (int i = 0; i < expectedSequence.Length - 1; i++)
            {
                var currentStep = expectedSequence[i];
                var expectedNextStep = expectedSequence[i + 1];
                var actualNextStep = GroomingSystem.GetNextStep(currentStep);
                
                Assert.AreEqual(
                    expectedNextStep,
                    actualNextStep,
                    $"After {currentStep}, next step should be {expectedNextStep}, but got {actualNextStep}"
                );
            }
        }
        
        /// <summary>
        /// Property 8: GetNextStep should always return a valid next step
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property8_GetNextStep_ShouldAlwaysReturnValidStep()
        {
            var allSteps = (GroomingSystem.GroomingStep[])Enum.GetValues(typeof(GroomingSystem.GroomingStep));
            
            foreach (var step in allSteps)
            {
                var nextStep = GroomingSystem.GetNextStep(step);
                
                // Next step should be a valid enum value
                Assert.IsTrue(
                    Enum.IsDefined(typeof(GroomingSystem.GroomingStep), nextStep),
                    $"GetNextStep({step}) returned invalid step: {nextStep}"
                );
            }
        }
        
        /// <summary>
        /// Property 8: Complete step should remain Complete (idempotent)
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property8_CompleteStep_ShouldRemainComplete()
        {
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                var nextStep = GroomingSystem.GetNextStep(GroomingSystem.GroomingStep.Complete);
                
                Assert.AreEqual(
                    GroomingSystem.GroomingStep.Complete,
                    nextStep,
                    "Complete step should remain Complete"
                );
            }
        }
        
        /// <summary>
        /// Property 8: Step sequence validation should work correctly
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property8_ValidSequence_ShouldBeRecognizedAsValid()
        {
            // Valid sequences
            var validSequences = new[]
            {
                new[] { GroomingSystem.GroomingStep.None, GroomingSystem.GroomingStep.Brush },
                new[] { GroomingSystem.GroomingStep.Brush, GroomingSystem.GroomingStep.Clean },
                new[] { GroomingSystem.GroomingStep.Clean, GroomingSystem.GroomingStep.Dry },
                new[] { GroomingSystem.GroomingStep.Dry, GroomingSystem.GroomingStep.Complete },
                GroomingSystem.GetCompleteSequence()
            };
            
            foreach (var sequence in validSequences)
            {
                Assert.IsTrue(
                    GroomingSystem.IsValidStepSequence(sequence),
                    $"Sequence should be valid: {string.Join(" → ", sequence)}"
                );
            }
        }
        
        /// <summary>
        /// Property 8: Invalid sequences should be detected
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property8_InvalidSequence_ShouldBeRecognizedAsInvalid()
        {
            // Invalid sequences (wrong order)
            var invalidSequences = new[]
            {
                new[] { GroomingSystem.GroomingStep.Brush, GroomingSystem.GroomingStep.None },
                new[] { GroomingSystem.GroomingStep.Clean, GroomingSystem.GroomingStep.Brush },
                new[] { GroomingSystem.GroomingStep.Dry, GroomingSystem.GroomingStep.Clean },
                new[] { GroomingSystem.GroomingStep.Complete, GroomingSystem.GroomingStep.Dry },
                new[] { GroomingSystem.GroomingStep.None, GroomingSystem.GroomingStep.Clean },
                new[] { GroomingSystem.GroomingStep.None, GroomingSystem.GroomingStep.Dry },
                new[] { GroomingSystem.GroomingStep.None, GroomingSystem.GroomingStep.Complete }
            };
            
            foreach (var sequence in invalidSequences)
            {
                Assert.IsFalse(
                    GroomingSystem.IsValidStepSequence(sequence),
                    $"Sequence should be invalid: {string.Join(" → ", sequence)}"
                );
            }
        }
        
        /// <summary>
        /// Property 8: Grooming is complete only at Complete step
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property8_IsGroomingComplete_ShouldOnlyBeTrueAtCompleteStep()
        {
            var allSteps = (GroomingSystem.GroomingStep[])Enum.GetValues(typeof(GroomingSystem.GroomingStep));
            
            foreach (var step in allSteps)
            {
                bool isComplete = GroomingSystem.IsGroomingComplete(step);
                bool expectedComplete = step == GroomingSystem.GroomingStep.Complete;
                
                Assert.AreEqual(
                    expectedComplete,
                    isComplete,
                    $"IsGroomingComplete({step}) should be {expectedComplete}"
                );
            }
        }
        
        /// <summary>
        /// Property 8: Total steps required should be 3
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property8_TotalStepsRequired_ShouldBeThree()
        {
            Assert.AreEqual(
                3,
                GroomingSystem.GetTotalStepsRequired(),
                "Total grooming steps should be 3 (Brush, Clean, Dry)"
            );
        }
        
        /// <summary>
        /// Property 8: Step indices should be monotonically increasing
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property8_StepIndices_ShouldBeMonotonicallyIncreasing()
        {
            var sequence = GroomingSystem.GetCompleteSequence();
            
            for (int i = 0; i < sequence.Length - 1; i++)
            {
                int currentIndex = GroomingSystem.GetStepIndex(sequence[i]);
                int nextIndex = GroomingSystem.GetStepIndex(sequence[i + 1]);
                
                Assert.Less(
                    currentIndex,
                    nextIndex,
                    $"Step index should increase: {sequence[i]}({currentIndex}) → {sequence[i + 1]}({nextIndex})"
                );
            }
        }
        
        #endregion
        
        #region Property 9: Escape Chance Reduction Formula
        
        /// <summary>
        /// Feature: pet-grooming-mvp, Property 9: Escape Chance Reduction Formula
        /// 
        /// *For any* grooming step count n (0 to 3), the Pet's escape chance shall 
        /// equal (base_chance - n * 0.1), where base_chance is 0.4.
        /// 
        /// Validates: Requirements 4.5
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property9_EscapeChance_ShouldFollowFormula()
        {
            for (int steps = 0; steps <= 3; steps++)
            {
                float expectedChance = DefaultBaseEscapeChance - (steps * DefaultEscapeChanceReductionPerStep);
                float actualChance = GroomingSystem.CalculateEscapeChance(
                    steps, 
                    DefaultBaseEscapeChance, 
                    DefaultEscapeChanceReductionPerStep
                );
                
                Assert.AreEqual(
                    expectedChance,
                    actualChance,
                    0.001f,
                    $"Escape chance for {steps} steps should be {expectedChance}, got {actualChance}"
                );
            }
        }
        
        /// <summary>
        /// Property 9: Escape chance should decrease with each step
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property9_EscapeChance_ShouldDecreaseWithEachStep()
        {
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Generate random but valid base chance and reduction
                float baseChance = (float)(_random.NextDouble() * 0.5 + 0.2); // 0.2 to 0.7
                float reductionPerStep = (float)(_random.NextDouble() * 0.15 + 0.05); // 0.05 to 0.2
                
                float chance0 = GroomingSystem.CalculateEscapeChance(0, baseChance, reductionPerStep);
                float chance1 = GroomingSystem.CalculateEscapeChance(1, baseChance, reductionPerStep);
                float chance2 = GroomingSystem.CalculateEscapeChance(2, baseChance, reductionPerStep);
                float chance3 = GroomingSystem.CalculateEscapeChance(3, baseChance, reductionPerStep);
                
                Assert.GreaterOrEqual(chance0, chance1, "Chance should decrease after step 1");
                Assert.GreaterOrEqual(chance1, chance2, "Chance should decrease after step 2");
                Assert.GreaterOrEqual(chance2, chance3, "Chance should decrease after step 3");
            }
        }
        
        /// <summary>
        /// Property 9: Escape chance should never be negative
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property9_EscapeChance_ShouldNeverBeNegative()
        {
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Generate random values that could potentially cause negative results
                int steps = _random.Next(0, 20);
                float baseChance = (float)(_random.NextDouble());
                float reductionPerStep = (float)(_random.NextDouble() * 0.5);
                
                float chance = GroomingSystem.CalculateEscapeChance(steps, baseChance, reductionPerStep);
                
                Assert.GreaterOrEqual(
                    chance,
                    0f,
                    $"Escape chance should never be negative. Steps: {steps}, Base: {baseChance}, Reduction: {reductionPerStep}, Result: {chance}"
                );
            }
        }
        
        /// <summary>
        /// Property 9: Escape chance at 0 steps should equal base chance
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property9_EscapeChanceAtZeroSteps_ShouldEqualBaseChance()
        {
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float baseChance = (float)(_random.NextDouble());
                float reductionPerStep = (float)(_random.NextDouble() * 0.5);
                
                float chance = GroomingSystem.CalculateEscapeChance(0, baseChance, reductionPerStep);
                
                Assert.AreEqual(
                    baseChance,
                    chance,
                    0.001f,
                    $"Escape chance at 0 steps should equal base chance {baseChance}"
                );
            }
        }
        
        /// <summary>
        /// Property 9: Escape chance reduction should be linear
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property9_EscapeChanceReduction_ShouldBeLinear()
        {
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float baseChance = (float)(_random.NextDouble() * 0.5 + 0.3); // 0.3 to 0.8
                float reductionPerStep = (float)(_random.NextDouble() * 0.1 + 0.05); // 0.05 to 0.15
                
                // Calculate chances for consecutive steps
                float chance0 = GroomingSystem.CalculateEscapeChance(0, baseChance, reductionPerStep);
                float chance1 = GroomingSystem.CalculateEscapeChance(1, baseChance, reductionPerStep);
                float chance2 = GroomingSystem.CalculateEscapeChance(2, baseChance, reductionPerStep);
                
                // The difference between consecutive steps should be constant (linear)
                float diff01 = chance0 - chance1;
                float diff12 = chance1 - chance2;
                
                // Both differences should equal reductionPerStep (if not clamped to 0)
                if (chance1 > 0 && chance2 > 0)
                {
                    Assert.AreEqual(
                        diff01,
                        diff12,
                        0.001f,
                        $"Reduction should be linear: diff01={diff01}, diff12={diff12}"
                    );
                }
            }
        }
        
        /// <summary>
        /// Property 9: Specific escape chance values for default config
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property9_DefaultConfig_ShouldHaveCorrectEscapeChances()
        {
            // With default config: base=0.4, reduction=0.1
            // Step 0: 0.4 - 0*0.1 = 0.4
            // Step 1: 0.4 - 1*0.1 = 0.3
            // Step 2: 0.4 - 2*0.1 = 0.2
            // Step 3: 0.4 - 3*0.1 = 0.1
            
            var expectedChances = new[] { 0.4f, 0.3f, 0.2f, 0.1f };
            
            for (int steps = 0; steps <= 3; steps++)
            {
                float actualChance = GroomingSystem.CalculateEscapeChance(
                    steps,
                    DefaultBaseEscapeChance,
                    DefaultEscapeChanceReductionPerStep
                );
                
                Assert.AreEqual(
                    expectedChances[steps],
                    actualChance,
                    0.001f,
                    $"Escape chance at step {steps} should be {expectedChances[steps]}"
                );
            }
        }
        
        /// <summary>
        /// Property 9: Escape chance should clamp to 0 when reduction exceeds base
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property9_EscapeChance_ShouldClampToZeroWhenReductionExceedsBase()
        {
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float baseChance = (float)(_random.NextDouble() * 0.3); // 0 to 0.3
                float reductionPerStep = 0.2f; // High reduction
                int steps = 5; // Many steps to ensure we exceed base
                
                float chance = GroomingSystem.CalculateEscapeChance(steps, baseChance, reductionPerStep);
                
                Assert.AreEqual(
                    0f,
                    chance,
                    0.001f,
                    $"Escape chance should clamp to 0 when reduction ({steps * reductionPerStep}) exceeds base ({baseChance})"
                );
            }
        }
        
        #endregion
        
        #region Additional Sequence Tests
        
        /// <summary>
        /// Empty sequence should be valid
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void StepSequence_EmptySequence_ShouldBeValid()
        {
            Assert.IsTrue(
                GroomingSystem.IsValidStepSequence(new GroomingSystem.GroomingStep[0]),
                "Empty sequence should be valid"
            );
            
            Assert.IsTrue(
                GroomingSystem.IsValidStepSequence(null),
                "Null sequence should be valid"
            );
        }
        
        /// <summary>
        /// Single step sequence should be valid
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void StepSequence_SingleStep_ShouldBeValid()
        {
            var allSteps = (GroomingSystem.GroomingStep[])Enum.GetValues(typeof(GroomingSystem.GroomingStep));
            
            foreach (var step in allSteps)
            {
                Assert.IsTrue(
                    GroomingSystem.IsValidStepSequence(new[] { step }),
                    $"Single step sequence [{step}] should be valid"
                );
            }
        }
        
        #endregion
    }
}
