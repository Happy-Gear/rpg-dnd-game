using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Dice;
using RPGGame.Grid;
using RPGGame.Characters;

namespace RPGGame.Core
{
    /// <summary>
    /// Enhanced movement system with dice-based movement
    /// </summary>
    public class MovementSystem
    {
        private DiceRoller _diceRoller;
        
        public MovementSystem(DiceRoller diceRoller)
        {
            _diceRoller = diceRoller;
        }
        
        /// <summary>
        /// Calculate movement options based on movement type
        /// </summary>
        public MovementResult CalculateMovement(Character character, MovementType moveType, Position targetDirection = null)
        {
            var result = new MovementResult
            {
                Character = character,
                MovementType = moveType,
                OriginalPosition = new Position(character.Position.X, character.Position.Y)
            };
            
            switch (moveType)
            {
                case MovementType.Simple:
                    result = CalculateSimpleMove(character, targetDirection);
                    break;
                case MovementType.Dash:
                    result = CalculateDashMove(character, targetDirection);
                    break;
            }
            
            return result;
        }
        
        /// <summary>
        /// Simple move: 1d6 distance, allows second action
        /// </summary>
        private MovementResult CalculateSimpleMove(Character character, Position targetDirection)
        {
            var moveRoll = _diceRoller.Roll1d6("Simple Move");
            var distance = moveRoll.Total;
            
            var result = new MovementResult
            {
                Character = character,
                MovementType = MovementType.Simple,
                MoveRoll = moveRoll,
                MaxDistance = distance,
                StaminaCost = 1,
                AllowsSecondAction = true,
                OriginalPosition = new Position(character.Position.X, character.Position.Y)
            };
            
            // Calculate available positions within rolled distance
            result.ValidPositions = GetPositionsWithinDistance(character.Position, distance);
            
            return result;
        }
        
        /// <summary>
        /// Dash move: 2d6 distance, ends turn
        /// </summary>
        private MovementResult CalculateDashMove(Character character, Position targetDirection)
        {
            var moveRoll = _diceRoller.Roll2d6("Dash Move");
            var distance = moveRoll.Total;
            
            var result = new MovementResult
            {
                Character = character,
                MovementType = MovementType.Dash,
                MoveRoll = moveRoll,
                MaxDistance = distance,
                StaminaCost = 1,
                AllowsSecondAction = false,
                OriginalPosition = new Position(character.Position.X, character.Position.Y)
            };
            
            // Calculate available positions within rolled distance
            result.ValidPositions = GetPositionsWithinDistance(character.Position, distance);
            
            return result;
        }
        
        /// <summary>
        /// Get all valid positions within movement distance
        /// </summary>
        private List<Position> GetPositionsWithinDistance(Position start, int maxDistance)
        {
            var positions = new List<Position>();
            
            // Check all positions within Manhattan distance
            for (int x = start.X - maxDistance; x <= start.X + maxDistance; x++)
            {
                for (int y = start.Y - maxDistance; y <= start.Y + maxDistance; y++)
                {
                    if (x == start.X && y == start.Y) continue; // Skip current position
                    
                    var newPos = new Position(x, y);
                    var distance = CalculateManhattanDistance(start, newPos);
                    
                    if (distance <= maxDistance && IsValidGridPosition(newPos))
                    {
                        positions.Add(newPos);
                    }
                }
            }
            
            return positions;
        }
        
        /// <summary>
        /// Calculate Manhattan distance (grid-based movement)
        /// </summary>
        private int CalculateManhattanDistance(Position from, Position to)
        {
            return Math.Abs(from.X - to.X) + Math.Abs(from.Y - to.Y);
        }
        
        /// <summary>
        /// Check if position is within grid bounds
        /// </summary>
        private bool IsValidGridPosition(Position pos)
        {
            return pos.X >= 0 && pos.X < 10 && pos.Y >= 0 && pos.Y < 8; // Grid size 10x8
        }
        
        /// <summary>
        /// Execute the movement
        /// </summary>
        public bool ExecuteMovement(MovementResult moveResult, Position targetPosition)
        {
            if (!moveResult.ValidPositions.Any(p => p.Equals(targetPosition)))
                return false;
            
            // Use stamina
            if (!moveResult.Character.UseStamina(moveResult.StaminaCost))
                return false;
            
            // Move character
            moveResult.Character.Position = targetPosition;
            moveResult.FinalPosition = targetPosition;
            moveResult.ActualDistance = CalculateManhattanDistance(moveResult.OriginalPosition, targetPosition);
            
            return true;
        }
    }
    
    /// <summary>
    /// Types of movement
    /// </summary>
    public enum MovementType
    {
        Simple, // 1d6 distance, allows second action
        Dash    // 2d6 distance, ends turn
    }
    
    /// <summary>
    /// Result of movement calculation
    /// </summary>
    public class MovementResult
    {
        public Character Character { get; set; }
        public MovementType MovementType { get; set; }
        public DiceResult MoveRoll { get; set; }
        public int MaxDistance { get; set; }
        public int ActualDistance { get; set; }
        public int StaminaCost { get; set; }
        public bool AllowsSecondAction { get; set; }
        public Position OriginalPosition { get; set; }
        public Position FinalPosition { get; set; }
        public List<Position> ValidPositions { get; set; } = new List<Position>();
        
        public override string ToString()
        {
            string moveType = MovementType == MovementType.Simple ? "Simple Move" : "Dash";
            string secondAction = AllowsSecondAction ? " (can act again)" : " (turn ends)";
            
            if (FinalPosition != null)
            {
                return $"{Character.Name} {moveType}: {MoveRoll} = {MaxDistance} max distance, " +
                       $"moved {ActualDistance} spaces to ({FinalPosition.X},{FinalPosition.Y}){secondAction}";
            }
            else
            {
                return $"{Character.Name} {moveType}: {MoveRoll} = {MaxDistance} movement points available{secondAction}";
            }
        }
    }
}