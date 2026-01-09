using System;
using NUnit.Framework;
using PetGrooming.Core;

namespace PetGrooming.Tests.Editor
{
    /// <summary>
    /// Property-based tests for GameManager victory conditions.
    /// Uses NUnit with manual random input generation (100+ iterations per property).
    /// 
    /// Feature: pet-grooming-mvp
    /// Validates: Requirements 5.5, 6.3, 6.4, 6.5
    /// </summary>
    [TestFixture]
    public class GameManagerPropertyTests
    {
        private const int MischiefThreshold = 500;
        private const int PropertyTestIterations = 100;
        private System.Random _random;
        
        [SetUp]
        public void SetUp()
        {
            _random = new System.Random(42); // Fixed seed for reproducibility
        }
        
        #region Property 10: Victory Condition - Mischief Threshold
        
        /// <summary>
        /// Feature: pet-grooming-mvp, Property 10: Victory Condition - Mischief Threshold
        /// 
        /// *For any* game state where mischief value is greater than or equal to 500, 
        /// the game state shall be PetWin.
        /// 
        /// Validates: Requirements 5.5, 6.5
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property10_MischiefThresholdReached_ShouldResultInPetWin()
        {
            // Property: For all mischief values >= threshold, result is PetWin
            // (when pet is not groomed)
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                int mischiefValue = _random.Next(MischiefThreshold, MischiefThreshold + 10000);
                float remainingTime = _random.Next(1, 181);
                
                var result = GameManager.DetermineVictoryCondition(
                    mischiefValue: mischiefValue,
                    mischiefThreshold: MischiefThreshold,
                    remainingTime: remainingTime,
                    isPetGroomed: false
                );
                
                Assert.AreEqual(
                    GameManager.GameState.PetWin, 
                    result,
                    $"Failed for mischiefValue={mischiefValue}, remainingTime={remainingTime}"
                );
            }
        }
        
