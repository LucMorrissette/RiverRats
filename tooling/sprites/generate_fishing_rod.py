"""
Generate fishing rod overlay sprites for the fishing mini-game screen.

Produces two sprites:
  1. fishing_rod.png       — idle pose: rod extends diagonally up-right with
                             a short line, bobber, and hook.
  2. fishing_rod_windup.png — wind-up pose: rod swept behind/above the
                             character's head, no line (preparing to cast).

Both sprites are drawn as overlays on the right-facing player character.

Output:
  - tooling/sprites/output/fishing_rod.png
  - tooling/sprites/output/fishing_rod_windup.png
  - src/DogDays.Game/Content/Sprites/fishing_rod.png
  - src/DogDays.Game/Content/Sprites/fishing_rod_windup.png

Sprite size: 48 x 64 pixels, RGBA.
"""

import os
import shutil
from PIL import Image, ImageDraw

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
OUTPUT_DIR = os.path.join(SCRIPT_DIR, "output")
PROJECT_ROOT = os.path.abspath(os.path.join(SCRIPT_DIR, "..", ".."))
CONTENT_SPRITES = os.path.join(
    PROJECT_ROOT, "src", "DogDays.Game", "Content", "Sprites"
)
os.makedirs(OUTPUT_DIR, exist_ok=True)

# ---------------------------------------------------------------------------
# Sprite geometry
# ---------------------------------------------------------------------------
SPRITE_W = 48
SPRITE_H = 64

# Handle attachment point (where the character grips the rod).
HANDLE_X = 5
HANDLE_Y = 30

# Rod tip (upper-right, where line attaches).
TIP_X = 40
TIP_Y = 6

# Fishing line drops from tip straight down.
LINE_BOTTOM_Y = 19

# Bobber position (on the line).
BOBBER_Y = 15

# ---------------------------------------------------------------------------
# Color palette
# ---------------------------------------------------------------------------
TRANSPARENT = (0, 0, 0, 0)
OUTLINE     = (26, 10, 13, 255)

ROD_LIGHT   = (160, 120, 72, 255)   # lighter wood
ROD_MID     = (130, 90, 50, 255)    # mid wood
ROD_DARK    = (100, 68, 36, 255)    # darker wood

CORK_LIGHT  = (196, 169, 108, 255)  # cork handle highlight
CORK_MID    = (170, 145, 88, 255)   # cork handle
CORK_DARK   = (140, 118, 68, 255)   # cork handle shadow

REEL_LIGHT  = (170, 170, 180, 255)  # reel metal highlight
REEL_MID    = (130, 130, 140, 255)  # reel body
REEL_DARK   = (90, 90, 100, 255)    # reel shadow

LINE_COLOR  = (40, 40, 50, 200)     # fishing line (slightly transparent)

BOBBER_RED  = (220, 50, 40, 255)
BOBBER_WHITE = (240, 240, 240, 255)


def draw_thick_line(img, x0, y0, x1, y1, color):
    """Draw a 1px line with an adjacent parallel line for 2px width."""
    draw = ImageDraw.Draw(img)
    draw.line([(x0, y0), (x1, y1)], fill=color)


def lerp_point(x0, y0, x1, y1, t):
    """Linearly interpolate between two points."""
    return (x0 + (x1 - x0) * t, y0 + (y1 - y0) * t)


