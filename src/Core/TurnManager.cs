using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Characters;
using RPGGame.Combat;

namespace RPGGame.Core
{
    /// <summary>
    /// Manages turn order and game flow for turn-based combat
    /// </summary>
    public class TurnManager
    {
        private List<Character> _participants;
        private Queue<Character> _turnQueue;
        private Character _currentActor;
        private TurnType _currentTurnType = TurnType.Normal;
        private int _roundNumber;
        private List<TurnLog> _turnHistory;
        
        public Character CurrentActor => _currentActor;
        public int RoundNumber => _roundNumber;
        public bool GameActive { get; private set; }
        
        public TurnManager()
        {
            _participants = new List<Character>();
            _turnQueue = new Queue<Character>();
            _turnHistory = new List<TurnLog>();
            _roundNumber = 0;
            GameActive = false;
        }
        
        /// <summary>
        /// Initialize combat with participants
        /// </summary>
        public void StartCombat(params Character[] characters)
        {
            _participants.Clear();
            _participants.AddRange(characters.Where(c => c.IsAlive));
            
            if (_participants.Count < 2)
                throw new InvalidOperationException("Need at least 2 living characters for combat");
            
            ShuffleTurnOrder();
            StartNewRound();
            GameActive = true;
            
            LogTurn($"Combat started with {_participants.Count} participants");
        }
        
        /// <summary>
        /// Advance to next player's turn
        /// </summary>
        public TurnResult NextTurn()
        {
            if (!GameActive)
                return new TurnResult { Success = false, Message = "Combat not active" };

            // Check for counter-attacks (badminton streak interruptions)
            var counterAttacker = CheckForCounterAttacks();
            if (counterAttacker != null)
            {
                return HandleCounterAttackTurn(counterAttacker);
            }

            // Normal turn progression
            if (_turnQueue.Count == 0)
            {
                StartNewRound();
            }

            int attempts = 0;
            int maxAttempts = GameConfig.Current.Turns.MaxSkipAttempts;

            while (attempts < maxAttempts)
            {
                if (_turnQueue.Count == 0)
                {
                    StartNewRound();
                }

                _currentActor = _turnQueue.Dequeue();

                if (_currentActor.CanAct)
                {
                    _currentTurnType = TurnType.Normal;
                    LogTurn($"{_currentActor.Name}'s turn begins");
                    return new TurnResult
                    {
                        Success = true,
                        CurrentActor = _currentActor,
                        TurnType = TurnType.Normal,
                        AvailableActions = GetAvailableActions(_currentActor),
                        Message = $"{_currentActor.Name}'s turn"
                    };
                }

                if (_currentActor.IsAlive && _currentActor.CurrentStamina == 0)
                {
                    var restAmount = GameConfig.Current.Turns.ForcedRestRestore;
                    LogTurn($"{_currentActor.Name} has no stamina - forced to rest");
                    _currentActor.RestoreStamina(restAmount);
                    _currentTurnType = TurnType.Normal;
                    
                    return new TurnResult
                    {
                        Success = true,
                        CurrentActor = _currentActor,
                        TurnType = TurnType.Normal,
                        AvailableActions = GetAvailableActions(_currentActor),
                        Message = $"{_currentActor.Name} was forced to rest (no stamina) and recovered {restAmount} stamina"
                    };
                }

                LogTurn($"{_currentActor.Name} cannot act, skipping turn");
                attempts++;
            }

            LogTurn("All characters unable to act - ending combat");
            GameActive = false;
            return new TurnResult 
            { 
                Success = false, 
                Message = "Combat ended - no valid actors remaining" 
            };
        }
        
        /// <summary>
        /// Execute a player action during their turn
        /// </summary>
        public ActionResult ExecuteAction(ActionChoice action, Character target = null)
        {
            if (!GameActive || _currentActor == null)
                return new ActionResult { Success = false, Message = "Not a valid turn" };
            
            var result = new ActionResult
            {
                Actor = _currentActor,
                Action = action,
                Target = target,
                Success = true
            };
            
            switch (action)
            {
                case ActionChoice.Attack:
                    result = HandleAttackAction(target);
                    break;
                case ActionChoice.Defend:
                    result = HandleDefendAction();
                    break;
                case ActionChoice.Move:
                    result = HandleMoveAction();
                    break;
                case ActionChoice.Rest:
                    result = HandleRestAction();
                    break;
                default:
                    result.Success = false;
                    result.Message = "Invalid action";
                    break;
            }
            
            if (CheckWinCondition())
            {
                GameActive = false;
                result.GameEnded = true;
                result.Winner = _participants.FirstOrDefault(p => p.IsAlive);
            }
            
            LogTurn($"{_currentActor.Name} used {action}" + (target != null ? $" on {target.Name}" : ""));
            
            return result;
        }
        
