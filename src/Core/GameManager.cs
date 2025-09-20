using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Characters;
using RPGGame.Combat;
using RPGGame.Core;
using RPGGame.Display;
using RPGGame.Grid;
using RPGGame.Input;

namespace RPGGame.Core
{
    /// <summary>
    /// Enhanced game manager with intuitive WASD movement system
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
        private InputHandler _inputHandler;
        
        // Movement state management
        private MovementResult _pendingMovement;
        private bool _waitingForMovementTarget;
        
        // Post-evasion movement state
        private bool _waitingForEvasionMovement;
        private DefenseResult _pendingEvasionResult;
        private Character _evadingCharacter;
        
        // NEW: Enhanced movement state for WASD system
        private bool _inMovementMode;
        private List<Position> _movementPath;
        private Position _movementStartPosition;
        private int _movementPointsUsed;
        private MovementResult _activeMovementResult;
        
        // Action economy tracking
        private int _actionsUsedThisTurn;
        private int _maxActionsThisTurn;
        
        public bool GameActive => _turnManager.GameActive;
        public Character CurrentActor => _turnManager.CurrentActor;
        
        // NEW: Properties for movement mode
        public bool InMovementMode => _inMovementMode;
        public int MovementPointsRemaining => _activeMovementResult?.MaxDistance - _movementPointsUsed ?? 0;
        public List<Position> CurrentPath => new List<Position>(_movementPath ?? new List<Position>());
        
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
            _inputHandler = new InputHandler();
            
            // Initialize movement mode state
            _inMovementMode = false;
            _movementPath = new List<Position>();
            _movementPointsUsed = 0;
            _activeMovementResult = null;
            
