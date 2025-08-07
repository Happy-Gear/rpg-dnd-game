using System;
using System.Collections.Generic;
using RPGGame.Character;
using RPGGame.Dice;

namespace RPGGame.Combat
{
    /// <summary>
    /// Core combat system handling ATK/DEF/MOV actions and badminton streak mechanics
    /// </summary>
    public class CombatSystem
    {
        private DiceRoller _diceRoller;
        private List<CombatLog> _combatHistory;
        
        public CombatSystem(int? seed = null)
        {
            _diceRoller = new DiceRoller(seed);
            _combatHistory = new List<CombatLog>();
        }
        
        /// <summary>
        /// Execute an attack action between two characters
        /// </summary>
        public AttackResult ExecuteAttack(Character attacker, Character defender)
        {
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
            var attackRoll = _diceRoller.Roll2d6("ATK");
            int totalAttackDamage = attackRoll.Total + attacker.AttackPoints;
            
            var result = new AttackResult
            {
                Success = true,
                Attacker = attacker.Name,
                Defender = defender.Name,
                AttackRoll = attackRoll,
                BaseAttackDamage = totalAttackDamage,
                Message = $"{attacker.Name} attacks {defender.Name} for {totalAttackDamage} damage!"
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
            var result = new DefenseResult
            {
                DefenseChoice = choice,
                IncomingDamage = incomingAttack.BaseAttackDamage
            };
            
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
            var defenseRoll = _diceRoller.Roll2d6("DEF");
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
                Message = BuildDefenseMessage(defender, totalDefense, incomingAttack.BaseAttackDamage, overDefense)
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
        /// Handle MOV action (evasion)
        /// </summary>
        private DefenseResult HandleMoveAction(Character defender, AttackResult incomingAttack)
        {
            // MOV costs 1 stamina and avoids damage entirely
            if (!defender.UseStamina(1))
            {
                return HandleTakeDamage(defender, incomingAttack, "Insufficient stamina to move!");
            }
            
            // Counter gauge is preserved (no reset)
            
            var result = new DefenseResult
            {
                DefenseChoice = DefenseChoice.Move,
                FinalDamage = 0,
                CounterBuilt = 0,
                CounterReady = defender.Counter.IsReady,
                Message = $"{defender.Name} evades the attack completely!"
            };
            
            // Log combat action
            LogCombat(new CombatLog
            {
                Action = CombatAction.Move,
                ActorName = defender.Name,
                StaminaCost = 1,
                AdditionalInfo = "Evaded attack, counter preserved",
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
            if (!counterAttacker.Counter.IsReady)
            {
                return new AttackResult
                {
                    Success = false,
                    Message = $"{counterAttacker.Name} counter gauge not ready!"
                };
            }
            
            // Consume counter gauge
            counterAttacker.Counter.ConsumeCounter();
            
            // Counter attacks are "free" (no stamina cost) but use same damage system
            var attackRoll = _diceRoller.Roll2d6("COUNTER");
            int totalDamage = attackRoll.Total + counterAttacker.AttackPoints;
            
            // Counter attacks bypass defense (immediate damage)
            target.TakeDamage(totalDamage);
            // Target's counter gauge is preserved (no reset)
            
            var result = new AttackResult
            {
                Success = true,
                Attacker = counterAttacker.Name,
                Defender = target.Name,
                AttackRoll = attackRoll,
                BaseAttackDamage = totalDamage,
                IsCounterAttack = true,
                Message = $"⚡ {counterAttacker.Name} COUNTER ATTACKS {target.Name} for {totalDamage} damage! [BADMINTON STREAK!]"
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
        private string BuildDefenseMessage(Character defender, int totalDefense, int incomingDamage, int counterBuilt)
        {
            if (totalDefense >= incomingDamage)
            {
                string counterMsg = counterBuilt > 0 ? $" Counter +{counterBuilt}!" : "";
                return $"{defender.Name} blocks completely!{counterMsg}";
            }
            else
            {
                int damage = incomingDamage - totalDefense;
                return $"{defender.Name} partially blocks, takes {damage} damage.";
            }
        }
        
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