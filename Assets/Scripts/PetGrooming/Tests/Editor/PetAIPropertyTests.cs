using System;
using NUnit.Framework;
using UnityEngine;
using PetGrooming.AI;

namespace PetGrooming.Tests.Editor
{
    /// <summary>
    /// Property-based tests for PetAI behavior.
    /// Uses NUnit with manual random input generation (100+ iterations per property).
    /// 
    /// Feature: pet-grooming-mvp
    /// Validates: Requirements 2.1, 2.2, 2.3, 3.4
    /// </summary>
    [TestFixture]
    public class PetAIPropertyTests
    {
        private const int PropertyTestIterations = 100;
        private System.Random _random;
        
        // Default play area bounds for testing
        private readonly Vector3 _defaultPlayAreaMin = new Vector3(-20f, 0f, -20f);
        private readonly Vector3 _defaultPlayAreaMax = new Vector3(20f, 0f, 20f);
        
        // Default config values
        private const float DefaultFleeDistance = 8f;
        private const float DefaultEscapeTeleportDistance = 3f;
        private const float DefaultBaseEscapeChance = 0.4f;
        private const float DefaultEscapeChanceReductionPerStep = 0.1f;
        
        [SetUp]
        public void SetUp()
        {
            _random = new System.Random(42); // Fixed seed for reproducibility
        }
        
        #region Property 2: Pet Spawn Position Validity
        
        /// <summary>
        /// Feature: pet-grooming-mvp, Property 2: Pet Spawn Position Validity
        /// 
        /// *For any* pet spawn event, the spawned position shall be within 
        /// the defined play area bounds.
        /// 
        /// Validates: Requirements 2.1
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property2_SpawnPosition_ShouldBeWithinPlayAreaBounds()
        {
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Generate random play area bounds
                Vector3 min = new Vector3(
                    (float)(_random.NextDouble() * -50),
                    0f,
                    (float)(_random.NextDouble() * -50)
                );
                Vector3 max = new Vector3(
                    min.x + (float)(_random.NextDouble() * 100 + 10),
                    0f,
                    min.z + (float)(_random.NextDouble() * 100 + 10)
                );
                
                // Generate spawn position
                Vector3 spawnPos = PetAI.GenerateRandomPositionInBounds(min, max);
                
                // Verify position is within bounds
                bool isInBounds = PetAI.IsPositionInBounds(spawnPos, min, max);
                
                Assert.IsTrue(
                    isInBounds,
                    $"Spawn position {spawnPos} is outside bounds [{min}, {max}]"
                );
            }
        }
        
