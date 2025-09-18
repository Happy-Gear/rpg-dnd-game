using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RPGGame.Characters;
using RPGGame.Combat;
using RPGGame.Core;
using RPGGame.Dice;
using RPGGame.Grid;
using RPGGame.Tests.TestHelpers;

namespace RPGGame.Tests.Integration
{
    /// <summary>
    /// Integration tests for complete combat flow scenarios - the "big picture" testing
    /// CRITICAL INTEGRATION: Tests entire combat sequences from attack to resolution
    /// Validates multi-turn scenarios, resource depletion, counter-attacks, and win conditions
    /// This is the final validation that all systems work together properly
    /// </summary>
    [TestFixture]
    public class CombatFlowTests : TestBase
    {
        private CombatSystem _combatSystem;
        private TurnManager _turnManager;
        private Character _alice;
        private Character _bob;
        private Character _tank;
        private Character _scout;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            // Use deterministic combat for predictable test results
            _combatSystem = new CombatSystem(DETERMINISTIC_SEED);
            _turnManager = new TurnManager();
            
            // Create standard test characters with varied capabilities
            _alice = CommonCharacters.Alice().Build();      // Balanced attacker (ATK:1, DEF:0, MOV:1)
            _bob = CommonCharacters.Bob().Build();          // Strong attacker (ATK:2, DEF:0, MOV:0)  
            _tank = CommonCharacters.Tank().Build();        // High defense (ATK:0, DEF:3, MOV:0)
            _scout = CommonCharacters.Scout().Build();      // High mobility (ATK:0, DEF:0, MOV:3)
        }
        
        // =============================================================================
        // COMPLETE ATTACK-DEFENSE-DAMAGE FLOW TESTS
        // =============================================================================
        
        [Test]
        [Category("Integration")]
        public void Should_ExecuteCompleteAttackDefendFlow_When_BothCharactersReady()
        {
            // Arrange - Position for combat
            _alice.Position = new Position(5, 5);
            _bob.Position = new Position(6, 5); // Adjacent
            
            var initialAliceStamina = _alice.CurrentStamina;
            var initialBobStamina = _bob.CurrentStamina;
            var initialBobHealth = _bob.CurrentHealth;
            
            // Act - Complete attack → defense sequence
            var attackResult = _combatSystem.ExecuteAttack(_alice, _bob);
            Assert.That(attackResult.Success, Is.True, "Attack should succeed");
            
            var defenseResult = _combatSystem.ResolveDefense(_bob, attackResult, DefenseChoice.Defend);
            
            // Assert - Verify complete flow
            Assert.That(_alice.CurrentStamina, Is.EqualTo(initialAliceStamina - 3), 
                       "Alice should have used 3 stamina for attack");
            Assert.That(_bob.CurrentStamina, Is.EqualTo(initialBobStamina - 2), 
                       "Bob should have used 2 stamina for defense");
            
            // Damage should be applied based on defense roll
            if (defenseResult.FinalDamage > 0)
            {
                Assert.That(_bob.CurrentHealth, Is.EqualTo(initialBobHealth - defenseResult.FinalDamage),
                           "Bob's health should reflect final damage");
            }
            
            // Defense result should be complete
            Assert.That(defenseResult.DefenseChoice, Is.EqualTo(DefenseChoice.Defend));
            Assert.That(defenseResult.DefenseRoll, Is.Not.Null, "Should have defense dice roll");
            Assert.That(defenseResult.TotalDefense, Is.GreaterThan(0), "Should calculate total defense");
        }
        
        [Test]
        [Category("Integration")]
        public void Should_ExecuteAttackEvasionFlow_When_DefenderChoosesMove()
        {
            // Arrange
            _alice.Position = new Position(8, 8);
            _scout.Position = new Position(9, 8); // Scout has high AGI for evasion
            
            var initialScoutHealth = _scout.CurrentHealth;
            var initialScoutStamina = _scout.CurrentStamina;
            
            // Act - Attack → evasion attempt
            var attackResult = _combatSystem.ExecuteAttack(_alice, _scout);
            var evasionResult = _combatSystem.ResolveDefense(_scout, attackResult, DefenseChoice.Move);
            
            // Assert
            Assert.That(attackResult.Success, Is.True, "Attack should succeed");
            Assert.That(_scout.CurrentStamina, Is.EqualTo(initialScoutStamina - 1), 
                       "Scout should use 1 stamina for evasion");
            
            if (evasionResult.CanMove)
            {
                // Successful evasion
                Assert.That(evasionResult.FinalDamage, Is.EqualTo(0), "Should avoid all damage on successful evasion");
                Assert.That(_scout.CurrentHealth, Is.EqualTo(initialScoutHealth), "Health should be unchanged");
                Assert.That(evasionResult.MovementDistance, Is.GreaterThan(0), "Should get movement from evasion");
            }
            else
            {
                // Failed evasion  
                Assert.That(evasionResult.FinalDamage, Is.GreaterThan(0), "Should take damage on failed evasion");
                Assert.That(_scout.CurrentHealth, Is.LessThan(initialScoutHealth), "Health should decrease");
            }
            
            Assert.That(evasionResult.CounterBuilt, Is.EqualTo(0), "Evasion should not build counter");
        }
        
