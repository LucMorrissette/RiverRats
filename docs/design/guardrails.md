# §30–§34 Architecture Guardrails

> **Purpose:** Prevent the drift patterns identified in the [2026-03-22 audit](../AUDIT-2026-03-22.md) and [2026-03-30 audit](../AUDIT-2026-03-30.md) from recurring. Every guardrail below is designed to be mechanically verifiable — a developer or AI agent can check compliance without subjective judgment.

---

## §30 File Size Limits

Large files accumulate responsibilities silently. `GameplayScreen` reached 1,234 lines with 10+ responsibilities before the audit caught it.

### Thresholds

| Threshold | Lines | Action Required |
|---|---|---|
| **Guideline** | ≤ 300 | Ideal target for most classes. No action needed. |
| **Soft limit** | 301–500 | Acceptable for screens, renderers, and orchestrating classes. Add a `// RATIONALE: <why this file is large>` comment at the top if approaching 500. |
| **Review trigger** | 501–750 | File must be reviewed for decomposition opportunities before adding more code. Document the rationale for the current size in a comment at the class declaration. |
| **Hard limit** | 751+ | **Do not add code.** Decompose first. Extract responsibilities into focused classes, then proceed with the new feature. |

### What Counts

- Count **all lines** in the file including blank lines, comments, and using directives.
- Nested types count toward the parent file's total.
- Auto-generated code and content pipeline files are exempt.

### Verification

```
# Count lines in all .cs files, sorted descending
find src/ -name "*.cs" -exec wc -l {} + | sort -rn | head -20
```

Any file exceeding 750 lines in the output is a blocking issue.

### Enforcement Consequence

Any PR that introduces or modifies a file that exceeds the Hard Limit (751+ lines) MUST include a decomposition plan in the PR description, referencing the specific responsibilities to be extracted and the target files. PRs that push an already-bloated file further will be rejected without review.

---

## §30.5 Parallel Array Anti-Pattern

Managing multiple arrays with matching indices to represent a pool of objects is PROHIBITED. This pattern is equivalent to a struct-of-arrays without type safety or encapsulation. A single index mismatch is a silent data corruption bug with no compile-time protection. Always use a struct or class to group the per-object state.

**BAD:**
```csharp
_orbPositions[i], _orbVelocities[i], _orbTimers[i]  // 3 parallel arrays
```

**GOOD:**
```csharp
EnergyOrb[] _orbs  // array of structs with Position, Velocity, Timer fields
```

*(Audit: GS-02 — 6 parallel arrays for energy orb state in `GameplayScreen`; GS-07 — 4 parallel arrays for explosion pool)*

---

## §30.6 String-Switch Map Configuration Anti-Pattern

Selecting behavior based on map asset name using switch expressions is PROHIBITED. This pattern scatters per-map configuration across multiple methods, violating the Open/Closed principle — adding a new map requires editing every switch method.

**BAD:**
```csharp
private static string GetSongForMap(string mapAssetName) => mapAssetName switch { ... };
private static bool HasDayNightCycle(string mapAssetName) => mapAssetName switch { ... };
// 3 more methods doing the same thing
```

**GOOD:**
```csharp
// A MapConfig record loaded from data or constructed via factory that bundles
// all per-map configuration into a single, typed object.
// The screen receives the config, not the map name.
public sealed record MapConfig(string SongName, bool HasDayNightCycle, bool HasCloudShadows, ...);
```

*(Audit: GS-03 — 5 string-switch methods for map configuration in `GameplayScreen`)*

---

## §31 Architecture Decision Record Discipline

The audit found undocumented entity types, missing interface contracts, and classes that diverged from stated design intent because documentation was not updated alongside code.

### Rule

**Every new class, interface, enum, or system MUST be documented in the corresponding `docs/design/*.md` file before or during implementation — never after.**

### Which Document to Update

This table is the canonical routing. It mirrors the table in [DESIGN.md](../DESIGN.md) § Quick Reference.

