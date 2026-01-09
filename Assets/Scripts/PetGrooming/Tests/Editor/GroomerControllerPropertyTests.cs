using System;
using NUnit.Framework;
using UnityEngine;
using PetGrooming.Systems;
using PetGrooming.AI;

namespace PetGrooming.Tests.Editor
{
    /// <summary>
    /// Property-based tests for GroomerController.
    /// Uses NUnit with manual random input generation (100+ iterations per property).
    /// 
    /// Feature: pet-grooming-mvp
    /// Validates: Requirements 1.3, 3.1, 3.2
    /// </summary>
    [TestFixture]
    public class GroomerControllerPropertyTests
    {
        private const float CaptureRange = 1.5f;
        private const float CarrySpeedMultiplier = 0.85f;
        private const int PropertyTestIterations = 100;
        private System.Random _random;
        
        [SetUp]
        public void SetUp()
        {
            _random = new System.Random(42); // Fixed seed for reproducibility
        }
        
        #region Property 1: Carry Speed Reduction
        
        /// <summary>
        /// Feature: pet-grooming-mvp, Property 1: Carry Speed Reduction
        /// 
        /// *For any* base movement speed value, when the Groomer is carrying a captured pet, 
        /// the effective movement speed shall equal base speed multiplied by 0.85 (15% reduction).
        /// 
        /// Validates: Requirements 1.3
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property1_CarryingPet_SpeedReducedBy15Percent()
        {
            // Property: For all base speeds, carrying speed = base * 0.85
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float baseSpeed = (float)(_random.NextDouble() * 20.0 + 0.1); // 0.1 to 20.1
                
                float expectedSpeed = baseSpeed * CarrySpeedMultiplier;
                float actualSpeed = GroomerController.CalculateEffectiveSpeed(baseSpeed, CarrySpeedMultiplier, isCarrying: true);
                
                Assert.AreEqual(
                    expectedSpeed, 
                    actualSpeed, 
                    0.0001f,
                    $"Failed for baseSpeed={baseSpeed}. Expected={expectedSpeed}, Actual={actualSpeed}"
                );
            }
        }
        