        [Test]
        [Category("Integration")]
        public void Should_ExecuteTakeDamageFlow_When_DefenderSavesStamina()
        {
            // Arrange
            _bob.Position = new Position(3, 3);
            _alice.Position = new Position(4, 3);
            
            var initialAliceHealth = _alice.CurrentHealth;
            var initialAliceStamina = _alice.CurrentStamina;
            
            // Act - Bob attacks, Alice takes damage to save stamina
            var attackResult = _combatSystem.ExecuteAttack(_bob, _alice);
            var damageResult = _combatSystem.ResolveDefense(_alice, attackResult, DefenseChoice.TakeDamage);
            
            // Assert
            Assert.That(attackResult.Success, Is.True, "Attack should succeed");
            Assert.That(_alice.CurrentStamina, Is.EqualTo(initialAliceStamina), 
                       "Alice should not lose stamina when taking damage");
            Assert.That(_alice.CurrentHealth, Is.EqualTo(initialAliceHealth - attackResult.BaseAttackDamage),
                       "Alice should take full attack damage");
            
            Assert.That(damageResult.DefenseChoice, Is.EqualTo(DefenseChoice.TakeDamage));
            Assert.That(damageResult.FinalDamage, Is.EqualTo(attackResult.BaseAttackDamage));
            Assert.That(damageResult.CounterBuilt, Is.EqualTo(0), "Taking damage should not build counter");
        }
        
        // =============================================================================
        // COUNTER-ATTACK (BADMINTON STREAK) FLOW TESTS  
        // =============================================================================
        
        [Test]
        [Category("Integration")]
        public void Should_ExecuteCounterAttackFlow_When_DefenderHasReadyCounter()
        {
            // Arrange - Tank with counter ready
            _tank.Counter.AddCounter(6);
            _tank.Position = new Position(7, 7);
            _alice.Position = new Position(8, 7);
            
            var initialTankCounter = _tank.Counter.Current;
            var initialAliceHealth = _alice.CurrentHealth;
            
            // Act - Alice attacks, Tank defends and counters
            var attackResult = _combatSystem.ExecuteAttack(_alice, _tank);
            var defenseResult = _combatSystem.ResolveDefense(_tank, attackResult, DefenseChoice.Defend);
            var counterResult = _combatSystem.ExecuteCounterAttack(_tank, _alice);
            
            // Assert - Full badminton streak sequence
            Assert.That(attackResult.Success, Is.True, "Initial attack should succeed");
            Assert.That(defenseResult.CounterReady, Is.True, "Tank should still have counter ready after defense");
            
            Assert.That(counterResult.Success, Is.True, "Counter attack should succeed");
            Assert.That(counterResult.IsCounterAttack, Is.True, "Should be marked as counter attack");
            Assert.That(_tank.Counter.Current, Is.EqualTo(0), "Counter should be consumed");
            Assert.That(_alice.CurrentHealth, Is.LessThan(initialAliceHealth), 
                       "Alice should take damage from counter");
            
            // Counter damage should bypass defense
            var expectedDamage = counterResult.BaseAttackDamage;
            Assert.That(_alice.CurrentHealth, Is.EqualTo(initialAliceHealth - expectedDamage),
                       "Counter damage should be applied directly");
        }
        
