# RPG Combat Engine

[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![Build Status](https://img.shields.io/badge/build-passing-brightgreen.svg)](https://github.com/Happy-Gear/rpg-dnd-game)
[![Contributions Welcome](https://img.shields.io/badge/contributions-welcome-brightgreen.svg)](CONTRIBUTING.md)

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
- **Extensible**: Plugin architecture for new mechanics

## 🔧 Technical Requirements

### Prerequisites
- **.NET 9.0 SDK** or later
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

## 📖 Gameplay Guide

### Basic Combat Flow
1. **Turn Order**: Characters act in sequence
2. **Action Selection**: Choose from available actions based on stamina
3. **Dice Resolution**: Roll dice + modifiers for action results
4. **Defense Response**: Target chooses how to respond to attacks
5. **Counter Opportunities**: Build counter gauge through over-defense

### Advanced Tactics
- **Movement Positioning**: Control distance and adjacency
- **Stamina Management**: Balance offense with sustainability  
- **Counter Timing**: Build and unleash "Badminton Streak" attacks
- **Evasion Tactics**: Use movement to avoid damage and reposition

### Example Combat Round
```
Alice attacks Bob: [4+2] + 1 ATK = 7 damage
Bob chooses to defend: [3+5] + 0 DEF = 8 defense
Result: Bob blocks 7 damage, builds +1 counter gauge
```

## 🗺️ Roadmap

### Phase 1: Enhanced Mechanics *(In Progress)*
- [ ] Equipment system (weapons, armor, accessories)
- [ ] Status effects (poison, buffs, debuffs)
- [ ] Area-of-effect abilities
- [ ] Character progression and skill trees

### Phase 2: Visual Upgrade *(Q2 2025)*
- [ ] Unity 3D integration
- [ ] 2.5D isometric graphics
- [ ] Particle effects and animations
- [ ] Enhanced UI/UX

### Phase 3: Multiplayer *(Q3 2025)*
- [ ] Network architecture
- [ ] Real-time synchronization
- [ ] Spectator mode
- [ ] Tournament systems

### Phase 4: Platform Expansion *(Q4 2025)*
- [ ] Steam integration
- [ ] Mobile platforms
- [ ] Console ports
- [ ] Mod support

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) and [Coding Standards](CODING_STANDARDS.md).

### Quick Contribution Steps
1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feature/amazing-feature`)
3. **Follow** our coding standards
4. **Add tests** for new functionality
5. **Submit** a pull request

### Areas We Need Help
- 🎨 **Game Design**: New mechanics and balance
- 🔧 **Engineering**: Performance optimization
- 📚 **Documentation**: Tutorials and guides
- 🎯 **Testing**: More comprehensive test coverage
- 🎨 **Art**: Icons, sprites, and UI elements

## 📊 Project Stats

- **Lines of Code**: ~2,500 C#
- **Test Coverage**: 85%+ (target)
- **Dependencies**: Zero external runtime dependencies
- **Platforms**: Windows, Linux, macOS

## 📞 Community & Support



## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🌟 Acknowledgments

- Inspired by classic D&D combat mechanics
- Built with modern software engineering practices
- Community-driven development philosophy

---

**[⭐ Star this repository](https://github.com/Happy-Gear/rpg-dnd-game)** if you find this project interesting!