using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Character;
using RPGGame.Combat;
using RPGGame.Core;
using RPGGame.Display;
using RPGGame.Grid;

namespace RPGGame.Core
{
    /// <summary>
    /// Main game manager that orchestrates combat with ASCII grid display
    /// </summary>
    public class GameManager
    {
        private TurnManager _turnManager;
        private CombatSystem _combatSystem;
        private GridDisplay _gridDisplay;
        private List<Character> _players;
        private bool _waitingForDefenseChoice;
        private AttackResult _pendingAttack;
        private Character _defendingCharacter;
        
        public bool GameActive => _turnManager.GameActive;
        public Character CurrentActor => _turnManager.CurrentActor;
        
        public GameManager(int gridWidth = 10, int gridHeight = 8)
        {
            _turnManager = new TurnManager();
            _combatSystem = new CombatSystem();
            _gridDisplay = new GridDisplay(gridWidth, gridHeight);
            _players = new List<Character>();
            _waitingForDefenseChoice = false;
        }
        
        /// <summary>
        /// Initialize a new game with players
        /// </summary>
        public string StartGame(params Character[] players)
        {
            _players = players.ToList();
            
            // Place players on grid (simple initial positioning)
            PlacePlayersOnGrid();
            
            // Start combat
            _turnManager.StartCombat(_players.ToArray());
            _gridDisplay.UpdateCharacters(_players);
            
            // Get first turn
            var firstTurn = _turnManager.NextTurn();
            
            return _gridDisplay.CreateFullDisplay(
                $"Game started! {firstTurn.Message}\n" +
                GetTurnInstructions(firstTurn)
            );
        }
        
        /// <summary>
        /// Process player action input
        /// </summary>
        public string ProcessAction(string input)
        {
            if (!GameActive)
                return "Game is not active.";
            
            // Handle defense choice if waiting for one
            if (_waitingForDefenseChoice)
            {
                return ProcessDefenseChoice(input);
            }
            
            // Parse normal action
            var action = ParseAction(input);
            if (action == null)
            {
                return GetHelpText();
            }
            
            return ExecuteAction(action);
        }
        
        /// <summary>
        /// Execute a parsed action
        /// </summary>
        private string ExecuteAction(ParsedAction action)
        {
            var result = "";
            
            switch (action.ActionType)
            {
                case ActionChoice.Attack:
                    result = HandleAttackAction(action.Target);
                    break;
                case ActionChoice.Move:
                    result = HandleMoveAction(action.TargetPosition);
                    break;
                case ActionChoice.Rest:
                    result = HandleRestAction();
                    break;
                case ActionChoice.Defend:
                    result = HandleDefendAction();
                    break;
                default:
                    result = "Invalid action.";
                    break;
            }
            
            _gridDisplay.UpdateCharacters(_players);
            return result;
        }
        
        /// <summary>
        /// Handle attack action with range checking
        /// </summary>
        private string HandleAttackAction(Character target)
        {
            var attacker = CurrentActor;
            
            // Validate target
            if (target == null)
            {
                var validTargets = _gridDisplay.GetCharactersInAttackRange(attacker);
                if (!validTargets.Any())
                {
                    return _gridDisplay.CreateFullDisplay(
                        "No valid targets in range!\n" +
                        "Move closer to attack. Use 'move x y' to reposition."
                    );
                }
                
                return _gridDisplay.CreateFullDisplay(
                    "Choose a target to attack:\n" +
                    _gridDisplay.DrawAttackRange(attacker) +
                    "Use 'attack [character letter]' (e.g., 'attack B')"
                );
            }
            
            // Check if target is in range
            if (!_gridDisplay.IsInAttackRange(attacker.Position, target.Position))
            {
                return _gridDisplay.CreateFullDisplay(
                    $"{target.Name} is not in attack range! You must be adjacent (including diagonal).\n" +
                    _gridDisplay.DrawAttackRange(attacker)
                );
            }
            
            // Execute attack
            var attackResult = _combatSystem.ExecuteAttack(attacker, target);
            
            if (!attackResult.Success)
            {
                return _gridDisplay.CreateFullDisplay(attackResult.Message);
            }
            
            // Store pending attack and wait for defense choice
            _pendingAttack = attackResult;
            _defendingCharacter = target;
            _waitingForDefenseChoice = true;
            
            return _gridDisplay.CreateFullDisplay(
                $"{attackResult.Message}\n\n" +
                $"{target.Name}, choose your response:\n" +
                "  'defend' - Spend 2 stamina, roll 2d6+DEF, build counter on over-defense\n" +
                "  'move' - Spend 1 stamina, avoid damage completely\n" +
                "  'take' - Save stamina, take full damage\n" +
                $"\nTarget has {target.CurrentStamina} stamina available."
            );
        }
        
