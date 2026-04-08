# Code Mode Rules — DogDays

## Before Writing Code

1. Read the relevant section of `docs/DESIGN.md` and any applicable `docs/design/*.md` sub-document to understand existing contracts and conventions.
2. Check existing code in the target folder to match style, naming, and patterns already in use.

## Code Standards

- **Namespace:** `DogDays` (sub-namespaces match folder: `DogDays.Entities`, `DogDays.Input`, etc.)
- **Naming:** `PascalCase` public members, `_camelCase` private fields, `camelCase` locals/params. Interfaces prefixed with `I`.
- **One type per file.** File name must match the type name.
- **XML doc comments** (`///`) on all public types and members.
- Always include necessary `using` directives, sorted alphabetically.

## MonoGame Rules (Enforced)

- **No `Keyboard.GetState()` or `Mouse.GetState()` in gameplay code.** All input goes through `IInputManager`.
- **Delta time everywhere.** Use `gameTime.ElapsedGameTime` for all movement, animation, timing. Never assume fixed frame rate.
- **`Update()` = logic only. `Draw()` = rendering only.** Never mix them. Never mutate state in `Draw()`.
- **Zero allocations in Update/Draw.** No `new` for collections, strings, delegates, or LINQ in the hot path. Pre-allocate and reuse.
- **No magic numbers.** Use named constants or configuration objects.
- **XNA-native first.** Check if MonoGame/XNA already provides a type or pattern before building a custom one. Comment any non-default XNA parameter usage explaining why.

## After Writing Code

1. **Always run `dotnet build`** to verify compilation.
2. If tests were created or modified, **run `dotnet test`** to verify they pass.
3. If a new entity, system, component, screen, interface, event, or test helper was created, **update `docs/DESIGN.md`** and the relevant `docs/design/*.md` sub-document. Design doc entries must describe engine capabilities only — not specific gameplay rules or content.

## Testing Checklist

When creating game logic, also create tests:

- **Unit tests** → `tests/DogDays.Tests/Unit/{ClassUnderTest}Tests.cs`
- **Integration tests** → `tests/DogDays.Tests/Integration/{Feature}Tests.cs`
- **Test helpers** → `tests/DogDays.Tests/Helpers/`
- Use existing fakes: `FakeInputManager`, `FakeGameTime`, `FakeMapCollisionData`
- Test method naming: `{Method}__{Scenario}__{ExpectedResult}`
- All tests must be deterministic — no randomness, no real clocks, no hardware input
- No `Texture2D` or `GraphicsDevice` in test code — use constructor overloads that accept only logic data

## Save Persistence Reminder

Any new entity or component with runtime-mutable state that should survive save/load **must** include: DTO, mapper capture/restore, gameplay screen wiring, and round-trip tests. Dynamically spawned entities additionally need a restore callback. See `.roo/rules.md` for the full checklist.
