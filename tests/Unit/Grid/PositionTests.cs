using System;
using System.Collections.Generic;
using NUnit.Framework;
using RPGGame.Grid;
using RPGGame.Tests.TestHelpers;

namespace RPGGame.Tests.Unit.Grid
{
    /// <summary>
    /// Tests for Position class - handles all spatial logic for grid-based combat
    /// Critical for: Attack range, movement validation, grid display, Unity integration
    /// Must be mathematically accurate for fair gameplay
    /// </summary>
    [TestFixture]
    public class PositionTests : TestBase
    {
        [Test]
        [Category("Unit")]
        public void Should_CreatePosition_When_UsingConstructorWith2D()
        {
            // Arrange & Act
            var position = new Position(5, 7);
            
            // Assert
            Assert.That(position.X, Is.EqualTo(5), "X coordinate should be set correctly");
            Assert.That(position.Y, Is.EqualTo(7), "Y coordinate should be set correctly");
            Assert.That(position.Z, Is.EqualTo(0), "Z coordinate should default to 0");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_CreatePosition_When_UsingConstructorWith3D()
        {
            // Arrange & Act
            var position = new Position(3, 4, 2);
            
            // Assert
            Assert.That(position.X, Is.EqualTo(3), "X coordinate should be set correctly");
            Assert.That(position.Y, Is.EqualTo(4), "Y coordinate should be set correctly");
            Assert.That(position.Z, Is.EqualTo(2), "Z coordinate should be set correctly");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_HandleNegativeCoordinates_When_CreatingPosition()
        {
            // Arrange & Act
            var position = new Position(-2, -3, -1);
            
            // Assert
            Assert.That(position.X, Is.EqualTo(-2), "Negative X should be allowed");
            Assert.That(position.Y, Is.EqualTo(-3), "Negative Y should be allowed");
            Assert.That(position.Z, Is.EqualTo(-1), "Negative Z should be allowed");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_HandleZeroCoordinates_When_CreatingPosition()
        {
            // Arrange & Act
            var position = new Position(0, 0, 0);
            
            // Assert
            Assert.That(position.X, Is.EqualTo(0), "Zero X should be allowed");
            Assert.That(position.Y, Is.EqualTo(0), "Zero Y should be allowed");
            Assert.That(position.Z, Is.EqualTo(0), "Zero Z should be allowed");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_HandleLargeCoordinates_When_CreatingPosition()
        {
            // Arrange & Act
            var position = new Position(1000, 2000, 500);
            
            // Assert
            Assert.That(position.X, Is.EqualTo(1000), "Large X should be allowed");
            Assert.That(position.Y, Is.EqualTo(2000), "Large Y should be allowed");
            Assert.That(position.Z, Is.EqualTo(500), "Large Z should be allowed");
        }
        
        // =============================================================================
        // DISTANCE CALCULATION TESTS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_CalculateZeroDistance_When_ComparingToSelf()
        {
            // Arrange
            var position = new Position(5, 5);
            
            // Act
            var distance = position.DistanceTo(position);
            
            // Assert
            Assert.That(distance, Is.EqualTo(0.0).Within(0.001), "Distance to self should be 0");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_CalculateCorrectDistance_When_MovingHorizontally()
        {
            // Arrange
            var start = new Position(0, 0);
            var end = new Position(3, 0);
            
            // Act
            var distance = start.DistanceTo(end);
            
            // Assert
            Assert.That(distance, Is.EqualTo(3.0).Within(0.001), "Horizontal distance should be 3");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_CalculateCorrectDistance_When_MovingVertically()
        {
            // Arrange
            var start = new Position(0, 0);
            var end = new Position(0, 4);
            
            // Act
            var distance = start.DistanceTo(end);
            
            // Assert
            Assert.That(distance, Is.EqualTo(4.0).Within(0.001), "Vertical distance should be 4");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_CalculateCorrectDistance_When_MovingDiagonally()
        {
            // Arrange
            var start = new Position(0, 0);
            var end = new Position(3, 4); // Classic 3-4-5 triangle
            
            // Act
            var distance = start.DistanceTo(end);
            
            // Assert
            Assert.That(distance, Is.EqualTo(5.0).Within(0.001), "Diagonal distance should be 5 (3-4-5 triangle)");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_CalculateCorrectDistance_When_Using3DCoordinates()
        {
            // Arrange
            var start = new Position(0, 0, 0);
            var end = new Position(1, 1, 1);
            var expected = Math.Sqrt(3); // sqrt(1² + 1² + 1²)
            
            // Act
            var distance = start.DistanceTo(end);
            
            // Assert
            Assert.That(distance, Is.EqualTo(expected).Within(0.001), "3D distance should be √3");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_BeSymmetric_When_CalculatingDistance()
        {
            // Arrange
            var pos1 = new Position(2, 3);
            var pos2 = new Position(7, 8);
            
            // Act
            var distance1to2 = pos1.DistanceTo(pos2);
            var distance2to1 = pos2.DistanceTo(pos1);
            
            // Assert
            Assert.That(distance1to2, Is.EqualTo(distance2to1).Within(0.001), 
                       "Distance should be symmetric: d(A,B) = d(B,A)");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_HandleNegativeCoordinates_When_CalculatingDistance()
        {
            // Arrange
            var pos1 = new Position(-3, -4);
            var pos2 = new Position(0, 0);
            
            // Act
            var distance = pos1.DistanceTo(pos2);
            
            // Assert
            Assert.That(distance, Is.EqualTo(5.0).Within(0.001), "Distance with negatives should be 5");
        }
        
        [Test]
        [Category("Unit")]
        [TestCaseSource(nameof(DistanceTestCases))]
        public void Should_CalculateCorrectDistance_When_UsingKnownDistances(
            int x1, int y1, int z1, int x2, int y2, int z2, double expectedDistance, string description)
        {
            // Arrange
            var pos1 = new Position(x1, y1, z1);
            var pos2 = new Position(x2, y2, z2);
            
            // Act
            var distance = pos1.DistanceTo(pos2);
            
            // Assert
            Assert.That(distance, Is.EqualTo(expectedDistance).Within(0.001), 
                       $"{description}: Expected {expectedDistance}, got {distance}");
        }
        
        // =============================================================================
        // ADJACENCY TESTS (Critical for attack range)
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_BeAdjacent_When_ComparingToSelf()
        {
            // Arrange
            var position = new Position(5, 5);
            
            // Act
            var isAdjacent = position.IsAdjacent(position);
            
            // Assert
            Assert.That(isAdjacent, Is.True, "Position is technically adjacent to itself (distance 0 ≤ 1.5)");
            
            // Note: In game logic, you typically wouldn't attack yourself,
            // but the mathematical definition includes distance 0
        }
        
        [Test]
        [Category("Unit")]
        public void Should_BeAdjacent_When_OneStepAway()
        {
            // Arrange
            var center = new Position(5, 5);
            var adjacent = new Position(6, 5); // One step right
            
            // Act
            var isAdjacent = center.IsAdjacent(adjacent);
            
            // Assert
            Assert.That(isAdjacent, Is.True, "Should be adjacent when one step away");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_BeAdjacent_When_DiagonallyOneStepAway()
        {
            // Arrange
            var center = new Position(5, 5);
            var diagonal = new Position(6, 6); // One step diagonal
            var expectedDistance = Math.Sqrt(2); // ~1.414
            
            // Act
            var distance = center.DistanceTo(diagonal);
            var isAdjacent = center.IsAdjacent(diagonal);
            
            // Assert
            Assert.That(distance, Is.EqualTo(expectedDistance).Within(0.001), "Diagonal distance should be √2");
            Assert.That(isAdjacent, Is.True, "Should be adjacent when diagonally one step away");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_NotBeAdjacent_When_TwoStepsAway()
        {
            // Arrange
            var center = new Position(5, 5);
            var farAway = new Position(7, 5); // Two steps right
            
            // Act
            var isAdjacent = center.IsAdjacent(farAway);
            
            // Assert
            Assert.That(isAdjacent, Is.False, "Should not be adjacent when two steps away");
        }
        
        [Test]
        [Category("Unit")]
        [TestCaseSource(nameof(AdjacencyTestCases))]
        public void Should_DetermineAdjacency_When_UsingKnownPositions(
            int centerX, int centerY, int testX, int testY, bool expectedAdjacent, string description)
        {
            // Arrange
            var center = new Position(centerX, centerY);
            var test = new Position(testX, testY);
            
            // Act
            var isAdjacent = center.IsAdjacent(test);
            
            // Assert
            Assert.That(isAdjacent, Is.EqualTo(expectedAdjacent), 
                       $"{description}: Expected {expectedAdjacent}, got {isAdjacent}");
        }
        
        // =============================================================================
        // EQUALITY TESTS (Critical for position comparisons)
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_BeEqual_When_ComparingIdenticalPositions()
        {
            // Arrange
            var pos1 = new Position(3, 4, 5);
            var pos2 = new Position(3, 4, 5);
            
            // Act & Assert
            Assert.That(pos1.Equals(pos2), Is.True, "Identical positions should be equal");
            Assert.That(pos1.GetHashCode(), Is.EqualTo(pos2.GetHashCode()), "Hash codes should match");
            
            // Note: == operator not implemented in Position class
        }
        
        [Test]
        [Category("Unit")]
        public void Should_NotBeEqual_When_ComparingDifferentPositions()
        {
            // Arrange
            var pos1 = new Position(3, 4, 5);
            var pos2 = new Position(3, 4, 6); // Different Z
            
            // Act & Assert
            Assert.That(pos1.Equals(pos2), Is.False, "Different positions should not be equal");
            Assert.That(pos1.GetHashCode(), Is.Not.EqualTo(pos2.GetHashCode()), "Hash codes should differ");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_NotBeEqual_When_ComparingToNull()
        {
            // Arrange
            var position = new Position(1, 2, 3);
            
            // Act & Assert
            Assert.That(position.Equals(null), Is.False, "Position should not equal null");
            Assert.That(position.Equals((object)null), Is.False, "Position should not equal null object");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_NotBeEqual_When_ComparingToNonPosition()
        {
            // Arrange
            var position = new Position(1, 2, 3);
            var notPosition = "not a position";
            
            // Act & Assert
            Assert.That(position.Equals(notPosition), Is.False, "Position should not equal non-Position object");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_BeReflexive_When_ComparingToSelf()
        {
            // Arrange
            var position = new Position(7, 8, 9);
            
            // Act & Assert
            Assert.That(position.Equals(position), Is.True, "Position should equal itself (reflexive)");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_BeSymmetric_When_ComparingPositions()
        {
            // Arrange
            var pos1 = new Position(1, 2);
            var pos2 = new Position(1, 2);
            
            // Act & Assert
            Assert.That(pos1.Equals(pos2), Is.EqualTo(pos2.Equals(pos1)), 
                       "Equality should be symmetric: if A=B then B=A");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_BeTransitive_When_ComparingThreeEqualPositions()
        {
            // Arrange
            var pos1 = new Position(5, 6);
            var pos2 = new Position(5, 6);
            var pos3 = new Position(5, 6);
            
            // Act & Assert
            Assert.That(pos1.Equals(pos2), Is.True, "pos1 should equal pos2");
            Assert.That(pos2.Equals(pos3), Is.True, "pos2 should equal pos3");
            Assert.That(pos1.Equals(pos3), Is.True, "pos1 should equal pos3 (transitive)");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_HaveConsistentHashCodes_When_PositionsAreEqual()
        {
            // Arrange
            var pos1 = new Position(10, 20, 30);
            var pos2 = new Position(10, 20, 30);
            
            // Act
            var hash1 = pos1.GetHashCode();
            var hash2 = pos2.GetHashCode();
            
            // Assert
            Assert.That(hash1, Is.EqualTo(hash2), "Equal positions must have equal hash codes");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_HaveDifferentHashCodes_When_PositionsDiffer()
        {
            // Arrange - Test many different positions to verify hash distribution
            var positions = new List<Position>
            {
                new Position(0, 0, 0),
                new Position(1, 0, 0),
                new Position(0, 1, 0),
                new Position(0, 0, 1),
                new Position(1, 1, 1),
                new Position(-1, -1, -1),
                new Position(100, 200, 300)
            };
            
            var hashCodes = new HashSet<int>();
            
            // Act
            foreach (var pos in positions)
            {
                hashCodes.Add(pos.GetHashCode());
            }
            
            // Assert - Most should have different hash codes (perfect distribution not required)
            Assert.That(hashCodes.Count, Is.GreaterThan(positions.Count / 2), 
                       "Most different positions should have different hash codes");
        }
        
        // =============================================================================
        // STRING REPRESENTATION TESTS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_FormatAs2D_When_ZIsZero()
        {
            // Arrange
            var position = new Position(5, 7, 0);
            
            // Act
            var formatted = position.ToString();
            
            // Assert
            Assert.That(formatted, Is.EqualTo("(5,7)"), "Should format as 2D when Z=0");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_FormatAs3D_When_ZIsNonZero()
        {
            // Arrange
            var position = new Position(3, 4, 2);
            
            // Act
            var formatted = position.ToString();
            
            // Assert
            Assert.That(formatted, Is.EqualTo("(3,4,2)"), "Should format as 3D when Z≠0");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_FormatNegatives_When_UsingNegativeCoordinates()
        {
            // Arrange
            var position2D = new Position(-1, -2);
            var position3D = new Position(-3, -4, -5);
            
            // Act
            var formatted2D = position2D.ToString();
            var formatted3D = position3D.ToString();
            
            // Assert
            Assert.That(formatted2D, Is.EqualTo("(-1,-2)"), "Should format negative 2D correctly");
            Assert.That(formatted3D, Is.EqualTo("(-3,-4,-5)"), "Should format negative 3D correctly");
        }
        
        [Test]
        [Category("Unit")]
        public void Should_FormatOrigin_When_UsingZeroCoordinates()
        {
            // Arrange
            var origin2D = new Position(0, 0);
            var origin3D = new Position(0, 0, 0);
            
            // Act
            var formatted2D = origin2D.ToString();
            var formatted3D = origin3D.ToString();
            
            // Assert
            Assert.That(formatted2D, Is.EqualTo("(0,0)"), "Should format 2D origin correctly");
            Assert.That(formatted3D, Is.EqualTo("(0,0)"), "Should format 3D origin as 2D when Z=0");
        }
        
        // =============================================================================
        // GAME-SPECIFIC POSITION TESTS
        // =============================================================================
        
        [Test]
        [Category("Unit")]
        public void Should_HandleGridPositions_When_UsingGameCoordinates()
        {
            // Arrange - Test common game grid positions (16x16 grid)
            var corner1 = new Position(0, 0);      // Top-left
            var corner2 = new Position(15, 0);     // Top-right
            var corner3 = new Position(0, 15);     // Bottom-left
            var corner4 = new Position(15, 15);    // Bottom-right
            var center = new Position(8, 8);       // Center
            
            // Act & Assert - All should be valid positions
            AssertValidPosition(corner1, maxX: 16, maxY: 16);
            AssertValidPosition(corner2, maxX: 16, maxY: 16);
            AssertValidPosition(corner3, maxX: 16, maxY: 16);
            AssertValidPosition(corner4, maxX: 16, maxY: 16);
            AssertValidPosition(center, maxX: 16, maxY: 16);
        }
        
        [Test]
        [Category("Unit")]
        public void Should_CalculateAttackRangeCorrectly_When_UsingAdjacentPositions()
        {
            // Arrange - Center position with all 8 adjacent positions + self
            var center = new Position(5, 5);
            var adjacentPositions = new List<Position>
            {
                new Position(4, 4), new Position(4, 5), new Position(4, 6), // Left column
                new Position(5, 4), new Position(5, 5), new Position(5, 6), // Center column (including self)
                new Position(6, 4), new Position(6, 5), new Position(6, 6)  // Right column
            };
            
            // Act & Assert - All should be adjacent (mathematically speaking)
            foreach (var pos in adjacentPositions)
            {
                var isAdjacent = center.IsAdjacent(pos);
                var distance = center.DistanceTo(pos);
                
                Assert.That(isAdjacent, Is.True, 
                           $"Position {pos} should be adjacent to center {center}");
                Assert.That(distance, Is.LessThanOrEqualTo(1.5), 
                           $"Distance to {pos} should be ≤1.5 for adjacency");
            }
            
            // Note: In actual game logic, you'd filter out self-targeting in the combat system,
            // not in the Position class which is purely mathematical
        }
        
        [Test]
        [Category("Unit")]
        public void Should_NotBeInAttackRange_When_TwoStepsAway()
        {
            // Arrange
            var center = new Position(8, 8);
            var nonAdjacentPositions = new List<Position>
            {
                new Position(6, 8), // Two steps left
                new Position(10, 8), // Two steps right
                new Position(8, 6), // Two steps up
                new Position(8, 10), // Two steps down
                new Position(6, 6), // Two steps diagonal
                new Position(10, 10) // Two steps diagonal
            };
            
            // Act & Assert - None should be adjacent
            foreach (var pos in nonAdjacentPositions)
            {
                var isAdjacent = center.IsAdjacent(pos);
                var distance = center.DistanceTo(pos);
                
                Assert.That(isAdjacent, Is.False, 
                           $"Position {pos} should NOT be adjacent to center {center}");
                Assert.That(distance, Is.GreaterThan(1.5), 
                           $"Distance to {pos} should be >1.5 for non-adjacency");
            }
        }
        
        // =============================================================================
        // TEST DATA
        // =============================================================================
        
        /// <summary>
        /// Test cases for distance calculations with known results
        /// </summary>
        private static readonly object[] DistanceTestCases = 
        {
            // 2D cases
            new object[] { 0, 0, 0, 3, 4, 0, 5.0, "3-4-5 triangle (2D)" },
            new object[] { 0, 0, 0, 5, 12, 0, 13.0, "5-12-13 triangle (2D)" },
            new object[] { 1, 1, 0, 4, 5, 0, 5.0, "Translated 3-4-5 triangle" },
            new object[] { -3, -4, 0, 0, 0, 0, 5.0, "Negative coordinates" },
            new object[] { 10, 10, 0, 10, 10, 0, 0.0, "Same position" },
            
            // 3D cases
            new object[] { 0, 0, 0, 1, 1, 1, 1.732, "Unit cube diagonal" },
            new object[] { 0, 0, 0, 2, 2, 2, 3.464, "2x2x2 cube diagonal" },
            new object[] { 1, 2, 3, 4, 6, 8, 7.071, "3D Pythagorean" },
            
            // Edge cases
            new object[] { 0, 0, 0, 1, 0, 0, 1.0, "Unit X distance" },
            new object[] { 0, 0, 0, 0, 1, 0, 1.0, "Unit Y distance" },
            new object[] { 0, 0, 0, 0, 0, 1, 1.0, "Unit Z distance" },
        };
        
        /// <summary>
        /// Test cases for adjacency calculations (attack range logic)
        /// </summary>
        private static readonly object[] AdjacencyTestCases = 
        {
            // Adjacent positions (should return true)
            new object[] { 5, 5, 5, 5, true, "Same position (mathematically adjacent - distance 0)" },
            new object[] { 5, 5, 6, 5, true, "One step right" },
            new object[] { 5, 5, 4, 5, true, "One step left" },
            new object[] { 5, 5, 5, 6, true, "One step up" },
            new object[] { 5, 5, 5, 4, true, "One step down" },
            new object[] { 5, 5, 6, 6, true, "One step diagonal (NE)" },
            new object[] { 5, 5, 4, 4, true, "One step diagonal (SW)" },
            new object[] { 5, 5, 4, 6, true, "One step diagonal (NW)" },
            new object[] { 5, 5, 6, 4, true, "One step diagonal (SE)" },
            
            // Non-adjacent positions (should return false)
            new object[] { 5, 5, 7, 5, false, "Two steps right" },
            new object[] { 5, 5, 3, 5, false, "Two steps left" },
            new object[] { 5, 5, 5, 7, false, "Two steps up" },
            new object[] { 5, 5, 5, 3, false, "Two steps down" },
            new object[] { 5, 5, 7, 7, false, "Two steps diagonal" },
            new object[] { 5, 5, 8, 9, false, "Knight's move (not adjacent)" },
            new object[] { 0, 0, 10, 10, false, "Far away diagonal" },
        };
    }
}