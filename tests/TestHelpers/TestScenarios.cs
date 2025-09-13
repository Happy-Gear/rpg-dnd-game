using System;
using System.Collections.Generic;
using RPGGame.Characters;
using RPGGame.Combat;
using RPGGame.Core;
using RPGGame.Dice;
using RPGGame.Grid;
using RPGGame.Tests.TestHelpers;

namespace RPGGame.Tests.TestHelpers
{
    /// <summary>
    /// Pre-configured test scenarios for common combat, movement, and game situations
    /// Following TEST_COVERAGE_CHECKLIST.md Phase 2 requirements
    /// Provides reusable setups to speed up test writing and ensure consistency
    /// </summary>
    public static class TestScenarios
    {
        // =============================================================================
        // COMBAT POSITIONING SCENARIOS
        // =============================================================================
        
        /// <summary>
        /// Characters positioned for adjacent combat (attack range)
        /// </summary>
        public static class AdjacentCombat
        {
            /// <summary>
            /// Attacker and defender side-by-side (horizontal)
            /// </summary>
            public static (Character attacker, Character defender) Horizontal()
            {
                var attacker = CommonCharacters.Alice()
                    .WithPosition(5, 5)
                    .Build();
                    
                var defender = CommonCharacters.Bob()
                    .WithPosition(6, 5) // One step right
                    .Build();
                    
                return (attacker, defender);
            }
            
            /// <summary>
            /// Attacker and defender vertically adjacent
            /// </summary>
            public static (Character attacker, Character defender) Vertical()
            {
                var attacker = CommonCharacters.Alice()
                    .WithPosition(8, 8)
                    .Build();
                    
                var defender = CommonCharacters.Bob()
                    .WithPosition(8, 9) // One step up
                    .Build();
                    
                return (attacker, defender);
            }
            
            /// <summary>
            /// Diagonal attack positioning
            /// </summary>
            public static (Character attacker, Character defender) Diagonal()
            {
                var attacker = CommonCharacters.Alice()
                    .WithPosition(3, 3)
                    .Build();
                    
                var defender = CommonCharacters.Bob()
                    .WithPosition(4, 4) // Diagonal northeast
                    .Build();
                    
                return (attacker, defender);
            }
            
            /// <summary>
            /// Multiple targets scenario - attacker surrounded
            /// </summary>
            public static (Character attacker, List<Character> defenders) Surrounded()
            {
                var attacker = CommonCharacters.Alice()
                    .WithPosition(8, 8) // Center
                    .Build();
                    
                var defenders = new List<Character>
                {
                    CommonCharacters.Bob().WithName("North").WithPosition(8, 9).Build(),
                    CommonCharacters.Tank().WithName("South").WithPosition(8, 7).Build(),
                    CommonCharacters.Scout().WithName("East").WithPosition(9, 8).Build(),
                    CommonCharacters.GlassCannon().WithName("West").WithPosition(7, 8).Build()
                };
                
                return (attacker, defenders);
            }
        }
        
        /// <summary>
        /// Characters positioned outside attack range
        /// </summary>
        public static class OutOfRange
        {
            /// <summary>
            /// Two steps apart horizontally
            /// </summary>
            public static (Character attacker, Character defender) TwoStepsHorizontal()
            {
                var attacker = CommonCharacters.Alice()
                    .WithPosition(5, 5)
                    .Build();
                    
                var defender = CommonCharacters.Bob()
                    .WithPosition(7, 5) // Two steps right
                    .Build();
                    
                return (attacker, defender);
            }
            
            /// <summary>
            /// Knight's move apart (not adjacent)
            /// </summary>
            public static (Character attacker, Character defender) KnightsMove()
            {
                var attacker = CommonCharacters.Alice()
                    .WithPosition(5, 5)
                    .Build();
                    
                var defender = CommonCharacters.Bob()
                    .WithPosition(7, 6) // Knight's L-shape
                    .Build();
                    
                return (attacker, defender);
            }
            
            /// <summary>
            /// Across the grid (far apart)
            /// </summary>
            public static (Character attacker, Character defender) FarApart()
            {
                var attacker = CommonCharacters.Alice()
                    .WithPosition(1, 1)
                    .Build();
                    
                var defender = CommonCharacters.Bob()
                    .WithPosition(14, 14) // Opposite corners
                    .Build();
                    
                return (attacker, defender);
            }
        }
        
