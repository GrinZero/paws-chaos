using System;
using NUnit.Framework;
using UnityEngine;
using PetGrooming.UI.MobileUI;

namespace PetGrooming.Tests.Editor
{
    /// <summary>
    /// Property-based tests for SkillButtonVisual.
    /// Uses NUnit with manual random input generation (100+ iterations per property).
    /// 
    /// Feature: mobile-input-migration
    /// Property 4: 技能按钮冷却显示同步
    /// Validates: Requirements 7.1, 7.2, 7.5
    /// </summary>
    [TestFixture]
    public class SkillButtonVisualPropertyTests
    {
        private const int PropertyTestIterations = 100;
        private System.Random _random;
        
        [SetUp]
        public void SetUp()
        {
            _random = new System.Random(42); // 固定种子以保证可重复性
        }
        
        #region Property 4: 技能按钮冷却显示同步
        
        /// <summary>
        /// Feature: mobile-input-migration, Property 4: 技能按钮冷却显示同步
        /// 
        /// *For any* 技能状态变化（冷却开始、冷却中、冷却结束），SkillButtonVisual 的显示状态
        /// 应该正确反映技能的冷却状态：
        /// - 冷却中：显示冷却遮罩和剩余时间
        /// - 冷却结束：隐藏冷却遮罩
        /// 
        /// Validates: Requirements 7.1, 7.2, 7.5
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property4_WhenCooldownGreaterThanZero_OverlayIsVisible()
        {
            // Property: 对于所有 remaining cooldown > 0，遮罩应该可见
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // 生成随机冷却值，remaining > 0
                float totalCooldown = (float)(_random.NextDouble() * 20 + 1); // 1 到 21 秒
                float remainingCooldown = (float)(_random.NextDouble() * totalCooldown) + 0.001f; // > 0
                
                var state = SkillButtonVisual.CalculateCooldownDisplayState(remainingCooldown, totalCooldown);
                
                Assert.IsTrue(
                    state.overlayVisible,
                    $"迭代 {i}: remaining={remainingCooldown}, total={totalCooldown}. " +
                    $"冷却 > 0 时遮罩应该可见"
                );
            }
        }
        