        /// <summary>
        /// Process defender's choice
        /// </summary>
        private string ProcessDefenseChoice(string input)
        {
            var choice = ParseDefenseChoice(input);
            if (choice == null)
            {
                return "Invalid defense choice. Use: 'defend', 'move', or 'take'";
            }
            
            // Execute defense
            var defenseResult = _combatSystem.ResolveDefense(_defendingCharacter, _pendingAttack, choice.Value);
            
            var resultMessage = $"{_pendingAttack.Message}\n{defenseResult.Message}\n";
            
            // Check for counter attack opportunity
            if (defenseResult.CounterReady)
            {
                resultMessage += $"\n⚡ {_defendingCharacter.Name}'s counter gauge is READY! " +
                               "They can use a counter attack on their turn!\n";
            }
            
            // Clear pending attack
            _waitingForDefenseChoice = false;
            _pendingAttack = null;
            _defendingCharacter = null;
            
            // Check if game ended
            if (!_turnManager.CurrentActor.IsAlive || _players.Count(p => p.IsAlive) <= 1)
            {
                var winner = _players.FirstOrDefault(p => p.IsAlive);
                return _gridDisplay.CreateFullDisplay(
                    resultMessage + $"\n🎉 GAME OVER! {winner?.Name} wins!"
                );
            }
            
            // Move to next turn
            var nextTurn = _turnManager.NextTurn();
            
            return _gridDisplay.CreateFullDisplay(
                resultMessage + $"\n{nextTurn.Message}\n" +
                GetTurnInstructions(nextTurn)
            );
        }
        
        /// <summary>
        /// Handle movement action
        /// </summary>
        private string HandleMoveAction(Position targetPosition)
        {
            var character = CurrentActor;
            
            if (targetPosition == null)
            {
                return _gridDisplay.CreateFullDisplay(
                    "Choose where to move:\n" +
                    _gridDisplay.DrawMovementOptions(character) +
                    "Use 'move x y' (e.g., 'move 3 4')"
                );
            }
            
            // Validate movement
            var validMoves = _gridDisplay.GetValidMovePositions(character);
            if (!validMoves.Any(p => p.Equals(targetPosition)))
            {
                return _gridDisplay.CreateFullDisplay(
                    "Invalid move position!\n" +
                    _gridDisplay.DrawMovementOptions(character)
                );
            }
            
            // Execute movement
            character.UseStamina(1);
            character.Position = targetPosition;
            
            var nextTurn = _turnManager.NextTurn();
            
            return _gridDisplay.CreateFullDisplay(
                $"{character.Name} moved to ({targetPosition.X},{targetPosition.Y})\n\n" +
                $"{nextTurn.Message}\n" +
                GetTurnInstructions(nextTurn)
            );
        }
        
        /// <summary>
        /// Handle rest action
        /// </summary>
        private string HandleRestAction()
        {
            var character = CurrentActor;
            var staminaRestored = Math.Min(5, character.MaxStamina - character.CurrentStamina);
            character.RestoreStamina(staminaRestored);
            
            var nextTurn = _turnManager.NextTurn();
            
            return _gridDisplay.CreateFullDisplay(
                $"{character.Name} rests and recovers {staminaRestored} stamina.\n\n" +
                $"{nextTurn.Message}\n" +
                GetTurnInstructions(nextTurn)
            );
        }
        
