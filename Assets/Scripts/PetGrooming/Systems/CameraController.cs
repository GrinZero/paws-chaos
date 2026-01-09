using System;
using UnityEngine;
using PetGrooming.Core;

namespace PetGrooming.Systems
{
    /// <summary>
    /// Smart camera controller that follows the Groomer with smooth interpolation,
    /// handles zoom, collision avoidance, boundary clamping, and special view modes.
    /// Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 9.6, 9.7, 9.8, 9.9, 9.10
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Configuration")]
        [SerializeField] private Phase2GameConfig _phase2Config;

        [Header("Follow Settings")]
        [Tooltip("The target to follow (usually the Groomer)")]
        [SerializeField] private Transform _target;
        
        [Tooltip("Camera follow interpolation speed (higher = faster, 0 = instant)")]
        [SerializeField] private float _followSpeed = 5f;
        
        [Tooltip("If true, camera follows target instantly without smoothing")]
        [SerializeField] private bool _instantFollow = true;
        
        [Tooltip("Default camera offset from target")]
        [SerializeField] private Vector3 _defaultOffset = new Vector3(0, 8, -6);
        
        [Tooltip("Default camera field of view")]
        [SerializeField] private float _defaultFOV = 60f;

        [Header("Zoom Settings")]
        [Tooltip("Zoom multiplier when Groomer captures a pet (1.2 = 20% zoom out)")]
        [SerializeField] private float _captureZoomMultiplier = 1.2f;
        
        [Tooltip("Camera zoom interpolation speed")]
        [SerializeField] private float _zoomSpeed = 2f;

        [Header("Collision Settings")]
        [Tooltip("Minimum distance camera can be from target")]
        [SerializeField] private float _minDistance = 2f;
        
        [Tooltip("Layers that the camera should avoid clipping through")]
        [SerializeField] private LayerMask _collisionLayers = ~0;
        
        [Tooltip("Radius for collision sphere cast")]
        [SerializeField] private float _collisionRadius = 0.3f;

        [Header("Boundary Settings")]
        [Tooltip("Scene bounds for camera clamping")]
        [SerializeField] private Bounds _sceneBounds = new Bounds(Vector3.zero, new Vector3(100, 50, 100));
        
        [Tooltip("Enable boundary clamping")]
        [SerializeField] private bool _enableBoundaryClamping = true;

        [Header("Grooming View Settings")]
        [Tooltip("Offset for grooming view camera position")]
        [SerializeField] private Vector3 _groomingViewOffset = new Vector3(2, 3, -3);
        
        [Tooltip("Speed for transitioning to/from grooming view")]
        [SerializeField] private float _groomingViewTransitionSpeed = 3f;

        [Header("Alert Effects")]
        [Tooltip("Screen shake intensity during alert state")]
        [SerializeField] private float _alertShakeIntensity = 0.1f;
        
        [Tooltip("Screen shake duration during alert state")]
        [SerializeField] private float _alertShakeDuration = 0.5f;
        
        [Tooltip("Frequency of screen shake")]
        [SerializeField] private float _shakeFrequency = 25f;

        #endregion

        #region Private Fields

        private Camera _camera;
        private GroomerController _groomerController;
        private Vector3 _currentOffset;
        private float _currentZoomMultiplier = 1f;
        private float _targetZoomMultiplier = 1f;
        private bool _isInGroomingView;
        private Transform _groomingStationTarget;
        private Vector3 _groomingViewPosition;
        private Quaternion _groomingViewRotation;
        private float _shakeTimer;
        private Vector3 _shakeOffset;
        private bool _isShaking;

        #endregion

        #region Properties

        /// <summary>
        /// The target transform being followed.
        /// </summary>
        public Transform Target
        {
            get => _target;
            set => _target = value;
        }

        /// <summary>
        /// Camera follow interpolation speed.
        /// Requirement 9.1: Configurable follow speed.
        /// </summary>
        public float FollowSpeed
        {
            get => _phase2Config != null ? _phase2Config.CameraFollowSpeed : _followSpeed;
            set => _followSpeed = value;
        }

        /// <summary>
        /// Whether camera follows target instantly without smoothing.
        /// </summary>
        public bool InstantFollow
        {
            get => _instantFollow;
            set => _instantFollow = value;
        }