| When you create a... | Update this document | Section |
|---|---|---|
| New entity class | `docs/design/entities.md` | §10 Entity Catalog |
| New system or manager | `docs/design/systems-components.md` | §11 Systems |
| New reusable component | `docs/design/systems-components.md` | §12 Components |
| New screen | `docs/design/screens-input.md` | §7 Screen Catalog |
| New interface or core class | `docs/design/core-data-classes.md` | §21 Core Classes |
| New data class, config, or enum | `docs/design/core-data-classes.md` | §22 Data Classes |
| New UI rendering class | `docs/design/ui.md` | §13 UI Catalog |
| New game event | `docs/design/events.md` | §26 Events |
| New test helper or fake | `docs/design/testing.md` | §25 Test Helpers |
| New architectural pattern | `docs/design/architecture.md` | §9 Patterns |
| New implemented feature | `docs/features/<name>.md` | — |
| New guardrail or convention | `docs/design/guardrails.md` | This file |

### What the Entry Must Include

Each catalog entry must contain at minimum:

1. **Class/interface name** — exact type name as it appears in code.
2. **Interfaces implemented** — list all; write `—` if none.
3. **One-line purpose** — what it does, not how.
4. **Key behaviors** — bullet list of notable responsibilities or constraints.

### Engine-Agnostic Principle

Design documents describe engine capabilities and system contracts — NOT specific gameplay rules, quest flows, zone content, or how game features compose engine building blocks.

### Enforcement

- The MonoGame Orchestrator mode includes explicit instructions to require doc updates in every implementation subtask (see rule 3a in `.roomodes`).
- Any PR or commit introducing a new type without a corresponding doc update is non-compliant.
- Any PR that adds a new class, interface, system, component, screen, event, or data class MUST include a corresponding update to the relevant `docs/design/*.md` catalog section. PRs without documentation updates for new types will be rejected. The Quick Reference routing table in [`docs/DESIGN.md`](../DESIGN.md) defines which document to update for each type of addition.

*(Audit: FD-01 through FD-08 — 32 types missing from design doc catalogs, `events.md` completely empty despite 6 active events)*

### Verification

```
# List all public/internal types in src/ and check each has a docs/design/ mention
# This is a heuristic — exact tooling TBD
grep -roh "public class \w\+\|public interface \w\+\|public enum \w\+\|internal class \w\+\|public sealed class \w\+" src/ | sort -u
```

Cross-reference the output against entries in `docs/design/*.md`.

---

## §32 Test Coverage Requirements

The audit found that `LightingRenderer`, `MusicManager`, and `CloudShadowRenderer` had zero or near-zero test coverage despite containing testable pure-function logic.

### Rule

**Every new class containing game logic MUST have a corresponding test file in `tests/RiverRats.Tests/`.**

### What Qualifies as Game Logic

A class has game logic if it contains any of:
- State that changes over time via `Update()` or similar tick methods
- Mathematical computations (collision, interpolation, conversion, clamping)
- State machines or conditional branching based on game state
- Event emission or handling
- Input processing or action mapping

### Test File Placement

| Logic type | Test location | Example |
|---|---|---|
| Single-class isolation | `tests/RiverRats.Tests/Unit/` | `PlayerBlockTests.cs` |
| Multi-system frame simulation | `tests/RiverRats.Tests/Integration/` | `PlayerBlockMovementTests.cs` |
| New test fakes or builders | `tests/RiverRats.Tests/Helpers/` | `FakeInputManager.cs` |

### Test File Naming

- Test file: `{ClassName}Tests.cs`
- Test class: `{ClassName}Tests`
- Must be in the same tier folder as similar tests (Unit/ or Integration/)

### Exempt Classes

The following categories do NOT require test files:

| Category | Rationale | Examples |
|---|---|---|
| Pure GPU renderers | Require `GraphicsDevice`; no testable pure logic | `OcclusionRevealRenderer` |
| Bootstrap/entry points | Thin wiring only | `Game1`, `Program` |
| Trivial data-only types | No logic to test | `LightData`, `ParticleProfile`, `FacingDirection` |
| Static constants holders | No behavior | `BlendStates` |
| Content pipeline artifacts | Generated code | `.mgcb` outputs |

**Exception to exemptions:** If an exempt class later gains testable logic (e.g., a renderer adds a pure-function helper method), it loses its exemption and needs tests for the new logic.

### GPU-Owning Classes: Testability Seam Pattern

Classes that own GPU resources but also contain testable logic should expose that logic through a testability seam:

