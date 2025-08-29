using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RPGGame.Dice;
using RPGGame.Tests.TestHelpers;

namespace RPGGame.Tests.Unit.Dice
{
    /// <summary>
    /// Tests for DiceRoller - the foundation of all combat randomization
    /// Critical system: All combat, movement, and evasion depend on dice rolls
    /// Must be thoroughly tested for correctness and statistical properties
    /// </summary>
    [TestFixture]
    public class DiceRollerTests : TestBase
    {
        [Test]
        [Category("Unit")]
        public void Should_CreateDiceRoller_When_UsingDefaultConstructor()
        {
            // Arrange & Act
            var roller = new DiceRoller();
            
            // Assert
            Assert.That(roller, Is.Not.Null, "DiceRoller should be created successfully");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_CreateDeterministicDiceRoller_When_UsingSeed()
        {
            // Arrange & Act
            var roller = new DiceRoller(42);
            
            // Assert
            Assert.That(roller, Is.Not.Null, "Seeded DiceRoller should be created successfully");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_RollValid1d6_When_UsingRoll1d6()
        {
            // Arrange
            var roller = new DiceRoller(DETERMINISTIC_SEED);
            
            // Act
            var result = roller.Roll1d6("test");
            
            // Assert
            AssertValidDiceResult(result, is1d6: true);
            Assert.That(result.RollType, Is.EqualTo("test"), "Roll type should be preserved");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_RollValid2d6_When_UsingRoll2d6()
        {
            // Arrange
            var roller = new DiceRoller(DETERMINISTIC_SEED);
            
            // Act
            var result = roller.Roll2d6("test");
            
            // Assert
            AssertValidDiceResult(result, is1d6: false);
            Assert.That(result.RollType, Is.EqualTo("test"), "Roll type should be preserved");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_Have1d6Properties_When_Rolling1d6()
        {
            // Arrange
            var roller = new DiceRoller(DETERMINISTIC_SEED);
            
            // Act
            var result = roller.Roll1d6("movement");
            
            // Assert
            Assert.That(result.Is1d6, Is.True, "Should be identified as 1d6");
            Assert.That(result.Is2d6, Is.False, "Should not be identified as 2d6");
            Assert.That(result.Die1, Is.InRange(1, 6), "Die1 should be 1-6");
            Assert.That(result.Die2, Is.EqualTo(0), "Die2 should be 0 for 1d6");
            Assert.That(result.Total, Is.EqualTo(result.Die1), "Total should equal Die1 for 1d6");
            Assert.That(result.Total, Is.InRange(1, 6), "1d6 total should be 1-6");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_Have2d6Properties_When_Rolling2d6()
        {
            // Arrange
            var roller = new DiceRoller(DETERMINISTIC_SEED);
            
            // Act
            var result = roller.Roll2d6("attack");
            
            // Assert
            Assert.That(result.Is1d6, Is.False, "Should not be identified as 1d6");
            Assert.That(result.Is2d6, Is.True, "Should be identified as 2d6");
            Assert.That(result.Die1, Is.InRange(1, 6), "Die1 should be 1-6");
            Assert.That(result.Die2, Is.InRange(1, 6), "Die2 should be 1-6");
            Assert.That(result.Total, Is.EqualTo(result.Die1 + result.Die2), "Total should equal Die1 + Die2");
            Assert.That(result.Total, Is.InRange(2, 12), "2d6 total should be 2-12");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_ProduceDifferentResults_When_RollingMultipleTimes()
        {
            // Arrange
            var roller = new DiceRoller(DETERMINISTIC_SEED);
            var results = new List<DiceResult>();
            
            // Act - Roll multiple times
            for (int i = 0; i < 10; i++)
            {
                results.Add(roller.Roll2d6($"test{i}"));
            }
            
            // Assert - Should get different results (extremely unlikely to get all same)
            var uniqueTotals = results.Select(r => r.Total).Distinct().Count();
            Assert.That(uniqueTotals, Is.GreaterThan(1), 
                       "Multiple rolls should produce different totals (statistical certainty)");
            
            // All results should be valid
            foreach (var result in results)
            {
                AssertValidDiceResult(result, is1d6: false);
            }
        }
        
        [Test]
        [Category("Unit")]
        public void Should_ProduceIdenticalSequences_When_UsingSameSeed()
        {
            // Arrange
            const int testSeed = 123;
            var roller1 = new DiceRoller(testSeed);
            var roller2 = new DiceRoller(testSeed);
            
            // Act - Roll same number of times with both rollers
            var results1 = new List<DiceResult>();
            var results2 = new List<DiceResult>();
            
            for (int i = 0; i < 5; i++)
            {
                results1.Add(roller1.Roll2d6("sync"));
                results2.Add(roller2.Roll2d6("sync"));
            }
            
            // Assert - All corresponding rolls should be identical
            for (int i = 0; i < 5; i++)
            {
                Assert.That(results2[i].Die1, Is.EqualTo(results1[i].Die1), 
                           $"Die1 should match for roll {i}");
                Assert.That(results2[i].Die2, Is.EqualTo(results1[i].Die2), 
                           $"Die2 should match for roll {i}");
                Assert.That(results2[i].Total, Is.EqualTo(results1[i].Total), 
                           $"Total should match for roll {i}");
            }
        }
        
        [Test]
        [Category("Unit")]
        public void Should_ProduceDifferentSequences_When_UsingDifferentSeeds()
        {
            // Arrange
            var roller1 = new DiceRoller(100);
            var roller2 = new DiceRoller(200);
            
            // Act
            var result1 = roller1.Roll2d6("different");
            var result2 = roller2.Roll2d6("different");
            
            // Assert - Very unlikely to be the same (but not impossible)
            // Test multiple aspects to make statistical failure extremely unlikely
            var differences = 0;
            if (result1.Die1 != result2.Die1) differences++;
            if (result1.Die2 != result2.Die2) differences++;
            if (result1.Total != result2.Total) differences++;
            
            Assert.That(differences, Is.GreaterThan(0), 
                       "Different seeds should produce different results (statistical certainty)");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_HandleDefaultRollType_When_NoTypeSpecified()
        {
            // Arrange
            var roller = new DiceRoller(DETERMINISTIC_SEED);
            
            // Act
            var result1d6 = roller.Roll1d6();
            var result2d6 = roller.Roll2d6();
            
            // Assert
            Assert.That(result1d6.RollType, Is.EqualTo("generic"), "Should default to 'generic'");
            Assert.That(result2d6.RollType, Is.EqualTo("generic"), "Should default to 'generic'");
            AssertValidDiceResult(result1d6, is1d6: true);
            AssertValidDiceResult(result2d6, is1d6: false);
        }
        
        [Test]
        [Category("Unit")]
        [TestCaseSource(nameof(RollTypeTestCases))]
        public void Should_PreserveRollType_When_SpecifyingCustomType(string rollType)
        {
            // Arrange
            var roller = new DiceRoller(DETERMINISTIC_SEED);
            
            // Act
            var result = roller.Roll2d6(rollType);
            
            // Assert
            Assert.That(result.RollType, Is.EqualTo(rollType), $"Roll type should be '{rollType}'");
            AssertValidDiceResult(result, is1d6: false);
        }
        
        [Test]
        [Category("Unit")]
        public void Should_FormatCorrectly_When_Using1d6ToString()
        {
            // Arrange
            var roller = new DiceRoller(DETERMINISTIC_SEED);
            var result = roller.Roll1d6("format_test");
            
            // Act
            var formatted = result.ToString();
            
            // Assert
            var expectedFormat = $"[{result.Die1}] = {result.Total}";
            Assert.That(formatted, Is.EqualTo(expectedFormat), "1d6 should format as [X] = X");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_FormatCorrectly_When_Using2d6ToString()
        {
            // Arrange
            var roller = new DiceRoller(DETERMINISTIC_SEED);
            var result = roller.Roll2d6("format_test");
            
            // Act
            var formatted = result.ToString();
            
            // Assert
            var expectedFormat = $"[{result.Die1}+{result.Die2}] = {result.Total}";
            Assert.That(formatted, Is.EqualTo(expectedFormat), "2d6 should format as [X+Y] = Z");
        }
        
        [Test]
        [Category("Performance")]
        public void Should_RollFast_When_PerformingManyRolls()
        {
            // Arrange
            var roller = new DiceRoller();
            const int rollCount = 10000;
            
            // Act
            var startTime = DateTime.UtcNow;
            
            for (int i = 0; i < rollCount; i++)
            {
                roller.Roll2d6("performance");
            }
            
            var elapsed = DateTime.UtcNow - startTime;
            
            // Assert - Should complete many rolls quickly
            Assert.That(elapsed.TotalMilliseconds, Is.LessThan(1000), 
                       $"10,000 rolls should complete in under 1 second, took {elapsed.TotalMilliseconds}ms");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_ProduceStatisticallyValid1d6Distribution_When_RollingManyTimes()
        {
            // Arrange
            var roller = new DiceRoller();
            const int trials = 6000; // 1000 per face for good statistics
            var results = new int[7]; // Index 0 unused, 1-6 for die faces
            
            // Act
            for (int i = 0; i < trials; i++)
            {
                var result = roller.Roll1d6("stats");
                results[result.Total]++;
            }
            
            // Assert - Each face should appear roughly 1000 times (±200 tolerance)
            for (int face = 1; face <= 6; face++)
            {
                var count = results[face];
                var expectedCount = trials / 6.0; // ~1000
                var tolerance = expectedCount * 0.2; // ±20% tolerance
                
                Assert.That(count, Is.InRange(expectedCount - tolerance, expectedCount + tolerance),
                           $"Face {face} should appear ~{expectedCount} times, got {count}");
            }
            
            // Verify no invalid results
            Assert.That(results[0], Is.EqualTo(0), "Should have no results for face 0");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_ProduceStatisticallyValid2d6Distribution_When_RollingManyTimes()
        {
            // Arrange
            var roller = new DiceRoller();
            const int trials = 10000;
            var results = new int[13]; // Index 0-1 unused, 2-12 for 2d6 totals
            
            // Act
            for (int i = 0; i < trials; i++)
            {
                var result = roller.Roll2d6("stats");
                results[result.Total]++;
            }
            
            // Assert - Check that 7 (most common) appears most frequently
            var count7 = results[7];
            var count2 = results[2];  // Least common (only 1+1)
            var count12 = results[12]; // Least common (only 6+6)
            
            Assert.That(count7, Is.GreaterThan(count2), "7 should be more common than 2");
            Assert.That(count7, Is.GreaterThan(count12), "7 should be more common than 12");
            
            // 7 should be roughly 6 times more common than 2 or 12
            // (7 has 6 ways to roll: 1+6,2+5,3+4,4+3,5+2,6+1 vs 2 has 1 way: 1+1)
            var ratio = (double)count7 / count2;
            Assert.That(ratio, Is.InRange(3.0, 9.0), 
                       $"7 should be ~6x more common than 2, ratio was {ratio:F2}");
            
            // Verify no invalid results
            Assert.That(results[0], Is.EqualTo(0), "Should have no results for total 0");
            Assert.That(results[1], Is.EqualTo(0), "Should have no results for total 1");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_Have2d6AverageNear7_When_RollingManyTimes()
        {
            // Arrange & Act - Using statistical assertion from TestBase
            AssertDiceStatistics(
                rollFunction: () => new DiceRoller().Roll2d6("average_test"),
                trials: 5000,
                expectedAverage: 7.0,
                tolerance: 0.2
            );
        }
        
        [Test]
        [Category("Unit")]
        public void Should_Have1d6AverageNear3Point5_When_RollingManyTimes()
        {
            // Arrange & Act - Using statistical assertion from TestBase
            AssertDiceStatistics(
                rollFunction: () => new DiceRoller().Roll1d6("average_test"),
                trials: 5000,
                expectedAverage: 3.5,
                tolerance: 0.2
            );
        }
        
        // =============================================================================
        // EDGE CASES AND BOUNDARY CONDITIONS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_HandleNullRollType_When_PassingNull()
        {
            // Arrange
            var roller = new DiceRoller(DETERMINISTIC_SEED);
            
            // Act & Assert - Should not throw
            var result1d6 = roller.Roll1d6(null);
            var result2d6 = roller.Roll2d6(null);
            
            // Should preserve null roll type (actual behavior)
            Assert.That(result1d6.RollType, Is.Null, "Null roll type should be preserved");
            Assert.That(result2d6.RollType, Is.Null, "Null roll type should be preserved");
            AssertValidDiceResult(result1d6, is1d6: true);
            AssertValidDiceResult(result2d6, is1d6: false);
        }
        
        [Test]
        [Category("Unit")]
        public void Should_HandleEmptyRollType_When_PassingEmptyString()
        {
            // Arrange
            var roller = new DiceRoller(DETERMINISTIC_SEED);
            
            // Act
            var result = roller.Roll2d6("");
            
            // Assert
            Assert.That(result.RollType, Is.EqualTo(""), "Empty string should be preserved");
            AssertValidDiceResult(result, is1d6: false);
        }
        
        [Test]
        [Category("Unit")]
        public void Should_HandleVeryLongRollType_When_PassingLongString()
        {
            // Arrange
            var roller = new DiceRoller(DETERMINISTIC_SEED);
            var longType = new string('X', 1000); // 1000 character string
            
            // Act
            var result = roller.Roll2d6(longType);
            
            // Assert
            Assert.That(result.RollType, Is.EqualTo(longType), "Long roll type should be preserved");
            AssertValidDiceResult(result, is1d6: false);
        }
        
        // =============================================================================
        // TEST DATA
        // =============================================================================
        
        /// <summary>
        /// Test cases for different roll types used in the game
        /// </summary>
        private static readonly object[] RollTypeTestCases = 
        {
            new object[] { "ATK" },
            new object[] { "DEF" },
            new object[] { "MOV" },
            new object[] { "EVASION" },
            new object[] { "COUNTER" },
            new object[] { "Simple Move" },
            new object[] { "Dash Move" },
            new object[] { "attack" },
            new object[] { "defense" },
            new object[] { "movement" },
            new object[] { "test_roll_123" },
            new object[] { "Special!@#$%^&*()" }, // Special characters
            new object[] { "   spaced   " }, // Whitespace
        };
    }
}