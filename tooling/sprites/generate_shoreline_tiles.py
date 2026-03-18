"""
Generate 8 shoreline tile variants for the RiverRats tileset.

All tiles are 32x32 pixels. Each tile represents a north-facing shoreline:
grass at the top, a muddy bank in the middle, and transparent pixels at the
bottom where the water layer shows through.

Variants share identical left/right edge profiles (columns 0-1 and 30-31)
so any tile can be placed next to any other tile seamlessly.

Output tiles:
    shoreline-1.png  - nearly flat grass edge, silt accents
    shoreline-2.png  - two scallops in grass edge, pebble accents
    shoreline-3.png  - single wide bank intrusion, shell accents
    shoreline-4.png  - left-side grass dip, root accents
    shoreline-5.png  - right-side grass dip, mixed accents
    shoreline-6.png  - gentle wave edge, grass tuft accents
    shoreline-7.png  - grass peninsula extending down, narrow bank
    shoreline-8.png  - irregular edge, mixed debris

Usage: run this script from the tooling/sprites folder.
"""

from PIL import Image
import math
import os
import shutil

TILE_SIZE = 32
EDGE_COLS = 2  # columns locked to edge profile on each side

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
OUTPUT_DIR = os.path.join(SCRIPT_DIR, "output", "tiles")
CONTENT_DIR = os.path.join(
    SCRIPT_DIR, "..", "..", "src", "RiverRats.Game", "Content", "Tilesets"
)

os.makedirs(OUTPUT_DIR, exist_ok=True)

# ----------------------------------------------------------------
# Palette (RGBA) — matches existing shoreline-1.png / riverbed tiles
# ----------------------------------------------------------------
TRANSPARENT = (0, 0, 0, 0)
GRASS = (83, 138, 11, 255)
GRASS_DARK = (68, 116, 9, 255)
MUD_BASE = (108, 88, 66, 255)
MUD_SHADE = (89, 70, 52, 255)
MUD_DARK = (67, 51, 37, 255)
SILT = (124, 102, 77, 255)
SILT_LIGHT = (136, 114, 87, 255)
STONE_LIGHT = (163, 154, 142, 255)
STONE_MID = (126, 118, 109, 255)
STONE_DARK = (91, 85, 79, 255)
SHELL_LIGHT = (206, 194, 173, 255)
SHELL_SHADE = (180, 165, 143, 255)
SHELL_DARK = (151, 136, 118, 255)
ROOT = (86, 74, 53, 255)
ROOT_DARK = (75, 60, 42, 255)

# ----------------------------------------------------------------
# Edge profile — shared by ALL variants at columns 0-1 and 30-31
# ----------------------------------------------------------------
EDGE_GRASS_Y = 14  # last grass row on edge columns
EDGE_BANK_Y = 22   # last bank row on edge columns


def edge_color(y):
    """Return the pixel color for the shared edge profile at row y."""
    if y <= EDGE_GRASS_Y:
        return GRASS
    if y == EDGE_GRASS_Y + 1:
        return MUD_SHADE
    if y <= EDGE_BANK_Y:
        return MUD_BASE
    return TRANSPARENT


# ----------------------------------------------------------------
# Pixel helpers
# ----------------------------------------------------------------
def put(img, x, y, color):
    """Set a pixel if within tile bounds."""
    if 0 <= x < TILE_SIZE and 0 <= y < TILE_SIZE:
        img.putpixel((x, y), color)


def draw_pebble(img, x, y):
    """2x2 pebble with highlight."""
    put(img, x, y, STONE_DARK)
    put(img, x + 1, y, STONE_MID)
    put(img, x, y + 1, STONE_MID)
    put(img, x + 1, y + 1, STONE_LIGHT)


def draw_small_stone(img, x, y):
    """2x2 stone, darker than pebble."""
    put(img, x, y, STONE_DARK)
    put(img, x + 1, y, STONE_DARK)
    put(img, x, y + 1, STONE_MID)
    put(img, x + 1, y + 1, STONE_DARK)


def draw_shell_bit(img, x, y):
    """Small shell fragment."""
    put(img, x, y, SHELL_SHADE)
    put(img, x + 1, y, SHELL_LIGHT)
    put(img, x, y + 1, SHELL_DARK)