1. Extract pure logic into `internal` methods or separate helper classes.
2. Test the extracted logic without a `GraphicsDevice`.
3. Document the seam in the class's XML doc comment.

Example: `LightingRenderer.GetAmbientColor()` is a pure function that can be tested without GPU resources, even though `LightingRenderer` itself owns render targets.

---

## §32.5 Testability Seam Requirement for GPU-Owning Screens

Screens (`IGameScreen` implementations) that own GPU resources are exempt from direct instantiation in tests. However, any game logic embedded in such a screen that exceeds ~50 lines MUST be extracted into a pure-logic helper class (a "seam") that can be tested independently.

This applies to:
- State machines (e.g., multi-phase timing, mode transitions)
- Coordinate conversion math
- Spawn/cull/lifecycle logic that does not reference `GraphicsDevice`, `SpriteBatch`, or `Texture2D`

The GPU-owning screen delegates to the seam; only the rendering code remains in the screen. Tests cover the seam; the rendering pipeline is verified via the existing GPU-exempt pattern described in §32.

**BAD:**
```csharp
// 1,975-line screen class with state machine, coordinate math, and spawn logic
// embedded directly — none of it is testable without GraphicsDevice
public class FishingScreen : IGameScreen { ... }
```

**GOOD:**
```csharp
// Pure-logic helper extracted from the screen — fully testable
public sealed class FishingLogic { ... }

// Screen delegates to the helper; only rendering remains
public class FishingScreen : IGameScreen
{
    private readonly FishingLogic _logic;
    ...
}
```

*(Audit: FS-01 — `FishingScreen` at 1,975 lines with untestable embedded game logic; TC-06 — `DeathScreen` timing logic, `RippleSystem` coordinate math need seams)*

---

### Verification

```
# List all production .cs files and check for corresponding test files
for f in $(find src/ -name "*.cs" -not -path "*/Content/*"); do
  class=$(basename "$f" .cs)
  if ! find tests/ -name "${class}Tests.cs" | grep -q .; then
    echo "MISSING TEST: $class"
  fi
done
```

Review the output against the exempt classes list. Any non-exempt class without tests is non-compliant.

---

## §33 Code Review Checklist

This checklist is derived from the most frequent and impactful violations found in the 2026-03-22 audit. Apply it before every commit or PR.

### Pre-Commit Checklist

#### Hot Path Safety
- [ ] **No `new` in `Update()`/`Draw()`.** Collections, delegates, strings, LINQ queries, and temporary objects must be pre-allocated and reused. *(Audit: GS-08 — `GetRenderTargets()` allocated arrays every frame)*
- [ ] **No `Keyboard.GetState()` or `Mouse.GetState()` outside `InputManager`.** All gameplay input flows through `IInputManager`. *(Audit: IM-05 — raw `Mouse.GetState()` in `InputManager.Update()`)*

#### Resource Management
- [ ] **All GPU-resource-owning classes implement `IDisposable`.** If a class creates `Texture2D`, `RenderTarget2D`, `Effect`, `SpriteBatch`, or any other `IDisposable` graphics resource, the class itself must implement `IDisposable` and clean up in `Dispose()`. *(Audit: LR-1, TWR-SP — render targets and sprite batches leaked)*
- [ ] **`IDisposable` dependencies are disposed by their owner.** If class A creates class B and B is `IDisposable`, A must dispose B. *(Audit: IM-06 — `Sdl2MouseListener` leaked)*

#### Design Clarity
- [ ] **No magic numbers.** All numeric literals in logic must be named constants or come from configuration objects. Acceptable exceptions: `0`, `1`, `-1`, `0f`, `1f`, `0.5f`, `2f` in obvious arithmetic contexts. *(Audit: 25+ magic number instances)*
- [ ] **All classes are `sealed` unless inheritance is explicitly intended.** Unsealed classes invite accidental coupling. Add `sealed` by default; remove it only with a comment explaining the inheritance design. *(Audit: GS-09 — `ParticleManager`, `ParticleProfile` unsealed without reason)*
- [ ] **All timing uses `gameTime.ElapsedGameTime`.** Never assume fixed frame rate. Never use `DateTime.Now` or wall-clock time for game logic. *(Audit: Principle verified as compliant — maintain it)*