        /// <summary>
        /// Property 2: Spawn position X coordinate is within bounds
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property2_SpawnPositionX_ShouldBeWithinBounds()
        {
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float minX = (float)(_random.NextDouble() * -100);
                float maxX = minX + (float)(_random.NextDouble() * 200 + 1);
                
                Vector3 min = new Vector3(minX, 0f, -20f);
                Vector3 max = new Vector3(maxX, 0f, 20f);
                
                Vector3 spawnPos = PetAI.GenerateRandomPositionInBounds(min, max);
                
                Assert.GreaterOrEqual(
                    spawnPos.x, min.x,
                    $"Spawn X {spawnPos.x} is less than min {min.x}"
                );
                Assert.LessOrEqual(
                    spawnPos.x, max.x,
                    $"Spawn X {spawnPos.x} is greater than max {max.x}"
                );
            }
        }
        
        /// <summary>
        /// Property 2: Spawn position Z coordinate is within bounds
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property2_SpawnPositionZ_ShouldBeWithinBounds()
        {
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float minZ = (float)(_random.NextDouble() * -100);
                float maxZ = minZ + (float)(_random.NextDouble() * 200 + 1);
                
                Vector3 min = new Vector3(-20f, 0f, minZ);
                Vector3 max = new Vector3(20f, 0f, maxZ);
                
                Vector3 spawnPos = PetAI.GenerateRandomPositionInBounds(min, max);
                
                Assert.GreaterOrEqual(
                    spawnPos.z, min.z,
                    $"Spawn Z {spawnPos.z} is less than min {min.z}"
                );
                Assert.LessOrEqual(
                    spawnPos.z, max.z,
                    $"Spawn Z {spawnPos.z} is greater than max {max.z}"
                );
            }
        }
        
        #endregion
        
        #region Property 3: Flee State Trigger Distance
        
        /// <summary>
        /// Feature: pet-grooming-mvp, Property 3: Flee State Trigger Distance
        /// 
        /// *For any* distance between Groomer and Pet, if the distance is less than 
        /// or equal to 8 units and the Pet is not captured, the Pet shall be in Flee state.
        /// 
        /// Validates: Requirements 2.2
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property3_DistanceWithinFleeRange_ShouldTriggerFlee()
        {
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Generate random distance within flee range (0 to 8)
                float distance = (float)(_random.NextDouble() * DefaultFleeDistance);
                
                bool shouldFlee = PetAI.ShouldEnterFleeState(distance, DefaultFleeDistance);
                
                Assert.IsTrue(
                    shouldFlee,
                    $"Pet should flee at distance {distance} (flee distance: {DefaultFleeDistance})"
                );
            }
        }
        
        /// <summary>
        /// Property 3: Distance exactly at flee threshold should trigger flee
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property3_DistanceExactlyAtThreshold_ShouldTriggerFlee()
        {
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Generate random flee distances
                float fleeDistance = (float)(_random.NextDouble() * 20 + 1);
                
                bool shouldFlee = PetAI.ShouldEnterFleeState(fleeDistance, fleeDistance);
                
                Assert.IsTrue(
                    shouldFlee,
                    $"Pet should flee at exact threshold distance {fleeDistance}"
                );
            }
        }
        
        /// <summary>
        /// Property 3: Distance beyond flee range should not trigger flee
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property3_DistanceBeyondFleeRange_ShouldNotTriggerFlee()
        {
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Generate random distance beyond flee range
                float distance = DefaultFleeDistance + (float)(_random.NextDouble() * 100 + 0.01);
                
                bool shouldFlee = PetAI.ShouldEnterFleeState(distance, DefaultFleeDistance);
                
                Assert.IsFalse(
                    shouldFlee,
                    $"Pet should not flee at distance {distance} (flee distance: {DefaultFleeDistance})"
                );
            }
        }
        
        /// <summary>
        /// Property 3: Flee direction should be away from groomer
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property3_FleeDirection_ShouldBeAwayFromGroomer()
        {
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Generate random positions
                Vector3 petPos = new Vector3(
                    (float)(_random.NextDouble() * 40 - 20),
                    0f,
                    (float)(_random.NextDouble() * 40 - 20)
                );
                Vector3 groomerPos = new Vector3(
                    (float)(_random.NextDouble() * 40 - 20),
                    0f,
                    (float)(_random.NextDouble() * 40 - 20)
                );
                
                // Skip if positions are the same
                if (Vector3.Distance(petPos, groomerPos) < 0.001f) continue;
                
                Vector3 fleeDirection = PetAI.CalculateFleeDirection(petPos, groomerPos);
                Vector3 toGroomer = (groomerPos - petPos).normalized;
                toGroomer.y = 0f;
                toGroomer = toGroomer.normalized;
                
                // Flee direction should be opposite to groomer direction
                float dot = Vector3.Dot(fleeDirection, toGroomer);
                
                Assert.Less(
                    dot, 0f,
                    $"Flee direction {fleeDirection} should be away from groomer (dot: {dot})"
                );
            }
        }
        
        #endregion
        
        #region Property 4: Wander Target Bounds
        
        /// <summary>
        /// Feature: pet-grooming-mvp, Property 4: Wander Target Bounds
        /// 
        /// *For any* wander target generated by the Pet AI in Idle state, 
        /// the target position shall be within the play area bounds.
        /// 
        /// Validates: Requirements 2.3
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property4_WanderTarget_ShouldBeWithinPlayAreaBounds()
        {
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Generate random current position within bounds
                Vector3 currentPos = new Vector3(
                    (float)(_random.NextDouble() * 40 - 20),
                    0f,
                    (float)(_random.NextDouble() * 40 - 20)
                );
                
                float wanderRadius = (float)(_random.NextDouble() * 15 + 1);
                
                Vector3 wanderTarget = PetAI.GenerateWanderTarget(
                    currentPos, 
                    wanderRadius, 
                    _defaultPlayAreaMin, 
                    _defaultPlayAreaMax
                );
                
                bool isInBounds = PetAI.IsPositionInBounds(
                    wanderTarget, 
                    _defaultPlayAreaMin, 
                    _defaultPlayAreaMax
                );
                
                Assert.IsTrue(
                    isInBounds,
                    $"Wander target {wanderTarget} is outside bounds [{_defaultPlayAreaMin}, {_defaultPlayAreaMax}]"
                );
            }
        }
        
        /// <summary>
        /// Property 4: Wander target from edge position should still be within bounds
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property4_WanderTargetFromEdge_ShouldBeWithinBounds()
        {
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Position at edge of play area
                Vector3 edgePos = new Vector3(
                    _random.Next(0, 2) == 0 ? _defaultPlayAreaMin.x : _defaultPlayAreaMax.x,
                    0f,
                    _random.Next(0, 2) == 0 ? _defaultPlayAreaMin.z : _defaultPlayAreaMax.z
                );
                
                float wanderRadius = (float)(_random.NextDouble() * 20 + 5);
                
                Vector3 wanderTarget = PetAI.GenerateWanderTarget(
                    edgePos, 
                    wanderRadius, 
                    _defaultPlayAreaMin, 
                    _defaultPlayAreaMax
                );
                
                bool isInBounds = PetAI.IsPositionInBounds(
                    wanderTarget, 
                    _defaultPlayAreaMin, 
                    _defaultPlayAreaMax
                );
                
                Assert.IsTrue(
                    isInBounds,
                    $"Wander target {wanderTarget} from edge {edgePos} is outside bounds"
                );
            }
        }
        
        #endregion
        
        #region Property 7: Escape Teleport Distance
        
        /// <summary>
        /// Feature: pet-grooming-mvp, Property 7: Escape Teleport Distance
        /// 
        /// *For any* successful escape attempt, the Pet's new position shall be 
        /// exactly 3 units away from the Groomer's position.
        /// 
        /// Validates: Requirements 3.4
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property7_EscapePosition_ShouldBeExactDistanceFromGroomer()
        {
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Generate random positions
                Vector3 petPos = new Vector3(
                    (float)(_random.NextDouble() * 40 - 20),
                    0f,
                    (float)(_random.NextDouble() * 40 - 20)
                );
                Vector3 groomerPos = new Vector3(
                    (float)(_random.NextDouble() * 40 - 20),
                    0f,
                    (float)(_random.NextDouble() * 40 - 20)
                );
                
                // Skip if positions are the same
                if (Vector3.Distance(petPos, groomerPos) < 0.001f) continue;
                
                Vector3 escapePos = PetAI.CalculateEscapePosition(
                    petPos, 
                    groomerPos, 
                    DefaultEscapeTeleportDistance
                );
                
                float distanceFromGroomer = PetAI.CalculateHorizontalDistance(escapePos, groomerPos);
                
                Assert.AreEqual(
                    DefaultEscapeTeleportDistance, 
                    distanceFromGroomer, 
                    0.001f,
                    $"Escape position should be exactly {DefaultEscapeTeleportDistance} units from groomer, was {distanceFromGroomer}"
                );
            }
        }
        
        /// <summary>
        /// Property 7: Escape position should be in direction away from groomer
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property7_EscapePosition_ShouldBeAwayFromGroomer()
        {
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Generate random positions where pet is closer to groomer
                Vector3 groomerPos = new Vector3(
                    (float)(_random.NextDouble() * 20 - 10),
                    0f,
                    (float)(_random.NextDouble() * 20 - 10)
                );
                
                // Pet is within capture range
                Vector3 offset = new Vector3(
                    (float)(_random.NextDouble() * 2 - 1),
                    0f,
                    (float)(_random.NextDouble() * 2 - 1)
                ).normalized * 1.5f;
                
                Vector3 petPos = groomerPos + offset;
                
                // Skip if positions are the same
                if (Vector3.Distance(petPos, groomerPos) < 0.001f) continue;
                
                Vector3 escapePos = PetAI.CalculateEscapePosition(
                    petPos, 
                    groomerPos, 
                    DefaultEscapeTeleportDistance
                );
                
                // Escape position should be further from groomer than pet was
                float originalDistance = Vector3.Distance(petPos, groomerPos);
                float escapeDistance = Vector3.Distance(escapePos, groomerPos);
                
                Assert.Greater(
                    escapeDistance, 
                    originalDistance,
                    $"Escape position should be further from groomer. Original: {originalDistance}, Escape: {escapeDistance}"
                );
            }
        }
        
        /// <summary>
        /// Property 7: Escape teleport distance should work with various teleport distances
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property7_EscapePosition_ShouldWorkWithVariousTeleportDistances()
        {
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Generate random teleport distance
                float teleportDistance = (float)(_random.NextDouble() * 10 + 1);
                
                Vector3 petPos = new Vector3(
                    (float)(_random.NextDouble() * 40 - 20),
                    0f,
                    (float)(_random.NextDouble() * 40 - 20)
                );
                Vector3 groomerPos = new Vector3(
                    (float)(_random.NextDouble() * 40 - 20),
                    0f,
                    (float)(_random.NextDouble() * 40 - 20)
                );
                
                // Skip if positions are the same
                if (Vector3.Distance(petPos, groomerPos) < 0.001f) continue;
                
                Vector3 escapePos = PetAI.CalculateEscapePosition(
                    petPos, 
                    groomerPos, 
                    teleportDistance
                );
                
                float distanceFromGroomer = PetAI.CalculateHorizontalDistance(escapePos, groomerPos);
                
                Assert.AreEqual(
                    teleportDistance, 
                    distanceFromGroomer, 
                    0.001f,
                    $"Escape position should be exactly {teleportDistance} units from groomer, was {distanceFromGroomer}"
                );
            }
        }
        
        #endregion
        
        #region Additional Property Tests: Escape Chance Calculation
        
        /// <summary>
        /// Property 9 (Related): Escape chance decreases with grooming steps
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void EscapeChance_ShouldDecreaseWithGroomingSteps()
        {
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float baseChance = (float)(_random.NextDouble() * 0.5 + 0.2);
                float reductionPerStep = (float)(_random.NextDouble() * 0.15 + 0.05);
                
                float chance0 = PetAI.CalculateEscapeChance(0, baseChance, reductionPerStep);
                float chance1 = PetAI.CalculateEscapeChance(1, baseChance, reductionPerStep);
                float chance2 = PetAI.CalculateEscapeChance(2, baseChance, reductionPerStep);
                float chance3 = PetAI.CalculateEscapeChance(3, baseChance, reductionPerStep);
                
                Assert.GreaterOrEqual(chance0, chance1, "Chance should decrease after step 1");
                Assert.GreaterOrEqual(chance1, chance2, "Chance should decrease after step 2");
                Assert.GreaterOrEqual(chance2, chance3, "Chance should decrease after step 3");
            }
        }
        
        /// <summary>
        /// Property 9 (Related): Escape chance should never be negative
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void EscapeChance_ShouldNeverBeNegative()
        {
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                int steps = _random.Next(0, 20);
                float baseChance = (float)(_random.NextDouble());
                float reductionPerStep = (float)(_random.NextDouble() * 0.5);
                
                float chance = PetAI.CalculateEscapeChance(steps, baseChance, reductionPerStep);
                
                Assert.GreaterOrEqual(
                    chance, 
                    0f,
                    $"Escape chance should never be negative. Steps: {steps}, Base: {baseChance}, Reduction: {reductionPerStep}"
                );
            }
        }
        
        /// <summary>
        /// Property 9 (Related): Escape chance formula is correct
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void EscapeChance_ShouldFollowFormula()
        {
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                int steps = _random.Next(0, 4);
                
                float expectedChance = Mathf.Max(0f, DefaultBaseEscapeChance - steps * DefaultEscapeChanceReductionPerStep);
                float actualChance = PetAI.CalculateEscapeChance(steps, DefaultBaseEscapeChance, DefaultEscapeChanceReductionPerStep);
                
                Assert.AreEqual(
                    expectedChance, 
                    actualChance, 
                    0.001f,
                    $"Escape chance formula incorrect for {steps} steps"
                );
            }
        }
        
        #endregion
        
        #region Bounds Clamping Tests
        
        /// <summary>
        /// ClampToPlayArea should always return position within bounds
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void ClampToPlayArea_ShouldAlwaysReturnPositionWithinBounds()
        {
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Generate random position (potentially outside bounds)
                Vector3 position = new Vector3(
                    (float)(_random.NextDouble() * 200 - 100),
                    0f,
                    (float)(_random.NextDouble() * 200 - 100)
                );
                
                Vector3 clamped = PetAI.ClampToPlayArea(position, _defaultPlayAreaMin, _defaultPlayAreaMax);
                
                bool isInBounds = PetAI.IsPositionInBounds(clamped, _defaultPlayAreaMin, _defaultPlayAreaMax);
                
                Assert.IsTrue(
                    isInBounds,
                    $"Clamped position {clamped} should be within bounds [{_defaultPlayAreaMin}, {_defaultPlayAreaMax}]"
                );
            }
        }
        
        #endregion
    }
}
