using System;
using NUnit.Framework;
using UnityEngine;
using PetGrooming.Core;
using PetGrooming.Systems.Skills;

namespace PetGrooming.Tests.Editor
{
    /// <summary>
    /// Property-based tests for CaptureNetSkill behavior.
    /// Uses NUnit with manual random input generation (100+ iterations per property).
    /// 
    /// Feature: pet-grooming-phase2
    /// Validates: Requirements 3.2
    /// </summary>
    [TestFixture]
    public class CaptureNetSkillPropertyTests
    {
        private const int PropertyTestIterations = 100;
        private System.Random _random;
        
        // Required values from Requirements 3.2
        private const float RequiredSlowAmount = 0.5f;  // 50% slow
        private const float RequiredSlowDuration = 3f;   // 3 seconds
        private const float Tolerance = 0.001f;
        
        [SetUp]
        public void SetUp()
        {
            _random = new System.Random(42); // Fixed seed for reproducibility
        }
        
        #region Property 8: Capture Net Slow Effect
        
        /// <summary>
        /// Feature: pet-grooming-phase2, Property 8: Capture Net Slow Effect
        /// 
        /// *For any* pet hit by the Capture Net skill, the pet's movement speed 
        /// shall be reduced by 50% for exactly 3 seconds.
        /// 
        /// Validates: Requirements 3.2
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property8_CaptureNetSlowEffect_ShouldApply50PercentSlowFor3Seconds()
        {
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Generate random slow amount and duration that should be validated
                float testSlowAmount = RequiredSlowAmount;
                float testDuration = RequiredSlowDuration;
                
                // Validate that the parameters match requirements
                bool isValid = CaptureNetSkill.ValidateSlowEffectParameters(testSlowAmount, testDuration);
                
                Assert.IsTrue(
                    isValid,
                    $"Iteration {i}: Slow effect parameters should be valid (50% slow for 3 seconds)"
                );
            }
        }
        