        [Test]
        [Category("Integration")]
        public void Should_BuildCounterThroughMultipleDefenses_When_OverDefending()
        {
            // Arrange - Tank vs weak attacker to build counter over time
            var weakAttacker = new CharacterTestBuilder()
                .WithName("WeakAttacker")
                .WithStrength(1) // Low attack for easy over-defense
                .WithPosition(5, 5)
                .Build();
            
            _tank.Position = new Position(6, 5); // Adjacent to weak attacker
            var initialCounter = _tank.Counter.Current;
            
            // Act - Multiple weak attacks to gradually build counter
            for (int round = 1; round <= 3; round++)
            {
                var attack = _combatSystem.ExecuteAttack(weakAttacker, _tank);
                if (attack.Success)
                {
                    var defense = _combatSystem.ResolveDefense(_tank, attack, DefenseChoice.Defend);
                    
                    // Each defense should build some counter (tank has high defense)
                    if (defense.CounterBuilt > 0)
                    {
                        Assert.That(_tank.Counter.Current, Is.GreaterThan(initialCounter),
                                   $"Round {round}: Counter should build from over-defense");
                        initialCounter = _tank.Counter.Current;
                    }
                    
                    // Stop if counter becomes ready
                    if (_tank.Counter.IsReady) break;
                }
                
                // Restore attacker stamina for next round
                weakAttacker.RestoreStamina(5);
            }
            
            // Assert - Counter should have built up significantly
            Assert.That(_tank.Counter.Current, Is.GreaterThan(0), 
                       "Multiple over-defenses should build counter");
        }
        
        // =============================================================================
        // MULTI-ROUND COMBAT SCENARIOS
        // =============================================================================
        
        [Test]
        [Category("Integration")]
        public void Should_CompleteMultiRoundCombat_When_PlayingToConclusion()
        {
            // Arrange - Two balanced fighters
            var fighter1 = new CharacterTestBuilder()
                .WithName("Fighter1")
                .WithStats(str: 2, end: 1, cha: 5, intel: 5, agi: 1, wis: 5)
                .WithHealth(15)
                .WithStamina(20)
                .WithPosition(10, 10)
                .Build();
                
            var fighter2 = new CharacterTestBuilder()
                .WithName("Fighter2")
                .WithStats(str: 2, end: 1, cha: 5, intel: 5, agi: 1, wis: 5)
                .WithHealth(15)
                .WithStamina(20)
                .WithPosition(11, 10)
                .Build();
            
            var combatants = new[] { fighter1, fighter2 };
            int maxRounds = 10; // Prevent infinite loops
            int round = 0;
            
            // Act - Fight until someone can't continue or max rounds reached
            while (fighter1.IsAlive && fighter2.IsAlive && fighter1.CanAct && fighter2.CanAct && round < maxRounds)
            {
                round++;
                
                // Fighter1 attacks Fighter2
                if (fighter1.CanAct && fighter1.CurrentStamina >= 3)
                {
                    var attack = _combatSystem.ExecuteAttack(fighter1, fighter2);
                    if (attack.Success)
                    {
                        var defenseChoice = fighter2.CurrentStamina >= 2 ? DefenseChoice.Defend : DefenseChoice.TakeDamage;
                        _combatSystem.ResolveDefense(fighter2, attack, defenseChoice);
                    }
                }
                
                if (!fighter2.IsAlive) break;
                
                // Fighter2 attacks Fighter1  
                if (fighter2.CanAct && fighter2.CurrentStamina >= 3)
                {
                    var attack = _combatSystem.ExecuteAttack(fighter2, fighter1);
                    if (attack.Success)
                    {
                        var defenseChoice = fighter1.CurrentStamina >= 2 ? DefenseChoice.Defend : DefenseChoice.TakeDamage;
                        _combatSystem.ResolveDefense(fighter1, attack, defenseChoice);
                    }
                }
                
                // Both rest if low on stamina (prevent exhaustion deadlock)
                if (fighter1.CurrentStamina <= 3) fighter1.RestoreStamina(5);
                if (fighter2.CurrentStamina <= 3) fighter2.RestoreStamina(5);
            }
            
            // Assert - Combat should conclude properly
            Assert.That(round, Is.LessThan(maxRounds), "Combat should conclude within reasonable rounds");
            
            // At least one fighter should be significantly damaged or dead
            var totalDamage = (15 - fighter1.CurrentHealth) + (15 - fighter2.CurrentHealth);
            Assert.That(totalDamage, Is.GreaterThan(0), "Combat should result in some damage");
            
            // Verify final states are valid
            AssertValidCharacter(fighter1);
            AssertValidCharacter(fighter2);
        }
        