def draw_root(img, x, y, length=3):
    """Horizontal root-like detail."""
    for i in range(length):
        put(img, x + i, y + (i % 2), ROOT if i % 2 == 0 else ROOT_DARK)


def draw_grass_tuft(img, x, y):
    """Small grass tuft growing in the dirt."""
    put(img, x, y, GRASS_DARK)
    put(img, x + 1, y, GRASS)
    put(img, x, y + 1, GRASS)


def draw_silt_patch(img, x, y):
    """3-pixel silt accent."""
    put(img, x, y, SILT_LIGHT)
    put(img, x + 1, y, SILT)
    put(img, x + 1, y + 1, SILT_LIGHT)


# ----------------------------------------------------------------
# Curve builders
# ----------------------------------------------------------------
def full_curve(interior, edge_val):
    """
    Build a 32-value curve from 28 interior values (columns 2-29).
    Columns 0-1 and 30-31 are set to edge_val.
    """
    assert len(interior) == 28, f"Expected 28 interior values, got {len(interior)}"
    return [edge_val, edge_val] + list(interior) + [edge_val, edge_val]


def flat(val, n=28):
    """N identical values."""
    return [val] * n


def dipped(base, dips, n=28):
    """
    Base value with one or more dips (bank eats into grass = lower y).
    dips: list of (center_i, radius, depth).
    """
    vals = [base] * n
    for cx, r, d in dips:
        for i in range(n):
            dist = abs(i - cx)
            if dist <= r:
                v = base - round(d * (1.0 - dist / r))
                vals[i] = min(vals[i], v)
    return vals


def bumped(base, bumps, n=28):
    """
    Base value with one or more bumps (grass extends further = higher y).
    bumps: list of (center_i, radius, height).
    """
    vals = [base] * n
    for cx, r, h in bumps:
        for i in range(n):
            dist = abs(i - cx)
            if dist <= r:
                v = base + round(h * (1.0 - dist / r))
                vals[i] = max(vals[i], v)
    return vals


def wavy(base, amplitude, period, n=28):
    """Sine-wave modulated values."""
    return [
        int(round(base + amplitude * math.sin(2 * math.pi * i / period)))
        for i in range(n)
    ]


# ----------------------------------------------------------------
# Variant curve definitions
# ----------------------------------------------------------------

# Grass curves — last row painted as grass per column
GRASS_CURVES = [
    # V1: Nearly flat
    full_curve(flat(14), EDGE_GRASS_Y),
    # V2: Two scallops where bank pushes into grass
    full_curve(dipped(14, [(6, 4, 2), (20, 4, 2)]), EDGE_GRASS_Y),
    # V3: Single wide bank intrusion at center
    full_curve(dipped(14, [(14, 7, 3)]), EDGE_GRASS_Y),
    # V4: Left-side bank intrusion
    full_curve(dipped(14, [(6, 5, 3)]), EDGE_GRASS_Y),
    # V5: Right-side bank intrusion
    full_curve(dipped(14, [(21, 5, 3)]), EDGE_GRASS_Y),
    # V6: Gentle wave
    full_curve(wavy(14, 1.5, 14), EDGE_GRASS_Y),
    # V7: Grass peninsula extends down in center
    full_curve(bumped(14, [(14, 8, 3)]), EDGE_GRASS_Y),
    # V8: Irregular bumps and dips
    full_curve(dipped(14, [(4, 3, 2), (13, 2, 1), (22, 3, 2)]), EDGE_GRASS_Y),
]

# Bank curves — last opaque row per column (below this = transparent)
BANK_CURVES = [
    # V1: Flat bank bottom
    full_curve(flat(22), EDGE_BANK_Y),
    # V2: Center undercut
    full_curve(dipped(22, [(14, 10, 3)]), EDGE_BANK_Y),
    # V3: Center undercut
    full_curve(dipped(22, [(14, 8, 2)]), EDGE_BANK_Y),
    # V4: Slight left undercut
    full_curve(dipped(22, [(8, 6, 2)]), EDGE_BANK_Y),
    # V5: Slight right undercut
    full_curve(dipped(22, [(19, 6, 2)]), EDGE_BANK_Y),
    # V6: Wavy bottom
    full_curve(wavy(21, 1, 16), EDGE_BANK_Y),
    # V7: Flat (bank is already narrow due to grass extending down)
    full_curve(flat(22), EDGE_BANK_Y),
    # V8: Center undercut
    full_curve(dipped(22, [(14, 7, 2)]), EDGE_BANK_Y),
]


