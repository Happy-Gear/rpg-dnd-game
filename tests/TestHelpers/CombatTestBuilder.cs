using System;
using System.Collections.Generic;
using RPGGame.Characters;
using RPGGame.Combat;
using RPGGame.Dice;
using RPGGame.Grid;

namespace RPGGame.Tests.TestHelpers
{
    /// <summary>
    /// Test builder for creating combat scenarios and setups
    /// Provides fluent interface for complex combat test arrangements
    /// Following CODING_STANDARDS.md builder pattern guidelines
    /// </summary>
    public class CombatTestBuilder
    {
        private CombatSystem _combatSystem;
        private Character _attacker;
        private Character _defender;
        private int? _seed;
        private bool _deterministic = true;
        
        public CombatTestBuilder()
        {
            // Default to deterministic for predictable tests
            _seed = 42;
        }
        
        /// <summary>
        /// Use deterministic dice (seeded) for predictable test results
        /// </summary>
        public CombatTestBuilder WithDeterministicDice(int seed = 42)
        {
            _seed = seed;
            _deterministic = true;
            return this;
        }
        
        /// <summary>
        /// Use random dice for statistical testing
        /// </summary>
        public CombatTestBuilder WithRandomDice()
        {
            _seed = null;
            _deterministic = false;
            return this;
        }
        
        /// <summary>
        /// Set the attacking character
        /// </summary>
        public CombatTestBuilder WithAttacker(Character attacker)
        {
            _attacker = attacker ?? throw new ArgumentNullException(nameof(attacker));
            return this;
        }
        
        /// <summary>
        /// Set the defending character
        /// </summary>
        public CombatTestBuilder WithDefender(Character defender)
        {
            _defender = defender ?? throw new ArgumentNullException(nameof(defender));
            return this;
        }
        
        /// <summary>
        /// Create attacker using builder pattern
        /// </summary>
        public CombatTestBuilder WithAttacker(Action<CharacterTestBuilder> builderAction)
        {
            var builder = new CharacterTestBuilder();
            builderAction(builder);
            _attacker = builder.Build();
            return this;
        }
        
        /// <summary>
        /// Create defender using builder pattern
        /// </summary>
        public CombatTestBuilder WithDefender(Action<CharacterTestBuilder> builderAction)
        {
            var builder = new CharacterTestBuilder();
            builderAction(builder);
            _defender = builder.Build();
            return this;
        }
        
        /// <summary>
        /// Build the combat system with configured settings
        /// </summary>
        public CombatSystem BuildCombatSystem()
        {
            if (_combatSystem == null)
            {
                _combatSystem = new CombatSystem(_seed);
            }
            return _combatSystem;
        }
        
        /// <summary>
        /// Build a complete combat scenario ready for testing
        /// </summary>
        public CombatScenario Build()
        {
            // Ensure we have characters
            if (_attacker == null)
            {
                _attacker = CommonCharacters.Alice().Build();
            }
            
            if (_defender == null)
            {
                _defender = CommonCharacters.Bob().Build();
            }
            
            // Build combat system
            var combatSystem = BuildCombatSystem();
            
            return new CombatScenario
            {
                CombatSystem = combatSystem,
                Attacker = _attacker,
                Defender = _defender,
                IsDeterministic = _deterministic,
                Seed = _seed
            };
        }
        
        /// <summary>
        /// Execute an attack scenario and return results
        /// </summary>
        public AttackResult ExecuteAttack()
        {
            var scenario = Build();
            return scenario.CombatSystem.ExecuteAttack(scenario.Attacker, scenario.Defender);
        }
        
        /// <summary>
        /// Execute a full attack-defense sequence
        /// </summary>
        public CombatSequenceResult ExecuteAttackDefenseSequence(DefenseChoice defenseChoice = DefenseChoice.Defend)
        {
            var scenario = Build();
            
            var attackResult = scenario.CombatSystem.ExecuteAttack(scenario.Attacker, scenario.Defender);
            DefenseResult defenseResult = null;
            AttackResult counterResult = null;
            
            if (attackResult.Success)
            {
                defenseResult = scenario.CombatSystem.ResolveDefense(scenario.Defender, attackResult, defenseChoice);
                
                // Check for counter attack if defender is ready
                if (defenseResult.CounterReady && scenario.Defender.Counter.IsReady)
                {
                    counterResult = scenario.CombatSystem.ExecuteCounterAttack(scenario.Defender, scenario.Attacker);
                }
            }
            
            return new CombatSequenceResult
            {
                Attack = attackResult,
                Defense = defenseResult,
                CounterAttack = counterResult,
                Scenario = scenario
            };
        }
    }
    
    /// <summary>
    /// Represents a configured combat scenario for testing
    /// </summary>
    public class CombatScenario
    {
        public CombatSystem CombatSystem { get; set; }
        public Character Attacker { get; set; }
        public Character Defender { get; set; }
        public bool IsDeterministic { get; set; }
        public int? Seed { get; set; }
        