            // Initialize action tracking
            ResetActionTracking();
        }
        
        /// <summary>
        /// Reset action tracking for a new turn
        /// </summary>
        private void ResetActionTracking()
        {
            _actionsUsedThisTurn = 0;
            _maxActionsThisTurn = 1;
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
            
            if (_actionsUsedThisTurn == 1 && allowsSecondAction)
            {
                _maxActionsThisTurn = 2;
            }
            
            return CanPerformAnotherAction();
        }
        
        /// <summary>
        /// Initialize a new game with players
        /// </summary>
        public string StartGame(params Character[] players)
        {
            _players = players.ToList();
            PlacePlayersOnGrid();
            _turnManager.StartCombat(_players.ToArray());
            _gridDisplay.UpdateCharacters(_players);
            
            var firstTurn = _turnManager.NextTurn();
            ResetActionTracking();
            
            return _gridDisplay.CreateFullDisplay(
                $"Game started! {firstTurn.Message}\n" +
                GetTurnInstructions(firstTurn)
            );
        }
        
        /// <summary>
        /// Process player action input - Text commands
        /// </summary>
        public string ProcessAction(string input)
        {
            if (!GameActive)
                return "Game is not active.";

            var actionCommand = _inputHandler.ProcessActionInput(input);
            return ProcessGameAction(actionCommand);
        }
        
        /// <summary>
        /// Process direct console key input for responsive controls
        /// </summary>
        public string ProcessKeyInput(ConsoleKeyInfo keyInfo)
        {
            if (!GameActive)
                return "Game is not active.";

            var inputCommand = _inputHandler.ProcessKeyInput(keyInfo);
            
            if (_inMovementMode)
            {
                return ProcessMovementModeKey(inputCommand);
            }
            
            var actionCommand = ConvertKeyToGameAction(inputCommand);
            return ProcessGameAction(actionCommand);
        }
        
        /// <summary>
        /// Process game action command (unified handler for both input types)
        /// </summary>
        private string ProcessGameAction(GameActionCommand actionCommand)
        {
            if (_inMovementMode)
            {
                return ProcessMovementModeAction(actionCommand);
            }
            
            if (_waitingForDefenseChoice)
            {
                return ProcessDefenseChoice(actionCommand);
            }
            
            if (_waitingForEvasionMovement)
            {
                return ProcessEvasionMovement(actionCommand);
            }
            
            if (_waitingForMovementTarget)
            {
                return ProcessMovementTarget(actionCommand);
            }
            
            if (!CanPerformAnotherAction())
            {
                return "You have used all your actions this turn. Advancing to next turn...\n\n" + 
                       AdvanceToNextTurn();
            }
            
            return ExecuteGameAction(actionCommand);
        }
        
        /// <summary>
        /// Convert InputCommand to GameActionCommand for unified processing
        /// </summary>
        private GameActionCommand ConvertKeyToGameAction(InputCommand inputCommand)
        {
            return inputCommand.Type switch
            {
                InputType.Movement => new GameActionCommand
                {
                    Type = GameActionType.MovementStep,
                    Direction = inputCommand.Direction,
                    RawInput = inputCommand.RawInput
                },
                InputType.Confirm => new GameActionCommand
                {
                    Type = GameActionType.Confirm,
                    RawInput = inputCommand.RawInput
                },
                InputType.Cancel => new GameActionCommand
                {
                    Type = GameActionType.Cancel,
                    RawInput = inputCommand.RawInput
                },
                InputType.Reset => new GameActionCommand
                {
                    Type = GameActionType.Reset,
                    RawInput = inputCommand.RawInput
                },
                InputType.Help => new GameActionCommand
                {
                    Type = GameActionType.Help,
                    RawInput = inputCommand.RawInput
                },
                _ => new GameActionCommand
                {
                    Type = GameActionType.Invalid,
                    RawInput = inputCommand.RawInput
                }
            };
        }
        
        /// <summary>
        /// Process input during movement mode using InputHandler commands
        /// </summary>
        private string ProcessMovementModeAction(GameActionCommand actionCommand)
        {
            switch (actionCommand.Type)
            {
                case GameActionType.MovementStep:
                    var (deltaX, deltaY) = _inputHandler.GetDirectionDelta(actionCommand.Direction);
                    return ProcessMovementStep(deltaX, deltaY);
                    
                case GameActionType.Confirm:
                    return ConfirmMovementPath();
                    
                case GameActionType.Reset:
                    return ResetMovementPath();
                    
                case GameActionType.Cancel:
                    return CancelMovement();
                    
                case GameActionType.Help:
                    return GetMovementModeHelp();
                    
                default:
                    return GetMovementModeHelp();
            }
        }
        
        /// <summary>
        /// Process key input during movement mode for responsive WASD controls
        /// </summary>
        private string ProcessMovementModeKey(InputCommand inputCommand)
        {
            switch (inputCommand.Type)
            {
                case InputType.Movement:
                    var (deltaX, deltaY) = _inputHandler.GetDirectionDelta(inputCommand.Direction);
                    return ProcessMovementStep(deltaX, deltaY);
                    
                case InputType.Confirm:
                    return ConfirmMovementPath();
                    
                case InputType.Reset:
                    return ResetMovementPath();
                    
                case InputType.Cancel:
                    return CancelMovement();
                    
                case InputType.Help:
                    return GetMovementModeHelp();
                    
                default:
                    return GetMovementModeHelp();
            }
        }
        
        /// <summary>
        /// Process a single movement step in WASD mode
        /// </summary>
        private string ProcessMovementStep(int deltaX, int deltaY)
        {
            var character = CurrentActor;
            var currentPos = _movementPath.Count > 0 ? _movementPath.Last() : _movementStartPosition;
            var newPos = new Position(currentPos.X + deltaX, currentPos.Y + deltaY);
            
            if (_movementPointsUsed >= _activeMovementResult.MaxDistance)
            {
                return DrawMovementInterface("No movement points remaining! Use 'enter' to confirm or 'reset' to start over.");
            }
            
            if (!IsValidGridPosition(newPos))
            {
                return DrawMovementInterface("Cannot move outside the grid!");
            }
            
            if (_players.Any(p => p != character && p.IsAlive && p.Position.Equals(newPos)))
            {
                return DrawMovementInterface("Position occupied by another character!");
            }
            
            if (_movementPath.Contains(newPos))
            {
                return DrawMovementInterface("Cannot move to a position already in your path!");
            }
            
            _movementPath.Add(newPos);
            _movementPointsUsed++;
            
            return DrawMovementInterface($"Moved to ({newPos.X},{newPos.Y})");
        }
        
        /// <summary>
        /// Confirm the movement path and execute it
        /// </summary>
        private string ConfirmMovementPath()
        {
            if (_movementPath == null || _movementPath.Count == 0)
            {
                return DrawMovementInterface("No movement path set! Use WASD to move, or 'cancel' to exit movement mode.");
            }
            
            var finalPosition = _movementPath.Last();
            
            // Use the character from movement result, not current actor (fixes evasion bug)
            var character = _activeMovementResult?.Character ?? CurrentActor;
            
            if (character == null)
            {
                return "Error: No current actor found.";
            }
            
            if (_activeMovementResult == null)
            {
                return "Error: Movement result not found.";
            }
            
            // Execute the movement on the correct character
            character.Position = finalPosition;
            
            // Check if this is evasion movement (no stamina cost, ends turn)
            bool isEvasionMovement = _activeMovementResult.StaminaCost == 0;
            
            if (!isEvasionMovement)
            {
                character.UseStamina(_activeMovementResult.StaminaCost);
            }
            
            // Store movement result before exiting movement mode
            bool allowsSecondAction = _activeMovementResult.AllowsSecondAction && !isEvasionMovement;
            
            ExitMovementMode();
            
            var result = "";
            
            if (isEvasionMovement)
            {
                result = $"{character.Name} moves to ({finalPosition.X},{finalPosition.Y}) after successful evasion.\n\n";
                return _gridDisplay.CreateFullDisplay(result) + AdvanceToNextTurn();
            }
            else
            {
                result = $"{character.Name} moved to ({finalPosition.X},{finalPosition.Y}) using {_movementPointsUsed} movement points.\n";
                
                if (allowsSecondAction)
                {
                    bool turnContinues = UseAction(allowsSecondAction: true);
                    
                    if (turnContinues)
                    {
                        result += "Used 1 stamina, can still act once more!\n\n";
                        result += GetContinuedTurnInstructions();
                        return _gridDisplay.CreateFullDisplay(result);
                    }
                    else
                    {
                        result += "Used second action, turn ends.\n\n";
                        return _gridDisplay.CreateFullDisplay(result) + AdvanceToNextTurn();
                    }
                }
                else
                {
                    UseAction(allowsSecondAction: false);
                    result += "Used 1 stamina, turn ends.\n\n";
                    return _gridDisplay.CreateFullDisplay(result) + AdvanceToNextTurn();
                }
            }
        }
        
        /// <summary>
        /// Reset the current movement path
        /// </summary>
        private string ResetMovementPath()
        {
            _movementPath.Clear();
            _movementPointsUsed = 0;
            return DrawMovementInterface("Movement path reset!");
        }
        
        /// <summary>
        /// Cancel movement mode and return to normal actions
        /// </summary>
        private string CancelMovement()
        {
            ExitMovementMode();
            
            var character = CurrentActor;
            return _gridDisplay.CreateFullDisplay(
                $"{character.Name} cancels movement.\n\n" +
                $"Actions used: {_actionsUsedThisTurn}/{_maxActionsThisTurn}\n" +
                GetContinuedTurnInstructions()
            );
        }
        
        /// <summary>
        /// Exit movement mode and clean up state
        /// </summary>
        private void ExitMovementMode()
        {
            _inMovementMode = false;
            _movementPath.Clear();
            _movementPointsUsed = 0;
            _activeMovementResult = null;
            _movementStartPosition = null;
        }
        
        /// <summary>
        /// Draw the movement interface with path and remaining points - WITH LIVE GRID PREVIEW
        /// </summary>
        private string DrawMovementInterface(string message = "")
        {
            // Use the moving character from movement result, not current actor (fixes evasion bug)
            var character = _activeMovementResult?.Character ?? CurrentActor;
            if (character == null || _activeMovementResult == null)
            {
                return "Movement mode error - please restart movement.";
            }
            
            // Create a temporary grid that shows the movement path
            var sb = new System.Text.StringBuilder();
            
            sb.AppendLine("╔═══════════════════ RPG COMBAT GRID ═══════════════════╗");
            sb.AppendLine();
            
            // Draw the grid with live path preview
            sb.Append(DrawGridWithMovementPreview(character));
            
            sb.AppendLine();
            sb.AppendLine("=== COMBAT STATUS ===");
            
            // Show other players' status (excluding the moving character)
            foreach (var player in _players.Where(p => p != character).OrderBy(c => c.Name))
            {
                char symbol = char.ToUpper(player.Name[0]);
                string counter = player.Counter.IsReady ? " [COUNTER READY!]" : 
                               $" (Counter: {player.Counter.Current}/6)";
                               
                sb.AppendLine($"[{symbol}] {player.Name}: " +
                             $"HP {player.CurrentHealth}/{player.MaxHealth}, " +
                             $"SP {player.CurrentStamina}/{player.MaxStamina}" +
                             $"{counter}");
                             
                sb.AppendLine($"    Position: ({player.Position.X},{player.Position.Y}) " +
                             $"ATK:{player.AttackPoints} DEF:{player.DefensePoints} MOV:{player.MovementPoints}");
            }
            
            sb.AppendLine();
            sb.AppendLine("=== MOVEMENT MODE ===");
            sb.AppendLine($"🎮 {character.Name} - Movement Points: {_movementPointsUsed}/{_activeMovementResult.MaxDistance}");
            sb.AppendLine($"📍 Starting Position: {_movementStartPosition}");
            
            if (_movementPath != null && _movementPath.Count > 0)
            {
                var currentPos = _movementPath.Last();
                sb.AppendLine($"📍 Current Preview: ({currentPos.X},{currentPos.Y})");
                sb.AppendLine($"🛤️  Path: {_movementStartPosition} → {string.Join(" → ", _movementPath)}");
            }
            else
            {
                sb.AppendLine($"📍 Current Preview: {character.Position} (no movement yet)");
            }
            
            sb.AppendLine();
            sb.AppendLine("🎮 Controls:");
            sb.AppendLine("  [W] ↑ Up      [S] ↓ Down");
            sb.AppendLine("  [A] ← Left    [D] → Right");
            sb.AppendLine("  [Enter] Confirm path");
            sb.AppendLine("  [R] Reset path    [ESC] Cancel");
            
            if (!string.IsNullOrEmpty(message))
            {
                sb.AppendLine();
                sb.AppendLine($">>> {message}");
            }
            
            sb.AppendLine("╚══════════════════════════════════════════════════════╝");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Draw grid with live movement path preview
        /// </summary>
        private string DrawGridWithMovementPreview(Character movingCharacter)
        {
            var sb = new System.Text.StringBuilder();
            
            // Draw grid with movement preview
            for (int y = 7; y >= 0; y--) // Top to bottom for proper display (8x8 grid)
            {
                // Draw row with character positions and movement preview
                for (int x = 0; x < 8; x++)
                {
                    char displayChar = GetCharacterAtPositionWithPreview(x, y, movingCharacter);
                    sb.Append(displayChar);
                    
                    // Add spaces between grid points (except last column)
                    if (x < 7)
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
        /// Get character display with movement path preview
        /// </summary>
        private char GetCharacterAtPositionWithPreview(int x, int y, Character movingCharacter)
        {
            var pos = new Position(x, y);
            
            // Check if this is where the moving character would end up
            if (_movementPath != null && _movementPath.Count > 0)
            {
                var finalPos = _movementPath.Last();
                if (finalPos.X == x && finalPos.Y == y)
                {
                    return char.ToUpper(movingCharacter.Name[0]); // Show character at new position
                }
                
                // Show path breadcrumbs for intermediate positions (except final)
                var pathPositions = _movementPath.Take(_movementPath.Count - 1); // All except last
                if (pathPositions.Any(p => p.X == x && p.Y == y))
                {
                    return '·'; // Show path trail
                }
            }
            else
            {
                // Show character at current position when no movement made yet
                if (movingCharacter.Position.X == x && movingCharacter.Position.Y == y)
                {
                    return char.ToUpper(movingCharacter.Name[0]);
                }
            }
            
            // Check for other characters (not the moving one)
            var otherCharacter = _players.FirstOrDefault(c => 
                c != movingCharacter && c.IsAlive && c.Position.X == x && c.Position.Y == y);
            
            if (otherCharacter != null)
            {
                return char.ToUpper(otherCharacter.Name[0]);
            }
            
            // Show starting position of moving character if they've moved
            if (_movementPath != null && _movementPath.Count > 0 && 
                movingCharacter.Position.X == x && movingCharacter.Position.Y == y)
            {
                return '○'; // Show starting position as empty circle
            }
            
            return '.'; // Empty position
        }
        
        /// <summary>
        /// Get help text for movement mode
        /// </summary>
        private string GetMovementModeHelp()
        {
            return DrawMovementInterface("Use WASD or arrow keys to move. Type 'enter' to confirm, 'reset' to clear path, or 'esc' to cancel.");
        }
        
        /// <summary>
        /// Set up WASD movement mode for evasion movement after successful dodge
        /// </summary>
        private void SetupEvasionMovementMode(Character character, int maxDistance)
        {
            // Create a fake movement result for evasion movement
            _activeMovementResult = new MovementResult
            {
                Character = character,
                MovementType = MovementType.Simple, // Evasion acts like simple movement
                MaxDistance = maxDistance,
                StaminaCost = 0, // No additional stamina cost (already paid for evasion)
                AllowsSecondAction = false, // Evasion movement ends the defense phase
                ValidPositions = GetPositionsWithinDistance(character.Position, maxDistance, character)
            };
            
            _movementStartPosition = new Position(character.Position.X, character.Position.Y);
            _movementPath.Clear();
            _movementPointsUsed = 0;
            _inMovementMode = true;
        }
        
        /// <summary>
        /// Get all valid positions within evasion distance (helper for evasion movement)
        /// </summary>
        private List<Position> GetPositionsWithinDistance(Position start, int maxDistance, Character movingCharacter)
        {
            var positions = new List<Position>();
            
            // Check all positions within Manhattan distance
            for (int x = start.X - maxDistance; x <= start.X + maxDistance; x++)
            {
                for (int y = start.Y - maxDistance; y <= start.Y + maxDistance; y++)
                {
                    if (x == start.X && y == start.Y) continue; // Skip current position
                    
                    var newPos = new Position(x, y);
                    var distance = Math.Abs(start.X - newPos.X) + Math.Abs(start.Y - newPos.Y);
                    
                    if (distance <= maxDistance && IsValidGridPosition(newPos))
                    {
                        // Check if position is not occupied by another character
                        if (!_players.Any(p => p != movingCharacter && p.IsAlive && p.Position.Equals(newPos)))
                        {
                            positions.Add(newPos);
                        }
                    }
                }
            }
            
            return positions;
        }
        
        /// <summary>
        /// Check if position is within grid bounds
        /// </summary>
        private bool IsValidGridPosition(Position pos)
        {
            return pos.X >= 0 && pos.X < 8 && pos.Y >= 0 && pos.Y < 8; // 8x8 grid
        }
        
        /// <summary>
        /// Execute a game action command
        /// </summary>
        private string ExecuteGameAction(GameActionCommand actionCommand)
        {
            var result = "";
            
            switch (actionCommand.Type)
            {
                case GameActionType.Attack:
                    var target = actionCommand.Target != null ? FindCharacterByLetter(actionCommand.Target[0]) : null;
                    result = HandleAttackAction(target);
                    break;
                    
                case GameActionType.EnterMovementMode:
                    result = HandleMoveAction();
                    break;
                    
                case GameActionType.MoveToPosition:
                    result = HandleMoveAction(actionCommand.TargetPosition);
                    break;
                    
                case GameActionType.EnterDashMode:
                    result = HandleDashAction();
                    break;
                    
                case GameActionType.DashToPosition:
                    result = HandleDashAction(actionCommand.TargetPosition);
                    break;
                    
                case GameActionType.Rest:
                    result = HandleRestAction();
                    break;
                    
                case GameActionType.Defend:
                    result = HandleDefendAction();
                    break;
                    
                case GameActionType.Help:
                    result = GetHelpText();
                    break;
                    
                case GameActionType.Quit:
                    result = "Game ended by player.";
                    break;
                    
                default:
                    result = GetHelpText();
                    break;
            }
            
            _gridDisplay.UpdateCharacters(_players);
            return result;
        }
        
        /// <summary>
        /// Enhanced movement action - supports both WASD mode and direct coordinates
        /// </summary>
        private string HandleMoveAction(Position targetPosition = null)
        {
            var character = CurrentActor;
            
            if (targetPosition != null)
            {
                return HandleDirectMovement(targetPosition, MovementType.Simple);
            }
            else
            {
                return EnterMovementMode(MovementType.Simple);
            }
        }
        
        /// <summary>
        /// Enhanced dash action - supports both WASD mode and direct coordinates
        /// </summary>
        private string HandleDashAction(Position targetPosition = null)
        {
            var character = CurrentActor;
            
            if (targetPosition != null)
            {
                return HandleDirectMovement(targetPosition, MovementType.Dash);
            }
            else
            {
                return EnterMovementMode(MovementType.Dash);
            }
        }
        
        /// <summary>
        /// Enter movement mode for WASD control
        /// </summary>
        private string EnterMovementMode(MovementType moveType)
        {
            var character = CurrentActor;
            
            _activeMovementResult = _movementSystem.CalculateMovement(character, moveType);
            _movementStartPosition = new Position(character.Position.X, character.Position.Y);
            _movementPath.Clear();
            _movementPointsUsed = 0;
            _inMovementMode = true;
            
            string moveTypeStr = moveType == MovementType.Simple ? "Move" : "Dash";
            string actionInfo = moveType == MovementType.Simple ? " (allows second action)" : " (ends turn)";
            
            var result = $"{character.Name} {moveTypeStr}: {_activeMovementResult.MoveRoll} + {character.MovementPoints} MOV = {_activeMovementResult.MaxDistance} movement points{actionInfo}\n\n";
            
            return result + DrawMovementInterface("Use WASD to move step by step!");
        }
        
        /// <summary>
        /// Legacy direct movement support
        /// </summary>
        private string HandleDirectMovement(Position targetPosition, MovementType moveType)
        {
            var character = CurrentActor;
            var moveResult = _movementSystem.CalculateMovement(character, moveType);
            
            string moveTypeStr = moveType == MovementType.Simple ? "moves" : "dashes";
            var result = $"{character.Name} {moveTypeStr}: {moveResult.MoveRoll} + {character.MovementPoints} MOV = {moveResult.MaxDistance} movement points\n\n";
            
            if (_movementSystem.ExecuteMovement(moveResult, targetPosition))
            {
                result += $"{character.Name} {moveTypeStr} to ({targetPosition.X},{targetPosition.Y})\n";
                
                if (moveType == MovementType.Simple)
                {
                    bool turnContinues = UseAction(allowsSecondAction: true);
                    
                    if (turnContinues)
                    {
                        result += "Used 1 stamina, can still act once more!\n\n";
                        result += GetContinuedTurnInstructions();
                        return _gridDisplay.CreateFullDisplay(result);
                    }
                    else
                    {
                        result += "Used second action, turn ends.\n\n";
                        return _gridDisplay.CreateFullDisplay(result) + AdvanceToNextTurn();
                    }
                }
                else
                {
                    UseAction(allowsSecondAction: false);
                    result += "Used 1 stamina, turn ends.\n\n";
                    return _gridDisplay.CreateFullDisplay(result) + AdvanceToNextTurn();
                }
            }
            else
            {
                return _gridDisplay.CreateFullDisplay(
                    result + "Invalid position! Use WASD mode by typing 'move' or 'dash' without coordinates."
                );
            }
        }
        
        /// <summary>
        /// Get instructions for continued turns (second action)
        /// </summary>
        private string GetContinuedTurnInstructions()
        {
            return $"{CurrentActor.Name}'s turn continues...\n" +
                   $"Actions used: {_actionsUsedThisTurn}/{_maxActionsThisTurn}\n" +
                   "Available actions:\n" +
                   "  'attack [letter]' - Attack adjacent character\n" +
                   "  'move' - Move again (1d6) using WASD\n" +
                   "  'dash' - Dash move (2d6) using WASD, ends turn\n" +
                   "  'rest' - Recover stamina\n" +
                   "  'defend' - Take defensive stance";
        }
        
        /// <summary>
        /// Advance to the next turn and reset action tracking
        /// </summary>
        private string AdvanceToNextTurn()
        {
            if (_inMovementMode)
            {
                ExitMovementMode();
            }
            
            var nextTurn = _turnManager.NextTurn();
            ResetActionTracking();
            _gridDisplay.UpdateCharacters(_players);
            
            return _gridDisplay.CreateFullDisplay(
                $"{nextTurn.Message}\n" +
                GetTurnInstructions(nextTurn)
            );
        }
        
        /// <summary>
        /// Process defender's choice using InputHandler
        /// </summary>
        private string ProcessDefenseChoice(GameActionCommand actionCommand)
        {
            RPGGame.Combat.DefenseChoice? choice = null;
            
            switch (actionCommand.Type)
            {
                case GameActionType.Defend:
                    choice = RPGGame.Combat.DefenseChoice.Defend;
                    break;
                case GameActionType.EnterMovementMode:
                    choice = RPGGame.Combat.DefenseChoice.Move;
                    break;
                case GameActionType.DefenseTakeDamage:
                    choice = RPGGame.Combat.DefenseChoice.TakeDamage;
                    break;
                default:
                    if (_inputHandler.IsDefenseChoice(actionCommand.RawInput))
                    {
                        choice = _inputHandler.ParseDefenseChoice(actionCommand.RawInput);
                    }
                    break;
            }
            
            if (choice == null)
            {
                return "Invalid defense choice. Use: 'defend', 'move', or 'take'";
            }
            
            var defenseResult = _combatSystem.ResolveDefense(_defendingCharacter, _pendingAttack, choice.Value);
            
            var resultMessage = $"{_pendingAttack.Message}\n{defenseResult.Message}\n";
            
            UseAction(allowsSecondAction: false);
            
            if (defenseResult.DefenseChoice == RPGGame.Combat.DefenseChoice.Move && defenseResult.CanMove)
            {
                // Enter WASD evasion movement mode instead of coordinate input
                _waitingForEvasionMovement = false; // Don't use old coordinate system
                _pendingEvasionResult = null;
                _evadingCharacter = null;
                
                // Set up WASD evasion movement mode
                SetupEvasionMovementMode(_defendingCharacter, defenseResult.MovementDistance);
                
                resultMessage += $"\n{_defendingCharacter.Name} successfully evaded and can now move up to {defenseResult.MovementDistance} spaces!\n";
                resultMessage += "🎮 Entering WASD evasion movement mode...\n\n";
                
                _waitingForDefenseChoice = false;
                _pendingAttack = null;
                _defendingCharacter = null;
                
                return resultMessage + DrawMovementInterface("Use WASD to move after successful evasion!");
            }
            
            if (defenseResult.CounterReady)
            {
                resultMessage += $"\n⚡ {_defendingCharacter.Name}'s counter gauge is READY! " +
                               "They can use a counter attack on their turn!\n";
            }
            
            _waitingForDefenseChoice = false;
            _pendingAttack = null;
            _defendingCharacter = null;
            
            if (!_turnManager.CurrentActor.IsAlive || _players.Count(p => p.IsAlive) <= 1)
            {
                var winner = _players.FirstOrDefault(p => p.IsAlive);
                return _gridDisplay.CreateFullDisplay(
                    resultMessage + $"\n🎉 GAME OVER! {winner?.Name} wins!"
                );
            }
            
            return _gridDisplay.CreateFullDisplay(resultMessage + "\n") + AdvanceToNextTurn();
        }
        
        /// <summary>
        /// Handle movement after successful evasion using InputHandler
        /// </summary>
        private string ProcessEvasionMovement(GameActionCommand actionCommand)
        {
            if (actionCommand.Type == GameActionType.Coordinates)
            {
                var targetPosition = actionCommand.TargetPosition;
                
                var validPositions = _gridDisplay.GetValidMovePositions(_evadingCharacter);
                var filteredPositions = validPositions.Where(pos => {
                    int distance = Math.Abs(_evadingCharacter.Position.X - pos.X) + Math.Abs(_evadingCharacter.Position.Y - pos.Y);
                    return distance <= _pendingEvasionResult.MovementDistance;
                }).ToList();
                
                if (filteredPositions.Any(p => p.Equals(targetPosition)))
                {
                    var characterName = _evadingCharacter.Name;
                    _evadingCharacter.Position = targetPosition;
                    
                    _waitingForEvasionMovement = false;
                    _pendingEvasionResult = null;
                    _evadingCharacter = null;
                    
                    return _gridDisplay.CreateFullDisplay(
                        $"{characterName} moves to ({targetPosition.X},{targetPosition.Y}) after evasion.\n\n"
                    ) + AdvanceToNextTurn();
                }
                else
                {
                    return "Invalid position! Choose from the available evasion positions.";
                }
            }
            else if (actionCommand.Type == GameActionType.Skip)
            {
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
        /// Handle movement target selection using InputHandler
        /// </summary>
        private string ProcessMovementTarget(GameActionCommand actionCommand)
        {
            if (actionCommand.Type == GameActionType.Coordinates)
            {
                var targetPosition = actionCommand.TargetPosition;
                
                if (_movementSystem.ExecuteMovement(_pendingMovement, targetPosition))
                {
                    var result = $"{_pendingMovement.Character.Name} moved to ({targetPosition.X},{targetPosition.Y})\n";
                    
                    _waitingForMovementTarget = false;
                    
                    if (_pendingMovement.AllowsSecondAction)
                    {
                        bool turnContinues = UseAction(allowsSecondAction: true);
                        
                        if (turnContinues)
                        {
                            result += "Used 1 stamina, can still act once more!\n\n";
                            result += GetContinuedTurnInstructions();
                            
                            _pendingMovement = null;
                            return _gridDisplay.CreateFullDisplay(result);
                        }
                        else
                        {
                            result += "Turn ends.\n\n";
                            _pendingMovement = null;
                            return _gridDisplay.CreateFullDisplay(result) + AdvanceToNextTurn();
                        }
                    }
                    else
                    {
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
                return "Please enter coordinates as 'x y' (e.g., '5 7'), or use the new WASD movement mode by typing just 'move' or 'dash'!";
            }
        }
        
        /// <summary>
        /// Handle attack action with range checking
        /// </summary>
        private string HandleAttackAction(Character target)
        {
            var attacker = CurrentActor;
            
            if (target == null)
            {
                var validTargets = _gridDisplay.GetCharactersInAttackRange(attacker);
                if (!validTargets.Any())
                {
                    return _gridDisplay.CreateFullDisplay(
                        "No valid targets in range!\n" +
                        "Move closer to attack. Use 'move' (WASD mode) or 'dash' to reposition."
                    );
                }
                
                return _gridDisplay.CreateFullDisplay(
                    "Choose a target to attack:\n" +
                    _gridDisplay.DrawAttackRange(attacker) +
                    "Use 'attack [character letter]' (e.g., 'attack B')"
                );
            }
            
            if (!_gridDisplay.IsInAttackRange(attacker.Position, target.Position))
            {
                return _gridDisplay.CreateFullDisplay(
                    $"{target.Name} is not in attack range! You must be adjacent (including diagonal).\n" +
                    _gridDisplay.DrawAttackRange(attacker)
                );
            }
            
            var attackResult = _combatSystem.ExecuteAttack(attacker, target);
            
            if (!attackResult.Success)
            {
                return _gridDisplay.CreateFullDisplay(attackResult.Message);
            }
            
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
        /// Get movement options display (legacy)
        /// </summary>
        private string GetMovementOptions(MovementResult moveResult)
        {
            var options = $"Available positions within {moveResult.MaxDistance} movement:\n";
            
            var sortedPositions = moveResult.ValidPositions
                .OrderBy(p => p.X)
                .ThenBy(p => p.Y)
                .Take(20);
            
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
            
            // Check if turn should continue after rest
            bool turnContinues = UseAction(allowsSecondAction: false);
            
            var result = $"{character.Name} rests and recovers {staminaRestored} stamina.\n";
            
            if (turnContinues)
            {
                // This shouldn't normally happen since rest doesn't allow second action
                result += "Turn continues...\n\n";
                result += GetContinuedTurnInstructions();
                return _gridDisplay.CreateFullDisplay(result);
            }
            else
            {
                // Turn ends after rest
                result += "\n";
                return _gridDisplay.CreateFullDisplay(result) + AdvanceToNextTurn();
            }
        }
        
        /// <summary>
        /// Handle defend action (defensive stance)
        /// </summary>
        private string HandleDefendAction()
        {
            var character = CurrentActor;
            
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
                new Position(2, 2),   
                new Position(5, 5), 
                new Position(2, 13),  
                new Position(13, 2)   
            };
            
            for (int i = 0; i < _players.Count && i < positions.Count; i++)
            {
                _players[i].Position = positions[i];
            }
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
                instructions += "  'move' - Enter WASD movement mode (allows second action)\n";
            if (turn.AvailableActions.Contains(ActionChoice.Move))
                instructions += "  'dash' - Enter WASD dash mode (ends turn)\n";
            if (turn.AvailableActions.Contains(ActionChoice.Rest))
                instructions += "  'rest' - Recover stamina\n";
            if (turn.AvailableActions.Contains(ActionChoice.Defend))
                instructions += "  'defend' - Take defensive stance\n";
            
            instructions += "\nMovement: Use WASD keys for intuitive step-by-step movement!";
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
                   "  move - Enter WASD movement mode (responsive controls)\n" +
                   "  dash - Enter WASD dash mode (responsive controls, ends turn)\n" +
                   "  move/dash x y - Legacy: Move to specific coordinates\n" +
                   "  rest - Recover stamina\n" +
                   "  defend - Take defensive stance\n\n" +
                   "WASD Movement (Responsive - No Enter Needed):\n" +
                   "  W/↑ - Move up     S/↓ - Move down\n" +
                   "  A/← - Move left   D/→ - Move right\n" +
                   "  Enter/Space - Confirm path   R - Reset path   ESC - Cancel\n\n" +
                   "Examples: 'attack B', 'move', 'dash'\n" +
                   "Characters are shown by their first letter on the grid.\n\n" +
                   "Input Modes:\n" +
                   "  • Text Commands: Type and press Enter (e.g., 'attack B')\n" +
                   "  • WASD Keys: Direct key presses in movement mode\n\n" +
                   "Action Economy: Most actions end your turn immediately.\n" +
                   "Only 'move' allows one additional action before ending turn.";
        }
    }
}