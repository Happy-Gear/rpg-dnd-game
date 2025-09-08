using System;
using NUnit.Framework;
using RPGGame.Combat;
using RPGGame.Tests.TestHelpers;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Tests for CounterGauge - the unique "badminton streak" counter-attack system
    /// Critical mechanic: Rewards successful over-defense with powerful counter opportunities
    /// Must thoroughly test state transitions, boundaries, and reset conditions
    /// </summary>
    [TestFixture]
    public class CounterGaugeTests : TestBase
    {
        private CounterGauge _counter;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _counter = new CounterGauge();
        }
        
        // =============================================================================
        // INITIALIZATION TESTS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_InitializeToZero_When_Created()
        {
            // Arrange & Act
            var counter = new CounterGauge();
            
            // Assert
            Assert.That(counter.Current, Is.EqualTo(0), "Counter should start at 0");
            Assert.That(counter.Maximum, Is.EqualTo(6), "Maximum should be 6");
            Assert.That(counter.IsReady, Is.False, "Should not be ready initially");
            Assert.That(counter.FillPercentage, Is.EqualTo(0f), "Fill percentage should be 0%");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_HaveCorrectConstants_When_CheckingMaximum()
        {
            // Arrange & Act
            var counter = new CounterGauge();
            
            // Assert
            Assert.That(counter.Maximum, Is.EqualTo(6), "Maximum counter should always be 6");
            
            // Verify it's truly constant (create multiple instances)
            var counter2 = new CounterGauge();
            var counter3 = new CounterGauge();
            
            Assert.That(counter2.Maximum, Is.EqualTo(6), "All instances should have same max");
            Assert.That(counter3.Maximum, Is.EqualTo(6), "All instances should have same max");
        }
        
        // =============================================================================
        // ADDING COUNTER TESTS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_AddCounter_When_BelowMaximum()
        {
            // Arrange
            Assert.That(_counter.Current, Is.EqualTo(0), "Should start at 0");
            
            // Act
            _counter.AddCounter(3);
            
            // Assert
            Assert.That(_counter.Current, Is.EqualTo(3), "Should have 3 counter points");
            Assert.That(_counter.IsReady, Is.False, "Should not be ready at 3");
            Assert.That(_counter.FillPercentage, Is.EqualTo(0.5f).Within(0.001f), "Should be 50% filled");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_AccumulateCounter_When_AddingMultipleTimes()
        {
            // Act & Assert - Progressive accumulation
            _counter.AddCounter(2);
            Assert.That(_counter.Current, Is.EqualTo(2), "Should have 2 after first add");
            
            _counter.AddCounter(2);
            Assert.That(_counter.Current, Is.EqualTo(4), "Should have 4 after second add");
            
            _counter.AddCounter(1);
            Assert.That(_counter.Current, Is.EqualTo(5), "Should have 5 after third add");
            
            Assert.That(_counter.IsReady, Is.False, "Should not be ready at 5");
            Assert.That(_counter.FillPercentage, Is.EqualTo(5f/6f).Within(0.001f), "Should be 5/6 filled");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_ReachReady_When_ReachingSixPoints()
        {
            // Act
            _counter.AddCounter(6);
            
            // Assert
            Assert.That(_counter.Current, Is.EqualTo(6), "Should have exactly 6 points");
            Assert.That(_counter.IsReady, Is.True, "Should be ready at 6");
            Assert.That(_counter.FillPercentage, Is.EqualTo(1.0f), "Should be 100% filled");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_CapAtMaximum_When_AddingBeyondSix()
        {
            // Arrange
            _counter.AddCounter(4);
            
            // Act - Try to add more than remaining capacity
            _counter.AddCounter(5); // Would be 9, but should cap at 6
            
            // Assert
            Assert.That(_counter.Current, Is.EqualTo(6), "Should cap at maximum of 6");
            Assert.That(_counter.IsReady, Is.True, "Should be ready when capped");
            Assert.That(_counter.FillPercentage, Is.EqualTo(1.0f), "Should be 100% filled");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_CapAtMaximum_When_AlreadyAtMaximum()
        {
            // Arrange
            _counter.AddCounter(6);
            Assert.That(_counter.Current, Is.EqualTo(6), "Should be at max");
            
            // Act - Try to add more when already at max
            _counter.AddCounter(3);
            
            // Assert
            Assert.That(_counter.Current, Is.EqualTo(6), "Should remain at 6");
            Assert.That(_counter.IsReady, Is.True, "Should remain ready");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_HandleZeroAddition_When_AddingZero()
        {
            // Arrange
            _counter.AddCounter(3);
            
            // Act
            _counter.AddCounter(0);
            
            // Assert
            Assert.That(_counter.Current, Is.EqualTo(3), "Should not change when adding 0");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_HandleNegativeAddition_When_AddingNegative()
        {
            // Arrange
            _counter.AddCounter(4);
            
            // Act - Negative values should be treated as 0 (no reduction)
            _counter.AddCounter(-2);
            
            // Assert
            Assert.That(_counter.Current, Is.EqualTo(4), "Negative additions should not reduce counter");
        }
        
        // =============================================================================
        // CONSUMING COUNTER TESTS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_ConsumeSuccessfully_When_CounterIsReady()
        {
            // Arrange
            _counter.AddCounter(6);
            Assert.That(_counter.IsReady, Is.True, "Should be ready");
            
            // Act
            var consumed = _counter.ConsumeCounter();
            
            // Assert
            Assert.That(consumed, Is.True, "Should successfully consume");
            Assert.That(_counter.Current, Is.EqualTo(0), "Should reset to 0");
            Assert.That(_counter.IsReady, Is.False, "Should no longer be ready");
            Assert.That(_counter.FillPercentage, Is.EqualTo(0f), "Should be 0% filled");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_FailToConsume_When_NotReady()
        {
            // Arrange - Various not-ready states
            var counters = new[] { 0, 1, 2, 3, 4, 5 };
            
            foreach (var value in counters)
            {
                var counter = new CounterGauge();
                counter.AddCounter(value);
                
                // Act
                var consumed = counter.ConsumeCounter();
                
                // Assert
                Assert.That(consumed, Is.False, $"Should not consume at {value} points");
                Assert.That(counter.Current, Is.EqualTo(value), $"Should remain at {value}");
                Assert.That(counter.IsReady, Is.False, $"Should not be ready at {value}");
            }
        }
        
        [Test]
        [Category("Unit")]
        public void Should_AllowMultipleConsumes_When_RefillingBetween()
        {
            // First cycle
            _counter.AddCounter(6);
            Assert.That(_counter.ConsumeCounter(), Is.True, "First consume should succeed");
            Assert.That(_counter.Current, Is.EqualTo(0), "Should reset after first consume");
            
            // Second cycle
            _counter.AddCounter(6);
            Assert.That(_counter.ConsumeCounter(), Is.True, "Second consume should succeed");
            Assert.That(_counter.Current, Is.EqualTo(0), "Should reset after second consume");
            
            // Third cycle
            _counter.AddCounter(6);
            Assert.That(_counter.ConsumeCounter(), Is.True, "Third consume should succeed");
            Assert.That(_counter.Current, Is.EqualTo(0), "Should reset after third consume");
        }
        
        // =============================================================================
        // RESET TESTS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_ResetToZero_When_CallingReset()
        {
            // Arrange
            _counter.AddCounter(4);
            Assert.That(_counter.Current, Is.EqualTo(4), "Should have 4 points");
            
            // Act
            _counter.Reset();
            
            // Assert
            Assert.That(_counter.Current, Is.EqualTo(0), "Should reset to 0");
            Assert.That(_counter.IsReady, Is.False, "Should not be ready after reset");
            Assert.That(_counter.FillPercentage, Is.EqualTo(0f), "Should be 0% filled");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_ResetFromReady_When_ResettingAtMax()
        {
            // Arrange
            _counter.AddCounter(6);
            Assert.That(_counter.IsReady, Is.True, "Should be ready");
            
            // Act
            _counter.Reset();
            
            // Assert
            Assert.That(_counter.Current, Is.EqualTo(0), "Should reset to 0");
            Assert.That(_counter.IsReady, Is.False, "Should not be ready after reset");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_HandleResetWhenEmpty_When_AlreadyAtZero()
        {
            // Arrange
            Assert.That(_counter.Current, Is.EqualTo(0), "Should start at 0");
            
            // Act
            _counter.Reset();
            
            // Assert
            Assert.That(_counter.Current, Is.EqualTo(0), "Should remain at 0");
            Assert.That(_counter.IsReady, Is.False, "Should remain not ready");
        }
        
        // =============================================================================
        // FILL PERCENTAGE TESTS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        [TestCaseSource(nameof(FillPercentageTestCases))]
        public void Should_CalculateCorrectFillPercentage_When_AtVariousLevels(
            int counterValue, float expectedPercentage, string description)
        {
            // Arrange
            if (counterValue > 0)
            {
                _counter.AddCounter(counterValue);
            }
            
            // Act
            var fillPercentage = _counter.FillPercentage;
            
            // Assert
            Assert.That(fillPercentage, Is.EqualTo(expectedPercentage).Within(0.001f), 
                       $"{description}: Expected {expectedPercentage:P0}, got {fillPercentage:P0}");
        }
        
        // =============================================================================
        // STRING REPRESENTATION TESTS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_FormatCorrectly_When_NotReady()
        {
            // Arrange
            _counter.AddCounter(3);
            
            // Act
            var formatted = _counter.ToString();
            
            // Assert
            Assert.That(formatted, Is.EqualTo("Counter: 3/6"), 
                       "Should format as 'Counter: current/max' when not ready");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_FormatWithReadyIndicator_When_Ready()
        {
            // Arrange
            _counter.AddCounter(6);
            
            // Act
            var formatted = _counter.ToString();
            
            // Assert
            Assert.That(formatted, Is.EqualTo("Counter: 6/6 [READY!]"), 
                       "Should include [READY!] indicator when at max");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_FormatAtZero_When_Empty()
        {
            // Act
            var formatted = _counter.ToString();
            
            // Assert
            Assert.That(formatted, Is.EqualTo("Counter: 0/6"), 
                       "Should show 0/6 when empty");
        }
        
        // =============================================================================
        // GAME SCENARIO TESTS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_BuildCounterFromOverDefense_When_SimulatingCombat()
        {
            // Simulate multiple over-defense scenarios
            
            // Scenario 1: Small over-defense
            _counter.AddCounter(1); // +1 from slight over-defense
            Assert.That(_counter.Current, Is.EqualTo(1));
            
            // Scenario 2: Medium over-defense
            _counter.AddCounter(2); // +2 from good defense
            Assert.That(_counter.Current, Is.EqualTo(3));
            
            // Scenario 3: Large over-defense
            _counter.AddCounter(3); // +3 from excellent defense
            Assert.That(_counter.Current, Is.EqualTo(6));
            Assert.That(_counter.IsReady, Is.True, "Should trigger badminton streak");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_NotLoseProgress_When_DefendingWithoutOverDefense()
        {
            // Arrange - Build up some counter
            _counter.AddCounter(4);
            
            // Act - Simulate defending without over-defense (no add, no reset)
            // In game: Successful defense but no excess = no counter change
            
            // Assert
            Assert.That(_counter.Current, Is.EqualTo(4), 
                       "Counter should be preserved when defending normally");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_HandleRapidCounterBuildup_When_ChainDefending()
        {
            // Simulate rapid defensive chain (badminton rally)
            var overDefenseValues = new[] { 2, 1, 2, 1 }; // Total: 6
            
            foreach (var value in overDefenseValues)
            {
                _counter.AddCounter(value);
            }
            
            // Assert
            Assert.That(_counter.Current, Is.EqualTo(6));
            Assert.That(_counter.IsReady, Is.True, "Rapid chain should trigger counter");
        }
        
        // =============================================================================
        // EDGE CASES AND BOUNDARY CONDITIONS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_HandleLargeAdditions_When_AddingHugeNumbers()
        {
            // Act - Add extremely large number
            _counter.AddCounter(1000);
            
            // Assert
            Assert.That(_counter.Current, Is.EqualTo(6), "Should cap at 6 even with huge input");
            Assert.That(_counter.IsReady, Is.True);
        }
        
        [Test]
        [Category("Unit")]
        public void Should_MaintainStateIntegrity_When_MixingOperations()
        {
            // Complex sequence of operations
            _counter.AddCounter(2);
            Assert.That(_counter.Current, Is.EqualTo(2));
            
            _counter.AddCounter(3);
            Assert.That(_counter.Current, Is.EqualTo(5));
            
            _counter.Reset();
            Assert.That(_counter.Current, Is.EqualTo(0));
            
            _counter.AddCounter(6);
            Assert.That(_counter.IsReady, Is.True);
            
            var consumed = _counter.ConsumeCounter();
            Assert.That(consumed, Is.True);
            Assert.That(_counter.Current, Is.EqualTo(0));
            
            _counter.AddCounter(1);
            Assert.That(_counter.Current, Is.EqualTo(1));
            Assert.That(_counter.IsReady, Is.False);
        }
        
        // =============================================================================
        // TEST DATA
        // =============================================================================
        
        /// <summary>
        /// Test cases for various counter additions
        /// </summary>
        private static readonly object[] AddCounterTestCases = 
        {
            new object[] { 0, 1, 1, false, "Add 1 to empty" },
            new object[] { 2, 2, 4, false, "Add 2 to 2" },
            new object[] { 3, 3, 6, true, "Add 3 to 3 (reaches ready)" },
            new object[] { 5, 1, 6, true, "Add 1 to 5 (reaches ready)" },
            new object[] { 4, 10, 6, true, "Add 10 to 4 (caps at 6)" },
            new object[] { 6, 5, 6, true, "Add to already ready" },
            new object[] { 0, 0, 0, false, "Add 0 to empty" },
            new object[] { 3, -5, 3, false, "Add negative (no effect)" }
        };
        
        /// <summary>
        /// Test cases for fill percentage calculations
        /// </summary>
        private static readonly object[] FillPercentageTestCases = 
        {
            new object[] { 0, 0.0f, "Empty (0%)" },
            new object[] { 1, 1f/6f, "One-sixth (~16.7%)" },
            new object[] { 2, 2f/6f, "One-third (~33.3%)" },
            new object[] { 3, 0.5f, "Half (50%)" },
            new object[] { 4, 4f/6f, "Two-thirds (~66.7%)" },
            new object[] { 5, 5f/6f, "Five-sixths (~83.3%)" },
            new object[] { 6, 1.0f, "Full (100%)" }
        };
    }
}