using System;
using NUnit.Framework;
using UnityEngine;
using PetGrooming.UI.MobileUI;

namespace PetGrooming.Tests.Editor
{
    /// <summary>
    /// Property-based tests for MobileSkillButton.
    /// Uses NUnit with manual random input generation (100+ iterations per property).
    /// 
    /// Feature: mobile-ui-controls
    /// Validates: Requirements 2.6, 2.7, 4.1
    /// </summary>
    [TestFixture]
    public class MobileSkillButtonPropertyTests
    {
        private const int PropertyTestIterations = 100;
        private System.Random _random;
        
        [SetUp]
        public void SetUp()
        {
            _random = new System.Random(42); // Fixed seed for reproducibility
        }
        
        #region Property 4: Cooldown Display State Consistency
        
        /// <summary>
        /// Feature: mobile-ui-controls, Property 4: Cooldown Display State Consistency
        /// 
        /// *For any* skill button bound to a skill, when the skill's remaining cooldown is 
        /// greater than 0, the cooldown overlay SHALL be visible AND the cooldown text 
        /// SHALL display the remaining time (rounded up to nearest second).
        /// 
        /// Validates: Requirements 2.6, 2.7
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property4_WhenCooldownGreaterThanZero_OverlayIsVisible()
        {
            // Property: For all remaining cooldown > 0, overlay is visible
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Generate random cooldown values where remaining > 0
                float totalCooldown = (float)(_random.NextDouble() * 20 + 1); // 1 to 21 seconds
                float remainingCooldown = (float)(_random.NextDouble() * totalCooldown) + 0.001f; // > 0
                
                var state = MobileSkillButton.CalculateCooldownDisplayState(remainingCooldown, totalCooldown);
                
                Assert.IsTrue(
                    state.overlayVisible,
                    $"Failed for remaining={remainingCooldown}, total={totalCooldown}. " +
                    $"Overlay should be visible when cooldown > 0"
                );
            }
        }
        
