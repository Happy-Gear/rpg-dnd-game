# Coding Standards & Best Practices

> Comprehensive coding guidelines for the RPG Combat Engine project

## 📋 Table of Contents

- [Philosophy](#philosophy)
- [Code Organization](#code-organization)
- [Naming Conventions](#naming-conventions)
- [Documentation](#documentation)
- [Architecture Patterns](#architecture-patterns)
- [Testing Standards](#testing-standards)
- [Git Workflow](#git-workflow)
- [Performance Guidelines](#performance-guidelines)
- [Security Practices](#security-practices)

## 🎯 Philosophy

### Core Principles
1. **Clarity over Cleverness**: Write code that tells a story
2. **Consistency**: Follow established patterns throughout the codebase
3. **Modularity**: Build loosely coupled, highly cohesive components
4. **Testability**: Design for easy unit and integration testing
5. **Future-Proofing**: Consider Unity integration and multiplayer expansion

### Design Values
- **SOLID Principles**: Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, Dependency Inversion
- **Clean Architecture**: Clear separation of concerns between layers
- **Domain-Driven Design**: Model the game domain accurately in code

## 🗂️ Code Organization

### Project Structure
```
src/
├── Characters/          # Character entities and stat management
├── Combat/             # Combat resolution and mechanics
├── Core/               # Game state, turns, and orchestration  
├── Dice/               # Randomization and probability systems
├── Display/            # ASCII visualization and UI
└── Grid/               # Spatial positioning and movement

tests/
├── Unit/               # Unit tests by component
├── Integration/        # Integration and system tests
└── Performance/        # Performance benchmarks
```

### File Organization Rules
1. **One class per file** (except for small, tightly related classes)
2. **Match namespace to folder structure**: `RPGGame.Combat` → `src/Combat/`
3. **Group related functionality** in the same namespace
4. **Use descriptive file names** that match the primary class

### Namespace Conventions
```csharp
// Root namespace
namespace RPGGame

// Feature namespaces
namespace RPGGame.Characters
namespace RPGGame.Combat
namespace RPGGame.Core
namespace RPGGame.Dice
namespace RPGGame.Display
namespace RPGGame.Grid

// Test namespaces mirror source
namespace RPGGame.Tests.Unit.Combat
namespace RPGGame.Tests.Integration.Core
```

## 🏷️ Naming Conventions

### General Rules
- **Use descriptive names** that explain intent
- **Avoid abbreviations** except for well-known terms (HP, ATK, DEF)
- **Prefer clarity over brevity**
- **Use consistent terminology** throughout the codebase

### Specific Conventions

#### Classes and Interfaces
```csharp
// Classes: PascalCase, noun phrases
public class CombatSystem
public class CharacterStats
public class GridDisplay

// Interfaces: PascalCase with 'I' prefix
public interface ICombatSystem
public interface ICharacterRepository

// Abstract classes: PascalCase with 'Base' suffix
public abstract class BaseCharacter
```

#### Methods and Properties
```csharp
// Methods: PascalCase, verb phrases
public void ExecuteAttack(Character attacker, Character defender)
public bool CanAttack(Character character)
public AttackResult CalculateDamage(int attackValue, int defenseValue)

// Properties: PascalCase, noun phrases
public int CurrentHealth { get; set; }
public bool IsAlive => CurrentHealth > 0;
public string Name { get; private set; }

// Boolean properties/methods: Start with Is/Can/Has/Should
public bool IsReady { get; }
public bool CanAct => IsAlive && CurrentStamina > 0;
public bool HasCounter => Counter.Current > 0;
```

#### Variables and Fields
```csharp
// Local variables: camelCase
var attackResult = combatSystem.ExecuteAttack(attacker, target);
var validPositions = GetPositionsWithinDistance(position, distance);

// Private fields: camelCase with underscore prefix
private readonly CombatSystem _combatSystem;
private List<Character> _participants;
private bool _waitingForInput;

// Constants: PascalCase or UPPER_CASE for traditional constants
private const int MAX_COUNTER = 6;
private const string DEFAULT_CHARACTER_NAME = "Unknown";
```

#### Enums
```csharp
// Enum types: PascalCase singular
public enum ActionChoice
public enum DefenseChoice
public enum MovementType

// Enum values: PascalCase
public enum ActionChoice
{
    Attack,
    Defend,
    Move,
    Rest
}
```

#### Events and Delegates
```csharp
// Events: PascalCase with descriptive names
public event EventHandler<AttackEventArgs> AttackExecuted;
public event EventHandler<CharacterEventArgs> CharacterDefeated;

// Event handlers: On[EventName] pattern
protected virtual void OnAttackExecuted(AttackEventArgs e)
```

## 📚 Documentation

### XML Documentation
All public APIs must have XML documentation:

```csharp
/// <summary>
/// Executes an attack action between two characters with dice resolution
/// </summary>
/// <param name="attacker">The character performing the attack</param>
/// <param name="defender">The character being targeted</param>
/// <returns>Result containing attack success, damage, and dice rolls</returns>
/// <exception cref="ArgumentNullException">Thrown when attacker or defender is null</exception>
public AttackResult ExecuteAttack(Character attacker, Character defender)
{
    // Implementation
}
```

### Code Comments
```csharp
// Good: Explain WHY, not WHAT
// Counter attacks bypass defense (immediate damage)
target.TakeDamage(totalDamage);

// Bad: Explaining obvious code
// Add totalDamage to target
target.TakeDamage(totalDamage);

// Good: Complex business logic explanation
// Over-defense builds counter gauge for "badminton streak" mechanic
// This encourages tactical defense timing
int overDefense = Math.Max(0, totalDefense - incomingAttack.BaseAttackDamage);
if (overDefense > 0)
{
    defender.Counter.AddCounter(overDefense);
}
```

### README Updates
When adding features:
1. Update feature list in README
2. Add to appropriate roadmap phase
3. Include usage examples
4. Update architecture diagram if needed

## 🏛️ Architecture Patterns

### Domain Model Guidelines
```csharp
// Rich domain objects with behavior
public class Character
{
    // Encapsulate business rules
    public bool CanAttack => IsAlive && CurrentStamina >= 3;
    
    // Domain methods, not just data holders
    public bool UseStamina(int amount)
    {
        if (CurrentStamina >= amount)
        {
            CurrentStamina -= amount;
            return true;
        }
        return false;
    }
}

// Avoid anemic domain models
public class BadCharacter
{
    public int Health { get; set; }  // Just data, no behavior
    public int Stamina { get; set; }
}
```

### Service Layer Pattern
```csharp
// Stateless services that orchestrate domain objects
public class CombatSystem
{
    private readonly DiceRoller _diceRoller;
    
    public CombatSystem(DiceRoller diceRoller)
    {
        _diceRoller = diceRoller ?? throw new ArgumentNullException(nameof(diceRoller));
    }
    
    public AttackResult ExecuteAttack(Character attacker, Character defender)
    {
        // Orchestrate the domain logic
        if (!CanAttack(attacker))
            return new AttackResult { Success = false, Message = "Cannot attack" };
            
        // Use injected dependencies
        var attackRoll = _diceRoller.Roll2d6("ATK");
        
        // Return value objects
        return new AttackResult
        {
            Success = true,
            AttackRoll = attackRoll,
            // ... other properties
        };
    }
}
```

### Value Objects
```csharp
// Immutable value objects for data that doesn't have identity
public class Position
{
    public int X { get; }
    public int Y { get; }
    public int Z { get; }
    
    public Position(int x, int y, int z = 0)
    {
        X = x;
        Y = y;
        Z = z;
    }
    
    // Implement equality properly
    public override bool Equals(object obj) => /* implementation */;
    public override int GetHashCode() => /* implementation */;
    
    // Value objects can have behavior
    public double DistanceTo(Position other) => /* implementation */;
}
```

### Error Handling
```csharp
// Use result patterns for expected failures
public class AttackResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    // ... other properties
}

// Throw exceptions for unexpected errors
public void ExecuteAttack(Character attacker, Character defender)
{
    if (attacker == null) 
        throw new ArgumentNullException(nameof(attacker));
        
    // Expected failures return false/messages
    if (!attacker.CanAttack)
        return new AttackResult { Success = false, Message = "Cannot attack" };
}

// Document exceptions in XML comments
/// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
/// <exception cref="InvalidOperationException">Thrown when game state is invalid</exception>
```

## 🧪 Testing Standards

### Test Organization
```csharp
// Test class naming: [ClassUnderTest]Tests
public class CombatSystemTests
{
    // Test method naming: Should_[ExpectedBehavior]_When_[Condition]
    [Test]
    public void Should_ReturnSuccessfulAttack_When_AttackerHasSufficientStamina()
    {
        // Arrange
        var attacker = CreateCharacterWithStamina(5);
        var defender = CreateCharacterWithHealth(10);
        var combatSystem = new CombatSystem(new DiceRoller(seed: 42)); // Deterministic
        
        // Act
        var result = combatSystem.ExecuteAttack(attacker, defender);
        
        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Attacker, Is.EqualTo(attacker.Name));
        Assert.That(attacker.CurrentStamina, Is.EqualTo(2)); // 5 - 3 = 2
    }
}
```

### Test Categories
```csharp
[Test]
[Category("Unit")]
public void Should_CalculateCorrectDamage_When_AttackSucceeds() { }

[Test]
[Category("Integration")]
public void Should_HandleCompleteAttackDefenseSequence() { }

[Test]
[Category("Performance")]
public void Should_ProcessCombatRoundUnder10Milliseconds() { }
```

### Test Data Builders
```csharp
// Use builder pattern for test data
public class CharacterBuilder
{
    private string _name = "TestCharacter";
    private int _health = 10;
    private int _stamina = 10;
    private CharacterStats _stats = new CharacterStats();
    
    public CharacterBuilder WithName(string name)
    {
        _name = name;
        return this;
    }
    
    public CharacterBuilder WithStamina(int stamina)
    {
        _stamina = stamina;
        return this;
    }
    
    public Character Build() => new Character(_name, _stats, _health, _stamina);
}

// Usage in tests
var attacker = new CharacterBuilder()
    .WithName("Alice")
    .WithStamina(5)
    .Build();
```

## 🌿 Git Workflow

### Branch Naming
```
feature/counter-attack-system
bugfix/movement-validation-error
hotfix/critical-combat-bug
refactor/combat-system-cleanup
docs/api-documentation-update
```

### Commit Messages
Follow conventional commits:
```
feat: add badminton streak counter-attack system

- Implement CounterGauge class with fill/consume mechanics
- Add counter building on over-defense
- Enable counter attacks in combat system
- Update UI to display counter status

Closes #123
```

### Commit Types
- `feat:` New feature
- `fix:` Bug fix  
- `docs:` Documentation changes
- `style:` Formatting changes
- `refactor:` Code refactoring
- `test:` Adding or updating tests
- `chore:` Maintenance tasks

### Pull Request Guidelines
1. **Branch from main**: Create feature branches from latest main
2. **Single responsibility**: One feature/fix per PR
3. **Tests included**: Add tests for new functionality
4. **Documentation updated**: Update README/docs as needed
5. **Descriptive title**: Summarize the change clearly
6. **Linked issues**: Reference related issues

## ⚡ Performance Guidelines

### Memory Management
```csharp
// Prefer immutable value objects
public readonly struct DiceResult
{
    public int Die1 { get; }
    public int Die2 { get; }
    public int Total { get; }
}

// Reuse collections where possible
private readonly List<Character> _characters = new List<Character>();

public void UpdateCharacters(IEnumerable<Character> newCharacters)
{
    _characters.Clear();
    _characters.AddRange(newCharacters.Where(c => c.IsAlive));
}

// Use object pools for frequently created objects
private readonly Queue<AttackResult> _attackResultPool = new();
```

### LINQ Usage
```csharp
// Good: Efficient LINQ usage
var livingPlayers = _players.Where(p => p.IsAlive).ToList();
var hasCounterReady = _players.Any(p => p.Counter.IsReady);

// Avoid: Multiple enumeration
var aliveCharacters = characters.Where(c => c.IsAlive);
var count = aliveCharacters.Count(); // First enumeration
var first = aliveCharacters.First(); // Second enumeration

// Better: Single enumeration
var aliveCharacters = characters.Where(c => c.IsAlive).ToList();
var count = aliveCharacters.Count;
var first = aliveCharacters.First();
```

### Async Guidelines
```csharp
// Use async/await for I/O operations (future multiplayer)
public async Task<AttackResult> ExecuteAttackAsync(Character attacker, Character defender)
{
    // Network validation for multiplayer
    await ValidateWithServerAsync(attacker, defender);
    
    // Local computation remains synchronous
    return ExecuteAttackLogic(attacker, defender);
}

// Avoid async void except for event handlers
public async void OnAttackButtonClicked(object sender, EventArgs e) // OK for events
{
    await ExecuteAttackAsync(attacker, defender);
}
```

## 🔒 Security Practices

### Input Validation
```csharp
public AttackResult ExecuteAttack(Character attacker, Character defender)
{
    // Validate all inputs
    if (attacker == null)
        throw new ArgumentNullException(nameof(attacker));
    if (defender == null)
        throw new ArgumentNullException(nameof(defender));
        
    // Validate business rules
    if (!attacker.IsAlive)
        return AttackResult.Failed("Attacker is not alive");
    if (!defender.IsAlive)
        return AttackResult.Failed("Defender is not alive");
        
    // Continue with logic...
}
```

### Deterministic Random
```csharp
// Provide seeded random for testing/reproducibility
public class DiceRoller
{
    private readonly Random _random;
    
    public DiceRoller(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }
}

// Usage
var deterministicRoller = new DiceRoller(seed: 42); // For tests
var randomRoller = new DiceRoller(); // For gameplay
```

## 🔍 Code Review Checklist

### Before Submitting
- [ ] Code follows naming conventions
- [ ] All public APIs have XML documentation
- [ ] Unit tests cover new functionality
- [ ] No hardcoded values (use constants/configuration)
- [ ] Error handling is appropriate
- [ ] Performance considerations addressed
- [ ] No security vulnerabilities introduced

### Reviewer Checklist
- [ ] Code is readable and maintainable
- [ ] Architecture patterns are followed
- [ ] Tests are meaningful and comprehensive
- [ ] Documentation is accurate and helpful
- [ ] Performance impact is acceptable
- [ ] Security best practices followed

## 📏 Metrics & Quality Gates

### Code Quality Targets
- **Test Coverage**: >85% for core logic
- **Cyclomatic Complexity**: <10 per method
- **Method Length**: <50 lines typical, <100 max
- **Class Length**: <500 lines typical
- **Dependency Count**: <7 dependencies per class

### Performance Targets
- **Turn Processing**: <10ms average
- **Combat Resolution**: <5ms per action
- **Memory Usage**: <50MB for typical games
- **Startup Time**: <2 seconds cold start

## 🚀 Continuous Improvement

### Regular Reviews
- **Weekly**: Code review standards adherence
- **Monthly**: Architecture pattern effectiveness  
- **Quarterly**: Performance and quality metrics review
- **Annually**: Major refactoring opportunities

### Learning Resources
- [Clean Code by Robert Martin](https://www.oreilly.com/library/view/clean-code-a/9780136083238/)
- [Domain-Driven Design by Eric Evans](https://www.domainlanguage.com/ddd/)
- [.NET Performance Best Practices](https://docs.microsoft.com/en-us/dotnet/framework/performance/)

---

**Remember**: These standards evolve with the project. Suggest improvements through issues or discussions!