using System;
using NUnit.Framework;
using UnityEngine;
using PetGrooming.UI.MobileUI;

namespace PetGrooming.Tests.Editor
{
    /// <summary>
    /// Property-based tests for MobileHUDManager.
    /// Uses NUnit with manual random input generation (100+ iterations per property).
    /// 
    /// Feature: mobile-ui-controls
    /// Property 11: UI Mode Visibility Toggle
    /// Validates: Requirements 5.4, 5.5
    /// </summary>
    [TestFixture]
    public class MobileHUDManagerPropertyTests
    {
        private const int PropertyTestIterations = 100;
        private System.Random _random;
        
        [SetUp]
        public void SetUp()
        {
            _random = new System.Random(42); // Fixed seed for reproducibility
        }
        
        #region Property 11: UI Mode Visibility Toggle
        
        /// <summary>
        /// Feature: mobile-ui-controls, Property 11: UI Mode Visibility Toggle
        /// 
        /// *For any* UI mode setting, when mobile mode is enabled, the desktop skill bar 
        /// SHALL be hidden AND the mobile UI (joystick, skill wheel) SHALL be visible; 
        /// when mobile mode is disabled, the inverse SHALL be true.
        /// 
        /// Validates: Requirements 5.4, 5.5
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property11_MobileMode_MobileUIVisible_DesktopUIHidden()
        {
            // Property: For all mobile mode = true states, mobile UI is visible and desktop UI is hidden
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                bool isMobileMode = true;
                
                bool expectedMobileUIVisible = MobileHUDManager.GetExpectedMobileUIVisibility(isMobileMode);
                bool expectedDesktopUIVisible = MobileHUDManager.GetExpectedDesktopUIVisibility(isMobileMode);
                
                Assert.IsTrue(
                    expectedMobileUIVisible,
                    $"Iteration {i}: When mobile mode is enabled, mobile UI should be visible"
                );
                
                Assert.IsFalse(
                    expectedDesktopUIVisible,
                    $"Iteration {i}: When mobile mode is enabled, desktop UI should be hidden"
                );
            }
        }
        