        /// <summary>
        /// Handle defend action (defensive stance)
        /// </summary>
        private string HandleDefendAction()
        {
            var character = CurrentActor;
            var nextTurn = _turnManager.NextTurn();
            
            return _gridDisplay.CreateFullDisplay(
                $"{character.Name} takes a defensive stance.\n\n" +
                $"{nextTurn.Message}\n" +
                GetTurnInstructions(nextTurn)
            );
        }
        
        /// <summary>
        /// Place players on grid at starting positions
        /// </summary>
        private void PlacePlayersOnGrid()
        {
            var positions = new List<Position>
            {
                new Position(1, 1),
                new Position(8, 6),
                new Position(2, 6),
                new Position(7, 1)
            };
            
            for (int i = 0; i < _players.Count && i < positions.Count; i++)
            {
                _players[i].Position = positions[i];
            }
        }
        
        /// <summary>
        /// Parse user input into actions
        /// </summary>
        private ParsedAction ParseAction(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            
            var parts = input.ToLower().Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var command = parts[0];
            
            switch (command)
            {
                case "attack" or "atk":
                    if (parts.Length > 1)
                    {
                        var target = FindCharacterByLetter(parts[1][0]);
                        return new ParsedAction { ActionType = ActionChoice.Attack, Target = target };
                    }
                    return new ParsedAction { ActionType = ActionChoice.Attack };
                    
                case "move" or "mov":
                    if (parts.Length >= 3 && int.TryParse(parts[1], out int x) && int.TryParse(parts[2], out int y))
                    {
                        return new ParsedAction { ActionType = ActionChoice.Move, TargetPosition = new Position(x, y) };
                    }
                    return new ParsedAction { ActionType = ActionChoice.Move };
                    
                case "rest":
                    return new ParsedAction { ActionType = ActionChoice.Rest };
                    
                case "defend" or "def":
                    return new ParsedAction { ActionType = ActionChoice.Defend };
                    
                default:
                    return null;
            }
        }
        
        /// <summary>
        /// Parse defense choice
        /// </summary>
        private DefenseChoice? ParseDefenseChoice(string input)
        {
            return input.ToLower().Trim() switch
            {
                "defend" or "def" => DefenseChoice.Defend,
                "move" or "mov" => DefenseChoice.Move,
                "take" => DefenseChoice.TakeDamage,
                _ => null
            };
        }
        
        /// <summary>
        /// Find character by first letter of name
        /// </summary>
        private Character FindCharacterByLetter(char letter)
        {
            return _players.FirstOrDefault(p => 
                p.IsAlive && char.ToUpper(p.Name[0]) == char.ToUpper(letter));
        }
        
        /// <summary>
        /// Get instructions for current turn
        /// </summary>
        private string GetTurnInstructions(TurnResult turn)
        {
            if (!turn.Success) return turn.Message;
            
            var instructions = "Available actions:\n";
            
            if (turn.AvailableActions.Contains(ActionChoice.Attack))
                instructions += "  'attack [letter]' - Attack adjacent character (e.g., 'attack B')\n";
            if (turn.AvailableActions.Contains(ActionChoice.Move))
                instructions += "  'move x y' - Move to position (e.g., 'move 3 4')\n";
            if (turn.AvailableActions.Contains(ActionChoice.Rest))
                instructions += "  'rest' - Recover stamina\n";
            if (turn.AvailableActions.Contains(ActionChoice.Defend))
                instructions += "  'defend' - Take defensive stance\n";
            
            instructions += "\nType 'help' for more information.";
            
            return instructions;
        }
        
        /// <summary>
        /// Get help text
        /// </summary>
        private string GetHelpText()
        {
            return "Commands:\n" +
                   "  attack [letter] - Attack character (must be adjacent)\n" +
                   "  move x y - Move to grid position\n" +
                   "  rest - Recover stamina\n" +
                   "  defend - Take defensive stance\n\n" +
                   "Examples: 'attack B', 'move 3 4', 'rest'\n" +
                   "Characters are shown by their first letter on the grid.";
        }
    }
    
    /// <summary>
    /// Parsed action from user input
    /// </summary>
    internal class ParsedAction
    {
        public ActionChoice ActionType { get; set; }
        public Character Target { get; set; }
        public Position TargetPosition { get; set; }
    }
}