        /// <summary>
        /// Property 4: Cooldown text is visible when cooldown > 0
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property4_WhenCooldownGreaterThanZero_TextIsVisible()
        {
            // Property: For all remaining cooldown > 0, text is visible
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float totalCooldown = (float)(_random.NextDouble() * 20 + 1);
                float remainingCooldown = (float)(_random.NextDouble() * totalCooldown) + 0.001f;
                
                var state = MobileSkillButton.CalculateCooldownDisplayState(remainingCooldown, totalCooldown);
                
                Assert.IsTrue(
                    state.textVisible,
                    $"Failed for remaining={remainingCooldown}, total={totalCooldown}. " +
                    $"Text should be visible when cooldown > 0"
                );
            }
        }
        
        /// <summary>
        /// Property 4: Cooldown text shows rounded up seconds
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property4_CooldownText_ShowsRoundedUpSeconds()
        {
            // Property: For all remaining cooldown > 0, text shows ceil(remaining)
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float totalCooldown = (float)(_random.NextDouble() * 20 + 1);
                float remainingCooldown = (float)(_random.NextDouble() * totalCooldown) + 0.001f;
                
                var state = MobileSkillButton.CalculateCooldownDisplayState(remainingCooldown, totalCooldown);
                
                int expectedValue = Mathf.CeilToInt(remainingCooldown);
                string expectedText = expectedValue.ToString();
                
                Assert.AreEqual(
                    expectedText, state.displayText,
                    $"Failed for remaining={remainingCooldown}. " +
                    $"Expected text='{expectedText}', got='{state.displayText}'"
                );
            }
        }
        
        /// <summary>
        /// Property 4: When cooldown is zero, overlay is hidden
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property4_WhenCooldownZero_OverlayIsHidden()
        {
            // Property: For remaining cooldown = 0, overlay is hidden
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float totalCooldown = (float)(_random.NextDouble() * 20 + 1);
                float remainingCooldown = 0f;
                
                var state = MobileSkillButton.CalculateCooldownDisplayState(remainingCooldown, totalCooldown);
                
                Assert.IsFalse(
                    state.overlayVisible,
                    $"Failed for remaining={remainingCooldown}, total={totalCooldown}. " +
                    $"Overlay should be hidden when cooldown = 0"
                );
            }
        }
        
        /// <summary>
        /// Property 4: When cooldown is zero, text is hidden
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property4_WhenCooldownZero_TextIsHidden()
        {
            // Property: For remaining cooldown = 0, text is hidden
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float totalCooldown = (float)(_random.NextDouble() * 20 + 1);
                float remainingCooldown = 0f;
                
                var state = MobileSkillButton.CalculateCooldownDisplayState(remainingCooldown, totalCooldown);
                
                Assert.IsFalse(
                    state.textVisible,
                    $"Failed for remaining={remainingCooldown}, total={totalCooldown}. " +
                    $"Text should be hidden when cooldown = 0"
                );
            }
        }
        
        /// <summary>
        /// Property 4: Fill amount is proportional to cooldown progress
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property4_FillAmount_IsProportionalToCooldownProgress()
        {
            // Property: fillAmount = remaining / total (clamped to [0, 1])
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float totalCooldown = (float)(_random.NextDouble() * 20 + 1);
                float remainingCooldown = (float)(_random.NextDouble() * totalCooldown * 1.5f); // Can exceed total
                
                var state = MobileSkillButton.CalculateCooldownDisplayState(remainingCooldown, totalCooldown);
                
                float expectedFill = Mathf.Clamp01(remainingCooldown / totalCooldown);
                
                Assert.AreEqual(
                    expectedFill, state.fillAmount, 0.0001f,
                    $"Failed for remaining={remainingCooldown}, total={totalCooldown}. " +
                    $"Expected fill={expectedFill}, got={state.fillAmount}"
                );
            }
        }
        
        /// <summary>
        /// Property 4: Fill amount is always in [0, 1] range
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property4_FillAmount_AlwaysInValidRange()
        {
            // Property: For all inputs, fillAmount is in [0, 1]
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float totalCooldown = (float)(_random.NextDouble() * 100 - 50); // Can be negative
                float remainingCooldown = (float)(_random.NextDouble() * 100 - 50);
                
                var state = MobileSkillButton.CalculateCooldownDisplayState(remainingCooldown, totalCooldown);
                
                Assert.GreaterOrEqual(
                    state.fillAmount, 0f,
                    $"Failed for remaining={remainingCooldown}, total={totalCooldown}. " +
                    $"Fill amount should be >= 0"
                );
                Assert.LessOrEqual(
                    state.fillAmount, 1f,
                    $"Failed for remaining={remainingCooldown}, total={totalCooldown}. " +
                    $"Fill amount should be <= 1"
                );
            }
        }
        
        /// <summary>
        /// Property 4: ShouldShowCooldownOverlay is consistent with CalculateCooldownDisplayState
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property4_ShouldShowCooldownOverlay_ConsistentWithDisplayState()
        {
            // Property: ShouldShowCooldownOverlay matches overlayVisible from CalculateCooldownDisplayState
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float remainingCooldown = (float)(_random.NextDouble() * 20 - 5); // -5 to 15
                float totalCooldown = (float)(_random.NextDouble() * 20 + 1);
                
                bool shouldShow = MobileSkillButton.ShouldShowCooldownOverlay(remainingCooldown);
                var state = MobileSkillButton.CalculateCooldownDisplayState(remainingCooldown, totalCooldown);
                
                Assert.AreEqual(
                    shouldShow, state.overlayVisible,
                    $"Failed for remaining={remainingCooldown}. " +
                    $"ShouldShowCooldownOverlay={shouldShow}, overlayVisible={state.overlayVisible}"
                );
            }
        }
        
        /// <summary>
        /// Property 4: CalculateCooldownText is consistent with CalculateCooldownDisplayState
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property4_CalculateCooldownText_ConsistentWithDisplayState()
        {
            // Property: CalculateCooldownText matches displayText from CalculateCooldownDisplayState
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float remainingCooldown = (float)(_random.NextDouble() * 20);
                float totalCooldown = (float)(_random.NextDouble() * 20 + 1);
                
                string text = MobileSkillButton.CalculateCooldownText(remainingCooldown);
                var state = MobileSkillButton.CalculateCooldownDisplayState(remainingCooldown, totalCooldown);
                
                Assert.AreEqual(
                    text, state.displayText,
                    $"Failed for remaining={remainingCooldown}. " +
                    $"CalculateCooldownText='{text}', displayText='{state.displayText}'"
                );
            }
        }
        
        #endregion
        
        #region Property 9: Press Scale Feedback
        
        /// <summary>
        /// Feature: mobile-ui-controls, Property 9: Press Scale Feedback
        /// 
        /// *For any* skill button, when a pointer down event occurs, the button's scale 
        /// SHALL be reduced to 95% of its original size.
        /// 
        /// Validates: Requirements 4.1
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property9_PressScale_Is95PercentOfOriginal()
        {
            // Property: For all original scales, press scale = original * 0.95
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Generate random original scale
                float scaleValue = (float)(_random.NextDouble() * 2 + 0.5f); // 0.5 to 2.5
                Vector3 originalScale = new Vector3(scaleValue, scaleValue, scaleValue);
                
                Vector3 pressScale = MobileSkillButton.CalculatePressScale(originalScale);
                
                Vector3 expectedScale = originalScale * 0.95f;
                
                Assert.AreEqual(
                    expectedScale.x, pressScale.x, 0.0001f,
                    $"Failed for originalScale={originalScale}. " +
                    $"Expected pressScale.x={expectedScale.x}, got={pressScale.x}"
                );
                Assert.AreEqual(
                    expectedScale.y, pressScale.y, 0.0001f,
                    $"Failed for originalScale={originalScale}. " +
                    $"Expected pressScale.y={expectedScale.y}, got={pressScale.y}"
                );
                Assert.AreEqual(
                    expectedScale.z, pressScale.z, 0.0001f,
                    $"Failed for originalScale={originalScale}. " +
                    $"Expected pressScale.z={expectedScale.z}, got={pressScale.z}"
                );
            }
        }
        
        /// <summary>
        /// Property 9: Press scale with custom multiplier
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property9_PressScale_WithCustomMultiplier()
        {
            // Property: For all original scales and multipliers, press scale = original * multiplier
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float scaleValue = (float)(_random.NextDouble() * 2 + 0.5f);
                Vector3 originalScale = new Vector3(scaleValue, scaleValue, scaleValue);
                float multiplier = (float)(_random.NextDouble() * 0.3 + 0.7f); // 0.7 to 1.0
                
                Vector3 pressScale = MobileSkillButton.CalculatePressScale(originalScale, multiplier);
                
                Vector3 expectedScale = originalScale * multiplier;
                
                Assert.AreEqual(
                    expectedScale.x, pressScale.x, 0.0001f,
                    $"Failed for originalScale={originalScale}, multiplier={multiplier}. " +
                    $"Expected pressScale.x={expectedScale.x}, got={pressScale.x}"
                );
            }
        }
        
        /// <summary>
        /// Property 9: Press scale is always smaller than original (with default multiplier)
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property9_PressScale_IsSmallerThanOriginal()
        {
            // Property: For all positive original scales, press scale < original
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float scaleValue = (float)(_random.NextDouble() * 10 + 0.1f); // 0.1 to 10.1
                Vector3 originalScale = new Vector3(scaleValue, scaleValue, scaleValue);
                
                Vector3 pressScale = MobileSkillButton.CalculatePressScale(originalScale);
                
                Assert.Less(
                    pressScale.magnitude, originalScale.magnitude,
                    $"Failed for originalScale={originalScale}. " +
                    $"Press scale magnitude={pressScale.magnitude} should be less than original={originalScale.magnitude}"
                );
            }
        }
        
        /// <summary>
        /// Property 9: Press scale preserves aspect ratio
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property9_PressScale_PreservesAspectRatio()
        {
            // Property: For all original scales, press scale maintains same proportions
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Generate non-uniform scale
                Vector3 originalScale = new Vector3(
                    (float)(_random.NextDouble() * 2 + 0.5f),
                    (float)(_random.NextDouble() * 2 + 0.5f),
                    (float)(_random.NextDouble() * 2 + 0.5f)
                );
                
                Vector3 pressScale = MobileSkillButton.CalculatePressScale(originalScale);
                
                // Check that ratios are preserved
                if (originalScale.x > 0.001f && originalScale.y > 0.001f)
                {
                    float originalRatioXY = originalScale.x / originalScale.y;
                    float pressRatioXY = pressScale.x / pressScale.y;
                    
                    Assert.AreEqual(
                        originalRatioXY, pressRatioXY, 0.0001f,
                        $"Failed for originalScale={originalScale}. " +
                        $"X/Y ratio should be preserved"
                    );
                }
                
                if (originalScale.y > 0.001f && originalScale.z > 0.001f)
                {
                    float originalRatioYZ = originalScale.y / originalScale.z;
                    float pressRatioYZ = pressScale.y / pressScale.z;
                    
                    Assert.AreEqual(
                        originalRatioYZ, pressRatioYZ, 0.0001f,
                        $"Failed for originalScale={originalScale}. " +
                        $"Y/Z ratio should be preserved"
                    );
                }
            }
        }
        
        /// <summary>
        /// Property 9: Zero scale produces zero press scale
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property9_ZeroScale_ProducesZeroPressScale()
        {
            // Property: Zero original scale produces zero press scale
            Vector3 originalScale = Vector3.zero;
            Vector3 pressScale = MobileSkillButton.CalculatePressScale(originalScale);
            
            Assert.AreEqual(0f, pressScale.x, 0.0001f, "Press scale X should be 0 for zero original");
            Assert.AreEqual(0f, pressScale.y, 0.0001f, "Press scale Y should be 0 for zero original");
            Assert.AreEqual(0f, pressScale.z, 0.0001f, "Press scale Z should be 0 for zero original");
        }
        
        /// <summary>
        /// Property 9: Press scale is idempotent with inverse operation
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property9_PressScale_CanBeReversed()
        {
            // Property: Dividing press scale by multiplier returns original scale
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float scaleValue = (float)(_random.NextDouble() * 2 + 0.5f);
                Vector3 originalScale = new Vector3(scaleValue, scaleValue, scaleValue);
                float multiplier = 0.95f;
                
                Vector3 pressScale = MobileSkillButton.CalculatePressScale(originalScale, multiplier);
                Vector3 recoveredScale = pressScale / multiplier;
                
                Assert.AreEqual(
                    originalScale.x, recoveredScale.x, 0.0001f,
                    $"Failed for originalScale={originalScale}. " +
                    $"Recovered scale should match original"
                );
            }
        }
        
        #endregion
        
        #region Combined Properties
        
        /// <summary>
        /// Combined: Cooldown state and press scale are independent
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Combined_CooldownAndPressScale_AreIndependent()
        {
            // Property: Cooldown state doesn't affect press scale calculation
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float scaleValue = (float)(_random.NextDouble() * 2 + 0.5f);
                Vector3 originalScale = new Vector3(scaleValue, scaleValue, scaleValue);
                
                float remainingCooldown1 = 0f;
                float remainingCooldown2 = (float)(_random.NextDouble() * 10 + 1);
                float totalCooldown = 10f;
                
                // Calculate press scale (should be same regardless of cooldown)
                Vector3 pressScale1 = MobileSkillButton.CalculatePressScale(originalScale);
                Vector3 pressScale2 = MobileSkillButton.CalculatePressScale(originalScale);
                
                Assert.AreEqual(
                    pressScale1, pressScale2,
                    $"Press scale should be independent of cooldown state"
                );
            }
        }
        
        #endregion
    }
}
