using System;
using NUnit.Framework;
using RPGGame.Tests.TestHelpers;
using RPGGame.Characters;
using RPGGame.Grid;
using RPGGame.Dice;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Tests to verify TestBase functionality works correctly
    /// This ensures our test infrastructure is solid before building on it
    /// </summary>
    [TestFixture]
    public class TestBaseTests : TestBase
    {
        [Test]
        [Category("Unit")]
        public void Should_SetupDeterministicDice_When_TestStarts()
        {
            // Arrange & Act - Setup happens in TestBase.SetUp()
            
            // Assert
            Assert.That(DeterministicDice, Is.Not.Null, "Deterministic dice should be initialized");
            Assert.That(RandomDice, Is.Not.Null, "Random dice should be initialized");
            
            // Deterministic dice should give consistent SEQUENCE when restarted with same seed
            var firstRoll = DeterministicDice.Roll2d6("test");
            
            // Create new dice roller with same seed - should get same first result
            var secondDeterministicDice = new DiceRoller(DETERMINISTIC_SEED);
            var secondRoll = secondDeterministicDice.Roll2d6("test");
            
            // Same seed should produce same first roll
            Assert.That(secondRoll.Die1, Is.EqualTo(firstRoll.Die1), "Same seed should give same first Die1");
            Assert.That(secondRoll.Die2, Is.EqualTo(firstRoll.Die2), "Same seed should give same first Die2");
            Assert.That(secondRoll.Total, Is.EqualTo(firstRoll.Total), "Same seed should give same first Total");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_ValidateTestCharacter_When_CreatedWithDefaults()
        {
            // Arrange & Act
            var character = CreateTestCharacter("TestHero");
            
            // Assert
            AssertValidCharacter(character, "TestHero");
            
            // Verify defaults
            Assert.That(character.CurrentHealth, Is.EqualTo(DEFAULT_HEALTH));
            Assert.That(character.CurrentStamina, Is.EqualTo(DEFAULT_STAMINA));
            Assert.That(character.Stats.Strength, Is.EqualTo(1), "Default STR should be 1");
            Assert.That(character.AttackPoints, Is.EqualTo(1), "ATK should equal STR (1)");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_CreateCharacterAtPosition_When_UsingPositionHelper()
        {
            // Arrange & Act
            var character = CreateCharacterAt("Positioned", 5, 7, str: 3, agi: 2);
            
            // Assert
            AssertValidCharacter(character, "Positioned");
            AssertValidPosition(character.Position);
            
            Assert.That(character.Position.X, Is.EqualTo(5));
            Assert.That(character.Position.Y, Is.EqualTo(7));
            Assert.That(character.AttackPoints, Is.EqualTo(3), "ATK should equal STR (3)");
            Assert.That(character.MovementPoints, Is.EqualTo(2), "MOV should equal AGI (2)");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_ModifyCharacterStamina_When_UsingStaminaHelper()
        {
            // Arrange
            var character = CreateTestCharacter("StaminaTest", stamina: 10);
            
            // Act & Assert
            SetCharacterStamina(character, 7);
            Assert.That(character.CurrentStamina, Is.EqualTo(7));
            
            SetCharacterStamina(character, 3);
            Assert.That(character.CurrentStamina, Is.EqualTo(3));
            
            SetCharacterStamina(character, 8);
            Assert.That(character.CurrentStamina, Is.EqualTo(8));
        }
        
        [Test]
        [Category("Unit")]
        public void Should_ModifyCharacterHealth_When_UsingHealthHelper()
        {
            // Arrange
            var character = CreateTestCharacter("HealthTest", health: 20);
            
            // Act & Assert
            SetCharacterHealth(character, 15);
            Assert.That(character.CurrentHealth, Is.EqualTo(15));
            
            SetCharacterHealth(character, 5);
            Assert.That(character.CurrentHealth, Is.EqualTo(5));
            
            SetCharacterHealth(character, 18);
            Assert.That(character.CurrentHealth, Is.EqualTo(18));
        }
        
        [Test]
        [Category("Unit")]
        public void Should_ModifyCounterGauge_When_UsingCounterHelper()
        {
            // Arrange
            var character = CreateTestCharacter("CounterTest");
            
            // Act & Assert
            SetCharacterCounter(character, 3);
            Assert.That(character.Counter.Current, Is.EqualTo(3));
            Assert.That(character.Counter.IsReady, Is.False, "Counter should not be ready at 3");
            
            SetCharacterCounter(character, 6);
            Assert.That(character.Counter.Current, Is.EqualTo(6));
            Assert.That(character.Counter.IsReady, Is.True, "Counter should be ready at 6");
            
            SetCharacterCounter(character, 0);
            Assert.That(character.Counter.Current, Is.EqualTo(0));
            Assert.That(character.Counter.IsReady, Is.False, "Counter should not be ready at 0");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_ValidateDiceResults_When_UsingDiceAssertions()
        {
            // Arrange
            var dice1d6 = DeterministicDice.Roll1d6("test");
            var dice2d6 = DeterministicDice.Roll2d6("test");
            
            // Act & Assert - Should not throw
            AssertValidDiceResult(dice1d6, is1d6: true);
            AssertValidDiceResult(dice2d6, is1d6: false);
            
            // Verify the specific validation works
            Assert.That(dice1d6.Is1d6, Is.True);
            Assert.That(dice1d6.Die2, Is.EqualTo(0));
            
            Assert.That(dice2d6.Is2d6, Is.True);
            Assert.That(dice2d6.Die1, Is.InRange(1, 6));
            Assert.That(dice2d6.Die2, Is.InRange(1, 6));
        }
        
        [Test]
        [Category("Unit")]
        [TestCaseSource(nameof(ValidStatTestCases))]
        public void Should_AcceptValidStats_When_CreatingCharacter(int statValue, string description)
        {
            // Arrange & Act
            var character = CreateTestCharacter("StatTest", str: statValue);
            
            // Assert
            AssertValidCharacter(character);
            Assert.That(character.Stats.Strength, Is.EqualTo(statValue), description);
        }
        
        [Test]
        [Category("Unit")]
        public void Should_HandleCommonPositions_When_Testing()
        {
            // Arrange & Act
            foreach (var position in CommonPositions)
            {
                // Assert
                AssertValidPosition(position, maxX: 16, maxY: 16);
            }
        }
    }
}