        // =============================================================================
        // MOVEMENT SCENARIOS
        // =============================================================================
        
        /// <summary>
        /// Standard movement test scenarios
        /// </summary>
        public static class Movement
        {
            /// <summary>
            /// Character in center with full movement options
            /// </summary>
            public static Character CenterPosition()
            {
                return CommonCharacters.Scout() // High AGI for movement
                    .WithPosition(8, 8) // Center of 16x16 grid
                    .Build();
            }
            
            /// <summary>
            /// Character in corner (limited movement)
            /// </summary>
            public static Character CornerPosition()
            {
                return CommonCharacters.Scout()
                    .WithPosition(0, 0) // Top-left corner
                    .Build();
            }
            
            /// <summary>
            /// Character near edge (some movement restrictions)
            /// </summary>
            public static Character EdgePosition()
            {
                return CommonCharacters.Scout()
                    .WithPosition(15, 8) // Right edge
                    .Build();
            }
            
            /// <summary>
            /// Low movement character (0 AGI)
            /// </summary>
            public static Character LowMovement()
            {
                return CommonCharacters.Bob() // 0 AGI
                    .WithPosition(8, 8)
                    .Build();
            }
            
            /// <summary>
            /// High movement character (3 AGI)
            /// </summary>
            public static Character HighMovement()
            {
                return CommonCharacters.Scout() // 3 AGI
                    .WithPosition(8, 8)
                    .Build();
            }
            
            /// <summary>
            /// Movement with obstacles (other characters blocking)
            /// </summary>
            public static (Character mover, List<Character> obstacles) WithObstacles()
            {
                var mover = CommonCharacters.Scout()
                    .WithPosition(8, 8)
                    .Build();
                    
                var obstacles = new List<Character>
                {
                    CommonCharacters.Alice().WithName("Block1").WithPosition(9, 8).Build(), // East
                    CommonCharacters.Bob().WithName("Block2").WithPosition(8, 9).Build(),   // North
                    CommonCharacters.Tank().WithName("Block3").WithPosition(7, 7).Build()   // Southwest
                };
                
                return (mover, obstacles);
            }
        }
        
        // =============================================================================
        // RESOURCE STATE SCENARIOS
        // =============================================================================
        
        /// <summary>
        /// Characters with various resource states for testing edge cases
        /// </summary>
        public static class ResourceStates
        {
            /// <summary>
            /// Character with just enough stamina for one attack
            /// </summary>
            public static Character OneAttackLeft()
            {
                return CommonCharacters.Alice()
                    .WithStamina(10)
                    .BuildWithCurrentStamina(3); // Exactly 3 for attack
            }
            
            /// <summary>
            /// Character with just enough stamina for defense
            /// </summary>
            public static Character OneDefenseLeft()
            {
                return CommonCharacters.Alice()
                    .WithStamina(10)
                    .BuildWithCurrentStamina(2); // Exactly 2 for defense
            }
            
            /// <summary>
            /// Character with just enough stamina for movement
            /// </summary>
            public static Character OneMoveLeft()
            {
                return CommonCharacters.Alice()
                    .WithStamina(10)
                    .BuildWithCurrentStamina(1); // Exactly 1 for move
            }
            
            /// <summary>
            /// Completely exhausted character
            /// </summary>
            public static Character Exhausted()
            {
                return CommonCharacters.Alice()
                    .WithStamina(10)
                    .BuildWithCurrentStamina(0); // No stamina
            }
            
            /// <summary>
            /// Nearly dead character (1 HP)
            /// </summary>
            public static Character NearlyDead()
            {
                return CommonCharacters.Alice()
                    .WithHealth(20)
                    .BuildWithCurrentHealth(1); // 1 HP left
            }
            
            /// <summary>
            /// Counter ready for badminton streak
            /// </summary>
            public static Character CounterReady()
            {
                return CommonCharacters.Alice()
                    .WithCounterReady() // 6/6 counter
                    .Build();
            }
            
            /// <summary>
            /// Counter almost ready (5/6)
            /// </summary>
            public static Character CounterAlmostReady()
            {
                return CommonCharacters.Alice()
                    .WithCounter(5) // 5/6 counter
                    .Build();
            }
        }
        
        // =============================================================================
        // COMBAT SEQUENCE SCENARIOS
        // =============================================================================
        
