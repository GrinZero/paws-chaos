using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using PetGrooming.UI.MobileUI;

namespace PetGrooming.Tests.Editor
{
    /// <summary>
    /// Property-based tests for MultiTouchHandler.
    /// Uses NUnit with manual random input generation (100+ iterations per property).
    /// 
    /// Feature: mobile-ui-controls
    /// Property 12: Multi-Touch Input Handling
    /// Validates: Requirements 6.5
    /// </summary>
    [TestFixture]
    public class MultiTouchHandlerPropertyTests
    {
        private const int PropertyTestIterations = 100;
        private System.Random _random;
        
        [SetUp]
        public void SetUp()
        {
            _random = new System.Random(42); // Fixed seed for reproducibility
        }
        
        #region Property 12: Multi-Touch Input Handling
        
        /// <summary>
        /// Feature: mobile-ui-controls, Property 12: Multi-Touch Input Handling
        /// 
        /// *For any* simultaneous touch inputs on both the joystick and a skill button, 
        /// both inputs SHALL be processed correctly (joystick outputs direction AND 
        /// skill button triggers activation).
        /// 
        /// Validates: Requirements 6.5
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property12_SimultaneousInputs_AreProcessedCorrectly()
        {
            // Property: For all combinations of joystick and skill button touches,
            // both should be processable simultaneously
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Generate random touch IDs (simulating different fingers)
                int joystickTouchId = _random.Next(0, 10);
                int skillButtonTouchId = _random.Next(0, 10);
                
                // Ensure different IDs for simultaneous touches
                while (skillButtonTouchId == joystickTouchId)
                {
                    skillButtonTouchId = _random.Next(0, 10);
                }
                
                bool canProcessBoth = MultiTouchHandler.ValidateSimultaneousInputs(
                    joystickTouchId, 
                    skillButtonTouchId
                );
                
                Assert.IsTrue(
                    canProcessBoth,
                    $"Failed for joystickTouchId={joystickTouchId}, skillButtonTouchId={skillButtonTouchId}. " +
                    "Both inputs should be processable simultaneously"
                );
            }
        }
        
