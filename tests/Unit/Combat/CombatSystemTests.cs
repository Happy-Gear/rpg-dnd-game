using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RPGGame.Characters;
using RPGGame.Combat;
using RPGGame.Dice;
using RPGGame.Tests.TestHelpers;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Tests for CombatSystem - the core combat resolution engine
    /// CRITICAL SYSTEM: All combat mechanics flow through here
    /// Tests attack resolution, defense choices, counter-attacks, and stamina management
    /// This is the most complex business logic in the game
    /// </summary>
    [TestFixture]
    public class CombatSystemTests : TestBase
    {
        private CombatSystem _combatSystem;
        private Character _attacker;
        private Character _defender;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            // Use deterministic dice for predictable tests
            _combatSystem = new CombatSystem(DETERMINISTIC_SEED);
            
            // Create standard test characters
            _attacker = new CharacterTestBuilder()
                .WithName("Attacker")
                .WithStats(str: 3, end: 2, cha: 5, intel: 5, agi: 1, wis: 5)
                .WithHealth(20)
                .WithStamina(10)
                .Build();
                
            _defender = new CharacterTestBuilder()
                .WithName("Defender")
                .WithStats(str: 1, end: 4, cha: 5, intel: 5, agi: 2, wis: 5)
                .WithHealth(20)
                .WithStamina(10)
                .Build();
        }
        
        // =============================================================================
        // CONSTRUCTOR TESTS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_CreateCombatSystem_When_UsingDefaultConstructor()
        {
            // Arrange & Act
            var combatSystem = new CombatSystem();
            
            // Assert
            Assert.That(combatSystem, Is.Not.Null);
            Assert.That(combatSystem.DiceRoller, Is.Not.Null);
        }
        
        [Test]
        [Category("Unit")]
        public void Should_CreateDeterministicCombatSystem_When_UsingSeed()
        {
            // Arrange & Act
            var combatSystem = new CombatSystem(42);
            
            // Assert
            Assert.That(combatSystem, Is.Not.Null);
            Assert.That(combatSystem.DiceRoller, Is.Not.Null);
        }
        
        // =============================================================================
        // EXECUTE ATTACK TESTS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_ExecuteSuccessfulAttack_When_AttackerHasStamina()
        {
            // Arrange
            var initialStamina = _attacker.CurrentStamina;
            
            // Act
            var result = _combatSystem.ExecuteAttack(_attacker, _defender);
            
            // Assert
            Assert.That(result.Success, Is.True, "Attack should succeed");
            Assert.That(result.Attacker, Is.EqualTo("Attacker"));
            Assert.That(result.Defender, Is.EqualTo("Defender"));
            Assert.That(result.AttackRoll, Is.Not.Null, "Should have dice roll");
            Assert.That(result.BaseAttackDamage, Is.GreaterThan(0), "Should calculate damage");
            Assert.That(result.IsCounterAttack, Is.False, "Should not be counter");
            Assert.That(_attacker.CurrentStamina, Is.EqualTo(initialStamina - 3), "Should consume 3 stamina");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_CalculateCorrectDamage_When_AttackRollsAndModifiersApplied()
        {
            // Arrange - Attacker has 3 STR (ATK)
            
            // Act
            var result = _combatSystem.ExecuteAttack(_attacker, _defender);
            
            // Assert
            var expectedMinDamage = 2 + 3;  // Min roll (1+1) + 3 ATK = 5
            var expectedMaxDamage = 12 + 3; // Max roll (6+6) + 3 ATK = 15
            
            Assert.That(result.BaseAttackDamage, Is.InRange(expectedMinDamage, expectedMaxDamage),
                       $"Damage should be dice roll + ATK modifier (3)");
            Assert.That(result.BaseAttackDamage, Is.EqualTo(result.AttackRoll.Total + 3),
                       "Damage should equal roll + ATK");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_FailAttack_When_InsufficientStamina()
        {
            // Arrange
            _attacker.UseStamina(8); // Leave only 2 stamina (need 3)
            
            // Act
            var result = _combatSystem.ExecuteAttack(_attacker, _defender);
            
            // Assert
            Assert.That(result.Success, Is.False, "Attack should fail");
            Assert.That(result.Message, Does.Contain("cannot attack"));
            Assert.That(result.Message, Does.Contain("stamina"));
            Assert.That(_attacker.CurrentStamina, Is.EqualTo(2), "Stamina should not change");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_FailAttack_When_AttackerIsDead()
        {
            // Arrange
            _attacker.TakeDamage(20); // Kill attacker
            
            // Act
            var result = _combatSystem.ExecuteAttack(_attacker, _defender);
            
            // Assert
            Assert.That(result.Success, Is.False, "Dead attacker cannot attack");
            Assert.That(result.Message, Does.Contain("cannot attack"));
            Assert.That(_attacker.IsAlive, Is.False);
        }
        
        [Test]
        [Category("Unit")]
        public void Should_ConsumeExactStamina_When_AttackSucceeds()
        {
            // Arrange
            var initialStamina = _attacker.CurrentStamina;
            
            // Act
            _combatSystem.ExecuteAttack(_attacker, _defender);
            
            // Assert
            Assert.That(_attacker.CurrentStamina, Is.EqualTo(initialStamina - 3),
                       "Attack should consume exactly 3 stamina");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_AllowAttackOnDeadTarget_When_TargetIsDead()
        {
            // Note: The attack can be executed, but defense handling is separate
            // This allows for overkill scenarios
            
            // Arrange
            _defender.TakeDamage(20); // Kill defender
            
            // Act
            var result = _combatSystem.ExecuteAttack(_attacker, _defender);
            
            // Assert
            Assert.That(result.Success, Is.True, "Can attack dead target");
            Assert.That(result.Defender, Is.EqualTo("Defender"));
        }
        
        // =============================================================================
        // RESOLVE DEFENSE TESTS - DEFEND CHOICE
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_BlockDamageWithDefend_When_DefenseRollHigher()
        {
            // Arrange
            var attack = new AttackResult
            {
                Success = true,
                BaseAttackDamage = 5,
                Attacker = "Attacker",
                Defender = "Defender"
            };
            
            // Act
            var defense = _combatSystem.ResolveDefense(_defender, attack, DefenseChoice.Defend);
            
            // Assert
            Assert.That(defense.DefenseChoice, Is.EqualTo(DefenseChoice.Defend));
            Assert.That(defense.DefenseRoll, Is.Not.Null, "Should have defense roll");
            Assert.That(defense.TotalDefense, Is.GreaterThanOrEqualTo(defense.DefenseRoll.Total),
                       "Total defense should include roll + DEF modifier");
            Assert.That(_defender.CurrentStamina, Is.EqualTo(8), "Should consume 2 stamina");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_TakePartialDamage_When_DefenseRollLower()
        {
            // Arrange
            var attack = new AttackResult
            {
                Success = true,
                BaseAttackDamage = 15, // High damage
                Attacker = "Attacker",
                Defender = "Defender"
            };
            var initialHealth = _defender.CurrentHealth;
            
            // Act
            var defense = _combatSystem.ResolveDefense(_defender, attack, DefenseChoice.Defend);
            
            // Assert
            if (defense.FinalDamage > 0)
            {
                Assert.That(_defender.CurrentHealth, Is.LessThan(initialHealth),
                           "Should take damage when defense insufficient");
                Assert.That(_defender.CurrentHealth, Is.EqualTo(initialHealth - defense.FinalDamage),
                           "Health reduction should match final damage");
            }
        }
        
        [Test]
        [Category("Unit")]
        public void Should_BuildCounter_When_OverDefending()
        {
            // Arrange
            var attack = new AttackResult
            {
                Success = true,
                BaseAttackDamage = 3, // Low damage for over-defense
                Attacker = "Attacker", 
                Defender = "Defender"
            };
            var initialCounter = _defender.Counter.Current;
            
            // Act
            var defense = _combatSystem.ResolveDefense(_defender, attack, DefenseChoice.Defend);
            
            // Assert
            if (defense.TotalDefense > attack.BaseAttackDamage)
            {
                Assert.That(defense.CounterBuilt, Is.GreaterThan(0),
                           "Over-defense should build counter");
                
                // Counter should increase, but cap at 6
                var expectedCounter = Math.Min(6, initialCounter + defense.CounterBuilt);
                Assert.That(_defender.Counter.Current, Is.EqualTo(expectedCounter),
                           "Counter gauge should increase by amount built (capped at 6)");
                
                Assert.That(_defender.Counter.Current, Is.LessThanOrEqualTo(6),
                           "Counter should never exceed maximum of 6");
            }
        }
        
        [Test]
        [Category("Unit")]
        public void Should_CapCounterAtSix_When_OverDefendingBeyondMax()
        {
            // Arrange
            _defender.Counter.AddCounter(5); // Almost full
            var attack = new AttackResult
            {
                Success = true,
                BaseAttackDamage = 2, // Low for over-defense
                Attacker = "Attacker",
                Defender = "Defender"
            };
            
            // Act
            var defense = _combatSystem.ResolveDefense(_defender, attack, DefenseChoice.Defend);
            
            // Assert
            Assert.That(_defender.Counter.Current, Is.LessThanOrEqualTo(6),
                       "Counter should cap at 6");
            if (_defender.Counter.Current == 6)
            {
                Assert.That(_defender.Counter.IsReady, Is.True,
                           "Counter should be ready at 6");
            }
        }
        
        [Test]
        [Category("Unit")]
        public void Should_FailDefend_When_InsufficientStamina()
        {
            // Arrange
            _defender.UseStamina(9); // Leave only 1 stamina (need 2)
            var attack = new AttackResult
            {
                Success = true,
                BaseAttackDamage = 10,
                Attacker = "Attacker",
                Defender = "Defender"
            };
            
            // Act
            var defense = _combatSystem.ResolveDefense(_defender, attack, DefenseChoice.Defend);
            
            // Assert
            Assert.That(defense.FinalDamage, Is.EqualTo(10),
                       "Should take full damage when can't defend");
            Assert.That(_defender.CurrentStamina, Is.EqualTo(1),
                       "Stamina should not change");
        }
        
        // =============================================================================
        // RESOLVE DEFENSE TESTS - MOVE/EVADE CHOICE
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_EvadeDamage_When_EvasionRollHigher()
        {
            // Arrange
            var attack = new AttackResult
            {
                Success = true,
                BaseAttackDamage = 5, // Low damage for successful evasion
                Attacker = "Attacker",
                Defender = "Defender"
            };
            var initialHealth = _defender.CurrentHealth;
            
            // Act
            var defense = _combatSystem.ResolveDefense(_defender, attack, DefenseChoice.Move);
            
            // Assert
            Assert.That(defense.DefenseChoice, Is.EqualTo(DefenseChoice.Move));
            Assert.That(defense.DefenseRoll, Is.Not.Null, "Should have evasion roll");
            Assert.That(_defender.CurrentStamina, Is.EqualTo(9), "Should consume 1 stamina");
            
            // Check if evasion succeeded
            if (defense.CanMove)
            {
                Assert.That(defense.FinalDamage, Is.EqualTo(0), "Should avoid all damage");
                Assert.That(_defender.CurrentHealth, Is.EqualTo(initialHealth), "Health unchanged");
                Assert.That(defense.MovementDistance, Is.GreaterThan(0), "Should have movement");
            }
        }
        
        [Test]
        [Category("Unit")]
        public void Should_TakeDamage_When_EvasionFails()
        {
            // Arrange
            var attack = new AttackResult
            {
                Success = true,
                BaseAttackDamage = 20, // High damage, hard to evade
                Attacker = "Attacker",
                Defender = "Defender"
            };
            var initialHealth = _defender.CurrentHealth;
            
            // Act
            var defense = _combatSystem.ResolveDefense(_defender, attack, DefenseChoice.Move);
            
            // Assert
            if (!defense.CanMove)
            {
                Assert.That(defense.FinalDamage, Is.GreaterThan(0),
                           "Should take damage on failed evasion");
                Assert.That(_defender.CurrentHealth, Is.LessThan(initialHealth),
                           "Health should decrease");
            }
        }
        
        [Test]
        [Category("Unit")]
        public void Should_NotBuildCounter_When_Evading()
        {
            // Arrange
            var attack = new AttackResult
            {
                Success = true,
                BaseAttackDamage = 5,
                Attacker = "Attacker",
                Defender = "Defender"
            };
            
            // Act
            var defense = _combatSystem.ResolveDefense(_defender, attack, DefenseChoice.Move);
            
            // Assert
            Assert.That(defense.CounterBuilt, Is.EqualTo(0),
                       "Evasion should not build counter");
        }
        
        // =============================================================================
        // RESOLVE DEFENSE TESTS - TAKE DAMAGE CHOICE
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_TakeFullDamage_When_ChoosingTakeDamage()
        {
            // Arrange
            var attack = new AttackResult
            {
                Success = true,
                BaseAttackDamage = 8,
                Attacker = "Attacker",
                Defender = "Defender"
            };
            var initialHealth = _defender.CurrentHealth;
            var initialStamina = _defender.CurrentStamina;
            
            // Act
            var defense = _combatSystem.ResolveDefense(_defender, attack, DefenseChoice.TakeDamage);
            
            // Assert
            Assert.That(defense.DefenseChoice, Is.EqualTo(DefenseChoice.TakeDamage));
            Assert.That(defense.FinalDamage, Is.EqualTo(8), "Should take full damage");
            Assert.That(_defender.CurrentHealth, Is.EqualTo(initialHealth - 8));
            Assert.That(_defender.CurrentStamina, Is.EqualTo(initialStamina),
                       "Should not consume stamina");
            Assert.That(defense.CounterBuilt, Is.EqualTo(0),
                       "Should not build counter");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_PreserveCounter_When_TakingDamage()
        {
            // Arrange
            _defender.Counter.AddCounter(3);
            var attack = new AttackResult
            {
                Success = true,
                BaseAttackDamage = 5,
                Attacker = "Attacker",
                Defender = "Defender"
            };
            
            // Act
            _combatSystem.ResolveDefense(_defender, attack, DefenseChoice.TakeDamage);
            
            // Assert
            Assert.That(_defender.Counter.Current, Is.EqualTo(3),
                       "Counter should be preserved when taking damage");
        }
        
        // =============================================================================
        // COUNTER ATTACK TESTS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_ExecuteCounterAttack_When_CounterReady()
        {
            // Arrange
            var counterAttacker = new CharacterTestBuilder()
                .WithName("Counter")
                .WithStats(str: 4, end: 2, cha: 5, intel: 5, agi: 1, wis: 5)
                .WithCounterReady()
                .Build();
            
            // Act
            var result = _combatSystem.ExecuteCounterAttack(counterAttacker, _defender);
            
            // Assert
            Assert.That(result.Success, Is.True, "Counter should succeed");
            Assert.That(result.IsCounterAttack, Is.True, "Should be marked as counter");
            Assert.That(result.Attacker, Is.EqualTo("Counter"));
            Assert.That(result.Defender, Is.EqualTo("Defender"));
            Assert.That(counterAttacker.Counter.Current, Is.EqualTo(0),
                       "Counter should be consumed");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_BypassDefense_When_CounterAttacking()
        {
            // Arrange
            var counterAttacker = new CharacterTestBuilder()
                .WithName("Counter")
                .WithCounterReady()
                .Build();
            var initialHealth = _defender.CurrentHealth;
            
            // Act
            var result = _combatSystem.ExecuteCounterAttack(counterAttacker, _defender);
            
            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(_defender.CurrentHealth, Is.LessThan(initialHealth),
                       "Counter should deal immediate damage");
            Assert.That(_defender.CurrentHealth, Is.EqualTo(initialHealth - result.BaseAttackDamage),
                       "Damage should bypass defense");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_NotConsumeStamina_When_CounterAttacking()
        {
            // Arrange
            var counterAttacker = new CharacterTestBuilder()
                .WithName("Counter")
                .WithStamina(5)
                .WithCounterReady()
                .BuildWithCurrentStamina(3);
            
            // Act
            _combatSystem.ExecuteCounterAttack(counterAttacker, _defender);
            
            // Assert
            Assert.That(counterAttacker.CurrentStamina, Is.EqualTo(3),
                       "Counter attack should be free (no stamina cost)");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_FailCounterAttack_When_CounterNotReady()
        {
            // Arrange
            var notReady = new CharacterTestBuilder()
                .WithName("NotReady")
                .WithCounter(3) // Only 3/6
                .Build();
            
            // Act
            var result = _combatSystem.ExecuteCounterAttack(notReady, _defender);
            
            // Assert
            Assert.That(result.Success, Is.False, "Should fail without ready counter");
            Assert.That(result.Message, Does.Contain("not ready"));
            Assert.That(notReady.Counter.Current, Is.EqualTo(3),
                       "Counter should not change");
        }
        
        // =============================================================================
        // COMBAT HISTORY TESTS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_LogCombatActions_When_ExecutingAttacks()
        {
            // Arrange & Act
            _combatSystem.ExecuteAttack(_attacker, _defender);
            var history = _combatSystem.GetCombatHistory();
            
            // Assert
            Assert.That(history, Is.Not.Empty, "Should have combat log");
            var lastLog = history.Last();
            Assert.That(lastLog.Action, Is.EqualTo(CombatAction.Attack));
            Assert.That(lastLog.ActorName, Is.EqualTo("Attacker"));
            Assert.That(lastLog.TargetName, Is.EqualTo("Defender"));
            Assert.That(lastLog.StaminaCost, Is.EqualTo(3));
        }
        
        [Test]
        [Category("Unit")]
        public void Should_LimitHistorySize_When_ManyActionsOccur()
        {
            // Arrange & Act - Execute many actions
            for (int i = 0; i < 20; i++)
            {
                if (_attacker.CurrentStamina >= 3)
                {
                    _combatSystem.ExecuteAttack(_attacker, _defender);
                }
                _attacker.RestoreStamina(3);
            }
            
            var history = _combatSystem.GetCombatHistory(10);
            
            // Assert
            Assert.That(history.Count, Is.EqualTo(10),
                       "Should return requested number of recent entries");
        }
        
        // =============================================================================
        // INTEGRATION SCENARIOS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_HandleFullCombatExchange_When_AttackDefendCounter()
        {
            // Arrange - Build counter through multiple exchanges
            var attacker = new CharacterTestBuilder()
                .WithName("Alice")
                .WithStats(str: 2, end: 1, cha: 5, intel: 5, agi: 1, wis: 5)
                .WithStamina(20)
                .Build();
                
            var defender = new CharacterTestBuilder()
                .WithName("Bob")
                .WithStats(str: 1, end: 5, cha: 5, intel: 5, agi: 1, wis: 5)
                .WithStamina(20)
                .Build();
            
            // Act - Multiple attacks to build counter
            for (int i = 0; i < 3; i++)
            {
                var attack = _combatSystem.ExecuteAttack(attacker, defender);
                if (attack.Success)
                {
                    _combatSystem.ResolveDefense(defender, attack, DefenseChoice.Defend);
                }
            }
            
            // Assert - Counter should build up
            Assert.That(defender.Counter.Current, Is.GreaterThan(0),
                       "Defender should have built counter");
            
            // If counter is ready, test counter attack
            if (defender.Counter.IsReady)
            {
                var counter = _combatSystem.ExecuteCounterAttack(defender, attacker);
                Assert.That(counter.Success, Is.True, "Counter should succeed");
                Assert.That(counter.IsCounterAttack, Is.True);
            }
        }
        
        [Test]
        [Category("Unit")]
        public void Should_HandleDeathScenario_When_DamageExceedsHealth()
        {
            // Arrange
            var weakDefender = new CharacterTestBuilder()
                .WithName("Weak")
                .WithHealth(5)
                .BuildWithCurrentHealth(3);
            
            var attack = new AttackResult
            {
                Success = true,
                BaseAttackDamage = 10,
                Attacker = "Attacker",
                Defender = "Weak"
            };
            
            // Act
            var defense = _combatSystem.ResolveDefense(weakDefender, attack, DefenseChoice.TakeDamage);
            
            // Assert
            Assert.That(weakDefender.CurrentHealth, Is.EqualTo(0), "Should be dead");
            Assert.That(weakDefender.IsAlive, Is.False);
            Assert.That(defense.FinalDamage, Is.EqualTo(10));
        }
        
        // =============================================================================
        // EDGE CASES AND ERROR HANDLING
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_HandleNullAttacker_When_ExecutingAttack()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                _combatSystem.ExecuteAttack(null, _defender));
        }
        
        [Test]
        [Category("Unit")]
        public void Should_HandleNullDefender_When_ExecutingAttack()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                _combatSystem.ExecuteAttack(_attacker, null));
        }
        
        [Test]
        [Category("Unit")]
        public void Should_HandleExactStamina_When_AttackingWithMinimum()
        {
            // Arrange
            _attacker.UseStamina(7); // Leave exactly 3 stamina
            
            // Act
            var result = _combatSystem.ExecuteAttack(_attacker, _defender);
            
            // Assert
            Assert.That(result.Success, Is.True, "Should succeed with exact stamina");
            Assert.That(_attacker.CurrentStamina, Is.EqualTo(0), "Should use all stamina");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_HandleZeroAttackModifier_When_StrengthIsZero()
        {
            // Arrange
            var weakAttacker = new CharacterTestBuilder()
                .WithName("Weak")
                .WithStrength(0)
                .Build();
            
            // Act
            var result = _combatSystem.ExecuteAttack(weakAttacker, _defender);
            
            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.BaseAttackDamage, Is.EqualTo(result.AttackRoll.Total),
                       "Damage should be just dice roll with 0 ATK");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_HandleHighAttackModifier_When_StrengthIsHigh()
        {
            // Arrange
            var strongAttacker = new CharacterTestBuilder()
                .WithName("Strong")
                .WithStrength(20)
                .Build();
            
            // Act
            var result = _combatSystem.ExecuteAttack(strongAttacker, _defender);
            
            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.BaseAttackDamage, Is.EqualTo(result.AttackRoll.Total + 20),
                       "Damage should include high ATK modifier");
        }
    }
}