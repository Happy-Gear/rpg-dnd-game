using System;
using NUnit.Framework;
using RPGGame.Tests.TestHelpers;
using RPGGame.Characters;
using RPGGame.Grid;

namespace RPGGame.Tests.Unit.TestHelpers
{
    /// <summary>
    /// Tests for CharacterTestBuilder to ensure it creates valid characters
    /// This is critical infrastructure - all other tests depend on this working
    /// </summary>
    [TestFixture]
    public class CharacterTestBuilderTests : TestBase
    {
        [Test]
        [Category("Unit")]
        public void Should_CreateDefaultCharacter_When_BuildingWithNoOptions()
        {
            // Arrange & Act
            var character = new CharacterTestBuilder().Build();
            
            // Assert
            AssertValidCharacter(character, "TestCharacter");
            Assert.That(character.CurrentHealth, Is.EqualTo(10), "Default health should be 10");
            Assert.That(character.CurrentStamina, Is.EqualTo(10), "Default stamina should be 10");
            Assert.That(character.Position.X, Is.EqualTo(0), "Default position X should be 0");
            Assert.That(character.Position.Y, Is.EqualTo(0), "Default position Y should be 0");
            Assert.That(character.Counter.Current, Is.EqualTo(0), "Default counter should be 0");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_SetCharacterName_When_UsingWithName()
        {
            // Arrange & Act
            var character = new CharacterTestBuilder()
                .WithName("CustomName")
                .Build();
            
            // Assert
            AssertValidCharacter(character, "CustomName");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_ThrowException_When_NameIsNull()
        {
            // Arrange
            var builder = new CharacterTestBuilder();
            
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => builder.WithName(null));
        }
        
        [Test]
        [Category("Unit")]
        public void Should_SetAllStats_When_UsingWithStats()
        {
            // Arrange & Act
            var character = new CharacterTestBuilder()
                .WithStats(str: 5, end: 4, cha: 3, intel: 2, agi: 1, wis: 6)
                .Build();
            
            // Assert
            AssertValidCharacter(character);
            Assert.That(character.Stats.Strength, Is.EqualTo(5));
            Assert.That(character.Stats.Endurance, Is.EqualTo(4));
            Assert.That(character.Stats.Charisma, Is.EqualTo(3));
            Assert.That(character.Stats.Intelligence, Is.EqualTo(2));
            Assert.That(character.Stats.Agility, Is.EqualTo(1));
            Assert.That(character.Stats.Wisdom, Is.EqualTo(6));
            
            // Verify derived stats
            Assert.That(character.AttackPoints, Is.EqualTo(5), "ATK should equal STR");
            Assert.That(character.DefensePoints, Is.EqualTo(4), "DEF should equal END");
            Assert.That(character.MovementPoints, Is.EqualTo(1), "MOV should equal AGI");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_SetIndividualStats_When_UsingIndividualMethods()
        {
            // Arrange & Act
            var character = new CharacterTestBuilder()
                .WithStrength(7)
                .WithEndurance(6)
                .WithAgility(5)
                .WithCharisma(4)
                .WithIntelligence(3)
                .WithWisdom(2)
                .Build();
            
            // Assert
            AssertValidCharacter(character);
            Assert.That(character.Stats.Strength, Is.EqualTo(7));
            Assert.That(character.Stats.Endurance, Is.EqualTo(6));
            Assert.That(character.Stats.Agility, Is.EqualTo(5));
            Assert.That(character.Stats.Charisma, Is.EqualTo(4));
            Assert.That(character.Stats.Intelligence, Is.EqualTo(3));
            Assert.That(character.Stats.Wisdom, Is.EqualTo(2));
        }
        
        [Test]
        [Category("Unit")]
        public void Should_SetHealthValues_When_UsingWithHealth()
        {
            // Arrange & Act
            var character = new CharacterTestBuilder()
                .WithHealth(maxHealth: 25)
                .Build();
            
            // Assert
            AssertValidCharacter(character);
            Assert.That(character.MaxHealth, Is.EqualTo(25));
            Assert.That(character.CurrentHealth, Is.EqualTo(25), "Current health should default to max");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_ThrowException_When_HealthIsZeroOrNegative()
        {
            // Arrange
            var builder = new CharacterTestBuilder();
            
            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.WithHealth(0));
            Assert.Throws<ArgumentException>(() => builder.WithHealth(-5));
        }
        
        [Test]
        [Category("Unit")]
        public void Should_SetStaminaValues_When_UsingWithStamina()
        {
            // Arrange & Act
            var character = new CharacterTestBuilder()
                .WithStamina(maxStamina: 15)
                .Build();
            
            // Assert
            AssertValidCharacter(character);
            Assert.That(character.MaxStamina, Is.EqualTo(15));
            Assert.That(character.CurrentStamina, Is.EqualTo(15), "Current stamina should default to max");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_ThrowException_When_StaminaIsZeroOrNegative()
        {
            // Arrange
            var builder = new CharacterTestBuilder();
            
            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.WithStamina(0));
            Assert.Throws<ArgumentException>(() => builder.WithStamina(-3));
        }
        
        [Test]
        [Category("Unit")]
        public void Should_SetPosition_When_UsingWithPositionCoordinates()
        {
            // Arrange & Act
            var character = new CharacterTestBuilder()
                .WithPosition(x: 7, y: 11, z: 2)
                .Build();
            
            // Assert
            AssertValidCharacter(character);
            AssertValidPosition(character.Position);
            Assert.That(character.Position.X, Is.EqualTo(7));
            Assert.That(character.Position.Y, Is.EqualTo(11));
            Assert.That(character.Position.Z, Is.EqualTo(2));
        }
        
        [Test]
        [Category("Unit")]
        public void Should_SetPosition_When_UsingWithPositionObject()
        {
            // Arrange
            var position = new Position(3, 4, 1);
            
            // Act
            var character = new CharacterTestBuilder()
                .WithPosition(position)
                .Build();
            
            // Assert
            AssertValidCharacter(character);
            Assert.That(character.Position, Is.EqualTo(position));
        }
        
        [Test]
        [Category("Unit")]
        public void Should_ThrowException_When_PositionIsNull()
        {
            // Arrange
            var builder = new CharacterTestBuilder();
            
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => builder.WithPosition((Position)null));
        }
        
        [Test]
        [Category("Unit")]
        public void Should_SetCounterGauge_When_UsingWithCounter()
        {
            // Arrange & Act
            var character = new CharacterTestBuilder()
                .WithCounter(4)
                .Build();
            
            // Assert
            AssertValidCharacter(character);
            Assert.That(character.Counter.Current, Is.EqualTo(4));
            Assert.That(character.Counter.IsReady, Is.False, "Counter should not be ready at 4");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_SetCounterToReady_When_UsingWithCounterReady()
        {
            // Arrange & Act
            var character = new CharacterTestBuilder()
                .WithCounterReady()
                .Build();
            
            // Assert
            AssertValidCharacter(character);
            Assert.That(character.Counter.Current, Is.EqualTo(6));
            Assert.That(character.Counter.IsReady, Is.True, "Counter should be ready at 6");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_ThrowException_When_CounterIsNegative()
        {
            // Arrange
            var builder = new CharacterTestBuilder();
            
            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.WithCounter(-1));
        }
        
        [Test]
        [Category("Unit")]
        public void Should_BuildWithCurrentHealth_When_UsingBuildWithCurrentHealth()
        {
            // Arrange & Act
            var character = new CharacterTestBuilder()
                .WithHealth(20)
                .BuildWithCurrentHealth(12);
            
            // Assert
            AssertValidCharacter(character);
            Assert.That(character.MaxHealth, Is.EqualTo(20));
            Assert.That(character.CurrentHealth, Is.EqualTo(12));
        }
        
        [Test]
        [Category("Unit")]
        public void Should_BuildWithCurrentStamina_When_UsingBuildWithCurrentStamina()
        {
            // Arrange & Act
            var character = new CharacterTestBuilder()
                .WithStamina(15)
                .BuildWithCurrentStamina(8);
            
            // Assert
            AssertValidCharacter(character);
            Assert.That(character.MaxStamina, Is.EqualTo(15));
            Assert.That(character.CurrentStamina, Is.EqualTo(8));
        }
        
        [Test]
        [Category("Unit")]
        public void Should_BuildWithCurrentResources_When_UsingBuildWithCurrentResources()
        {
            // Arrange & Act
            var character = new CharacterTestBuilder()
                .WithHealth(25)
                .WithStamina(20)
                .BuildWithCurrentResources(currentHealth: 18, currentStamina: 12);
            
            // Assert
            AssertValidCharacter(character);
            Assert.That(character.MaxHealth, Is.EqualTo(25));
            Assert.That(character.CurrentHealth, Is.EqualTo(18));
            Assert.That(character.MaxStamina, Is.EqualTo(20));
            Assert.That(character.CurrentStamina, Is.EqualTo(12));
        }
        
        [Test]
        [Category("Unit")]
        public void Should_ChainMethodCalls_When_UsingFluentInterface()
        {
            // Arrange & Act - Demonstrate fluent interface
            var character = new CharacterTestBuilder()
                .WithName("FluentTest")
                .WithStats(str: 3, end: 2, cha: 1, intel: 4, agi: 5, wis: 6)
                .WithHealth(15)
                .WithStamina(12)
                .WithPosition(9, 7)
                .WithCounter(3)
                .Build();
            
            // Assert
            AssertValidCharacter(character, "FluentTest");
            Assert.That(character.AttackPoints, Is.EqualTo(3));
            Assert.That(character.DefensePoints, Is.EqualTo(2));
            Assert.That(character.MovementPoints, Is.EqualTo(5));
            Assert.That(character.MaxHealth, Is.EqualTo(15));
            Assert.That(character.MaxStamina, Is.EqualTo(12));
            Assert.That(character.Position.X, Is.EqualTo(9));
            Assert.That(character.Position.Y, Is.EqualTo(7));
            Assert.That(character.Counter.Current, Is.EqualTo(3));
        }
    }
    
    /// <summary>
    /// Tests for CommonCharacters pre-configured builders
    /// </summary>
    [TestFixture]
    public class CommonCharactersTests : TestBase
    {
        [Test]
        [Category("Unit")]
        public void Should_CreateAlice_When_UsingCommonCharacter()
        {
            // Arrange & Act
            var alice = CommonCharacters.Alice().Build();
            
            // Assert
            AssertValidCharacter(alice, "Alice");
            Assert.That(alice.AttackPoints, Is.EqualTo(1), "Alice ATK should be 1");
            Assert.That(alice.DefensePoints, Is.EqualTo(0), "Alice DEF should be 0");
            Assert.That(alice.MovementPoints, Is.EqualTo(1), "Alice MOV should be 1");
            Assert.That(alice.Position.X, Is.EqualTo(2));
            Assert.That(alice.Position.Y, Is.EqualTo(2));
        }
        
        [Test]
        [Category("Unit")]
        public void Should_CreateBob_When_UsingCommonCharacter()
        {
            // Arrange & Act
            var bob = CommonCharacters.Bob().Build();
            
            // Assert
            AssertValidCharacter(bob, "Bob");
            Assert.That(bob.AttackPoints, Is.EqualTo(2), "Bob ATK should be 2");
            Assert.That(bob.DefensePoints, Is.EqualTo(0), "Bob DEF should be 0");
            Assert.That(bob.MovementPoints, Is.EqualTo(0), "Bob MOV should be 0");
            Assert.That(bob.Position.X, Is.EqualTo(5));
            Assert.That(bob.Position.Y, Is.EqualTo(5));
        }
        
        [Test]
        [Category("Unit")]
        public void Should_CreateTank_When_UsingCommonCharacter()
        {
            // Arrange & Act
            var tank = CommonCharacters.Tank().Build();
            
            // Assert
            AssertValidCharacter(tank, "Tank");
            Assert.That(tank.AttackPoints, Is.EqualTo(0), "Tank ATK should be 0");
            Assert.That(tank.DefensePoints, Is.EqualTo(3), "Tank DEF should be 3");
            Assert.That(tank.MovementPoints, Is.EqualTo(0), "Tank MOV should be 0");
            Assert.That(tank.MaxHealth, Is.EqualTo(20), "Tank should have high health");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_CreateCounterReady_When_UsingCommonCharacter()
        {
            // Arrange & Act
            var counterReady = CommonCharacters.CounterReady().Build();
            
            // Assert
            AssertValidCharacter(counterReady, "CounterReady");
            Assert.That(counterReady.Counter.IsReady, Is.True, "Counter should be ready");
            Assert.That(counterReady.Counter.Current, Is.EqualTo(6));
        }
        
        [Test]
        [Category("Unit")]
        public void Should_AllowModificationOfCommonCharacters_When_ChainedWithOtherMethods()
        {
            // Arrange & Act - Modify Alice to have different health
            var modifiedAlice = CommonCharacters.Alice()
                .WithHealth(25)
                .WithCounter(4)
                .Build();
            
            // Assert
            AssertValidCharacter(modifiedAlice, "Alice");
            Assert.That(modifiedAlice.MaxHealth, Is.EqualTo(25), "Should use modified health");
            Assert.That(modifiedAlice.Counter.Current, Is.EqualTo(4), "Should use modified counter");
            Assert.That(modifiedAlice.AttackPoints, Is.EqualTo(1), "Should keep Alice's ATK");
        }
        
        [Test]
        [Category("Unit")]
        [TestCaseSource(nameof(AllCommonCharacterBuilders))]
        public void Should_CreateValidCharacter_When_UsingAnyCommonCharacterBuilder(Func<CharacterTestBuilder> builderFactory, string expectedName)
        {
            // Arrange & Act
            var character = builderFactory().Build();
            
            // Assert
            AssertValidCharacter(character, expectedName);
        }
        
        /// <summary>
        /// Test data for parameterized testing of all common character builders
        /// </summary>
        private static readonly object[] AllCommonCharacterBuilders = 
        {
            new object[] { (Func<CharacterTestBuilder>)(() => CommonCharacters.Alice()), "Alice" },
            new object[] { (Func<CharacterTestBuilder>)(() => CommonCharacters.Bob()), "Bob" },
            new object[] { (Func<CharacterTestBuilder>)(() => CommonCharacters.Tank()), "Tank" },
            new object[] { (Func<CharacterTestBuilder>)(() => CommonCharacters.Scout()), "Scout" },
            new object[] { (Func<CharacterTestBuilder>)(() => CommonCharacters.GlassCannon()), "GlassCannon" },
            new object[] { (Func<CharacterTestBuilder>)(() => CommonCharacters.Weakened()), "Weakened" },
            new object[] { (Func<CharacterTestBuilder>)(() => CommonCharacters.CounterReady()), "CounterReady" },
            new object[] { (Func<CharacterTestBuilder>)(() => CommonCharacters.Minimal()), "Minimal" }
        };
    }
}