        /// <summary>
        /// Default camera offset from target.
        /// Requirement 9.2: Configurable offset distance and height.
        /// </summary>
        public Vector3 DefaultOffset
        {
            get => _phase2Config != null ? _phase2Config.CameraDefaultOffset : _defaultOffset;
            set => _defaultOffset = value;
        }

        /// <summary>
        /// Default camera field of view.
        /// Requirement 9.9: Configurable field of view.
        /// </summary>
        public float DefaultFOV
        {
            get => _phase2Config != null ? _phase2Config.CameraDefaultFOV : _defaultFOV;
            set => _defaultFOV = value;
        }

        /// <summary>
        /// Zoom multiplier when capturing a pet.
        /// Requirement 9.4: Zoom out when capturing.
        /// </summary>
        public float CaptureZoomMultiplier
        {
            get => _phase2Config != null ? _phase2Config.CameraCaptureZoomMultiplier : _captureZoomMultiplier;
        }

        /// <summary>
        /// Camera zoom interpolation speed.
        /// </summary>
        public float ZoomSpeed
        {
            get => _phase2Config != null ? _phase2Config.CameraZoomSpeed : _zoomSpeed;
        }

        /// <summary>
        /// Minimum distance from target for collision avoidance.
        /// Requirement 9.8: Move closer to maintain visibility.
        /// </summary>
        public float MinDistance
        {
            get => _phase2Config != null ? _phase2Config.CameraMinDistance : _minDistance;
        }

        /// <summary>
        /// Screen shake intensity during alert.
        /// Requirement 9.10: Subtle screen shake during alert.
        /// </summary>
        public float AlertShakeIntensity
        {
            get => _phase2Config != null ? _phase2Config.AlertShakeIntensity : _alertShakeIntensity;
        }

        /// <summary>
        /// Screen shake duration during alert.
        /// </summary>
        public float AlertShakeDuration
        {
            get => _phase2Config != null ? _phase2Config.AlertShakeDuration : _alertShakeDuration;
        }

        /// <summary>
        /// Whether the camera is currently in grooming view mode.
        /// Requirement 9.6: Fixed grooming view angle.
        /// </summary>
        public bool IsInGroomingView => _isInGroomingView;

        /// <summary>
        /// Current zoom multiplier being applied.
        /// </summary>
        public float CurrentZoomMultiplier => _currentZoomMultiplier;

        /// <summary>
        /// Scene bounds for camera clamping.
        /// Requirement 9.3: Clamp to prevent showing out-of-bounds areas.
        /// </summary>
        public Bounds SceneBounds
        {
            get => _sceneBounds;
            set => _sceneBounds = value;
        }

        /// <summary>
        /// Reference to the Phase 2 game configuration.
        /// </summary>
        public Phase2GameConfig Phase2Config => _phase2Config;

        #endregion

        #region Events

        /// <summary>
        /// Fired when camera enters grooming view.
        /// </summary>
        public event Action OnGroomingViewEntered;

        /// <summary>
        /// Fired when camera exits grooming view.
        /// </summary>
        public event Action OnGroomingViewExited;

        /// <summary>
        /// Fired when zoom state changes.
        /// </summary>
        public event Action<float> OnZoomChanged;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            _currentOffset = DefaultOffset;