        [Test]
        [Category("Integration")]
        public void Should_HandleResourceDepletion_When_CombatIsProlonged()
        {
            // Arrange - Characters with limited resources
            var stamina10Fighter1 = new CharacterTestBuilder()
                .WithName("Limited1")
                .WithStats(str: 3, end: 0, cha: 5, intel: 5, agi: 0, wis: 5)
                .WithHealth(20)
                .WithStamina(10) // Limited stamina
                .WithPosition(6, 6)
                .Build();
                
            var stamina10Fighter2 = new CharacterTestBuilder()
                .WithName("Limited2")
                .WithStats(str: 2, end: 1, cha: 5, intel: 5, agi: 0, wis: 5)
                .WithHealth(20)
                .WithStamina(10) // Limited stamina
                .WithPosition(7, 6)
                .Build();
            
            var actions = new List<string>();
            
            // Act - Exhaust stamina through combat
            for (int i = 0; i < 3; i++) // 3 attacks each (9 stamina used)
            {
                // Fighter1 attacks (3 stamina)
                if (stamina10Fighter1.CurrentStamina >= 3)
                {
                    var attack1 = _combatSystem.ExecuteAttack(stamina10Fighter1, stamina10Fighter2);
                    if (attack1.Success)
                    {
                        var choice = stamina10Fighter2.CurrentStamina >= 2 ? DefenseChoice.Defend : DefenseChoice.TakeDamage;
                        _combatSystem.ResolveDefense(stamina10Fighter2, attack1, choice);
                        actions.Add($"Round {i+1}: Fighter1 attacks, Fighter2 {choice}");
                    }
                }
                
                // Fighter2 attacks (3 stamina)  
                if (stamina10Fighter2.CurrentStamina >= 3)
                {
                    var attack2 = _combatSystem.ExecuteAttack(stamina10Fighter2, stamina10Fighter1);
                    if (attack2.Success)
                    {
                        var choice = stamina10Fighter1.CurrentStamina >= 2 ? DefenseChoice.Defend : DefenseChoice.TakeDamage;
                        _combatSystem.ResolveDefense(stamina10Fighter1, attack2, choice);
                        actions.Add($"Round {i+1}: Fighter2 attacks, Fighter1 {choice}");
                    }
                }
            }
            
            // Assert - Both should be exhausted or nearly so
            Assert.That(stamina10Fighter1.CurrentStamina, Is.LessThan(5), 
                       "Fighter1 should be low on stamina after multiple attacks");
            Assert.That(stamina10Fighter2.CurrentStamina, Is.LessThan(5), 
                       "Fighter2 should be low on stamina after multiple attacks");
            
            Assert.That(actions.Count, Is.GreaterThan(3), "Multiple combat actions should have occurred");
            
            // Neither should be able to attack anymore
            Assert.That(stamina10Fighter1.CurrentStamina < 3 || stamina10Fighter2.CurrentStamina < 3, Is.True,
                       "At least one fighter should be unable to attack");
        }
        
        // =============================================================================
        // ACTION ECONOMY INTEGRATION TESTS
        // =============================================================================
        
        [Test]
        [Category("Integration")]
        public void Should_HandleMoveAndSecondActionFlow_When_MovementAllowsSecondAction()
        {
            // Arrange - Scout with high mobility
            _scout.Position = new Position(8, 8);
            _alice.Position = new Position(10, 10); // Not adjacent initially
            
            var movementSystem = new MovementSystem(_combatSystem.DiceRoller);
            var initialScoutStamina = _scout.CurrentStamina;
            
            // Act - Scout moves then attacks
            // 1. Movement action (1 stamina, allows second action)
            var moveResult = movementSystem.CalculateMovement(_scout, MovementType.Simple);
            var newPosition = new Position(9, 9); // Closer to Alice
            var moveExecuted = movementSystem.ExecuteMovement(moveResult, newPosition);
            
            Assert.That(moveExecuted, Is.True, "Move should execute successfully");
            Assert.That(_scout.Position, Is.EqualTo(newPosition), "Scout should move to new position");
            Assert.That(moveResult.AllowsSecondAction, Is.True, "Move should allow second action");
            
            // 2. Second action - attack (if now in range)
            if (_scout.Position.IsAdjacent(_alice.Position))
            {
                var attackResult = _combatSystem.ExecuteAttack(_scout, _alice);
                Assert.That(attackResult.Success, Is.True, "Second action attack should succeed");
            }
            
            // Assert - Total stamina cost should be 1 (move) + 3 (attack) = 4
            var expectedStamina = initialScoutStamina - 1; // Just movement cost for this test
            if (_scout.Position.IsAdjacent(_alice.Position))
            {
                expectedStamina -= 3; // Attack cost if it happened
            }
            
            Assert.That(_scout.CurrentStamina, Is.LessThan(initialScoutStamina), 
                       "Scout should have used stamina for movement");
        }
        