def generate_fishing_rod():
    """Generate the fishing rod overlay sprite."""
    img = Image.new("RGBA", (SPRITE_W, SPRITE_H), TRANSPARENT)

    # ----- Cork handle (grip area) -----
    # A small vertical rectangle for the grip.
    for dy in range(-3, 4):
        y = HANDLE_Y + dy
        if 0 <= y < SPRITE_H:
            # 3px wide handle
            img.putpixel((HANDLE_X - 1, y), CORK_DARK)
            img.putpixel((HANDLE_X, y), CORK_MID)
            img.putpixel((HANDLE_X + 1, y), CORK_LIGHT)

    # Handle outline (top and bottom)
    for dx in range(-1, 2):
        yTop = HANDLE_Y - 4
        yBot = HANDLE_Y + 4
        if 0 <= yTop < SPRITE_H:
            img.putpixel((HANDLE_X + dx, yTop), OUTLINE)
        if 0 <= yBot < SPRITE_H:
            img.putpixel((HANDLE_X + dx, yBot), OUTLINE)

    # Handle side outlines
    for dy in range(-3, 4):
        y = HANDLE_Y + dy
        if 0 <= y < SPRITE_H:
            img.putpixel((HANDLE_X - 2, y), OUTLINE)
            img.putpixel((HANDLE_X + 2, y), OUTLINE)

    # ----- Reel (small block near handle) -----
    reel_x = HANDLE_X + 3
    reel_y = HANDLE_Y + 1
    # 3x4 reel body
    for dy in range(-1, 3):
        for dx in range(0, 3):
            px = reel_x + dx
            py = reel_y + dy
            if 0 <= px < SPRITE_W and 0 <= py < SPRITE_H:
                if dy == -1 or dy == 2 or dx == 0 or dx == 2:
                    img.putpixel((px, py), OUTLINE)
                else:
                    img.putpixel((px, py), REEL_MID)
    # Reel highlight
    img.putpixel((reel_x + 1, reel_y), REEL_LIGHT)
    # Reel crank (small nub)
    img.putpixel((reel_x + 1, reel_y + 3), OUTLINE)
    img.putpixel((reel_x + 1, reel_y + 4), REEL_DARK)

    # ----- Rod pole (diagonal from handle to tip) -----
    # Draw the rod from just above the handle to the tip.
    rod_start_x = HANDLE_X + 1
    rod_start_y = HANDLE_Y - 4

    # The rod tapers: thicker near handle, thinner near tip.
    # Use multiple segments for a slight curve.
    segments = 40
    for i in range(segments + 1):
        t = i / segments
        # Slight upward curve using quadratic bezier.
        # Control point is above the straight line.
        mid_x = (rod_start_x + TIP_X) / 2
        mid_y = min(rod_start_y, TIP_Y) - 3  # curve upward slightly

        # Quadratic bezier: B(t) = (1-t)²P0 + 2(1-t)tP1 + t²P2
        bx = ((1 - t) ** 2) * rod_start_x + 2 * (1 - t) * t * mid_x + (t ** 2) * TIP_X
        by = ((1 - t) ** 2) * rod_start_y + 2 * (1 - t) * t * mid_y + (t ** 2) * TIP_Y

        px = int(round(bx))
        py = int(round(by))

        if 0 <= px < SPRITE_W and 0 <= py < SPRITE_H:
            # Thicker near handle (first 40%), 2px wide; rest is 1px.
            if t < 0.35:
                color = ROD_DARK
                img.putpixel((px, py), color)
                if py + 1 < SPRITE_H:
                    img.putpixel((px, py + 1), ROD_MID)
            elif t < 0.65:
                img.putpixel((px, py), ROD_MID)
                if py + 1 < SPRITE_H:
                    img.putpixel((px, py + 1), ROD_LIGHT)
            else:
                # Thin tip section
                img.putpixel((px, py), ROD_LIGHT if t < 0.85 else ROD_MID)

    # Rod outline on top edge (just trace the top pixel row of the curve
    # for the thick section).
    for i in range(int(segments * 0.35) + 1):
        t = i / segments
        mid_x = (rod_start_x + TIP_X) / 2
        mid_y = min(rod_start_y, TIP_Y) - 3
        bx = ((1 - t) ** 2) * rod_start_x + 2 * (1 - t) * t * mid_x + (t ** 2) * TIP_X
        by = ((1 - t) ** 2) * rod_start_y + 2 * (1 - t) * t * mid_y + (t ** 2) * TIP_Y
        px = int(round(bx))
        py = int(round(by)) - 1
        if 0 <= px < SPRITE_W and 0 <= py < SPRITE_H:
            img.putpixel((px, py), OUTLINE)

    # ----- Rod tip guide (small ring at the tip) -----
    if 0 <= TIP_X < SPRITE_W and 0 <= TIP_Y < SPRITE_H:
        img.putpixel((TIP_X, TIP_Y), OUTLINE)
        if TIP_Y - 1 >= 0:
            img.putpixel((TIP_X, TIP_Y - 1), REEL_MID)
        if TIP_X + 1 < SPRITE_W:
            img.putpixel((TIP_X + 1, TIP_Y), OUTLINE)

    # Line, bobber, and hook are drawn procedurally at runtime so the
    # lure can be swapped. The sprite contains only the rod itself.

    return img