            if (_phase2Config == null)
            {
                Debug.LogWarning("[CameraController] Phase2GameConfig is not assigned, using default values.");
            }
        }

        private void Start()
        {
            InitializeTarget();
            SubscribeToEvents();
            
            // Set initial FOV
            if (_camera != null)
            {
                _camera.fieldOfView = DefaultFOV;
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void LateUpdate()
        {
            // Try to find target if not set
            if (_target == null)
            {
                InitializeTarget();
                if (_target == null)
                {
                    return;
                }
            }

            UpdateZoom();
            UpdateShake();

            if (_isInGroomingView)
            {
                UpdateGroomingView();
            }
            else
            {
                UpdateFollowPosition();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the camera to grooming view mode.
        /// Requirement 9.6: Switch to fixed grooming view angle.
        /// </summary>
        /// <param name="groomingStation">The grooming station to focus on.</param>
        public void SetGroomingView(Transform groomingStation)
        {
            if (groomingStation == null)
            {
                Debug.LogWarning("[CameraController] Cannot set grooming view with null station.");
                return;
            }

            _isInGroomingView = true;
            _groomingStationTarget = groomingStation;

            // Calculate grooming view position and rotation
            _groomingViewPosition = groomingStation.position + _groomingViewOffset;
            _groomingViewRotation = Quaternion.LookRotation(groomingStation.position - _groomingViewPosition);

            OnGroomingViewEntered?.Invoke();
            Debug.Log("[CameraController] Entered grooming view.");
        }

        /// <summary>
        /// Resets the camera to default follow view.
        /// Requirement 9.6: Return from grooming view.
        /// </summary>
        public void ResetToDefaultView()
        {
            if (!_isInGroomingView)
            {
                return;
            }

            _isInGroomingView = false;
            _groomingStationTarget = null;

            OnGroomingViewExited?.Invoke();
            Debug.Log("[CameraController] Exited grooming view.");
        }

        /// <summary>
        /// Triggers the alert screen shake effect.
        /// Requirement 9.10: Subtle screen shake during alert.
        /// </summary>
        public void TriggerAlertShake()
        {
            _isShaking = true;
            _shakeTimer = AlertShakeDuration;
            Debug.Log("[CameraController] Alert shake triggered.");
        }

        /// <summary>
        /// Stops any ongoing screen shake.
        /// </summary>
        public void StopShake()
        {
            _isShaking = false;
            _shakeTimer = 0f;
            _shakeOffset = Vector3.zero;
        }

        /// <summary>
        /// Sets the zoom multiplier for capture state.
        /// Requirement 9.4, 9.5: Zoom out on capture, return on release.
        /// </summary>
        /// <param name="isCapturing">Whether the groomer is capturing/carrying a pet.</param>
        public void SetCaptureZoom(bool isCapturing)
        {
            _targetZoomMultiplier = isCapturing ? CaptureZoomMultiplier : 1f;
            OnZoomChanged?.Invoke(_targetZoomMultiplier);
        }

        /// <summary>
        /// Sets the scene bounds for camera clamping.
        /// Requirement 9.3: Prevent showing out-of-bounds areas.
        /// </summary>
        /// <param name="bounds">The scene bounds.</param>
        public void SetSceneBounds(Bounds bounds)
        {
            _sceneBounds = bounds;
        }

        /// <summary>
        /// Enables or disables boundary clamping.
        /// </summary>
        /// <param name="enabled">Whether to enable boundary clamping.</param>
        public void SetBoundaryClampingEnabled(bool enabled)
        {
            _enableBoundaryClamping = enabled;
        }

        #endregion

        #region Private Methods

        private void InitializeTarget()
        {
            if (_target == null)
            {
                // Try to find the GroomerController
                _groomerController = FindObjectOfType<GroomerController>();
                if (_groomerController != null)
                {
                    _target = _groomerController.transform;
                    Debug.Log($"[CameraController] Found target: {_target.name}");
                }
                else
                {
                    // Try to find by tag
                    GameObject player = GameObject.FindGameObjectWithTag("Player");
                    if (player != null)
                    {
                        _target = player.transform;
                        _groomerController = player.GetComponent<GroomerController>();
                        Debug.Log($"[CameraController] Found target by tag: {_target.name}");
                    }
                    else
                    {
                        Debug.LogWarning("[CameraController] No target assigned and no Player found.");
                    }
                }
            }
            else
            {
                _groomerController = _target.GetComponent<GroomerController>();
            }
        }

        private void SubscribeToEvents()
        {
            // Subscribe to groomer events for zoom
            if (_groomerController != null)
            {
                _groomerController.OnPetCaptured += OnPetCaptured;
                _groomerController.OnPetEscaped += OnPetReleased;
                _groomerController.OnPetStoredInCage += OnPetStoredInCage;
            }

            // Subscribe to alert system for shake
            if (AlertSystem.Instance != null)
            {
                AlertSystem.Instance.OnAlertStarted += OnAlertStarted;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_groomerController != null)
            {
                _groomerController.OnPetCaptured -= OnPetCaptured;
                _groomerController.OnPetEscaped -= OnPetReleased;
                _groomerController.OnPetStoredInCage -= OnPetStoredInCage;
            }

            if (AlertSystem.Instance != null)
            {
                AlertSystem.Instance.OnAlertStarted -= OnAlertStarted;
            }
        }

        private void OnPetCaptured(AI.PetAI pet)
        {
            SetCaptureZoom(true);
        }

        private void OnPetReleased()
        {
            SetCaptureZoom(false);
        }

        private void OnPetStoredInCage(PetCage cage)
        {
            SetCaptureZoom(false);
        }

        private void OnAlertStarted()
        {
            TriggerAlertShake();
        }

        private void UpdateFollowPosition()
        {
            // Calculate desired position with zoom
            Vector3 zoomedOffset = _currentOffset * _currentZoomMultiplier;
            Vector3 desiredPosition = _target.position + zoomedOffset;

            // Apply collision avoidance
            desiredPosition = HandleCollision(desiredPosition);

            // Apply boundary clamping
            if (_enableBoundaryClamping)
            {
                desiredPosition = ClampToBounds(desiredPosition);
            }

            // Apply shake offset
            desiredPosition += _shakeOffset;

            // Follow target - instant or smooth
            if (_instantFollow)
            {
                transform.position = desiredPosition;
            }
            else
            {
                // Smooth follow
                // Requirement 9.1: Smooth interpolation at configurable follow speed
                transform.position = CalculateSmoothPosition(transform.position, desiredPosition, FollowSpeed, Time.deltaTime);
            }

            // Look at target
            transform.LookAt(_target.position + Vector3.up * 1.5f);
        }

        private void UpdateGroomingView()
        {
            // Smoothly transition to grooming view
            // Requirement 9.6: Fixed grooming view angle
            transform.position = Vector3.Lerp(
                transform.position, 
                _groomingViewPosition, 
                _groomingViewTransitionSpeed * Time.deltaTime);
            
            transform.rotation = Quaternion.Slerp(
                transform.rotation, 
                _groomingViewRotation, 
                _groomingViewTransitionSpeed * Time.deltaTime);
        }

        private void UpdateZoom()
        {
            // Smoothly interpolate zoom
            // Requirement 9.4, 9.5: Smooth zoom transitions
            _currentZoomMultiplier = Mathf.Lerp(
                _currentZoomMultiplier, 
                _targetZoomMultiplier, 
                ZoomSpeed * Time.deltaTime);
        }

        private void UpdateShake()
        {
            if (!_isShaking)
            {
                _shakeOffset = Vector3.zero;
                return;
            }

            _shakeTimer -= Time.deltaTime;
            if (_shakeTimer <= 0f)
            {
                _isShaking = false;
                _shakeOffset = Vector3.zero;
                return;
            }

            // Calculate shake offset using Perlin noise for smooth shake
            // Requirement 9.10: Subtle screen shake effect
            _shakeOffset = CalculateShakeOffset(Time.time, _shakeFrequency, AlertShakeIntensity);
        }

        private Vector3 HandleCollision(Vector3 desiredPosition)
        {
            if (_target == null)
            {
                return desiredPosition;
            }

            // Requirement 9.7, 9.8: Collision detection and distance adjustment
            Vector3 direction = desiredPosition - _target.position;
            float distance = direction.magnitude;

            // Perform sphere cast from target to desired position
            if (Physics.SphereCast(
                _target.position + Vector3.up * 1.5f,
                _collisionRadius,
                direction.normalized,
                out RaycastHit hit,
                distance,
                _collisionLayers))
            {
                // Move camera closer to avoid obstacle
                float newDistance = Mathf.Max(hit.distance - _collisionRadius, MinDistance);
                return _target.position + Vector3.up * 1.5f + direction.normalized * newDistance;
            }

            return desiredPosition;
        }

        private Vector3 ClampToBounds(Vector3 position)
        {
            // Requirement 9.3: Clamp position to prevent showing out-of-bounds areas
            return ClampPositionToBounds(position, _sceneBounds);
        }

        #endregion

        #region Static Calculation Methods (Testable)

        /// <summary>
        /// Calculates smooth camera position using interpolation.
        /// Requirement 9.1: Smooth interpolation at configurable follow speed.
        /// </summary>
        /// <param name="currentPosition">Current camera position.</param>
        /// <param name="targetPosition">Target camera position.</param>
        /// <param name="followSpeed">Follow interpolation speed.</param>
        /// <param name="deltaTime">Time since last frame.</param>
        /// <returns>New interpolated position.</returns>
        public static Vector3 CalculateSmoothPosition(
            Vector3 currentPosition, 
            Vector3 targetPosition, 
            float followSpeed, 
            float deltaTime)
        {
            return Vector3.Lerp(currentPosition, targetPosition, followSpeed * deltaTime);
        }

        /// <summary>
        /// Calculates the camera offset with zoom applied.
        /// Requirement 9.4: Zoom out when capturing.
        /// </summary>
        /// <param name="defaultOffset">Default camera offset.</param>
        /// <param name="zoomMultiplier">Zoom multiplier (>1 = zoom out).</param>
        /// <returns>Zoomed offset.</returns>
        public static Vector3 CalculateZoomedOffset(Vector3 defaultOffset, float zoomMultiplier)
        {
            return defaultOffset * zoomMultiplier;
        }

        /// <summary>
        /// Clamps a position to stay within bounds.
        /// Requirement 9.3: Clamp to prevent showing out-of-bounds areas.
        /// Property 22: Camera Boundary Clamping
        /// </summary>
        /// <param name="position">Position to clamp.</param>
        /// <param name="bounds">Bounds to clamp within.</param>
        /// <returns>Clamped position.</returns>
        public static Vector3 ClampPositionToBounds(Vector3 position, Bounds bounds)
        {
            return new Vector3(
                Mathf.Clamp(position.x, bounds.min.x, bounds.max.x),
                Mathf.Clamp(position.y, bounds.min.y, bounds.max.y),
                Mathf.Clamp(position.z, bounds.min.z, bounds.max.z)
            );
        }

        /// <summary>
        /// Validates that a position is within bounds.
        /// Property 22: Camera Boundary Clamping
        /// </summary>
        /// <param name="position">Position to validate.</param>
        /// <param name="bounds">Bounds to check against.</param>
        /// <returns>True if position is within bounds.</returns>
        public static bool IsPositionWithinBounds(Vector3 position, Bounds bounds)
        {
            return bounds.Contains(position);
        }

        /// <summary>
        /// Calculates shake offset using Perlin noise.
        /// Requirement 9.10: Subtle screen shake effect.
        /// </summary>
        /// <param name="time">Current time.</param>
        /// <param name="frequency">Shake frequency.</param>
        /// <param name="intensity">Shake intensity.</param>
        /// <returns>Shake offset vector.</returns>
        public static Vector3 CalculateShakeOffset(float time, float frequency, float intensity)
        {
            float x = (Mathf.PerlinNoise(time * frequency, 0f) - 0.5f) * 2f * intensity;
            float y = (Mathf.PerlinNoise(0f, time * frequency) - 0.5f) * 2f * intensity;
            return new Vector3(x, y, 0f);
        }

        /// <summary>
        /// Calculates the zoom multiplier based on capture state.
        /// Property 21: Camera Zoom State Consistency
        /// Requirement 9.4, 9.5: Zoom out on capture, return on release.
        /// </summary>
        /// <param name="isCarryingPet">Whether the groomer is carrying a pet.</param>
        /// <param name="captureZoomMultiplier">Zoom multiplier for capture state.</param>
        /// <returns>Target zoom multiplier.</returns>
        public static float CalculateTargetZoomMultiplier(bool isCarryingPet, float captureZoomMultiplier)
        {
            return isCarryingPet ? captureZoomMultiplier : 1f;
        }

        /// <summary>
        /// Validates zoom state consistency.
        /// Property 21: Camera Zoom State Consistency
        /// </summary>
        /// <param name="isCarryingPet">Whether carrying a pet.</param>
        /// <param name="currentZoomMultiplier">Current zoom multiplier.</param>
        /// <param name="captureZoomMultiplier">Expected capture zoom multiplier.</param>
        /// <param name="tolerance">Tolerance for float comparison.</param>
        /// <returns>True if zoom state is consistent with carry state.</returns>
        public static bool ValidateZoomStateConsistency(
            bool isCarryingPet, 
            float currentZoomMultiplier, 
            float captureZoomMultiplier,
            float tolerance = 0.01f)
        {
            float expectedZoom = CalculateTargetZoomMultiplier(isCarryingPet, captureZoomMultiplier);
            return Mathf.Abs(currentZoomMultiplier - expectedZoom) <= tolerance;
        }

        /// <summary>
        /// Calculates collision-adjusted camera distance.
        /// Property 23: Camera Collision Avoidance
        /// Requirement 9.7, 9.8: Collision detection and distance adjustment.
        /// </summary>
        /// <param name="hitDistance">Distance to collision hit.</param>
        /// <param name="collisionRadius">Collision sphere radius.</param>
        /// <param name="minDistance">Minimum allowed distance.</param>
        /// <returns>Adjusted camera distance.</returns>
        public static float CalculateCollisionAdjustedDistance(
            float hitDistance, 
            float collisionRadius, 
            float minDistance)
        {
            return Mathf.Max(hitDistance - collisionRadius, minDistance);
        }

        /// <summary>
        /// Validates collision avoidance.
        /// Property 23: Camera Collision Avoidance
        /// </summary>
        /// <param name="cameraPosition">Camera position.</param>
        /// <param name="targetPosition">Target position.</param>
        /// <param name="obstacleExists">Whether an obstacle exists between camera and target.</param>
        /// <param name="hasLineOfSight">Whether camera has line of sight to target.</param>
        /// <returns>True if collision avoidance is working correctly.</returns>
        public static bool ValidateCollisionAvoidance(
            Vector3 cameraPosition, 
            Vector3 targetPosition, 
            bool obstacleExists, 
            bool hasLineOfSight)
        {
            // If obstacle exists, camera should have adjusted to maintain line of sight
            if (obstacleExists)
            {
                return hasLineOfSight;
            }
            return true;
        }

        /// <summary>
        /// Validates grooming view switch.
        /// Property 24: Camera Grooming View Switch
        /// Requirement 9.6: Switch to fixed grooming view angle.
        /// </summary>
        /// <param name="isGrooming">Whether groomer is in grooming state.</param>
        /// <param name="isInGroomingView">Whether camera is in grooming view.</param>
        /// <returns>True if grooming view state matches grooming state.</returns>
        public static bool ValidateGroomingViewSwitch(bool isGrooming, bool isInGroomingView)
        {
            return isGrooming == isInGroomingView;
        }

        #endregion

        #region Editor Support

#if UNITY_EDITOR
        /// <summary>
        /// Sets the Phase 2 config for testing purposes.
        /// </summary>
        public void SetPhase2ConfigForTesting(Phase2GameConfig config)
        {
            _phase2Config = config;
        }

        /// <summary>
        /// Sets the target for testing purposes.
        /// </summary>
        public void SetTargetForTesting(Transform target)
        {
            _target = target;
        }

        /// <summary>
        /// Sets the grooming view state for testing purposes.
        /// </summary>
        public void SetGroomingViewStateForTesting(bool isInGroomingView)
        {
            _isInGroomingView = isInGroomingView;
        }

        /// <summary>
        /// Sets the zoom multiplier for testing purposes.
        /// </summary>
        public void SetZoomMultiplierForTesting(float current, float target)
        {
            _currentZoomMultiplier = current;
            _targetZoomMultiplier = target;
        }

        /// <summary>
        /// Gets the current offset for testing purposes.
        /// </summary>
        public Vector3 GetCurrentOffsetForTesting()
        {
            return _currentOffset;
        }

        /// <summary>
        /// Sets the collision layers for testing purposes.
        /// </summary>
        public void SetCollisionLayersForTesting(LayerMask layers)
        {
            _collisionLayers = layers;
        }
#endif

        private void OnDrawGizmosSelected()
        {
            // Draw scene bounds
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(_sceneBounds.center, _sceneBounds.size);

            // Draw default offset
            if (_target != null)
            {
                Gizmos.color = Color.cyan;
                Vector3 offsetPos = _target.position + DefaultOffset;
                Gizmos.DrawLine(_target.position, offsetPos);
                Gizmos.DrawWireSphere(offsetPos, 0.3f);
            }

            // Draw min distance sphere
            if (_target != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(_target.position + Vector3.up * 1.5f, MinDistance);
            }
        }

        #endregion
    }
}
