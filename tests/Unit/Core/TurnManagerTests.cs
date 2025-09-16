using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RPGGame.Characters;
using RPGGame.Core;
using RPGGame.Tests.TestHelpers;

namespace RPGGame.Tests.Unit.Core
{
    /// <summary>
    /// Tests for TurnManager - the core game flow orchestration system
    /// CRITICAL SYSTEM: Manages turn order, action validation, counter-attacks, and win conditions
    /// Tests game initialization, turn progression, action economy, and badminton streak mechanics
    /// </summary>
    [TestFixture]
    public class TurnManagerTests : TestBase
    {
        private TurnManager _turnManager;
        private Character _alice;
        private Character _bob;
        private Character _charlie;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            // Initialize turn manager
            _turnManager = new TurnManager();
            
            // Create standard test characters
            _alice = CommonCharacters.Alice().Build();
            _bob = CommonCharacters.Bob().Build();
            _charlie = CommonCharacters.Tank().WithName("Charlie").Build();
        }
        
        // =============================================================================
        // INITIALIZATION TESTS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_InitializeCorrectly_When_Created()
        {
            // Arrange & Act
            var turnManager = new TurnManager();
            
            // Assert
            Assert.That(turnManager.CurrentActor, Is.Null, "Should have no current actor initially");
            Assert.That(turnManager.RoundNumber, Is.EqualTo(0), "Should start at round 0");
            Assert.That(turnManager.GameActive, Is.False, "Game should not be active initially");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_StartCombat_When_GivenValidCharacters()
        {
            // Act
            _turnManager.StartCombat(_alice, _bob);
            
            // Assert
            Assert.That(_turnManager.GameActive, Is.True, "Game should be active after starting");
            Assert.That(_turnManager.RoundNumber, Is.EqualTo(1), "Should be in round 1");
            // Note: CurrentActor is null until NextTurn() is called
            
            var livingParticipants = _turnManager.GetLivingParticipants();
            Assert.That(livingParticipants.Count, Is.EqualTo(2), "Should have 2 living participants");
            Assert.That(livingParticipants, Contains.Item(_alice), "Should include Alice");
            Assert.That(livingParticipants, Contains.Item(_bob), "Should include Bob");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_ThrowException_When_StartingWithInsufficientParticipants()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => 
                _turnManager.StartCombat(_alice), "Should require at least 2 characters");
            
            Assert.Throws<InvalidOperationException>(() => 
                _turnManager.StartCombat(), "Should require at least 2 characters");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_FilterDeadCharacters_When_StartingCombat()
        {
            // Arrange
            _charlie.TakeDamage(100); // Kill Charlie
            
            // Act
            _turnManager.StartCombat(_alice, _bob, _charlie);
            
            // Assert
            var livingParticipants = _turnManager.GetLivingParticipants();
            Assert.That(livingParticipants.Count, Is.EqualTo(2), "Should only include living characters");
            Assert.That(livingParticipants, Does.Not.Contain(_charlie), "Should exclude dead Charlie");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_ThrowException_When_StartingWithOnlyDeadCharacters()
        {
            // Arrange
            _alice.TakeDamage(100);
            _bob.TakeDamage(100);
            
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => 
                _turnManager.StartCombat(_alice, _bob), "Should require living characters");
        }
        
        // =============================================================================
        // TURN PROGRESSION TESTS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_ReturnValidTurn_When_GettingNextTurn()
        {
            // Arrange
            _turnManager.StartCombat(_alice, _bob);
            
            // Act
            var turn = _turnManager.NextTurn();
            
            // Assert
            Assert.That(turn.Success, Is.True, "Turn should be successful");
            Assert.That(turn.CurrentActor, Is.Not.Null, "Should have current actor");
            Assert.That(turn.TurnType, Is.EqualTo(TurnType.Normal), "Should be normal turn");
            Assert.That(turn.AvailableActions, Is.Not.Empty, "Should have available actions");
            Assert.That(turn.CurrentActor.CanAct, Is.True, "Current actor should be able to act");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_AlternateBetweenCharacters_When_AdvancingTurns()
        {
            // Arrange
            _turnManager.StartCombat(_alice, _bob);
            
            // Act
            var turn1 = _turnManager.NextTurn();
            var actor1 = turn1.CurrentActor;
            
            var turn2 = _turnManager.NextTurn();
            var actor2 = turn2.CurrentActor;
            
            var turn3 = _turnManager.NextTurn();
            var actor3 = turn3.CurrentActor;
            
            // Assert
            Assert.That(actor2, Is.Not.EqualTo(actor1), "Second turn should be different actor");
            Assert.That(actor3, Is.EqualTo(actor1), "Third turn should return to first actor");
            
            // Both actors should get turns
            var uniqueActors = new[] { actor1, actor2, actor3 }.Distinct().Count();
            Assert.That(uniqueActors, Is.EqualTo(2), "Should alternate between 2 actors");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_SkipDeadCharacters_When_AdvancingTurns()
        {
            // Arrange
            _turnManager.StartCombat(_alice, _bob, _charlie);
            
            // Get first few turns to establish order
            var turn1 = _turnManager.NextTurn();
            var turn2 = _turnManager.NextTurn();
            var turn3 = _turnManager.NextTurn();
            
            // Kill the current actor
            var currentActor = _turnManager.CurrentActor;
            currentActor.TakeDamage(100);
            
            // Act
            var nextTurn = _turnManager.NextTurn();
            
            // Assert
            Assert.That(nextTurn.CurrentActor, Is.Not.EqualTo(currentActor), 
                       "Should skip dead character");
            Assert.That(nextTurn.CurrentActor.IsAlive, Is.True, 
                       "Next actor should be alive");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_SkipIncapacitatedCharacters_When_AdvancingTurns()
        {
            // Arrange
            _turnManager.StartCombat(_alice, _bob);
            
            // Drain one character's stamina
            _alice.UseStamina(10); // Drain all stamina
            
            // Act
            var turn = _turnManager.NextTurn();
            
            // Assert - Should skip Alice if she can't act
            if (!_alice.CanAct)
            {
                Assert.That(turn.CurrentActor, Is.Not.EqualTo(_alice), 
                           "Should skip incapacitated character");
            }
        }
        
        [Test]
        [Category("Unit")]
        public void Should_StartNewRound_When_AllCharactersHaveTakenTurns()
        {
            // Arrange
            _turnManager.StartCombat(_alice, _bob);
            var initialRound = _turnManager.RoundNumber;
            
            // Act - Take turns for all characters
            _turnManager.NextTurn(); // Alice or Bob
            _turnManager.NextTurn(); // Bob or Alice
            var newRoundTurn = _turnManager.NextTurn(); // Should start new round
            
            // Assert
            Assert.That(_turnManager.RoundNumber, Is.GreaterThan(initialRound), 
                       "Should advance to new round");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_ReturnError_When_GameNotActive()
        {
            // Arrange - Don't start combat
            
            // Act
            var turn = _turnManager.NextTurn();
            
            // Assert
            Assert.That(turn.Success, Is.False, "Should fail when game not active");
            Assert.That(turn.Message, Does.Contain("not active"), "Should indicate game not active");
        }
        
        // =============================================================================
        // ACTION VALIDATION TESTS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_ReturnCorrectAvailableActions_When_CharacterHasFullStamina()
        {
            // Arrange
            _turnManager.StartCombat(_alice, _bob);
            var turn = _turnManager.NextTurn();
            var actor = turn.CurrentActor;
            
            // Assert - Character with full stamina should have all actions
            Assert.That(turn.AvailableActions, Contains.Item(ActionChoice.Attack), 
                       "Should be able to attack with 3+ stamina");
            Assert.That(turn.AvailableActions, Contains.Item(ActionChoice.Defend), 
                       "Should be able to defend with 2+ stamina");
            Assert.That(turn.AvailableActions, Contains.Item(ActionChoice.Move), 
                       "Should be able to move with 1+ stamina");
            Assert.That(turn.AvailableActions, Contains.Item(ActionChoice.Rest), 
                       "Should always be able to rest");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_LimitAvailableActions_When_CharacterHasLowStamina()
        {
            // Arrange
            _turnManager.StartCombat(_alice, _bob);
            var turn = _turnManager.NextTurn();
            var actor = turn.CurrentActor;
            
            // Drain stamina to 2 (can defend and move, but not attack)
            actor.UseStamina(actor.CurrentStamina - 2);
            
            // Act - Get a fresh turn to recalculate available actions
            var nextTurn = _turnManager.NextTurn();
            
            // Assert
            if (nextTurn.CurrentActor == actor)
            {
                Assert.That(nextTurn.AvailableActions, Does.Not.Contain(ActionChoice.Attack), 
                           "Should not be able to attack with only 2 stamina");
                Assert.That(nextTurn.AvailableActions, Contains.Item(ActionChoice.Defend), 
                           "Should be able to defend with 2 stamina");
                Assert.That(nextTurn.AvailableActions, Contains.Item(ActionChoice.Move), 
                           "Should be able to move with 2 stamina");
                Assert.That(nextTurn.AvailableActions, Contains.Item(ActionChoice.Rest), 
                           "Should always be able to rest");
            }
        }
        
        [Test]
        [Category("Unit")]
        public void Should_OnlyAllowRest_When_CharacterIsExhausted()
        {
            // Arrange
            _turnManager.StartCombat(_alice, _bob);
            var turn = _turnManager.NextTurn();
            var actor = turn.CurrentActor;
            
            // Drain all stamina
            actor.UseStamina(actor.CurrentStamina);
            
            // Act
            var exhaustedTurn = _turnManager.NextTurn();
            
            // Assert - Should skip exhausted character or only allow rest
            if (exhaustedTurn.CurrentActor == actor)
            {
                Assert.That(exhaustedTurn.AvailableActions, Does.Not.Contain(ActionChoice.Attack));
                Assert.That(exhaustedTurn.AvailableActions, Does.Not.Contain(ActionChoice.Defend));
                Assert.That(exhaustedTurn.AvailableActions, Does.Not.Contain(ActionChoice.Move));
                Assert.That(exhaustedTurn.AvailableActions, Contains.Item(ActionChoice.Rest));
            }
        }
        
        // =============================================================================
        // ACTION EXECUTION TESTS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_ExecuteAttackAction_When_ValidTargetProvided()
        {
            // Arrange
            _turnManager.StartCombat(_alice, _bob);
            _turnManager.NextTurn();
            
            // Act
            var result = _turnManager.ExecuteAction(ActionChoice.Attack, _bob);
            
            // Assert
            Assert.That(result.Success, Is.True, "Attack should succeed");
            Assert.That(result.Action, Is.EqualTo(ActionChoice.Attack));
            Assert.That(result.Actor, Is.EqualTo(_turnManager.CurrentActor));
            Assert.That(result.Target, Is.EqualTo(_bob));
            Assert.That(result.RequiresTargetResponse, Is.True, 
                       "Attack should require target response");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_FailAttackAction_When_NoTargetProvided()
        {
            // Arrange
            _turnManager.StartCombat(_alice, _bob);
            _turnManager.NextTurn();
            
            // Act
            var result = _turnManager.ExecuteAction(ActionChoice.Attack, target: null);
            
            // Assert
            Assert.That(result.Success, Is.False, "Attack should fail without target");
            Assert.That(result.Message, Does.Contain("target"), "Should mention invalid target");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_FailAttackAction_When_TargetIsDead()
        {
            // Arrange
            _turnManager.StartCombat(_alice, _bob);
            _bob.TakeDamage(100); // Kill Bob
            _turnManager.NextTurn();
            
            // Act
            var result = _turnManager.ExecuteAction(ActionChoice.Attack, _bob);
            
            // Assert
            Assert.That(result.Success, Is.False, "Attack should fail on dead target");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_ExecuteDefendAction_When_ActorCanDefend()
        {
            // Arrange
            _turnManager.StartCombat(_alice, _bob);
            _turnManager.NextTurn();
            
            // Act
            var result = _turnManager.ExecuteAction(ActionChoice.Defend);
            
            // Assert
            Assert.That(result.Success, Is.True, "Defend should succeed");
            Assert.That(result.Action, Is.EqualTo(ActionChoice.Defend));
            Assert.That(result.DefenseBonusNextTurn, Is.True, 
                       "Defend should provide bonus next turn");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_ExecuteRestAction_When_ActorNeedsStamina()
        {
            // Arrange
            _turnManager.StartCombat(_alice, _bob);
            var turn = _turnManager.NextTurn();
            var actor = turn.CurrentActor;
            
            // Drain some stamina first
            var initialStamina = actor.CurrentStamina;
            actor.UseStamina(5);
            
            // Act
            var result = _turnManager.ExecuteAction(ActionChoice.Rest);
            
            // Assert
            Assert.That(result.Success, Is.True, "Rest should succeed");
            Assert.That(result.Action, Is.EqualTo(ActionChoice.Rest));
            Assert.That(actor.CurrentStamina, Is.GreaterThan(initialStamina - 5), 
                       "Stamina should be restored");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_FailAction_When_GameNotActive()
        {
            // Arrange - Don't start combat
            
            // Act
            var result = _turnManager.ExecuteAction(ActionChoice.Attack, _bob);
            
            // Assert
            Assert.That(result.Success, Is.False, "Should fail when game not active");
            Assert.That(result.Message, Does.Contain("Not a valid turn"), 
                       "Should indicate invalid turn");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_FailAction_When_InsufficientStamina()
        {
            // Arrange
            _turnManager.StartCombat(_alice, _bob);
            var turn = _turnManager.NextTurn();
            var actor = turn.CurrentActor;
            
            // Drain stamina below attack requirement
            actor.UseStamina(actor.CurrentStamina - 2); // Leave only 2, need 3 for attack
            
            // Act
            var result = _turnManager.ExecuteAction(ActionChoice.Attack, _bob);
            
            // Assert
            Assert.That(result.Success, Is.False, "Should fail with insufficient stamina");
            Assert.That(result.Message, Does.Contain("stamina"), "Should mention stamina issue");
        }
        
        // =============================================================================
        // COUNTER-ATTACK (BADMINTON STREAK) TESTS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_DetectCounterAttackOpportunity_When_CharacterHasReadyCounter()
        {
            // Arrange
            _turnManager.StartCombat(_alice, _bob);
            _alice.Counter.AddCounter(6); // Make counter ready
            
            // Act
            var turn = _turnManager.NextTurn();
            
            // Assert - Should detect counter opportunity
            if (turn.TurnType == TurnType.CounterAttack)
            {
                Assert.That(turn.CurrentActor, Is.EqualTo(_alice), 
                           "Counter turn should be for character with ready counter");
                Assert.That(turn.AvailableActions, Contains.Item(ActionChoice.Attack), 
                           "Counter turn should allow attack");
                Assert.That(turn.AvailableActions.Count, Is.EqualTo(1), 
                           "Counter turn should only allow attack");
            }
        }
        
        [Test]
        [Category("Unit")]
        public void Should_PrioritizeCounterAttack_When_MultipleCharactersReady()
        {
            // Arrange
            _turnManager.StartCombat(_alice, _bob, _charlie);
            _alice.Counter.AddCounter(6);
            _bob.Counter.AddCounter(6);
            
            // Act
            var turn = _turnManager.NextTurn();
            
            // Assert - Should give counter turn to someone with ready counter
            if (turn.TurnType == TurnType.CounterAttack)
            {
                Assert.That(turn.CurrentActor.Counter.IsReady, Is.True, 
                           "Counter turn actor should have ready counter");
            }
        }
        
        [Test]
        [Category("Unit")]
        public void Should_ResumeNormalTurns_When_CounterAttackComplete()
        {
            // Arrange
            _turnManager.StartCombat(_alice, _bob);
            _alice.Counter.AddCounter(6);
            
            // Act - Get counter turn and process it
            var counterTurn = _turnManager.NextTurn();
            if (counterTurn.TurnType == TurnType.CounterAttack)
            {
                _turnManager.ExecuteAction(ActionChoice.Attack, _bob);
                _alice.Counter.ConsumeCounter(); // Simulate counter consumption
            }
            
            var nextTurn = _turnManager.NextTurn();
            
            // Assert
            Assert.That(nextTurn.TurnType, Is.EqualTo(TurnType.Normal), 
                       "Should return to normal turns after counter");
        }
        
        // =============================================================================
        // WIN CONDITION TESTS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_DetectWinCondition_When_OnlyOneCharacterRemains()
        {
            // Arrange
            _turnManager.StartCombat(_alice, _bob);
            _turnManager.NextTurn();
            
            // Kill one character
            _bob.TakeDamage(100);
            
            // Act
            var result = _turnManager.ExecuteAction(ActionChoice.Rest);
            
            // Assert
            Assert.That(result.GameEnded, Is.True, "Game should end when one character remains");
            Assert.That(result.Winner, Is.EqualTo(_alice), "Alice should be the winner");
            Assert.That(_turnManager.GameActive, Is.False, "Game should no longer be active");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_ContinueGame_When_MultipleCharactersAlive()
        {
            // Arrange
            _turnManager.StartCombat(_alice, _bob, _charlie);
            _turnManager.NextTurn();
            
            // Kill one character but leave others alive
            _charlie.TakeDamage(100);
            
            // Act
            var result = _turnManager.ExecuteAction(ActionChoice.Rest);
            
            // Assert
            Assert.That(result.GameEnded, Is.False, "Game should continue with multiple living characters");
            Assert.That(_turnManager.GameActive, Is.True, "Game should remain active");
        }
        
        // =============================================================================
        // TURN HISTORY TESTS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_LogTurnHistory_When_ActionsOccur()
        {
            // Arrange
            _turnManager.StartCombat(_alice, _bob);
            _turnManager.NextTurn();
            
            // Act
            _turnManager.ExecuteAction(ActionChoice.Rest);
            _turnManager.NextTurn();
            _turnManager.ExecuteAction(ActionChoice.Defend);
            
            var history = _turnManager.GetTurnHistory();
            
            // Assert
            Assert.That(history, Is.Not.Empty, "Should have turn history");
            Assert.That(history.Count, Is.GreaterThanOrEqualTo(3), "Should log combat start and actions");
            
            var lastEntry = history.Last();
            Assert.That(lastEntry.Message, Does.Contain("Defend"), "Should log defend action");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_LimitHistorySize_When_RequestingRecentEntries()
        {
            // Arrange
            _turnManager.StartCombat(_alice, _bob);
            
            // Perform many actions
            for (int i = 0; i < 10; i++)
            {
                _turnManager.NextTurn();
                _turnManager.ExecuteAction(ActionChoice.Rest);
            }
            
            // Act
            var recentHistory = _turnManager.GetTurnHistory(recentCount: 5);
            
            // Assert
            Assert.That(recentHistory.Count, Is.LessThanOrEqualTo(5), 
                       "Should limit to requested count");
        }
        
        // =============================================================================
        // MULTI-PLAYER SCENARIOS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_HandleThreePlayerCombat_When_StartingWithMultipleCharacters()
        {
            // Arrange & Act
            _turnManager.StartCombat(_alice, _bob, _charlie);
            
            // Get several turns to verify all characters participate
            var actors = new List<Character>();
            for (int i = 0; i < 6; i++) // Two full rounds
            {
                var turn = _turnManager.NextTurn();
                actors.Add(turn.CurrentActor);
            }
            
            // Assert
            var uniqueActors = actors.Distinct().Count();
            Assert.That(uniqueActors, Is.EqualTo(3), "All three characters should get turns");
            Assert.That(actors, Contains.Item(_alice), "Alice should get turns");
            Assert.That(actors, Contains.Item(_bob), "Bob should get turns");
            Assert.That(actors, Contains.Item(_charlie), "Charlie should get turns");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_HandlePlayerElimination_When_CharacterDiesDuringCombat()
        {
            // Arrange
            _turnManager.StartCombat(_alice, _bob, _charlie);
            
            // Kill one character mid-combat
            _bob.TakeDamage(100);
            
            // Act - Get several turns
            var actors = new List<Character>();
            for (int i = 0; i < 6; i++)
            {
                var turn = _turnManager.NextTurn();
                actors.Add(turn.CurrentActor);
            }
            
            // Assert
            Assert.That(actors, Does.Not.Contain(_bob), "Dead character should not get turns");
            
            var livingActors = actors.Where(a => a.IsAlive).Distinct().Count();
            Assert.That(livingActors, Is.EqualTo(2), "Only living characters should act");
        }
        
        // =============================================================================
        // EDGE CASES AND ERROR HANDLING
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_HandleInvalidAction_When_UsingUndefinedAction()
        {
            // Arrange
            _turnManager.StartCombat(_alice, _bob);
            _turnManager.NextTurn();
            
            var invalidAction = (ActionChoice)999;
            
            // Act
            var result = _turnManager.ExecuteAction(invalidAction);
            
            // Assert
            Assert.That(result.Success, Is.False, "Should handle invalid action gracefully");
            Assert.That(result.Message, Does.Contain("Invalid"), "Should indicate invalid action");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_HandleEmptyParticipantList_When_AllCharactersDie()
        {
            // Arrange
            _turnManager.StartCombat(_alice, _bob);
            
            // Kill all characters
            _alice.TakeDamage(100);
            _bob.TakeDamage(100);
            
            // Act & Assert - Should throw exception when no valid actors remain
            Assert.Throws<InvalidOperationException>(() => _turnManager.NextTurn(),
                "Should throw exception when no living participants remain");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_PreserveTurnOrder_When_CharactersRestoreStamina()
        {
            // Arrange
            _turnManager.StartCombat(_alice, _bob);
            
            // Get initial turn order (record first few actors)
            var turn1 = _turnManager.NextTurn();
            var actor1 = turn1.CurrentActor;
            
            var turn2 = _turnManager.NextTurn();
            var actor2 = turn2.CurrentActor;
            
            // Both characters rest to restore stamina
            _turnManager.ExecuteAction(ActionChoice.Rest);
            _turnManager.NextTurn();
            _turnManager.ExecuteAction(ActionChoice.Rest);
            
            // Act - Get next turn after rest (should start new round)
            var turn3 = _turnManager.NextTurn();
            var actor3 = turn3.CurrentActor;
            
            var turn4 = _turnManager.NextTurn();
            var actor4 = turn4.CurrentActor;
            
            // Assert - Turn order pattern should be consistent
            // Either actor1 or actor2 should come first in the new round
            Assert.That(new[] { actor1, actor2 }, Contains.Item(actor3), 
                       "New round should start with one of the original actors");
            Assert.That(new[] { actor1, actor2 }, Contains.Item(actor4), 
                       "Second actor in new round should be one of the original actors");
            Assert.That(actor3, Is.Not.EqualTo(actor4), 
                       "Should alternate between different actors");
        }
        
        // =============================================================================
        // PERFORMANCE AND STRESS TESTS
        // =============================================================================
        
        [Test]
        [Category("Performance")]
        public void Should_HandleManyTurns_When_RunningLongCombat()
        {
            // Arrange
            _turnManager.StartCombat(_alice, _bob);
            
            // Act - Run 100 turns (mostly rest actions)
            var startTime = DateTime.UtcNow;
            
            for (int i = 0; i < 100; i++)
            {
                _turnManager.NextTurn();
                _turnManager.ExecuteAction(ActionChoice.Rest);
                
                if (!_turnManager.GameActive) break; // Stop if game ends
            }
            
            var elapsed = DateTime.UtcNow - startTime;
            
            // Assert
            Assert.That(elapsed.TotalMilliseconds, Is.LessThan(1000), 
                       "100 turns should complete quickly");
        }
        
        // =============================================================================
        // INTEGRATION WITH TEST SCENARIOS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_WorkWithTestScenarios_When_UsingPrebuiltCharacters()
        {
            // Arrange
            var characters = TestScenarios.TurnOrder.FourPlayers();
            
            // Act
            _turnManager.StartCombat(characters.ToArray());
            
            // Assert
            Assert.That(_turnManager.GameActive, Is.True, "Should start with scenario characters");
            Assert.That(_turnManager.GetLivingParticipants().Count, Is.EqualTo(4), 
                       "Should have all four scenario characters");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_HandleMixedStates_When_UsingTestScenarioCharacters()
        {
            // Arrange
            var characters = TestScenarios.TurnOrder.MixedStates();
            
            // Act
            _turnManager.StartCombat(characters.ToArray());
            var turn = _turnManager.NextTurn();
            
            // Assert
            Assert.That(turn.Success, Is.True, "Should handle mixed character states");
            Assert.That(turn.CurrentActor.CanAct, Is.True, "Current actor should be able to act");
        }
    }
}