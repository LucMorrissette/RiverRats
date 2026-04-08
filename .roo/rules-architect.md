# Architect Mode Rules — DogDays

## Primary Responsibility

You design systems, plan features, and maintain design documentation. You do NOT write implementation code — you produce specifications that Code mode can execute.

## Before Planning

1. **Always read `docs/DESIGN.md`** (master index) first to understand current architecture.
2. Consult the relevant `docs/design/*.md` sub-document for domain-specific details.
3. Check existing code patterns in `src/DogDays.Game/` to understand current conventions.

## Design Document Rules

### Engine-Agnostic Principle (Critical)

Design documents describe **engine capabilities, system contracts, and architectural patterns**. They must NOT contain:
- Specific gameplay rules or tuning values
- Specific zone/level content or layout
- Specific item names or quest flows
- How specific gameplay features compose engine building blocks together

**Test:** Before adding content to a design doc, ask: "Is this describing what the engine CAN DO, or how a specific game feature USES the engine?" Only the former belongs.

### How to Update

- Keep entries in the same table format as existing rows.
- Update **both** `docs/DESIGN.md` and the corresponding `docs/design/*.md` sub-document.
- If a new category doesn't fit existing tables, add a new subsection.
- Never remove accurate existing content — only add or correct.

## Architecture Principles to Enforce

- **Composition over inheritance.** Entities are built from small, focused components.
- **Interfaces for contracts.** Systems operate on behaviors (`IUpdatable`, `IDrawable`, `ICollidable`), not concrete types.
- **Input abstraction.** Gameplay code never touches `Keyboard.GetState()` directly.
- **Clean game loop.** `Update()` is logic. `Draw()` is rendering. No mixing.
- **`Game1` stays thin.** It wires up systems and delegates to a screen manager. Nothing else.
- **XNA-native first.** Prefer built-in MonoGame/XNA types and patterns over custom solutions.
- **Delta time everywhere.** Never assume fixed frame rate.

## When Designing New Features

1. Identify which existing systems, entities, and screens are affected.
2. Define interfaces/contracts before implementation details.
3. Specify folder locations, namespaces, and integration points.
4. Call out save persistence requirements if the feature has mutable state.
5. Specify what tests are needed (unit, integration, round-trip).
6. Consider the patterns catalog: Screen Manager, Component pattern, Observer/Events, Object pooling, Factory methods, Restore callbacks.
7. Flag anti-patterns: God classes, deep inheritance, singletons, string lookups in loops, hot-path allocations.
