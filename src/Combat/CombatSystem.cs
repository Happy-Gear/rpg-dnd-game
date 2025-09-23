using System;
using System.Collections.Generic;
using RPGGame.Characters;
using RPGGame.Dice;

namespace RPGGame.Combat
{
    /// <summary>
    /// Core combat system handling ATK/DEF/MOV actions and badminton streak mechanics
    /// </summary>
    public class CombatSystem
    {
        public DiceRoller DiceRoller { get; private set; }
        private List<CombatLog> _combatHistory;
        
        public CombatSystem(int? seed = null)
        {
            DiceRoller = new DiceRoller(seed);
            _combatHistory = new List<CombatLog>();
        }
        
        /// <summary>
        /// Execute an attack action between two characters
        /// </summary>
        public AttackResult ExecuteAttack(Character attacker, Character defender)
        {
            // Validate inputs
            if (attacker == null)
                throw new ArgumentNullException(nameof(attacker));
            if (defender == null)
                throw new ArgumentNullException(nameof(defender));
            
            // Validate attack can happen
            if (!CanAttack(attacker))
            {
                return new AttackResult
                {
                    Success = false,
                    Message = $"{attacker.Name} cannot attack (insufficient stamina or incapacitated)"
                };
            }
            
            // Consume stamina for attack
            attacker.UseStamina(3);
            
            // Roll attack dice (2d6 + ATK modifier)
            var attackRoll = DiceRoller.Roll2d6("ATK");
            int totalAttackDamage = attackRoll.Total + attacker.AttackPoints;
            
            var result = new AttackResult
			{
				Success = true,
				Attacker = attacker.Name,
				Defender = defender.Name,
				AttackRoll = attackRoll,
				BaseAttackDamage = totalAttackDamage,
				Message = $"{attacker.Name} attacks {defender.Name}: {attackRoll} + {attacker.AttackPoints} ATK = {totalAttackDamage} damage!"
			};
            // Log combat action
            LogCombat(new CombatLog
            {
                Action = CombatAction.Attack,
                ActorName = attacker.Name,
                TargetName = defender.Name,
                DiceRoll = attackRoll,
                StaminaCost = 3,
                Timestamp = DateTime.Now
            });
            
            return result;
        }
        
        /// <summary>
        /// Handle defender's response to an incoming attack
        /// </summary>
		public DefenseResult ResolveDefense(Character defender, AttackResult incomingAttack, DefenseChoice choice)
		{
            // Validate inputs
            if (defender == null)
                throw new ArgumentNullException(nameof(defender));
            if (incomingAttack == null)
                throw new ArgumentNullException(nameof(incomingAttack));
            
			var result = new DefenseResult();
			
			switch (choice)
			{
				case DefenseChoice.Defend:
					result = HandleDefenseAction(defender, incomingAttack);
					break;
					
				case DefenseChoice.Move:
					result = HandleMoveAction(defender, incomingAttack);
					break;
					
				case DefenseChoice.TakeDamage:
					result = HandleTakeDamage(defender, incomingAttack);
					break;
			}
			
			return result;
		}
		/// <summary>
		/// Handle DEF action with counter gauge mechanics
		/// </summary>
		private DefenseResult HandleDefenseAction(Character defender, AttackResult incomingAttack)
		{
			// Check if defender has enough stamina
			if (!CanDefend(defender))
			{
				return HandleTakeDamage(defender, incomingAttack, "Insufficient stamina to defend!");
			}
			
			// Consume stamina for defense
			defender.UseStamina(2);
			
			// Roll defense dice (2d6 + DEF modifier)
			var defenseRoll = DiceRoller.Roll2d6("DEF");
			int totalDefense = defenseRoll.Total + defender.DefensePoints;
			
			// Calculate damage reduction
			int damageBlocked = Math.Min(totalDefense, incomingAttack.BaseAttackDamage);
			int finalDamage = Math.Max(0, incomingAttack.BaseAttackDamage - totalDefense);
			
			// Handle counter gauge for over-defense (badminton streak)
			int overDefense = Math.Max(0, totalDefense - incomingAttack.BaseAttackDamage);
			if (overDefense > 0)
			{
				defender.Counter.AddCounter(overDefense);
			}
			
			// Apply remaining damage
			if (finalDamage > 0)
			{
				defender.TakeDamage(finalDamage);
			}
			
			var result = new DefenseResult
			{
				DefenseChoice = DefenseChoice.Defend,
				DefenseRoll = defenseRoll,
				TotalDefense = totalDefense,
				DamageBlocked = damageBlocked,
				FinalDamage = finalDamage,
				CounterBuilt = overDefense,
				CounterReady = defender.Counter.IsReady,
				IncomingDamage = incomingAttack.BaseAttackDamage,
				Message = $"{defender.Name} defends: {defenseRoll} + {defender.DefensePoints} DEF = {totalDefense} defense" +
						 (finalDamage > 0 ? $" - takes {finalDamage} damage" : " - blocks completely!") +
						 (overDefense > 0 ? $" Counter +{overDefense}!" : "")
			};
			
			// Log combat action
			LogCombat(new CombatLog
			{
				Action = CombatAction.Defend,
				ActorName = defender.Name,
				DiceRoll = defenseRoll,
				StaminaCost = 2,
				AdditionalInfo = $"Blocked {damageBlocked}, Counter +{overDefense}",
				Timestamp = DateTime.Now
			});
			
			return result;
		}
        
