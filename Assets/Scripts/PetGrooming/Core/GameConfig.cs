using UnityEngine;

namespace PetGrooming.Core
{
    /// <summary>
    /// ScriptableObject containing all game configuration parameters.
    /// Requirements: 1.1, 2.5, 3.1, 5.5, 6.1
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "PetGrooming/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Match Settings")]
        [Tooltip("Duration of a match in seconds (3 minutes for MVP)")]
        public float MatchDuration = 180f;
        
        [Tooltip("Mischief value threshold for Pet victory")]
        public int MischiefThreshold = 500;

        [Header("Groomer Settings")]
        [Tooltip("Base movement speed of the Groomer in units per second")]
        public float GroomerMoveSpeed = 5f;
        
        [Tooltip("Distance within which capture can be initiated")]
        public float CaptureRange = 1.5f;
        
        [Tooltip("Speed multiplier when carrying a pet (15% reduction = 0.85)")]
        public float CarrySpeedMultiplier = 0.85f;

        [Header("Pet Settings")]
        [Tooltip("Base movement speed of the Pet in units per second")]
        public float PetMoveSpeed = 6f;
        
        [Tooltip("Distance at which Pet detects Groomer and enters flee state")]
        public float FleeDetectionRange = 8f;
        
        [Tooltip("Base chance for Pet to escape when captured (40%)")]
        public float BaseEscapeChance = 0.4f;
        
        [Tooltip("Distance Pet teleports away upon successful escape")]
        public float EscapeTeleportDistance = 3f;
        
        [Tooltip("Reduction in escape chance per completed grooming step")]
        public float EscapeChanceReductionPerStep = 0.1f;
        
        [Tooltip("Interval between struggle attempts in seconds")]
        public float StruggleInterval = 1f;

        [Header("Mischief Values")]
        [Tooltip("Mischief points added when Pet knocks over a shelf item")]
        public int ShelfItemMischief = 50;
        
        [Tooltip("Mischief points added when Pet knocks over a cleaning cart")]
        public int CleaningCartMischief = 80;
    }
}