        /// <summary>
        /// Standard combat action sequences for testing
        /// </summary>
        public static class CombatSequences
        {
            /// <summary>
            /// Basic attack -> defend -> resolve sequence
            /// </summary>
            public static class AttackDefend
            {
                public static (Character attacker, Character defender) Setup()
                {
                    return AdjacentCombat.Horizontal();
                }
                
                public static void Execute(CombatSystem combat, Character attacker, Character defender, DefenseChoice choice = DefenseChoice.Defend)
                {
                    var attack = combat.ExecuteAttack(attacker, defender);
                    if (attack.Success)
                    {
                        combat.ResolveDefense(defender, attack, choice);
                    }
                }
            }
            
            /// <summary>
            /// Attack -> defend -> counter attack sequence
            /// </summary>
            public static class BadmintonStreak
            {
                public static (Character attacker, Character defender) Setup()
                {
                    var (att, def) = AdjacentCombat.Horizontal();
                    
                    // Set defender with counter ready
                    def.Counter.AddCounter(6);
                    
                    return (att, def);
                }
                
                public static (AttackResult attack, DefenseResult defense, AttackResult counter) Execute(CombatSystem combat, Character attacker, Character defender)
                {
                    var attack = combat.ExecuteAttack(attacker, defender);
                    var defense = combat.ResolveDefense(defender, attack, DefenseChoice.Defend);
                    var counter = combat.ExecuteCounterAttack(defender, attacker);
                    
                    return (attack, defense, counter);
                }
            }
            
            /// <summary>
            /// Multiple rounds of combat until someone can't continue
            /// </summary>
            public static class ExtendedCombat
            {
                public static (Character fighter1, Character fighter2) Setup()
                {
                    var fighter1 = CommonCharacters.Alice()
                        .WithName("Fighter1")
                        .WithHealth(15)
                        .WithStamina(20)
                        .WithPosition(5, 5)
                        .Build();
                        
                    var fighter2 = CommonCharacters.Bob()
                        .WithName("Fighter2")
                        .WithHealth(15)
                        .WithStamina(20)
                        .WithPosition(6, 5)
                        .Build();
                        
                    return (fighter1, fighter2);
                }
            }
        }
        
        // =============================================================================
        // TURN MANAGEMENT SCENARIOS
        // =============================================================================
        
        /// <summary>
        /// Turn order and game state scenarios
        /// </summary>
        public static class TurnOrder
        {
            /// <summary>
            /// Standard 2-player turn sequence
            /// </summary>
            public static List<Character> TwoPlayers()
            {
                return new List<Character>
                {
                    CommonCharacters.Alice().Build(),
                    CommonCharacters.Bob().Build()
                };
            }
            
            /// <summary>
            /// 4-player free-for-all
            /// </summary>
            public static List<Character> FourPlayers()
            {
                return new List<Character>
                {
                    CommonCharacters.Alice().WithPosition(2, 2).Build(),
                    CommonCharacters.Bob().WithPosition(5, 5).Build(),
                    CommonCharacters.Tank().WithPosition(8, 8).Build(),
                    CommonCharacters.Scout().WithPosition(11, 11).Build()
                };
            }
            
            /// <summary>
            /// Mixed health/stamina states for turn management testing
            /// </summary>
            public static List<Character> MixedStates()
            {
                return new List<Character>
                {
                    ResourceStates.OneAttackLeft(),
                    ResourceStates.NearlyDead(),
                    ResourceStates.CounterReady(),
                    CommonCharacters.Tank().Build() // Healthy tank
                };
            }
            
            /// <summary>
            /// One character dead (turn skipping test)
            /// </summary>
            public static List<Character> OneCharacterDead()
            {
                var characters = TwoPlayers();
                characters[1].TakeDamage(20); // Kill second character
                
                return characters;
            }
        }
        
        // =============================================================================
        // GRID BOUNDARY SCENARIOS
        // =============================================================================
        
        /// <summary>
        /// Grid edge and boundary test scenarios
        /// </summary>
        public static class GridBoundaries
        {
            /// <summary>
            /// All corner positions (16x16 grid)
            /// </summary>
            public static List<Position> Corners()
            {
                return new List<Position>
                {
                    new Position(0, 0),    // Top-left
                    new Position(15, 0),   // Top-right
                    new Position(0, 15),   // Bottom-left
                    new Position(15, 15)   // Bottom-right
                };
            }
            