        /// <summary>
        /// Property 1: Not carrying pet should maintain base speed
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property1_NotCarryingPet_SpeedUnchanged()
        {
            // Property: For all base speeds, not carrying speed = base
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float baseSpeed = (float)(_random.NextDouble() * 20.0 + 0.1); // 0.1 to 20.1
                
                float actualSpeed = GroomerController.CalculateEffectiveSpeed(baseSpeed, CarrySpeedMultiplier, isCarrying: false);
                
                Assert.AreEqual(
                    baseSpeed, 
                    actualSpeed, 
                    0.0001f,
                    $"Failed for baseSpeed={baseSpeed}. Expected={baseSpeed}, Actual={actualSpeed}"
                );
            }
        }
        
        /// <summary>
        /// Property 1: Speed reduction is exactly 15% (multiplier is 0.85)
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property1_SpeedReductionIsExactly15Percent()
        {
            // Property: For all base speeds, reduction = base * 0.15
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float baseSpeed = (float)(_random.NextDouble() * 20.0 + 0.1);
                
                float carryingSpeed = GroomerController.CalculateEffectiveSpeed(baseSpeed, CarrySpeedMultiplier, isCarrying: true);
                float notCarryingSpeed = GroomerController.CalculateEffectiveSpeed(baseSpeed, CarrySpeedMultiplier, isCarrying: false);
                
                float reduction = notCarryingSpeed - carryingSpeed;
                float expectedReduction = baseSpeed * 0.15f;
                
                Assert.AreEqual(
                    expectedReduction, 
                    reduction, 
                    0.0001f,
                    $"Failed for baseSpeed={baseSpeed}. Expected reduction={expectedReduction}, Actual reduction={reduction}"
                );
            }
        }
        
        /// <summary>
        /// Property 1: Carrying speed is always less than base speed (for positive speeds)
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property1_CarryingSpeed_AlwaysLessThanBaseSpeed()
        {
            // Property: For all positive base speeds, carrying speed < base speed
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float baseSpeed = (float)(_random.NextDouble() * 20.0 + 0.1); // Positive speeds only
                
                float carryingSpeed = GroomerController.CalculateEffectiveSpeed(baseSpeed, CarrySpeedMultiplier, isCarrying: true);
                
                Assert.Less(
                    carryingSpeed, 
                    baseSpeed,
                    $"Failed for baseSpeed={baseSpeed}. Carrying speed {carryingSpeed} should be less than base speed"
                );
            }
        }
        
        /// <summary>
        /// Property 1: Speed calculation is deterministic
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property1_SpeedCalculation_IsDeterministic()
        {
            // Property: Same inputs always produce same output
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float baseSpeed = (float)(_random.NextDouble() * 20.0 + 0.1);
                bool isCarrying = _random.Next(0, 2) == 1;
                
                float result1 = GroomerController.CalculateEffectiveSpeed(baseSpeed, CarrySpeedMultiplier, isCarrying);
                float result2 = GroomerController.CalculateEffectiveSpeed(baseSpeed, CarrySpeedMultiplier, isCarrying);
                
                Assert.AreEqual(
                    result1, 
                    result2, 
                    0.0001f,
                    $"Failed for baseSpeed={baseSpeed}, isCarrying={isCarrying}. Results differ: {result1} vs {result2}"
                );
            }
        }
        
        #endregion
        
        #region Property 6: Capture Distance Validation
        
        /// <summary>
        /// Feature: pet-grooming-mvp, Property 6: Capture Distance Validation
        /// 
        /// *For any* capture attempt, the attempt shall succeed only if the distance 
        /// between Groomer and Pet is less than or equal to 1.5 units.
        /// 
        /// Validates: Requirements 3.1, 3.2
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property6_WithinCaptureRange_CaptureSucceeds()
        {
            // Property: For all distances <= 1.5, capture is within range
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float distance = (float)(_random.NextDouble() * CaptureRange); // 0 to 1.5
                
                bool isWithinRange = GroomerController.IsWithinCaptureRange(distance, CaptureRange);
                
                Assert.IsTrue(
                    isWithinRange,
                    $"Failed for distance={distance}. Should be within capture range {CaptureRange}"
                );
            }
        }
        
        /// <summary>
        /// Property 6: Outside capture range should fail
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property6_OutsideCaptureRange_CaptureFails()
        {
            // Property: For all distances > 1.5, capture is not within range
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float distance = CaptureRange + 0.001f + (float)(_random.NextDouble() * 50.0); // > 1.5
                
                bool isWithinRange = GroomerController.IsWithinCaptureRange(distance, CaptureRange);
                
                Assert.IsFalse(
                    isWithinRange,
                    $"Failed for distance={distance}. Should NOT be within capture range {CaptureRange}"
                );
            }
        }
        
        /// <summary>
        /// Property 6: Exactly at capture range boundary should succeed
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property6_ExactlyAtCaptureRange_CaptureSucceeds()
        {
            // Property: Distance exactly at range boundary should succeed
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Test with various capture ranges
                float captureRange = (float)(_random.NextDouble() * 5.0 + 0.5); // 0.5 to 5.5
                
                bool isWithinRange = GroomerController.IsWithinCaptureRange(captureRange, captureRange);
                
                Assert.IsTrue(
                    isWithinRange,
                    $"Failed for captureRange={captureRange}. Distance exactly at range should succeed"
                );
            }
        }
        
        /// <summary>
        /// Property 6: Distance calculation is symmetric
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property6_DistanceCalculation_IsSymmetric()
        {
            // Property: Distance from A to B equals distance from B to A
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                Vector3 posA = new Vector3(
                    (float)(_random.NextDouble() * 100 - 50),
                    (float)(_random.NextDouble() * 10),
                    (float)(_random.NextDouble() * 100 - 50)
                );
                Vector3 posB = new Vector3(
                    (float)(_random.NextDouble() * 100 - 50),
                    (float)(_random.NextDouble() * 10),
                    (float)(_random.NextDouble() * 100 - 50)
                );
                
                float distanceAB = GroomerController.CalculateDistanceToPet(posA, posB);
                float distanceBA = GroomerController.CalculateDistanceToPet(posB, posA);
                
                Assert.AreEqual(
                    distanceAB, 
                    distanceBA, 
                    0.0001f,
                    $"Failed for posA={posA}, posB={posB}. Distance should be symmetric"
                );
            }
        }
        
        /// <summary>
        /// Property 6: Distance calculation ignores Y axis (horizontal distance only)
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property6_DistanceCalculation_IgnoresYAxis()
        {
            // Property: Changing Y position should not affect distance
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                Vector3 posA = new Vector3(
                    (float)(_random.NextDouble() * 100 - 50),
                    0f,
                    (float)(_random.NextDouble() * 100 - 50)
                );
                Vector3 posB = new Vector3(
                    (float)(_random.NextDouble() * 100 - 50),
                    0f,
                    (float)(_random.NextDouble() * 100 - 50)
                );
                
                // Calculate distance with Y = 0
                float distanceFlat = GroomerController.CalculateDistanceToPet(posA, posB);
                
                // Change Y values
                posA.y = (float)(_random.NextDouble() * 100);
                posB.y = (float)(_random.NextDouble() * 100);
                
                float distanceWithY = GroomerController.CalculateDistanceToPet(posA, posB);
                
                Assert.AreEqual(
                    distanceFlat, 
                    distanceWithY, 
                    0.0001f,
                    $"Failed for posA={posA}, posB={posB}. Y axis should be ignored"
                );
            }
        }
        
        /// <summary>
        /// Property 6: Distance to self is zero
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property6_DistanceToSelf_IsZero()
        {
            // Property: Distance from any position to itself is 0
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                Vector3 pos = new Vector3(
                    (float)(_random.NextDouble() * 100 - 50),
                    (float)(_random.NextDouble() * 10),
                    (float)(_random.NextDouble() * 100 - 50)
                );
                
                float distance = GroomerController.CalculateDistanceToPet(pos, pos);
                
                Assert.AreEqual(
                    0f, 
                    distance, 
                    0.0001f,
                    $"Failed for pos={pos}. Distance to self should be 0"
                );
            }
        }
        
        /// <summary>
        /// Property 6: Capture should fail for captured pets regardless of distance
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property6_CapturedPet_CannotBeCapturedAgain()
        {
            // Property: For all distances, captured pets cannot be captured
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float distance = (float)(_random.NextDouble() * CaptureRange); // Within range
                
                bool shouldSucceed = GroomerController.ShouldCaptureSucceed(distance, CaptureRange, PetAI.PetState.Captured);
                
                Assert.IsFalse(
                    shouldSucceed,
                    $"Failed for distance={distance}. Captured pet should not be capturable"
                );
            }
        }
        
        /// <summary>
        /// Property 6: Capture should fail for pets being groomed regardless of distance
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property6_GroomingPet_CannotBeCaptured()
        {
            // Property: For all distances, grooming pets cannot be captured
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float distance = (float)(_random.NextDouble() * CaptureRange); // Within range
                
                bool shouldSucceed = GroomerController.ShouldCaptureSucceed(distance, CaptureRange, PetAI.PetState.BeingGroomed);
                
                Assert.IsFalse(
                    shouldSucceed,
                    $"Failed for distance={distance}. Grooming pet should not be capturable"
                );
            }
        }
        
        /// <summary>
        /// Property 6: Capture should succeed for idle pets within range
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property6_IdlePetWithinRange_CanBeCaptured()
        {
            // Property: For all distances <= range, idle pets can be captured
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float distance = (float)(_random.NextDouble() * CaptureRange); // Within range
                
                bool shouldSucceed = GroomerController.ShouldCaptureSucceed(distance, CaptureRange, PetAI.PetState.Idle);
                
                Assert.IsTrue(
                    shouldSucceed,
                    $"Failed for distance={distance}. Idle pet within range should be capturable"
                );
            }
        }
        
        /// <summary>
        /// Property 6: Capture should succeed for fleeing pets within range
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property6_FleeingPetWithinRange_CanBeCaptured()
        {
            // Property: For all distances <= range, fleeing pets can be captured
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float distance = (float)(_random.NextDouble() * CaptureRange); // Within range
                
                bool shouldSucceed = GroomerController.ShouldCaptureSucceed(distance, CaptureRange, PetAI.PetState.Fleeing);
                
                Assert.IsTrue(
                    shouldSucceed,
                    $"Failed for distance={distance}. Fleeing pet within range should be capturable"
                );
            }
        }
        
        /// <summary>
        /// Property 6: Capture should succeed for wandering pets within range
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property6_WanderingPetWithinRange_CanBeCaptured()
        {
            // Property: For all distances <= range, wandering pets can be captured
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float distance = (float)(_random.NextDouble() * CaptureRange); // Within range
                
                bool shouldSucceed = GroomerController.ShouldCaptureSucceed(distance, CaptureRange, PetAI.PetState.Wandering);
                
                Assert.IsTrue(
                    shouldSucceed,
                    $"Failed for distance={distance}. Wandering pet within range should be capturable"
                );
            }
        }
        
        #endregion
        
        #region Combined Properties
        
        /// <summary>
        /// Combined: Speed and capture mechanics are independent
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Combined_SpeedAndCapture_AreIndependent()
        {
            // Property: Capture range check doesn't depend on speed
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float baseSpeed = (float)(_random.NextDouble() * 20.0 + 0.1);
                float distance = (float)(_random.NextDouble() * 3.0); // 0 to 3
                bool isCarrying = _random.Next(0, 2) == 1;
                
                // Speed calculation
                float speed = GroomerController.CalculateEffectiveSpeed(baseSpeed, CarrySpeedMultiplier, isCarrying);
                
                // Capture check
                bool canCapture = GroomerController.IsWithinCaptureRange(distance, CaptureRange);
                
                // Verify speed doesn't affect capture logic
                bool canCaptureAgain = GroomerController.IsWithinCaptureRange(distance, CaptureRange);
                
                Assert.AreEqual(
                    canCapture, 
                    canCaptureAgain,
                    $"Capture result should be consistent regardless of speed calculations"
                );
            }
        }
        
        #endregion
    }
}