#### Entity Contracts
- [ ] **New world-placed entities implement `IWorldProp`.** Any entity that exists in world space, has a position and bounds, and is drawn in the world pass must implement `IWorldProp`. *(Audit: CC-1 — 8 entity types with no shared interface)*
- [ ] **Entity Draw methods accept `layerDepth` parameter.** Y-sorting requires consistent depth parameterization. *(Audit: FS-3 — `FlatShoreDepthSimulator` missing `layerDepth`)*
- [ ] **Solid props define collision boxes in code, not in the tilemap.** New entity types that block player movement (trees, buildings, etc.) must define their collision area as a `localCollisionBox` `Rectangle` relative to the sprite origin, stored as a `static readonly` constant in `PropFactory`. **Do not** hand-place collider rectangles in the TMX `Colliders` layer for props — that approach does not scale. Use `Tree` or `Cabin` as the entity class (or create a new entity following the same pattern), and merge collision bounds in `GameplayScreen.LoadContent()` via `PropFactory.GetTreeCollisionBounds()` / `PropFactory.GetCabinCollisionBounds()`. The TMX `Colliders` layer should only contain terrain/world-boundary colliders.

#### Structural Health
- [ ] **File is under 750 lines.** See §30 for thresholds and actions. *(Audit: GS-01 — `GameplayScreen` at 1,234 lines)*
- [ ] **New type is documented in `docs/design/*.md`.** See §31 for routing table. *(Audit: multiple undocumented entity types)*
- [ ] **New logic class has a test file.** See §32 for requirements and exemptions. *(Audit: 3 logic classes with zero test coverage)*

#### Code Hygiene
- [ ] **No duplicate logic across classes.** If two classes share structurally identical code, extract it into a shared component, utility, or base pattern. *(Audit: XC-2, XC-3, XC-4, CC-3 — multiple duplication findings)*
- [ ] **`Update()` contains only logic; `Draw()` contains only rendering.** No state mutation in `Draw()`. No rendering decisions in `Update()` unless computing render data. *(Audit: G-02 — screenshot logic in `Draw()`)*
- [ ] **Deferred mutation guards are consistent.** If a collection is iterated during `Update()`, mutations to that collection must be deferred (queued and applied after iteration). *(Audit: SM-01 — `ScreenManager.Replace()` bypassed guards)*
- [ ] **All new test methods use double-underscore naming: `Method__Condition__Outcome`.** Single-underscore naming is prohibited per §34. *(Audit: TC-07, TC-08 — 10 test methods with non-compliant naming)*
- [ ] **No parallel arrays for pooled object state.** Use a struct or class to group per-object fields. See §30.5. *(Audit: GS-02, GS-07 — parallel arrays in `GameplayScreen`)*
- [ ] **No string-switch map configuration.** Per-map behavior must be bundled into a typed config record. See §30.6. *(Audit: GS-03 — 5 switch methods in `GameplayScreen`)*

---

## §34 Naming Conventions

### General Code Naming

| Element | Convention | Example |
|---|---|---|
| Public types | `PascalCase` | `PlayerBlock`, `IWorldProp` |
| Public methods and properties | `PascalCase` | `Update()`, `Position` |
| Private fields | `_camelCase` | `_position`, `_moveSpeed` |
| Local variables and parameters | `camelCase` | `elapsed`, `startPosition` |
| Constants | `PascalCase` | `MaxLights`, `DefaultSpeed` |
| Interfaces | `I` + `PascalCase` | `IInputManager`, `IWorldProp` |
| Enums | `PascalCase` type, `PascalCase` members | `FacingDirection.North` |
| Namespaces | `RiverRats.Game.{Folder}` | `RiverRats.Game.Entities` |

### File Naming

| Rule | Example |
|---|---|
| One type per file | `PlayerBlock.cs` contains `class PlayerBlock` |
| File name matches type name exactly | `IWorldProp.cs` for `interface IWorldProp` |
| Test files: `{ClassName}Tests.cs` | `PlayerBlockTests.cs` |
| Design docs: lowercase with hyphens | `core-data-classes.md` |
| Feature specs: numbered prefix | `003-screen-manager.md` |

### Test Method Naming

**Convention:** `Method__Condition__ExpectedResult`

