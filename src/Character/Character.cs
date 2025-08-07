using System;
using RPGGame.Grid;
using RPGGame.Combat;

namespace RPGGame.Characters
{
    /// <summary>
    /// Core character class representing a player or NPC in the game
    /// </summary>
    public class Character
    {
        public string Name { get; set; }
        public int Id { get; private set; }
        
        // Core Stats (Future: STR, END, CHA, INT, AGI, +1 more)
        public CharacterStats Stats { get; private set; }
        
        // Derived Combat Stats
        public int AttackPoints => CalculateAttack();
        public int DefensePoints => CalculateDefense();
        public int MovementPoints => CalculateMovement();
        
        // Combat Resources
        public int CurrentHealth { get; private set; }
        public int MaxHealth { get; private set; }
        public int CurrentStamina { get; private set; }
        public int MaxStamina { get; private set; }
        
        // Position on grid
        public Position Position { get; set; }
        
        // Counter system for "badminton streak"
        public CounterGauge Counter { get; private set; }
        
        // Constructor
        public Character(string name, CharacterStats stats, int health = 100, int stamina = 20)
        {
            Name = name;
            Id = GenerateId();
            Stats = stats;
            MaxHealth = CurrentHealth = health;
            MaxStamina = CurrentStamina = stamina;
            Position = new Position(0, 0); // Default position
            Counter = new CounterGauge();
        }
        
        // Combat stat calculations (placeholder formulas)
        private int CalculateAttack()
        {
            // Future: 3*STR + 2*END + AGI
            return Stats.Strength * 3 + Stats.Endurance * 2 + Stats.Agility;
        }
        
        private int CalculateDefense()
        {
            // Future: 2*END + STR + AGI
            return Stats.Endurance * 2 + Stats.Strength + Stats.Agility;
        }
        
        private int CalculateMovement()
        {
            // Future: 3*AGI + INT
            return Stats.Agility * 3 + Stats.Intelligence;
        }
        
        // Resource management
        public bool UseStamina(int amount)
        {
            if (CurrentStamina >= amount)
            {
                CurrentStamina -= amount;
                return true;
            }
            return false;
        }
        
        public void RestoreStamina(int amount)
        {
            CurrentStamina = Math.Min(MaxStamina, CurrentStamina + amount);
        }
        
        public void TakeDamage(int damage)
        {
            CurrentHealth = Math.Max(0, CurrentHealth - damage);
        }
        
        public void Heal(int amount)
        {
            CurrentHealth = Math.Min(MaxHealth, CurrentHealth + amount);
        }
        
        public bool IsAlive => CurrentHealth > 0;
        public bool CanAct => IsAlive && CurrentStamina > 0;
        
        // Utility methods
        private static int GenerateId()
        {
            return Math.Abs(Guid.NewGuid().GetHashCode());
        }
        
        public override string ToString()
        {
            return $"{Name} (HP: {CurrentHealth}/{MaxHealth}, SP: {CurrentStamina}/{MaxStamina})";
        }
    }
}
