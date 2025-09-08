# RPG Combat Engine

[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![Build Status](https://img.shields.io/badge/build-passing-brightgreen.svg)](https://github.com/Happy-Gear/rpg-dnd-game)
[![Test Coverage](https://img.shields.io/badge/coverage-85%25-green.svg)](tests/)
[![Tests](https://img.shields.io/badge/tests-200%2B%20passing-brightgreen.svg)](tests/)

> A tactical turn-based RPG combat engine with dice-based mechanics, featuring the innovative "Badminton Streak" counter-attack system.

![Demo Screenshot](docs/images/gameplay-demo.png)

## 🎯 Quick Start

```bash
# Clone the repository
git clone https://github.com/Happy-Gear/rpg-dnd-game.git
cd rpg-dnd-game

# Build and run
dotnet build
dotnet run

# Run tests
dotnet test

# Or use convenience scripts
./build-and-run.sh    # Linux/macOS
.\build-and-run.bat   # Windows
```

**First Game Commands:**
```
attack B    # Attack character B (must be adjacent)
move        # Roll 1d6 movement (allows second action)
dash 8 10   # Roll 2d6 movement to position (8,10)
rest        # Recover stamina
defend      # Take defensive stance
```

## 🧪 Testing

### Run Tests
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test categories
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"

# Run specific test files
dotnet test --filter "FullyQualifiedName~CounterGaugeTests"
dotnet test --filter "FullyQualifiedName~DiceRollerTests"
```

### Test Coverage
- **Unit Tests**: 200+ tests covering core mechanics
- **Test Coverage**: 85%+ for critical systems
- **Test Infrastructure**: Custom builders and helpers for maintainable tests

### Test Organization
```
tests/
├── Unit/
│   ├── Characters/     # Character entity tests
│   ├── Combat/         # Combat system tests
│   ├── Core/          # Turn manager and game flow
│   ├── Dice/          # Randomization tests
│   └── Grid/          # Position and movement tests
├── Integration/       # End-to-end scenario tests
└── TestHelpers/      # Test utilities and builders
```

## 🎮 Core Features

### Combat System
- **Turn-Based Tactical Combat** with action economy management
- **Dice-Based Mechanics** using 1d6/2d6 rolls for all actions
- **Grid-Based Movement** on 16x16 tactical battlefield
- **Innovative Counter-Attack System** - "Badminton Streak" mechanics

### Action Types
| Action | Cost | Roll | Effect |
|--------|------|------|--------|
| **Attack** | 3 Stamina | 2d6 + ATK | Forces target response |
| **Defend** | 2 Stamina | 2d6 + DEF | Builds counter on over-defense |
| **Move** | 1 Stamina | 1d6 + MOV | Allows second action |
| **Dash** | 1 Stamina | 2d6 + MOV | Greater distance, ends turn |
| **Evasion** | 1 Stamina | 2d6 + MOV | Avoid damage, reposition |
| **Rest** | 0 Stamina | - | Restore 5 stamina |

### Character System
- **6-Stat System**: STR, END, CHA, INT, AGI, WIS
- **Derived Combat Values**: Attack, Defense, Movement points
- **Resource Management**: Health and Stamina pools
- **Counter Gauge**: Build up to unleash devastating counter-attacks

## 🏗️ Architecture

```
src/
├── Characters/     # Character entities and stats
├── Combat/         # Combat resolution and mechanics  
├── Core/          # Game state and orchestration
├── Dice/          # Randomization systems
├── Display/       # ASCII visualization
└── Grid/          # Spatial positioning
```

### Design Principles
- **Modular Architecture**: Loosely coupled, independently testable systems
- **Unity-Ready**: Clean separation between logic and presentation
- **Event-Driven**: Comprehensive logging and state notifications
- **Test-Driven Development**: 85%+ test coverage on core systems
- **Extensible**: Plugin architecture for new mechanics

## 🔧 Technical Stack

### Requirements
- **.NET 9.0 SDK** or later
- **NUnit 3.13+** for testing
- **Git** for version control
- **IDE**: Visual Studio, VS Code, or JetBrains Rider

### Development Setup
```bash
# Verify .NET installation
dotnet --version

# Restore dependencies
dotnet restore

# Run tests
dotnet test

# Generate coverage report
dotnet test --collect:"XPlat Code Coverage"
```

## 📖 Development Progress

### ✅ Phase 1: Foundation (COMPLETE)
- [x] Core character system with stats
- [x] Dice-based randomization
- [x] Grid positioning and movement
- [x] Basic combat mechanics
- [x] Counter gauge system
- [x] Turn management
- [x] ASCII display
- [x] **Comprehensive test suite (200+ tests)**

### 🚧 Phase 2: Enhanced Mechanics (IN PROGRESS)
- [x] Test infrastructure and helpers
- [x] Unit tests for all core systems
- [ ] Integration tests for combat flow
- [ ] Equipment system (weapons, armor)
- [ ] Status effects (buffs, debuffs)
- [ ] Area-of-effect abilities

### 📅 Future Phases
- **Phase 3**: Unity 3D Integration
- **Phase 4**: Multiplayer Support
- **Phase 5**: Platform Expansion (Steam, Mobile)

## 📊 Project Statistics

- **Lines of Code**: ~3,000 C# (including tests)
- **Test Coverage**: 85%+ for core systems
- **Test Count**: 200+ unit tests
- **Dependencies**: Zero external runtime dependencies
- **Test Framework**: NUnit 3.13
- **Platforms**: Windows, Linux, macOS

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) and [Coding Standards](CODING_STANDARDS.md).

### Quick Contribution Steps
1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feature/amazing-feature`)
3. **Follow** our coding standards
4. **Write tests** for new functionality (maintain 85%+ coverage)
5. **Run tests** (`dotnet test`)
6. **Submit** a pull request

### Areas We Need Help
- 🎨 **ASCII Art Design**: Character sprites and animations
- 🧭 **Directional System**: 72-direction implementation
- 📐 **Grid Expansion**: Large grid optimization
- 🎯 **Combat Mechanics**: Directional modifiers and vision
- 🔧 **Performance**: Grid rendering optimization
- 🧪 **Testing**: Directional combat scenarios
- 📚 **Documentation**: Visual system guides
- 🎮 **Game Design**: Balance for directional combat

## 📈 Recent Updates

### v0.2.0 - Test Infrastructure (Current)
- ✅ Complete test infrastructure with TestBase and builders
- ✅ 200+ unit tests covering all core systems
- ✅ Fixed CounterGauge negative value handling
- ✅ Character, Dice, Grid, and Combat test coverage
- ✅ Test helpers for maintainable test code

### v0.1.0 - Core Systems
- Initial combat engine implementation
- Turn-based combat with dice mechanics
- Badminton streak counter system
- ASCII grid display

## 🐛 Known Issues

- Warning CS2002 in test compilation (harmless, .NET artifact)
- Grid display may flicker on some terminals
- Movement pathfinding not yet implemented

## 📞 Community & Support

- **Issues**: [GitHub Issues](https://github.com/Happy-Gear/rpg-dnd-game/issues)
- **Discussions**: [GitHub Discussions](https://github.com/Happy-Gear/rpg-dnd-game/discussions)
- **Wiki**: [Project Wiki](https://github.com/Happy-Gear/rpg-dnd-game/wiki)

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🌟 Acknowledgments

- Inspired by classic D&D combat mechanics
- Built with modern software engineering practices
- Test-driven development philosophy
- Community-driven development

---

**[⭐ Star this repository](https://github.com/Happy-Gear/rpg-dnd-game)** if you find this project interesting!

### 🚀 Next Development Goals
- [ ] Complete integration tests for combat scenarios
- [ ] Implement equipment and inventory system
- [ ] Add status effects and conditions
- [ ] Create combat AI for NPCs
- [ ] Build scenario editor