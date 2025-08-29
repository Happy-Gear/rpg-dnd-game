using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RPGGame.Characters;
using RPGGame.Combat;
using RPGGame.Dice;
using RPGGame.Grid;

namespace RPGGame.Tests.TestHelpers
{
    /// <summary>
    /// Base class for all tests - provides common setup, utilities, and custom assertions
    /// Following coding standards: clear, consistent, testable
    /// </summary>
    [TestFixture]
    public abstract class TestBase
    {
        // Common test constants
        protected const int DETERMINISTIC_SEED = 42;
        protected const int DEFAULT_HEALTH = 10;
        protected const int DEFAULT_STAMINA = 10;
        
        // Common test objects - initialized fresh for each test
        protected DiceRoller DeterministicDice { get; private set; }
        protected DiceRoller RandomDice { get; private set; }
        
        /// <summary>
        /// Setup run before each test method
        /// </summary>
        [SetUp]
        public virtual void SetUp()
        {
            // Create deterministic dice roller for predictable tests
            DeterministicDice = new DiceRoller(DETERMINISTIC_SEED);
            
            // Create random dice roller for statistical tests
            RandomDice = new DiceRoller();
            
            // Hook for derived classes
            OnSetUp();
        }
        
        /// <summary>
        /// Cleanup run after each test method
        /// </summary>
        [TearDown]
        public virtual void TearDown()
        {
            // Hook for derived classes
            OnTearDown();
        }
        
        /// <summary>
        /// Override in derived classes for additional setup
        /// </summary>
        protected virtual void OnSetUp() { }
        
        /// <summary>
        /// Override in derived classes for additional cleanup
        /// </summary>
        protected virtual void OnTearDown() { }
        
        // =============================================================================
        // CUSTOM ASSERTIONS - Game-Specific Validation
        // =============================================================================
        
        /// <summary>
        /// Assert that a DiceResult is valid (following game rules)
        /// </summary>
        protected void AssertValidDiceResult(DiceResult result, bool is1d6 = false)
        {
            Assert.That(result, Is.Not.Null, "DiceResult should not be null");
            
            if (is1d6)
            {
                Assert.That(result.Is1d6, Is.True, "Should be 1d6 roll");
                Assert.That(result.Die1, Is.InRange(1, 6), "Die1 should be 1-6");
                Assert.That(result.Die2, Is.EqualTo(0), "Die2 should be 0 for 1d6");
                Assert.That(result.Total, Is.EqualTo(result.Die1), "Total should equal Die1 for 1d6");
            }
            else
            {
                Assert.That(result.Is2d6, Is.True, "Should be 2d6 roll");
                Assert.That(result.Die1, Is.InRange(1, 6), "Die1 should be 1-6");
                Assert.That(result.Die2, Is.InRange(1, 6), "Die2 should be 1-6");
                Assert.That(result.Total, Is.EqualTo(result.Die1 + result.Die2), "Total should equal Die1 + Die2");
                Assert.That(result.Total, Is.InRange(2, 12), "2d6 total should be 2-12");
            }
        }
        
        /// <summary>
        /// Assert that a Character is in a valid state
        /// </summary>
        protected void AssertValidCharacter(Character character, string expectedName = null)
        {
            Assert.That(character, Is.Not.Null, "Character should not be null");
            Assert.That(character.Name, Is.Not.Null.And.Not.Empty, "Character must have a name");
            
            if (expectedName != null)
            {
                Assert.That(character.Name, Is.EqualTo(expectedName), $"Character name should be {expectedName}");
            }
            
            // Health validation
            Assert.That(character.CurrentHealth, Is.GreaterThanOrEqualTo(0), "Health cannot be negative");
            Assert.That(character.CurrentHealth, Is.LessThanOrEqualTo(character.MaxHealth), "Current health cannot exceed max health");
            Assert.That(character.MaxHealth, Is.GreaterThan(0), "Max health must be positive");
            
            // Stamina validation
            Assert.That(character.CurrentStamina, Is.GreaterThanOrEqualTo(0), "Stamina cannot be negative");
            Assert.That(character.CurrentStamina, Is.LessThanOrEqualTo(character.MaxStamina), "Current stamina cannot exceed max stamina");
            Assert.That(character.MaxStamina, Is.GreaterThan(0), "Max stamina must be positive");
            
            // Stats validation
            Assert.That(character.Stats, Is.Not.Null, "Character must have stats");
            
            // Position validation
            Assert.That(character.Position, Is.Not.Null, "Character must have a position");
            
            // Counter validation
            Assert.That(character.Counter, Is.Not.Null, "Character must have a counter gauge");
            
            // Derived stats should be calculated correctly
            Assert.That(character.AttackPoints, Is.EqualTo(character.Stats.Strength), "ATK should equal STR");
            Assert.That(character.DefensePoints, Is.EqualTo(character.Stats.Endurance), "DEF should equal END");
            Assert.That(character.MovementPoints, Is.EqualTo(character.Stats.Agility), "MOV should equal AGI");
        }
        
        /// <summary>
        /// Assert that a Position is valid and within expected bounds
        /// </summary>
        protected void AssertValidPosition(Position position, int? maxX = null, int? maxY = null)
        {
            Assert.That(position, Is.Not.Null, "Position should not be null");
            Assert.That(position.X, Is.GreaterThanOrEqualTo(0), "X coordinate cannot be negative");
            Assert.That(position.Y, Is.GreaterThanOrEqualTo(0), "Y coordinate cannot be negative");
            
            if (maxX.HasValue)
            {
                Assert.That(position.X, Is.LessThan(maxX.Value), $"X coordinate should be less than {maxX.Value}");
            }
            
            if (maxY.HasValue)
            {
                Assert.That(position.Y, Is.LessThan(maxY.Value), $"Y coordinate should be less than {maxY.Value}");
            }
        }
        
