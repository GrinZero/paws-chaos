using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using PetGrooming.Core;

namespace PetGrooming.AI
{
    /// <summary>
    /// AI controller for the pet character using a state machine.
    /// Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 3.2, 3.3, 3.4, 3.7, 4.4, 8.4
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class PetAI : MonoBehaviour, IEffectReceiver
    {
        #region Enums
        
        /// <summary>
        /// Represents the type of pet (Cat or Dog).
        /// Requirements: 2.2, 2.3, 2.4
        /// </summary>
        public enum PetType
        {
            Cat,
            Dog
        }
        
        /// <summary>
        /// Represents the current state of the pet.
        /// </summary>
        public enum PetState
        {
            Idle,
            Wandering,
            Fleeing,
            Captured,
            BeingGroomed
        }
        
        #endregion

        #region Serialized Fields
        
        [Header("Configuration")]
        [SerializeField] private GameConfig _gameConfig;
        [SerializeField] private Phase2GameConfig _phase2Config;
        
        [Header("Pet Type")]
        [Tooltip("The type of this pet (Cat or Dog)")]
        [SerializeField] private PetType _petType = PetType.Cat;
        
        [Header("Play Area Bounds")]
        [SerializeField] private Vector3 _playAreaMin = new Vector3(-20f, 0f, -20f);
        [SerializeField] private Vector3 _playAreaMax = new Vector3(20f, 0f, 20f);
        
        [Header("Wander Settings")]
        [SerializeField] private float _wanderWaitTime = 2f;
        [SerializeField] private float _wanderRadius = 10f;
        
        [Header("Visual")]
        [SerializeField] private Renderer _petRenderer;
        
        #endregion

        #region Private Fields
        
        private NavMeshAgent _navAgent;
        private Transform _groomerTransform;
        private float _wanderTimer;
        private float _struggleTimer;
        private int _groomingStepsCompleted;
        
        // Status effect tracking
        private List<SkillEffectData> _activeEffects = new List<SkillEffectData>();
        private float _originalSpeed;
        private Color _originalColor;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// Current state of the pet.
        /// </summary>
        public PetState CurrentState { get; private set; } = PetState.Idle;
        
        /// <summary>
        /// The type of this pet (Cat or Dog).
        /// Requirements: 2.2, 2.3, 2.4
        /// </summary>
        public PetType Type => _petType;
        
        /// <summary>
        /// Whether this pet has completed grooming.
        /// </summary>
        public bool IsGroomed { get; private set; }
        
        /// <summary>
        /// Whether this pet is currently stored in a pet cage.
        /// Requirement 8.6: Caged pets don't contribute to mischief.
        /// </summary>
        public bool IsCaged { get; private set; }
        
        /// <summary>
        /// Collision radius based on pet type.
        /// Requirement 2.2: Dog has larger collision radius (1.0) compared to Cat (0.5).
        /// </summary>
        public float CollisionRadius => GetCollisionRadiusForType(_petType, _phase2Config);
        
        /// <summary>
        /// Knockback force based on pet type.
        /// Requirement 2.3: Dog applies stronger knockback force than Cat.
        /// </summary>
        public float KnockbackForce => GetKnockbackForceForType(_petType, _phase2Config);
        
        /// <summary>
        /// Movement speed of the pet.
        /// Requirement 2.5: Pet moves at 6 units per second (Cat), Dog moves at 5 units per second.
        /// </summary>
        public float MoveSpeed
        {
            get
            {
                if (_petType == PetType.Dog && _phase2Config != null)
                {
                    return _phase2Config.DogMoveSpeed;
                }
                return _gameConfig != null ? _gameConfig.PetMoveSpeed : 6f;
            }
        }
        
        /// <summary>
        /// Distance at which pet detects groomer and flees.
        /// Requirement 2.2: Pet enters flee state when groomer is within 8 units.
        /// </summary>
        public float FleeDistance => _gameConfig != null ? _gameConfig.FleeDetectionRange : 8f;
        
        /// <summary>
        /// Distance pet teleports upon escape.
        /// Requirement 3.4: Pet teleports 3 units away on escape.
        /// </summary>
        public float EscapeTeleportDistance => _gameConfig != null ? _gameConfig.EscapeTeleportDistance : 3f;
        
        /// <summary>
        /// Base chance for pet to escape when captured.
        /// Requirement 2.4: Cat has 40% base escape chance, Dog has 30%.
        /// </summary>
        public float BaseEscapeChance => GetBaseEscapeChanceForType(_petType, _phase2Config, _gameConfig);
        
        /// <summary>
        /// Interval between struggle attempts.
        /// </summary>
        public float StruggleInterval => _gameConfig != null ? _gameConfig.StruggleInterval : 1f;
        
        /// <summary>
        /// Reduction in escape chance per grooming step.
        /// </summary>
        public float EscapeChanceReductionPerStep => _gameConfig != null ? _gameConfig.EscapeChanceReductionPerStep : 0.1f;
        
        /// <summary>
        /// Reference to the game configuration.
        /// </summary>
        public GameConfig Config => _gameConfig;
        
        /// <summary>
        /// Reference to the Phase 2 game configuration.
        /// </summary>
        public Phase2GameConfig Phase2Config => _phase2Config;
        
        /// <summary>
        /// Play area minimum bounds.
        /// </summary>
        public Vector3 PlayAreaMin => _playAreaMin;
        
        /// <summary>
        /// Play area maximum bounds.
        /// </summary>
        public Vector3 PlayAreaMax => _playAreaMax;
        
        /// <summary>
        /// Number of grooming steps completed (affects escape chance).
        /// </summary>
        public int GroomingStepsCompleted => _groomingStepsCompleted;
        
        // Status Effect Properties
        
        /// <summary>
        /// Whether the pet is currently slowed.
        /// Requirement 3.2: Capture Net slows pet by 50%.
        /// </summary>
        public bool IsSlowed => HasEffect(SkillEffectType.Slow);
        
        /// <summary>
        /// Whether the pet is currently stunned.
        /// Requirement 3.7: Calming Spray stuns pets.
        /// </summary>
        public bool IsStunned => HasEffect(SkillEffectType.Stun);
        
        /// <summary>
        /// Whether the pet is currently invisible.
        /// Requirement 4.4: Hide In Gap makes cat invisible.
        /// </summary>
        public bool IsInvisible => HasEffect(SkillEffectType.Invisible);
        
        /// <summary>
        /// Whether the pet is currently invulnerable.
        /// Requirement 8.4: Pet released from cage has 3 seconds invulnerability.
        /// </summary>
        public bool IsInvulnerable => HasEffect(SkillEffectType.Invulnerable);
        
        /// <summary>
        /// Current opacity of the pet (for invisibility effects).
        /// </summary>
        public float CurrentOpacity { get; private set; } = 1f;
        
        /// <summary>
        /// List of currently active effects on this pet.
        /// </summary>
        public IReadOnlyList<SkillEffectData> ActiveEffects => _activeEffects.AsReadOnly();
        
        #endregion

        #region Events
        
        /// <summary>
        /// Fired when the pet escapes from capture.
        /// </summary>
        public event Action OnEscaped;
        
        /// <summary>
        /// Fired when grooming is complete.
        /// </summary>
        public event Action OnGroomingComplete;
        
        /// <summary>
        /// Fired when the pet state changes.
        /// </summary>
        public event Action<PetState> OnStateChanged;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            _navAgent = GetComponent<NavMeshAgent>();
            
            if (_gameConfig == null)
            {
                Debug.LogError("[PetAI] GameConfig is not assigned!");
            }
            
            // Cache original values for effect restoration
            if (_petRenderer != null)
            {
                _originalColor = _petRenderer.material.color;
            }
        }
        