        /// <summary>
        /// Get initial state snapshot
        /// </summary>
        public CombatSnapshot GetInitialState()
        {
            return new CombatSnapshot
            {
                AttackerHealth = Attacker.CurrentHealth,
                AttackerStamina = Attacker.CurrentStamina,
                AttackerCounter = Attacker.Counter.Current,
                DefenderHealth = Defender.CurrentHealth,
                DefenderStamina = Defender.CurrentStamina,
                DefenderCounter = Defender.Counter.Current
            };
        }
    }
    
    /// <summary>
    /// Results of a complete combat sequence (attack -> defense -> possible counter)
    /// </summary>
    public class CombatSequenceResult
    {
        public AttackResult Attack { get; set; }
        public DefenseResult Defense { get; set; }
        public AttackResult CounterAttack { get; set; }
        public CombatScenario Scenario { get; set; }
        
        public bool AttackSucceeded => Attack?.Success == true;
        public bool DefenseOccurred => Defense != null;
        public bool CounterOccurred => CounterAttack?.Success == true;
        
        /// <summary>
        /// Get final state after sequence
        /// </summary>
        public CombatSnapshot GetFinalState()
        {
            return new CombatSnapshot
            {
                AttackerHealth = Scenario.Attacker.CurrentHealth,
                AttackerStamina = Scenario.Attacker.CurrentStamina,
                AttackerCounter = Scenario.Attacker.Counter.Current,
                DefenderHealth = Scenario.Defender.CurrentHealth,
                DefenderStamina = Scenario.Defender.CurrentStamina,
                DefenderCounter = Scenario.Defender.Counter.Current
            };
        }
        
        public override string ToString()
        {
            var parts = new List<string>();
            
            if (AttackSucceeded)
                parts.Add($"ATK: {Attack.BaseAttackDamage} dmg");
            
            if (DefenseOccurred)
                parts.Add($"DEF: {Defense.DefenseChoice}");
                
            if (CounterOccurred)
                parts.Add($"COUNTER: {CounterAttack.BaseAttackDamage} dmg");
            
            return string.Join(" → ", parts);
        }
    }
    
    /// <summary>
    /// Snapshot of character states for before/after comparisons
    /// </summary>
    public class CombatSnapshot
    {
        public int AttackerHealth { get; set; }
        public int AttackerStamina { get; set; }
        public int AttackerCounter { get; set; }
        public int DefenderHealth { get; set; }
        public int DefenderStamina { get; set; }
        public int DefenderCounter { get; set; }
        
        public int TotalDamageToAttacker(CombatSnapshot before)
        {
            return before.AttackerHealth - AttackerHealth;
        }
        
        public int TotalDamageToDefender(CombatSnapshot before)
        {
            return before.DefenderHealth - DefenderHealth;
        }
        
        public int AttackerStaminaUsed(CombatSnapshot before)
        {
            return before.AttackerStamina - AttackerStamina;
        }
        
        public int DefenderStaminaUsed(CombatSnapshot before)
        {
            return before.DefenderStamina - DefenderStamina;
        }
    }
    
    /// <summary>
    /// Pre-configured combat scenarios for common test cases
    /// </summary>
    public static class CommonCombatScenarios
    {
        /// <summary>
        /// Basic attacker vs defender (Alice vs Bob)
        /// </summary>
        public static CombatTestBuilder BasicAttack()
        {
            return new CombatTestBuilder()
                .WithAttacker(CommonCharacters.Alice().Build())
                .WithDefender(CommonCharacters.Bob().Build())
                .WithDeterministicDice();
        }
        
        /// <summary>
        /// Strong attacker vs tank defender
        /// </summary>
        public static CombatTestBuilder PowerVsTank()
        {
            return new CombatTestBuilder()
                .WithAttacker(CommonCharacters.GlassCannon().Build())
                .WithDefender(CommonCharacters.Tank().Build())
                .WithDeterministicDice();
        }
        
        /// <summary>
        /// Counter-ready defender scenario
        /// </summary>
        public static CombatTestBuilder CounterAttackScenario()
        {
            return new CombatTestBuilder()
                .WithAttacker(CommonCharacters.Alice().Build())
                .WithDefender(CommonCharacters.CounterReady().Build())
                .WithDeterministicDice();
        }
        
        /// <summary>
        /// Low stamina stress test
        /// </summary>
        public static CombatTestBuilder LowStaminaStress()
        {
            return new CombatTestBuilder()
                .WithAttacker(builder => builder
                    .WithName("LowStaminaAttacker")
                    .WithStamina(5)
                    .BuildWithCurrentStamina(3))
                .WithDefender(builder => builder
                    .WithName("LowStaminaDefender")
                    .WithStamina(5)
                    .BuildWithCurrentStamina(2))
                .WithDeterministicDice();
        }
        
        /// <summary>
        /// Nearly dead characters (edge case testing)
        /// </summary>
        public static CombatTestBuilder NearDeathScenario()
        {
            return new CombatTestBuilder()
                .WithAttacker(builder => builder
                    .WithName("NearDeathAttacker")
                    .WithHealth(10)
                    .BuildWithCurrentHealth(2))
                .WithDefender(builder => builder
                    .WithName("NearDeathDefender")
                    .WithHealth(10)
                    .BuildWithCurrentHealth(1))
                .WithDeterministicDice();
        }
    }
}