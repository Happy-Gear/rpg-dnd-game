using System;
using NUnit.Framework;
using RPGGame.Characters;
using RPGGame.Grid;
using RPGGame.Tests.TestHelpers;

namespace RPGGame.Tests.Unit.Characters
{
    /// <summary>
    /// Tests for Character class - the core domain model of the game
    /// Critical system: All combat, movement, and gameplay revolves around characters
    /// Tests business rules, derived stats, resource management, and state validation
    /// </summary>
    [TestFixture]
    public class CharacterTests : TestBase
    {
        // =============================================================================
        // CONSTRUCTION AND INITIALIZATION TESTS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_CreateValidCharacter_When_UsingConstructor()
        {
            // Arrange
            var stats = new CharacterStats(str: 3, end: 2, cha: 1, intel: 4, agi: 5, wis: 6);
            
            // Act
            var character = new Character("TestHero", stats, health: 15, stamina: 12);
            
            // Assert
            AssertValidCharacter(character, "TestHero");
            Assert.That(character.MaxHealth, Is.EqualTo(15));
            Assert.That(character.CurrentHealth, Is.EqualTo(15));
            Assert.That(character.MaxStamina, Is.EqualTo(12));
            Assert.That(character.CurrentStamina, Is.EqualTo(12));
        }
        
