# Feature 5 - Forest Dash Roll

## Goal

Give the woods survival minigame an action-button dodge that lets the player burst through danger instead of only relying on baseline movement speed.

## Why This Feature Exists

The forest map already asks the player to survive waves of lunging gnomes, but it lacked an active defensive input. A short invulnerable dash adds a reactive tool without rewriting the existing follower or auto-attack systems.

## Player-Facing Outcome

On `Maps/WoodsBehindCabin`, pressing the action button while moving triggers a commando roll in that travel direction. The roll moves faster than normal walking, ignores damage for the duration of the roll, and starts a 4-second cooldown shown as a shrinking bar under the player's feet.

## Requirements

1. Reserve `InputAction.Confirm` for dodge-roll activation on the woods survival map.
2. Require non-zero movement input so the roll follows the current travel direction.
3. Apply collision-aware dash movement rather than teleporting through walls or props.
4. Grant temporary invulnerability for the roll duration without breaking existing post-hit invulnerability.
5. Render a commando-roll animation from the character sprite sheets while the dash is active.
6. Show cooldown state with a shrinking world-space gauge under the player.
7. Add matching roll frames to both player and companion sprite sheets so the animation atlases stay aligned.
8. Add unit and integration tests covering start rules, cooldown timing, and collision stopping.

## Non-Goals

- Reworking the follower into a second player-controlled dodge state
- Adding a separate HUD widget for dash cooldown
- Changing action-button behavior on non-forest maps

## Acceptance Criteria

1. Pressing action while moving on the woods map starts a visible roll instead of a world interaction.
2. The player travels farther per frame during the roll than during ordinary walking.
3. Gnome hits and bomber blasts do not damage the player while the roll is active.
4. The player cannot re-trigger the roll until the 4-second cooldown has expired.
5. The cooldown bar shrinks away as the timer recovers and disappears when the roll is ready again.
6. Build and targeted tests pass.