        /// <summary>
        /// Assert that an AttackResult is valid and contains expected data
        /// </summary>
        protected void AssertValidAttackResult(AttackResult result, bool shouldSucceed = true, string expectedAttacker = null, string expectedDefender = null)
        {
            Assert.That(result, Is.Not.Null, "AttackResult should not be null");
            Assert.That(result.Success, Is.EqualTo(shouldSucceed), $"AttackResult.Success should be {shouldSucceed}");
            
            if (shouldSucceed)
            {
                Assert.That(result.AttackRoll, Is.Not.Null, "Successful attack should have dice roll");
                AssertValidDiceResult(result.AttackRoll, is1d6: false); // Attacks use 2d6
                Assert.That(result.BaseAttackDamage, Is.GreaterThan(0), "Attack damage should be positive");
                
                if (expectedAttacker != null)
                {
                    Assert.That(result.Attacker, Is.EqualTo(expectedAttacker), $"Attacker should be {expectedAttacker}");
                }
                
                if (expectedDefender != null)
                {
                    Assert.That(result.Defender, Is.EqualTo(expectedDefender), $"Defender should be {expectedDefender}");
                }
            }
            else
            {
                Assert.That(result.Message, Is.Not.Null.And.Not.Empty, "Failed attack should have error message");
            }
        }
        
        // =============================================================================
        // UTILITY METHODS - Common Test Operations
        // =============================================================================
        
        /// <summary>
        /// Create a test character with specified stats
        /// </summary>
        protected Character CreateTestCharacter(string name = "TestChar", 
                                               int str = 1, int end = 1, int cha = 1, 
                                               int intel = 1, int agi = 1, int wis = 1,
                                               int health = DEFAULT_HEALTH, int stamina = DEFAULT_STAMINA)
        {
            var stats = new CharacterStats(str, end, cha, intel, agi, wis);
            return new Character(name, stats, health, stamina);
        }
        
        /// <summary>
        /// Create a character positioned at specific coordinates
        /// </summary>
        protected Character CreateCharacterAt(string name, int x, int y, 
                                             int str = 1, int end = 1, int cha = 1, 
                                             int intel = 1, int agi = 1, int wis = 1)
        {
            var character = CreateTestCharacter(name, str, end, cha, intel, agi, wis);
            character.Position = new Position(x, y);
            return character;
        }
        
        /// <summary>
        /// Drain character's stamina to a specific level
        /// </summary>
        protected void SetCharacterStamina(Character character, int targetStamina)
        {
            var staminaDifference = character.CurrentStamina - targetStamina;
            
            if (staminaDifference > 0)
            {
                character.UseStamina(staminaDifference);
            }
            else if (staminaDifference < 0)
            {
                character.RestoreStamina(-staminaDifference);
            }
            
            Assert.That(character.CurrentStamina, Is.EqualTo(targetStamina), 
                       $"Character stamina should be set to {targetStamina}");
        }
        
        /// <summary>
        /// Set character's health to a specific level
        /// </summary>
        protected void SetCharacterHealth(Character character, int targetHealth)
        {
            var healthDifference = character.CurrentHealth - targetHealth;
            
            if (healthDifference > 0)
            {
                character.TakeDamage(healthDifference);
            }
            else if (healthDifference < 0)
            {
                character.Heal(-healthDifference);
            }
            
            Assert.That(character.CurrentHealth, Is.EqualTo(targetHealth), 
                       $"Character health should be set to {targetHealth}");
        }
        
        /// <summary>
        /// Set character's counter gauge to a specific level
        /// </summary>
        protected void SetCharacterCounter(Character character, int targetCounter)
        {
            // Reset counter first
            character.Counter.Reset();
            
            // Add counter points to reach target
            if (targetCounter > 0)
            {
                character.Counter.AddCounter(targetCounter);
            }
            
            Assert.That(character.Counter.Current, Is.EqualTo(targetCounter), 
                       $"Character counter should be set to {targetCounter}");
        }
        
        /// <summary>
        /// Verify that dice statistics are reasonable over many rolls
        /// </summary>
        protected void AssertDiceStatistics(Func<DiceResult> rollFunction, int trials = 1000, 
                                           double expectedAverage = 7.0, double tolerance = 0.5)
        {
            var results = new List<int>();
            
            for (int i = 0; i < trials; i++)
            {
                var result = rollFunction();
                results.Add(result.Total);
            }
            
            var average = results.Sum() / (double)results.Count;
            
            Assert.That(average, Is.EqualTo(expectedAverage).Within(tolerance), 
                       $"Average of {trials} rolls should be approximately {expectedAverage}");
        }
        
        // =============================================================================
        // TEST DATA GENERATION
        // =============================================================================
        
        /// <summary>
        /// Generate test cases for parameterized tests
        /// </summary>
        protected static readonly object[] ValidStatTestCases = 
        {
            new object[] { 0, "Minimum stat value" },
            new object[] { 1, "Low stat value" },
            new object[] { 10, "Default stat value" },
            new object[] { 20, "High stat value" },
            new object[] { 30, "Very high stat value" }
        };
        
        protected static readonly object[] InvalidStatTestCases = 
        {
            new object[] { -1, "Negative stat value" },
            new object[] { -10, "Very negative stat value" }
        };
        
        /// <summary>
        /// Common position test cases for grid testing
        /// </summary>
        protected static readonly Position[] CommonPositions = 
        {
            new Position(0, 0),    // Origin
            new Position(1, 1),    // Adjacent diagonal
            new Position(8, 8),    // Center of 16x16 grid
            new Position(15, 15),  // Far corner
            new Position(7, 3),    // Random valid position
        };
    }
}