        /// <summary>
        /// Property 12: Same touch ID cannot be on both joystick and skill button
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property12_SameTouchId_CannotBeOnBothElements()
        {
            // Property: A single touch cannot be on both elements simultaneously
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                int touchId = _random.Next(0, 10);
                
                // Same ID on both should be invalid (physically impossible)
                bool isValid = MultiTouchHandler.ValidateSimultaneousInputs(touchId, touchId);
                
                Assert.IsFalse(
                    isValid,
                    $"Failed for touchId={touchId}. Same touch ID cannot be on both joystick and skill button"
                );
            }
        }
        
        /// <summary>
        /// Property 12: Touch IDs are unique across all active touches
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property12_TouchIds_AreUnique()
        {
            // Property: All active touch IDs should be unique
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Generate random number of touches (1-5)
                int touchCount = _random.Next(1, 6);
                var touches = new MultiTouchHandler.TouchInfo[touchCount];
                
                // Generate unique touch IDs
                HashSet<int> usedIds = new HashSet<int>();
                for (int j = 0; j < touchCount; j++)
                {
                    int id;
                    do
                    {
                        id = _random.Next(0, 100);
                    } while (usedIds.Contains(id));
                    
                    usedIds.Add(id);
                    
                    touches[j] = new MultiTouchHandler.TouchInfo(
                        id,
                        new Vector2((float)_random.NextDouble() * 1920, (float)_random.NextDouble() * 1080),
                        (MultiTouchHandler.TouchTarget)_random.Next(0, 4)
                    );
                }
                
                bool areUnique = MultiTouchHandler.ValidateTouchIdUniqueness(touches);
                
                Assert.IsTrue(
                    areUnique,
                    $"Failed for {touchCount} touches. All touch IDs should be unique"
                );
            }
        }
        
        /// <summary>
        /// Property 12: Duplicate touch IDs are detected
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property12_DuplicateTouchIds_AreDetected()
        {
            // Property: Duplicate touch IDs should be detected as invalid
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Generate touches with at least one duplicate
                int touchCount = _random.Next(2, 6);
                var touches = new MultiTouchHandler.TouchInfo[touchCount];
                
                int duplicateId = _random.Next(0, 100);
                
                for (int j = 0; j < touchCount; j++)
                {
                    // Make first two touches have the same ID
                    int id = (j < 2) ? duplicateId : _random.Next(0, 100);
                    
                    touches[j] = new MultiTouchHandler.TouchInfo(
                        id,
                        new Vector2((float)_random.NextDouble() * 1920, (float)_random.NextDouble() * 1080),
                        (MultiTouchHandler.TouchTarget)_random.Next(0, 4)
                    );
                }
                
                bool areUnique = MultiTouchHandler.ValidateTouchIdUniqueness(touches);
                
                Assert.IsFalse(
                    areUnique,
                    $"Failed for {touchCount} touches with duplicate ID {duplicateId}. Duplicates should be detected"
                );
            }
        }
        
        /// <summary>
        /// Property 12: Touch target determination is consistent
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property12_TouchTargetDetermination_IsConsistent()
        {
            // Property: Same position should always return same target
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Generate random bounds
                Rect joystickBounds = new Rect(
                    (float)_random.NextDouble() * 200,
                    (float)_random.NextDouble() * 200,
                    (float)_random.NextDouble() * 200 + 100,
                    (float)_random.NextDouble() * 200 + 100
                );
                
                Rect skillWheelBounds = new Rect(
                    (float)_random.NextDouble() * 200 + 500,
                    (float)_random.NextDouble() * 200,
                    (float)_random.NextDouble() * 200 + 100,
                    (float)_random.NextDouble() * 200 + 100
                );
                
                // Generate random position
                Vector2 position = new Vector2(
                    (float)_random.NextDouble() * 1920,
                    (float)_random.NextDouble() * 1080
                );
                
                // Call twice and verify consistency
                var target1 = MultiTouchHandler.DetermineTouchTarget(position, joystickBounds, skillWheelBounds);
                var target2 = MultiTouchHandler.DetermineTouchTarget(position, joystickBounds, skillWheelBounds);
                
                Assert.AreEqual(
                    target1, target2,
                    $"Failed for position={position}. Target determination should be consistent"
                );
            }
        }
        
        /// <summary>
        /// Property 12: Position inside joystick bounds returns Joystick target
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property12_PositionInsideJoystick_ReturnsJoystickTarget()
        {
            // Property: Position inside joystick bounds should return Joystick target
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Generate joystick bounds
                float x = (float)_random.NextDouble() * 200;
                float y = (float)_random.NextDouble() * 200;
                float width = (float)_random.NextDouble() * 200 + 100;
                float height = (float)_random.NextDouble() * 200 + 100;
                Rect joystickBounds = new Rect(x, y, width, height);
                
                // Skill wheel bounds (non-overlapping)
                Rect skillWheelBounds = new Rect(x + width + 100, y, width, height);
                
                // Generate position inside joystick
                Vector2 position = new Vector2(
                    x + (float)_random.NextDouble() * width,
                    y + (float)_random.NextDouble() * height
                );
                
                var target = MultiTouchHandler.DetermineTouchTarget(position, joystickBounds, skillWheelBounds);
                
                Assert.AreEqual(
                    MultiTouchHandler.TouchTarget.Joystick, target,
                    $"Failed for position={position} inside joystick bounds={joystickBounds}"
                );
            }
        }
        
        /// <summary>
        /// Property 12: Position inside skill wheel bounds returns SkillButton target
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property12_PositionInsideSkillWheel_ReturnsSkillButtonTarget()
        {
            // Property: Position inside skill wheel bounds should return SkillButton target
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Generate joystick bounds
                float jx = (float)_random.NextDouble() * 200;
                float jy = (float)_random.NextDouble() * 200;
                float jwidth = (float)_random.NextDouble() * 200 + 100;
                float jheight = (float)_random.NextDouble() * 200 + 100;
                Rect joystickBounds = new Rect(jx, jy, jwidth, jheight);
                
                // Skill wheel bounds (non-overlapping)
                float sx = jx + jwidth + 100;
                float sy = (float)_random.NextDouble() * 200;
                float swidth = (float)_random.NextDouble() * 200 + 100;
                float sheight = (float)_random.NextDouble() * 200 + 100;
                Rect skillWheelBounds = new Rect(sx, sy, swidth, sheight);
                
                // Generate position inside skill wheel
                Vector2 position = new Vector2(
                    sx + (float)_random.NextDouble() * swidth,
                    sy + (float)_random.NextDouble() * sheight
                );
                
                var target = MultiTouchHandler.DetermineTouchTarget(position, joystickBounds, skillWheelBounds);
                
                Assert.AreEqual(
                    MultiTouchHandler.TouchTarget.SkillButton, target,
                    $"Failed for position={position} inside skill wheel bounds={skillWheelBounds}"
                );
            }
        }
        
        /// <summary>
        /// Property 12: Position outside both bounds returns None target
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property12_PositionOutsideBothBounds_ReturnsNoneTarget()
        {
            // Property: Position outside both bounds should return None target
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Generate non-overlapping bounds in corners
                Rect joystickBounds = new Rect(0, 0, 200, 200);
                Rect skillWheelBounds = new Rect(1720, 0, 200, 200);
                
                // Generate position in the middle (outside both)
                Vector2 position = new Vector2(
                    500 + (float)_random.NextDouble() * 920, // 500 to 1420
                    500 + (float)_random.NextDouble() * 380  // 500 to 880
                );
                
                var target = MultiTouchHandler.DetermineTouchTarget(position, joystickBounds, skillWheelBounds);
                
                Assert.AreEqual(
                    MultiTouchHandler.TouchTarget.None, target,
                    $"Failed for position={position} outside both bounds"
                );
            }
        }
        
        /// <summary>
        /// Property 12: Simultaneous inputs produce correct result
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property12_SimultaneousInputs_ProduceCorrectResult()
        {
            // Property: Processing simultaneous inputs should produce correct result
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Generate joystick input
                var joystickInput = new MultiTouchHandler.TouchInfo(
                    _random.Next(0, 5),
                    new Vector2((float)_random.NextDouble() * 200, (float)_random.NextDouble() * 200),
                    MultiTouchHandler.TouchTarget.Joystick
                );
                
                // Generate skill button input with different ID
                var skillButtonInput = new MultiTouchHandler.TouchInfo(
                    _random.Next(5, 10),
                    new Vector2(1720 + (float)_random.NextDouble() * 200, (float)_random.NextDouble() * 200),
                    MultiTouchHandler.TouchTarget.SkillButton
                );
                
                var result = MultiTouchHandler.ProcessSimultaneousInputs(joystickInput, skillButtonInput);
                
                Assert.IsTrue(
                    result.JoystickActive,
                    "Joystick should be active when joystick input is provided"
                );
                Assert.IsTrue(
                    result.SkillButtonPressed,
                    "Skill button should be pressed when skill button input is provided"
                );
            }
        }
        
        /// <summary>
        /// Property 12: Joystick state is preserved during skill button press
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property12_JoystickState_IsPreservedDuringSkillPress()
        {
            // Property: Joystick state should be preserved when skill button is pressed
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Generate random joystick state
                bool joystickActive = _random.NextDouble() > 0.3; // 70% chance active
                Vector2 joystickDirection = joystickActive 
                    ? new Vector2((float)(_random.NextDouble() * 2 - 1), (float)(_random.NextDouble() * 2 - 1)).normalized
                    : Vector2.zero;
                
                bool skillButtonPressed = true;
                
                // After skill press, joystick should maintain state
                bool joystickActiveAfter = joystickActive;
                Vector2 joystickDirectionAfter = joystickDirection;
                
                bool isPreserved = MultiTouchHandler.ValidateJoystickPreservedDuringSkillPress(
                    joystickActive,
                    joystickDirection,
                    skillButtonPressed,
                    joystickActiveAfter,
                    joystickDirectionAfter
                );
                
                Assert.IsTrue(
                    isPreserved,
                    $"Failed for joystickActive={joystickActive}, direction={joystickDirection}. " +
                    "Joystick state should be preserved during skill press"
                );
            }
        }
        
        /// <summary>
        /// Property 12: Joystick deactivation during skill press is invalid
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property12_JoystickDeactivation_DuringSkillPress_IsInvalid()
        {
            // Property: Joystick should not be deactivated by skill button press
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Joystick was active
                bool joystickActive = true;
                Vector2 joystickDirection = new Vector2(
                    (float)(_random.NextDouble() * 2 - 1),
                    (float)(_random.NextDouble() * 2 - 1)
                ).normalized;
                
                bool skillButtonPressed = true;
                
                // Simulate invalid state: joystick deactivated after skill press
                bool joystickActiveAfter = false;
                Vector2 joystickDirectionAfter = Vector2.zero;
                
                bool isPreserved = MultiTouchHandler.ValidateJoystickPreservedDuringSkillPress(
                    joystickActive,
                    joystickDirection,
                    skillButtonPressed,
                    joystickActiveAfter,
                    joystickDirectionAfter
                );
                
                Assert.IsFalse(
                    isPreserved,
                    "Joystick deactivation during skill press should be detected as invalid"
                );
            }
        }
        
        /// <summary>
        /// Property 12: No input produces empty result
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property12_NoInput_ProducesEmptyResult()
        {
            // Property: No input should produce empty result
            var result = MultiTouchHandler.ProcessSimultaneousInputs(null, null);
            
            Assert.IsFalse(result.JoystickActive, "Joystick should not be active with no input");
            Assert.IsFalse(result.SkillButtonPressed, "Skill button should not be pressed with no input");
            Assert.AreEqual(Vector2.zero, result.JoystickDirection, "Direction should be zero with no input");
        }
        
        /// <summary>
        /// Property 12: Single joystick input works correctly
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property12_SingleJoystickInput_WorksCorrectly()
        {
            // Property: Single joystick input should work without skill button
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                var joystickInput = new MultiTouchHandler.TouchInfo(
                    _random.Next(0, 10),
                    new Vector2((float)_random.NextDouble() * 200, (float)_random.NextDouble() * 200),
                    MultiTouchHandler.TouchTarget.Joystick
                );
                
                var result = MultiTouchHandler.ProcessSimultaneousInputs(joystickInput, null);
                
                Assert.IsTrue(result.JoystickActive, "Joystick should be active");
                Assert.IsFalse(result.SkillButtonPressed, "Skill button should not be pressed");
            }
        }
        
        /// <summary>
        /// Property 12: Single skill button input works correctly
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property12_SingleSkillButtonInput_WorksCorrectly()
        {
            // Property: Single skill button input should work without joystick
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                var skillButtonInput = new MultiTouchHandler.TouchInfo(
                    _random.Next(0, 10),
                    new Vector2(1720 + (float)_random.NextDouble() * 200, (float)_random.NextDouble() * 200),
                    MultiTouchHandler.TouchTarget.SkillButton
                );
                
                var result = MultiTouchHandler.ProcessSimultaneousInputs(null, skillButtonInput);
                
                Assert.IsFalse(result.JoystickActive, "Joystick should not be active");
                Assert.IsTrue(result.SkillButtonPressed, "Skill button should be pressed");
            }
        }
        
        #endregion
        
        #region Edge Cases
        
        /// <summary>
        /// Test empty touch array
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void EdgeCase_EmptyTouchArray_IsValid()
        {
            var touches = new MultiTouchHandler.TouchInfo[0];
            bool areUnique = MultiTouchHandler.ValidateTouchIdUniqueness(touches);
            Assert.IsTrue(areUnique, "Empty array should be valid");
        }
        
        /// <summary>
        /// Test null touch array
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void EdgeCase_NullTouchArray_IsValid()
        {
            bool areUnique = MultiTouchHandler.ValidateTouchIdUniqueness(null);
            Assert.IsTrue(areUnique, "Null array should be valid");
        }
        
        /// <summary>
        /// Test single touch array
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void EdgeCase_SingleTouchArray_IsValid()
        {
            var touches = new MultiTouchHandler.TouchInfo[]
            {
                new MultiTouchHandler.TouchInfo(0, Vector2.zero, MultiTouchHandler.TouchTarget.Joystick)
            };
            bool areUnique = MultiTouchHandler.ValidateTouchIdUniqueness(touches);
            Assert.IsTrue(areUnique, "Single touch array should be valid");
        }
        
        /// <summary>
        /// Test inactive touch IDs (-1)
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void EdgeCase_InactiveTouchIds_AreHandled()
        {
            // -1 indicates no active touch
            bool canProcess = MultiTouchHandler.ValidateSimultaneousInputs(-1, -1);
            Assert.IsTrue(canProcess, "Both inactive should be valid");
            
            canProcess = MultiTouchHandler.ValidateSimultaneousInputs(0, -1);
            Assert.IsTrue(canProcess, "One active, one inactive should be valid");
            
            canProcess = MultiTouchHandler.ValidateSimultaneousInputs(-1, 0);
            Assert.IsTrue(canProcess, "One inactive, one active should be valid");
        }
        
        #endregion
    }
}
