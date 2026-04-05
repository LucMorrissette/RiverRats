# RiverRats — Design Document

Living source of truth for the RiverRats project's architecture, design decisions, and technical implementation. Every decision here acts as a guardrail for both human and AI contributors.

> **This is the master index.** Detailed content lives in focused sub-documents under [`docs/design/`](design/). Each sub-document is self-contained so LLM agents only need to load the sections relevant to their task.

---

## 1. Project Overview

| Decision | Value | Rationale |
|---|---|---|
| **Framework** | MonoGame 3.8 (DesktopGL) | Cross-platform desktop backend for macOS, Linux, and Windows. |
| **Target Runtime** | .NET 10 (net10.0) | Matches the DesktopGL build and supports cross-platform local development. |
| **Target Platforms** | PC (windowed + fullscreen) | Guides resolution/input decisions from day one. |
| **XNA-native first** | Always verify MonoGame/XNA default behaviors before building custom solutions | Prevents bugs from unverified assumptions about framework defaults. |
| **Water rendering grouping** | TMX layers prefixed with `Water/` are composited into the water distortion pass | Decouples the shader from individual tile definitions and supports stacked underwater layers. |
| **Night lighting** | Low-resolution lightmap composited with multiply blend and populated from frame snapshots of world lights | Keeps day/night darkening and localized fire glow decoupled from gameplay entities while preserving a soft pixel-art look. |
| **Prototype actor control** | `PlayerBlock` uses action input + delta-time movement in world space | Establishes first controllable gameplay actor without sprite/animation complexity. |
| **Party follow prototype** | `FollowerBlock` samples the player path and renders one companion at a fixed breadcrumb lag | Keeps follower motion deterministic, corner-safe, and aligned to the leader's real route. |
| **Graph-patrolling NPC** | `MomNpc` navigates via `IndoorNavigator` component backed by `IndoorNavGraph` — delegates route selection, progress tracking, and repath to `IndoorNavigator`; handles movement and collision locally | Provides autonomous indoor NPC presence using authored navigation graphs instead of hardcoded waypoints. |
| **Reusable indoor navigator** | `IndoorNavigator` component picks random destination nodes on an `IndoorNavGraph`, computes A* routes, tracks progress, pauses at destinations, and repaths when stuck — exposes `CurrentTargetPosition` for the owning entity to steer toward | Decouples graph-based route-following from any specific NPC, enabling reuse across indoor actors. |
| **Static world obstacles** | TMX object-layer prop placements spawn screen-owned entities that feed `WorldCollisionMap` | Keeps terrain collision and placed solid props under one movement query contract. |
| **Static prop rendering** | TMX object-layer placements support both collidable and decorative prop sprites | Keeps world decoration editor-driven without coupling visuals to collision rules. |
| **Typed prop entities** | TMX `propType` metadata maps to focused entity classes (`Boulder`, `Dock`, `SunkenChest`, `Firepit`) in gameplay composition | Preserves single-purpose prop behavior and keeps renderer/collision wiring explicit. |
| **Prop state variants** | Distinct TMX `propType` values can select authored visual states of the same focused prop entity, and gameplay may swap between those authored states at runtime based on local context (for example, doors opening on approach) | Keeps editor-authored state swaps explicit without pushing ad-hoc branching into generic prop code. |
| **Walkable prop surfaces** | Prop bounds can override blocked terrain beneath them | Supports authored surfaces like docks or bridges without hand-editing collision mask tiles. |
| **Object-layer colliders** | TMX `Colliders` object layer provides sub-tile collision rectangles merged into `WorldCollisionMap` | Enables precise collision geometry for props and barriers without being constrained to tile-grid granularity. |
| **Zone transitions** | TMX object-layer rectangles in `ZoneTriggers` request map replacement into another `GameplayScreen` using a destination map asset and spawn-point id | Keeps exits editor-authored, per-instance configurable, and decoupled from tile definitions. |
| **Named spawn points** | TMX `SpawnPoints` object layer defines reusable arrival positions by name | Lets any trigger target a stable entry point in another map without hardcoded coordinates in gameplay code. |
| **Entity Y-sorting** | `SpriteSortMode.FrontToBack` with `layerDepth = Bounds.Bottom / mapPixelHeight` per entity | Correct depth overlap (e.g., player behind house) using XNA's built-in sprite sorting — no custom sort or interface needed. |
| **Occlusion reveal** | Entities in front of the player are drawn to a separate render target and composited with the `OcclusionReveal` shader, which creates a circular alpha-fade lens around the player centre | Lets the player remain visible behind tall props (trees, cabins) without breaking Y-sort or requiring per-entity transparency logic. |
| **CRT post-process filter** | Optional full-screen shader (barrel distortion + scanlines + vignette) applied during the final scene blit, toggled at runtime via `InputAction.ToggleCrtFilter` (F9) | Adds retro CRT aesthetic without affecting the HUD overlay pass or gameplay logic. |
| **Fishing water shader** | `FishingWater.fx` post-process renders the water tile layer to a render target and composites with UV wave distortion, event-driven concentric ripples, expanding splash highlight rings, red spook rings for bad casts, and procedural underwater caustics | Adds visual juice to the side-view fishing scene without coupling effects to tile definitions or CPU particle logic. |
| **Quest progression** | Data-driven linear quests are owned by a session-scoped `QuestManager` and advance from `GameEventBus` payloads | Keeps quest logic decoupled from NPC, combat, and zone code while preserving progress across screen replacement. |
| **Quest discovery HUD** | Newly started quests queue through `QuestDiscoverySequence`, play a discovery sting, flash warm, sweep shimmer, and spawn additive glint bursts when each queued banner becomes active | Preserves quest-start payoff even when multiple discoveries queue while keeping the feedback in a dedicated UI path instead of world-space effects. |
| **Quest completion HUD** | Completed quests queue through `QuestCompletionSequence`, play a cue, kick and flash the quest panel green, pop the completion badge, sweep a shimmer pass, then hand the tracker back to the next active quest or a no-active placeholder | Preserves completion feedback long enough to read without leaving stale completed quests pinned to the HUD. |
| **Forest dodge roll** | `InputAction.Confirm` triggers a short dash roll on `Maps/WoodsBehindCabin`, granting brief invulnerability and starting a 4-second world-space cooldown gauge under the player | Adds an active defense to the survival map without adding a separate HUD widget or bypassing the input abstraction layer. |
| **Player death collapse** | `GameplayScreen` holds on the world view briefly after `Health.OnDied`, playing a bottom-anchored collapse squash before replacing itself with `DeathScreen` | Preserves spatial context for failure feedback instead of hard-cutting directly from live control to the failure overlay. |
| **Indoor navigation graph** | `IndoorNavGraph` provides authored node/link pathfinding for indoor maps, loaded from TMX `NavNodes` and `NavLinks` object layers | Gives indoor NPCs graph-based route selection instead of raw waypoints, decoupled from the tile-grid collision system. |

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
| [guardrails.md](design/guardrails.md) | §30–§34 | File size limits, ADR discipline, test coverage rules, code review checklist, naming conventions |
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
| New guardrail or convention | `docs/design/guardrails.md` | §30–§34 |
| New implemented feature | `docs/features/<feature-name>.md` | — |
| Folder structure changes | `docs/DESIGN.md` only | §2 |
