# RiverRats — Design Document

Living source of truth for the RiverRats project's architecture, design decisions, and technical implementation. Every decision here acts as a guardrail for both human and AI contributors.

> **This is the master index.** Detailed content lives in focused sub-documents under [`docs/design/`](design/). Each sub-document is self-contained so LLM agents only need to load the sections relevant to their task.

---

## 1. Project Overview

| Decision | Value | Rationale |
|---|---|---|
| **Framework** | MonoGame 3.8 (WindowsDX) | Windows-native DirectX backend, mature, good community. |
| **Target Runtime** | .NET 9 (net9.0-windows) | Current LTS-adjacent; aligns with MonoGame 3.8 support. |
| **Target Platforms** | PC (windowed + fullscreen) | Guides resolution/input decisions from day one. |
| **XNA-native first** | Always verify MonoGame/XNA default behaviors before building custom solutions | Prevents bugs from unverified assumptions about framework defaults. |
| **Prototype actor control** | `PlayerBlock` uses action input + delta-time movement in world space | Establishes first controllable gameplay actor without sprite/animation complexity. |
| **Party follow prototype** | `FollowerBlock` maintains side-adjacent formation slots relative to leader facing | Keeps companion visible beside the player while preserving deterministic movement. |
| **Static world obstacles** | Screen-owned prop entities feed a shared `WorldCollisionMap` | Keeps terrain collision and placed solid props under one movement query contract. |

---

## 2. Folder Structure

```
src/RiverRats.Game/
    Game1.cs, Program.cs
    /Audio/         Audio services
    /Components/    Reusable behaviors attached to entities (SpriteAnimator, Health, etc.)
    /Core/          Foundational interfaces, events, services
    /Data/          Plain data structures, configs, enums
    /Data/Save/     Save game data models
    /Effects/       Visual effects
    /Entities/      Game world objects (Player, NPC, etc.)
    /Graphics/      Camera, rendering pipeline, graphics helpers
    /Input/         Input abstraction layer (IInputManager, InputManager, InputAction)
    /Screens/       Screen/state management (TitleScreen, GameplayScreen, PauseScreen, etc.)
    /Systems/       Managers that operate on groups of entities (CollisionSystem, object pools, etc.)
    /UI/            UI rendering classes (HudRenderer, DialogueBoxRenderer, etc.)
    /Util/          Pure utility/helper classes
    /World/         Tilemap and world systems
docs/
    DESIGN.md       Master design index (this file)
    design/         Focused design sub-documents
    features/       Implemented feature specs (.md files)
tests/RiverRats.Tests/
    /Unit/          Unit tests
    /Integration/   Integration tests
    /Helpers/       Test fakes and builders
```

---

## Design Sub-Documents

Each sub-document covers a focused domain. Load only what you need for your current task.

| Document | Sections | What's Inside |
|---|---|---|
| [rendering.md](design/rendering.md) | §3, §4, §19, §23 | Display pipeline, art style, particle/effects decisions, graphics classes |
| [gameplay.md](design/gameplay.md) | §5, §6 | Movement, collision, combat, characters, animation |
| [architecture.md](design/architecture.md) | §9 | Core patterns, composition, zones, sequencers |
| [screens-input.md](design/screens-input.md) | §7, §8 | Screen stack, input abstraction, `IInputManager` API |
| [entities.md](design/entities.md) | §10 | Entity catalog (all game world objects with interfaces) |
| [systems-components.md](design/systems-components.md) | §11, §12 | Systems catalog + components catalog |
| [ui.md](design/ui.md) | §13 | UI/HUD decisions and class catalog |
| [audio.md](design/audio.md) | §14 | Audio pipeline, SFX, music, ambient sound |
| [save-system.md](design/save-system.md) | §18 | Save/load persistence, versioning, persisted state |
| [world.md](design/world.md) | §20 | World/tilemap classes |
| [core-data-classes.md](design/core-data-classes.md) | §21, §22, §24 | Core classes, data classes, utility classes |
| [testing.md](design/testing.md) | §25 | Testing strategy, test helpers catalog |
| [events.md](design/events.md) | §26 | Complete game events catalog |
| [tooling.md](design/tooling.md) | §27, §28, §29 | Technical gotchas, dev assets, developer tools |
| [`features/`](features/) | — | Implemented feature specs (one `.md` per feature) |

---

## Quick Reference: Which Doc to Update

| When you create a... | Sub-document | Master section |
|---|---|---|
| New entity | `docs/design/entities.md` | §10 |
| New system or component | `docs/design/systems-components.md` | §11, §12 |
| New screen | `docs/design/screens-input.md` | §7 |
| New interface or core class | `docs/design/core-data-classes.md` | §21 |
| New data class or config | `docs/design/core-data-classes.md` | §22 |
| New UI class | `docs/design/ui.md` | §13 |
| New game event | `docs/design/events.md` | §26 |
| New test helper/fake | `docs/design/testing.md` | §25 |
| New architectural pattern | `docs/design/architecture.md` | §9 |
| New implemented feature | `docs/features/<feature-name>.md` | — |
| Folder structure changes | `docs/DESIGN.md` only | §2 |
