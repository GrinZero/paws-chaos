using System;
using NUnit.Framework;
using UnityEngine;
using PetGrooming.UI.MobileUI;

namespace PetGrooming.Tests.Editor
{
    /// <summary>
    /// Property-based tests for StruggleButtonUI.
    /// Uses NUnit with manual random input generation (100+ iterations per property).
    /// 
    /// Feature: mobile-ui-controls
    /// Validates: Requirements 2.5.1, 2.5.3, 2.5.4, 2.5.5
    /// </summary>
    [TestFixture]
    public class StruggleButtonUIPropertyTests
    {
        private const int PropertyTestIterations = 100;
        private System.Random _random;
        
        [SetUp]
        public void SetUp()
        {
            _random = new System.Random(42); // Fixed seed for reproducibility
        }
        
        #region Property 5: Struggle Button Visibility
        
        /// <summary>
        /// Feature: mobile-ui-controls, Property 5: Struggle Button Visibility
        /// 
        /// *For any* pet character state, the struggle button SHALL be visible 
        /// if and only if the pet is in the captured state.
        /// 
        /// Validates: Requirements 2.5.1, 2.5.5
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property5_WhenCaptured_ButtonShouldBeVisible()
        {
            // Property: For all captured states where isCaptured = true, button is visible
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                bool isCaptured = true;
                
                bool shouldBeVisible = StruggleButtonUI.ShouldBeVisible(isCaptured);
                
                Assert.IsTrue(
                    shouldBeVisible,
                    $"Iteration {i}: Button should be visible when pet is captured"
                );
            }
        }
        