        /// <summary>
        /// Handle counter-attack turns (badminton streak)
        /// </summary>
        private TurnResult HandleCounterAttackTurn(Character counterAttacker)
        {
            _currentActor = counterAttacker;
            _currentTurnType = TurnType.CounterAttack;
            LogTurn($"⚡ {counterAttacker.Name} gets a COUNTER ATTACK turn! [BADMINTON STREAK]");
            
            // Consume counter immediately to prevent infinite loop
            counterAttacker.Counter.ConsumeCounter();
            
            return new TurnResult
            {
                Success = true,
                CurrentActor = counterAttacker,
                TurnType = TurnType.CounterAttack,
                AvailableActions = new List<ActionChoice> { ActionChoice.Attack },
                Message = $"⚡ {counterAttacker.Name} COUNTER ATTACK! Counter consumed - use it now!"
            };
        }

        /// <summary>
        /// Check if any character has a ready counter
        /// </summary>
        private Character CheckForCounterAttacks()
        {
            return _participants.FirstOrDefault(p => p.IsAlive && p.Counter.IsReady);
        }
        
        private void StartNewRound()
        {
            _roundNumber++;
            _turnQueue.Clear();
            
            var livingParticipants = _participants.Where(p => p.IsAlive).ToList();
            
            foreach (var participant in livingParticipants)
            {
                _turnQueue.Enqueue(participant);
            }
            
            LogTurn($"=== ROUND {_roundNumber} BEGINS ===");
        }
        
        private void ShuffleTurnOrder()
        {
            var random = new Random();
            _participants = _participants.OrderBy(x => random.Next()).ToList();
        }
        
        private List<ActionChoice> GetAvailableActions(Character character)
        {
            var actions = new List<ActionChoice>();
            var costs = GameConfig.Current.Combat.StaminaCosts;
            
            if (character.CurrentStamina >= costs.Attack) actions.Add(ActionChoice.Attack);
            if (character.CurrentStamina >= costs.Defend) actions.Add(ActionChoice.Defend);
            if (character.CurrentStamina >= costs.Move) actions.Add(ActionChoice.Move);
            actions.Add(ActionChoice.Rest);
            
            return actions;
        }
        
        private ActionResult HandleAttackAction(Character target)
        {
            if (target == null || !target.IsAlive)
                return new ActionResult { Success = false, Message = "Invalid target" };
            
            if (_currentActor.CurrentStamina < GameConfig.Current.Combat.StaminaCosts.Attack)
                return new ActionResult { Success = false, Message = "Insufficient stamina for attack" };
            
            return new ActionResult
            {
                Success = true,
                Action = ActionChoice.Attack,
                Actor = _currentActor,
                Target = target,
                Message = $"{_currentActor.Name} attacks {target.Name}",
                RequiresTargetResponse = true
            };
        }
        
        private ActionResult HandleDefendAction()
        {
            return new ActionResult
            {
                Success = true,
                Action = ActionChoice.Defend,
                Actor = _currentActor,
                Message = $"{_currentActor.Name} takes a defensive stance",
                DefenseBonusNextTurn = true
            };
        }
        
        private ActionResult HandleMoveAction()
        {
            var moveCost = GameConfig.Current.Combat.StaminaCosts.Move;
            
            if (_currentActor.CurrentStamina < moveCost)
                return new ActionResult { Success = false, Message = "Insufficient stamina for movement" };
            
            _currentActor.UseStamina(moveCost);
            
            return new ActionResult
            {
                Success = true,
                Action = ActionChoice.Move,
                Actor = _currentActor,
                Message = $"{_currentActor.Name} moves to a new position"
            };
        }
        
        private ActionResult HandleRestAction()
        {
            var restAmount = GameConfig.Current.Combat.RestStaminaRestore;
            int staminaRestored = Math.Min(restAmount, _currentActor.MaxStamina - _currentActor.CurrentStamina);
            _currentActor.RestoreStamina(staminaRestored);
            
            return new ActionResult
            {
                Success = true,
                Action = ActionChoice.Rest,
                Actor = _currentActor,
                Message = $"{_currentActor.Name} rests and recovers {staminaRestored} stamina"
            };
        }
        
        private bool CheckWinCondition()
        {
            return _participants.Where(p => p.IsAlive).Count() <= 1;
        }
        
        private void LogTurn(string message)
        {
            _turnHistory.Add(new TurnLog
            {
                Round = _roundNumber,
                Message = message,
                Timestamp = DateTime.Now
            });
        }
        
        public List<TurnLog> GetTurnHistory(int recentCount = 20)
        {
            int start = Math.Max(0, _turnHistory.Count - recentCount);
            return _turnHistory.GetRange(start, _turnHistory.Count - start);
        }
        
        public List<Character> GetLivingParticipants()
        {
            return _participants.Where(p => p.IsAlive).ToList();
        }

        public TurnType GetCurrentTurnType()
        {
            return _currentTurnType;
        }
    }
}