        [Test]
        [Category("Unit")]
        public void Should_UseDefaultResources_When_NotSpecified()
        {
            // Arrange
            var stats = new CharacterStats();
            
            // Act
            var character = new Character("DefaultChar", stats);
            
            // Assert
            AssertValidCharacter(character, "DefaultChar");
            Assert.That(character.MaxHealth, Is.EqualTo(100), "Default max health should be 100");
            Assert.That(character.CurrentHealth, Is.EqualTo(100), "Default current health should be 100");
            Assert.That(character.MaxStamina, Is.EqualTo(20), "Default max stamina should be 20");
            Assert.That(character.CurrentStamina, Is.EqualTo(20), "Default current stamina should be 20");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_GenerateUniqueIds_When_CreatingMultipleCharacters()
        {
            // Arrange & Act
            var char1 = CreateTestCharacter("Char1");
            var char2 = CreateTestCharacter("Char2");
            var char3 = CreateTestCharacter("Char3");
            
            // Assert
            Assert.That(char1.Id, Is.Not.EqualTo(char2.Id), "Characters should have unique IDs");
            Assert.That(char1.Id, Is.Not.EqualTo(char3.Id), "Characters should have unique IDs");
            Assert.That(char2.Id, Is.Not.EqualTo(char3.Id), "Characters should have unique IDs");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_SetDefaultPosition_When_Created()
        {
            // Arrange & Act
            var character = CreateTestCharacter("PositionTest");
            
            // Assert
            AssertValidPosition(character.Position);
            Assert.That(character.Position.X, Is.EqualTo(0), "Default X position should be 0");
            Assert.That(character.Position.Y, Is.EqualTo(0), "Default Y position should be 0");
            Assert.That(character.Position.Z, Is.EqualTo(0), "Default Z position should be 0");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_InitializeCounterToZero_When_Created()
        {
            // Arrange & Act
            var character = CreateTestCharacter("CounterTest");
            
            // Assert
            Assert.That(character.Counter, Is.Not.Null, "Counter should be initialized");
            Assert.That(character.Counter.Current, Is.EqualTo(0), "Counter should start at 0");
            Assert.That(character.Counter.IsReady, Is.False, "Counter should not be ready initially");
        }
        
        // =============================================================================
        // DERIVED STAT CALCULATION TESTS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_CalculateAttackFromStrength_When_AccessingAttackPoints()
        {
            // Arrange & Act
            var character = new CharacterTestBuilder()
                .WithStrength(7)
                .Build();
            
            // Assert
            Assert.That(character.AttackPoints, Is.EqualTo(7), "Attack points should equal Strength");
            Assert.That(character.Stats.Strength, Is.EqualTo(7), "Strength should be preserved");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_CalculateDefenseFromEndurance_When_AccessingDefensePoints()
        {
            // Arrange & Act
            var character = new CharacterTestBuilder()
                .WithEndurance(5)
                .Build();
            
            // Assert
            Assert.That(character.DefensePoints, Is.EqualTo(5), "Defense points should equal Endurance");
            Assert.That(character.Stats.Endurance, Is.EqualTo(5), "Endurance should be preserved");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_CalculateMovementFromAgility_When_AccessingMovementPoints()
        {
            // Arrange & Act
            var character = new CharacterTestBuilder()
                .WithAgility(3)
                .Build();
            
            // Assert
            Assert.That(character.MovementPoints, Is.EqualTo(3), "Movement points should equal Agility");
            Assert.That(character.Stats.Agility, Is.EqualTo(3), "Agility should be preserved");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_HandleZeroStats_When_CalculatingDerivedValues()
        {
            // Arrange & Act
            var character = new CharacterTestBuilder()
                .WithStats(str: 0, end: 0, cha: 0, intel: 0, agi: 0, wis: 0)
                .Build();
            
            // Assert
            Assert.That(character.AttackPoints, Is.EqualTo(0), "Zero strength should give zero attack");
            Assert.That(character.DefensePoints, Is.EqualTo(0), "Zero endurance should give zero defense");
            Assert.That(character.MovementPoints, Is.EqualTo(0), "Zero agility should give zero movement");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_HandleHighStats_When_CalculatingDerivedValues()
        {
            // Arrange & Act
            var character = new CharacterTestBuilder()
                .WithStats(str: 25, end: 30, cha: 10, intel: 15, agi: 20, wis: 12)
                .Build();
            
            // Assert
            Assert.That(character.AttackPoints, Is.EqualTo(25), "High strength should be preserved");
            Assert.That(character.DefensePoints, Is.EqualTo(30), "High endurance should be preserved");
            Assert.That(character.MovementPoints, Is.EqualTo(20), "High agility should be preserved");
        }
        
        // =============================================================================
        // STAMINA MANAGEMENT TESTS (Critical for Action Economy)
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_UseStamina_When_HasSufficientStamina()
        {
            // Arrange
            var character = new CharacterTestBuilder()
                .WithStamina(10)
                .Build();
            
            // Act
            var result = character.UseStamina(3);
            
            // Assert
            Assert.That(result, Is.True, "Should successfully use stamina");
            Assert.That(character.CurrentStamina, Is.EqualTo(7), "Should have 7 stamina remaining (10-3)");
            Assert.That(character.MaxStamina, Is.EqualTo(10), "Max stamina should not change");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_NotUseStamina_When_InsufficientStamina()
        {
            // Arrange
            var character = new CharacterTestBuilder()
                .WithStamina(5)
                .BuildWithCurrentStamina(2); // Only 2 current stamina
            
            // Act
            var result = character.UseStamina(3); // Try to use 3
            
            // Assert
            Assert.That(result, Is.False, "Should fail to use stamina");
            Assert.That(character.CurrentStamina, Is.EqualTo(2), "Stamina should not change");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_UseExactStamina_When_UsingAllRemainingStamina()
        {
            // Arrange
            var character = new CharacterTestBuilder()
                .WithStamina(10)
                .BuildWithCurrentStamina(3);
            
            // Act
            var result = character.UseStamina(3); // Use exactly what remains
            
            // Assert
            Assert.That(result, Is.True, "Should successfully use all remaining stamina");
            Assert.That(character.CurrentStamina, Is.EqualTo(0), "Should have 0 stamina remaining");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_HandleZeroStamina_When_AlreadyExhausted()
        {
            // Arrange
            var character = new CharacterTestBuilder()
                .WithStamina(10)
                .BuildWithCurrentStamina(0);
            
            // Act
            var result = character.UseStamina(1);
            
            // Assert
            Assert.That(result, Is.False, "Should not be able to use stamina when exhausted");
            Assert.That(character.CurrentStamina, Is.EqualTo(0), "Should remain at 0 stamina");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_RestoreStamina_When_BelowMaximum()
        {
            // Arrange
            var character = new CharacterTestBuilder()
                .WithStamina(15)
                .BuildWithCurrentStamina(5);
            
            // Act
            character.RestoreStamina(7);
            
            // Assert
            Assert.That(character.CurrentStamina, Is.EqualTo(12), "Should restore to 12 (5+7)");
            Assert.That(character.MaxStamina, Is.EqualTo(15), "Max stamina should not change");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_CapAtMaximum_When_RestoringStaminaAboveMax()
        {
            // Arrange
            var character = new CharacterTestBuilder()
                .WithStamina(10)
                .BuildWithCurrentStamina(8);
            
            // Act
            character.RestoreStamina(5); // Would be 13, but max is 10
            
            // Assert
            Assert.That(character.CurrentStamina, Is.EqualTo(10), "Should cap at maximum stamina");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_HandleZeroRestore_When_RestoringZeroStamina()
        {
            // Arrange
            var character = new CharacterTestBuilder()
                .WithStamina(10)
                .BuildWithCurrentStamina(6);
            
            // Act
            character.RestoreStamina(0);
            
            // Assert
            Assert.That(character.CurrentStamina, Is.EqualTo(6), "Should not change when restoring 0");
        }
        
        // =============================================================================
        // HEALTH MANAGEMENT TESTS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_TakeDamage_When_DamageApplied()
        {
            // Arrange
            var character = new CharacterTestBuilder()
                .WithHealth(20)
                .Build();
            
            // Act
            character.TakeDamage(7);
            
            // Assert
            Assert.That(character.CurrentHealth, Is.EqualTo(13), "Should have 13 health remaining (20-7)");
            Assert.That(character.MaxHealth, Is.EqualTo(20), "Max health should not change");
            Assert.That(character.IsAlive, Is.True, "Should still be alive");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_NotGoBelowZero_When_DamageExceedsHealth()
        {
            // Arrange
            var character = new CharacterTestBuilder()
                .WithHealth(10)
                .BuildWithCurrentHealth(3);
            
            // Act
            character.TakeDamage(8); // More damage than remaining health
            
            // Assert
            Assert.That(character.CurrentHealth, Is.EqualTo(0), "Health should not go below 0");
            Assert.That(character.IsAlive, Is.False, "Should be dead");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_HandleZeroDamage_When_NoDamageApplied()
        {
            // Arrange
            var character = new CharacterTestBuilder()
                .WithHealth(15)
                .Build();
            
            // Act
            character.TakeDamage(0);
            
            // Assert
            Assert.That(character.CurrentHealth, Is.EqualTo(15), "Health should not change");
            Assert.That(character.IsAlive, Is.True, "Should remain alive");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_HealDamage_When_BelowMaximum()
        {
            // Arrange
            var character = new CharacterTestBuilder()
                .WithHealth(25)
                .BuildWithCurrentHealth(10);
            
            // Act
            character.Heal(8);
            
            // Assert
            Assert.That(character.CurrentHealth, Is.EqualTo(18), "Should heal to 18 (10+8)");
            Assert.That(character.MaxHealth, Is.EqualTo(25), "Max health should not change");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_CapAtMaximum_When_HealingAboveMax()
        {
            // Arrange
            var character = new CharacterTestBuilder()
                .WithHealth(20)
                .BuildWithCurrentHealth(18);
            
            // Act
            character.Heal(5); // Would be 23, but max is 20
            
            // Assert
            Assert.That(character.CurrentHealth, Is.EqualTo(20), "Should cap at maximum health");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_ReviveCharacter_When_HealingFromZero()
        {
            // Arrange
            var character = new CharacterTestBuilder()
                .WithHealth(15)
                .BuildWithCurrentHealth(0); // Dead
            
            // Act
            character.Heal(5);
            
            // Assert
            Assert.That(character.CurrentHealth, Is.EqualTo(5), "Should heal to 5");
            Assert.That(character.IsAlive, Is.True, "Should be alive again");
        }
        
        // =============================================================================
        // POSITION MANAGEMENT TESTS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_UpdatePosition_When_SettingNewPosition()
        {
            // Arrange
            var character = CreateTestCharacter("MoveTest");
            var newPosition = new Position(7, 9, 2);
            
            // Act
            character.Position = newPosition;
            
            // Assert
            Assert.That(character.Position, Is.EqualTo(newPosition), "Position should be updated");
            Assert.That(character.Position.X, Is.EqualTo(7));
            Assert.That(character.Position.Y, Is.EqualTo(9));
            Assert.That(character.Position.Z, Is.EqualTo(2));
        }
        
        [Test]
        [Category("Unit")]
        public void Should_AllowNullPosition_When_SettingPosition()
        {
            // Arrange
            var character = CreateTestCharacter("NullPosTest");
            
            // Act & Assert - Should not throw
            character.Position = null;
            Assert.That(character.Position, Is.Null);
        }
        
        // =============================================================================
        // CHARACTER STATE TESTS (IsAlive, CanAct)
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_BeAlive_When_HealthAboveZero()
        {
            // Arrange & Act
            var character = new CharacterTestBuilder()
                .WithHealth(20)
                .BuildWithCurrentHealth(1); // Just barely alive
            
            // Assert
            Assert.That(character.IsAlive, Is.True, "Should be alive with any health > 0");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_BeDead_When_HealthIsZero()
        {
            // Arrange & Act
            var character = new CharacterTestBuilder()
                .WithHealth(20)
                .BuildWithCurrentHealth(0);
            
            // Assert
            Assert.That(character.IsAlive, Is.False, "Should be dead with 0 health");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_CanAct_When_AliveAndHasStamina()
        {
            // Arrange & Act
            var character = new CharacterTestBuilder()
                .WithHealth(15)
                .WithStamina(10)
                .BuildWithCurrentResources(currentHealth: 5, currentStamina: 3);
            
            // Assert
            Assert.That(character.CanAct, Is.True, "Should be able to act when alive and has stamina");
            Assert.That(character.IsAlive, Is.True, "Should be alive");
            Assert.That(character.CurrentStamina, Is.GreaterThan(0), "Should have stamina");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_NotCanAct_When_Dead()
        {
            // Arrange & Act
            var character = new CharacterTestBuilder()
                .WithHealth(15)
                .WithStamina(10)
                .BuildWithCurrentResources(currentHealth: 0, currentStamina: 8); // Dead but has stamina
            
            // Assert
            Assert.That(character.CanAct, Is.False, "Should not be able to act when dead");
            Assert.That(character.IsAlive, Is.False, "Should be dead");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_NotCanAct_When_AliveButNoStamina()
        {
            // Arrange & Act
            var character = new CharacterTestBuilder()
                .WithHealth(15)
                .WithStamina(10)
                .BuildWithCurrentResources(currentHealth: 5, currentStamina: 0); // Alive but exhausted
            
            // Assert
            Assert.That(character.CanAct, Is.False, "Should not be able to act when exhausted");
            Assert.That(character.IsAlive, Is.True, "Should be alive");
            Assert.That(character.CurrentStamina, Is.EqualTo(0), "Should have no stamina");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_NotCanAct_When_DeadAndNoStamina()
        {
            // Arrange & Act
            var character = new CharacterTestBuilder()
                .WithHealth(15)
                .WithStamina(10)
                .BuildWithCurrentResources(currentHealth: 0, currentStamina: 0); // Dead and exhausted
            
            // Assert
            Assert.That(character.CanAct, Is.False, "Should not be able to act when dead and exhausted");
            Assert.That(character.IsAlive, Is.False, "Should be dead");
            Assert.That(character.CurrentStamina, Is.EqualTo(0), "Should have no stamina");
        }
        
        // =============================================================================
        // COUNTER GAUGE INTEGRATION TESTS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_HaveWorkingCounter_When_CharacterCreated()
        {
            // Arrange & Act
            var character = CreateTestCharacter("CounterGaugeTest");
            
            // Assert
            Assert.That(character.Counter, Is.Not.Null, "Counter should exist");
            Assert.That(character.Counter.Current, Is.EqualTo(0), "Counter should start at 0");
            Assert.That(character.Counter.Maximum, Is.EqualTo(6), "Counter maximum should be 6");
            Assert.That(character.Counter.IsReady, Is.False, "Counter should not be ready initially");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_BuildCounter_When_AddingCounterPoints()
        {
            // Arrange
            var character = CreateTestCharacter("CounterBuildTest");
            
            // Act
            character.Counter.AddCounter(4);
            
            // Assert
            Assert.That(character.Counter.Current, Is.EqualTo(4), "Counter should have 4 points");
            Assert.That(character.Counter.IsReady, Is.False, "Counter should not be ready at 4");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_BeCounterReady_When_ReachingSixPoints()
        {
            // Arrange
            var character = CreateTestCharacter("CounterReadyTest");
            
            // Act
            character.Counter.AddCounter(6);
            
            // Assert
            Assert.That(character.Counter.Current, Is.EqualTo(6), "Counter should have 6 points");
            Assert.That(character.Counter.IsReady, Is.True, "Counter should be ready at 6");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_ConsumeCounter_When_UsingCounterAttack()
        {
            // Arrange
            var character = new CharacterTestBuilder()
                .WithCounterReady() // Counter at 6
                .Build();
            
            // Act
            var consumed = character.Counter.ConsumeCounter();
            
            // Assert
            Assert.That(consumed, Is.True, "Should successfully consume counter");
            Assert.That(character.Counter.Current, Is.EqualTo(0), "Counter should be reset to 0");
            Assert.That(character.Counter.IsReady, Is.False, "Counter should no longer be ready");
        }
        
        // =============================================================================
        // COMMON CHARACTERS INTEGRATION TESTS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_CreateAliceCorrectly_When_UsingCommonCharacters()
        {
            // Arrange & Act
            var alice = CommonCharacters.Alice().Build();
            
            // Assert
            AssertValidCharacter(alice, "Alice");
            Assert.That(alice.AttackPoints, Is.EqualTo(1), "Alice should have 1 ATK (STR)");
            Assert.That(alice.DefensePoints, Is.EqualTo(0), "Alice should have 0 DEF (END)");
            Assert.That(alice.MovementPoints, Is.EqualTo(1), "Alice should have 1 MOV (AGI)");
            Assert.That(alice.Position.X, Is.EqualTo(2), "Alice should start at (2,2)");
            Assert.That(alice.Position.Y, Is.EqualTo(2));
            Assert.That(alice.CanAct, Is.True, "Alice should be able to act");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_CreateBobCorrectly_When_UsingCommonCharacters()
        {
            // Arrange & Act
            var bob = CommonCharacters.Bob().Build();
            
            // Assert
            AssertValidCharacter(bob, "Bob");
            Assert.That(bob.AttackPoints, Is.EqualTo(2), "Bob should have 2 ATK (STR)");
            Assert.That(bob.DefensePoints, Is.EqualTo(0), "Bob should have 0 DEF (END)");
            Assert.That(bob.MovementPoints, Is.EqualTo(0), "Bob should have 0 MOV (AGI)");
            Assert.That(bob.Position.X, Is.EqualTo(5), "Bob should start at (5,5)");
            Assert.That(bob.Position.Y, Is.EqualTo(5));
            Assert.That(bob.CanAct, Is.True, "Bob should be able to act");
        }
        
        // =============================================================================
        // STRING REPRESENTATION TESTS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_FormatCorrectly_When_UsingToString()
        {
            // Arrange
            var character = new CharacterTestBuilder()
                .WithName("TestChar")
                .WithHealth(25)
                .WithStamina(15)
                .BuildWithCurrentResources(currentHealth: 18, currentStamina: 10);
            
            // Act
            var formatted = character.ToString();
            
            // Assert
            Assert.That(formatted, Is.EqualTo("TestChar (HP: 18/25, SP: 10/15)"), 
                       "Should format as 'Name (HP: current/max, SP: current/max)'");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_FormatDeadCharacter_When_UsingToString()
        {
            // Arrange
            var character = new CharacterTestBuilder()
                .WithName("DeadChar")
                .WithHealth(20)
                .WithStamina(12)
                .BuildWithCurrentResources(currentHealth: 0, currentStamina: 5);
            
            // Act
            var formatted = character.ToString();
            
            // Assert
            Assert.That(formatted, Is.EqualTo("DeadChar (HP: 0/20, SP: 5/12)"), 
                       "Should show 0 HP for dead character");
        }
        
        // =============================================================================
        // EDGE CASES AND BOUNDARY CONDITIONS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_HandleMaxValues_When_UsingLargeNumbers()
        {
            // Arrange & Act
            var character = new CharacterTestBuilder()
                .WithName("MaxChar")
                .WithStats(str: 999, end: 999, cha: 999, intel: 999, agi: 999, wis: 999)
                .WithHealth(10000)
                .WithStamina(5000)
                .Build();
            
            // Assert
            AssertValidCharacter(character, "MaxChar");
            Assert.That(character.AttackPoints, Is.EqualTo(999), "Should handle large attack values");
            Assert.That(character.DefensePoints, Is.EqualTo(999), "Should handle large defense values");
            Assert.That(character.MovementPoints, Is.EqualTo(999), "Should handle large movement values");
            Assert.That(character.MaxHealth, Is.EqualTo(10000), "Should handle large health");
            Assert.That(character.MaxStamina, Is.EqualTo(5000), "Should handle large stamina");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_HandleMinValues_When_UsingMinimalStats()
        {
            // Arrange & Act
            var character = CommonCharacters.Minimal().Build();
            
            // Assert
            AssertValidCharacter(character, "Minimal");
            Assert.That(character.AttackPoints, Is.EqualTo(0), "Minimal ATK should be 0");
            Assert.That(character.DefensePoints, Is.EqualTo(0), "Minimal DEF should be 0");
            Assert.That(character.MovementPoints, Is.EqualTo(0), "Minimal MOV should be 0");
            Assert.That(character.MaxHealth, Is.EqualTo(1), "Minimal health should be 1");
            Assert.That(character.MaxStamina, Is.EqualTo(1), "Minimal stamina should be 1");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_HandleLongNames_When_CreatingCharacter()
        {
            // Arrange
            var longName = new string('X', 1000); // 1000 character name
            
            // Act & Assert - Should not throw
            var character = CreateTestCharacter(longName);
            AssertValidCharacter(character, longName);
            Assert.That(character.Name, Is.EqualTo(longName), "Should preserve long names");
        }
        
        [Test]
        [Category("Unit")]
        [TestCaseSource(nameof(EdgeCaseResourceTestCases))]
        public void Should_HandleResourceEdgeCases_When_UsingBoundaryValues(
            int health, int stamina, string description)
        {
            // Arrange & Act
            var character = new CharacterTestBuilder()
                .WithName("EdgeCase")
                .WithHealth(health)
                .WithStamina(stamina)
                .Build();
            
            // Assert
            AssertValidCharacter(character, "EdgeCase");
            Assert.That(character.MaxHealth, Is.EqualTo(health), $"{description} - Health");
            Assert.That(character.MaxStamina, Is.EqualTo(stamina), $"{description} - Stamina");
        }
        
        // =============================================================================
        // TEST DATA
        // =============================================================================
        
        /// <summary>
        /// Edge case test data for resource boundary testing
        /// </summary>
        private static readonly object[] EdgeCaseResourceTestCases = 
        {
            new object[] { 1, 1, "Minimum viable resources" },
            new object[] { 100, 20, "Default resources" },
            new object[] { 500, 100, "High resources" },
            new object[] { 1000, 500, "Very high resources" },
            new object[] { 1, 100, "Low health, high stamina" },
            new object[] { 100, 1, "High health, low stamina" },
        };
    }
}