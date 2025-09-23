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
            
            // Determine turn order (could be based on AGI/initiative later)
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

			// SAFETY: Prevent infinite recursion
			int attempts = 0;
			const int MAX_ATTEMPTS = 10;

			while (attempts < MAX_ATTEMPTS)
			{
				if (_turnQueue.Count == 0)
				{
					StartNewRound();
				}

				_currentActor = _turnQueue.Dequeue();

				// If character can act normally, return
				if (_currentActor.CanAct)
				{
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

				// Character can't act - force rest if alive but no stamina
				if (_currentActor.IsAlive && _currentActor.CurrentStamina == 0)
				{
					LogTurn($"{_currentActor.Name} has no stamina - forced to rest");
					_currentActor.RestoreStamina(5); // Force rest
					
					return new TurnResult
					{
						Success = true,
						CurrentActor = _currentActor,
						TurnType = TurnType.Normal,
						AvailableActions = GetAvailableActions(_currentActor),
						Message = $"{_currentActor.Name} was forced to rest (no stamina) and recovered 5 stamina"
					};
				}

				// Character is dead - skip
				LogTurn($"{_currentActor.Name} cannot act, skipping turn");
				attempts++;
			}

			// Safety fallback - end game if all characters stuck
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
            
            // Check for win condition
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
			LogTurn($"⚡ {counterAttacker.Name} gets a COUNTER ATTACK turn! [BADMINTON STREAK]");
			
			// CRITICAL FIX: Consume counter immediately when granting counter turn
			// This prevents infinite loop of counter attack turns
			counterAttacker.Counter.ConsumeCounter();
			
			return new TurnResult
			{
				Success = true,
				CurrentActor = counterAttacker,
				TurnType = TurnType.CounterAttack,
				AvailableActions = new List<ActionChoice> { ActionChoice.Attack }, // Only attack on counter turns
				Message = $"⚡ {counterAttacker.Name} COUNTER ATTACK! Counter consumed - use it now!"
			};
		}
		// <summary>
        /// Check if any character has a ready counter
        /// </summary>
        private Character CheckForCounterAttacks()
        {
            return _participants.FirstOrDefault(p => p.IsAlive && p.Counter.IsReady);
        }
        
        /// <summary>
        /// Start a new round with all living participants
        /// </summary>
        private void StartNewRound()
        {
            _roundNumber++;
            _turnQueue.Clear();
            
            // Add all living participants to turn queue
            var livingParticipants = _participants.Where(p => p.IsAlive).ToList();
            
            // Shuffle turn order each round (or implement initiative system)
            foreach (var participant in livingParticipants)
            {
                _turnQueue.Enqueue(participant);
            }
            
            LogTurn($"=== ROUND {_roundNumber} BEGINS ===");
        }
        
        /// <summary>
        /// Determine turn order (placeholder for initiative system)
        /// </summary>
        private void ShuffleTurnOrder()
        {
            // Future: Implement initiative based on AGI + dice roll
            // For now, random order
            var random = new Random();
            _participants = _participants.OrderBy(x => random.Next()).ToList();
        }
        
        /// <summary>
        /// Get available actions for current character
        /// </summary>
        private List<ActionChoice> GetAvailableActions(Character character)
        {
            var actions = new List<ActionChoice>();
            
            if (character.CurrentStamina >= 3) actions.Add(ActionChoice.Attack);
            if (character.CurrentStamina >= 2) actions.Add(ActionChoice.Defend);
            if (character.CurrentStamina >= 1) actions.Add(ActionChoice.Move);
            actions.Add(ActionChoice.Rest); // Always available
            
            return actions;
        }
        
        /// <summary>
        /// Handle different action types
        /// </summary>
        private ActionResult HandleAttackAction(Character target)
        {
            if (target == null || !target.IsAlive)
                return new ActionResult { Success = false, Message = "Invalid target" };
            
            if (_currentActor.CurrentStamina < 3)
                return new ActionResult { Success = false, Message = "Insufficient stamina for attack" };
            
            return new ActionResult
            {
                Success = true,
                Action = ActionChoice.Attack,
                Actor = _currentActor,
                Target = target,
                Message = $"{_currentActor.Name} attacks {target.Name}",
                RequiresTargetResponse = true // Target must choose DEF/MOV/TakeDamage
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
            if (_currentActor.CurrentStamina < 1)
                return new ActionResult { Success = false, Message = "Insufficient stamina for movement" };
            
            _currentActor.UseStamina(1);
            
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
            // Rest restores stamina and allows access to special abilities
            int staminaRestored = Math.Min(5, _currentActor.MaxStamina - _currentActor.CurrentStamina);
            _currentActor.RestoreStamina(staminaRestored);
            
            return new ActionResult
            {
                Success = true,
                Action = ActionChoice.Rest,
                Actor = _currentActor,
                Message = $"{_currentActor.Name} rests and recovers {staminaRestored} stamina"
            };
        }
        
        /// <summary>
        /// Check if combat should end
        /// </summary>
        private bool CheckWinCondition()
        {
            var livingParticipants = _participants.Where(p => p.IsAlive).Count();
            return livingParticipants <= 1;
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
			return TurnType.Normal; // Placeholder - implement based on your turn tracking
		}
    }
}
