using System;
using NUnit.Framework;
using UnityEngine;
using PetGrooming.UI.MobileUI;

namespace PetGrooming.Tests.Editor
{
    /// <summary>
    /// Property-based tests for ScreenAdapter.
    /// Uses NUnit with manual random input generation (100+ iterations per property).
    /// 
    /// Feature: mobile-ui-controls
    /// Property 8: Screen Size Adaptation
    /// Validates: Requirements 3.6
    /// </summary>
    [TestFixture]
    public class ScreenAdapterPropertyTests
    {
        private const int PropertyTestIterations = 100;
        private System.Random _random;
        
        [SetUp]
        public void SetUp()
        {
            _random = new System.Random(42); // Fixed seed for reproducibility
        }
        
        #region Property 8: Screen Size Adaptation
        
        /// <summary>
        /// Feature: mobile-ui-controls, Property 8: Screen Size Adaptation
        /// 
        /// *For any* screen size and aspect ratio, the skill wheel and joystick SHALL remain 
        /// fully visible within the screen bounds and maintain their relative positions 
        /// (joystick in bottom-left quadrant, skill wheel in bottom-right quadrant).
        /// 
        /// Validates: Requirements 3.6
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property8_ScaleFactor_IsWithinValidRange()
        {
            // Property: For all screen sizes, scale factor is within [MinScaleFactor, MaxScaleFactor]
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Generate random screen dimensions (from small mobile to large desktop)
                float width = (float)(_random.NextDouble() * 3840 + 320); // 320 to 4160
                float height = (float)(_random.NextDouble() * 2160 + 240); // 240 to 2400
                
                float scaleFactor = ScreenAdapter.CalculateScaleFactor(width, height);
                
                Assert.GreaterOrEqual(
                    scaleFactor, ScreenAdapter.MinScaleFactor,
                    $"Failed for screen {width}x{height}. Scale factor {scaleFactor} should be >= {ScreenAdapter.MinScaleFactor}"
                );
                Assert.LessOrEqual(
                    scaleFactor, ScreenAdapter.MaxScaleFactor,
                    $"Failed for screen {width}x{height}. Scale factor {scaleFactor} should be <= {ScreenAdapter.MaxScaleFactor}"
                );
            }
        }
        
        /// <summary>
        /// Property 8: Scale factor is 1.0 for reference screen size
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property8_ScaleFactor_IsOneForReferenceSize()
        {
            float scaleFactor = ScreenAdapter.CalculateScaleFactor(
                ScreenAdapter.ReferenceWidth, 
                ScreenAdapter.ReferenceHeight
            );
            
            Assert.AreEqual(
                1f, scaleFactor, 0.001f,
                "Scale factor should be 1.0 for reference screen size"
            );
        }
        
