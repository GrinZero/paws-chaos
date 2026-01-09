using System;
using UnityEngine;
using PetGrooming.Core;
using PetGrooming.AI;

namespace PetGrooming.Systems.Skills
{
    /// <summary>
    /// Steal Tool skill for Dog pets.
    /// Removes one tool from the nearest Grooming Station, adding 1 step to the grooming process.
    /// Requirements: 5.5
    /// </summary>
    public class StealToolSkill : SkillBase
    {
        #region Serialized Fields
        [Header("Steal Tool Settings")]
        [Tooltip("Range to detect nearest Grooming Station")]
        public float DetectionRange = 5f;
        
        [Tooltip("Additional grooming steps added when tool is stolen")]
        public int ExtraStepsAdded = 1;
        
        [Tooltip("Visual effect when stealing tool")]
        public ParticleSystem StealEffect;
        
        [Header("Configuration")]
        [Tooltip("Phase 2 game configuration")]
        public Phase2GameConfig GameConfig;
        #endregion

        #region Private Fields
        private PetAI _ownerPet;
        #endregion

        #region Events
        /// <summary>
        /// Fired when a tool is successfully stolen.
        /// </summary>
        public event Action<GroomingStation, int> OnToolStolen;
        
        /// <summary>
        /// Fired when no station is in range.
        /// </summary>
        public event Action OnNoStationInRange;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            
            SkillName = "Steal Tool";
            