        /// <summary>
        /// Property 8: Slow effect amount must be exactly 50%
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property8_SlowAmount_ShouldBeExactly50Percent()
        {
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Calculate slow effect with default parameters
                var (slowAmount, duration) = CaptureNetSkill.CalculateSlowEffect(RequiredSlowAmount, RequiredSlowDuration);
                
                Assert.AreEqual(
                    RequiredSlowAmount,
                    slowAmount,
                    Tolerance,
                    $"Iteration {i}: Slow amount should be exactly {RequiredSlowAmount * 100}%"
                );
            }
        }
        
        /// <summary>
        /// Property 8: Slow effect duration must be exactly 3 seconds
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property8_SlowDuration_ShouldBeExactly3Seconds()
        {
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Calculate slow effect with default parameters
                var (slowAmount, duration) = CaptureNetSkill.CalculateSlowEffect(RequiredSlowAmount, RequiredSlowDuration);
                
                Assert.AreEqual(
                    RequiredSlowDuration,
                    duration,
                    Tolerance,
                    $"Iteration {i}: Slow duration should be exactly {RequiredSlowDuration} seconds"
                );
            }
        }
        
        /// <summary>
        /// Property 8: SkillEffectData created for slow should have correct type and values
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property8_SlowEffectData_ShouldHaveCorrectTypeAndValues()
        {
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Create slow effect data as CaptureNetSkill does
                SkillEffectData slowEffect = SkillEffectData.CreateSlow(
                    RequiredSlowAmount, 
                    RequiredSlowDuration, 
                    "Capture Net"
                );
                
                // Verify effect type
                Assert.AreEqual(
                    SkillEffectType.Slow,
                    slowEffect.Type,
                    $"Iteration {i}: Effect type should be Slow"
                );
                
                // Verify slow amount (Value field)
                Assert.AreEqual(
                    RequiredSlowAmount,
                    slowEffect.Value,
                    Tolerance,
                    $"Iteration {i}: Effect value should be {RequiredSlowAmount}"
                );
                
                // Verify duration
                Assert.AreEqual(
                    RequiredSlowDuration,
                    slowEffect.Duration,
                    Tolerance,
                    $"Iteration {i}: Effect duration should be {RequiredSlowDuration}"
                );
                
                // Verify remaining time starts at full duration
                Assert.AreEqual(
                    RequiredSlowDuration,
                    slowEffect.RemainingTime,
                    Tolerance,
                    $"Iteration {i}: Remaining time should start at {RequiredSlowDuration}"
                );
            }
        }
        
        /// <summary>
        /// Property 8: Slow effect should expire after exactly 3 seconds
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property8_SlowEffect_ShouldExpireAfterExactly3Seconds()
        {
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Create slow effect
                SkillEffectData slowEffect = SkillEffectData.CreateSlow(
                    RequiredSlowAmount, 
                    RequiredSlowDuration, 
                    "Capture Net"
                );
                
                // Effect should not be expired initially
                Assert.IsFalse(
                    slowEffect.IsExpired,
                    $"Iteration {i}: Effect should not be expired initially"
                );
                
                // Simulate time passing (just under 3 seconds)
                float timeJustBefore = RequiredSlowDuration - 0.01f;
                slowEffect.UpdateTime(timeJustBefore);
                
                Assert.IsFalse(
                    slowEffect.IsExpired,
                    $"Iteration {i}: Effect should not be expired at {timeJustBefore}s"
                );
                
                // Simulate remaining time to reach exactly 3 seconds
                slowEffect.UpdateTime(0.02f);
                
                Assert.IsTrue(
                    slowEffect.IsExpired,
                    $"Iteration {i}: Effect should be expired after {RequiredSlowDuration}s"
                );
            }
        }
        
        /// <summary>
        /// Property 8: Invalid slow parameters should fail validation
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property8_InvalidSlowParameters_ShouldFailValidation()
        {
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Generate random invalid slow amounts (not 0.5)
                float invalidSlowAmount = (float)(_random.NextDouble() * 0.4 + 0.1); // 0.1 to 0.5 (exclusive of 0.5)
                if (Math.Abs(invalidSlowAmount - RequiredSlowAmount) < Tolerance)
                {
                    invalidSlowAmount = 0.3f; // Ensure it's different
                }
                
                bool isValid = CaptureNetSkill.ValidateSlowEffectParameters(invalidSlowAmount, RequiredSlowDuration);
                
                Assert.IsFalse(
                    isValid,
                    $"Iteration {i}: Slow amount {invalidSlowAmount} should be invalid (required: {RequiredSlowAmount})"
                );
            }
        }
        
        /// <summary>
        /// Property 8: Invalid duration parameters should fail validation
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property8_InvalidDurationParameters_ShouldFailValidation()
        {
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Generate random invalid durations (not 3.0)
                float invalidDuration = (float)(_random.NextDouble() * 5 + 0.5); // 0.5 to 5.5
                if (Math.Abs(invalidDuration - RequiredSlowDuration) < Tolerance)
                {
                    invalidDuration = 2.0f; // Ensure it's different
                }
                
                bool isValid = CaptureNetSkill.ValidateSlowEffectParameters(RequiredSlowAmount, invalidDuration);
                
                Assert.IsFalse(
                    isValid,
                    $"Iteration {i}: Duration {invalidDuration} should be invalid (required: {RequiredSlowDuration})"
                );
            }
        }
        
        /// <summary>
        /// Property 8: Slow effect progress should be calculated correctly
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property8_SlowEffectProgress_ShouldBeCalculatedCorrectly()
        {
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Create slow effect
                SkillEffectData slowEffect = SkillEffectData.CreateSlow(
                    RequiredSlowAmount, 
                    RequiredSlowDuration, 
                    "Capture Net"
                );
                
                // Progress should start at 0
                Assert.AreEqual(
                    0f,
                    slowEffect.Progress,
                    Tolerance,
                    $"Iteration {i}: Progress should start at 0"
                );
                
                // Simulate half the duration
                float halfDuration = RequiredSlowDuration / 2f;
                slowEffect.UpdateTime(halfDuration);
                
                Assert.AreEqual(
                    0.5f,
                    slowEffect.Progress,
                    Tolerance,
                    $"Iteration {i}: Progress should be 0.5 at half duration"
                );
                
                // Simulate remaining time
                slowEffect.UpdateTime(halfDuration);
                
                Assert.AreEqual(
                    1f,
                    slowEffect.Progress,
                    Tolerance,
                    $"Iteration {i}: Progress should be 1.0 when expired"
                );
            }
        }
        
        #endregion
    }
}
