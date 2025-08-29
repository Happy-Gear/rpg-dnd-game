using System;
using RPGGame.Characters;
using RPGGame.Grid;

namespace RPGGame.Tests.TestHelpers
{
    /// <summary>
    /// Test builder for Character objects using fluent interface pattern
    /// Following CODING_STANDARDS.md builder pattern guidelines
    /// Makes test data creation consistent, readable, and maintainable
    /// </summary>
    public class CharacterTestBuilder
    {
        private string _name = "TestCharacter";
        private CharacterStats _stats = new CharacterStats();
        private int _health = 10;
        private int _stamina = 10;
        private Position _position = new Position(0, 0);
        private int _counterGauge = 0;
        
        /// <summary>
        /// Set character name
        /// </summary>
        public CharacterTestBuilder WithName(string name)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            return this;
        }
        
        /// <summary>
        /// Set all character stats at once
        /// </summary>
        public CharacterTestBuilder WithStats(int str, int end, int cha, int intel, int agi, int wis)
        {
            _stats = new CharacterStats(str, end, cha, intel, agi, wis);
            return this;
        }
        
        /// <summary>
        /// Set individual stat - Strength (affects Attack Points)
        /// </summary>
        public CharacterTestBuilder WithStrength(int strength)
        {
            _stats.Strength = strength;
            return this;
        }
        
        /// <summary>
        /// Set individual stat - Endurance (affects Defense Points)
        /// </summary>
        public CharacterTestBuilder WithEndurance(int endurance)
        {
            _stats.Endurance = endurance;
            return this;
        }
        
        /// <summary>
        /// Set individual stat - Agility (affects Movement Points)
        /// </summary>
        public CharacterTestBuilder WithAgility(int agility)
        {
            _stats.Agility = agility;
            return this;
        }
        
        /// <summary>
        /// Set individual stat - Charisma
        /// </summary>
        public CharacterTestBuilder WithCharisma(int charisma)
        {
            _stats.Charisma = charisma;
            return this;
        }
        
        /// <summary>
        /// Set individual stat - Intelligence
        /// </summary>
        public CharacterTestBuilder WithIntelligence(int intelligence)
        {
            _stats.Intelligence = intelligence;
            return this;
        }
        
        /// <summary>
        /// Set individual stat - Wisdom
        /// </summary>
        public CharacterTestBuilder WithWisdom(int wisdom)
        {
            _stats.Wisdom = wisdom;
            return this;
        }
        
        /// <summary>
        /// Set health values (current = max by default)
        /// </summary>
        public CharacterTestBuilder WithHealth(int maxHealth, int? currentHealth = null)
        {
            if (maxHealth <= 0)
                throw new ArgumentException("Health must be positive", nameof(maxHealth));
                
            _health = maxHealth;
            
            // If current health not specified, set it to max
            // We'll modify current health after character creation if needed
            return this;
        }
        
        /// <summary>
        /// Set stamina values (current = max by default)
        /// </summary>
        public CharacterTestBuilder WithStamina(int maxStamina, int? currentStamina = null)
        {
            if (maxStamina <= 0)
                throw new ArgumentException("Stamina must be positive", nameof(maxStamina));
                
            _stamina = maxStamina;
            
            // If current stamina not specified, set it to max
            // We'll modify current stamina after character creation if needed
            return this;
        }
        
        /// <summary>
        /// Set character position on grid
        /// </summary>
        public CharacterTestBuilder WithPosition(int x, int y, int z = 0)
        {
            _position = new Position(x, y, z);
            return this;
        }
        
        /// <summary>
        /// Set character position using Position object
        /// </summary>
        public CharacterTestBuilder WithPosition(Position position)
        {
            _position = position ?? throw new ArgumentNullException(nameof(position));
            return this;
        }
        
        /// <summary>
        /// Set counter gauge level (for badminton streak testing)
        /// </summary>
        public CharacterTestBuilder WithCounter(int counterLevel)
        {
            if (counterLevel < 0)
                throw new ArgumentException("Counter level cannot be negative", nameof(counterLevel));
                
            _counterGauge = counterLevel;
            return this;
        }
        
        /// <summary>
        /// Set counter gauge to ready state (6 points)
        /// </summary>
        public CharacterTestBuilder WithCounterReady()
        {
            _counterGauge = 6; // Maximum counter level
            return this;
        }
        
        /// <summary>
        /// Build the Character object with current configuration
        /// </summary>
        public Character Build()
        {
            var character = new Character(_name, _stats, _health, _stamina);
            
            // Set position
            character.Position = _position;
            
            // Set counter gauge level if specified
            if (_counterGauge > 0)
            {
                character.Counter.AddCounter(_counterGauge);
            }
            
            return character;
        }
        