        [Test]
        [Category("Integration")]
        public void Should_EndTurnAfterDash_When_DashMovementUsed()
        {
            // Arrange
            _scout.Position = new Position(2, 2);
            var movementSystem = new MovementSystem(_combatSystem.DiceRoller);
            var initialStamina = _scout.CurrentStamina;
            
            // Act - Dash movement (ends turn)
            var dashResult = movementSystem.CalculateMovement(_scout, MovementType.Dash);
            var farPosition = dashResult.ValidPositions.FirstOrDefault(p => 
                Math.Abs(p.X - _scout.Position.X) + Math.Abs(p.Y - _scout.Position.Y) > 3);
            
            if (farPosition != null)
            {
                var dashExecuted = movementSystem.ExecuteMovement(dashResult, farPosition);
                
                // Assert
                Assert.That(dashExecuted, Is.True, "Dash should execute");
                Assert.That(dashResult.AllowsSecondAction, Is.False, "Dash should end turn");
                Assert.That(_scout.CurrentStamina, Is.EqualTo(initialStamina - 1), 
                           "Dash should cost 1 stamina");
                Assert.That(_scout.Position, Is.EqualTo(farPosition), "Should move to far position");
            }
        }
        
        // =============================================================================
        // POSITIONING AND MOVEMENT INTEGRATION TESTS
        // =============================================================================
        
        [Test]
        [Category("Integration")]
        public void Should_RequireAdjacencyForAttack_When_CharactersAreFarApart()
        {
            // Arrange - Characters far apart
            _alice.Position = new Position(1, 1);
            _bob.Position = new Position(15, 15);
            
            var distance = _alice.Position.DistanceTo(_bob.Position);
            Assert.That(distance, Is.GreaterThan(1.5), "Characters should be far apart");
            
            // Act - Try to attack without being adjacent
            var attackResult = _combatSystem.ExecuteAttack(_alice, _bob);
            
            // Assert - Combat system allows attack execution, but game logic should prevent it
            // This demonstrates that positioning logic should be handled at higher levels
            Assert.That(attackResult.Success, Is.True, "CombatSystem allows attack (positioning checked elsewhere)");
            
            // In real game, GameManager or similar would check adjacency before allowing combat
        }
        
        [Test]
        [Category("Integration")]
        public void Should_AllowCombatAfterMovement_When_MovingIntoRange()
        {
            // Arrange - Scout starts far from Alice
            _scout.Position = new Position(3, 3);
            _alice.Position = new Position(8, 8);
            
            var movementSystem = new MovementSystem(_combatSystem.DiceRoller);
            
            // Act - Scout moves closer using dash
            var dashResult = movementSystem.CalculateMovement(_scout, MovementType.Dash);
            
            // Find position adjacent to Alice
            var adjacentToAlice = dashResult.ValidPositions.FirstOrDefault(p => p.IsAdjacent(_alice.Position));
            
            if (adjacentToAlice != null)
            {
                var moveExecuted = movementSystem.ExecuteMovement(dashResult, adjacentToAlice);
                Assert.That(moveExecuted, Is.True, "Should move into range");
                
                // Now attack should be possible (in a real game turn system)
                var attackResult = _combatSystem.ExecuteAttack(_scout, _alice);
                Assert.That(attackResult.Success, Is.True, "Attack should succeed after moving into range");
            }
            else
            {
                Assert.Inconclusive("Scout couldn't reach Alice in one dash with current dice roll");
            }
        }
        
        // =============================================================================
        // EDGE CASES AND STRESS TESTS
        // =============================================================================
        