            /// <summary>
            /// Edge positions (not corners)
            /// </summary>
            public static List<Position> Edges()
            {
                return new List<Position>
                {
                    new Position(8, 0),    // Top edge
                    new Position(15, 8),   // Right edge
                    new Position(8, 15),   // Bottom edge
                    new Position(0, 8)     // Left edge
                };
            }
            
            /// <summary>
            /// Invalid positions (outside grid)
            /// </summary>
            public static List<Position> Invalid()
            {
                return new List<Position>
                {
                    new Position(-1, -1),  // Negative coordinates
                    new Position(16, 16),  // Beyond grid
                    new Position(-5, 8),   // Negative X
                    new Position(8, -5),   // Negative Y
                    new Position(20, 20)   // Way outside
                };
            }
        }
        
        // =============================================================================
        // DETERMINISTIC DICE SCENARIOS
        // =============================================================================
        
        /// <summary>
        /// Pre-configured dice results for predictable testing
        /// </summary>
        public static class DiceScenarios
        {
            /// <summary>
            /// Get deterministic combat system with known first few results
            /// </summary>
            public static CombatSystem DeterministicCombat(int seed = 42)
            {
                return new CombatSystem(seed);
            }
            
            /// <summary>
            /// Get movement system with known results
            /// </summary>
            public static MovementSystem DeterministicMovement(int seed = 42)
            {
                return new MovementSystem(new DiceRoller(seed));
            }
            
            /// <summary>
            /// Known good attack outcome (for seed 42)
            /// Use this when you need an attack to succeed reliably
            /// </summary>
            public static (Character attacker, Character defender, CombatSystem combat) GuaranteedHit()
            {
                var combat = DeterministicCombat(42);
                var (attacker, defender) = AdjacentCombat.Horizontal();
                
                // Seed 42 should give predictable results
                return (attacker, defender, combat);
            }
        }
        
        // =============================================================================
        // UTILITY METHODS
        // =============================================================================
        
        /// <summary>
        /// Quick setup for common test patterns
        /// </summary>
        public static class QuickSetup
        {
            /// <summary>
            /// Basic combat test with deterministic dice
            /// </summary>
            public static (Character attacker, Character defender, CombatSystem combat) BasicCombatTest()
            {
                var (attacker, defender) = AdjacentCombat.Horizontal();
                var combat = new CombatSystem(42); // Deterministic
                
                return (attacker, defender, combat);
            }
            
            /// <summary>
            /// Movement test with character in center
            /// </summary>
            public static (Character character, MovementSystem movement) BasicMovementTest()
            {
                var character = Movement.CenterPosition();
                var movement = new MovementSystem(new DiceRoller(42));
                
                return (character, movement);
            }
            
            /// <summary>
            /// Full game manager setup with two characters
            /// </summary>
            public static (GameManager game, Character player1, Character player2) BasicGameTest()
            {
                var game = new GameManager(16, 16);
                var player1 = CommonCharacters.Alice().Build();
                var player2 = CommonCharacters.Bob().Build();
                
                return (game, player1, player2);
            }
        }
    }
    
    /// <summary>
    /// Extension methods for common test operations
    /// </summary>
    public static class TestScenarioExtensions
    {
        /// <summary>
        /// Position two characters adjacent to each other
        /// </summary>
        public static void PositionAdjacent(this Character char1, Character char2, int centerX = 8, int centerY = 8)
        {
            char1.Position = new Position(centerX, centerY);
            char2.Position = new Position(centerX + 1, centerY); // Adjacent right
        }
        
        /// <summary>
        /// Set character to near-death state
        /// </summary>
        public static Character SetNearDeath(this Character character)
        {
            if (character.CurrentHealth > 1)
            {
                character.TakeDamage(character.CurrentHealth - 1);
            }
            return character;
        }
        
        /// <summary>
        /// Set character to low stamina state
        /// </summary>
        public static Character SetLowStamina(this Character character, int staminaLeft = 1)
        {
            if (character.CurrentStamina > staminaLeft)
            {
                character.UseStamina(character.CurrentStamina - staminaLeft);
            }
            return character;
        }
        
        /// <summary>
        /// Verify character is in valid state for testing
        /// </summary>
        public static bool IsValidForTesting(this Character character)
        {
            return character != null && 
                   character.Position != null && 
                   character.Stats != null && 
                   character.Counter != null;
        }
    }
}