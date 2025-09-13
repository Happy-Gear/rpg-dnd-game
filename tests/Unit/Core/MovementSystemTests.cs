using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RPGGame.Core;
using RPGGame.Characters;
using RPGGame.Dice;
using RPGGame.Grid;
using RPGGame.Tests.TestHelpers;

namespace RPGGame.Tests.Unit.Core
{
    /// <summary>
    /// Tests for MovementSystem - dice-based movement with action economy
    /// Critical system: Movement affects positioning, action economy, and tactical decisions
    /// Tests 1d6 vs 2d6 mechanics, stat integration, grid boundaries, and distance validation
    /// </summary>
    [TestFixture]
    public class MovementSystemTests : TestBase
    {
        private MovementSystem _movementSystem;
        private Character _character;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            // Use deterministic dice for predictable tests
            _movementSystem = new MovementSystem(DeterministicDice);
            
            // Standard test character with balanced stats
            _character = new CharacterTestBuilder()
                .WithName("TestMover")
                .WithAgility(2) // 2 AGI = +2 movement points
                .WithStamina(10)
                .WithPosition(8, 8) // Center of 16x16 grid
                .Build();
        }
        
        // =============================================================================
        // CONSTRUCTOR TESTS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_CreateMovementSystem_When_UsingDiceRoller()
        {
            // Arrange & Act
            var movementSystem = new MovementSystem(DeterministicDice);
            
            // Assert
            Assert.That(movementSystem, Is.Not.Null, "MovementSystem should be created");
        }
        
        [Test]
		[Category("Unit")]
		public void Should_ThrowException_When_DiceRollerIsNull()
		{
			// Act & Assert - Constructor allows null, but usage fails
			Assert.DoesNotThrow(() => new MovementSystem(null));
			
			var movementSystem = new MovementSystem(null);
			Assert.Throws<NullReferenceException>(() => 
				movementSystem.CalculateMovement(_character, MovementType.Simple));
		}
        