            // Apply config values if available
            if (GameConfig != null)
            {
                Cooldown = GameConfig.StealToolCooldown;
                DetectionRange = GameConfig.StealToolRange;
                ExtraStepsAdded = GameConfig.StealToolExtraSteps;
            }
            else
            {
                // Default cooldown: 12 seconds (Requirement 5.5)
                Cooldown = 12f;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Sets the owner pet for this skill.
        /// </summary>
        public void SetOwner(PetAI owner)
        {
            _ownerPet = owner;
        }

        /// <summary>
        /// Checks if the skill can be activated.
        /// Requires a Grooming Station to be in range.
        /// </summary>
        public override bool CanActivate()
        {
            if (!base.CanActivate()) return false;
            
            // Check if there's a station in range
            GroomingStation nearestStation = FindNearestGroomingStation();
            return nearestStation != null;
        }

        /// <summary>
        /// Activates the Steal Tool skill.
        /// Requirement 5.5: Removes one tool from nearest Grooming Station, adding 1 step to grooming process.
        /// </summary>
        public override void Activate()
        {
            base.Activate();
            PerformSteal();
        }
        #endregion

        #region Private Methods
        private void PerformSteal()
        {
            GroomingStation nearestStation = FindNearestGroomingStation();
            
            if (nearestStation == null)
            {
                OnNoStationInRange?.Invoke();
                Debug.Log("[StealToolSkill] No Grooming Station in range!");
                return;
            }
            
            // Play visual effect
            if (StealEffect != null)
            {
                StealEffect.transform.position = nearestStation.transform.position;
                StealEffect.Play();
            }
            
            // Add extra grooming steps to the station
            AddExtraGroomingSteps(nearestStation, ExtraStepsAdded);
            
            OnToolStolen?.Invoke(nearestStation, ExtraStepsAdded);
            
            Debug.Log($"[StealToolSkill] Stole tool from {nearestStation.name}! Added {ExtraStepsAdded} extra step(s).");
        }

        private GroomingStation FindNearestGroomingStation()
        {
            Vector3 searchOrigin = _ownerPet != null ? _ownerPet.transform.position : transform.position;
            
            GroomingStation[] stations = FindObjectsOfType<GroomingStation>();
            GroomingStation nearest = null;
            float nearestDistance = float.MaxValue;
            
            foreach (GroomingStation station in stations)
            {
                float distance = Vector3.Distance(searchOrigin, station.transform.position);
                
                if (distance <= DetectionRange && distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = station;
                }
            }
            
            return nearest;
        }

        private void AddExtraGroomingSteps(GroomingStation station, int extraSteps)
        {
            if (station == null) return;
            
            // Get the grooming system from the station
            GroomingSystem groomingSystem = station.GroomingSystem;
            if (groomingSystem != null)
            {
                // The grooming system tracks steps - we need to add extra required steps
                // This is handled by the GroomingStation's extended functionality
                AddExtraStepsToStation(station, extraSteps);
            }
        }

        /// <summary>
        /// Adds extra grooming steps to a station.
        /// This modifies the station's grooming requirements.
        /// </summary>
        private void AddExtraStepsToStation(GroomingStation station, int extraSteps)
        {
            // The station needs to track extra steps required
            // This would typically be implemented in the GroomingStation class
            // For now, we'll use a component-based approach
            
            StationExtraSteps extraStepsComponent = station.GetComponent<StationExtraSteps>();
            if (extraStepsComponent == null)
            {
                extraStepsComponent = station.gameObject.AddComponent<StationExtraSteps>();
            }
            
            extraStepsComponent.AddExtraSteps(extraSteps);
        }
        #endregion

        #region Static Methods (Testable)
        /// <summary>
        /// Calculates the total grooming steps after stealing.
        /// Property 14: Steal Tool Increases Grooming Steps
        /// Requirement 5.5: Adds 1 step to grooming process
        /// </summary>
        /// <param name="baseSteps">Base number of grooming steps (3)</param>
        /// <param name="extraSteps">Extra steps added by stealing</param>
        /// <returns>Total grooming steps required</returns>
        public static int CalculateTotalGroomingSteps(int baseSteps, int extraSteps)
        {
            return baseSteps + extraSteps;
        }

        /// <summary>
        /// Validates that stealing adds the correct number of steps.
        /// Property 14: Steal Tool Increases Grooming Steps
        /// Requirement 5.5: Adds 1 step to grooming process
        /// </summary>
        /// <param name="stepsAdded">Number of steps added</param>
        /// <returns>True if correct number of steps added</returns>
        public static bool ValidateStepsAdded(int stepsAdded)
        {
            const int RequiredStepsAdded = 1;
            return stepsAdded == RequiredStepsAdded;
        }

        /// <summary>
        /// Validates the Steal Tool cooldown matches requirements.
        /// Requirement 5.5: 12 second cooldown
        /// </summary>
        /// <param name="cooldown">The cooldown to validate</param>
        /// <returns>True if cooldown matches requirement</returns>
        public static bool ValidateCooldown(float cooldown)
        {
            const float RequiredCooldown = 12f;
            const float Tolerance = 0.001f;
            return Mathf.Abs(cooldown - RequiredCooldown) < Tolerance;
        }

        /// <summary>
        /// Checks if a Grooming Station is within steal range.
        /// </summary>
        /// <param name="petPosition">Position of the pet</param>
        /// <param name="stationPosition">Position of the station</param>
        /// <param name="range">Detection range</param>
        /// <returns>True if within range</returns>
        public static bool IsStationInRange(Vector3 petPosition, Vector3 stationPosition, float range)
        {
            float distance = Vector3.Distance(petPosition, stationPosition);
            return distance <= range;
        }

        /// <summary>
        /// Finds the nearest station from a list within range.
        /// </summary>
        /// <param name="petPosition">Position of the pet</param>
        /// <param name="stationPositions">Array of station positions</param>
        /// <param name="range">Detection range</param>
        /// <returns>Index of nearest station, or -1 if none in range</returns>
        public static int FindNearestStationIndex(Vector3 petPosition, Vector3[] stationPositions, float range)
        {
            int nearestIndex = -1;
            float nearestDistance = float.MaxValue;
            
            for (int i = 0; i < stationPositions.Length; i++)
            {
                float distance = Vector3.Distance(petPosition, stationPositions[i]);
                
                if (distance <= range && distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestIndex = i;
                }
            }
            
            return nearestIndex;
        }
        #endregion

        #region Editor Support
#if UNITY_EDITOR
        /// <summary>
        /// Sets config for testing purposes.
        /// </summary>
        public void SetConfigForTesting(Phase2GameConfig config)
        {
            GameConfig = config;
            if (config != null)
            {
                Cooldown = config.StealToolCooldown;
                DetectionRange = config.StealToolRange;
                ExtraStepsAdded = config.StealToolExtraSteps;
            }
        }

        /// <summary>
        /// Sets the owner for testing purposes.
        /// </summary>
        public void SetOwnerForTesting(PetAI owner)
        {
            _ownerPet = owner;
        }
#endif

        private void OnDrawGizmosSelected()
        {
            // Draw detection range
            Gizmos.color = new Color(0.5f, 0f, 1f, 0.3f);
            Gizmos.DrawSphere(transform.position, DetectionRange);
            
            Gizmos.color = new Color(0.5f, 0f, 1f, 1f);
            Gizmos.DrawWireSphere(transform.position, DetectionRange);
        }
        #endregion
    }

    /// <summary>
    /// Helper component to track extra grooming steps added to a station.
    /// </summary>
    public class StationExtraSteps : MonoBehaviour
    {
        /// <summary>
        /// Number of extra steps added to this station.
        /// </summary>
        public int ExtraSteps { get; private set; }

        /// <summary>
        /// Event fired when extra steps are added.
        /// </summary>
        public event Action<int> OnExtraStepsAdded;

        /// <summary>
        /// Adds extra grooming steps to this station.
        /// </summary>
        /// <param name="steps">Number of steps to add</param>
        public void AddExtraSteps(int steps)
        {
            ExtraSteps += steps;
            OnExtraStepsAdded?.Invoke(steps);
            Debug.Log($"[StationExtraSteps] Added {steps} extra steps. Total extra: {ExtraSteps}");
        }

        /// <summary>
        /// Resets the extra steps count.
        /// </summary>
        public void Reset()
        {
            ExtraSteps = 0;
        }

        /// <summary>
        /// Gets the total grooming steps required (base + extra).
        /// </summary>
        /// <param name="baseSteps">Base number of steps (default 3)</param>
        /// <returns>Total steps required</returns>
        public int GetTotalStepsRequired(int baseSteps = 3)
        {
            return baseSteps + ExtraSteps;
        }
    }
}