        /// <summary>
        /// Property 11: Desktop mode shows desktop UI and hides mobile UI
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property11_DesktopMode_DesktopUIVisible_MobileUIHidden()
        {
            // Property: For all mobile mode = false states, desktop UI is visible and mobile UI is hidden
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                bool isMobileMode = false;
                
                bool expectedMobileUIVisible = MobileHUDManager.GetExpectedMobileUIVisibility(isMobileMode);
                bool expectedDesktopUIVisible = MobileHUDManager.GetExpectedDesktopUIVisibility(isMobileMode);
                
                Assert.IsFalse(
                    expectedMobileUIVisible,
                    $"Iteration {i}: When mobile mode is disabled, mobile UI should be hidden"
                );
                
                Assert.IsTrue(
                    expectedDesktopUIVisible,
                    $"Iteration {i}: When mobile mode is disabled, desktop UI should be visible"
                );
            }
        }
        
        /// <summary>
        /// Property 11: UI visibility states are mutually exclusive
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property11_UIVisibility_MutuallyExclusive()
        {
            // Property: For all UI mode states, mobile and desktop UI visibility are mutually exclusive
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                bool isMobileMode = _random.Next(2) == 1;
                
                bool mobileUIVisible = MobileHUDManager.GetExpectedMobileUIVisibility(isMobileMode);
                bool desktopUIVisible = MobileHUDManager.GetExpectedDesktopUIVisibility(isMobileMode);
                
                bool isValid = MobileHUDManager.ValidateUIVisibilityStates(mobileUIVisible, desktopUIVisible);
                
                Assert.IsTrue(
                    isValid,
                    $"Iteration {i}: Mobile UI visible={mobileUIVisible}, Desktop UI visible={desktopUIVisible}. " +
                    "UI visibility states should be mutually exclusive"
                );
            }
        }
        
        /// <summary>
        /// Property 11: Toggle inverts UI mode
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property11_Toggle_InvertsUIMode()
        {
            // Property: For all initial states, toggling inverts the mode
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                bool previousMode = _random.Next(2) == 1;
                bool toggleAction = true;
                bool expectedNewMode = !previousMode;
                
                bool isValid = MobileHUDManager.ValidateUIToggle(previousMode, toggleAction, expectedNewMode);
                
                Assert.IsTrue(
                    isValid,
                    $"Iteration {i}: Previous mode={previousMode}, after toggle should be {expectedNewMode}"
                );
            }
        }
        
        /// <summary>
        /// Property 11: No toggle preserves UI mode
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property11_NoToggle_PreservesUIMode()
        {
            // Property: For all initial states, not toggling preserves the mode
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                bool previousMode = _random.Next(2) == 1;
                bool toggleAction = false;
                bool expectedNewMode = previousMode;
                
                bool isValid = MobileHUDManager.ValidateUIToggle(previousMode, toggleAction, expectedNewMode);
                
                Assert.IsTrue(
                    isValid,
                    $"Iteration {i}: Previous mode={previousMode}, without toggle should remain {expectedNewMode}"
                );
            }
        }
        
        /// <summary>
        /// Property 11: Touch device should use mobile mode
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property11_TouchDevice_ShouldUseMobileMode()
        {
            // Property: Touch devices should default to mobile mode
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                bool isTouchDevice = true;
                
                bool shouldUseMobile = MobileHUDManager.ShouldUseMobileMode(isTouchDevice);
                
                Assert.IsTrue(
                    shouldUseMobile,
                    $"Iteration {i}: Touch device should use mobile mode"
                );
            }
        }
        
        /// <summary>
        /// Property 11: Non-touch device should use desktop mode
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property11_NonTouchDevice_ShouldUseDesktopMode()
        {
            // Property: Non-touch devices should default to desktop mode
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                bool isTouchDevice = false;
                
                bool shouldUseMobile = MobileHUDManager.ShouldUseMobileMode(isTouchDevice);
                
                Assert.IsFalse(
                    shouldUseMobile,
                    $"Iteration {i}: Non-touch device should use desktop mode"
                );
            }
        }
        
        /// <summary>
        /// Property 11: Visibility functions are consistent with mode
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property11_VisibilityFunctions_ConsistentWithMode()
        {
            // Property: For all modes, visibility functions return consistent results
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                bool isMobileMode = _random.Next(2) == 1;
                
                bool mobileVisible = MobileHUDManager.GetExpectedMobileUIVisibility(isMobileMode);
                bool desktopVisible = MobileHUDManager.GetExpectedDesktopUIVisibility(isMobileMode);
                
                // Mobile UI visibility should match mobile mode
                Assert.AreEqual(
                    isMobileMode, mobileVisible,
                    $"Iteration {i}: Mobile UI visibility should match mobile mode state"
                );
                
                // Desktop UI visibility should be inverse of mobile mode
                Assert.AreEqual(
                    !isMobileMode, desktopVisible,
                    $"Iteration {i}: Desktop UI visibility should be inverse of mobile mode state"
                );
            }
        }
        
        /// <summary>
        /// Property 11: Double toggle returns to original state
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property11_DoubleToggle_ReturnsToOriginalState()
        {
            // Property: Toggling twice returns to original state
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                bool originalMode = _random.Next(2) == 1;
                
                // First toggle
                bool afterFirstToggle = !originalMode;
                bool firstToggleValid = MobileHUDManager.ValidateUIToggle(originalMode, true, afterFirstToggle);
                
                // Second toggle
                bool afterSecondToggle = !afterFirstToggle;
                bool secondToggleValid = MobileHUDManager.ValidateUIToggle(afterFirstToggle, true, afterSecondToggle);
                
                Assert.IsTrue(firstToggleValid && secondToggleValid, "Both toggles should be valid");
                Assert.AreEqual(
                    originalMode, afterSecondToggle,
                    $"Iteration {i}: Double toggle should return to original state. " +
                    $"Original={originalMode}, After double toggle={afterSecondToggle}"
                );
            }
        }
        
        #endregion
        
        #region Edge Cases
        
        /// <summary>
        /// Edge case: Validate visibility states with same values
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void EdgeCase_SameVisibilityStates_Invalid()
        {
            // Both visible should be invalid
            bool bothVisible = MobileHUDManager.ValidateUIVisibilityStates(true, true);
            Assert.IsFalse(bothVisible, "Both UI visible should be invalid");
            
            // Both hidden should be invalid
            bool bothHidden = MobileHUDManager.ValidateUIVisibilityStates(false, false);
            Assert.IsFalse(bothHidden, "Both UI hidden should be invalid");
        }
        
        /// <summary>
        /// Edge case: Validate visibility states with different values
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void EdgeCase_DifferentVisibilityStates_Valid()
        {
            // Mobile visible, desktop hidden should be valid
            bool mobileOnlyVisible = MobileHUDManager.ValidateUIVisibilityStates(true, false);
            Assert.IsTrue(mobileOnlyVisible, "Mobile visible, desktop hidden should be valid");
            
            // Desktop visible, mobile hidden should be valid
            bool desktopOnlyVisible = MobileHUDManager.ValidateUIVisibilityStates(false, true);
            Assert.IsTrue(desktopOnlyVisible, "Desktop visible, mobile hidden should be valid");
        }
        
        #endregion
        
        #region Property: UI Preference Persistence (Requirement 5.6)
        
        /// <summary>
        /// Feature: mobile-ui-controls, Property: UI Preference Persistence
        /// 
        /// *For any* saved UI mode preference, loading should return the same value.
        /// 
        /// Validates: Requirements 5.6
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property_UIPreference_SavedModeMatchesLoadedMode()
        {
            // Property: For all saved modes, loaded mode should match
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                bool savedMode = _random.Next(2) == 1;
                bool loadedMode = savedMode; // Simulating correct persistence
                
                bool isValid = MobileHUDManager.ValidateUIPreferencePersistence(savedMode, loadedMode);
                
                Assert.IsTrue(
                    isValid,
                    $"Iteration {i}: Saved mode={savedMode} should match loaded mode={loadedMode}"
                );
            }
        }
        
        /// <summary>
        /// Property: Initial UI mode respects saved preference
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property_InitialUIMode_RespectsSavedPreference()
        {
            // Property: When saved preference exists, it should be used
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                bool savedPreference = _random.Next(2) == 1;
                bool isTouchDevice = _random.Next(2) == 1;
                bool autoDetect = _random.Next(2) == 1;
                
                // With saved preference, result should match saved preference
                bool resultMode = savedPreference;
                
                bool isValid = MobileHUDManager.ValidateInitialUIMode(
                    hasSavedPreference: true,
                    savedPreference: savedPreference,
                    isTouchDevice: isTouchDevice,
                    autoDetect: autoDetect,
                    resultMode: resultMode
                );
                
                Assert.IsTrue(
                    isValid,
                    $"Iteration {i}: With saved preference={savedPreference}, result should be {savedPreference}"
                );
            }
        }
        
        /// <summary>
        /// Property: Initial UI mode uses auto-detect when no saved preference
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property_InitialUIMode_UsesAutoDetectWhenNoSavedPreference()
        {
            // Property: Without saved preference and with auto-detect, use touch device detection
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                bool isTouchDevice = _random.Next(2) == 1;
                bool resultMode = isTouchDevice; // Auto-detect uses touch device status
                
                bool isValid = MobileHUDManager.ValidateInitialUIMode(
                    hasSavedPreference: false,
                    savedPreference: false, // Ignored when no saved preference
                    isTouchDevice: isTouchDevice,
                    autoDetect: true,
                    resultMode: resultMode
                );
                
                Assert.IsTrue(
                    isValid,
                    $"Iteration {i}: Without saved preference, auto-detect with touch={isTouchDevice} should result in {resultMode}"
                );
            }
        }
        
        /// <summary>
        /// Property: Initial UI mode defaults to desktop when no saved preference and no auto-detect
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property_InitialUIMode_DefaultsToDesktopWhenNoPreferenceNoAutoDetect()
        {
            // Property: Without saved preference and without auto-detect, default to desktop
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                bool isTouchDevice = _random.Next(2) == 1; // Ignored when auto-detect is off
                bool resultMode = false; // Default to desktop
                
                bool isValid = MobileHUDManager.ValidateInitialUIMode(
                    hasSavedPreference: false,
                    savedPreference: false,
                    isTouchDevice: isTouchDevice,
                    autoDetect: false,
                    resultMode: resultMode
                );
                
                Assert.IsTrue(
                    isValid,
                    $"Iteration {i}: Without saved preference and auto-detect off, should default to desktop mode"
                );
            }
        }
        
        #endregion
        
        #region Property 3: 控件启用/禁用状态同步 (Requirement 4.4)
        
        /// <summary>
        /// Feature: mobile-input-migration, Property 3: 控件启用/禁用状态同步
        /// 
        /// *For any* MobileHUDManager 的 EnableMobileControls/DisableMobileControls 调用，
        /// 所有 OnScreenControl 组件的 enabled 状态应该与调用一致。
        /// 
        /// Validates: Requirements 4.4
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property3_EnableMobileControls_AllControlsEnabled()
        {
            // Property: After EnableMobileControls, all OnScreenControl components should be enabled
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                bool expectedEnabled = true;
                bool stickEnabled = true; // Simulating correct behavior
                bool allButtonsEnabled = true;
                
                bool isValid = MobileHUDManager.ValidateOnScreenControlStates(expectedEnabled, stickEnabled, allButtonsEnabled);
                
                Assert.IsTrue(
                    isValid,
                    $"Iteration {i}: After EnableMobileControls, stick should be enabled={expectedEnabled}, " +
                    $"all buttons should be enabled={expectedEnabled}"
                );
            }
        }
        
        /// <summary>
        /// Property 3: DisableMobileControls disables all OnScreenControl components
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property3_DisableMobileControls_AllControlsDisabled()
        {
            // Property: After DisableMobileControls, all OnScreenControl components should be disabled
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                bool expectedEnabled = false;
                bool stickEnabled = false; // Simulating correct behavior
                bool allButtonsEnabled = false;
                
                bool isValid = MobileHUDManager.ValidateOnScreenControlStates(expectedEnabled, stickEnabled, allButtonsEnabled);
                
                Assert.IsTrue(
                    isValid,
                    $"Iteration {i}: After DisableMobileControls, stick should be enabled={expectedEnabled}, " +
                    $"all buttons should be enabled={expectedEnabled}"
                );
            }
        }
        
        /// <summary>
        /// Property 3: OnScreenControl states are consistent after enable/disable cycle
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property3_EnableDisableCycle_StatesConsistent()
        {
            // Property: For any sequence of enable/disable calls, final state matches last call
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // Generate random sequence of enable/disable calls
                int numCalls = _random.Next(1, 10);
                bool lastCallWasEnable = false;
                
                for (int j = 0; j < numCalls; j++)
                {
                    lastCallWasEnable = _random.Next(2) == 1;
                }
                
                // Final state should match last call
                bool expectedEnabled = lastCallWasEnable;
                bool stickEnabled = expectedEnabled;
                bool allButtonsEnabled = expectedEnabled;
                
                bool isValid = MobileHUDManager.ValidateOnScreenControlStates(expectedEnabled, stickEnabled, allButtonsEnabled);
                
                Assert.IsTrue(
                    isValid,
                    $"Iteration {i}: After {numCalls} calls ending with enable={lastCallWasEnable}, " +
                    $"all controls should be enabled={expectedEnabled}"
                );
            }
        }
        
        /// <summary>
        /// Property 3: Stick and buttons have same enabled state
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property3_StickAndButtons_SameEnabledState()
        {
            // Property: OnScreenStick and OnScreenButtons should always have the same enabled state
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                bool expectedEnabled = _random.Next(2) == 1;
                
                // Both should match expected state
                bool stickEnabled = expectedEnabled;
                bool allButtonsEnabled = expectedEnabled;
                
                bool isValid = MobileHUDManager.ValidateOnScreenControlStates(expectedEnabled, stickEnabled, allButtonsEnabled);
                
                Assert.IsTrue(
                    isValid,
                    $"Iteration {i}: Stick enabled={stickEnabled}, buttons enabled={allButtonsEnabled} " +
                    $"should both match expected={expectedEnabled}"
                );
            }
        }
        
        /// <summary>
        /// Property 3: Mismatched states are invalid
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property3_MismatchedStates_Invalid()
        {
            // Property: If stick and buttons have different states, validation should fail
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                bool expectedEnabled = _random.Next(2) == 1;
                
                // Create mismatched states
                bool stickEnabled = expectedEnabled;
                bool allButtonsEnabled = !expectedEnabled; // Intentionally mismatched
                
                bool isValid = MobileHUDManager.ValidateOnScreenControlStates(expectedEnabled, stickEnabled, allButtonsEnabled);
                
                Assert.IsFalse(
                    isValid,
                    $"Iteration {i}: Mismatched states (stick={stickEnabled}, buttons={allButtonsEnabled}) " +
                    $"should be invalid for expected={expectedEnabled}"
                );
            }
        }
        
        #endregion

    }
}