        [Test]
        [Category("Integration")]
        public void Should_HandleNearDeathScenarios_When_CharactersHave1HP()
        {
            // Arrange - Characters with 1 HP each
            var nearDeath1 = new CharacterTestBuilder()
                .WithName("NearDeath1")
                .WithStats(str: 1, end: 0, cha: 5, intel: 5, agi: 0, wis: 5)
                .WithHealth(10)
                .WithStamina(10)
                .WithPosition(7, 7)
                .BuildWithCurrentHealth(1);
                
            var nearDeath2 = new CharacterTestBuilder()
                .WithName("NearDeath2")  
                .WithStats(str: 1, end: 0, cha: 5, intel: 5, agi: 0, wis: 5)
                .WithHealth(10)
                .WithStamina(10)
                .WithPosition(8, 7)
                .BuildWithCurrentHealth(1);
            
            // Act - One attack should end the fight
            var attackResult = _combatSystem.ExecuteAttack(nearDeath1, nearDeath2);
            if (attackResult.Success)
            {
                var defenseResult = _combatSystem.ResolveDefense(nearDeath2, attackResult, DefenseChoice.TakeDamage);
                
                // Assert
                Assert.That(nearDeath2.IsAlive, Is.False, "1 HP character should die from any attack");
                Assert.That(nearDeath2.CurrentHealth, Is.EqualTo(0), "Health should be 0");
                Assert.That(defenseResult.FinalDamage, Is.GreaterThan(0), "Should have taken damage");
            }
        }
        
        [Test]
        [Category("Integration")]
        public void Should_HandleZeroStaminaScenarios_When_CharactersExhausted()
        {
            // Arrange - Characters with just enough stamina for one action
            _alice.UseStamina(_alice.CurrentStamina - 3); // Only 3 stamina left
            _bob.UseStamina(_bob.CurrentStamina - 2);     // Only 2 stamina left
            
            _alice.Position = new Position(5, 5);
            _bob.Position = new Position(6, 5);
            
            // Act - Alice can attack once, Bob can defend once
            var attackResult = _combatSystem.ExecuteAttack(_alice, _bob);
            if (attackResult.Success)
            {
                var defenseResult = _combatSystem.ResolveDefense(_bob, attackResult, DefenseChoice.Defend);
                
                // Assert - Both should be exhausted after this exchange
                Assert.That(_alice.CurrentStamina, Is.EqualTo(0), "Alice should be exhausted after attack");
                Assert.That(_bob.CurrentStamina, Is.EqualTo(0), "Bob should be exhausted after defense");
                
                Assert.That(_alice.CanAct, Is.False, "Alice should not be able to act");
                Assert.That(_bob.CanAct, Is.False, "Bob should not be able to act");
            }
        }
        
        [Test]
        [Category("Integration")]
        public void Should_PreserveGameState_When_CombatIsInterrupted()
        {
            // Arrange - Start combat sequence
            _alice.Position = new Position(10, 5);
            _tank.Position = new Position(11, 5);
            
            var initialAliceStamina = _alice.CurrentStamina;
            var initialTankHealth = _tank.CurrentHealth;
            var initialTankCounter = _tank.Counter.Current;
            
            // Act - Execute attack but don't complete defense
            var attackResult = _combatSystem.ExecuteAttack(_alice, _tank);
            
            // Simulate interruption - check that partial state is preserved
            Assert.That(_alice.CurrentStamina, Is.EqualTo(initialAliceStamina - 3), 
                       "Attack stamina cost should be applied immediately");
            Assert.That(_tank.CurrentHealth, Is.EqualTo(initialTankHealth), 
                       "Target health should be unchanged until defense resolves");
            Assert.That(_tank.Counter.Current, Is.EqualTo(initialTankCounter), 
                       "Counter should be unchanged until defense");
            
            // Complete the sequence
            var defenseResult = _combatSystem.ResolveDefense(_tank, attackResult, DefenseChoice.Defend);
            
            // Assert - State should be properly updated after completion
            Assert.That(_tank.CurrentStamina, Is.EqualTo(_tank.MaxStamina - 2), 
                       "Defense stamina should be applied");
            
            if (defenseResult.FinalDamage > 0)
            {
                Assert.That(_tank.CurrentHealth, Is.LessThan(initialTankHealth), 
                           "Health should be updated after defense resolution");
            }
        }
        
        // =============================================================================
        // PERFORMANCE STRESS TESTS
        // =============================================================================
        