def generate_fishing_rod_windup():
    """Generate the wind-up pose: rod swept behind/above the character's head.

    The handle stays near the character's hands (raised), and the rod pole
    extends up and to the LEFT (behind the character who faces right).
    No fishing line, bobber, or hook — those dangle freely during the wind-up.
    """
    img = Image.new("RGBA", (SPRITE_W, SPRITE_H), TRANSPARENT)

    # Wind-up handle position — raised up and slightly left compared to idle.
    wu_handle_x = 10
    wu_handle_y = 22

    # Rod tip — extends up-left (behind head).
    wu_tip_x = 2
    wu_tip_y = 4

    # ----- Cork handle (grip area) -----
    for dy in range(-3, 4):
        y = wu_handle_y + dy
        if 0 <= y < SPRITE_H:
            img.putpixel((wu_handle_x - 1, y), CORK_DARK)
            img.putpixel((wu_handle_x, y), CORK_MID)
            img.putpixel((wu_handle_x + 1, y), CORK_LIGHT)

    # Handle outline (top and bottom)
    for dx in range(-1, 2):
        y_top = wu_handle_y - 4
        y_bot = wu_handle_y + 4
        if 0 <= y_top < SPRITE_H:
            img.putpixel((wu_handle_x + dx, y_top), OUTLINE)
        if 0 <= y_bot < SPRITE_H:
            img.putpixel((wu_handle_x + dx, y_bot), OUTLINE)

    # Handle side outlines
    for dy in range(-3, 4):
        y = wu_handle_y + dy
        if 0 <= y < SPRITE_H:
            img.putpixel((wu_handle_x - 2, y), OUTLINE)
            img.putpixel((wu_handle_x + 2, y), OUTLINE)

    # ----- Reel (small block near handle) -----
    reel_x = wu_handle_x + 3
    reel_y = wu_handle_y + 1
    for dy in range(-1, 3):
        for dx in range(0, 3):
            px = reel_x + dx
            py = reel_y + dy
            if 0 <= px < SPRITE_W and 0 <= py < SPRITE_H:
                if dy == -1 or dy == 2 or dx == 0 or dx == 2:
                    img.putpixel((px, py), OUTLINE)
                else:
                    img.putpixel((px, py), REEL_MID)
    if 0 <= reel_x + 1 < SPRITE_W and 0 <= reel_y < SPRITE_H:
        img.putpixel((reel_x + 1, reel_y), REEL_LIGHT)
    if 0 <= reel_x + 1 < SPRITE_W and reel_y + 3 < SPRITE_H:
        img.putpixel((reel_x + 1, reel_y + 3), OUTLINE)
    if 0 <= reel_x + 1 < SPRITE_W and reel_y + 4 < SPRITE_H:
        img.putpixel((reel_x + 1, reel_y + 4), REEL_DARK)

    # ----- Rod pole (curved from handle up-left to tip) -----
    rod_start_x = wu_handle_x
    rod_start_y = wu_handle_y - 4

    segments = 40
    for i in range(segments + 1):
        t = i / segments
        # Bezier control point — curve it slightly outward (left and up).
        mid_x = (rod_start_x + wu_tip_x) / 2 - 2
        mid_y = min(rod_start_y, wu_tip_y) - 3

        bx = ((1 - t) ** 2) * rod_start_x + 2 * (1 - t) * t * mid_x + (t ** 2) * wu_tip_x
        by = ((1 - t) ** 2) * rod_start_y + 2 * (1 - t) * t * mid_y + (t ** 2) * wu_tip_y

        px = int(round(bx))
        py = int(round(by))

        if 0 <= px < SPRITE_W and 0 <= py < SPRITE_H:
            if t < 0.35:
                img.putpixel((px, py), ROD_DARK)
                if py + 1 < SPRITE_H:
                    img.putpixel((px, py + 1), ROD_MID)
            elif t < 0.65:
                img.putpixel((px, py), ROD_MID)
                if py + 1 < SPRITE_H:
                    img.putpixel((px, py + 1), ROD_LIGHT)
            else:
                img.putpixel((px, py), ROD_LIGHT if t < 0.85 else ROD_MID)

    # Rod outline on top edge for the thick section.
    for i in range(int(segments * 0.35) + 1):
        t = i / segments
        mid_x = (rod_start_x + wu_tip_x) / 2 - 2
        mid_y = min(rod_start_y, wu_tip_y) - 3
        bx = ((1 - t) ** 2) * rod_start_x + 2 * (1 - t) * t * mid_x + (t ** 2) * wu_tip_x
        by = ((1 - t) ** 2) * rod_start_y + 2 * (1 - t) * t * mid_y + (t ** 2) * wu_tip_y
        px = int(round(bx))
        py = int(round(by)) - 1
        if 0 <= px < SPRITE_W and 0 <= py < SPRITE_H:
            img.putpixel((px, py), OUTLINE)

    # ----- Rod tip guide -----
    if 0 <= wu_tip_x < SPRITE_W and 0 <= wu_tip_y < SPRITE_H:
        img.putpixel((wu_tip_x, wu_tip_y), OUTLINE)
        if wu_tip_y - 1 >= 0:
            img.putpixel((wu_tip_x, wu_tip_y - 1), REEL_MID)
        if wu_tip_x - 1 >= 0:
            img.putpixel((wu_tip_x - 1, wu_tip_y), OUTLINE)

    return img