Uses **double underscores** (`__`) to separate the three parts. This makes the method-under-test, the setup condition, and the expected outcome immediately scannable.

| Part | Description | Example segment |
|---|---|---|
| **Method** | The method or behavior being tested | `Update`, `Center`, `GetAmbientColor` |
| **Condition** | The specific scenario or input state | `MoveRightForOneSecond`, `EmptyStack`, `ZeroDuration` |
| **ExpectedResult** | What should happen | `AdvancesByConfiguredSpeed`, `ReturnsNull`, `ClampsToWorldBounds` |

**Full examples from the codebase:**

```csharp
Update__MoveRightForOneSecond__AdvancesByConfiguredSpeed()
Update__MoveDiagonallyForOneSecond__NormalizesSpeed()
Update__MovePastRightEdge__ClampsToWorldBounds()
Center__FromPositionAndSize__ReturnsMidpoint()
```

**Rules:**
- All three parts are required. Do not omit the condition.
- Use PascalCase within each part (no underscores inside a part).
- Keep each part concise but descriptive — prefer clarity over brevity.
- The condition should describe the state, not the assertion: `EmptyStack` not `WhenStackIsEmpty`.

### Enforcement

Single-underscore test method naming (`Method_Condition_Outcome`) is PROHIBITED. All test methods must use double underscores as separators. Methods named with single underscores will be flagged in code review and renamed before merge. This is enforced by the code review checklist in §33.

*(Audit: TC-07 — 9 methods in `SlashSystemTests.cs` use single underscores; TC-08 — 1 method in `GnomeEnemyTests.cs` uses mixed naming)*

### Namespace Conventions

```
RiverRats.Game                    — Root namespace for production code
RiverRats.Game.{Folder}           — Sub-namespace matching folder name
RiverRats.Tests.Unit              — Unit test namespace
RiverRats.Tests.Integration       — Integration test namespace
RiverRats.Tests.Helpers           — Test helper namespace
```

- Namespace must match folder structure exactly.
- Do not create namespaces for nested folders within a namespace (e.g., `Content/Maps/` does not create `RiverRats.Game.Content.Maps`).

---

## Summary: Guardrail Verification Quick Reference

| Guardrail | How to Verify | Blocking? |
|---|---|---|
| File size ≤ 750 lines | `wc -l` on changed files | Yes — decompose before adding code |
| New type documented in design docs | Cross-reference type list against `docs/design/*.md` | Yes — document before or during implementation |
| New logic class has test file | Check `tests/` for `{ClassName}Tests.cs` | Yes — write tests before merging |
| No `new` in hot paths | Grep for `new ` inside `Update`/`Draw` method bodies | Yes — pre-allocate instead |
| No raw input API calls | Grep for `Keyboard.GetState\|Mouse.GetState` outside Input/ | Yes — use `IInputManager` |
| GPU owners implement `IDisposable` | Grep for `RenderTarget2D\|new SpriteBatch\|new Effect` and verify class implements `IDisposable` | Yes — add disposal |
| All classes `sealed` | Grep for `class ` without `sealed` or `abstract` | Advisory — add `sealed` unless justified |
| No magic numbers | Code review judgment call on numeric literals | Advisory — extract to constants |
| Double-underscore test naming | Grep test methods for `__` pattern compliance | Yes — rename before merge (§34) |
| `IWorldProp` on world entities | Check entity classes for interface implementation | Yes — implement before merging |
| Solid prop collision in code | Verify no new prop-specific rectangles in TMX `Colliders` layer; collision boxes defined in `PropFactory` | Yes — define in code, not tilemap |
| No parallel arrays for pools | Grep for matching-index array patterns; verify struct/class grouping | Yes — extract to struct/class (§30.5) |
| No string-switch map config | Grep for switch on asset name strings; verify typed config record | Yes — use typed config (§30.6) |
| Bloated file PR includes decomposition plan | PR description references extraction targets for 751+ line files | Yes — reject PR without plan (§30) |
| PR includes doc updates for new types | Cross-reference new types against `docs/design/*.md` changes in same PR | Yes — reject PR without docs (§31) |
| GPU screen logic extracted to seam | Review screen files for >50 lines of pure logic; verify separate helper class | Yes — extract before merge (§32.5) |