        // =============================================================================
        // CALCULATE MOVEMENT TESTS - SIMPLE MOVE (1d6)
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_CalculateSimpleMove_When_UsingMovementTypeSimple()
        {
            // Act
            var result = _movementSystem.CalculateMovement(_character, MovementType.Simple);
            
            // Assert
            Assert.That(result, Is.Not.Null, "Should return movement result");
            Assert.That(result.Character, Is.EqualTo(_character), "Should reference character");
            Assert.That(result.MovementType, Is.EqualTo(MovementType.Simple), "Should be Simple movement");
            Assert.That(result.MoveRoll, Is.Not.Null, "Should have dice roll");
            Assert.That(result.MoveRoll.Is1d6, Is.True, "Simple move should use 1d6");
            Assert.That(result.StaminaCost, Is.EqualTo(1), "Simple move costs 1 stamina");
            Assert.That(result.AllowsSecondAction, Is.True, "Simple move allows second action");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_AddMovementPointsToRoll_When_CalculatingSimpleMove()
        {
            // Arrange - Character has 2 AGI (movement points)
            
            // Act
            var result = _movementSystem.CalculateMovement(_character, MovementType.Simple);
            
            // Assert
            var expectedDistance = result.MoveRoll.Total + 2; // Roll + AGI
            Assert.That(result.MaxDistance, Is.EqualTo(expectedDistance), 
                       "Max distance should be roll + movement points");
            Assert.That(result.MaxDistance, Is.InRange(3, 8), 
                       "Distance should be 1d6 (1-6) + 2 AGI = 3-8");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_GenerateValidPositions_When_CalculatingSimpleMove()
        {
            // Act
            var result = _movementSystem.CalculateMovement(_character, MovementType.Simple);
            
            // Assert
            Assert.That(result.ValidPositions, Is.Not.Null, "Should have valid positions");
            Assert.That(result.ValidPositions.Count, Is.GreaterThan(0), "Should have some valid positions");
            
            // All positions should be within max distance
            foreach (var pos in result.ValidPositions)
            {
                var distance = CalculateManhattanDistance(_character.Position, pos);
                Assert.That(distance, Is.LessThanOrEqualTo(result.MaxDistance), 
                           $"Position {pos} should be within max distance {result.MaxDistance}");
            }
        }
        
        [Test]
        [Category("Unit")]
        public void Should_ExcludeCurrentPosition_When_GeneratingValidPositions()
        {
            // Act
            var result = _movementSystem.CalculateMovement(_character, MovementType.Simple);
            
            // Assert
            var currentPosition = _character.Position;
            var hasCurrentPosition = result.ValidPositions.Any(p => p.Equals(currentPosition));
            Assert.That(hasCurrentPosition, Is.False, "Should exclude current position from valid moves");
        }
        
        // =============================================================================
        // CALCULATE MOVEMENT TESTS - DASH MOVE (2d6)
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_CalculateDashMove_When_UsingMovementTypeDash()
        {
            // Act
            var result = _movementSystem.CalculateMovement(_character, MovementType.Dash);
            
            // Assert
            Assert.That(result, Is.Not.Null, "Should return movement result");
            Assert.That(result.Character, Is.EqualTo(_character), "Should reference character");
            Assert.That(result.MovementType, Is.EqualTo(MovementType.Dash), "Should be Dash movement");
            Assert.That(result.MoveRoll, Is.Not.Null, "Should have dice roll");
            Assert.That(result.MoveRoll.Is2d6, Is.True, "Dash move should use 2d6");
            Assert.That(result.StaminaCost, Is.EqualTo(1), "Dash move costs 1 stamina");
            Assert.That(result.AllowsSecondAction, Is.False, "Dash move ends turn");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_AddMovementPointsToDashRoll_When_CalculatingDashMove()
        {
            // Arrange - Character has 2 AGI (movement points)
            
            // Act
            var result = _movementSystem.CalculateMovement(_character, MovementType.Dash);
            
            // Assert
            var expectedDistance = result.MoveRoll.Total + 2; // Roll + AGI
            Assert.That(result.MaxDistance, Is.EqualTo(expectedDistance), 
                       "Max distance should be roll + movement points");
            Assert.That(result.MaxDistance, Is.InRange(4, 14), 
                       "Distance should be 2d6 (2-12) + 2 AGI = 4-14");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_HaveMoreRangeInDash_When_ComparingToDashVsSimple()
        {
            // Arrange - Use separate movement systems to get fresh dice sequences
            var simpleMovement = new MovementSystem(new DiceRoller(42));
            var dashMovement = new MovementSystem(new DiceRoller(42));
            
            // Act
            var simpleResult = simpleMovement.CalculateMovement(_character, MovementType.Simple);
            var dashResult = dashMovement.CalculateMovement(_character, MovementType.Dash);
            
            // Assert - Dash should have higher maximum potential (2d6 vs 1d6)
            var simpleMaxPossible = 6 + _character.MovementPoints; // 1d6 max + AGI
            var dashMaxPossible = 12 + _character.MovementPoints;   // 2d6 max + AGI
            
            Assert.That(dashMaxPossible, Is.GreaterThan(simpleMaxPossible),
                       "Dash should have higher maximum potential range");
            
            // Verify the actual results are within expected ranges
            Assert.That(simpleResult.MaxDistance, Is.InRange(3, simpleMaxPossible), 
                       "Simple move should be in expected range");
            Assert.That(dashResult.MaxDistance, Is.InRange(4, dashMaxPossible), 
                       "Dash move should be in expected range");
        }
        
        // =============================================================================
        // MOVEMENT STAT INTEGRATION TESTS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_HandleZeroMovementPoints_When_AgilityIsZero()
        {
            // Arrange
            var lowAgilityCharacter = new CharacterTestBuilder()
                .WithAgility(0) // 0 AGI = no movement bonus
                .WithPosition(8, 8)
                .Build();
            
            // Act
            var result = _movementSystem.CalculateMovement(lowAgilityCharacter, MovementType.Simple);
            
            // Assert
            Assert.That(result.MaxDistance, Is.EqualTo(result.MoveRoll.Total), 
                       "Distance should equal just the dice roll with 0 AGI");
            Assert.That(result.MaxDistance, Is.InRange(1, 6), 
                       "Distance should be 1d6 (1-6) + 0 AGI = 1-6");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_HandleHighMovementPoints_When_AgilityIsHigh()
        {
            // Arrange
            var highAgilityCharacter = new CharacterTestBuilder()
                .WithAgility(10) // High AGI = +10 movement bonus
                .WithPosition(8, 8)
                .Build();
            
            // Act
            var result = _movementSystem.CalculateMovement(highAgilityCharacter, MovementType.Simple);
            
            // Assert
            var expectedDistance = result.MoveRoll.Total + 10; // Roll + 10 AGI
            Assert.That(result.MaxDistance, Is.EqualTo(expectedDistance), 
                       "Distance should be roll + 10 AGI");
            Assert.That(result.MaxDistance, Is.InRange(11, 16), 
                       "Distance should be 1d6 (1-6) + 10 AGI = 11-16");
            Assert.That(result.ValidPositions.Count, Is.GreaterThan(50), 
                       "High AGI should provide many movement options");
        }
        
        [Test]
        [Category("Unit")]
        [TestCaseSource(nameof(MovementStatTestCases))]
        public void Should_CalculateCorrectDistance_When_UsingVariousAgilityValues(
            int agility, MovementType moveType, int minExpected, int maxExpected, string description)
        {
            // Arrange
            var character = new CharacterTestBuilder()
                .WithAgility(agility)
                .WithPosition(8, 8)
                .Build();
            
            // Act
            var result = _movementSystem.CalculateMovement(character, moveType);
            
            // Assert
            Assert.That(result.MaxDistance, Is.InRange(minExpected, maxExpected), 
                       $"{description}: Expected {minExpected}-{maxExpected}, got {result.MaxDistance}");
        }
        
        // =============================================================================
        // EXECUTE MOVEMENT TESTS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_ExecuteMovement_When_TargetPositionIsValid()
        {
            // Arrange
            var moveResult = _movementSystem.CalculateMovement(_character, MovementType.Simple);
            var targetPosition = moveResult.ValidPositions.First();
            var originalPosition = new Position(_character.Position.X, _character.Position.Y);
            var originalStamina = _character.CurrentStamina;
            
            // Act
            var executed = _movementSystem.ExecuteMovement(moveResult, targetPosition);
            
            // Assert
            Assert.That(executed, Is.True, "Should successfully execute movement");
            Assert.That(_character.Position, Is.EqualTo(targetPosition), "Character should move to target");
            Assert.That(_character.CurrentStamina, Is.EqualTo(originalStamina - 1), "Should consume 1 stamina");
            Assert.That(moveResult.FinalPosition, Is.EqualTo(targetPosition), "Result should record final position");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_FailToExecute_When_TargetPositionIsInvalid()
        {
            // Arrange
            var moveResult = _movementSystem.CalculateMovement(_character, MovementType.Simple);
            var invalidPosition = new Position(0, 0); // Likely outside range from center
            var originalPosition = new Position(_character.Position.X, _character.Position.Y);
            var originalStamina = _character.CurrentStamina;
            
            // Act
            var executed = _movementSystem.ExecuteMovement(moveResult, invalidPosition);
            
            // Assert
            Assert.That(executed, Is.False, "Should fail to execute invalid movement");
            Assert.That(_character.Position, Is.EqualTo(originalPosition), "Character should not move");
            Assert.That(_character.CurrentStamina, Is.EqualTo(originalStamina), "Should not consume stamina");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_FailToExecute_When_InsufficientStamina()
        {
            // Arrange
            _character.UseStamina(10); // Drain all stamina
            var moveResult = _movementSystem.CalculateMovement(_character, MovementType.Simple);
            var targetPosition = moveResult.ValidPositions.First();
            var originalPosition = new Position(_character.Position.X, _character.Position.Y);
            
            // Act
            var executed = _movementSystem.ExecuteMovement(moveResult, targetPosition);
            
            // Assert
            Assert.That(executed, Is.False, "Should fail without stamina");
            Assert.That(_character.Position, Is.EqualTo(originalPosition), "Character should not move");
            Assert.That(_character.CurrentStamina, Is.EqualTo(0), "Stamina should remain 0");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_CalculateActualDistance_When_ExecutingMovement()
        {
            // Arrange
            var moveResult = _movementSystem.CalculateMovement(_character, MovementType.Simple);
            var targetPosition = moveResult.ValidPositions.First();
            var originalPosition = new Position(_character.Position.X, _character.Position.Y);
            
            // Act
            _movementSystem.ExecuteMovement(moveResult, targetPosition);
            
            // Assert
            var expectedDistance = CalculateManhattanDistance(originalPosition, targetPosition);
            Assert.That(moveResult.ActualDistance, Is.EqualTo(expectedDistance), 
                       "Should calculate actual Manhattan distance moved");
        }
        
        // =============================================================================
        // GRID BOUNDARY TESTS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_RespectGridBoundaries_When_CalculatingMovement()
        {
            // Arrange - Character near edge
            var edgeCharacter = new CharacterTestBuilder()
                .WithAgility(10) // High movement to test boundaries
                .WithPosition(1, 1) // Near corner
                .Build();
            
            // Act
            var result = _movementSystem.CalculateMovement(edgeCharacter, MovementType.Dash);
            
            // Assert
            foreach (var pos in result.ValidPositions)
            {
                Assert.That(pos.X, Is.InRange(0, 15), "X should be within grid bounds 0-15");
                Assert.That(pos.Y, Is.InRange(0, 15), "Y should be within grid bounds 0-15");
            }
        }
        
        [Test]
        [Category("Unit")]
        public void Should_HandleCornerPosition_When_MovementIsLimited()
        {
            // Arrange - Character in corner
            var cornerCharacter = new CharacterTestBuilder()
                .WithAgility(1)
                .WithPosition(0, 0) // Top-left corner
                .Build();
            
            // Act
            var result = _movementSystem.CalculateMovement(cornerCharacter, MovementType.Simple);
            
            // Assert
            Assert.That(result.ValidPositions.Count, Is.GreaterThan(0), "Should have some valid positions");
            
            // All positions should be valid grid coordinates
            foreach (var pos in result.ValidPositions)
            {
                Assert.That(pos.X, Is.GreaterThanOrEqualTo(0), "X should not be negative");
                Assert.That(pos.Y, Is.GreaterThanOrEqualTo(0), "Y should not be negative");
                Assert.That(pos.X, Is.LessThan(16), "X should be within grid");
                Assert.That(pos.Y, Is.LessThan(16), "Y should be within grid");
            }
        }
        
        [Test]
        [Category("Unit")]
        [TestCaseSource(nameof(GridBoundaryTestCases))]
        public void Should_ValidateGridBoundaries_When_UsingEdgePositions(
            int startX, int startY, string description)
        {
            // Arrange
            var character = new CharacterTestBuilder()
                .WithAgility(5) // Moderate movement
                .WithPosition(startX, startY)
                .Build();
            
            // Act
            var result = _movementSystem.CalculateMovement(character, MovementType.Dash);
            
            // Assert
            Assert.That(result.ValidPositions.Count, Is.GreaterThan(0), 
                       $"{description}: Should have valid positions");
            
            foreach (var pos in result.ValidPositions)
            {
                Assert.That(pos.X, Is.InRange(0, 15), $"{description}: X should be in bounds");
                Assert.That(pos.Y, Is.InRange(0, 15), $"{description}: Y should be in bounds");
            }
        }
        
        // =============================================================================
        // MANHATTAN DISTANCE TESTS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_UseCorrectManhattanDistance_When_CalculatingValidPositions()
        {
            // Arrange
            var character = new CharacterTestBuilder()
                .WithAgility(0) // No bonus, just dice
                .WithPosition(8, 8)
                .Build();
            
            // Force a specific roll by using deterministic dice
            var result = _movementSystem.CalculateMovement(character, MovementType.Simple);
            var maxDistance = result.MaxDistance;
            
            // Act & Assert
            foreach (var pos in result.ValidPositions)
            {
                var actualDistance = CalculateManhattanDistance(character.Position, pos);
                Assert.That(actualDistance, Is.LessThanOrEqualTo(maxDistance), 
                           $"Position {pos} distance {actualDistance} should be ≤ {maxDistance}");
                Assert.That(actualDistance, Is.GreaterThan(0), 
                           "Distance should be > 0 (current position excluded)");
            }
        }
        
        [Test]
        [Category("Unit")]
        [TestCaseSource(nameof(ManhattanDistanceTestCases))]
        public void Should_CalculateCorrectManhattanDistance_When_UsingKnownPositions(
            int x1, int y1, int x2, int y2, int expectedDistance, string description)
        {
            // Arrange
            var pos1 = new Position(x1, y1);
            var pos2 = new Position(x2, y2);
            
            // Act
            var distance = CalculateManhattanDistance(pos1, pos2);
            
            // Assert
            Assert.That(distance, Is.EqualTo(expectedDistance), 
                       $"{description}: Expected {expectedDistance}, got {distance}");
        }
        
        // =============================================================================
        // INTEGRATION WITH TEST SCENARIOS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_WorkWithTestScenarios_When_UsingMovementHelpers()
        {
            // Arrange
            var character = TestScenarios.Movement.CenterPosition();
            var movementSystem = TestScenarios.DiceScenarios.DeterministicMovement(42);
            
            // Act
            var result = movementSystem.CalculateMovement(character, MovementType.Simple);
            
            // Assert
            Assert.That(result, Is.Not.Null, "Should work with test scenarios");
            Assert.That(result.Character.Name, Is.EqualTo("Scout"), "Should use Scout character");
            Assert.That(result.Character.MovementPoints, Is.EqualTo(3), "Scout should have 3 AGI");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_HandleQuickSetup_When_UsingTestHelpers()
        {
            // Arrange & Act
            var (character, movement) = TestScenarios.QuickSetup.BasicMovementTest();
            var result = movement.CalculateMovement(character, MovementType.Dash);
            
            // Assert
            Assert.That(result, Is.Not.Null, "Should work with quick setup");
            Assert.That(result.MovementType, Is.EqualTo(MovementType.Dash));
            Assert.That(character.Position.X, Is.EqualTo(8), "Should be in center");
            Assert.That(character.Position.Y, Is.EqualTo(8), "Should be in center");
        }
        
        // =============================================================================
        // MOVEMENT RESULT VALIDATION TESTS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_FormatCorrectly_When_UsingToString()
        {
            // Arrange
            var result = _movementSystem.CalculateMovement(_character, MovementType.Simple);
            
            // Act
            var formatted = result.ToString();
            
            // Assert
            Assert.That(formatted, Does.Contain("TestMover"), "Should include character name");
            Assert.That(formatted, Does.Contain("Simple Move"), "Should include movement type");
            Assert.That(formatted, Does.Contain("can act again"), "Should mention second action");
            Assert.That(formatted, Does.Contain(_character.MovementPoints.ToString()), "Should include AGI");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_FormatWithFinalPosition_When_MovementExecuted()
        {
            // Arrange
            var result = _movementSystem.CalculateMovement(_character, MovementType.Dash);
            var targetPosition = result.ValidPositions.First();
            
            // Act
            _movementSystem.ExecuteMovement(result, targetPosition);
            var formatted = result.ToString();
            
            // Assert
            Assert.That(formatted, Does.Contain("moved"), "Should mention movement occurred");
            Assert.That(formatted, Does.Contain(targetPosition.X.ToString()), "Should include final X");
            Assert.That(formatted, Does.Contain(targetPosition.Y.ToString()), "Should include final Y");
            Assert.That(formatted, Does.Contain("turn ends"), "Dash should mention turn ending");
        }
        
        // =============================================================================
        // EDGE CASES AND ERROR HANDLING
        // =============================================================================
        
        [Test]
		[Category("Unit")]
		public void Should_HandleNullCharacter_When_CalculatingMovement()
		{
			// Act & Assert
			Assert.Throws<NullReferenceException>(() => 
				_movementSystem.CalculateMovement(null, MovementType.Simple));
		}
        
        [Test]
        [Category("Unit")]
        public void Should_HandleInvalidMovementType_When_UsingUndefinedEnum()
        {
            // Arrange
            var invalidType = (MovementType)999;
            
            // Act & Assert - Should handle gracefully or throw appropriate exception
            Assert.DoesNotThrow(() => _movementSystem.CalculateMovement(_character, invalidType),
                              "Should handle invalid enum gracefully");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_HandleExtremelyHighAgility_When_MovementIsVeryLarge()
        {
            // Arrange
            var superFastCharacter = new CharacterTestBuilder()
                .WithAgility(100) // Extremely high AGI
                .WithPosition(8, 8)
                .Build();
            
            // Act
            var result = _movementSystem.CalculateMovement(superFastCharacter, MovementType.Dash);
            
            // Assert
            Assert.That(result.MaxDistance, Is.InRange(102, 112), "Should handle high AGI");
            Assert.That(result.ValidPositions.Count, Is.GreaterThan(100), "Should have many positions");
            
            // All positions should still be within grid
            foreach (var pos in result.ValidPositions)
            {
                Assert.That(pos.X, Is.InRange(0, 15), "Should still respect grid bounds");
                Assert.That(pos.Y, Is.InRange(0, 15), "Should still respect grid bounds");
            }
        }
        
        // =============================================================================
        // UTILITY METHODS
        // =============================================================================
        
        /// <summary>
        /// Calculate Manhattan distance between two positions
        /// </summary>
        private int CalculateManhattanDistance(Position from, Position to)
        {
            return Math.Abs(from.X - to.X) + Math.Abs(from.Y - to.Y);
        }
        
        // =============================================================================
        // TEST DATA
        // =============================================================================
        
        /// <summary>
        /// Test cases for movement stat calculations
        /// </summary>
        private static readonly object[] MovementStatTestCases = 
        {
            new object[] { 0, MovementType.Simple, 1, 6, "0 AGI Simple: 1d6 + 0 = 1-6" },
            new object[] { 1, MovementType.Simple, 2, 7, "1 AGI Simple: 1d6 + 1 = 2-7" },
            new object[] { 5, MovementType.Simple, 6, 11, "5 AGI Simple: 1d6 + 5 = 6-11" },
            new object[] { 0, MovementType.Dash, 2, 12, "0 AGI Dash: 2d6 + 0 = 2-12" },
            new object[] { 2, MovementType.Dash, 4, 14, "2 AGI Dash: 2d6 + 2 = 4-14" },
            new object[] { 10, MovementType.Dash, 12, 22, "10 AGI Dash: 2d6 + 10 = 12-22" },
        };
        
        /// <summary>
        /// Test cases for grid boundary validation
        /// </summary>
        private static readonly object[] GridBoundaryTestCases = 
        {
            new object[] { 0, 0, "Top-left corner" },
            new object[] { 15, 0, "Top-right corner" },
            new object[] { 0, 15, "Bottom-left corner" },
            new object[] { 15, 15, "Bottom-right corner" },
            new object[] { 0, 8, "Left edge center" },
            new object[] { 15, 8, "Right edge center" },
            new object[] { 8, 0, "Top edge center" },
            new object[] { 8, 15, "Bottom edge center" },
        };
        
        /// <summary>
        /// Test cases for Manhattan distance calculations
        /// </summary>
        private static readonly object[] ManhattanDistanceTestCases = 
        {
            new object[] { 0, 0, 0, 0, 0, "Same position" },
            new object[] { 0, 0, 1, 0, 1, "One step right" },
            new object[] { 0, 0, 0, 1, 1, "One step up" },
            new object[] { 0, 0, 1, 1, 2, "One step diagonal" },
            new object[] { 5, 5, 8, 9, 7, "Multi-step movement" },
            new object[] { 0, 0, 3, 4, 7, "3-4 right triangle" },
            new object[] { 10, 10, 5, 15, 10, "Negative X, positive Y" },
        };
    }
}