def generate_fishing_rod_cast():
    """Generate the cast-complete pose: rod in front position with a slight
    downward bend at the tip, no line/bobber/hook (lure has been cast out).

    Same handle position as the idle rod, but the pole has a slightly more
    pronounced downward droop since it's no longer supporting the hanging
    lure weight — just the tension of the line going out to the water.
    """
    img = Image.new("RGBA", (SPRITE_W, SPRITE_H), TRANSPARENT)

    # Same handle position as idle.
    cast_handle_x = HANDLE_X
    cast_handle_y = HANDLE_Y

    # Tip droops slightly lower than idle (more bend).
    cast_tip_x = 42
    cast_tip_y = 10

    # ----- Cork handle (grip area) -----
    for dy in range(-3, 4):
        y = cast_handle_y + dy
        if 0 <= y < SPRITE_H:
            img.putpixel((cast_handle_x - 1, y), CORK_DARK)
            img.putpixel((cast_handle_x, y), CORK_MID)
            img.putpixel((cast_handle_x + 1, y), CORK_LIGHT)

    # Handle outline (top and bottom)
    for dx in range(-1, 2):
        y_top = cast_handle_y - 4
        y_bot = cast_handle_y + 4
        if 0 <= y_top < SPRITE_H:
            img.putpixel((cast_handle_x + dx, y_top), OUTLINE)
        if 0 <= y_bot < SPRITE_H:
            img.putpixel((cast_handle_x + dx, y_bot), OUTLINE)

    # Handle side outlines
    for dy in range(-3, 4):
        y = cast_handle_y + dy
        if 0 <= y < SPRITE_H:
            img.putpixel((cast_handle_x - 2, y), OUTLINE)
            img.putpixel((cast_handle_x + 2, y), OUTLINE)

    # ----- Reel (small block near handle) -----
    reel_x = cast_handle_x + 3
    reel_y = cast_handle_y + 1
    for dy in range(-1, 3):
        for dx in range(0, 3):
            px = reel_x + dx
            py = reel_y + dy
            if 0 <= px < SPRITE_W and 0 <= py < SPRITE_H:
                if dy == -1 or dy == 2 or dx == 0 or dx == 2:
                    img.putpixel((px, py), OUTLINE)
                else:
                    img.putpixel((px, py), REEL_MID)
    if 0 <= reel_x + 1 < SPRITE_W and 0 <= reel_y < SPRITE_H:
        img.putpixel((reel_x + 1, reel_y), REEL_LIGHT)
    if 0 <= reel_x + 1 < SPRITE_W and reel_y + 3 < SPRITE_H:
        img.putpixel((reel_x + 1, reel_y + 3), OUTLINE)
    if 0 <= reel_x + 1 < SPRITE_W and reel_y + 4 < SPRITE_H:
        img.putpixel((reel_x + 1, reel_y + 4), REEL_DARK)

    # ----- Rod pole with more pronounced droop (quadratic bezier) -----
    rod_start_x = cast_handle_x + 1
    rod_start_y = cast_handle_y - 4

    # Control point sits lower than the idle version — creates visible bend.
    segments = 40
    for i in range(segments + 1):
        t = i / segments
        mid_x = (rod_start_x + cast_tip_x) / 2
        mid_y = min(rod_start_y, cast_tip_y) + 2  # BELOW the line = droop

        bx = ((1 - t) ** 2) * rod_start_x + 2 * (1 - t) * t * mid_x + (t ** 2) * cast_tip_x
        by = ((1 - t) ** 2) * rod_start_y + 2 * (1 - t) * t * mid_y + (t ** 2) * cast_tip_y

        px = int(round(bx))
        py = int(round(by))

        if 0 <= px < SPRITE_W and 0 <= py < SPRITE_H:
            if t < 0.35:
                img.putpixel((px, py), ROD_DARK)
                if py + 1 < SPRITE_H:
                    img.putpixel((px, py + 1), ROD_MID)
            elif t < 0.65:
                img.putpixel((px, py), ROD_MID)
                if py + 1 < SPRITE_H:
                    img.putpixel((px, py + 1), ROD_LIGHT)
            else:
                img.putpixel((px, py), ROD_LIGHT if t < 0.85 else ROD_MID)

    # Rod outline on top edge for thick section.
    for i in range(int(segments * 0.35) + 1):
        t = i / segments
        mid_x = (rod_start_x + cast_tip_x) / 2
        mid_y = min(rod_start_y, cast_tip_y) + 2
        bx = ((1 - t) ** 2) * rod_start_x + 2 * (1 - t) * t * mid_x + (t ** 2) * cast_tip_x
        by = ((1 - t) ** 2) * rod_start_y + 2 * (1 - t) * t * mid_y + (t ** 2) * cast_tip_y
        px = int(round(bx))
        py = int(round(by)) - 1
        if 0 <= px < SPRITE_W and 0 <= py < SPRITE_H:
            img.putpixel((px, py), OUTLINE)

    # ----- Rod tip guide -----
    if 0 <= cast_tip_x < SPRITE_W and 0 <= cast_tip_y < SPRITE_H:
        img.putpixel((cast_tip_x, cast_tip_y), OUTLINE)
        if cast_tip_y - 1 >= 0:
            img.putpixel((cast_tip_x, cast_tip_y - 1), REEL_MID)
        if cast_tip_x + 1 < SPRITE_W:
            img.putpixel((cast_tip_x + 1, cast_tip_y), OUTLINE)

    # No line, bobber, or hook — lure is out in the water.

    return img