        /// <summary>
        /// Property 10 Edge Case: Mischief exactly at threshold
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property10_MischiefExactlyAtThreshold_ShouldResultInPetWin()
        {
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float remainingTime = _random.Next(1, 181);
                
                var result = GameManager.DetermineVictoryCondition(
                    mischiefValue: MischiefThreshold,
                    mischiefThreshold: MischiefThreshold,
                    remainingTime: remainingTime,
                    isPetGroomed: false
                );
                
                Assert.AreEqual(
                    GameManager.GameState.PetWin, 
                    result,
                    $"Failed for remainingTime={remainingTime}"
                );
            }
        }
        
        /// <summary>
        /// Property 10 Negative: Below threshold should not trigger PetWin (from mischief)
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property10_MischiefBelowThreshold_ShouldNotResultInPetWin()
        {
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                int mischiefValue = _random.Next(0, MischiefThreshold);
                float remainingTime = _random.Next(1, 181);
                
                var result = GameManager.DetermineVictoryCondition(
                    mischiefValue: mischiefValue,
                    mischiefThreshold: MischiefThreshold,
                    remainingTime: remainingTime,
                    isPetGroomed: false
                );
                
                Assert.AreEqual(
                    GameManager.GameState.Playing, 
                    result,
                    $"Failed for mischiefValue={mischiefValue}, remainingTime={remainingTime}"
                );
            }
        }
        
        #endregion
        
        #region Property 11: Victory Condition - Timer Expiry
        
        /// <summary>
        /// Feature: pet-grooming-mvp, Property 11: Victory Condition - Timer Expiry
        /// 
        /// *For any* game state where timer reaches 0 and Pet is not groomed, 
        /// the game state shall be PetWin.
        /// 
        /// Validates: Requirements 6.3
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property11_TimerExpiredAndPetNotGroomed_ShouldResultInPetWin()
        {
            // Property: For all cases where time <= 0 and pet not groomed, result is PetWin
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float remainingTime = _random.Next(-100, 1); // Time <= 0
                int mischiefValue = _random.Next(0, MischiefThreshold); // Below threshold
                
                var result = GameManager.DetermineVictoryCondition(
                    mischiefValue: mischiefValue,
                    mischiefThreshold: MischiefThreshold,
                    remainingTime: remainingTime,
                    isPetGroomed: false
                );
                
                Assert.AreEqual(
                    GameManager.GameState.PetWin, 
                    result,
                    $"Failed for remainingTime={remainingTime}, mischiefValue={mischiefValue}"
                );
            }
        }
        
        /// <summary>
        /// Property 11 Edge Case: Timer exactly at 0
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property11_TimerExactlyZero_ShouldResultInPetWin()
        {
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                int mischiefValue = _random.Next(0, MischiefThreshold);
                
                var result = GameManager.DetermineVictoryCondition(
                    mischiefValue: mischiefValue,
                    mischiefThreshold: MischiefThreshold,
                    remainingTime: 0f,
                    isPetGroomed: false
                );
                
                Assert.AreEqual(
                    GameManager.GameState.PetWin, 
                    result,
                    $"Failed for mischiefValue={mischiefValue}"
                );
            }
        }
        
        /// <summary>
        /// Property 11 Negative: Timer not expired should continue playing
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property11_TimerNotExpired_ShouldContinuePlaying()
        {
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float remainingTime = _random.Next(1, 181); // Time > 0
                int mischiefValue = _random.Next(0, MischiefThreshold); // Below threshold
                
                var result = GameManager.DetermineVictoryCondition(
                    mischiefValue: mischiefValue,
                    mischiefThreshold: MischiefThreshold,
                    remainingTime: remainingTime,
                    isPetGroomed: false
                );
                
                Assert.AreEqual(
                    GameManager.GameState.Playing, 
                    result,
                    $"Failed for remainingTime={remainingTime}, mischiefValue={mischiefValue}"
                );
            }
        }
        
        #endregion
        
        #region Property 12: Victory Condition - Grooming Complete
        
        /// <summary>
        /// Feature: pet-grooming-mvp, Property 12: Victory Condition - Grooming Complete
        /// 
        /// *For any* game state where Pet grooming is complete, 
        /// the game state shall be GroomerWin.
        /// 
        /// Validates: Requirements 6.4
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property12_GroomingComplete_ShouldResultInGroomerWin()
        {
            // Property: For all cases where pet is groomed, result is GroomerWin
            // regardless of mischief value or remaining time
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                int mischiefValue = _random.Next(0, 1001);
                float remainingTime = _random.Next(-10, 181);
                
                var result = GameManager.DetermineVictoryCondition(
                    mischiefValue: mischiefValue,
                    mischiefThreshold: MischiefThreshold,
                    remainingTime: remainingTime,
                    isPetGroomed: true
                );
                
                Assert.AreEqual(
                    GameManager.GameState.GroomerWin, 
                    result,
                    $"Failed for mischiefValue={mischiefValue}, remainingTime={remainingTime}"
                );
            }
        }
        
        /// <summary>
        /// Property 12: Grooming complete takes priority over mischief threshold
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property12_GroomingComplete_TakesPriorityOverMischief()
        {
            // Even if mischief is at or above threshold, grooming complete wins
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                int mischiefValue = _random.Next(MischiefThreshold, MischiefThreshold + 1001);
                float remainingTime = _random.Next(1, 181);
                
                var result = GameManager.DetermineVictoryCondition(
                    mischiefValue: mischiefValue,
                    mischiefThreshold: MischiefThreshold,
                    remainingTime: remainingTime,
                    isPetGroomed: true
                );
                
                Assert.AreEqual(
                    GameManager.GameState.GroomerWin, 
                    result,
                    $"Failed for mischiefValue={mischiefValue}, remainingTime={remainingTime}"
                );
            }
        }
        
        /// <summary>
        /// Property 12: Grooming complete takes priority over timer expiry
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property12_GroomingComplete_TakesPriorityOverTimerExpiry()
        {
            // Even if timer is expired, grooming complete wins
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                int mischiefValue = _random.Next(0, MischiefThreshold);
                float remainingTime = _random.Next(-100, 1);
                
                var result = GameManager.DetermineVictoryCondition(
                    mischiefValue: mischiefValue,
                    mischiefThreshold: MischiefThreshold,
                    remainingTime: remainingTime,
                    isPetGroomed: true
                );
                
                Assert.AreEqual(
                    GameManager.GameState.GroomerWin, 
                    result,
                    $"Failed for mischiefValue={mischiefValue}, remainingTime={remainingTime}"
                );
            }
        }
        
        #endregion
        
        #region Combined Victory Condition Properties
        
        /// <summary>
        /// Combined property: Victory conditions are mutually exclusive and exhaustive
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void VictoryConditions_AreExhaustive_AlwaysReturnValidState()
        {
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                int mischiefValue = _random.Next(0, 1001);
                float remainingTime = _random.Next(-100, 201);
                bool isPetGroomed = _random.Next(0, 2) == 1;
                
                var result = GameManager.DetermineVictoryCondition(
                    mischiefValue: mischiefValue,
                    mischiefThreshold: MischiefThreshold,
                    remainingTime: remainingTime,
                    isPetGroomed: isPetGroomed
                );
                
                // Result must be one of the valid states
                bool isValidState = result == GameManager.GameState.Playing ||
                                   result == GameManager.GameState.GroomerWin ||
                                   result == GameManager.GameState.PetWin;
                
                Assert.IsTrue(
                    isValidState,
                    $"Invalid state {result} for mischiefValue={mischiefValue}, remainingTime={remainingTime}, isPetGroomed={isPetGroomed}"
                );
            }
        }
        
        #endregion
    }
}