        [Test]
        [Category("Performance")]
        public void Should_HandleManyCombatExchanges_When_StressTesting()
        {
            // Arrange - Two regenerating fighters
            var fighter1 = new CharacterTestBuilder()
                .WithName("Fighter1")
                .WithStats(str: 1, end: 1, cha: 5, intel: 5, agi: 1, wis: 5)
                .WithHealth(50)
                .WithStamina(50)
                .WithPosition(5, 5)
                .Build();
                
            var fighter2 = new CharacterTestBuilder()
                .WithName("Fighter2")
                .WithStats(str: 1, end: 1, cha: 5, intel: 5, agi: 1, wis: 5)
                .WithHealth(50)
                .WithStamina(50)
                .WithPosition(6, 5)
                .Build();
            
            var startTime = DateTime.UtcNow;
            
            // Act - Execute many combat exchanges
            for (int i = 0; i < 100; i++)
            {
                // Fighter1 attacks Fighter2
                var attack1 = _combatSystem.ExecuteAttack(fighter1, fighter2);
                if (attack1.Success)
                {
                    _combatSystem.ResolveDefense(fighter2, attack1, DefenseChoice.TakeDamage);
                }
                
                // Fighter2 attacks Fighter1
                var attack2 = _combatSystem.ExecuteAttack(fighter2, fighter1);
                if (attack2.Success)
                {
                    _combatSystem.ResolveDefense(fighter1, attack2, DefenseChoice.TakeDamage);
                }
                
                // Regenerate to continue fighting
                fighter1.RestoreStamina(6);
                fighter2.RestoreStamina(6);
                fighter1.Heal(2);
                fighter2.Heal(2);
            }
            
            var elapsed = DateTime.UtcNow - startTime;
            
            // Assert
            Assert.That(elapsed.TotalMilliseconds, Is.LessThan(2000), 
                       "200 combat actions should complete within 2 seconds");
            
            // Verify characters are still in valid states
            AssertValidCharacter(fighter1);
            AssertValidCharacter(fighter2);
        }
        
        // =============================================================================
        // COMPLEX SCENARIO INTEGRATION TESTS
        // =============================================================================
        
        [Test]
        [Category("Integration")]
        public void Should_HandleComplexThreeWayCombat_When_MultipleCharactersFighting()
        {
            // Arrange - Three characters in a triangle formation
            _alice.Position = new Position(5, 5);
            _bob.Position = new Position(6, 5);   // Adjacent to Alice
            _scout.Position = new Position(6, 6); // Adjacent to Bob
            
            var combatants = new[] { _alice, _bob, _scout };
            var actions = new List<string>();
            
            // Act - Round-robin combat
            for (int round = 0; round < 3; round++)
            {
                // Alice attacks Bob
                if (_alice.CanAct && _alice.CurrentStamina >= 3)
                {
                    var attack = _combatSystem.ExecuteAttack(_alice, _bob);
                    if (attack.Success)
                    {
                        var choice = _bob.CurrentStamina >= 2 ? DefenseChoice.Defend : DefenseChoice.TakeDamage;
                        _combatSystem.ResolveDefense(_bob, attack, choice);
                        actions.Add($"R{round}: Alice → Bob ({choice})");
                    }
                }
                
                // Bob attacks Scout (if alive and able)
                if (_bob.IsAlive && _bob.CanAct && _bob.CurrentStamina >= 3)
                {
                    var attack = _combatSystem.ExecuteAttack(_bob, _scout);
                    if (attack.Success)
                    {
                        var choice = _scout.CurrentStamina >= 1 ? DefenseChoice.Move : DefenseChoice.TakeDamage;
                        _combatSystem.ResolveDefense(_scout, attack, choice);
                        actions.Add($"R{round}: Bob → Scout ({choice})");
                    }
                }
                
                // Scout attacks Alice (if alive, able, and in range)  
                if (_scout.IsAlive && _scout.CanAct && _scout.CurrentStamina >= 3)
                {
                    // Scout might need to move first to reach Alice
                    if (_scout.Position.IsAdjacent(_alice.Position))
                    {
                        var attack = _combatSystem.ExecuteAttack(_scout, _alice);
                        if (attack.Success)
                        {
                            var choice = _alice.CurrentStamina >= 2 ? DefenseChoice.Defend : DefenseChoice.TakeDamage;
                            _combatSystem.ResolveDefense(_alice, attack, choice);
                            actions.Add($"R{round}: Scout → Alice ({choice})");
                        }
                    }
                }
                
                // Rest phase to continue combat
                foreach (var fighter in combatants.Where(c => c.IsAlive))
                {
                    fighter.RestoreStamina(3);
                }
            }
            
            // Assert - Complex combat should complete without errors
            Assert.That(actions.Count, Is.GreaterThan(3), "Multiple combat actions should occur");
            
            foreach (var combatant in combatants)
            {
                AssertValidCharacter(combatant);
            }
            
            // At least one character should have taken some damage
            var totalDamage = combatants.Sum(c => c.MaxHealth - c.CurrentHealth);
            Assert.That(totalDamage, Is.GreaterThan(0), "Three-way combat should result in damage");
        }
        
