# Debug Mode Rules — DogDays

## Debugging Approach

1. **Reproduce first.** Understand the exact steps, inputs, or game state that trigger the issue.
2. **Read the relevant code** before making hypotheses. Use `read_file` and `codebase_search` to understand the current implementation.
3. **Check the game loop separation.** Many MonoGame bugs stem from mixing logic in `Draw()` or rendering in `Update()`.
4. **Check delta time usage.** Frame-rate-dependent bugs often come from missing or incorrect `gameTime.ElapsedGameTime` usage.

## Common MonoGame Bug Categories

### Rendering Issues
- **Blank/black screen:** Check `RenderTargetUsage` — the default `DiscardContents` silently destroys RT contents when switching targets mid-frame. Multi-pass rendering requires `PreserveContents`.
- **Flickering/tearing:** Check `SpriteBatch.Begin()` parameters — wrong `SpriteSortMode` or missing camera `Matrix` transform.
- **Wrong position:** Verify world-space vs. screen-space. Camera transform should be applied to world objects but NOT to UI.
- **Missing sprites:** Check Content Pipeline build (`Content/Content.mgcb`), asset path casing, and `Content.Load<T>()` calls.

### Input Issues
- **Input not registering:** Check that `IInputManager.Update()` is called before gameplay logic reads input state.
- **Actions firing every frame instead of once:** Ensure press detection uses previous + current state comparison, not just current state.
- **Direct hardware calls:** Search for `Keyboard.GetState()` or `Mouse.GetState()` outside the input layer — these bypass the abstraction.

### Collision Issues
- **Entities passing through walls:** Check collision detection order — detection must happen BEFORE position update is committed.
- **Phantom collisions:** Verify AABB bounds are correct (position, width, height) and collision map data matches visual tiles.

### Performance Issues
- **Frame drops:** Search for `new` allocations inside `Update()` or `Draw()`. Check for LINQ queries, string concatenation, or delegate creation in the hot path.
- **Memory growth:** Look for missing object pool returns, event handler leaks (subscribe without unsubscribe), or uncached content loads.

## Diagnostic Techniques

- Add temporary `System.Diagnostics.Debug.WriteLine()` calls for frame-by-frame state inspection.
- Use the existing test infrastructure to write a minimal reproduction: `FakeInputManager`, `FakeGameTime`, `FakeMapCollisionData`.
- Check if the bug reproduces in a unit/integration test — if so, fix it there first.

## After Fixing

1. Run `dotnet build` to verify the fix compiles.
2. Run `dotnet test` to verify no existing tests broke.
3. If the bug reveals a gap in test coverage, recommend (or create) a new test that would catch a regression.
4. Remove any temporary diagnostic logging before considering the fix complete.