def generate_fishing_rod_hooked():
    """Generate the hooked pose: rod under tension from a fish on the line.

    Same handle position as the cast sprite.  The rod shaft bends upward
    at roughly 45° but with a visible arc/flex — relatively straight near
    the handle, curving more sharply toward the tip to convey load.
    """
    img = Image.new("RGBA", (SPRITE_W, SPRITE_H), TRANSPARENT)

    # Same handle position as cast/idle.
    hk_handle_x = HANDLE_X
    hk_handle_y = HANDLE_Y

    # Rod tip — upper-right area, pulled down slightly by the load.
    hk_tip_x = 36
    hk_tip_y = 6

    # ----- Cork handle (grip area) -----
    for dy in range(-3, 4):
        y = hk_handle_y + dy
        if 0 <= y < SPRITE_H:
            img.putpixel((hk_handle_x - 1, y), CORK_DARK)
            img.putpixel((hk_handle_x, y), CORK_MID)
            img.putpixel((hk_handle_x + 1, y), CORK_LIGHT)

    # Handle outline (top and bottom)
    for dx in range(-1, 2):
        y_top = hk_handle_y - 4
        y_bot = hk_handle_y + 4
        if 0 <= y_top < SPRITE_H:
            img.putpixel((hk_handle_x + dx, y_top), OUTLINE)
        if 0 <= y_bot < SPRITE_H:
            img.putpixel((hk_handle_x + dx, y_bot), OUTLINE)

    # Handle side outlines
    for dy in range(-3, 4):
        y = hk_handle_y + dy
        if 0 <= y < SPRITE_H:
            img.putpixel((hk_handle_x - 2, y), OUTLINE)
            img.putpixel((hk_handle_x + 2, y), OUTLINE)

    # ----- Reel (small block near handle) -----
    reel_x = hk_handle_x + 3
    reel_y = hk_handle_y + 1
    for dy in range(-1, 3):
        for dx in range(0, 3):
            px = reel_x + dx
            py = reel_y + dy
            if 0 <= px < SPRITE_W and 0 <= py < SPRITE_H:
                if dy == -1 or dy == 2 or dx == 0 or dx == 2:
                    img.putpixel((px, py), OUTLINE)
                else:
                    img.putpixel((px, py), REEL_MID)
    if 0 <= reel_x + 1 < SPRITE_W and 0 <= reel_y < SPRITE_H:
        img.putpixel((reel_x + 1, reel_y), REEL_LIGHT)
    if 0 <= reel_x + 1 < SPRITE_W and reel_y + 3 < SPRITE_H:
        img.putpixel((reel_x + 1, reel_y + 3), OUTLINE)
    if 0 <= reel_x + 1 < SPRITE_W and reel_y + 4 < SPRITE_H:
        img.putpixel((reel_x + 1, reel_y + 4), REEL_DARK)

    # ----- Rod pole — cubic bezier for loaded-rod flex -----
    # A cubic bezier gives better control over the progressive bend:
    # straight near the butt, curving sharply near the tip.
    rod_start_x = hk_handle_x + 1  # P0
    rod_start_y = hk_handle_y - 4

    # Control points: cp1 extends steeply upward from the handle (the stiff
    # butt section), cp2 pulls the curve toward the tip (the flexible tip
    # section bending under load).
    cp1_x, cp1_y = 12, 10
    cp2_x, cp2_y = 32, 2

    segments = 50
    for i in range(segments + 1):
        t = i / segments
        u = 1 - t
        # Cubic bezier: B(t) = u³P0 + 3u²tP1 + 3ut²P2 + t³P3
        bx = (u**3 * rod_start_x + 3 * u**2 * t * cp1_x
              + 3 * u * t**2 * cp2_x + t**3 * hk_tip_x)
        by = (u**3 * rod_start_y + 3 * u**2 * t * cp1_y
              + 3 * u * t**2 * cp2_y + t**3 * hk_tip_y)

        px = int(round(bx))
        py = int(round(by))

        if 0 <= px < SPRITE_W and 0 <= py < SPRITE_H:
            if t < 0.35:
                img.putpixel((px, py), ROD_DARK)
                if py + 1 < SPRITE_H:
                    img.putpixel((px, py + 1), ROD_MID)
            elif t < 0.65:
                img.putpixel((px, py), ROD_MID)
                if py + 1 < SPRITE_H:
                    img.putpixel((px, py + 1), ROD_LIGHT)
            else:
                img.putpixel((px, py), ROD_LIGHT if t < 0.85 else ROD_MID)

    # Rod outline on top edge for the thick section.
    for i in range(int(segments * 0.35) + 1):
        t = i / segments
        u = 1 - t
        bx = (u**3 * rod_start_x + 3 * u**2 * t * cp1_x
              + 3 * u * t**2 * cp2_x + t**3 * hk_tip_x)
        by = (u**3 * rod_start_y + 3 * u**2 * t * cp1_y
              + 3 * u * t**2 * cp2_y + t**3 * hk_tip_y)
        px = int(round(bx))
        py = int(round(by)) - 1
        if 0 <= px < SPRITE_W and 0 <= py < SPRITE_H:
            img.putpixel((px, py), OUTLINE)

    # ----- Rod tip guide -----
    if 0 <= hk_tip_x < SPRITE_W and 0 <= hk_tip_y < SPRITE_H:
        img.putpixel((hk_tip_x, hk_tip_y), OUTLINE)
        if hk_tip_y - 1 >= 0:
            img.putpixel((hk_tip_x, hk_tip_y - 1), REEL_MID)
        if hk_tip_x + 1 < SPRITE_W:
            img.putpixel((hk_tip_x + 1, hk_tip_y), OUTLINE)

    return img