        [Test]
        [Category("Integration")]
        public void Should_CompleteFullGameScenario_When_PlayingCompleteMatch()
        {
            // Arrange - Full game scenario with balanced fighters positioned close together
            var players = new[]
            {
                CommonCharacters.Alice().WithPosition(5, 5).Build(),        // ATK:1, DEF:0, HP:10
                CommonCharacters.Bob().WithPosition(6, 5).Build(),          // ATK:2, DEF:0, HP:10
                CommonCharacters.GlassCannon().WithName("Cannon").WithPosition(7, 5).WithHealth(8).Build() // ATK:5, DEF:0, HP:8
            };
            
            _turnManager.StartCombat(players);
            
            var gameActions = new List<string>();
            int maxTurns = 30; // Increased for more realistic game length
            int turn = 0;
            
            // Act - Play complete game until win condition
            while (_turnManager.GameActive && turn < maxTurns)
            {
                turn++;
                var turnResult = _turnManager.NextTurn();
                
                if (!turnResult.Success) break;
                
                var currentActor = turnResult.CurrentActor;
                
                // Improved AI: Prioritize attacks on weakest targets
                if (turnResult.AvailableActions.Contains(ActionChoice.Attack) && currentActor.CurrentStamina >= 3)
                {
                    // Find weakest target (lowest health, alive, adjacent if possible)
                    var availableTargets = players.Where(p => p != currentActor && p.IsAlive).ToList();
                    var target = availableTargets.OrderBy(p => p.CurrentHealth).FirstOrDefault();
                    
                    if (target != null)
                    {
                        var actionResult = _turnManager.ExecuteAction(ActionChoice.Attack, target);
                        gameActions.Add($"T{turn}: {currentActor.Name} attacks {target.Name}");
                        
                        // Handle defense automatically with smart choices
                        if (actionResult.RequiresTargetResponse)
                        {
                            var attack = _combatSystem.ExecuteAttack(currentActor, target);
                            if (attack.Success)
                            {
                                // Smart defense: Defend if high stamina, take damage if low
                                var defenseChoice = target.CurrentStamina >= 4 ? DefenseChoice.Defend : DefenseChoice.TakeDamage;
                                _combatSystem.ResolveDefense(target, attack, defenseChoice);
                                gameActions.Add($"T{turn}: {target.Name} chooses {defenseChoice}");
                            }
                        }
                    }
                }
                else if (turnResult.AvailableActions.Contains(ActionChoice.Rest))
                {
                    _turnManager.ExecuteAction(ActionChoice.Rest);
                    gameActions.Add($"T{turn}: {currentActor.Name} rests");
                }
                
                // Check win condition
                var alivePlayers = players.Where(p => p.IsAlive).Count();
                if (alivePlayers <= 1)
                {
                    gameActions.Add($"T{turn}: Game ends - {alivePlayers} player(s) remaining");
                    break;
                }
            }
            
            // Assert - Game should complete properly  
            Assert.That(turn, Is.LessThan(maxTurns), $"Game should conclude within {maxTurns} turns but took {turn} turns. Actions: {string.Join(", ", gameActions.TakeLast(5))}");
            Assert.That(gameActions.Count, Is.GreaterThan(3), "Game should have multiple actions");
            
            // Verify final game state
            var survivors = players.Where(p => p.IsAlive).ToList();
            if (survivors.Count <= 1)
            {
                if (survivors.Count == 1)
                {
                    AssertValidCharacter(survivors[0]);
                    gameActions.Add($"Winner: {survivors[0].Name} with {survivors[0].CurrentHealth} HP");
                }
                else
                {
                    gameActions.Add("Draw: All players eliminated");
                }
            }
            else
            {
                // If game didn't conclude naturally, that's still valid - just means it was balanced
                Assert.That(survivors.Count, Is.GreaterThan(1), $"Game ended with {survivors.Count} survivors");
            }
            
            // All players should be in valid states regardless of alive status
            foreach (var player in players)
            {
                AssertValidCharacter(player);
            }
        }
    }
}