		/// <summary>
		/// Handle MOV action (evasion with dice roll)
		/// </summary>
		private DefenseResult HandleMoveAction(Character defender, AttackResult incomingAttack)
		{
			// MOV costs 1 stamina
			if (!defender.UseStamina(1))
			{
				return HandleTakeDamage(defender, incomingAttack, "Insufficient stamina to move!");
			}
			
			// Roll evasion dice (2d6 + MOV modifier)
			var evasionRoll = DiceRoller.Roll2d6("EVASION");
			int totalEvasion = evasionRoll.Total + defender.MovementPoints;
			
			// Compare evasion vs incoming attack
			int attackValue = incomingAttack.BaseAttackDamage;
			int evasionDifference = totalEvasion - attackValue;
			
			var result = new DefenseResult
			{
				DefenseChoice = DefenseChoice.Move,
				DefenseRoll = evasionRoll, // Store the evasion roll
				TotalDefense = totalEvasion,
				IncomingDamage = attackValue,
				CounterReady = defender.Counter.IsReady
			};
			
			if (evasionDifference >= 0)
			{
				// Successful evasion - no damage, can move
				result.FinalDamage = 0;
				result.CounterBuilt = 0;
				result.CanMove = true;
				result.MovementDistance = Math.Max(1, evasionDifference);
				result.Message = $"{defender.Name} evades: {evasionRoll} + {defender.MovementPoints} MOV = {totalEvasion} evasion vs {attackValue} attack - " +
								$"evades completely and can move {result.MovementDistance} spaces!";
			}
			else
			{
				// Failed evasion - take damage, no movement
				int damage = Math.Abs(evasionDifference);
				defender.TakeDamage(damage);
				result.FinalDamage = damage;
				result.CounterBuilt = 0;
				result.CanMove = false;
				result.MovementDistance = 0;
				result.Message = $"{defender.Name} evades: {evasionRoll} + {defender.MovementPoints} MOV = {totalEvasion} evasion vs {attackValue} attack - " +
								$"fails to evade, takes {damage} damage!";
			}
			
			// Log combat action
			LogCombat(new CombatLog
			{
				Action = CombatAction.Move,
				ActorName = defender.Name,
				DiceRoll = evasionRoll,
				StaminaCost = 1,
				AdditionalInfo = $"Evasion: {totalEvasion} vs Attack: {attackValue}, Difference: {evasionDifference}",
				Timestamp = DateTime.Now
			});
			
			return result;
		}
        
        /// <summary>
        /// Handle taking damage without defense
        /// </summary>
        private DefenseResult HandleTakeDamage(Character defender, AttackResult incomingAttack, string reason = "")
        {
            defender.TakeDamage(incomingAttack.BaseAttackDamage);
            // Counter gauge is preserved (no reset)
            
            string message = string.IsNullOrEmpty(reason) 
                ? $"{defender.Name} takes {incomingAttack.BaseAttackDamage} damage!"
                : $"{defender.Name} takes {incomingAttack.BaseAttackDamage} damage! ({reason})";
            
            return new DefenseResult
            {
                DefenseChoice = DefenseChoice.TakeDamage,
                FinalDamage = incomingAttack.BaseAttackDamage,
                IncomingDamage = incomingAttack.BaseAttackDamage,
                CounterBuilt = 0,
                CounterReady = defender.Counter.IsReady,
                Message = message
            };
        }
        
        /// <summary>
        /// Execute counter attack (badminton streak)
        /// </summary>
		public AttackResult ExecuteCounterAttack(Character counterAttacker, Character target)
		{
			// Validate inputs
			if (counterAttacker == null)
				throw new ArgumentNullException(nameof(counterAttacker));
			if (target == null)
				throw new ArgumentNullException(nameof(target));
			
			// Counter should already be consumed by HandleCounterAttackTurn
			// But check anyway for safety
			if (counterAttacker.Counter.Current < counterAttacker.Counter.Maximum)
			{
				// Counter was already consumed - this is expected for counter turns
				// Continue with counter attack at full power
			}
			else if (counterAttacker.Counter.IsReady)
			{
				// Counter still ready - consume it now (legacy path)
				counterAttacker.Counter.ConsumeCounter();
			}
			else
			{
				// No counter available
				return new AttackResult
				{
					Success = false,
					Message = $"{counterAttacker.Name} counter gauge not ready!"
				};
			}
			
			// Execute counter attack at full power (always uses maximum counter value)
			var attackRoll = DiceRoller.Roll2d6("COUNTER");
			int totalDamage = attackRoll.Total + counterAttacker.AttackPoints;
			
			// Counter attacks bypass defense (immediate damage)
			target.TakeDamage(totalDamage);
			
			var result = new AttackResult
			{
				Success = true,
				Attacker = counterAttacker.Name,
				Defender = target.Name,
				AttackRoll = attackRoll,
				BaseAttackDamage = totalDamage,
				IsCounterAttack = true,
				Message = $"⚡ {counterAttacker.Name} COUNTER ATTACKS {target.Name}: {attackRoll} + {counterAttacker.AttackPoints} ATK = {totalDamage} damage! [BADMINTON STREAK!]"
			};
			
			// Log combat action
			LogCombat(new CombatLog
			{
				Action = CombatAction.CounterAttack,
				ActorName = counterAttacker.Name,
				TargetName = target.Name,
				DiceRoll = attackRoll,
				StaminaCost = 0,
				AdditionalInfo = "Badminton Streak activated!",
				Timestamp = DateTime.Now
			});
			
			return result;
		}     
        // Validation methods
        private bool CanAttack(Character character) => character.CanAct && character.CurrentStamina >= 3;
        private bool CanDefend(Character character) => character.CanAct && character.CurrentStamina >= 2;
        
        // Utility methods
        private void LogCombat(CombatLog log)
        {
            _combatHistory.Add(log);
        }
        
        public List<CombatLog> GetCombatHistory(int recentCount = 10)
        {
            int start = Math.Max(0, _combatHistory.Count - recentCount);
            return _combatHistory.GetRange(start, _combatHistory.Count - start);
        }
    }
}