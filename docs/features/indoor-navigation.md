# Indoor Navigation — Implementation Plan

## Problem Statement

Navigation and collision are doing jobs they were not designed to do. The collision contract (`IMapCollisionData`) only answers "blocked or not blocked." That is enough for player movement. It is not enough for NPCs that need to decide how to get around furniture, corners, and other actors.

Current symptoms:
- Mom gets stuck on furniture because she steers toward raw waypoints with no obstacle awareness.
- Gnomes jitter because the flow field plans on a coarse 32px grid while movement collides in finer world-space rectangles.
- Different movers use different collision hulls (foot bounds vs full sprite).
- Recovery hacks (timeout skips, phasing) compensate for the missing navigation layer.

## Scope

### In scope
- Mom and future cabin-style indoor NPCs.
- Authored navigation graph for indoor maps.
- Reusable navigator component.

### Out of scope
- Player movement changes.
- Combat/enemy movement changes.
- Physics engine integration.
- Gnome flow-field cleanup (separate effort).

---

## Architecture

### Responsibility split

| Layer | Question it answers | Owner |
|---|---|---|
| **Navigation** | What reachable route should I take? | `IndoorNavGraph` + `IndoorNavigator` |
| **Steering** | How do I follow that route and react to temporary blockers? | NPC entity (`MomNpc`) |
| **Collision** | Can I occupy this space right now? | `WorldCollisionMap` / `IMapCollisionData` |

Navigation selects the route. Steering follows it. Collision resolves each step locally. They do not replace each other.

### Key types

| Type | Location | Purpose |
|---|---|---|
| `IndoorNavNode` | `World/` | Single navigable point with an id, world position, and optional tags. |
| `IndoorNavLink` | `World/` | Explicit connection between two nodes (bidirectional). |
| `IndoorNavGraph` | `World/` | Collection of nodes and links. Supports nearest-node lookup and A* route queries. |
| `IndoorNavigator` | `Systems/` or `Components/` | Reusable component that picks destination nodes, computes graph routes, and exposes the current target position for a mover to steer toward. |

---

## Authoring Model (Tiled)

Navigation data is authored in Tiled using two dedicated object layers per map:

### `NavNodes` object layer
- **Point objects** placed at walkable positions.
- Each object has:
  - `id` — unique integer (Tiled auto-assigns).
  - `name` — optional human-readable label (e.g. `kitchen`, `doorway`).
  - `type` — `nav-node`.
  - Custom properties:
    - `tags` (string, comma-separated) — optional behavior hints like `idle`, `lounge`, `entry`.

### `NavLinks` object layer
- **Polyline objects** connecting exactly two `NavNode` positions.
- Each polyline's endpoints snap to the corresponding `NavNode` positions.
- Links are **bidirectional** by default.
- Why polylines instead of auto-connect: explicit links are visible in the editor and prevent hidden routing through walls.

Maps without nav data continue to work unchanged — the graph is optional.

---

## Rollout Phases

### Phase 1 — Data model + TMX loading + debug rendering
1. Define `IndoorNavNode`, `IndoorNavLink`, `IndoorNavGraph` in `World/`.
2. Extend TMX loading to parse `NavNodes` and `NavLinks` object layers.
3. Expose the loaded graph from `TiledWorldRenderer` (nullable — not all maps have one).
4. Add a debug overlay that draws nodes, links, current path, and NPC foot bounds.
5. Author nav nodes and links in `CabinIndoors.tmx`.
6. Unit tests: graph construction, nearest-node lookup, A* route computation.

### Phase 2 — Mom migrates to graph routes
1. Refactor `MomNpc` to accept an `IndoorNavGraph` instead of a raw waypoint array.
2. New behavior:
   - Pick a destination node (random from allowed set, or tagged subset).
   - Compute a route across graph links (A* or BFS — graph is tiny).
   - Walk node-to-node using existing foot-bounds collision.
   - Pause at each destination node.
   - When blocked temporarily (e.g. player in the way), wait briefly and retry.
   - If still blocked after timeout, repath from nearest reachable node.
3. Remove the hardcoded waypoint array from `GameplayScreen`.
4. Remove the stuck-timeout-skip hack (graph repath replaces it).
5. Integration tests: Mom traverses cabin graph without entering furniture bounds; Mom recovers when player blocks her.

### Phase 3 — Reusable navigator component
1. Extract route-following logic from `MomNpc` into `IndoorNavigator`.
2. `IndoorNavigator` becomes a composable component any indoor NPC can use.
3. `MomNpc` delegates to `IndoorNavigator` for route selection and progress tracking.
4. Future indoor NPCs (e.g. Dad, shopkeeper) reuse the same component.

### Phase 4 — Gnome navigation cleanup (separate effort)
1. Align flow-field grid resolution with actual agent collision hulls.
2. Standardize movement hulls across all walking actors.
3. Remove phasing hack once navigation and collision agree.
4. This is a separate workstream and should not block indoor navigation.

---

## Debug Tooling

Before expanding usage, add a debug overlay (toggled via existing debug key) that renders:
- Nav nodes (small circles or crosses).
- Nav links (lines between connected nodes).
- Current target node (highlighted).
- Current planned path (colored line).
- NPC foot bounds (rectangle outline).
- Blocked/repath state indicator.

This overlay makes graph authoring and NPC tuning inspectable without guesswork.

---

## Testing Strategy

### Unit tests
- Graph construction from node/link data.
- Nearest-node lookup by world position.
- A* route: shortest path across multiple nodes.
- A* route: unreachable node returns empty path.
- Repath: new route from alternate start node.

### Integration tests
- `CabinIndoors` graph loads correctly from TMX data.
- Mom traverses between multiple authored nodes without entering couch/furniture bounds.
- Mom reroutes or waits when the player blocks her current path segment.
- Round-trip: nav graph survives zone transition (if Mom state is persisted).

---

## What not to do

- **Do not introduce a physics engine.** The problem is navigation, not simulation.
- **Do not try to unify Mom and gnome movement in the first pass.** They have different needs.
- **Do not keep adding timeout hacks to waypoint steering.** That approach has reached its limit.
- **Do not auto-generate links from node proximity.** Explicit authored links are more reliable for small indoor spaces.

---

## Design doc updates required

When implementation begins, update:
- `docs/DESIGN.md` — add indoor navigation graph as an engine capability.
- `docs/design/architecture.md` — document the navigation/steering/collision responsibility split.
- `docs/design/world.md` — document `IndoorNavGraph` and TMX authoring model.
- `docs/design/entities.md` — update `MomNpc` entry to reference graph-based routing.
- `docs/design/systems-components.md` — add `IndoorNavigator` component entry.

All entries must describe engine capabilities, not specific gameplay content.
