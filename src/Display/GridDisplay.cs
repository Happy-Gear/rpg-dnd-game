using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RPGGame.Characters;
using RPGGame.Grid;

namespace RPGGame.Display
{
    /// <summary>
    /// ASCII art grid display for combat visualization
    /// </summary>
    public class GridDisplay
    {
        private int _width;
        private int _height;
        private List<Character> _characters;
        
        public GridDisplay(int width = 10, int height = 8)
        {
            _width = width;
            _height = height;
            _characters = new List<Character>();
        }
        
        /// <summary>
        /// Update character positions for display
        /// </summary>
        public void UpdateCharacters(List<Character> characters)
        {
            _characters = characters?.Where(c => c.IsAlive).ToList() ?? new List<Character>();
        }
        
        /// <summary>
        /// Generate ASCII grid with character positions
        /// </summary>
        public string DrawGrid()
        {
            var sb = new StringBuilder();
            
            // Draw grid with characters at junction points (+)
            for (int y = _height - 1; y >= 0; y--) // Top to bottom for proper display
            {
			// Draw row with character positions
			for (int x = 0; x < _width; x++)
			{
				char displayChar = GetCharacterAtPosition(x, y);
				sb.Append(displayChar);
				
				// Add spaces between grid points (except last column)
				if (x < _width - 1)
				{
					sb.Append("   "); // 3 spaces between positions
				}
			}
			sb.AppendLine();

			// Add vertical spacing (except last row)
			if (y > 0)
			{
				sb.AppendLine(); // Empty line for vertical spacing
			}
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Get character letter at specific position, or '+' if empty
        /// </summary>
        private char GetCharacterAtPosition(int x, int y)
        {
            var character = _characters.FirstOrDefault(c => 
                c.Position.X == x && c.Position.Y == y);
            
            if (character != null)
            {
                // Use first letter of character name
                return char.ToUpper(character.Name[0]);
            }
            
            return '.'; // Empty junction point
        }
        
        /// <summary>
        /// Generate combat status display
        /// </summary>
        public string DrawCombatStatus()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== COMBAT STATUS ===");
            
            foreach (var character in _characters.OrderBy(c => c.Name))
            {
                char symbol = char.ToUpper(character.Name[0]);
                string counter = character.Counter.IsReady ? " [COUNTER READY!]" : 
                               $" (Counter: {character.Counter.Current}/6)";
                               
                sb.AppendLine($"[{symbol}] {character.Name}: " +
                             $"HP {character.CurrentHealth}/{character.MaxHealth}, " +
                             $"SP {character.CurrentStamina}/{character.MaxStamina}" +
                             $"{counter}");
                             
                sb.AppendLine($"    Position: ({character.Position.X},{character.Position.Y}) " +
                             $"ATK:{character.AttackPoints} DEF:{character.DefensePoints} MOV:{character.MovementPoints}");
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Show valid attack targets for a character
        /// </summary>
        public string DrawAttackRange(Character attacker)
        {
            if (attacker == null) return "";
            
            var targets = GetCharactersInAttackRange(attacker);
            
            if (!targets.Any())
            {
                return $"{attacker.Name} has no valid targets in attack range (adjacent positions).\n";
            }
            
            var sb = new StringBuilder();
            sb.AppendLine($"{attacker.Name} can attack:");
            
            foreach (var target in targets)
            {
                char symbol = char.ToUpper(target.Name[0]);
                double distance = attacker.Position.DistanceTo(target.Position);
                sb.AppendLine($"  [{symbol}] {target.Name} at ({target.Position.X},{target.Position.Y}) - Distance: {distance:F1}");
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Get all characters within attack range (8 adjacent positions)
        /// </summary>
        public List<Character> GetCharactersInAttackRange(Character attacker)
        {
            if (attacker == null) return new List<Character>();
            
            return _characters.Where(c => 
                c != attacker && 
                c.IsAlive && 
                IsInAttackRange(attacker.Position, c.Position)
            ).ToList();
        }
        
        /// <summary>
        /// Check if target position is within attack range (adjacent, including diagonal)
        /// </summary>
        public bool IsInAttackRange(Position attackerPos, Position targetPos)
        {
            int dx = Math.Abs(attackerPos.X - targetPos.X);
            int dy = Math.Abs(attackerPos.Y - targetPos.Y);
            
            // Adjacent positions (including diagonal): max 1 step in any direction
            return dx <= 1 && dy <= 1 && !(dx == 0 && dy == 0);
        }
        
        /// <summary>
        /// Display movement options for a character
        /// </summary>
        public string DrawMovementOptions(Character character)
        {
            if (character == null) return "";
            
            var sb = new StringBuilder();
            sb.AppendLine($"{character.Name} movement options:");
            
            var validMoves = GetValidMovePositions(character);
            
            foreach (var pos in validMoves)
            {
                bool wouldBeInDanger = WouldBeInDangerAt(character, pos);
                string dangerWarning = wouldBeInDanger ? " [DANGER!]" : "";
                sb.AppendLine($"  → ({pos.X},{pos.Y}){dangerWarning}");
            }
            
            if (!validMoves.Any())
            {
                sb.AppendLine("  No valid movement positions available.");
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Get valid movement positions (adjacent empty spots)
        /// </summary>
        public List<Position> GetValidMovePositions(Character character)
        {
            var positions = new List<Position>();
            var currentPos = character.Position;
            
            // Check all 8 adjacent positions
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue; // Skip current position
                    
                    var newPos = new Position(currentPos.X + dx, currentPos.Y + dy);
                    
                    // Check bounds
                    if (newPos.X >= 0 && newPos.X < _width && 
                        newPos.Y >= 0 && newPos.Y < _height)
                    {
                        // Check if position is empty
                        if (!_characters.Any(c => c.Position.Equals(newPos)))
                        {
                            positions.Add(newPos);
                        }
                    }
                }
            }
            
            return positions;
        }
        
        /// <summary>
        /// Check if character would be in attack range of enemies at this position
        /// </summary>
        private bool WouldBeInDangerAt(Character character, Position position)
        {
            var enemies = _characters.Where(c => c != character && c.IsAlive);
            
            foreach (var enemy in enemies)
            {
                if (IsInAttackRange(enemy.Position, position))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Create a formatted game display with grid and status
        /// </summary>
        public string CreateFullDisplay(string additionalInfo = "")
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("╔═══════════════════ RPG COMBAT GRID ═══════════════════╗");
            sb.AppendLine();
            sb.Append(DrawGrid());
            sb.AppendLine();
            sb.Append(DrawCombatStatus());
            
            if (!string.IsNullOrEmpty(additionalInfo))
            {
                sb.AppendLine();
                sb.AppendLine("=== GAME INFO ===");
                sb.AppendLine(additionalInfo);
            }
            
            sb.AppendLine("╚══════════════════════════════════════════════════════╝");
            
            return sb.ToString();
        }
    }
}
