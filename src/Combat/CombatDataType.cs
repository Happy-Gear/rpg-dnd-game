using System;
using RPGGame.Dice;

namespace RPGGame.Combat
{
    /// <summary>
    /// Result of an attack action
    /// </summary>
    public class AttackResult
    {
        public bool Success { get; set; }
        public string Attacker { get; set; }
        public string Defender { get; set; }
        public DiceResult AttackRoll { get; set; }
        public int BaseAttackDamage { get; set; }
        public bool IsCounterAttack { get; set; } = false;
        public string Message { get; set; }
        
        public override string ToString()
        {
            if (!Success) return Message;
            
            string prefix = IsCounterAttack ? "⚡ COUNTER: " : "";
            return $"{prefix}{Attacker} → {Defender}: {AttackRoll} = {BaseAttackDamage} damage";
        }
    }
    
    /// <summary>
    /// Result of a defense action
    /// </summary>
    public class DefenseResult
    {
        public DefenseChoice DefenseChoice { get; set; }
        public DiceResult DefenseRoll { get; set; }
        public int IncomingDamage { get; set; }
        public int TotalDefense { get; set; }
        public int DamageBlocked { get; set; }
        public int FinalDamage { get; set; }
        public int CounterBuilt { get; set; }
        public bool CounterReady { get; set; }
        public string Message { get; set; }
        
        public override string ToString()
        {
            return DefenseChoice switch
            {
                DefenseChoice.Defend => $"DEF: {DefenseRoll} = {TotalDefense} defense, blocked {DamageBlocked}, counter +{CounterBuilt}",
                DefenseChoice.Move => "MOVED: Evaded completely",
                DefenseChoice.TakeDamage => $"NO DEFENSE: Took {FinalDamage} damage",
                _ => Message
            };
        }
    }
    
    /// <summary>
    /// Complete combat round result
    /// </summary>
    public class CombatRoundResult
    {
        public AttackResult Attack { get; set; }
        public DefenseResult Defense { get; set; }
        public AttackResult CounterAttack { get; set; } // Optional badminton streak
        public string RoundSummary { get; set; }
        public DateTime RoundTime { get; set; } = DateTime.Now;
        
        public override string ToString()
        {
            string result = $"ROUND: {Attack}";
            if (Defense != null) result += $" | {Defense}";
            if (CounterAttack != null) result += $" | {CounterAttack}";
            return result;
        }
    }
    
    /// <summary>
    /// Player's choice when defending against an attack
    /// </summary>
    public enum DefenseChoice
    {
        Defend,      // Use 2 stamina, roll 2d6 + DEF, build counter on over-defense
        Move,        // Use 1 stamina, avoid damage completely, reset counter
        TakeDamage   // Save stamina, take full damage, reset counter
    }
    
    /// <summary>
    /// Types of combat actions for logging
    /// </summary>
    public enum CombatAction
    {
        Attack,
        Defend,
        Move,
        Rest,
        CounterAttack,
        Special
    }
    
    /// <summary>
    /// Combat log entry for history and debugging
    /// </summary>
    public class CombatLog
    {
        public CombatAction Action { get; set; }
        public string ActorName { get; set; }
        public string TargetName { get; set; }
        public DiceResult DiceRoll { get; set; }
        public int StaminaCost { get; set; }
        public string AdditionalInfo { get; set; }
        public DateTime Timestamp { get; set; }
        
        public override string ToString()
        {
            string target = !string.IsNullOrEmpty(TargetName) ? $" → {TargetName}" : "";
            string dice = DiceRoll != null ? $" [{DiceRoll}]" : "";
            string stamina = StaminaCost > 0 ? $" (-{StaminaCost} SP)" : "";
            string extra = !string.IsNullOrEmpty(AdditionalInfo) ? $" ({AdditionalInfo})" : "";
            
            return $"{ActorName}: {Action}{target}{dice}{stamina}{extra}";
        }
    }
    
    /// <summary>
    /// Combat participant status for UI/debugging
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
    }
}
