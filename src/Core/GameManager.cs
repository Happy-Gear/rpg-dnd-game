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
    /// Game manager with WASD movement and viewport-centered grid display.
    /// All grid bounds come from GameConfig (balance.json) — no hardcoded sizes.
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

        // Movement state
        private MovementResult _pendingMovement;
        private bool _waitingForMovementTarget;

        // Post-evasion movement state
        private bool _waitingForEvasionMovement;
        private DefenseResult _pendingEvasionResult;
        private Character _evadingCharacter;

        // WASD movement mode state
        private bool _inMovementMode;
        private List<Position> _movementPath;
        private Position _movementStartPosition;
        private int _movementPointsUsed;
        private MovementResult _activeMovementResult;

        // Action economy
        private int _actionsUsedThisTurn;
        private int _maxActionsThisTurn;

        // Convenience: arena size from config (avoids repeated lookups)
        private int ArenaWidth  => GameConfig.Current.Grid.Width;
        private int ArenaHeight => GameConfig.Current.Grid.Height;

        public bool GameActive => _turnManager.GameActive;
        public Character CurrentActor => _turnManager.CurrentActor;
        public bool InMovementMode => _inMovementMode;
        public int MovementPointsRemaining => (_activeMovementResult?.MaxDistance ?? 0) - _movementPointsUsed;
        public List<Position> CurrentPath => new List<Position>(_movementPath ?? new List<Position>());

        public GameManager()
        {
            _turnManager   = new TurnManager();
            _combatSystem  = new CombatSystem();
            _gridDisplay   = new GridDisplay(); // reads config internally
            _players       = new List<Character>();
            _movementPath  = new List<Position>();
            _movementSystem = new MovementSystem(_combatSystem.DiceRoller);
            _inputHandler  = new InputHandler();
            ResetActionTracking();
        }

        /// <summary>
        /// Legacy constructor — grid size now comes from config; params are ignored.
        /// Kept so existing call sites compile without changes.
        /// </summary>
        public GameManager(int ignoredWidth, int ignoredHeight) : this() { }

        // ─────────────────────────────────────────────────────────────────────
        // Action economy
        // ─────────────────────────────────────────────────────────────────────

        private void ResetActionTracking()
        {
            _actionsUsedThisTurn = 0;
            _maxActionsThisTurn  = 1;
        }

        private bool CanPerformAnotherAction() => _actionsUsedThisTurn < _maxActionsThisTurn;

        private bool UseAction(bool allowsSecondAction = false)
        {
            _actionsUsedThisTurn++;
            if (_actionsUsedThisTurn == 1 && allowsSecondAction)
                _maxActionsThisTurn = 2;
            return CanPerformAnotherAction();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Game startup
        // ─────────────────────────────────────────────────────────────────────

        public string StartGame(params Character[] players)
        {
            _players = players.ToList();
            PlacePlayersOnGrid();
            _turnManager.StartCombat(_players.ToArray());
            _gridDisplay.UpdateCharacters(_players);

            var firstTurn = _turnManager.NextTurn();
            ResetActionTracking();

            return CreateDisplay(
                firstTurn.CurrentActor,
                $"Game started! {firstTurn.Message}\n" + GetTurnInstructions(firstTurn)
            );
        }

        // ─────────────────────────────────────────────────────────────────────
        // Input entry points
        // ─────────────────────────────────────────────────────────────────────

        public string ProcessAction(string input)
        {
            if (!GameActive) return "Game is not active.";
            var cmd = _inputHandler.ProcessActionInput(input);
            return ProcessGameAction(cmd);
        }

        public string ProcessKeyInput(ConsoleKeyInfo keyInfo)
        {
            if (!GameActive) return "Game is not active.";
            var inputCmd = _inputHandler.ProcessKeyInput(keyInfo);
            if (_inMovementMode)
                return ProcessMovementModeKey(inputCmd);
            return ProcessGameAction(ConvertKeyToGameAction(inputCmd));
        }

        // ─────────────────────────────────────────────────────────────────────
        // Unified action router
        // ─────────────────────────────────────────────────────────────────────

        private string ProcessGameAction(GameActionCommand cmd)
        {
            if (_inMovementMode)         return ProcessMovementModeAction(cmd);
            if (_waitingForDefenseChoice) return ProcessDefenseChoice(cmd);
            if (_waitingForEvasionMovement) return ProcessEvasionMovement(cmd);
            if (_waitingForMovementTarget)  return ProcessMovementTarget(cmd);

            if (!CanPerformAnotherAction())
                return "You have used all your actions this turn. Advancing...\n\n" + AdvanceToNextTurn();

            return ExecuteGameAction(cmd);
        }

        private GameActionCommand ConvertKeyToGameAction(InputCommand inputCmd)
        {
            return inputCmd.Type switch
            {
                InputType.Movement => new GameActionCommand { Type = GameActionType.MovementStep, Direction = inputCmd.Direction, RawInput = inputCmd.RawInput },
                InputType.Confirm  => new GameActionCommand { Type = GameActionType.Confirm,      RawInput = inputCmd.RawInput },
                InputType.Cancel   => new GameActionCommand { Type = GameActionType.Cancel,       RawInput = inputCmd.RawInput },
                InputType.Reset    => new GameActionCommand { Type = GameActionType.Reset,        RawInput = inputCmd.RawInput },
                InputType.Help     => new GameActionCommand { Type = GameActionType.Help,         RawInput = inputCmd.RawInput },
                _                  => new GameActionCommand { Type = GameActionType.Invalid,      RawInput = inputCmd.RawInput }
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // Movement mode (WASD)
        // ─────────────────────────────────────────────────────────────────────

        private string ProcessMovementModeAction(GameActionCommand cmd)
        {
            switch (cmd.Type)
            {
                case GameActionType.MovementStep:
                    var (dx, dy) = _inputHandler.GetDirectionDelta(cmd.Direction);
                    return ProcessMovementStep(dx, dy);
                case GameActionType.Confirm: return ConfirmMovementPath();
                case GameActionType.Reset:   return ResetMovementPath();
                case GameActionType.Cancel:  return CancelMovement();
                default:                     return GetMovementModeHelp();
            }
        }

        private string ProcessMovementModeKey(InputCommand inputCmd)
        {
            switch (inputCmd.Type)
            {
                case InputType.Movement:
                    var (dx, dy) = _inputHandler.GetDirectionDelta(inputCmd.Direction);
                    return ProcessMovementStep(dx, dy);
                case InputType.Confirm: return ConfirmMovementPath();
                case InputType.Reset:   return ResetMovementPath();
                case InputType.Cancel:  return CancelMovement();
                default:                return GetMovementModeHelp();
            }
        }

        private string ProcessMovementStep(int deltaX, int deltaY)
        {
            var character = _activeMovementResult?.Character ?? CurrentActor;
            var currentPos = _movementPath.Count > 0 ? _movementPath.Last() : _movementStartPosition;
            var newPos = new Position(currentPos.X + deltaX, currentPos.Y + deltaY);

            if (_movementPointsUsed >= _activeMovementResult.MaxDistance)
                return DrawMovementInterface("No movement points remaining! Press Enter to confirm or R to reset.");

            if (!IsInArenaBounds(newPos))
                return DrawMovementInterface("Cannot move outside the arena!");

            if (_players.Any(p => p != character && p.IsAlive && p.Position.Equals(newPos)))
                return DrawMovementInterface("Position occupied by another character!");

            if (_movementPath.Contains(newPos))
                return DrawMovementInterface("Cannot revisit a position already in your path!");

            _movementPath.Add(newPos);
            _movementPointsUsed++;

            return DrawMovementInterface($"Moved to ({newPos.X},{newPos.Y})");
        }

        /// <summary>
        /// BUG FIX: Capture _movementPointsUsed BEFORE ExitMovementMode() clears it to 0.
        /// </summary>
        private string ConfirmMovementPath()
        {
            if (_movementPath == null || _movementPath.Count == 0)
                return DrawMovementInterface("No path set! Use WASD to move, or ESC to cancel.");

            var character = _activeMovementResult?.Character ?? CurrentActor;
            if (character == null)     return "Error: No current actor.";
            if (_activeMovementResult == null) return "Error: Movement result missing.";

            var finalPosition = _movementPath.Last();

            // BUG FIX: capture BEFORE ExitMovementMode() clears _movementPointsUsed
            int pointsUsed = _movementPointsUsed;
            bool isEvasion = _activeMovementResult.StaminaCost == 0;
            bool allowsSecond = _activeMovementResult.AllowsSecondAction && !isEvasion;

            character.Position = finalPosition;
            if (!isEvasion) character.UseStamina(_activeMovementResult.StaminaCost);

            ExitMovementMode(); // clears _movementPointsUsed — must be AFTER capture above

            _gridDisplay.UpdateCharacters(_players);

            if (isEvasion)
            {
                return CreateDisplay(character,
                    $"{character.Name} moves to ({finalPosition.X},{finalPosition.Y}) after evasion.\n")
                    + AdvanceToNextTurn();
            }

            var result = $"{character.Name} moved to ({finalPosition.X},{finalPosition.Y}) " +
                         $"using {pointsUsed} movement points.\n";

            if (allowsSecond)
            {
                bool continues = UseAction(allowsSecondAction: true);
                if (continues)
                {
                    result += "Used 1 stamina, can still act once more!\n\n" + GetContinuedTurnInstructions();
                    return CreateDisplay(character, result);
                }
                result += "Used second action, turn ends.\n";
                return CreateDisplay(character, result) + AdvanceToNextTurn();
            }

            UseAction(allowsSecondAction: false);
            result += "Used 1 stamina, turn ends.\n";
            return CreateDisplay(character, result) + AdvanceToNextTurn();
        }

        private string ResetMovementPath()
        {
            _movementPath.Clear();
            _movementPointsUsed = 0;
            return DrawMovementInterface("Path reset!");
        }

        private string CancelMovement()
        {
            var character = CurrentActor;
            ExitMovementMode();
            return CreateDisplay(character,
                $"{character.Name} cancels movement.\n" +
                $"Actions used: {_actionsUsedThisTurn}/{_maxActionsThisTurn}\n\n" +
                GetContinuedTurnInstructions());
        }

        private void ExitMovementMode()
        {
            _inMovementMode      = false;
            _movementPath.Clear();
            _movementPointsUsed  = 0;
            _activeMovementResult = null;
            _movementStartPosition = null;
        }

        private string EnterMovementMode(MovementType moveType)
        {
            var character = CurrentActor;
            _activeMovementResult  = _movementSystem.CalculateMovement(character, moveType);
            _movementStartPosition = new Position(character.Position.X, character.Position.Y);
            _movementPath.Clear();
            _movementPointsUsed = 0;
            _inMovementMode = true;

            string typeStr  = moveType == MovementType.Simple ? "Move" : "Dash";
            string actionInfo = moveType == MovementType.Simple ? "(allows second action)" : "(ends turn)";
            string header = $"{character.Name} {typeStr}: {_activeMovementResult.MoveRoll} + " +
                            $"{character.MovementPoints} MOV = {_activeMovementResult.MaxDistance} points {actionInfo}\n\n";

            return header + DrawMovementInterface("Use WASD to move step by step!");
        }

        private void SetupEvasionMovementMode(Character character, int maxDistance)
        {
            _activeMovementResult = new MovementResult
            {
                Character        = character,
                MovementType     = MovementType.Simple,
                MaxDistance      = maxDistance,
                StaminaCost      = 0,
                AllowsSecondAction = false,
                ValidPositions   = new List<Position>()
            };
            _movementStartPosition = new Position(character.Position.X, character.Position.Y);
            _movementPath.Clear();
            _movementPointsUsed = 0;
            _inMovementMode = true;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Movement interface display (delegates to GridDisplay viewport)
        // ─────────────────────────────────────────────────────────────────────

        private string DrawMovementInterface(string message = "")
        {
            var character = _activeMovementResult?.Character ?? CurrentActor;
            if (character == null || _activeMovementResult == null)
                return "Movement mode error — please restart movement.";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("╔═══════════════════ RPG COMBAT GRID ═══════════════════╗");
            sb.AppendLine();

            // Use the viewport-aware drawing from GridDisplay
            sb.Append(_gridDisplay.DrawViewportWithMovementPreview(
                character, _movementStartPosition, _movementPath));

            sb.AppendLine();
            sb.AppendLine("=== COMBAT STATUS ===");
            foreach (var p in _players.Where(p => p != character).OrderBy(c => c.Name))
            {
                char sym = char.ToUpper(p.Name[0]);
                string counter = p.Counter.IsReady ? " [COUNTER READY!]" : $" (Counter: {p.Counter.Current}/{p.Counter.Maximum})";
                sb.AppendLine($"[{sym}] {p.Name}: HP {p.CurrentHealth}/{p.MaxHealth}, SP {p.CurrentStamina}/{p.MaxStamina}{counter}");
                sb.AppendLine($"    Position: ({p.Position.X},{p.Position.Y}) ATK:{p.AttackPoints} DEF:{p.DefensePoints} MOV:{p.MovementPoints}");
            }

            sb.AppendLine();
            sb.AppendLine("=== MOVEMENT MODE ===");
            sb.AppendLine($"🎮 {character.Name} — Points used: {_movementPointsUsed}/{_activeMovementResult.MaxDistance}");
            sb.AppendLine($"📍 Start: {_movementStartPosition}");

            if (_movementPath != null && _movementPath.Count > 0)
            {
                sb.AppendLine($"📍 Preview: {_movementPath.Last()}");
                sb.AppendLine($"🛤️  Path: {_movementStartPosition} → {string.Join(" → ", _movementPath)}");
            }
            else
            {
                sb.AppendLine($"📍 Preview: {character.Position} (no movement yet)");
            }

            sb.AppendLine();
            sb.AppendLine("🎮 Controls: [W/↑] Up  [S/↓] Down  [A/←] Left  [D/→] Right");
            sb.AppendLine("             [Enter] Confirm  [R] Reset  [ESC] Cancel");

            if (!string.IsNullOrEmpty(message))
                sb.AppendLine($"\n>>> {message}");

            sb.AppendLine("╚══════════════════════════════════════════════════════╝");
            return sb.ToString();
        }

        private string GetMovementModeHelp()
            => DrawMovementInterface("WASD / arrow keys to move. Enter to confirm, R to reset, ESC to cancel.");

        // ─────────────────────────────────────────────────────────────────────
        // Turn advancement
        // ─────────────────────────────────────────────────────────────────────

        private string AdvanceToNextTurn()
        {
            if (_inMovementMode) ExitMovementMode();

            // Safety: ensure no character is stuck at 0 stamina before NextTurn loop
            foreach (var p in _players.Where(p => p.IsAlive && p.CurrentStamina == 0))
                p.RestoreStamina(GameConfig.Current.Turns.ForcedRestRestore);

            var nextTurn = _turnManager.NextTurn();
            ResetActionTracking();
            _gridDisplay.UpdateCharacters(_players);

            return CreateDisplay(
                nextTurn.CurrentActor,
                $"{nextTurn.Message}\n" + GetTurnInstructions(nextTurn)
            );
        }

        // ─────────────────────────────────────────────────────────────────────
        // Defense choice
        // ─────────────────────────────────────────────────────────────────────

        private string ProcessDefenseChoice(GameActionCommand cmd)
        {
            RPGGame.Combat.DefenseChoice? choice = cmd.Type switch
            {
                GameActionType.Defend           => RPGGame.Combat.DefenseChoice.Defend,
                GameActionType.EnterMovementMode => RPGGame.Combat.DefenseChoice.Move,
                GameActionType.DefenseTakeDamage => RPGGame.Combat.DefenseChoice.TakeDamage,
                _ => _inputHandler.IsDefenseChoice(cmd.RawInput)
                         ? _inputHandler.ParseDefenseChoice(cmd.RawInput)
                         : null
            };

            if (choice == null)
                return "Invalid defense choice. Use: 'defend', 'move', or 'take'";

            var defenseResult = _combatSystem.ResolveDefense(_defendingCharacter, _pendingAttack, choice.Value);
            var msg = $"{_pendingAttack.Message}\n{defenseResult.Message}\n";

            UseAction(allowsSecondAction: false);

            if (defenseResult.DefenseChoice == RPGGame.Combat.DefenseChoice.Move && defenseResult.CanMove)
            {
                SetupEvasionMovementMode(_defendingCharacter, defenseResult.MovementDistance);
                msg += $"\n{_defendingCharacter.Name} evaded! Can move up to {defenseResult.MovementDistance} tiles.\n" +
                       "🎮 Entering WASD evasion movement mode...\n\n";

                _waitingForDefenseChoice = false;
                _pendingAttack           = null;
                _defendingCharacter      = null;
                return msg + DrawMovementInterface("Use WASD to reposition after evasion!");
            }

            if (defenseResult.CounterReady)
                msg += $"\n⚡ {_defendingCharacter.Name}'s counter gauge is READY!\n";

            _waitingForDefenseChoice = false;
            _pendingAttack           = null;
            _defendingCharacter      = null;

            _gridDisplay.UpdateCharacters(_players);

            if (_players.Count(p => p.IsAlive) <= 1)
            {
                var winner = _players.FirstOrDefault(p => p.IsAlive);
                return CreateDisplay(winner, msg + $"\n🎉 GAME OVER! {winner?.Name} wins!");
            }

            return CreateDisplay(CurrentActor, msg + "\n") + AdvanceToNextTurn();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Evasion movement (legacy coordinate path — still supported)
        // ─────────────────────────────────────────────────────────────────────

        private string ProcessEvasionMovement(GameActionCommand cmd)
        {
            if (cmd.Type == GameActionType.Coordinates)
            {
                var pos = cmd.TargetPosition;
                int dist = Math.Abs(_evadingCharacter.Position.X - pos.X) +
                           Math.Abs(_evadingCharacter.Position.Y - pos.Y);

                if (dist <= _pendingEvasionResult.MovementDistance && IsInArenaBounds(pos) &&
                    !_players.Any(p => p != _evadingCharacter && p.IsAlive && p.Position.Equals(pos)))
                {
                    var name = _evadingCharacter.Name;
                    _evadingCharacter.Position = pos;
                    _waitingForEvasionMovement = false;
                    _pendingEvasionResult = null;
                    _evadingCharacter = null;
                    return CreateDisplay(CurrentActor,
                        $"{name} moves to ({pos.X},{pos.Y}) after evasion.\n") + AdvanceToNextTurn();
                }
                return "Invalid position! Choose a tile within evasion range.";
            }

            if (cmd.Type == GameActionType.Skip)
            {
                var name = _evadingCharacter.Name;
                _waitingForEvasionMovement = false;
                _pendingEvasionResult = null;
                _evadingCharacter = null;
                return CreateDisplay(CurrentActor, $"{name} skips evasion movement.\n") + AdvanceToNextTurn();
            }

            return "Enter coordinates as 'x y' or type 'skip' to skip repositioning.";
        }

        // ─────────────────────────────────────────────────────────────────────
        // Legacy movement target (direct coordinates)
        // ─────────────────────────────────────────────────────────────────────

        private string ProcessMovementTarget(GameActionCommand cmd)
        {
            if (cmd.Type != GameActionType.Coordinates)
                return "Enter coordinates as 'x y', or type 'move' to use WASD mode instead.";

            var pos = cmd.TargetPosition;
            if (_movementSystem.ExecuteMovement(_pendingMovement, pos))
            {
                var result = $"{_pendingMovement.Character.Name} moved to ({pos.X},{pos.Y})\n";
                _waitingForMovementTarget = false;

                if (_pendingMovement.AllowsSecondAction)
                {
                    bool continues = UseAction(allowsSecondAction: true);
                    _pendingMovement = null;
                    if (continues)
                        return CreateDisplay(CurrentActor, result + "Can still act once more!\n\n" + GetContinuedTurnInstructions());
                    return CreateDisplay(CurrentActor, result) + AdvanceToNextTurn();
                }

                UseAction(allowsSecondAction: false);
                _pendingMovement = null;
                return CreateDisplay(CurrentActor, result) + AdvanceToNextTurn();
            }

            return CreateDisplay(CurrentActor, "Invalid position! Choose within movement range.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Action dispatch
        // ─────────────────────────────────────────────────────────────────────

        private string ExecuteGameAction(GameActionCommand cmd)
        {
            string result = cmd.Type switch
            {
                GameActionType.Attack =>
                    HandleAttackAction(cmd.Target != null ? FindCharacterByLetter(cmd.Target[0]) : null),
                GameActionType.EnterMovementMode => HandleMoveAction(),
                GameActionType.MoveToPosition    => HandleMoveAction(cmd.TargetPosition),
                GameActionType.EnterDashMode     => HandleDashAction(),
                GameActionType.DashToPosition    => HandleDashAction(cmd.TargetPosition),
                GameActionType.Rest              => HandleRestAction(),
                GameActionType.Defend            => HandleDefendAction(),
                GameActionType.Help              => GetHelpText(),
                GameActionType.Quit              => "Game ended by player.",
                _                                => GetHelpText()
            };

            _gridDisplay.UpdateCharacters(_players);
            return result;
        }

        private string HandleMoveAction(Position target = null)
            => target != null ? HandleDirectMovement(target, MovementType.Simple) : EnterMovementMode(MovementType.Simple);

        private string HandleDashAction(Position target = null)
            => target != null ? HandleDirectMovement(target, MovementType.Dash) : EnterMovementMode(MovementType.Dash);

        private string HandleDirectMovement(Position targetPos, MovementType moveType)
        {
            var character = CurrentActor;
            var moveResult = _movementSystem.CalculateMovement(character, moveType);
            string typeStr = moveType == MovementType.Simple ? "moves" : "dashes";
            var result = $"{character.Name} {typeStr}: {moveResult.MoveRoll} + {character.MovementPoints} MOV = {moveResult.MaxDistance} points\n\n";

            if (_movementSystem.ExecuteMovement(moveResult, targetPos))
            {
                result += $"{character.Name} {typeStr} to ({targetPos.X},{targetPos.Y})\n";
                _gridDisplay.UpdateCharacters(_players);

                if (moveType == MovementType.Simple)
                {
                    bool continues = UseAction(allowsSecondAction: true);
                    if (continues)
                        return CreateDisplay(character, result + "Can still act once more!\n\n" + GetContinuedTurnInstructions());
                    return CreateDisplay(character, result) + AdvanceToNextTurn();
                }
                UseAction(allowsSecondAction: false);
                return CreateDisplay(character, result) + AdvanceToNextTurn();
            }

            return CreateDisplay(character, result + "Invalid position! Use 'move' for WASD mode.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Individual actions
        // ─────────────────────────────────────────────────────────────────────

        private string HandleAttackAction(Character target)
        {
            var attacker = CurrentActor;
            if (attacker == null) return CreateDisplay(null, "No current actor found!");

            if (target == null)
            {
                var validTargets = _gridDisplay.GetCharactersInAttackRange(attacker);
                if (!validTargets.Any())
                    return CreateDisplay(attacker,
                        "No valid targets in range!\nMove closer to attack.");

                return CreateDisplay(attacker,
                    "Choose a target:\n" +
                    _gridDisplay.DrawAttackRange(attacker) +
                    "Use 'attack [letter]' (e.g., 'attack B')");
            }

            if (!_gridDisplay.IsInAttackRange(attacker.Position, target.Position))
                return CreateDisplay(attacker,
                    $"{target.Name} is not adjacent!\n" +
                    _gridDisplay.DrawAttackRange(attacker) +
                    $"\n{attacker.Name} @ ({attacker.Position.X},{attacker.Position.Y}) → " +
                    $"{target.Name} @ ({target.Position.X},{target.Position.Y})");

            var attackResult = _combatSystem.ExecuteAttack(attacker, target);
            if (!attackResult.Success)
                return CreateDisplay(attacker, attackResult.Message);

            // Counter attack turns bypass defense
            if (_turnManager.GetCurrentTurnType() == TurnType.CounterAttack)
            {
                var counterResult = _combatSystem.ExecuteCounterAttack(attacker, target);
                _gridDisplay.UpdateCharacters(_players);

                if (_players.Count(p => p.IsAlive) <= 1)
                {
                    var winner = _players.FirstOrDefault(p => p.IsAlive);
                    return CreateDisplay(winner, counterResult.Message + $"\n\n🎉 GAME OVER! {winner?.Name} wins!");
                }
                return CreateDisplay(attacker, counterResult.Message + "\n") + AdvanceToNextTurn();
            }

            // Regular attack — wait for defense response
            _pendingAttack           = attackResult;
            _defendingCharacter      = target;
            _waitingForDefenseChoice = true;

            return CreateDisplay(attacker,
                $"{attackResult.Message}\n\n" +
                $"{target.Name}, choose your response:\n" +
                "  'defend' — 2 SP, roll 2d6+DEF, build counter on over-defense\n" +
                "  'move'   — 1 SP, roll 2d6+MOV evasion vs attack value\n" +
                "  'take'   — save SP, take full damage\n\n" +
                $"Target has {target.CurrentStamina} SP available.");
        }

        private string HandleRestAction()
        {
            var character = CurrentActor;
            int restored = Math.Min(
                GameConfig.Current.Combat.RestStaminaRestore,
                character.MaxStamina - character.CurrentStamina);
            character.RestoreStamina(restored);
            UseAction(allowsSecondAction: false);
            _gridDisplay.UpdateCharacters(_players);
            return CreateDisplay(character, $"{character.Name} rests and recovers {restored} SP.\n") + AdvanceToNextTurn();
        }

        /// <summary>
        /// BUG FIX: Defend stance costs 1 stamina to prevent infinite stalemate.
        /// </summary>
        private string HandleDefendAction()
        {
            var character = CurrentActor;

            if (!character.UseStamina(1))
                return CreateDisplay(character,
                    $"{character.Name} doesn't have enough stamina to defend!\n" +
                    "Try 'rest' to recover stamina.");

            UseAction(allowsSecondAction: false);
            _gridDisplay.UpdateCharacters(_players);
            return CreateDisplay(character, $"{character.Name} takes a defensive stance. (-1 SP)\n") + AdvanceToNextTurn();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Display helper — always centers viewport on a character
        // ─────────────────────────────────────────────────────────────────────

        private string CreateDisplay(Character focusCharacter, string info = "")
        {
            _gridDisplay.UpdateCharacters(_players);
            return _gridDisplay.CreateFullDisplay(focusCharacter ?? CurrentActor, info);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Utilities
        // ─────────────────────────────────────────────────────────────────────

        private void PlacePlayersOnGrid()
        {
            var positions = GameConfig.Current.Grid.GetStartingPositions();

            // If config has fewer positions than players, generate extras
            for (int i = positions.Count; i < _players.Count; i++)
                positions.Add(new Position(i * 3, i * 3));

            for (int i = 0; i < _players.Count; i++)
                _players[i].Position = positions[i];
        }

        private Character FindCharacterByLetter(char letter)
            => _players.FirstOrDefault(p => p.IsAlive && char.ToUpper(p.Name[0]) == char.ToUpper(letter));

        /// <summary>
        /// Check whether a position is inside the arena bounds from config.
        /// </summary>
        private bool IsInArenaBounds(Position pos)
            => pos.X >= 0 && pos.X < ArenaWidth && pos.Y >= 0 && pos.Y < ArenaHeight;

        private bool IsInArenaBounds(int x, int y)
            => x >= 0 && x < ArenaWidth && y >= 0 && y < ArenaHeight;

        private string GetContinuedTurnInstructions()
            => $"{CurrentActor?.Name}'s turn continues...\n" +
               $"Actions used: {_actionsUsedThisTurn}/{_maxActionsThisTurn}\n" +
               "  'attack [letter]' — attack adjacent character\n" +
               "  'move' — WASD movement (1d6), allows second action\n" +
               "  'dash' — WASD dash (2d6), ends turn\n" +
               "  'rest' — recover stamina\n" +
               "  'defend' — defensive stance (1 SP)";

        private string GetTurnInstructions(TurnResult turn)
        {
            if (!turn.Success) return turn.Message;
            var sb = new System.Text.StringBuilder("Available actions:\n");
            if (turn.AvailableActions.Contains(ActionChoice.Attack))
                sb.AppendLine("  'attack [letter]' — attack adjacent character");
            if (turn.AvailableActions.Contains(ActionChoice.Move))
            {
                sb.AppendLine("  'move' — WASD movement mode (allows second action)");
                sb.AppendLine("  'dash' — WASD dash mode (ends turn)");
            }
            if (turn.AvailableActions.Contains(ActionChoice.Rest))
                sb.AppendLine("  'rest' — recover stamina");
            if (turn.AvailableActions.Contains(ActionChoice.Defend))
                sb.AppendLine("  'defend' — defensive stance (1 SP)");
            sb.AppendLine("\nMovement: WASD keys for step-by-step control!");
            sb.AppendLine("Type 'help' for more information.");
            return sb.ToString();
        }

        private string GetHelpText()
            => "Commands:\n" +
               "  attack [letter]   attack adjacent character\n" +
               "  move              WASD movement (1d6 + MOV), allows second action\n" +
               "  dash              WASD dash (2d6 + MOV), ends turn\n" +
               "  move x y / dash x y   move to specific coordinates (legacy)\n" +
               "  rest              recover stamina\n" +
               "  defend            defensive stance (costs 1 SP)\n\n" +
               "WASD Controls (in movement mode):\n" +
               "  W/↑ Up   S/↓ Down   A/← Left   D/→ Right\n" +
               "  Enter — confirm path    R — reset path    ESC — cancel\n\n" +
               "Stamina Costs:\n" +
               $"  Attack: {GameConfig.Current.Combat.StaminaCosts.Attack} SP   " +
               $"Defend (response): {GameConfig.Current.Combat.StaminaCosts.Defend} SP   " +
               $"Evasion: {GameConfig.Current.Combat.StaminaCosts.Move} SP\n" +
               $"  Move/Dash: 1 SP   Defend stance: 1 SP   Rest: free (restores {GameConfig.Current.Combat.RestStaminaRestore} SP)\n\n" +
               $"Arena: {ArenaWidth}×{ArenaHeight} tiles   " +
               $"Viewport: {GameConfig.Current.Grid.Viewport.Width}×{GameConfig.Current.Grid.Viewport.Height} tiles";
    }
}