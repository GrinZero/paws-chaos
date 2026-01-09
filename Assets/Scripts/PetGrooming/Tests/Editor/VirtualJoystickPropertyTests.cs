using System;
using NUnit.Framework;
using UnityEngine;
using PetGrooming.UI.MobileUI;

namespace PetGrooming.Tests.Editor
{
    /// <summary>
    /// Property-based tests for VirtualJoystick.
    /// Uses NUnit with manual random input generation (100+ iterations per property).
    /// 
    /// Feature: mobile-ui-controls
    /// Validates: Requirements 1.4, 1.5
    /// </summary>
    [TestFixture]
    public class VirtualJoystickPropertyTests
    {
        private const int PropertyTestIterations = 100;
        private System.Random _random;
        
        [SetUp]
        public void SetUp()
        {
            _random = new System.Random(42); // Fixed seed for reproducibility
        }
        
        #region Property 1: Joystick Direction Normalization
        
        /// <summary>
        /// Feature: mobile-ui-controls, Property 1: Joystick Direction Normalization
        /// 
        /// *For any* drag position on the virtual joystick, the output direction vector 
        /// SHALL have components within the range [-1, 1] on both X and Y axes.
        /// 
        /// Validates: Requirements 1.4
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property1_DirectionComponents_AreWithinNormalizedRange()
        {
            // Property: For all handle positions, direction.x and direction.y are in [-1, 1]
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Generate random handle position (can be anywhere, even outside radius)
                Vector2 handlePosition = new Vector2(
                    (float)(_random.NextDouble() * 200 - 100), // -100 to 100
                    (float)(_random.NextDouble() * 200 - 100)
                );
                
                Vector2 direction = VirtualJoystick.CalculateNormalizedDirection(handlePosition);
                
                Assert.GreaterOrEqual(
                    direction.x, -1f,
                    $"Failed for handlePosition={handlePosition}. Direction.x={direction.x} should be >= -1"
                );
                Assert.LessOrEqual(
                    direction.x, 1f,
                    $"Failed for handlePosition={handlePosition}. Direction.x={direction.x} should be <= 1"
                );
                Assert.GreaterOrEqual(
                    direction.y, -1f,
                    $"Failed for handlePosition={handlePosition}. Direction.y={direction.y} should be >= -1"
                );
                Assert.LessOrEqual(
                    direction.y, 1f,
                    $"Failed for handlePosition={handlePosition}. Direction.y={direction.y} should be <= 1"
                );
            }
        }
        
