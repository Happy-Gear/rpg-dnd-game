using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Characters;
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
        private MovementSystem _movementSystem;
        
        // Movement state management
        private MovementResult _pendingMovement;
        private bool _waitingForMovementTarget;
        
        // Post-evasion movement state
        private bool _waitingForEvasionMovement;
        private DefenseResult _pendingEvasionResult;
        private Character _evadingCharacter;
        
        // Action economy tracking - FIX FOR THE MOVE ACTION BUG
        private int _actionsUsedThisTurn;
        private int _maxActionsThisTurn;
        private bool _firstActionWasMove; // Track if first action allows second action
        
        public bool GameActive => _turnManager.GameActive;
        public Character CurrentActor => _turnManager.CurrentActor;
        
        public GameManager(int gridWidth = 16, int gridHeight = 16)
        {
            _turnManager = new TurnManager();
            _combatSystem = new CombatSystem();
            _gridDisplay = new GridDisplay(gridWidth, gridHeight);
            _players = new List<Character>();
            _waitingForDefenseChoice = false;
            _waitingForMovementTarget = false;
            _waitingForEvasionMovement = false;
            _pendingMovement = null;
            _pendingEvasionResult = null;
            _evadingCharacter = null;
            _movementSystem = new MovementSystem(_combatSystem.DiceRoller);
            
            // Initialize action tracking
            ResetActionTracking();
        }
        
        /// <summary>
        /// Reset action tracking for a new turn
        /// </summary>
        private void ResetActionTracking()
        {
            _actionsUsedThisTurn = 0;
            _maxActionsThisTurn = 1; // Default: most actions end turn
            _firstActionWasMove = false;
        }
        
        /// <summary>
        /// Check if the current actor can perform another action this turn
        /// </summary>
        private bool CanPerformAnotherAction()
        {
            return _actionsUsedThisTurn < _maxActionsThisTurn;
        }
        
        /// <summary>
        /// Record that an action was used and determine if more actions are allowed
        /// </summary>
        private bool UseAction(bool allowsSecondAction = false)
        {
            _actionsUsedThisTurn++;
            
            // If this is the first action and it allows a second action, increase limit
            if (_actionsUsedThisTurn == 1 && allowsSecondAction)
            {
                _maxActionsThisTurn = 2;
                _firstActionWasMove = true;
            }
            
            // Return true if turn should continue, false if turn should end
            return CanPerformAnotherAction();
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
            
            // Get first turn and reset action tracking
            var firstTurn = _turnManager.NextTurn();
            ResetActionTracking();
            
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
            
            // Handle post-evasion movement if waiting for one
            if (_waitingForEvasionMovement)
            {
                return ProcessEvasionMovement(input);
            }
            
            // Handle movement target selection if waiting for one
            if (_waitingForMovementTarget)
            {
                return ProcessMovementTarget(input);
            }
            
            // Check if player can perform another action this turn
            if (!CanPerformAnotherAction())
            {
                return "You have used all your actions this turn. Advancing to next turn...\n\n" + 
                       AdvanceToNextTurn();
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
        /// Advance to the next turn and reset action tracking
        /// </summary>
        private string AdvanceToNextTurn()
        {
            var nextTurn = _turnManager.NextTurn();
            ResetActionTracking();
            _gridDisplay.UpdateCharacters(_players);
            
            return _gridDisplay.CreateFullDisplay(
                $"{nextTurn.Message}\n" +
                GetTurnInstructions(nextTurn)
            );
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
                case ActionChoice.Dash:
                    result = HandleDashAction(action.TargetPosition);
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
        /// Handle movement after successful evasion
        /// </summary>
        private string ProcessEvasionMovement(string input)
        {
            var parts = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length >= 2 && int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y))
            {
                var targetPosition = new Position(x, y);
                
                // Check if position is valid for evasion movement
                var validPositions = _gridDisplay.GetValidMovePositions(_evadingCharacter);
                var filteredPositions = validPositions.Where(pos => {
                    int distance = Math.Abs(_evadingCharacter.Position.X - pos.X) + Math.Abs(_evadingCharacter.Position.Y - pos.Y);
                    return distance <= _pendingEvasionResult.MovementDistance;
                }).ToList();
                
                if (filteredPositions.Any(p => p.Equals(targetPosition)))
                {
                    // Execute the evasion movement
                    var characterName = _evadingCharacter.Name;
                    _evadingCharacter.Position = targetPosition;
                    
                    // Clear evasion state
                    _waitingForEvasionMovement = false;
                    _pendingEvasionResult = null;
                    _evadingCharacter = null;
                    
                    // Continue to next turn and reset action tracking
                    return _gridDisplay.CreateFullDisplay(
                        $"{characterName} moves to ({targetPosition.X},{targetPosition.Y}) after evasion.\n\n"
                    ) + AdvanceToNextTurn();
                }
                else
                {
                    return "Invalid position! Choose from the available evasion positions.";
                }
            }
            else if (input.ToLower().Trim() == "skip" || input.ToLower().Trim() == "continue")
            {
                // Skip movement, continue to next turn
                var characterName = _evadingCharacter.Name;
                _waitingForEvasionMovement = false;
                _pendingEvasionResult = null;
                _evadingCharacter = null;
                
                return _gridDisplay.CreateFullDisplay(
                    $"{characterName} skips evasion movement.\n\n"
                ) + AdvanceToNextTurn();
            }
            else
            {
                return "Enter coordinates as 'x y' (e.g., '7 8') or type 'skip' to continue without moving.";
            }
        }
        
        /// <summary>
        /// Handle movement target selection
        /// </summary>
        private string ProcessMovementTarget(string input)
        {
            var parts = input.ToLower().Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length >= 2 && int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y))
            {
                var targetPosition = new Position(x, y);
                
                // Execute movement with stored dice result
                if (_movementSystem.ExecuteMovement(_pendingMovement, targetPosition))
                {
                    var result = $"{_pendingMovement.Character.Name} moved to ({targetPosition.X},{targetPosition.Y})\n";
                    
                    // Clear pending movement
                    _waitingForMovementTarget = false;
                    
                    if (_pendingMovement.AllowsSecondAction)
                    {
                        // Simple move - record action and check if turn continues
                        bool turnContinues = UseAction(allowsSecondAction: true);
                        
                        if (turnContinues)
                        {
                            result += "Used 1 stamina, can still act once more!\n\n";
                            result += $"{_pendingMovement.Character.Name}'s turn continues...\n";
                            result += $"Actions used: {_actionsUsedThisTurn}/{_maxActionsThisTurn}\n";
                            result += "Available actions:\n";
                            result += "  'attack [letter]' - Attack adjacent character\n";
                            result += "  'move' - Move again (1d6)\n";
                            result += "  'dash' - Dash move (2d6, ends turn)\n";
                            result += "  'rest' - Recover stamina\n";
                            result += "  'defend' - Take defensive stance";
                            
                            _pendingMovement = null;
                            return _gridDisplay.CreateFullDisplay(result);
                        }
                        else
                        {
                            // Turn should end (shouldn't normally happen for simple moves)
                            result += "Turn ends.\n\n";
                            _pendingMovement = null;
                            return _gridDisplay.CreateFullDisplay(result) + AdvanceToNextTurn();
                        }
                    }
                    else
                    {
                        // Dash move - record action (ends turn)
                        UseAction(allowsSecondAction: false);
                        result += "Used 1 stamina, turn ends.\n\n";
                        
                        _pendingMovement = null;
                        return _gridDisplay.CreateFullDisplay(result) + AdvanceToNextTurn();
                    }
                }
                else
                {
                    return _gridDisplay.CreateFullDisplay(
                        "Invalid position! Choose a position within your movement range.\n" +
                        GetMovementOptions(_pendingMovement)
                    );
                }
            }
            else
            {
                return "Please enter coordinates as 'x y' (e.g., '5 7')";
            }
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
                        "Move closer to attack. Use 'move' or 'dash' to reposition."
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
            
            // Execute attack and record action (attacks always end turn after defense)
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
                "  'move' - Spend 1 stamina, roll 2d6+MOV evasion vs attack\n" +
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
            
            // Record the attack action (attacks end turns after resolution)
            UseAction(allowsSecondAction: false);
            
            // Handle movement after successful evasion
            if (defenseResult.DefenseChoice == DefenseChoice.Move && defenseResult.CanMove)
            {
                // Set up evasion movement state
                _waitingForEvasionMovement = true;
                _pendingEvasionResult = defenseResult;
                _evadingCharacter = _defendingCharacter;
                
                resultMessage += $"\n{_defendingCharacter.Name} can now move up to {defenseResult.MovementDistance} spaces.\n";
                resultMessage += "Available movement positions:\n";
                
                var movePositions = _gridDisplay.GetValidMovePositions(_defendingCharacter);
                var filteredPositions = movePositions.Where(pos => {
                    int distance = Math.Abs(_defendingCharacter.Position.X - pos.X) + Math.Abs(_defendingCharacter.Position.Y - pos.Y);
                    return distance <= defenseResult.MovementDistance;
                }).ToList();
                
                foreach (var pos in filteredPositions.Take(10))
                {
                    resultMessage += $"  → ({pos.X},{pos.Y})\n";
                }
                resultMessage += "Enter coordinates as 'x y' (e.g., '7 8') or type 'skip' to continue.\n";
                
                // Clear defense state but don't advance turn yet
                _waitingForDefenseChoice = false;
                _pendingAttack = null;
                _defendingCharacter = null;
                
                return _gridDisplay.CreateFullDisplay(resultMessage);
            }
            
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
            
            // Move to next turn and reset action tracking
            return _gridDisplay.CreateFullDisplay(resultMessage + "\n") + AdvanceToNextTurn();
        }
        
        /// <summary>
        /// Handle movement action with dice rolling
        /// </summary>
        private string HandleMoveAction(Position targetPosition = null)
        {
            var character = CurrentActor;
            
            if (targetPosition != null)
            {
                // Direct move to specific position - roll dice and execute immediately
                var moveResult = _movementSystem.CalculateMovement(character, MovementType.Simple);
                var result = $"{character.Name} moves: {moveResult.MoveRoll} + {character.MovementPoints} MOV = {moveResult.MaxDistance} movement points\n\n";
                
                if (_movementSystem.ExecuteMovement(moveResult, targetPosition))
                {
                    // Record action and check if turn continues
                    bool turnContinues = UseAction(allowsSecondAction: true);
                    
                    result += $"{character.Name} moved to ({targetPosition.X},{targetPosition.Y})\n";
                    
                    if (turnContinues)
                    {
                        result += "Used 1 stamina, can still act once more!\n\n";
                        result += $"{character.Name}'s turn continues...\n";
                        result += $"Actions used: {_actionsUsedThisTurn}/{_maxActionsThisTurn}\n";
                        result += "Available actions:\n";
                        result += "  'attack [letter]' - Attack adjacent character\n";
                        result += "  'move' - Move again (1d6)\n";
                        result += "  'dash' - Dash move (2d6, ends turn)\n";
                        result += "  'rest' - Recover stamina\n";
                        result += "  'defend' - Take defensive stance";
                        
                        return _gridDisplay.CreateFullDisplay(result);
                    }
                    else
                    {
                        // Turn ends after second action
                        result += "Used second action, turn ends.\n\n";
                        return _gridDisplay.CreateFullDisplay(result) + AdvanceToNextTurn();
                    }
                }
                else
                {
                    return _gridDisplay.CreateFullDisplay(
                        result + "Invalid move position! Choose a position within your movement range.\n" +
                        GetMovementOptions(moveResult)
                    );
                }
            }
            else
            {
                // Show movement options - roll dice once and store result
                _pendingMovement = _movementSystem.CalculateMovement(character, MovementType.Simple);
                _waitingForMovementTarget = true;
                
                var result = $"{character.Name} moves: {_pendingMovement.MoveRoll} + {character.MovementPoints} MOV = {_pendingMovement.MaxDistance} movement points\n\n";
                
                return _gridDisplay.CreateFullDisplay(
                    result + GetMovementOptions(_pendingMovement) + 
                    "\n\nEnter coordinates as 'x y' (e.g., '5 7')"
                );
            }
        }
        
        /// <summary>
        /// Handle dash movement action
        /// </summary>
        private string HandleDashAction(Position targetPosition = null)
        {
            var character = CurrentActor;
            
            if (targetPosition != null)
            {
                // Direct dash to specific position - roll dice and execute immediately
                var moveResult = _movementSystem.CalculateMovement(character, MovementType.Dash);
                var result = $"{character.Name} dashes: {moveResult.MoveRoll} + {character.MovementPoints} MOV = {moveResult.MaxDistance} movement points\n\n";
                
                if (_movementSystem.ExecuteMovement(moveResult, targetPosition))
                {
                    // Record action (dash always ends turn)
                    UseAction(allowsSecondAction: false);
                    
                    result += $"{character.Name} dashed to ({targetPosition.X},{targetPosition.Y})\n";
                    result += "Used 1 stamina, turn ends.\n\n";
                    
                    return _gridDisplay.CreateFullDisplay(result) + AdvanceToNextTurn();
                }
                else
                {
                    return _gridDisplay.CreateFullDisplay(
                        result + "Invalid dash position! Choose a position within your movement range.\n" +
                        GetMovementOptions(moveResult)
                    );
                }
            }
            else
            {
                // Show dash options - roll dice once and store result
                _pendingMovement = _movementSystem.CalculateMovement(character, MovementType.Dash);
                _waitingForMovementTarget = true;
                
                var result = $"{character.Name} dashes: {_pendingMovement.MoveRoll} + {character.MovementPoints} MOV = {_pendingMovement.MaxDistance} movement points\n\n";
                
                return _gridDisplay.CreateFullDisplay(
                    result + GetMovementOptions(_pendingMovement) + 
                    "\n\nEnter coordinates as 'x y' (e.g., '8 10')"
                );
            }
        }

        /// <summary>
        /// Get movement options display
        /// </summary>
        private string GetMovementOptions(MovementResult moveResult)
        {
            var options = $"Available positions within {moveResult.MaxDistance} movement:\n";
            
            var sortedPositions = moveResult.ValidPositions
                .OrderBy(p => p.X)
                .ThenBy(p => p.Y)
                .Take(20); // Limit display for readability
            
            foreach (var pos in sortedPositions)
            {
                var distance = Math.Abs(moveResult.OriginalPosition.X - pos.X) + Math.Abs(moveResult.OriginalPosition.Y - pos.Y);
                options += $"  → ({pos.X},{pos.Y}) [distance: {distance}]\n";
            }
            
            if (moveResult.ValidPositions.Count > 20)
            {
                options += $"  ... and {moveResult.ValidPositions.Count - 20} more positions\n";
            }
            
            return options;
        }
        
        /// <summary>
        /// Handle rest action
        /// </summary>
        private string HandleRestAction()
        {
            var character = CurrentActor;
            var staminaRestored = Math.Min(5, character.MaxStamina - character.CurrentStamina);
            character.RestoreStamina(staminaRestored);
            
            // Record action (rest ends turn)
            UseAction(allowsSecondAction: false);
            
            var result = $"{character.Name} rests and recovers {staminaRestored} stamina.\n\n";
            return _gridDisplay.CreateFullDisplay(result) + AdvanceToNextTurn();
        }
        
        /// <summary>
        /// Handle defend action (defensive stance)
        /// </summary>
        private string HandleDefendAction()
        {
            var character = CurrentActor;
            
            // Record action (defend ends turn)
            UseAction(allowsSecondAction: false);
            
            var result = $"{character.Name} takes a defensive stance.\n\n";
            return _gridDisplay.CreateFullDisplay(result) + AdvanceToNextTurn();
        }
        
        /// <summary>
        /// Place players on grid at starting positions
        /// </summary>
        private void PlacePlayersOnGrid()
        {
            var positions = new List<Position>
            {
                new Position(2, 2),   // Alice - bottom-left area
                new Position(5, 5), // Bob - top-right area
                new Position(2, 13),  // Player 3 - top-left area
                new Position(13, 2)   // Player 4 - bottom-right area
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
                    
                case "dash":
                    if (parts.Length >= 3 && int.TryParse(parts[1], out int dashX) && int.TryParse(parts[2], out int dashY))
                    {
                        return new ParsedAction { ActionType = ActionChoice.Dash, TargetPosition = new Position(dashX, dashY) };
                    }
                    return new ParsedAction { ActionType = ActionChoice.Dash };
                    
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
                instructions += "  'move' - Roll 1d6 movement, allows second action\n";
            if (turn.AvailableActions.Contains(ActionChoice.Move))
                instructions += "  'dash' - Roll 2d6 movement, ends turn\n";
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
                   "  move - Roll 1d6, move that distance, can act again ONCE\n" +
                   "  dash - Roll 2d6, move that distance, turn ends\n" +
                   "  move/dash x y - Move to specific position (if in range)\n" +
                   "  rest - Recover stamina\n" +
                   "  defend - Take defensive stance\n\n" +
                   "Examples: 'attack B', 'move', 'dash 8 10', 'rest'\n" +
                   "Characters are shown by their first letter on the grid.\n\n" +
                   "Action Economy: Most actions end your turn immediately.\n" +
                   "Only 'move' allows one additional action before ending turn.";
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