# ----------------------------------------------------------------
# Detail functions (per variant)
# ----------------------------------------------------------------
def details_v1(img):
    """Plain: subtle silt patches."""
    draw_silt_patch(img, 8, 17)
    draw_silt_patch(img, 20, 19)


def details_v2(img):
    """Pebbles in bank under scallops."""
    draw_pebble(img, 7, 17)
    draw_pebble(img, 22, 17)
    draw_silt_patch(img, 14, 18)


def details_v3(img):
    """Shell and stone in wide intrusion."""
    draw_small_stone(img, 12, 15)
    draw_shell_bit(img, 18, 16)
    draw_silt_patch(img, 24, 18)


def details_v4(img):
    """Roots at left grass edge."""
    draw_root(img, 5, 17, 4)
    draw_pebble(img, 20, 18)


def details_v5(img):
    """Shell and silt on right side."""
    draw_shell_bit(img, 22, 17)
    draw_silt_patch(img, 8, 18)


def details_v6(img):
    """Grass tufts along wavy edge."""
    draw_grass_tuft(img, 10, 17)
    draw_grass_tuft(img, 22, 17)


def details_v7(img):
    """Grass tufts at peninsula edges."""
    draw_grass_tuft(img, 6, 19)
    draw_grass_tuft(img, 24, 19)
    draw_silt_patch(img, 15, 20)


def details_v8(img):
    """Mixed debris in irregular bank."""
    draw_pebble(img, 5, 17)
    draw_shell_bit(img, 14, 18)
    draw_root(img, 20, 18, 3)


DETAIL_FNS = [
    details_v1, details_v2, details_v3, details_v4,
    details_v5, details_v6, details_v7, details_v8,
]


# ----------------------------------------------------------------
# Tile builder
# ----------------------------------------------------------------
def make_shoreline(grass_curve, bank_curve, details_fn):
    """
    Build a shoreline tile.

    grass_curve: 32-value list, last row painted as grass per column.
    bank_curve:  32-value list, last row painted as bank per column.
    details_fn:  callable(img) that draws variant-specific accents.
    """
    img = Image.new("RGBA", (TILE_SIZE, TILE_SIZE), TRANSPARENT)

    for x in range(TILE_SIZE):
        g_end = grass_curve[x]
        b_end = bank_curve[x]

        for y in range(TILE_SIZE):
            if y <= g_end:
                img.putpixel((x, y), GRASS)
            elif y == g_end + 1:
                # 1px dirt transition
                img.putpixel((x, y), MUD_SHADE)
            elif y <= b_end:
                # Mottled bank fill
                val = ((x * 17) + (y * 11) + (x * y * 3)) % 7
                if val == 0:
                    color = SILT
                elif val == 1:
                    color = MUD_SHADE
                else:
                    color = MUD_BASE
                img.putpixel((x, y), color)
            # else: stays transparent

    # Enforce edge strips (columns 0-1 and 30-31)
    for y in range(TILE_SIZE):
        ec = edge_color(y)
        for col in range(EDGE_COLS):
            img.putpixel((col, y), ec)
            img.putpixel((TILE_SIZE - 1 - col, y), ec)

    # Apply variant details
    details_fn(img)

    return img


# ----------------------------------------------------------------
# Main
# ----------------------------------------------------------------
def main():
    for i in range(8):
        name = f"shoreline-{i + 1}.png"
        img = make_shoreline(GRASS_CURVES[i], BANK_CURVES[i], DETAIL_FNS[i])
        out_path = os.path.join(OUTPUT_DIR, name)
        img.save(out_path)
        print(f"  saved {out_path}")

    for i in range(8):
        name = f"shoreline-{i + 1}.png"
        src = os.path.join(OUTPUT_DIR, name)
        dst = os.path.join(CONTENT_DIR, name)
        shutil.copy2(src, dst)
        print(f"  copied -> {dst}")


if __name__ == "__main__":
    main()
