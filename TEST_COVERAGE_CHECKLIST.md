# 🧪 RPG DnD Game Test Coverage Checklist

This checklist tracks automated test coverage for all major systems and scenarios in the project, organized by strategic phases and priority.

---

## 🎯 Strategic Testing Plan

### **Phase 1: Complete Foundation (30 min)** ✅ **COMPLETE**
- [x] CharacterTests.cs - Basic character logic and DTOs
- [x] CounterGaugeTests.cs - Counter gauge mechanics

---

### **Phase 2: Build Test Helpers First (1.5 hours)** ✅ **COMPLETE**
**KEY: Build helpers before core tests for speed and maintainability**
- [x] CombatTestBuilder.cs ✅ **DONE**
  - Pre-configured combat scenarios
  - Attack/defense setup
  - Dice determinism for combat
- [x] TestScenarios.cs ✅ **DONE**
  - Common adjacent combat, movement situations
  - Pre-built character configs
  - Standard sequences

---

### **Phase 3: Core Systems Testing (4-5 hours)** 🚧 **READY TO START**
**Dependency order recommended:**
- [x] CombatSystemTests.cs ⭐ **NEXT PRIORITY - CRITICAL**
  - Attack mechanics
  - Defense choices (DEF/MOV/TAKE)
  - Counter gauge building
  - Badminton streak activation
  - Edge cases (no stamina, dead characters)
- [x] MovementSystemTests.cs
  - 1d6 vs 2d6 movement
  - Stat-based movement calculations
  - Grid boundary validation
  - Manhattan distance checks
- [x] TurnManagerTests.cs
  - Turn order logic
  - Action availability per turn
  - Counter-attack interrupts
  - Win conditions
  - Dead character skipping

---

### **Phase 4: Integration Testing (3-4 hours)**
- [ ] CombatFlowTests.cs
  - Full attack → defense → damage flow
  - Counter attack sequences
  - Multi-round combat
  - Resource depletion
- [ ] GameManagerTests.cs ⭐ MOST COMPLEX
  - Game initialization
  - Input parsing
  - Action economy (move → 2nd action)
  - Defense choice prompts
  - Post-evasion movement
  - ASCII grid updates
  - Game ending conditions

---

### **Phase 5: Polish (Optional, 30 min)**
- [ ] CharacterStatsTests.cs (DTOs, stat calculations)

---

## 📊 Risk-Based Priority Matrix

| System          | Complexity | Bug Risk | Test Priority | Rank |
|-----------------|------------|----------|--------------|------|
| GameManager     | 🔴 Very High | 🔴 Very High | #1         |
| CombatSystem    | 🔴 High      | 🔴 High     | #2         |
| TurnManager     | 🟡 Medium    | 🟡 Medium   | #3         |
| MovementSystem  | 🟡 Medium    | 🟡 Medium   | #4         |
| CounterGauge    | 🟢 Low       | 🟡 Medium   | #5 ✅      |

---

## 🚀 **Current Status: Ready for Phase 3**

**✅ Foundations Complete:** All helpers and infrastructure ready
**🎯 Next Target:** CombatSystemTests.cs (highest impact, most critical)
**📈 Progress:** 2/5 phases complete, test helpers built

---

## 🎮 **Test Helper Usage Examples**

With Phase 2 complete, you can now write tests efficiently:

```csharp
// Quick combat test setup
var (attacker, defender, combat) = TestScenarios.QuickSetup.BasicCombatTest();

// Specific positioning
var (att, def) = TestScenarios.AdjacentCombat.Diagonal();

// Resource edge cases
var exhausted = TestScenarios.ResourceStates.Exhausted();

// Complex scenarios
var sequence = CommonCombatScenarios.BasicAttack()
    .ExecuteAttackDefenseSequence(DefenseChoice.Defend);
```

---

## Notes
- Mark items as `[x]` complete or `[ ]` incomplete.
- Link to test files or scenarios as coverage improves.
- Update this checklist as the project evolves.
- **Phase 2 helpers will dramatically speed up Phase 3 development**