        /// <summary>
        /// Build character with modified current health (different from max)
        /// </summary>
        public Character BuildWithCurrentHealth(int currentHealth)
        {
            var character = Build();
            
            if (currentHealth > _health)
            {
                character.Heal(currentHealth - _health);
            }
            else if (currentHealth < _health)
            {
                character.TakeDamage(_health - currentHealth);
            }
            
            return character;
        }
        
        /// <summary>
        /// Build character with modified current stamina (different from max)
        /// </summary>
        public Character BuildWithCurrentStamina(int currentStamina)
        {
            var character = Build();
            
            if (currentStamina > _stamina)
            {
                character.RestoreStamina(currentStamina - _stamina);
            }
            else if (currentStamina < _stamina)
            {
                character.UseStamina(_stamina - currentStamina);
            }
            
            return character;
        }
        
        /// <summary>
        /// Build character with both current health and stamina different from max
        /// </summary>
        public Character BuildWithCurrentResources(int currentHealth, int currentStamina)
        {
            var character = Build();
            
            // Adjust health
            if (currentHealth > _health)
            {
                character.Heal(currentHealth - _health);
            }
            else if (currentHealth < _health)
            {
                character.TakeDamage(_health - currentHealth);
            }
            
            // Adjust stamina
            if (currentStamina > _stamina)
            {
                character.RestoreStamina(currentStamina - _stamina);
            }
            else if (currentStamina < _stamina)
            {
                character.UseStamina(_stamina - currentStamina);
            }
            
            return character;
        }
    }
    
    /// <summary>
    /// Pre-configured character builders for common test scenarios
    /// </summary>
    public static class CommonCharacters
    {
        /// <summary>
        /// Alice - Balanced attacker (from original game)
        /// ATK: 1, DEF: 0, MOV: 1
        /// </summary>
        public static CharacterTestBuilder Alice()
        {
            return new CharacterTestBuilder()
                .WithName("Alice")
                .WithStats(str: 1, end: 0, cha: 5, intel: 5, agi: 1, wis: 5)
                .WithPosition(2, 2);
        }
        
        /// <summary>
        /// Bob - Strong attacker (from original game)
        /// ATK: 2, DEF: 0, MOV: 0
        /// </summary>
        public static CharacterTestBuilder Bob()
        {
            return new CharacterTestBuilder()
                .WithName("Bob")
                .WithStats(str: 2, end: 0, cha: 5, intel: 5, agi: 0, wis: 5)
                .WithPosition(5, 5);
        }
        
        /// <summary>
        /// Tank - High defense character
        /// ATK: 0, DEF: 3, MOV: 0
        /// </summary>
        public static CharacterTestBuilder Tank()
        {
            return new CharacterTestBuilder()
                .WithName("Tank")
                .WithStats(str: 0, end: 3, cha: 5, intel: 5, agi: 0, wis: 5)
                .WithHealth(20)
                .WithPosition(8, 8);
        }
        
        /// <summary>
        /// Scout - High mobility character
        /// ATK: 0, DEF: 0, MOV: 3
        /// </summary>
        public static CharacterTestBuilder Scout()
        {
            return new CharacterTestBuilder()
                .WithName("Scout")
                .WithStats(str: 0, end: 0, cha: 5, intel: 5, agi: 3, wis: 5)
                .WithPosition(1, 1);
        }
        
        /// <summary>
        /// Glass Cannon - High attack, low defense
        /// ATK: 5, DEF: 0, MOV: 1
        /// </summary>
        public static CharacterTestBuilder GlassCannon()
        {
            return new CharacterTestBuilder()
                .WithName("GlassCannon")
                .WithStats(str: 5, end: 0, cha: 5, intel: 5, agi: 1, wis: 5)
                .WithHealth(5)
                .WithPosition(0, 0);
        }
        
        /// <summary>
        /// Weakened character (low resources for testing edge cases)
        /// </summary>
        public static CharacterTestBuilder Weakened()
        {
            return new CharacterTestBuilder()
                .WithName("Weakened")
                .WithStats(str: 1, end: 1, cha: 1, intel: 1, agi: 1, wis: 1)
                .WithHealth(3)
                .WithStamina(2);
        }
        
        /// <summary>
        /// Counter-ready character (for badminton streak testing)
        /// </summary>
        public static CharacterTestBuilder CounterReady()
        {
            return new CharacterTestBuilder()
                .WithName("CounterReady")
                .WithStats(str: 2, end: 2, cha: 5, intel: 5, agi: 1, wis: 5)
                .WithCounterReady();
        }
        
        /// <summary>
        /// Minimal character (all stats at minimum for boundary testing)
        /// </summary>
        public static CharacterTestBuilder Minimal()
        {
            return new CharacterTestBuilder()
                .WithName("Minimal")
                .WithStats(str: 0, end: 0, cha: 0, intel: 0, agi: 0, wis: 0)
                .WithHealth(1)
                .WithStamina(1);
        }
    }
}