        private void Start()
        {
            InitializeNavAgent();
            _originalSpeed = _navAgent.speed;
        }
        
        private void Update()
        {
            if (GameManager.Instance != null && 
                GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            {
                return;
            }
            
            UpdateState();
            UpdateActiveEffects();
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Sets the groomer transform for flee calculations.
        /// </summary>
        public void SetGroomerTransform(Transform groomer)
        {
            _groomerTransform = groomer;
        }
        
        /// <summary>
        /// Sets the pet state.
        /// </summary>
        public void SetState(PetState newState)
        {
            if (CurrentState == newState) return;
            
            var previousState = CurrentState;
            CurrentState = newState;
            
            OnStateEnter(newState, previousState);
            OnStateChanged?.Invoke(newState);
            
            // Debug.Log($"[PetAI] State changed: {previousState} → {newState}");
        }
        
        /// <summary>
        /// Called when the pet is captured by the groomer.
        /// </summary>
        public void OnCaptured(Transform groomer)
        {
            _groomerTransform = groomer;
            _groomingStepsCompleted = 0;
            SetState(PetState.Captured);
        }
        
        /// <summary>
        /// Called when grooming starts.
        /// </summary>
        public void OnGroomingStarted()
        {
            SetState(PetState.BeingGroomed);
        }
        
        /// <summary>
        /// Called when a grooming step is completed.
        /// </summary>
        public void OnGroomingStepCompleted()
        {
            _groomingStepsCompleted++;
            
            if (_groomingStepsCompleted >= 3)
            {
                OnGroomingComplete?.Invoke();
            }
        }
        
        /// <summary>
        /// Attempts to escape from capture.
        /// Requirement 3.3: 40% base escape chance, reduced by 10% per grooming step.
        /// </summary>
        /// <returns>True if escape was successful.</returns>
        public bool TryEscape()
        {
            float escapeChance = CalculateEscapeChance(_groomingStepsCompleted, BaseEscapeChance, EscapeChanceReductionPerStep);
            float roll = UnityEngine.Random.value;
            
            bool escaped = roll < escapeChance;
            
            if (escaped)
            {
                PerformEscape();
            }
            
            return escaped;
        }
        
        /// <summary>
        /// Spawns the pet at a random valid position.
        /// Requirement 2.1: Pet spawns at random valid position.
        /// </summary>
        public void SpawnAtRandomPosition()
        {
            Vector3 randomPos = GenerateRandomPositionInBounds(_playAreaMin, _playAreaMax);
            
            if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
            }
            else
            {
                transform.position = randomPos;
            }
            
            SetState(PetState.Idle);
        }
        
        /// <summary>
        /// Marks this pet as groomed.
        /// </summary>
        public void MarkAsGroomed()
        {
            IsGroomed = true;
        }
        
        /// <summary>
        /// Sets the caged state of this pet.
        /// Requirement 8.6: Caged pets don't contribute to mischief.
        /// </summary>
        /// <param name="caged">Whether the pet is caged.</param>
        public void SetCaged(bool caged)
        {
            IsCaged = caged;
            Debug.Log($"[PetAI] Pet caged state set to: {caged}");
        }
        
        #endregion

        #region IEffectReceiver Implementation
        
        /// <summary>
        /// Applies a skill effect to this pet.
        /// Requirements: 3.2, 3.7, 4.4, 8.4
        /// </summary>
        /// <param name="effect">The effect data to apply</param>
        public void ApplyEffect(SkillEffectData effect)
        {
            if (effect == null) return;
            
            // Check for invulnerability - cannot apply effects while invulnerable (except invulnerability itself)
            if (IsInvulnerable && effect.Type != SkillEffectType.Invulnerable)
            {
                Debug.Log($"[PetAI] Cannot apply {effect.Type} - pet is invulnerable");
                return;
            }
            
            // Remove existing effect of same type (no stacking)
            RemoveEffect(effect.Type);
            
            // Add the new effect
            _activeEffects.Add(effect.Clone());
            
            // Apply immediate effect
            ApplyEffectImmediate(effect);
            
            Debug.Log($"[PetAI] Applied effect: {effect}");
        }
        
        /// <summary>
        /// Removes a specific effect type from this pet.
        /// </summary>
        /// <param name="effectType">The type of effect to remove</param>
        public void RemoveEffect(SkillEffectType effectType)
        {
            var effectToRemove = _activeEffects.Find(e => e.Type == effectType);
            if (effectToRemove != null)
            {
                _activeEffects.Remove(effectToRemove);
                OnEffectRemoved(effectType);
                Debug.Log($"[PetAI] Removed effect: {effectType}");
            }
        }
        
        /// <summary>
        /// Checks if this pet currently has a specific effect type.
        /// </summary>
        /// <param name="effectType">The type of effect to check</param>
        /// <returns>True if the effect is active</returns>
        public bool HasEffect(SkillEffectType effectType)
        {
            return _activeEffects.Exists(e => e.Type == effectType && !e.IsExpired);
        }
        
        /// <summary>
        /// Clears all active effects from this pet.
        /// </summary>
        public void ClearAllEffects()
        {
            foreach (var effect in _activeEffects)
            {
                OnEffectRemoved(effect.Type);
            }
            _activeEffects.Clear();
            Debug.Log("[PetAI] Cleared all effects");
        }
        
        #endregion

        #region Status Effect Methods
        
        /// <summary>
        /// Applies a slow effect to this pet.
        /// Requirement 3.2: Capture Net slows pet by 50% for 3 seconds.
        /// </summary>
        /// <param name="slowAmount">Speed reduction (0.5 = 50% slower)</param>
        /// <param name="duration">Duration in seconds</param>
        public void ApplySlow(float slowAmount, float duration)
        {
            var effect = SkillEffectData.CreateSlow(slowAmount, duration, "ApplySlow");
            ApplyEffect(effect);
        }
        
        /// <summary>
        /// Applies a stun effect to this pet.
        /// Requirement 3.7: Calming Spray stuns pets for 1 second.
        /// </summary>
        /// <param name="duration">Duration in seconds</param>
        public void ApplyStun(float duration)
        {
            var effect = SkillEffectData.CreateStun(duration, "ApplyStun");
            ApplyEffect(effect);
        }
        
        /// <summary>
        /// Sets the invisibility state of this pet.
        /// Requirement 4.4: Hide In Gap makes cat invisible while stationary.
        /// Requirement 4.5: Moving while using Hide In Gap makes cat semi-transparent (50% opacity).
        /// </summary>
        /// <param name="invisible">Whether to become invisible</param>
        /// <param name="opacity">Opacity level when visible (0 = fully invisible, 1 = fully visible)</param>
        /// <param name="duration">Duration in seconds (0 for indefinite)</param>
        public void SetInvisible(bool invisible, float opacity = 0f, float duration = 0f)
        {
            if (invisible)
            {
                var effect = SkillEffectData.CreateInvisible(opacity, duration > 0 ? duration : float.MaxValue, "SetInvisible");
                ApplyEffect(effect);
            }
            else
            {
                RemoveEffect(SkillEffectType.Invisible);
            }
        }
        
        /// <summary>
        /// Sets the invulnerability state of this pet.
        /// Requirement 8.4: Pet released from cage has 3 seconds invulnerability.
        /// </summary>
        /// <param name="duration">Duration in seconds</param>
        public void SetInvulnerable(float duration)
        {
            var effect = SkillEffectData.CreateInvulnerable(duration, "SetInvulnerable");
            ApplyEffect(effect);
        }
        
        /// <summary>
        /// Updates the opacity for invisibility effect based on movement.
        /// Requirement 4.5: Moving while using Hide In Gap makes cat semi-transparent.
        /// </summary>
        /// <param name="isMoving">Whether the pet is currently moving</param>
        public void UpdateInvisibilityOpacity(bool isMoving)
        {
            if (!IsInvisible) return;
            
            float targetOpacity = isMoving ? 0.5f : 0f;
            var invisEffect = _activeEffects.Find(e => e.Type == SkillEffectType.Invisible);
            if (invisEffect != null)
            {
                invisEffect.Value = targetOpacity;
                UpdateVisualOpacity(targetOpacity);
            }
        }
        
        #endregion

        #region Effect Processing
        
        /// <summary>
        /// Updates all active effects, removing expired ones.
        /// </summary>
        private void UpdateActiveEffects()
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = _activeEffects[i];
                if (!effect.UpdateTime(Time.deltaTime))
                {
                    // Effect expired
                    OnEffectRemoved(effect.Type);
                    _activeEffects.RemoveAt(i);
                    Debug.Log($"[PetAI] Effect expired: {effect.Type}");
                }
            }
        }
        
        /// <summary>
        /// Applies the immediate effect when an effect is added.
        /// </summary>
        private void ApplyEffectImmediate(SkillEffectData effect)
        {
            switch (effect.Type)
            {
                case SkillEffectType.Slow:
                    ApplySpeedModifier(1f - effect.Value);
                    break;
                case SkillEffectType.Stun:
                    if (_navAgent != null)
                    {
                        _navAgent.isStopped = true;
                    }
                    break;
                case SkillEffectType.Invisible:
                    UpdateVisualOpacity(effect.Value);
                    break;
                case SkillEffectType.SpeedBoost:
                    ApplySpeedModifier(1f + effect.Value);
                    break;
                case SkillEffectType.Invulnerable:
                    // Visual feedback for invulnerability could be added here
                    break;
            }
        }
        
        /// <summary>
        /// Called when an effect is removed to restore normal state.
        /// </summary>
        private void OnEffectRemoved(SkillEffectType effectType)
        {
            switch (effectType)
            {
                case SkillEffectType.Slow:
                case SkillEffectType.SpeedBoost:
                    RestoreSpeed();
                    break;
                case SkillEffectType.Stun:
                    if (_navAgent != null && CurrentState != PetState.Captured && CurrentState != PetState.BeingGroomed)
                    {
                        _navAgent.isStopped = false;
                    }
                    break;
                case SkillEffectType.Invisible:
                    UpdateVisualOpacity(1f);
                    break;
                case SkillEffectType.Invulnerable:
                    // Visual feedback removal could be added here
                    break;
            }
        }
        
        /// <summary>
        /// Applies a speed modifier to the NavMeshAgent.
        /// </summary>
        private void ApplySpeedModifier(float multiplier)
        {
            if (_navAgent != null)
            {
                _navAgent.speed = _originalSpeed * multiplier;
            }
        }
        
        /// <summary>
        /// Restores the original speed.
        /// </summary>
        private void RestoreSpeed()
        {
            if (_navAgent != null)
            {
                _navAgent.speed = _originalSpeed;
            }
        }
        
        /// <summary>
        /// Updates the visual opacity of the pet.
        /// </summary>
        private void UpdateVisualOpacity(float opacity)
        {
            CurrentOpacity = opacity;
            
            if (_petRenderer != null)
            {
                Color color = _originalColor;
                color.a = opacity;
                _petRenderer.material.color = color;
            }
        }
        
        #endregion

        #region State Machine
        
        private void UpdateState()
        {
            switch (CurrentState)
            {
                case PetState.Idle:
                    UpdateIdleState();
                    break;
                case PetState.Wandering:
                    UpdateWanderingState();
                    break;
                case PetState.Fleeing:
                    UpdateFleeingState();
                    break;
                case PetState.Captured:
                    UpdateCapturedState();
                    break;
                case PetState.BeingGroomed:
                    UpdateBeingGroomedState();
                    break;
            }
        }
        
        private void OnStateEnter(PetState newState, PetState previousState)
        {
            switch (newState)
            {
                case PetState.Idle:
                    _navAgent.isStopped = true;
                    _wanderTimer = _wanderWaitTime;
                    break;
                case PetState.Wandering:
                    _navAgent.isStopped = false;
                    SetNewWanderTarget();
                    break;
                case PetState.Fleeing:
                    _navAgent.isStopped = false;
                    break;
                case PetState.Captured:
                    _navAgent.isStopped = true;
                    _struggleTimer = StruggleInterval;
                    break;
                case PetState.BeingGroomed:
                    _navAgent.isStopped = true;
                    _struggleTimer = StruggleInterval;
                    break;
            }
        }
        
        private void UpdateIdleState()
        {
            // Check for groomer proximity
            if (ShouldFlee())
            {
                SetState(PetState.Fleeing);
                return;
            }
            
            // Wait then start wandering
            _wanderTimer -= Time.deltaTime;
            if (_wanderTimer <= 0f)
            {
                SetState(PetState.Wandering);
            }
        }
        
        private void UpdateWanderingState()
        {
            // Check for groomer proximity
            if (ShouldFlee())
            {
                SetState(PetState.Fleeing);
                return;
            }
            
            // Check if reached destination
            if (!_navAgent.pathPending && _navAgent.remainingDistance <= _navAgent.stoppingDistance)
            {
                SetState(PetState.Idle);
            }
        }
        
        private void UpdateFleeingState()
        {
            // Check if groomer is far enough
            if (!ShouldFlee())
            {
                SetState(PetState.Idle);
                return;
            }
            
            // Update flee direction
            UpdateFleeDirection();
        }
        
        private void UpdateCapturedState()
        {
            // Struggle attempts
            _struggleTimer -= Time.deltaTime;
            if (_struggleTimer <= 0f)
            {
                _struggleTimer = StruggleInterval;
                TryEscape();
            }
        }
        
        private void UpdateBeingGroomedState()
        {
            // Struggle attempts with reduced chance
            _struggleTimer -= Time.deltaTime;
            if (_struggleTimer <= 0f)
            {
                _struggleTimer = StruggleInterval;
                TryEscape();
            }
        }
        
        #endregion

        #region Movement and Navigation
        
        /// <summary>
        /// NavMesh area mask for elevated surfaces (climbable areas).
        /// Dogs cannot use this area.
        /// </summary>
        private const int ElevatedAreaMask = 1 << 3; // Assuming area 3 is for elevated surfaces
        
        /// <summary>
        /// NavMesh area mask for open areas.
        /// Dogs prefer these when fleeing.
        /// </summary>
        private const int OpenAreaMask = 1 << 4; // Assuming area 4 is for open areas
        
        private void InitializeNavAgent()
        {
            if (_navAgent != null)
            {
                _navAgent.speed = MoveSpeed;
                _navAgent.angularSpeed = 360f;
                _navAgent.acceleration = 8f;
                _navAgent.stoppingDistance = 0.5f;
                
                // Apply pet type specific NavMesh restrictions
                ApplyPetTypeNavMeshRestrictions();
            }
        }
        
        /// <summary>
        /// Applies NavMesh area restrictions based on pet type.
        /// Requirement 2.6: Dog cannot climb elevated surfaces.
        /// </summary>
        private void ApplyPetTypeNavMeshRestrictions()
        {
            if (_navAgent == null) return;
            
            if (_petType == PetType.Dog)
            {
                // Dogs cannot use elevated areas
                // Remove elevated area from the agent's area mask
                _navAgent.areaMask = NavMesh.AllAreas & ~ElevatedAreaMask;
                Debug.Log("[PetAI] Dog: Elevated surfaces disabled");
            }
            else
            {
                // Cats can use all areas
                _navAgent.areaMask = NavMesh.AllAreas;
            }
        }
        
        private void SetNewWanderTarget()
        {
            Vector3 targetPos = GenerateWanderTarget(transform.position, _wanderRadius, _playAreaMin, _playAreaMax);
            
            int areaMask = GetNavigationAreaMask();
            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, _wanderRadius, areaMask))
            {
                _navAgent.SetDestination(hit.position);
            }
        }
        
        private void UpdateFleeDirection()
        {
            if (_groomerTransform == null) return;
            
            Vector3 fleeTarget;
            
            // Requirement 2.5: Dog prefers open areas when fleeing
            if (_petType == PetType.Dog)
            {
                fleeTarget = CalculateDogFleeTarget(transform.position, _groomerTransform.position, FleeDistance, _playAreaMin, _playAreaMax);
            }
            else
            {
                Vector3 fleeDirection = CalculateFleeDirection(transform.position, _groomerTransform.position);
                fleeTarget = transform.position + fleeDirection * FleeDistance;
            }
            
            // Clamp to play area
            fleeTarget = ClampToPlayArea(fleeTarget, _playAreaMin, _playAreaMax);
            
            int areaMask = GetNavigationAreaMask();
            if (NavMesh.SamplePosition(fleeTarget, out NavMeshHit hit, FleeDistance, areaMask))
            {
                _navAgent.SetDestination(hit.position);
            }
        }
        
        /// <summary>
        /// Gets the appropriate NavMesh area mask for this pet type.
        /// </summary>
        private int GetNavigationAreaMask()
        {
            if (_petType == PetType.Dog)
            {
                // Dogs cannot use elevated areas
                return NavMesh.AllAreas & ~ElevatedAreaMask;
            }
            return NavMesh.AllAreas;
        }
        
        /// <summary>
        /// Calculates the flee target for dogs, preferring open areas.
        /// Requirement 2.5: Dog prefers open areas over tight spaces when fleeing.
        /// </summary>
        private Vector3 CalculateDogFleeTarget(Vector3 petPosition, Vector3 groomerPosition, float fleeDistance, Vector3 areaMin, Vector3 areaMax)
        {
            Vector3 baseFleeDirection = CalculateFleeDirection(petPosition, groomerPosition);
            Vector3 baseTarget = petPosition + baseFleeDirection * fleeDistance;
            
            // Try to find an open area in the flee direction
            Vector3 bestTarget = baseTarget;
            float bestOpenness = 0f;
            
            // Sample multiple directions to find the most open area
            int sampleCount = 8;
            for (int i = 0; i < sampleCount; i++)
            {
                float angle = (360f / sampleCount) * i;
                Vector3 sampleDirection = Quaternion.Euler(0, angle, 0) * baseFleeDirection;
                Vector3 sampleTarget = petPosition + sampleDirection * fleeDistance;
                sampleTarget = ClampToPlayArea(sampleTarget, areaMin, areaMax);
                
                // Calculate openness score (distance from walls/obstacles)
                float openness = CalculateOpennessScore(sampleTarget, fleeDistance * 0.5f);
                
                // Prefer directions away from groomer
                float awayBonus = Vector3.Dot(sampleDirection, baseFleeDirection);
                openness += awayBonus * 0.5f;
                
                if (openness > bestOpenness)
                {
                    bestOpenness = openness;
                    bestTarget = sampleTarget;
                }
            }
            
            return bestTarget;
        }
        
        /// <summary>
        /// Calculates how "open" an area is by checking for nearby obstacles.
        /// Higher score means more open space.
        /// </summary>
        private float CalculateOpennessScore(Vector3 position, float checkRadius)
        {
            // Check for obstacles in multiple directions
            int hitCount = 0;
            int checkDirections = 8;
            
            for (int i = 0; i < checkDirections; i++)
            {
                float angle = (360f / checkDirections) * i;
                Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                
                if (Physics.Raycast(position, direction, checkRadius))
                {
                    hitCount++;
                }
            }
            
            // Return normalized openness (1 = fully open, 0 = surrounded)
            return 1f - ((float)hitCount / checkDirections);
        }
        
        private bool ShouldFlee()
        {
            if (_groomerTransform == null) return false;
            
            float distance = Vector3.Distance(transform.position, _groomerTransform.position);
            return distance <= FleeDistance;
        }
        
        private void PerformEscape()
        {
            if (_groomerTransform == null) return;
            
            Vector3 escapePosition = CalculateEscapePosition(
                transform.position, 
                _groomerTransform.position, 
                EscapeTeleportDistance
            );
            
            // Clamp to play area
            escapePosition = ClampToPlayArea(escapePosition, _playAreaMin, _playAreaMax);
            
            // Find valid NavMesh position using appropriate area mask
            int areaMask = GetNavigationAreaMask();
            if (NavMesh.SamplePosition(escapePosition, out NavMeshHit hit, EscapeTeleportDistance, areaMask))
            {
                transform.position = hit.position;
            }
            else
            {
                transform.position = escapePosition;
            }
            
            SetState(PetState.Fleeing);
            OnEscaped?.Invoke();
            
            Debug.Log($"[PetAI] Escaped to position: {transform.position}");
        }
        
        /// <summary>
        /// Checks if a position is on an elevated surface.
        /// Used to validate dog movement restrictions.
        /// </summary>
        /// <param name="position">Position to check</param>
        /// <returns>True if the position is on an elevated surface</returns>
        public bool IsOnElevatedSurface(Vector3 position)
        {
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 1f, ElevatedAreaMask))
            {
                return Vector3.Distance(position, hit.position) < 0.5f;
            }
            return false;
        }
        
        /// <summary>
        /// Checks if this pet can navigate to a target position.
        /// Dogs cannot navigate to elevated surfaces.
        /// </summary>
        /// <param name="targetPosition">Target position to check</param>
        /// <returns>True if the pet can navigate to the position</returns>
        public bool CanNavigateTo(Vector3 targetPosition)
        {
            if (_petType == PetType.Dog && IsOnElevatedSurface(targetPosition))
            {
                return false;
            }
            
            NavMeshPath path = new NavMeshPath();
            int areaMask = GetNavigationAreaMask();
            return NavMesh.CalculatePath(transform.position, targetPosition, areaMask, path) && 
                   path.status == NavMeshPathStatus.PathComplete;
        }
        
        #endregion

        #region Static Calculation Methods (Testable)
        
        /// <summary>
        /// Calculates the escape chance based on grooming steps completed.
        /// Property 9: Escape Chance Reduction Formula
        /// </summary>
        public static float CalculateEscapeChance(int stepsCompleted, float baseChance, float reductionPerStep)
        {
            float reduction = stepsCompleted * reductionPerStep;
            return Mathf.Max(0f, baseChance - reduction);
        }
        
        /// <summary>
        /// Calculates the flee direction away from the groomer.
        /// Requirement 2.2: Pet moves away from groomer.
        /// </summary>
        public static Vector3 CalculateFleeDirection(Vector3 petPosition, Vector3 groomerPosition)
        {
            Vector3 direction = (petPosition - groomerPosition).normalized;
            direction.y = 0f; // Keep on ground plane
            return direction.normalized;
        }
        
        /// <summary>
        /// Calculates the escape teleport position.
        /// Property 7: Escape Teleport Distance
        /// Requirement 3.4: Pet teleports exactly 3 units away.
        /// </summary>
        public static Vector3 CalculateEscapePosition(Vector3 petPosition, Vector3 groomerPosition, float teleportDistance)
        {
            Vector3 direction = CalculateFleeDirection(petPosition, groomerPosition);
            return groomerPosition + direction * teleportDistance;
        }
        
        /// <summary>
        /// Generates a random position within the play area bounds.
        /// Property 2: Pet Spawn Position Validity
        /// </summary>
        public static Vector3 GenerateRandomPositionInBounds(Vector3 min, Vector3 max)
        {
            return new Vector3(
                UnityEngine.Random.Range(min.x, max.x),
                (min.y + max.y) / 2f,
                UnityEngine.Random.Range(min.z, max.z)
            );
        }
        
        /// <summary>
        /// Generates a wander target within bounds.
        /// Property 4: Wander Target Bounds
        /// </summary>
        public static Vector3 GenerateWanderTarget(Vector3 currentPosition, float wanderRadius, Vector3 min, Vector3 max)
        {
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * wanderRadius;
            Vector3 target = currentPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);
            return ClampToPlayArea(target, min, max);
        }
        
        /// <summary>
        /// Clamps a position to the play area bounds.
        /// </summary>
        public static Vector3 ClampToPlayArea(Vector3 position, Vector3 min, Vector3 max)
        {
            return new Vector3(
                Mathf.Clamp(position.x, min.x, max.x),
                position.y,
                Mathf.Clamp(position.z, min.z, max.z)
            );
        }
        
        /// <summary>
        /// Checks if a position is within the play area bounds.
        /// Property 2: Pet Spawn Position Validity
        /// Property 4: Wander Target Bounds
        /// </summary>
        public static bool IsPositionInBounds(Vector3 position, Vector3 min, Vector3 max)
        {
            return position.x >= min.x && position.x <= max.x &&
                   position.z >= min.z && position.z <= max.z;
        }
        
        /// <summary>
        /// Determines if the pet should enter flee state based on distance.
        /// Property 3: Flee State Trigger Distance
        /// </summary>
        public static bool ShouldEnterFleeState(float distance, float fleeDistance)
        {
            return distance <= fleeDistance;
        }
        
        /// <summary>
        /// Calculates the distance between two positions (ignoring Y).
        /// </summary>
        public static float CalculateHorizontalDistance(Vector3 pos1, Vector3 pos2)
        {
            Vector3 diff = pos1 - pos2;
            diff.y = 0f;
            return diff.magnitude;
        }
        
        /// <summary>
        /// Gets the collision radius for a pet type.
        /// Property 5: Pet Type Attribute Differences
        /// Requirement 2.2: Dog has larger collision radius (1.0) compared to Cat (0.5).
        /// </summary>
        /// <param name="petType">The type of pet</param>
        /// <param name="config">Phase 2 config (optional)</param>
        /// <returns>Collision radius for the pet type</returns>
        public static float GetCollisionRadiusForType(PetType petType, Phase2GameConfig config = null)
        {
            if (config != null)
            {
                return petType == PetType.Cat ? config.CatCollisionRadius : config.DogCollisionRadius;
            }
            // Default values from requirements
            return petType == PetType.Cat ? 0.5f : 1.0f;
        }
        
        /// <summary>
        /// Gets the base escape chance for a pet type.
        /// Property 5: Pet Type Attribute Differences
        /// Requirement 2.4: Dog has 30% base escape chance, Cat has 40%.
        /// </summary>
        /// <param name="petType">The type of pet</param>
        /// <param name="phase2Config">Phase 2 config (optional)</param>
        /// <param name="gameConfig">Base game config (optional)</param>
        /// <returns>Base escape chance for the pet type</returns>
        public static float GetBaseEscapeChanceForType(PetType petType, Phase2GameConfig phase2Config = null, GameConfig gameConfig = null)
        {
            if (phase2Config != null)
            {
                return petType == PetType.Cat ? phase2Config.CatBaseEscapeChance : phase2Config.DogBaseEscapeChance;
            }
            if (gameConfig != null && petType == PetType.Cat)
            {
                return gameConfig.BaseEscapeChance;
            }
            // Default values from requirements
            return petType == PetType.Cat ? 0.4f : 0.3f;
        }
        
        /// <summary>
        /// Gets the knockback force for a pet type.
        /// Property 6: Dog Knockback Force Greater Than Cat
        /// Requirement 2.3: Dog applies stronger knockback force than Cat.
        /// </summary>
        /// <param name="petType">The type of pet</param>
        /// <param name="config">Phase 2 config (optional)</param>
        /// <returns>Knockback force for the pet type</returns>
        public static float GetKnockbackForceForType(PetType petType, Phase2GameConfig config = null)
        {
            if (config != null)
            {
                return petType == PetType.Cat ? config.CatKnockbackForce : config.DogKnockbackForce;
            }
            // Default values from requirements
            return petType == PetType.Cat ? 5f : 10f;
        }
        
        /// <summary>
        /// Checks if a pet type can climb elevated surfaces.
        /// Property 7: Dog Cannot Climb Elevated Surfaces
        /// Requirement 2.6: Dog cannot climb elevated surfaces that Cat can access.
        /// </summary>
        /// <param name="petType">The type of pet</param>
        /// <returns>True if the pet can climb elevated surfaces</returns>
        public static bool CanClimbElevatedSurfaces(PetType petType)
        {
            return petType == PetType.Cat;
        }
        
        /// <summary>
        /// Checks if a pet type prefers open areas when fleeing.
        /// Requirement 2.5: Dog prefers open areas over tight spaces when fleeing.
        /// </summary>
        /// <param name="petType">The type of pet</param>
        /// <returns>True if the pet prefers open areas</returns>
        public static bool PrefersOpenAreasWhenFleeing(PetType petType)
        {
            return petType == PetType.Dog;
        }
        
        #endregion

        #region Editor Support
        