        /// <summary>
        /// Property 4: 冷却 > 0 时文本可见
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property4_WhenCooldownGreaterThanZero_TextIsVisible()
        {
            // Property: 对于所有 remaining cooldown > 0，文本应该可见
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float totalCooldown = (float)(_random.NextDouble() * 20 + 1);
                float remainingCooldown = (float)(_random.NextDouble() * totalCooldown) + 0.001f;
                
                var state = SkillButtonVisual.CalculateCooldownDisplayState(remainingCooldown, totalCooldown);
                
                Assert.IsTrue(
                    state.textVisible,
                    $"迭代 {i}: remaining={remainingCooldown}, total={totalCooldown}. " +
                    $"冷却 > 0 时文本应该可见"
                );
            }
        }
        
        /// <summary>
        /// Property 4: 冷却文本显示向上取整的秒数
        /// Requirement 7.2: 显示剩余冷却时间（秒）
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property4_CooldownText_ShowsRoundedUpSeconds()
        {
            // Property: 对于所有 remaining cooldown > 0，文本显示 ceil(remaining)
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float totalCooldown = (float)(_random.NextDouble() * 20 + 1);
                float remainingCooldown = (float)(_random.NextDouble() * totalCooldown) + 0.001f;
                
                var state = SkillButtonVisual.CalculateCooldownDisplayState(remainingCooldown, totalCooldown);
                
                int expectedValue = Mathf.CeilToInt(remainingCooldown);
                string expectedText = expectedValue.ToString();
                
                Assert.AreEqual(
                    expectedText, state.displayText,
                    $"迭代 {i}: remaining={remainingCooldown}. " +
                    $"期望文本='{expectedText}', 实际='{state.displayText}'"
                );
            }
        }
        
        /// <summary>
        /// Property 4: 冷却为零时遮罩隐藏
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property4_WhenCooldownZero_OverlayIsHidden()
        {
            // Property: 对于 remaining cooldown = 0，遮罩应该隐藏
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float totalCooldown = (float)(_random.NextDouble() * 20 + 1);
                float remainingCooldown = 0f;
                
                var state = SkillButtonVisual.CalculateCooldownDisplayState(remainingCooldown, totalCooldown);
                
                Assert.IsFalse(
                    state.overlayVisible,
                    $"迭代 {i}: remaining={remainingCooldown}, total={totalCooldown}. " +
                    $"冷却 = 0 时遮罩应该隐藏"
                );
            }
        }
        
        /// <summary>
        /// Property 4: 冷却为零时文本隐藏
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property4_WhenCooldownZero_TextIsHidden()
        {
            // Property: 对于 remaining cooldown = 0，文本应该隐藏
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float totalCooldown = (float)(_random.NextDouble() * 20 + 1);
                float remainingCooldown = 0f;
                
                var state = SkillButtonVisual.CalculateCooldownDisplayState(remainingCooldown, totalCooldown);
                
                Assert.IsFalse(
                    state.textVisible,
                    $"迭代 {i}: remaining={remainingCooldown}, total={totalCooldown}. " +
                    $"冷却 = 0 时文本应该隐藏"
                );
            }
        }
        
        /// <summary>
        /// Property 4: 填充量与冷却进度成比例
        /// Requirement 7.1: 显示径向冷却遮罩
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property4_FillAmount_IsProportionalToCooldownProgress()
        {
            // Property: fillAmount = remaining / total (限制在 [0, 1])
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float totalCooldown = (float)(_random.NextDouble() * 20 + 1);
                float remainingCooldown = (float)(_random.NextDouble() * totalCooldown * 1.5f); // 可能超过 total
                
                var state = SkillButtonVisual.CalculateCooldownDisplayState(remainingCooldown, totalCooldown);
                
                float expectedFill = Mathf.Clamp01(remainingCooldown / totalCooldown);
                
                Assert.AreEqual(
                    expectedFill, state.fillAmount, 0.0001f,
                    $"迭代 {i}: remaining={remainingCooldown}, total={totalCooldown}. " +
                    $"期望 fill={expectedFill}, 实际={state.fillAmount}"
                );
            }
        }
        
        /// <summary>
        /// Property 4: 填充量始终在 [0, 1] 范围内
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property4_FillAmount_AlwaysInValidRange()
        {
            // Property: 对于所有输入，fillAmount 在 [0, 1] 范围内
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float totalCooldown = (float)(_random.NextDouble() * 100 - 50); // 可能为负
                float remainingCooldown = (float)(_random.NextDouble() * 100 - 50);
                
                var state = SkillButtonVisual.CalculateCooldownDisplayState(remainingCooldown, totalCooldown);
                
                Assert.GreaterOrEqual(
                    state.fillAmount, 0f,
                    $"迭代 {i}: remaining={remainingCooldown}, total={totalCooldown}. " +
                    $"填充量应该 >= 0"
                );
                Assert.LessOrEqual(
                    state.fillAmount, 1f,
                    $"迭代 {i}: remaining={remainingCooldown}, total={totalCooldown}. " +
                    $"填充量应该 <= 1"
                );
            }
        }
        
        /// <summary>
        /// Property 4: ShouldShowCooldownOverlay 与 CalculateCooldownDisplayState 一致
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property4_ShouldShowCooldownOverlay_ConsistentWithDisplayState()
        {
            // Property: ShouldShowCooldownOverlay 与 CalculateCooldownDisplayState 的 overlayVisible 匹配
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float remainingCooldown = (float)(_random.NextDouble() * 20 - 5); // -5 到 15
                float totalCooldown = (float)(_random.NextDouble() * 20 + 1);
                
                bool shouldShow = SkillButtonVisual.ShouldShowCooldownOverlay(remainingCooldown);
                var state = SkillButtonVisual.CalculateCooldownDisplayState(remainingCooldown, totalCooldown);
                
                Assert.AreEqual(
                    shouldShow, state.overlayVisible,
                    $"迭代 {i}: remaining={remainingCooldown}. " +
                    $"ShouldShowCooldownOverlay={shouldShow}, overlayVisible={state.overlayVisible}"
                );
            }
        }
        
        /// <summary>
        /// Property 4: CalculateCooldownText 与 CalculateCooldownDisplayState 一致
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property4_CalculateCooldownText_ConsistentWithDisplayState()
        {
            // Property: CalculateCooldownText 与 CalculateCooldownDisplayState 的 displayText 匹配
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float remainingCooldown = (float)(_random.NextDouble() * 20);
                float totalCooldown = (float)(_random.NextDouble() * 20 + 1);
                
                string text = SkillButtonVisual.CalculateCooldownText(remainingCooldown);
                var state = SkillButtonVisual.CalculateCooldownDisplayState(remainingCooldown, totalCooldown);
                
                Assert.AreEqual(
                    text, state.displayText,
                    $"迭代 {i}: remaining={remainingCooldown}. " +
                    $"CalculateCooldownText='{text}', displayText='{state.displayText}'"
                );
            }
        }
        
        /// <summary>
        /// Property 4: 冷却状态变化的一致性
        /// Requirement 7.5: SkillButtonVisual 观察技能状态并更新 UI
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void Property4_CooldownStateTransition_IsConsistent()
        {
            // Property: 从冷却中到就绪的状态转换是一致的
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float totalCooldown = (float)(_random.NextDouble() * 20 + 1);
                
                // 模拟冷却开始
                float startCooldown = totalCooldown;
                var startState = SkillButtonVisual.CalculateCooldownDisplayState(startCooldown, totalCooldown);
                
                // 模拟冷却中
                float midCooldown = totalCooldown * 0.5f;
                var midState = SkillButtonVisual.CalculateCooldownDisplayState(midCooldown, totalCooldown);
                
                // 模拟冷却结束
                float endCooldown = 0f;
                var endState = SkillButtonVisual.CalculateCooldownDisplayState(endCooldown, totalCooldown);
                
                // 验证状态转换
                Assert.IsTrue(startState.overlayVisible, $"迭代 {i}: 冷却开始时遮罩应该可见");
                Assert.IsTrue(midState.overlayVisible, $"迭代 {i}: 冷却中遮罩应该可见");
                Assert.IsFalse(endState.overlayVisible, $"迭代 {i}: 冷却结束时遮罩应该隐藏");
                
                // 验证填充量递减
                Assert.Greater(startState.fillAmount, midState.fillAmount, 
                    $"迭代 {i}: 开始填充量应该大于中间填充量");
                Assert.Greater(midState.fillAmount, endState.fillAmount, 
                    $"迭代 {i}: 中间填充量应该大于结束填充量");
            }
        }
        
        #endregion
        
        #region Press Scale Properties
        
        /// <summary>
        /// 按下缩放是原始的 95%
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void PressScale_Is95PercentOfOriginal()
        {
            // Property: 对于所有原始缩放，按下缩放 = 原始 * 0.95
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float scaleValue = (float)(_random.NextDouble() * 2 + 0.5f); // 0.5 到 2.5
                Vector3 originalScale = new Vector3(scaleValue, scaleValue, scaleValue);
                
                Vector3 pressScale = SkillButtonVisual.CalculatePressScale(originalScale);
                
                Vector3 expectedScale = originalScale * 0.95f;
                
                Assert.AreEqual(
                    expectedScale.x, pressScale.x, 0.0001f,
                    $"迭代 {i}: originalScale={originalScale}. " +
                    $"期望 pressScale.x={expectedScale.x}, 实际={pressScale.x}"
                );
                Assert.AreEqual(
                    expectedScale.y, pressScale.y, 0.0001f,
                    $"迭代 {i}: originalScale={originalScale}. " +
                    $"期望 pressScale.y={expectedScale.y}, 实际={pressScale.y}"
                );
                Assert.AreEqual(
                    expectedScale.z, pressScale.z, 0.0001f,
                    $"迭代 {i}: originalScale={originalScale}. " +
                    $"期望 pressScale.z={expectedScale.z}, 实际={pressScale.z}"
                );
            }
        }
        
        /// <summary>
        /// 按下缩放使用自定义乘数
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void PressScale_WithCustomMultiplier()
        {
            // Property: 对于所有原始缩放和乘数，按下缩放 = 原始 * 乘数
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float scaleValue = (float)(_random.NextDouble() * 2 + 0.5f);
                Vector3 originalScale = new Vector3(scaleValue, scaleValue, scaleValue);
                float multiplier = (float)(_random.NextDouble() * 0.3 + 0.7f); // 0.7 到 1.0
                
                Vector3 pressScale = SkillButtonVisual.CalculatePressScale(originalScale, multiplier);
                
                Vector3 expectedScale = originalScale * multiplier;
                
                Assert.AreEqual(
                    expectedScale.x, pressScale.x, 0.0001f,
                    $"迭代 {i}: originalScale={originalScale}, multiplier={multiplier}. " +
                    $"期望 pressScale.x={expectedScale.x}, 实际={pressScale.x}"
                );
            }
        }
        
        /// <summary>
        /// 按下缩放小于原始（使用默认乘数）
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void PressScale_IsSmallerThanOriginal()
        {
            // Property: 对于所有正的原始缩放，按下缩放 < 原始
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                float scaleValue = (float)(_random.NextDouble() * 10 + 0.1f); // 0.1 到 10.1
                Vector3 originalScale = new Vector3(scaleValue, scaleValue, scaleValue);
                
                Vector3 pressScale = SkillButtonVisual.CalculatePressScale(originalScale);
                
                Assert.Less(
                    pressScale.magnitude, originalScale.magnitude,
                    $"迭代 {i}: originalScale={originalScale}. " +
                    $"按下缩放大小={pressScale.magnitude} 应该小于原始={originalScale.magnitude}"
                );
            }
        }
        
        /// <summary>
        /// 按下缩放保持宽高比
        /// </summary>
        [Test]
        [Category("PropertyBasedTest")]
        public void PressScale_PreservesAspectRatio()
        {
            // Property: 对于所有原始缩放，按下缩放保持相同比例
            for (int i = 0; i < PropertyTestIterations; i++)
            {
                // 生成非均匀缩放
                Vector3 originalScale = new Vector3(
                    (float)(_random.NextDouble() * 2 + 0.5f),
                    (float)(_random.NextDouble() * 2 + 0.5f),
                    (float)(_random.NextDouble() * 2 + 0.5f)
                );
                
                Vector3 pressScale = SkillButtonVisual.CalculatePressScale(originalScale);
                
                // 检查比例是否保持
                if (originalScale.x > 0.001f && originalScale.y > 0.001f)
                {
                    float originalRatioXY = originalScale.x / originalScale.y;
                    float pressRatioXY = pressScale.x / pressScale.y;
                    
                    Assert.AreEqual(
                        originalRatioXY, pressRatioXY, 0.0001f,
                        $"迭代 {i}: originalScale={originalScale}. " +
                        $"X/Y 比例应该保持"
                    );
                }
                
                if (originalScale.y > 0.001f && originalScale.z > 0.001f)
                {
                    float originalRatioYZ = originalScale.y / originalScale.z;
                    float pressRatioYZ = pressScale.y / pressScale.z;
                    
                    Assert.AreEqual(
                        originalRatioYZ, pressRatioYZ, 0.0001f,
                        $"迭代 {i}: originalScale={originalScale}. " +
                        $"Y/Z 比例应该保持"
                    );
                }
            }
        }
        
        #endregion
    }
}