        /// <summary>
        /// Property 1: Direction magnitude is at most 1 (normalized)
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property1_DirectionMagnitude_IsAtMostOne()
        {
            // Property: For all handle positions, |direction| <= 1
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                Vector2 handlePosition = new Vector2(
                    (float)(_random.NextDouble() * 200 - 100),
                    (float)(_random.NextDouble() * 200 - 100)
                );
                
                Vector2 direction = VirtualJoystick.CalculateNormalizedDirection(handlePosition);
                float magnitude = direction.magnitude;
                
                Assert.LessOrEqual(
                    magnitude, 1f + 0.0001f, // Small epsilon for float comparison
                    $"Failed for handlePosition={handlePosition}. Direction magnitude={magnitude} should be <= 1"
                );
            }
        }
        
        /// <summary>
        /// Property 1: Zero handle position produces zero direction
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property1_ZeroHandlePosition_ProducesZeroDirection()
        {
            // Property: Handle at center produces zero direction
            Vector2 direction = VirtualJoystick.CalculateNormalizedDirection(Vector2.zero);
            
            Assert.AreEqual(0f, direction.x, 0.0001f, "Direction.x should be 0 for zero handle position");
            Assert.AreEqual(0f, direction.y, 0.0001f, "Direction.y should be 0 for zero handle position");
        }
        
        /// <summary>
        /// Property 1: Very small handle positions produce zero direction
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property1_VerySmallHandlePosition_ProducesZeroDirection()
        {
            // Property: Handle positions below threshold produce zero direction
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Generate very small positions (below 0.01)
                Vector2 handlePosition = new Vector2(
                    (float)(_random.NextDouble() * 0.009),
                    (float)(_random.NextDouble() * 0.009)
                );
                
                Vector2 direction = VirtualJoystick.CalculateNormalizedDirection(handlePosition);
                
                Assert.AreEqual(
                    0f, direction.magnitude, 0.0001f,
                    $"Failed for handlePosition={handlePosition}. Very small positions should produce zero direction"
                );
            }
        }
        
        /// <summary>
        /// Property 1: Direction preserves angle of handle position
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property1_Direction_PreservesAngle()
        {
            // Property: For all non-zero handle positions, direction angle equals handle angle
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Generate non-zero handle position
                float angle = (float)(_random.NextDouble() * 2 * Math.PI);
                float distance = (float)(_random.NextDouble() * 100 + 1); // 1 to 101
                
                Vector2 handlePosition = new Vector2(
                    Mathf.Cos(angle) * distance,
                    Mathf.Sin(angle) * distance
                );
                
                Vector2 direction = VirtualJoystick.CalculateNormalizedDirection(handlePosition);
                
                // Calculate angles
                float handleAngle = Mathf.Atan2(handlePosition.y, handlePosition.x);
                float directionAngle = Mathf.Atan2(direction.y, direction.x);
                
                Assert.AreEqual(
                    handleAngle, directionAngle, 0.001f,
                    $"Failed for handlePosition={handlePosition}. Direction angle should match handle angle"
                );
            }
        }
        
        /// <summary>
        /// Property 1: ClampDirection always produces values in [-1, 1]
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property1_ClampDirection_AlwaysProducesValidRange()
        {
            // Property: For all input vectors, clamped output is in [-1, 1]
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                Vector2 input = new Vector2(
                    (float)(_random.NextDouble() * 10 - 5), // -5 to 5
                    (float)(_random.NextDouble() * 10 - 5)
                );
                
                Vector2 clamped = VirtualJoystick.ClampDirection(input);
                
                Assert.GreaterOrEqual(clamped.x, -1f, $"Clamped.x should be >= -1 for input={input}");
                Assert.LessOrEqual(clamped.x, 1f, $"Clamped.x should be <= 1 for input={input}");
                Assert.GreaterOrEqual(clamped.y, -1f, $"Clamped.y should be >= -1 for input={input}");
                Assert.LessOrEqual(clamped.y, 1f, $"Clamped.y should be <= 1 for input={input}");
            }
        }
        
        #endregion
        
        #region Property 2: Joystick Handle Clamping
        
        /// <summary>
        /// Feature: mobile-ui-controls, Property 2: Joystick Handle Clamping
        /// 
        /// *For any* touch position outside the joystick background radius, the handle position 
        /// SHALL be clamped to the edge of the background circle (distance from center equals radius).
        /// 
        /// Validates: Requirements 1.5
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property2_HandleOutsideRadius_ClampedToEdge()
        {
            // Property: For all positions outside radius, clamped position is at radius
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float maxRadius = (float)(_random.NextDouble() * 100 + 10); // 10 to 110
                
                // Generate position outside radius
                float angle = (float)(_random.NextDouble() * 2 * Math.PI);
                float distance = maxRadius + (float)(_random.NextDouble() * 100 + 1); // Beyond radius
                
                Vector2 inputOffset = new Vector2(
                    Mathf.Cos(angle) * distance,
                    Mathf.Sin(angle) * distance
                );
                
                Vector2 clampedPosition = VirtualJoystick.CalculateClampedHandlePosition(inputOffset, maxRadius);
                float clampedDistance = clampedPosition.magnitude;
                
                Assert.AreEqual(
                    maxRadius, clampedDistance, 0.001f,
                    $"Failed for inputOffset={inputOffset}, maxRadius={maxRadius}. " +
                    $"Clamped distance={clampedDistance} should equal maxRadius"
                );
            }
        }
        
        /// <summary>
        /// Property 2: Handle inside radius is not modified
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property2_HandleInsideRadius_NotModified()
        {
            // Property: For all positions inside radius, position is unchanged
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float maxRadius = (float)(_random.NextDouble() * 100 + 10); // 10 to 110
                
                // Generate position inside radius
                float angle = (float)(_random.NextDouble() * 2 * Math.PI);
                float distance = (float)(_random.NextDouble() * maxRadius * 0.99); // Inside radius
                
                Vector2 inputOffset = new Vector2(
                    Mathf.Cos(angle) * distance,
                    Mathf.Sin(angle) * distance
                );
                
                Vector2 clampedPosition = VirtualJoystick.CalculateClampedHandlePosition(inputOffset, maxRadius);
                
                Assert.AreEqual(
                    inputOffset.x, clampedPosition.x, 0.001f,
                    $"Failed for inputOffset={inputOffset}. X should be unchanged"
                );
                Assert.AreEqual(
                    inputOffset.y, clampedPosition.y, 0.001f,
                    $"Failed for inputOffset={inputOffset}. Y should be unchanged"
                );
            }
        }
        
        /// <summary>
        /// Property 2: Handle exactly at radius is not modified
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property2_HandleExactlyAtRadius_NotModified()
        {
            // Property: For all positions exactly at radius, position is unchanged
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float maxRadius = (float)(_random.NextDouble() * 100 + 10); // 10 to 110
                
                // Generate position exactly at radius
                float angle = (float)(_random.NextDouble() * 2 * Math.PI);
                
                Vector2 inputOffset = new Vector2(
                    Mathf.Cos(angle) * maxRadius,
                    Mathf.Sin(angle) * maxRadius
                );
                
                Vector2 clampedPosition = VirtualJoystick.CalculateClampedHandlePosition(inputOffset, maxRadius);
                float clampedDistance = clampedPosition.magnitude;
                
                Assert.AreEqual(
                    maxRadius, clampedDistance, 0.001f,
                    $"Failed for inputOffset={inputOffset}. Position at radius should remain at radius"
                );
            }
        }
        
        /// <summary>
        /// Property 2: Clamped handle preserves direction
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property2_ClampedHandle_PreservesDirection()
        {
            // Property: For all positions outside radius, clamped direction equals original direction
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float maxRadius = (float)(_random.NextDouble() * 100 + 10);
                
                // Generate position outside radius
                float angle = (float)(_random.NextDouble() * 2 * Math.PI);
                float distance = maxRadius + (float)(_random.NextDouble() * 100 + 1);
                
                Vector2 inputOffset = new Vector2(
                    Mathf.Cos(angle) * distance,
                    Mathf.Sin(angle) * distance
                );
                
                Vector2 clampedPosition = VirtualJoystick.CalculateClampedHandlePosition(inputOffset, maxRadius);
                
                // Calculate angles
                float inputAngle = Mathf.Atan2(inputOffset.y, inputOffset.x);
                float clampedAngle = Mathf.Atan2(clampedPosition.y, clampedPosition.x);
                
                Assert.AreEqual(
                    inputAngle, clampedAngle, 0.001f,
                    $"Failed for inputOffset={inputOffset}. Clamped direction should match original"
                );
            }
        }
        
        /// <summary>
        /// Property 2: Handle is always within radius after clamping
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property2_ClampedHandle_AlwaysWithinRadius()
        {
            // Property: For all input positions, clamped position is within or at radius
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float maxRadius = (float)(_random.NextDouble() * 100 + 10);
                
                // Generate any position
                Vector2 inputOffset = new Vector2(
                    (float)(_random.NextDouble() * 400 - 200), // -200 to 200
                    (float)(_random.NextDouble() * 400 - 200)
                );
                
                Vector2 clampedPosition = VirtualJoystick.CalculateClampedHandlePosition(inputOffset, maxRadius);
                
                bool isWithinRadius = VirtualJoystick.IsHandleWithinRadius(clampedPosition, maxRadius);
                
                Assert.IsTrue(
                    isWithinRadius,
                    $"Failed for inputOffset={inputOffset}, maxRadius={maxRadius}. " +
                    $"Clamped position={clampedPosition} (distance={clampedPosition.magnitude}) should be within radius"
                );
            }
        }
        
        /// <summary>
        /// Property 2: Zero radius produces zero position
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property2_ZeroRadius_ProducesZeroPosition()
        {
            // Property: For zero radius, all positions clamp to zero
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                Vector2 inputOffset = new Vector2(
                    (float)(_random.NextDouble() * 200 - 100),
                    (float)(_random.NextDouble() * 200 - 100)
                );
                
                Vector2 clampedPosition = VirtualJoystick.CalculateClampedHandlePosition(inputOffset, 0f);
                
                Assert.AreEqual(
                    0f, clampedPosition.magnitude, 0.0001f,
                    $"Failed for inputOffset={inputOffset}. Zero radius should produce zero position"
                );
            }
        }
        
        /// <summary>
        /// Property 2: IsHandleWithinRadius is consistent with clamping
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property2_IsHandleWithinRadius_ConsistentWithClamping()
        {
            // Property: Position is within radius iff clamping doesn't change it
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float maxRadius = (float)(_random.NextDouble() * 100 + 10);
                
                Vector2 inputOffset = new Vector2(
                    (float)(_random.NextDouble() * 200 - 100),
                    (float)(_random.NextDouble() * 200 - 100)
                );
                
                bool isWithinRadius = VirtualJoystick.IsHandleWithinRadius(inputOffset, maxRadius);
                Vector2 clampedPosition = VirtualJoystick.CalculateClampedHandlePosition(inputOffset, maxRadius);
                
                bool wasModified = Vector2.Distance(inputOffset, clampedPosition) > 0.001f;
                
                // If within radius, should not be modified
                // If outside radius, should be modified
                Assert.AreEqual(
                    isWithinRadius, !wasModified,
                    $"Failed for inputOffset={inputOffset}, maxRadius={maxRadius}. " +
                    $"isWithinRadius={isWithinRadius}, wasModified={wasModified}"
                );
            }
        }
        
        #endregion
        
        #region Combined Properties
        
        /// <summary>
        /// Combined: Clamping and normalization work together correctly
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Combined_ClampingAndNormalization_WorkTogether()
        {
            // Property: Clamped position produces valid normalized direction
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float maxRadius = (float)(_random.NextDouble() * 100 + 10);
                
                Vector2 inputOffset = new Vector2(
                    (float)(_random.NextDouble() * 400 - 200),
                    (float)(_random.NextDouble() * 400 - 200)
                );
                
                Vector2 clampedPosition = VirtualJoystick.CalculateClampedHandlePosition(inputOffset, maxRadius);
                Vector2 direction = VirtualJoystick.CalculateNormalizedDirection(clampedPosition);
                
                // Direction should be valid
                Assert.GreaterOrEqual(direction.x, -1f);
                Assert.LessOrEqual(direction.x, 1f);
                Assert.GreaterOrEqual(direction.y, -1f);
                Assert.LessOrEqual(direction.y, 1f);
                Assert.LessOrEqual(direction.magnitude, 1f + 0.0001f);
            }
        }
        
        #endregion
    }
}
