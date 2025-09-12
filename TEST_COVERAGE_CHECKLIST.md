# 🧪 RPG DnD Game Test Coverage Checklist

This checklist tracks automated test coverage for all major systems and scenarios in the project, organized by strategic phases and priority.

---

## 🎯 Strategic Testing Plan

### **Phase 1: Complete Foundation (30 min)**
- [x] CharacterTests.cs - Basic character logic and DTOs
- [ ] CounterGaugeTests.cs - Counter gauge mechanics

---

### **Phase 2: Build Test Helpers First (1.5 hours)**
**KEY: Build helpers before core tests for speed and maintainability**
- [ ] CombatTestBuilder.cs
  - Pre-configured combat scenarios
  - Attack/defense setup
  - Dice determinism for combat
- [ ] TestScenarios.cs
  - Common adjacent combat, movement situations
  - Pre-built character configs
  - Standard sequences

---

### **Phase 3: Core Systems Testing (4-5 hours)**
**Dependency order recommended:**
- [ ] CombatSystemTests.cs ⭐ CRITICAL
  - Attack mechanics
  - Defense choices (DEF/MOV/TAKE)
  - Counter gauge building
  - Badminton streak activation
  - Edge cases (no stamina, dead characters)
- [ ] MovementSystemTests.cs
  - 1d6 vs 2d6 movement
  - Stat-based movement calculations
  - Grid boundary validation
  - Manhattan distance checks
- [ ] TurnManagerTests.cs
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
| CounterGauge    | 🟢 Low       | 🟡 Medium   | #5         |

---

## 🚀 Optimized Execution Order
Follow the phases and priority ranking above for maximum effectiveness.

---

## Notes
- Mark items as `[x]` complete or `[ ]` incomplete.
- Link to test files or scenarios as coverage improves.
- Update this checklist as the project evolves.