def main():
    # Generate idle rod.
    img = generate_fishing_rod()

    output_path = os.path.join(OUTPUT_DIR, "fishing_rod.png")
    img.save(output_path)
    print(f"Saved: {output_path}")

    content_path = os.path.join(CONTENT_SPRITES, "fishing_rod.png")
    shutil.copy2(output_path, content_path)
    print(f"Copied to: {content_path}")

    # Generate wind-up rod.
    windup_img = generate_fishing_rod_windup()

    windup_output_path = os.path.join(OUTPUT_DIR, "fishing_rod_windup.png")
    windup_img.save(windup_output_path)
    print(f"Saved: {windup_output_path}")

    windup_content_path = os.path.join(CONTENT_SPRITES, "fishing_rod_windup.png")
    shutil.copy2(windup_output_path, windup_content_path)
    print(f"Copied to: {windup_content_path}")

    # Generate cast-complete rod.
    cast_img = generate_fishing_rod_cast()

    cast_output_path = os.path.join(OUTPUT_DIR, "fishing_rod_cast.png")
    cast_img.save(cast_output_path)
    print(f"Saved: {cast_output_path}")

    cast_content_path = os.path.join(CONTENT_SPRITES, "fishing_rod_cast.png")
    shutil.copy2(cast_output_path, cast_content_path)
    print(f"Copied to: {cast_content_path}")

    # Generate hooked rod (under tension).
    hooked_img = generate_fishing_rod_hooked()

    hooked_output_path = os.path.join(OUTPUT_DIR, "fishing_rod_hooked.png")
    hooked_img.save(hooked_output_path)
    print(f"Saved: {hooked_output_path}")

    hooked_content_path = os.path.join(CONTENT_SPRITES, "fishing_rod_hooked.png")
    shutil.copy2(hooked_output_path, hooked_content_path)
    print(f"Copied to: {hooked_content_path}")


if __name__ == "__main__":
    main()
