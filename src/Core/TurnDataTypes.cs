using System;
using System.Collections.Generic;
using RPGGame.Character;

namespace RPGGame.Core
{
    /// <summary>
    /// Available actions a character can take on their turn
    /// </summary>
    public enum ActionChoice
    {
        Attack,    // 3 stamina, 2d6 + ATK mod damage
        Defend,    // 2 stamina, 2d6 + DEF mod defense, build counter
        Move,      // 1 stamina, reposition, tactical advantages
        Rest       // 0 stamina, restore stamina, access special abilities
    }
    
    /// <summary>
    /// Types of turns in the game
    /// </summary>
    public enum TurnType
    {
        Normal,        // Regular turn in turn order
        CounterAttack, // Badminton streak interrupt turn
        Special        // Future: Initiative bonus turns, etc.
    }
    
    /// <summary>
    /// Result of requesting the next turn
    /// </summary>
    public class TurnResult
    {
        public bool Success { get; set; }
        public Character CurrentActor { get; set; }
        public TurnType TurnType { get; set; }
        public List<ActionChoice> AvailableActions { get; set; } = new List<ActionChoice>();
        public string Message { get; set; }
        
        public override string ToString()
        {
            string actionList = string.Join(", ", AvailableActions);
            string turnTypeStr = TurnType == TurnType.CounterAttack ? " [COUNTER!]" : "";
            return $"{CurrentActor?.Name}'s turn{turnTypeStr} - Available: {actionList}";
        }
    }
    
    /// <summary>
    /// Result of executing an action during a turn
    /// </summary>
    public class ActionResult
    {
        public bool Success { get; set; }
        public Character Actor { get; set; }
        public Character Target { get; set; }
        public ActionChoice Action { get; set; }
        public string Message { get; set; }
        
        // Special flags for different action types
        public bool RequiresTargetResponse { get; set; } // Attack needs target to choose defense
        public bool DefenseBonusNextTurn { get; set; }   // Defend action effects
        public bool GameEnded { get; set; }
        public Character Winner { get; set; }
        
        public override string ToString()
        {
            string target = Target != null ? $" → {Target.Name}" : "";
            string status = Success ? "✓" : "✗";
            return $"{status} {Actor?.Name}: {Action}{target} - {Message}";
        }
    }
    
    /// <summary>
    /// Turn history log entry
    /// </summary>
    public class TurnLog
    {
        public int Round { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        
        public override string ToString()
        {
            return $"R{Round}: {Message}";
        }
    }
    
    /// <summary>
    /// Complete game state snapshot
    /// </summary>
    public class GameState
    {
        public int Round { get; set; }
        public Character CurrentActor { get; set; }
        public TurnType CurrentTurnType { get; set; }
        public List<CombatStatus> ParticipantStatus { get; set; } = new List<CombatStatus>();
        public bool GameActive { get; set; }
        public Character Winner { get; set; }
        public DateTime StateTime { get; set; } = DateTime.Now;
        
        public override string ToString()
        {
            if (!GameActive && Winner != null)
                return $"GAME OVER - {Winner.Name} wins!";
            
            string turnInfo = CurrentTurnType == TurnType.CounterAttack ? " [COUNTER TURN]" : "";
            return $"Round {Round}: {CurrentActor?.Name}'s turn{turnInfo}";
        }
    }
    
    /// <summary>
    /// Combat status from Combat namespace (avoiding circular reference)
    /// </summary>
    public class CombatStatus
    {
        public string Name { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public int Stamina { get; set; }
        public int MaxStamina { get; set; }
        public int CounterGauge { get; set; }
        public int MaxCounter { get; set; }
        public bool CanAttack => Stamina >= 3;
        public bool CanDefend => Stamina >= 2;
        public bool CanMove => Stamina >= 1;
        public bool CounterReady => CounterGauge >= MaxCounter;
        
        public float HealthPercentage => MaxHealth > 0 ? (float)Health / MaxHealth : 0f;
        public float StaminaPercentage => MaxStamina > 0 ? (float)Stamina / MaxStamina : 0f;
        public float CounterPercentage => MaxCounter > 0 ? (float)CounterGauge / MaxCounter : 0f;
        
        public override string ToString()
        {
            string counter = CounterReady ? " [COUNTER READY!]" : $" (Counter: {CounterGauge}/{MaxCounter})";
            return $"{Name}: {Health}/{MaxHealth} HP, {Stamina}/{MaxStamina} SP{counter}";
        }
        
        /// <summary>
        /// Create status from Character object
        /// </summary>
        public static CombatStatus FromCharacter(Character character)
        {
            return new CombatStatus
            {
                Name = character.Name,
                Health = character.CurrentHealth,
                MaxHealth = character.MaxHealth,
                Stamina = character.CurrentStamina,
                MaxStamina = character.MaxStamina,
                CounterGauge = character.Counter.Current,
                MaxCounter = character.Counter.Maximum
            };
        }
    }
}