using System;
using UnityEngine;

namespace PetGrooming.Systems.Skills
{
    /// <summary>
    /// Abstract base class for all skills in the game.
    /// Manages cooldown timing, ready state detection, and event callbacks.
    /// Requirements: 3.3, 3.6, 3.8, 4.2, 4.3, 4.4, 5.2, 5.4, 5.5
    /// </summary>
    public abstract class SkillBase : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Skill Settings")]
        [Tooltip("Display name of the skill")]
        public string SkillName;
        
        [Tooltip("Cooldown duration in seconds")]
        public float Cooldown;
        
        [Tooltip("Icon sprite for UI display")]
        public Sprite Icon;
        #endregion

        #region Properties
        /// <summary>
        /// Remaining cooldown time in seconds.
        /// </summary>
        public float RemainingCooldown { get; protected set; }
        
        /// <summary>
        /// Whether the skill is ready to be activated (cooldown complete).
        /// </summary>
        public bool IsReady => RemainingCooldown <= 0f;
        
        /// <summary>
        /// Normalized cooldown progress (0 = ready, 1 = just activated).
        /// </summary>
        public float CooldownProgress => Cooldown > 0f ? RemainingCooldown / Cooldown : 0f;
        #endregion

        #region Events
        /// <summary>
        /// Fired when cooldown value changes. Parameter is remaining cooldown time.
        /// </summary>
        public event Action<float> OnCooldownChanged;
        
        /// <summary>
        /// Fired when the skill is successfully activated.
        /// </summary>
        public event Action OnSkillActivated;
        
        /// <summary>
        /// Fired when the skill becomes ready after cooldown completes.
        /// </summary>
        public event Action OnSkillReady;
        #endregion

        #region Private Fields
        private bool _wasOnCooldown;
        #endregion

        #region Unity Lifecycle
        protected virtual void Awake()
        {
            // Start with skill ready
            RemainingCooldown = 0f;
            _wasOnCooldown = false;
        }

        protected virtual void Update()
        {
            UpdateCooldown();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Checks if the skill can be activated.
        /// Override in derived classes to add additional conditions.
        /// </summary>
        /// <returns>True if the skill can be activated</returns>
        public virtual bool CanActivate()
        {
            return IsReady;
        }

        /// <summary>
        /// Attempts to activate the skill.
        /// </summary>
        /// <returns>True if activation was successful</returns>
        public bool TryActivate()
        {
            if (!CanActivate())
            {
                return false;
            }
            
            Activate();
            return true;
        }

        /// <summary>
        /// Activates the skill and starts cooldown.
        /// Override in derived classes to implement skill-specific behavior.
        /// </summary>
        public virtual void Activate()
        {
            StartCooldown();
            OnSkillActivated?.Invoke();
        }

        /// <summary>
        /// Resets the cooldown to zero, making the skill immediately ready.
        /// </summary>
        public void ResetCooldown()
        {
            float previousCooldown = RemainingCooldown;
            RemainingCooldown = 0f;
            
            if (previousCooldown > 0f)
            {
                OnCooldownChanged?.Invoke(RemainingCooldown);
                OnSkillReady?.Invoke();
            }
            
            _wasOnCooldown = false;
        }

        /// <summary>
        /// Sets the cooldown to a specific value.
        /// </summary>
        /// <param name="cooldownTime">The cooldown time to set</param>
        public void SetCooldown(float cooldownTime)
        {
            RemainingCooldown = Mathf.Max(0f, cooldownTime);
            _wasOnCooldown = RemainingCooldown > 0f;
            OnCooldownChanged?.Invoke(RemainingCooldown);
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Starts the cooldown timer.
        /// </summary>
        protected void StartCooldown()
        {
            RemainingCooldown = Cooldown;
            _wasOnCooldown = true;
            OnCooldownChanged?.Invoke(RemainingCooldown);
        }

        /// <summary>
        /// Updates the cooldown timer each frame.
        /// </summary>
        protected virtual void UpdateCooldown()
        {
            if (RemainingCooldown <= 0f)
            {
                return;
            }
            
            RemainingCooldown -= Time.deltaTime;
            
            if (RemainingCooldown <= 0f)
            {
                RemainingCooldown = 0f;
                
                if (_wasOnCooldown)
                {
                    _wasOnCooldown = false;
                    OnSkillReady?.Invoke();
                }
            }
            
            OnCooldownChanged?.Invoke(RemainingCooldown);
        }
        #endregion
    }
}