#if UNITY_EDITOR
        /// <summary>
        /// Sets the game config for testing purposes.
        /// </summary>
        public void SetConfigForTesting(GameConfig config)
        {
            _gameConfig = config;
        }
        
        /// <summary>
        /// Sets the Phase 2 game config for testing purposes.
        /// </summary>
        public void SetPhase2ConfigForTesting(Phase2GameConfig config)
        {
            _phase2Config = config;
        }
        
        /// <summary>
        /// Sets the pet type for testing purposes.
        /// </summary>
        public void SetPetTypeForTesting(PetType petType)
        {
            _petType = petType;
        }
        
        /// <summary>
        /// Sets the play area bounds for testing.
        /// </summary>
        public void SetPlayAreaBoundsForTesting(Vector3 min, Vector3 max)
        {
            _playAreaMin = min;
            _playAreaMax = max;
        }
        
        /// <summary>
        /// Sets grooming steps completed for testing.
        /// </summary>
        public void SetGroomingStepsForTesting(int steps)
        {
            _groomingStepsCompleted = steps;
        }
        
        /// <summary>
        /// Gets the active effects list for testing.
        /// </summary>
        public List<SkillEffectData> GetActiveEffectsForTesting()
        {
            return _activeEffects;
        }
        
        /// <summary>
        /// Sets the original speed for testing.
        /// </summary>
        public void SetOriginalSpeedForTesting(float speed)
        {
            _originalSpeed = speed;
        }
#endif
        
        private void OnDrawGizmosSelected()
        {
            // Draw play area bounds
            Gizmos.color = Color.green;
            Vector3 center = (_playAreaMin + _playAreaMax) / 2f;
            Vector3 size = _playAreaMax - _playAreaMin;
            Gizmos.DrawWireCube(center, size);
            
            // Draw flee distance
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, FleeDistance);
            
            // Draw collision radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, CollisionRadius);
        }
        
        #endregion
    }
}
