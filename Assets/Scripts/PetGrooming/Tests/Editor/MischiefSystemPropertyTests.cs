using System;
using NUnit.Framework;
using PetGrooming.Systems;

namespace PetGrooming.Tests.Editor
{
    /// <summary>
    /// Property-based tests for MischiefSystem mischief value calculations.
    /// Uses NUnit with manual random input generation (100+ iterations per property).
    /// 
    /// Feature: pet-grooming-mvp, Property 5: Mischief Value Calculation
    /// Validates: Requirements 2.4, 5.2, 5.3
    /// </summary>
    [TestFixture]
    public class MischiefSystemPropertyTests
    {
        private const int ShelfItemMischief = 50;
        private const int CleaningCartMischief = 80;
        private const int PropertyTestIterations = 100;
        private System.Random _random;
        
        [SetUp]
        public void SetUp()
        {
            _random = new System.Random(42); // Fixed seed for reproducibility
        }
        
        #region Property 5: Mischief Value Calculation
        
        /// <summary>
        /// Feature: pet-grooming-mvp, Property 5: Mischief Value Calculation
        /// 
        /// *For any* destructible object collision, the mischief value shall increase 
        /// by exactly the object's defined mischief value (50 for shelf items, 80 for cleaning carts).
        /// 
        /// Validates: Requirements 2.4, 5.2, 5.3
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property5_ShelfItemCollision_ShouldAddExactly50Points()
        {
            // Property: For all shelf item collisions, mischief increases by exactly 50
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                int initialValue = _random.Next(0, 1000);
                
                int result = MischiefSystem.CalculateMischiefValue(initialValue, ShelfItemMischief);
                int expected = initialValue + ShelfItemMischief;
                
                Assert.AreEqual(
                    expected, 
                    result,
                    $"Failed for initialValue={initialValue}. Expected {expected}, got {result}"
                );
            }
        }
        
        /// <summary>
        /// Feature: pet-grooming-mvp, Property 5: Mischief Value Calculation
        /// 
        /// *For any* cleaning cart collision, the mischief value shall increase 
        /// by exactly 80 points.
        /// 
        /// Validates: Requirements 2.4, 5.3
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property5_CleaningCartCollision_ShouldAddExactly80Points()
        {
            // Property: For all cleaning cart collisions, mischief increases by exactly 80
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                int initialValue = _random.Next(0, 1000);
                
                int result = MischiefSystem.CalculateMischiefValue(initialValue, CleaningCartMischief);
                int expected = initialValue + CleaningCartMischief;
                
                Assert.AreEqual(
                    expected, 
                    result,
                    $"Failed for initialValue={initialValue}. Expected {expected}, got {result}"
                );
            }
        }
        
        /// <summary>
        /// Property 5: Object type mapping returns correct mischief values
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property5_ObjectTypeMapping_ShouldReturnCorrectValues()
        {
            // Property: For all object types, the correct mischief value is returned
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Test shelf item
                int shelfResult = MischiefSystem.GetMischiefValueForObjectType(
                    DestructibleObjectType.ShelfItem,
                    ShelfItemMischief,
                    CleaningCartMischief
                );
                Assert.AreEqual(
                    ShelfItemMischief, 
                    shelfResult,
                    $"Shelf item should return {ShelfItemMischief}, got {shelfResult}"
                );
                
                // Test cleaning cart
                int cartResult = MischiefSystem.GetMischiefValueForObjectType(
                    DestructibleObjectType.CleaningCart,
                    ShelfItemMischief,
                    CleaningCartMischief
                );
                Assert.AreEqual(
                    CleaningCartMischief, 
                    cartResult,
                    $"Cleaning cart should return {CleaningCartMischief}, got {cartResult}"
                );
            }
        }
        
        /// <summary>
        /// Property 5: Mischief accumulation is additive
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property5_MischiefAccumulation_ShouldBeAdditive()
        {
            // Property: For any sequence of collisions, total mischief equals sum of individual values
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                int numShelfItems = _random.Next(0, 10);
                int numCleaningCarts = _random.Next(0, 10);
                
                int currentValue = 0;
                
                // Simulate collisions
                for (int j = 0; j < numShelfItems; j++)
                {
                    currentValue = MischiefSystem.CalculateMischiefValue(currentValue, ShelfItemMischief);
                }
                for (int j = 0; j < numCleaningCarts; j++)
                {
                    currentValue = MischiefSystem.CalculateMischiefValue(currentValue, CleaningCartMischief);
                }
                
                int expected = (numShelfItems * ShelfItemMischief) + (numCleaningCarts * CleaningCartMischief);
                
                Assert.AreEqual(
                    expected, 
                    currentValue,
                    $"Failed for {numShelfItems} shelf items and {numCleaningCarts} carts. Expected {expected}, got {currentValue}"
                );
            }
        }
        
        /// <summary>
        /// Property 5: Non-positive amounts should not change mischief value
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property5_NonPositiveAmount_ShouldNotChangeMischief()
        {
            // Property: For all non-positive amounts, mischief value remains unchanged
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                int initialValue = _random.Next(0, 1000);
                int nonPositiveAmount = _random.Next(-100, 1); // -100 to 0
                
                int result = MischiefSystem.CalculateMischiefValue(initialValue, nonPositiveAmount);
                
                Assert.AreEqual(
                    initialValue, 
                    result,
                    $"Failed for initialValue={initialValue}, amount={nonPositiveAmount}. Value should remain {initialValue}, got {result}"
                );
            }
        }
        
        /// <summary>
        /// Property 5: Threshold detection is accurate
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property5_ThresholdDetection_ShouldBeAccurate()
        {
            const int threshold = 500;
            
            // Property: For all values >= threshold, IsThresholdReached returns true
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                int valueAtOrAbove = _random.Next(threshold, threshold + 1000);
                int valueBelow = _random.Next(0, threshold);
                
                bool resultAbove = MischiefSystem.IsThresholdReached(valueAtOrAbove, threshold);
                bool resultBelow = MischiefSystem.IsThresholdReached(valueBelow, threshold);
                
                Assert.IsTrue(
                    resultAbove,
                    $"Value {valueAtOrAbove} should be at or above threshold {threshold}"
                );
                
                Assert.IsFalse(
                    resultBelow,
                    $"Value {valueBelow} should be below threshold {threshold}"
                );
            }
        }
        
        /// <summary>
        /// Property 5 Edge Case: Threshold exactly at boundary
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property5_ThresholdExactlyAtBoundary_ShouldBeReached()
        {
            const int threshold = 500;
            
            // Property: Value exactly at threshold should be considered reached
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                bool result = MischiefSystem.IsThresholdReached(threshold, threshold);
                
                Assert.IsTrue(
                    result,
                    $"Value exactly at threshold ({threshold}) should be considered reached"
                );
            }
        }
        
        #endregion
    }
}
