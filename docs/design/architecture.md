# §9 Architecture & Patterns

| Decision | Value | Rationale |
|---|---|---|
| **Entity design** | Composition over inheritance | No `GameObject` base class. Entities are built from focused components. |
| **System decoupling** | `GameEventBus` + local C# events | Gameplay systems keep focused local events, while cross-cutting progression flows publish lightweight `GameEvent` payloads through a shared bus. |
| **Game1 responsibility** | Wire up systems, delegate to ScreenManager | `Game1` stays thin — no gameplay logic. |
| **Dependency injection** | `Game.Services` + constructor injection | Avoids singletons; keeps classes testable. |
| **Screen management** | Stack-based ScreenManager | Screens push/pop (gameplay, pause, inventory). Topmost screen receives input. |
| **Input abstraction** | IInputManager interface | Gameplay code never touches `Keyboard.GetState()` directly. Testable via fakes. |
| **XNA-native first** | Prefer MonoGame/XNA built-in types and patterns | Custom solutions only when XNA doesn't provide what's needed. |
| **Navigation / steering / collision split** | Navigation (route selection) → Steering (route following) → Collision (step validation) | Each layer has a single job: nav picks reachable routes, steering follows them, collision resolves each step. Prevents mixing pathfinding with physics. |
| **Quest progression** | Session-owned `QuestManager` reacts to `GameEventBus` payloads | Quest state survives `GameplayScreen` replacement while quest objectives remain decoupled from NPC, combat, and zone code. |

*(Add entries as new architectural patterns emerge: zone systems, object pooling, factories, sequencers, trigger zones, etc.)*
