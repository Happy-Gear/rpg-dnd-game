using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Dice;
using RPGGame.Grid;
using RPGGame.Characters;

namespace RPGGame.Core
{
    /// <summary>
    /// Dice-based movement system.
    /// Arena bounds come from GameConfig (balance.json) — no hardcoded sizes.
    /// </summary>
    public class MovementSystem
    {
        private DiceRoller _diceRoller;

        // Read arena size from config once so we're not calling it on every bounds check
        private int ArenaWidth  => GameConfig.Current.Grid.Width;
        private int ArenaHeight => GameConfig.Current.Grid.Height;

        public MovementSystem(DiceRoller diceRoller)
        {
            _diceRoller = diceRoller;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Calculate movement options for a character based on movement type.
        /// </summary>
        public MovementResult CalculateMovement(Character character, MovementType moveType, Position targetDirection = null)
        {
            return moveType switch
            {
                MovementType.Simple => CalculateSimpleMove(character),
                MovementType.Dash   => CalculateDashMove(character),
                _                   => CalculateSimpleMove(character)
            };
        }

        /// <summary>
        /// Attempt to execute a movement to the target position.
        /// Returns false if the position is not in the valid set or stamina is insufficient.
        /// </summary>
        public bool ExecuteMovement(MovementResult moveResult, Position targetPosition)
        {
            if (!moveResult.ValidPositions.Any(p => p.Equals(targetPosition)))
                return false;

            if (!moveResult.Character.UseStamina(moveResult.StaminaCost))
                return false;

            moveResult.Character.Position = targetPosition;
            moveResult.FinalPosition      = targetPosition;
            moveResult.ActualDistance     = CalculateManhattanDistance(moveResult.OriginalPosition, targetPosition);

            return true;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Movement type calculations
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Simple move: 1d6 + MOV stat distance, allows second action this turn.
        /// </summary>
        private MovementResult CalculateSimpleMove(Character character)
        {
            var cfg      = GameConfig.Current.Movement.SimpleMove;
            var moveRoll = _diceRoller.Roll1d6("Simple Move");
            int total    = moveRoll.Total + character.MovementPoints;

            return new MovementResult
            {
                Character          = character,
                MovementType       = MovementType.Simple,
                MoveRoll           = moveRoll,
                MaxDistance        = total,
                StaminaCost        = GameConfig.Current.Combat.StaminaCosts.Move,
                AllowsSecondAction = true,
                OriginalPosition   = new Position(character.Position.X, character.Position.Y),
                ValidPositions     = GetPositionsWithinDistance(character.Position, total)
            };
        }

        /// <summary>
        /// Dash move: 2d6 + MOV stat distance, ends turn.
        /// </summary>
        private MovementResult CalculateDashMove(Character character)
        {
            var cfg      = GameConfig.Current.Movement.DashMove;
            var moveRoll = _diceRoller.Roll2d6("Dash Move");
            int total    = moveRoll.Total + character.MovementPoints;

            return new MovementResult
            {
                Character          = character,
                MovementType       = MovementType.Dash,
                MoveRoll           = moveRoll,
                MaxDistance        = total,
                StaminaCost        = GameConfig.Current.Combat.StaminaCosts.Move,
                AllowsSecondAction = false,
                OriginalPosition   = new Position(character.Position.X, character.Position.Y),
                ValidPositions     = GetPositionsWithinDistance(character.Position, total)
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // Spatial helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Return all arena tiles reachable from <paramref name="start"/> within
        /// <paramref name="maxDistance"/> steps (Manhattan distance).
        /// </summary>
        private List<Position> GetPositionsWithinDistance(Position start, int maxDistance)
        {
            var positions = new List<Position>();

            for (int x = start.X - maxDistance; x <= start.X + maxDistance; x++)
            {
                for (int y = start.Y - maxDistance; y <= start.Y + maxDistance; y++)
                {
                    if (x == start.X && y == start.Y) continue;

                    var candidate = new Position(x, y);
                    if (CalculateManhattanDistance(start, candidate) <= maxDistance &&
                        IsInArenaBounds(candidate))
                    {
                        positions.Add(candidate);
                    }
                }
            }

            return positions;
        }

        private static int CalculateManhattanDistance(Position a, Position b)
            => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);

        /// <summary>
        /// Uses arena dimensions from GameConfig — no hardcoded grid size.
        /// </summary>
        private bool IsInArenaBounds(Position pos)
            => pos.X >= 0 && pos.X < ArenaWidth && pos.Y >= 0 && pos.Y < ArenaHeight;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Supporting types
    // ─────────────────────────────────────────────────────────────────────────

    public enum MovementType
    {
        Simple, // 1d6 + MOV, allows second action
        Dash    // 2d6 + MOV, ends turn
    }

    public class MovementResult
    {
        public Character Character          { get; set; }
        public MovementType MovementType    { get; set; }
        public DiceResult MoveRoll          { get; set; }
        public int MaxDistance              { get; set; }
        public int ActualDistance           { get; set; }
        public int StaminaCost              { get; set; }
        public bool AllowsSecondAction      { get; set; }
        public Position OriginalPosition    { get; set; }
        public Position FinalPosition       { get; set; }
        public List<Position> ValidPositions { get; set; } = new List<Position>();

        public override string ToString()
        {
            string type   = MovementType == MovementType.Simple ? "Simple Move" : "Dash";
            string action = AllowsSecondAction ? "(can act again)" : "(turn ends)";

            return FinalPosition != null
                ? $"{Character.Name} {type}: {MoveRoll} + {Character.MovementPoints} MOV = {MaxDistance} total, " +
                  $"moved {ActualDistance} to ({FinalPosition.X},{FinalPosition.Y}) {action}"
                : $"{Character.Name} {type}: {MoveRoll} + {Character.MovementPoints} MOV = {MaxDistance} points {action}";
        }
    }
}