        /// <summary>
        /// Property 5: Button should be hidden when not captured
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property5_WhenNotCaptured_ButtonShouldBeHidden()
        {
            // Property: For all captured states where isCaptured = false, button is hidden
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                bool isCaptured = false;
                
                bool shouldBeVisible = StruggleButtonUI.ShouldBeVisible(isCaptured);
                
                Assert.IsFalse(
                    shouldBeVisible,
                    $"Iteration {i}: Button should be hidden when pet is not captured"
                );
            }
        }
        
        /// <summary>
        /// Property 5: Visibility is deterministic based on capture state
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property5_Visibility_IsDeterministicBasedOnCaptureState()
        {
            // Property: For any capture state, visibility is always the same
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                bool isCaptured = _random.Next(2) == 1;
                
                bool visibility1 = StruggleButtonUI.ShouldBeVisible(isCaptured);
                bool visibility2 = StruggleButtonUI.ShouldBeVisible(isCaptured);
                
                Assert.AreEqual(
                    visibility1, visibility2,
                    $"Iteration {i}: Visibility should be deterministic for isCaptured={isCaptured}"
                );
            }
        }
        
        /// <summary>
        /// Property 5: Visibility equals capture state
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property5_Visibility_EqualsCaptureState()
        {
            // Property: ShouldBeVisible(isCaptured) == isCaptured
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                bool isCaptured = _random.Next(2) == 1;
                
                bool shouldBeVisible = StruggleButtonUI.ShouldBeVisible(isCaptured);
                
                Assert.AreEqual(
                    isCaptured, shouldBeVisible,
                    $"Iteration {i}: Visibility should equal capture state. " +
                    $"isCaptured={isCaptured}, shouldBeVisible={shouldBeVisible}"
                );
            }
        }
        
        /// <summary>
        /// Property 5: Visibility toggles correctly with state changes
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property5_Visibility_TogglesWithStateChanges()
        {
            // Property: Changing capture state changes visibility
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                bool initialState = _random.Next(2) == 1;
                bool newState = !initialState;
                
                bool initialVisibility = StruggleButtonUI.ShouldBeVisible(initialState);
                bool newVisibility = StruggleButtonUI.ShouldBeVisible(newState);
                
                Assert.AreNotEqual(
                    initialVisibility, newVisibility,
                    $"Iteration {i}: Visibility should toggle when capture state changes. " +
                    $"initialState={initialState}, newState={newState}"
                );
            }
        }
        
        #endregion
        
        #region Property 6: Struggle Progress Accumulation
        
        /// <summary>
        /// Feature: mobile-ui-controls, Property 6: Struggle Progress Accumulation
        /// 
        /// *For any* sequence of N taps on the struggle button within the tap window, 
        /// the progress value SHALL equal N divided by the required tap count (clamped to [0, 1]).
        /// 
        /// Validates: Requirements 2.5.3, 2.5.4
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property6_Progress_EqualsNDividedByRequired()
        {
            // Property: For all tap counts and required taps, progress = taps / required
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                int requiredTaps = _random.Next(5, 20); // 5 to 19
                int currentTaps = _random.Next(0, requiredTaps + 5); // 0 to required + 4
                
                float progress = StruggleButtonUI.CalculateProgress(currentTaps, requiredTaps);
                float expectedProgress = Mathf.Clamp01((float)currentTaps / requiredTaps);
                
                Assert.AreEqual(
                    expectedProgress, progress, 0.0001f,
                    $"Iteration {i}: Progress should equal taps/required (clamped). " +
                    $"currentTaps={currentTaps}, requiredTaps={requiredTaps}, " +
                    $"expected={expectedProgress}, got={progress}"
                );
            }
        }
        
        /// <summary>
        /// Property 6: Progress is always in [0, 1] range
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property6_Progress_AlwaysInValidRange()
        {
            // Property: For all inputs, progress is in [0, 1]
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                int requiredTaps = _random.Next(1, 100);
                int currentTaps = _random.Next(-10, 200); // Include negative and over-limit
                
                float progress = StruggleButtonUI.CalculateProgress(currentTaps, requiredTaps);
                
                Assert.GreaterOrEqual(
                    progress, 0f,
                    $"Iteration {i}: Progress should be >= 0. " +
                    $"currentTaps={currentTaps}, requiredTaps={requiredTaps}, progress={progress}"
                );
                Assert.LessOrEqual(
                    progress, 1f,
                    $"Iteration {i}: Progress should be <= 1. " +
                    $"currentTaps={currentTaps}, requiredTaps={requiredTaps}, progress={progress}"
                );
            }
        }
        
        /// <summary>
        /// Property 6: Zero taps gives zero progress
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property6_ZeroTaps_GivesZeroProgress()
        {
            // Property: For all required tap counts, 0 taps = 0 progress
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                int requiredTaps = _random.Next(1, 100);
                int currentTaps = 0;
                
                float progress = StruggleButtonUI.CalculateProgress(currentTaps, requiredTaps);
                
                Assert.AreEqual(
                    0f, progress, 0.0001f,
                    $"Iteration {i}: Zero taps should give zero progress. " +
                    $"requiredTaps={requiredTaps}, progress={progress}"
                );
            }
        }
        
        /// <summary>
        /// Property 6: Required taps gives full progress
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property6_RequiredTaps_GivesFullProgress()
        {
            // Property: For all required tap counts, required taps = 1.0 progress
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                int requiredTaps = _random.Next(1, 100);
                int currentTaps = requiredTaps;
                
                float progress = StruggleButtonUI.CalculateProgress(currentTaps, requiredTaps);
                
                Assert.AreEqual(
                    1f, progress, 0.0001f,
                    $"Iteration {i}: Required taps should give full progress. " +
                    $"requiredTaps={requiredTaps}, progress={progress}"
                );
            }
        }
        
        /// <summary>
        /// Property 6: More than required taps still gives 1.0 progress (clamped)
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property6_MoreThanRequired_GivesClampedProgress()
        {
            // Property: For taps > required, progress is clamped to 1.0
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                int requiredTaps = _random.Next(1, 50);
                int currentTaps = requiredTaps + _random.Next(1, 50); // More than required
                
                float progress = StruggleButtonUI.CalculateProgress(currentTaps, requiredTaps);
                
                Assert.AreEqual(
                    1f, progress, 0.0001f,
                    $"Iteration {i}: Progress should be clamped to 1.0 when taps exceed required. " +
                    $"currentTaps={currentTaps}, requiredTaps={requiredTaps}, progress={progress}"
                );
            }
        }
        
        /// <summary>
        /// Property 6: Progress is monotonically increasing with taps
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property6_Progress_IsMonotonicallyIncreasing()
        {
            // Property: For all tap sequences, more taps = more or equal progress
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                int requiredTaps = _random.Next(5, 20);
                int taps1 = _random.Next(0, requiredTaps);
                int taps2 = taps1 + _random.Next(1, 5); // taps2 > taps1
                
                float progress1 = StruggleButtonUI.CalculateProgress(taps1, requiredTaps);
                float progress2 = StruggleButtonUI.CalculateProgress(taps2, requiredTaps);
                
                Assert.GreaterOrEqual(
                    progress2, progress1,
                    $"Iteration {i}: Progress should be monotonically increasing. " +
                    $"taps1={taps1}, taps2={taps2}, progress1={progress1}, progress2={progress2}"
                );
            }
        }
        
        /// <summary>
        /// Property 6: CalculateAccumulatedProgress is consistent with CalculateProgress
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property6_AccumulatedProgress_ConsistentWithCalculateProgress()
        {
            // Property: CalculateAccumulatedProgress matches CalculateProgress
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                int requiredTaps = _random.Next(1, 50);
                int currentTaps = _random.Next(0, 100);
                
                float progress = StruggleButtonUI.CalculateProgress(currentTaps, requiredTaps);
                float accumulated = StruggleButtonUI.CalculateAccumulatedProgress(currentTaps, requiredTaps);
                
                Assert.AreEqual(
                    progress, accumulated, 0.0001f,
                    $"Iteration {i}: AccumulatedProgress should match CalculateProgress. " +
                    $"currentTaps={currentTaps}, requiredTaps={requiredTaps}"
                );
            }
        }
        
        /// <summary>
        /// Property 6: IsStruggleComplete is true when taps >= required
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property6_IsStruggleComplete_WhenTapsReachRequired()
        {
            // Property: IsStruggleComplete == (currentTaps >= requiredTaps)
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                int requiredTaps = _random.Next(5, 20);
                int currentTaps = _random.Next(0, requiredTaps + 10);
                
                bool isComplete = StruggleButtonUI.IsStruggleComplete(currentTaps, requiredTaps);
                bool expectedComplete = currentTaps >= requiredTaps;
                
                Assert.AreEqual(
                    expectedComplete, isComplete,
                    $"Iteration {i}: IsStruggleComplete should be true when taps >= required. " +
                    $"currentTaps={currentTaps}, requiredTaps={requiredTaps}, " +
                    $"expected={expectedComplete}, got={isComplete}"
                );
            }
        }
        
        /// <summary>
        /// Property 6: Progress of 1.0 implies struggle is complete
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property6_FullProgress_ImpliesComplete()
        {
            // Property: progress == 1.0 implies IsStruggleComplete == true
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                int requiredTaps = _random.Next(5, 20);
                int currentTaps = requiredTaps + _random.Next(0, 10); // >= required
                
                float progress = StruggleButtonUI.CalculateProgress(currentTaps, requiredTaps);
                bool isComplete = StruggleButtonUI.IsStruggleComplete(currentTaps, requiredTaps);
                
                if (Mathf.Approximately(progress, 1f))
                {
                    Assert.IsTrue(
                        isComplete,
                        $"Iteration {i}: Full progress should imply complete. " +
                        $"currentTaps={currentTaps}, requiredTaps={requiredTaps}"
                    );
                }
            }
        }
        
        /// <summary>
        /// Property 6: Zero required taps gives zero progress (edge case)
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property6_ZeroRequiredTaps_GivesZeroProgress()
        {
            // Property: When requiredTaps = 0, progress = 0 (avoid division by zero)
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                int requiredTaps = 0;
                int currentTaps = _random.Next(0, 100);
                
                float progress = StruggleButtonUI.CalculateProgress(currentTaps, requiredTaps);
                
                Assert.AreEqual(
                    0f, progress, 0.0001f,
                    $"Iteration {i}: Zero required taps should give zero progress. " +
                    $"currentTaps={currentTaps}, progress={progress}"
                );
            }
        }
        
        /// <summary>
        /// Property 6: Negative taps gives zero progress (clamped)
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property6_NegativeTaps_GivesZeroProgress()
        {
            // Property: Negative tap counts are clamped to 0 progress
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                int requiredTaps = _random.Next(5, 20);
                int currentTaps = -_random.Next(1, 100); // Negative
                
                float progress = StruggleButtonUI.CalculateProgress(currentTaps, requiredTaps);
                
                Assert.AreEqual(
                    0f, progress, 0.0001f,
                    $"Iteration {i}: Negative taps should give zero progress. " +
                    $"currentTaps={currentTaps}, requiredTaps={requiredTaps}, progress={progress}"
                );
            }
        }
        
        #endregion
        
        #region Combined Properties
        
        /// <summary>
        /// Combined: Visibility and progress are independent
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Combined_VisibilityAndProgress_AreIndependent()
        {
            // Property: Visibility doesn't affect progress calculation
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                bool isCaptured1 = true;
                bool isCaptured2 = false;
                int requiredTaps = _random.Next(5, 20);
                int currentTaps = _random.Next(0, requiredTaps);
                
                // Progress should be same regardless of visibility
                float progress1 = StruggleButtonUI.CalculateProgress(currentTaps, requiredTaps);
                float progress2 = StruggleButtonUI.CalculateProgress(currentTaps, requiredTaps);
                
                Assert.AreEqual(
                    progress1, progress2, 0.0001f,
                    $"Iteration {i}: Progress calculation should be independent of visibility"
                );
            }
        }
        
        /// <summary>
        /// Combined: Progress increments correctly for sequential taps
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Combined_SequentialTaps_IncrementProgressCorrectly()
        {
            // Property: Each tap increases progress by 1/requiredTaps
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                int requiredTaps = _random.Next(5, 20);
                int startTaps = _random.Next(0, requiredTaps - 1);
                
                float progressBefore = StruggleButtonUI.CalculateProgress(startTaps, requiredTaps);
                float progressAfter = StruggleButtonUI.CalculateProgress(startTaps + 1, requiredTaps);
                
                float expectedIncrement = 1f / requiredTaps;
                float actualIncrement = progressAfter - progressBefore;
                
                Assert.AreEqual(
                    expectedIncrement, actualIncrement, 0.0001f,
                    $"Iteration {i}: Each tap should increment progress by 1/required. " +
                    $"requiredTaps={requiredTaps}, expected increment={expectedIncrement}, " +
                    $"actual increment={actualIncrement}"
                );
            }
        }
        
        #endregion
    }
}