        /// <summary>
        /// Property 8: Scale factor increases with larger screens (up to max)
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property8_ScaleFactor_IncreasesWithLargerScreens()
        {
            // Property: For screens larger than reference, scale factor >= 1 (up to max)
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Generate screen larger than reference
                float multiplier = (float)(_random.NextDouble() * 2 + 1); // 1x to 3x
                float width = ScreenAdapter.ReferenceWidth * multiplier;
                float height = ScreenAdapter.ReferenceHeight * multiplier;
                
                float scaleFactor = ScreenAdapter.CalculateScaleFactor(width, height);
                
                Assert.GreaterOrEqual(
                    scaleFactor, 1f,
                    $"Failed for screen {width}x{height}. Scale factor {scaleFactor} should be >= 1 for larger screens"
                );
            }
        }
        
        /// <summary>
        /// Property 8: Scale factor decreases with smaller screens (down to min)
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property8_ScaleFactor_DecreasesWithSmallerScreens()
        {
            // Property: For screens smaller than reference, scale factor <= 1 (down to min)
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Generate screen smaller than reference
                float multiplier = (float)(_random.NextDouble() * 0.5 + 0.3); // 0.3x to 0.8x
                float width = ScreenAdapter.ReferenceWidth * multiplier;
                float height = ScreenAdapter.ReferenceHeight * multiplier;
                
                float scaleFactor = ScreenAdapter.CalculateScaleFactor(width, height);
                
                Assert.LessOrEqual(
                    scaleFactor, 1f,
                    $"Failed for screen {width}x{height}. Scale factor {scaleFactor} should be <= 1 for smaller screens"
                );
            }
        }
        
        /// <summary>
        /// Property 8: Joystick position is always in bottom-left quadrant
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property8_JoystickPosition_IsInBottomLeftQuadrant()
        {
            // Property: For all screen sizes, joystick center is in bottom-left quadrant
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float width = (float)(_random.NextDouble() * 3840 + 320);
                float height = (float)(_random.NextDouble() * 2160 + 240);
                Vector2 screenSize = new Vector2(width, height);
                
                float scaleFactor = ScreenAdapter.CalculateScaleFactor(width, height);
                Rect safeArea = new Rect(0, 0, width, height);
                
                // Use typical joystick settings
                Vector2 baseOffset = new Vector2(150f, 150f);
                float joystickSize = 180f;
                
                Vector2 position = ScreenAdapter.CalculateJoystickPosition(
                    baseOffset, joystickSize, scaleFactor, safeArea, screenSize
                );
                
                bool isInBottomLeft = ScreenAdapter.IsInBottomLeftQuadrant(position, screenSize);
                
                Assert.IsTrue(
                    isInBottomLeft,
                    $"Failed for screen {width}x{height}. Joystick at {position} should be in bottom-left quadrant"
                );
            }
        }
        
        /// <summary>
        /// Property 8: Skill wheel position is always in bottom-right quadrant
        /// (when screen is wide enough to accommodate it)
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property8_SkillWheelPosition_IsInBottomRightQuadrant()
        {
            // Property: For all reasonably-sized screens, skill wheel center is in bottom-right quadrant
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Use minimum width that can accommodate both UI elements
                float width = (float)(_random.NextDouble() * 3840 + 800); // 800 to 4640 (reasonable minimum)
                float height = (float)(_random.NextDouble() * 2160 + 480); // 480 to 2640
                Vector2 screenSize = new Vector2(width, height);
                
                float scaleFactor = ScreenAdapter.CalculateScaleFactor(width, height);
                Rect safeArea = new Rect(0, 0, width, height);
                
                // Use typical skill wheel settings
                Vector2 baseOffset = new Vector2(-100f, 100f);
                float wheelRadius = 250f; // arcRadius + skillButtonSize
                
                Vector2 position = ScreenAdapter.CalculateSkillWheelPosition(
                    baseOffset, wheelRadius, scaleFactor, safeArea, screenSize
                );
                
                bool isInBottomRight = ScreenAdapter.IsInBottomRightQuadrant(position, screenSize);
                
                Assert.IsTrue(
                    isInBottomRight,
                    $"Failed for screen {width}x{height}. Skill wheel at {position} should be in bottom-right quadrant"
                );
            }
        }
        
        /// <summary>
        /// Property 8: Joystick is always within screen bounds
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property8_JoystickPosition_IsWithinScreenBounds()
        {
            // Property: For all screen sizes, joystick is fully within screen bounds
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float width = (float)(_random.NextDouble() * 3840 + 320);
                float height = (float)(_random.NextDouble() * 2160 + 240);
                Vector2 screenSize = new Vector2(width, height);
                
                float scaleFactor = ScreenAdapter.CalculateScaleFactor(width, height);
                Rect safeArea = new Rect(0, 0, width, height);
                
                Vector2 baseOffset = new Vector2(150f, 150f);
                float joystickSize = 180f;
                
                Vector2 position = ScreenAdapter.CalculateJoystickPosition(
                    baseOffset, joystickSize, scaleFactor, safeArea, screenSize
                );
                
                float scaledSize = joystickSize * scaleFactor;
                Vector2 size = new Vector2(scaledSize, scaledSize);
                
                bool isWithinBounds = ScreenAdapter.IsWithinScreenBounds(position, size, screenSize);
                
                Assert.IsTrue(
                    isWithinBounds,
                    $"Failed for screen {width}x{height}. Joystick at {position} with size {size} should be within bounds"
                );
            }
        }
        
        /// <summary>
        /// Property 8: Skill wheel is always within screen bounds
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property8_SkillWheelPosition_IsWithinScreenBounds()
        {
            // Property: For all screen sizes, skill wheel is fully within screen bounds
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Use reasonable minimum screen size that can fit the UI
                float width = (float)(_random.NextDouble() * 3840 + 600); // 600 to 4440
                float height = (float)(_random.NextDouble() * 2160 + 400); // 400 to 2560
                Vector2 screenSize = new Vector2(width, height);
                
                float scaleFactor = ScreenAdapter.CalculateScaleFactor(width, height);
                Rect safeArea = new Rect(0, 0, width, height);
                
                Vector2 baseOffset = new Vector2(-100f, 100f);
                float wheelRadius = 250f;
                
                Vector2 position = ScreenAdapter.CalculateSkillWheelPosition(
                    baseOffset, wheelRadius, scaleFactor, safeArea, screenSize
                );
                
                float scaledRadius = wheelRadius * scaleFactor;
                Vector2 size = new Vector2(scaledRadius * 2, scaledRadius * 2);
                
                bool isWithinBounds = ScreenAdapter.IsWithinScreenBounds(position, size, screenSize);
                
                Assert.IsTrue(
                    isWithinBounds,
                    $"Failed for screen {width}x{height}. Skill wheel at {position} with size {size} should be within bounds"
                );
            }
        }
        
        /// <summary>
        /// Property 8: Joystick and skill wheel do not overlap
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property8_JoystickAndSkillWheel_DoNotOverlap()
        {
            // Property: For all screen sizes, joystick and skill wheel don't overlap
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Use reasonable minimum screen size to ensure both can fit
                float width = (float)(_random.NextDouble() * 3840 + 640); // 640 to 4480
                float height = (float)(_random.NextDouble() * 2160 + 480); // 480 to 2640
                Vector2 screenSize = new Vector2(width, height);
                
                float scaleFactor = ScreenAdapter.CalculateScaleFactor(width, height);
                Rect safeArea = new Rect(0, 0, width, height);
                
                // Joystick
                Vector2 joystickOffset = new Vector2(150f, 150f);
                float joystickSize = 180f;
                Vector2 joystickPos = ScreenAdapter.CalculateJoystickPosition(
                    joystickOffset, joystickSize, scaleFactor, safeArea, screenSize
                );
                float scaledJoystickRadius = (joystickSize * scaleFactor) / 2f;
                Rect joystickBounds = ScreenAdapter.CalculateElementBounds(joystickPos, scaledJoystickRadius);
                
                // Skill wheel
                Vector2 wheelOffset = new Vector2(-100f, 100f);
                float wheelRadius = 250f;
                Vector2 wheelPos = ScreenAdapter.CalculateSkillWheelPosition(
                    wheelOffset, wheelRadius, scaleFactor, safeArea, screenSize
                );
                float scaledWheelRadius = wheelRadius * scaleFactor;
                Rect wheelBounds = ScreenAdapter.CalculateElementBounds(wheelPos, scaledWheelRadius);
                
                bool doNotOverlap = ScreenAdapter.ElementsDoNotOverlap(joystickBounds, wheelBounds);
                
                Assert.IsTrue(
                    doNotOverlap,
                    $"Failed for screen {width}x{height}. Joystick bounds {joystickBounds} and wheel bounds {wheelBounds} should not overlap"
                );
            }
        }
        
        /// <summary>
        /// Property 8: Safe area is respected for joystick positioning
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property8_JoystickPosition_RespectsSafeArea()
        {
            // Property: Joystick position respects safe area margins
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float width = (float)(_random.NextDouble() * 3840 + 320);
                float height = (float)(_random.NextDouble() * 2160 + 240);
                Vector2 screenSize = new Vector2(width, height);
                
                // Simulate a safe area with notch/cutout
                float safeMargin = (float)(_random.NextDouble() * 50 + 10); // 10 to 60 pixels
                Rect safeArea = new Rect(safeMargin, safeMargin, width - safeMargin * 2, height - safeMargin * 2);
                
                float scaleFactor = ScreenAdapter.CalculateScaleFactor(width, height);
                
                Vector2 baseOffset = new Vector2(150f, 150f);
                float joystickSize = 180f;
                
                Vector2 position = ScreenAdapter.CalculateJoystickPosition(
                    baseOffset, joystickSize, scaleFactor, safeArea, screenSize
                );
                
                float scaledRadius = (joystickSize * scaleFactor) / 2f;
                
                // Check that joystick edge is within safe area (with margin)
                float leftEdge = position.x - scaledRadius;
                float bottomEdge = position.y - scaledRadius;
                
                Assert.GreaterOrEqual(
                    leftEdge, safeArea.xMin,
                    $"Failed for screen {width}x{height} with safe area {safeArea}. Joystick left edge {leftEdge} should be >= safe area left {safeArea.xMin}"
                );
                Assert.GreaterOrEqual(
                    bottomEdge, safeArea.yMin,
                    $"Failed for screen {width}x{height} with safe area {safeArea}. Joystick bottom edge {bottomEdge} should be >= safe area bottom {safeArea.yMin}"
                );
            }
        }
        
        /// <summary>
        /// Property 8: Scale factor handles edge cases gracefully
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property8_ScaleFactor_HandlesEdgeCases()
        {
            // Zero dimensions should return 1.0
            Assert.AreEqual(1f, ScreenAdapter.CalculateScaleFactor(0, 0), "Zero dimensions should return 1.0");
            Assert.AreEqual(1f, ScreenAdapter.CalculateScaleFactor(0, 1080), "Zero width should return 1.0");
            Assert.AreEqual(1f, ScreenAdapter.CalculateScaleFactor(1920, 0), "Zero height should return 1.0");
            
            // Negative dimensions should return 1.0
            Assert.AreEqual(1f, ScreenAdapter.CalculateScaleFactor(-1920, 1080), "Negative width should return 1.0");
            Assert.AreEqual(1f, ScreenAdapter.CalculateScaleFactor(1920, -1080), "Negative height should return 1.0");
        }
        
        /// <summary>
        /// Property 8: Extreme aspect ratios are handled correctly
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property8_ExtremeAspectRatios_AreHandled()
        {
            // Property: Even extreme aspect ratios produce valid scale factors
            float[] extremeWidths = { 3840, 1920, 800, 2560 };
            float[] extremeHeights = { 600, 1920, 1200, 1080 };
            
            for (int i = 0; i < extremeWidths.Length; i++)
            {
                float width = extremeWidths[i];
                float height = extremeHeights[i];
                
                float scaleFactor = ScreenAdapter.CalculateScaleFactor(width, height);
                
                Assert.GreaterOrEqual(scaleFactor, ScreenAdapter.MinScaleFactor);
                Assert.LessOrEqual(scaleFactor, ScreenAdapter.MaxScaleFactor);
                
                // Verify UI elements can still be positioned
                Vector2 screenSize = new Vector2(width, height);
                Rect safeArea = new Rect(0, 0, width, height);
                
                Vector2 joystickPos = ScreenAdapter.CalculateJoystickPosition(
                    new Vector2(150f, 150f), 180f, scaleFactor, safeArea, screenSize
                );
                
                Vector2 wheelPos = ScreenAdapter.CalculateSkillWheelPosition(
                    new Vector2(-100f, 100f), 250f, scaleFactor, safeArea, screenSize
                );
                
                Assert.IsTrue(
                    ScreenAdapter.IsInBottomLeftQuadrant(joystickPos, screenSize),
                    $"Joystick should be in bottom-left for {width}x{height}"
                );
                Assert.IsTrue(
                    ScreenAdapter.IsInBottomRightQuadrant(wheelPos, screenSize),
                    $"Skill wheel should be in bottom-right for {width}x{height}"
                );
            }
        }
        
        #endregion
        
        #region Helper Method Tests
        
        /// <summary>
        /// Test IsWithinScreenBounds helper method
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Helper_IsWithinScreenBounds_WorksCorrectly()
        {
            Vector2 screenSize = new Vector2(1920, 1080);
            
            // Center of screen - should be within bounds
            Assert.IsTrue(ScreenAdapter.IsWithinScreenBounds(new Vector2(960, 540), new Vector2(100, 100), screenSize));
            
            // Edge cases
            Assert.IsTrue(ScreenAdapter.IsWithinScreenBounds(new Vector2(50, 50), new Vector2(100, 100), screenSize));
            Assert.IsTrue(ScreenAdapter.IsWithinScreenBounds(new Vector2(1870, 1030), new Vector2(100, 100), screenSize));
            
            // Outside bounds
            Assert.IsFalse(ScreenAdapter.IsWithinScreenBounds(new Vector2(0, 540), new Vector2(100, 100), screenSize));
            Assert.IsFalse(ScreenAdapter.IsWithinScreenBounds(new Vector2(1920, 540), new Vector2(100, 100), screenSize));
        }
        
        /// <summary>
        /// Test quadrant detection helper methods
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Helper_QuadrantDetection_WorksCorrectly()
        {
            Vector2 screenSize = new Vector2(1920, 1080);
            
            // Bottom-left quadrant
            Assert.IsTrue(ScreenAdapter.IsInBottomLeftQuadrant(new Vector2(480, 270), screenSize));
            Assert.IsFalse(ScreenAdapter.IsInBottomLeftQuadrant(new Vector2(1440, 270), screenSize));
            Assert.IsFalse(ScreenAdapter.IsInBottomLeftQuadrant(new Vector2(480, 810), screenSize));
            
            // Bottom-right quadrant
            Assert.IsTrue(ScreenAdapter.IsInBottomRightQuadrant(new Vector2(1440, 270), screenSize));
            Assert.IsFalse(ScreenAdapter.IsInBottomRightQuadrant(new Vector2(480, 270), screenSize));
            Assert.IsFalse(ScreenAdapter.IsInBottomRightQuadrant(new Vector2(1440, 810), screenSize));
        }
        
        /// <summary>
        /// Test element bounds calculation
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Helper_CalculateElementBounds_WorksCorrectly()
        {
            Vector2 center = new Vector2(100, 100);
            float radius = 50f;
            
            Rect bounds = ScreenAdapter.CalculateElementBounds(center, radius);
            
            Assert.AreEqual(50f, bounds.x, 0.001f);
            Assert.AreEqual(50f, bounds.y, 0.001f);
            Assert.AreEqual(100f, bounds.width, 0.001f);
            Assert.AreEqual(100f, bounds.height, 0.001f);
        }
        
        /// <summary>
        /// Test overlap detection
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Helper_ElementsDoNotOverlap_WorksCorrectly()
        {
            // Non-overlapping rects
            Rect rect1 = new Rect(0, 0, 100, 100);
            Rect rect2 = new Rect(200, 0, 100, 100);
            Assert.IsTrue(ScreenAdapter.ElementsDoNotOverlap(rect1, rect2));
            
            // Overlapping rects
            Rect rect3 = new Rect(0, 0, 100, 100);
            Rect rect4 = new Rect(50, 50, 100, 100);
            Assert.IsFalse(ScreenAdapter.ElementsDoNotOverlap(rect3, rect4));
            
            // Adjacent rects (touching but not overlapping)
            Rect rect5 = new Rect(0, 0, 100, 100);
            Rect rect6 = new Rect(100, 0, 100, 100);
            Assert.IsTrue(ScreenAdapter.ElementsDoNotOverlap(rect5, rect6));
        }
        
        #endregion
    }
}
