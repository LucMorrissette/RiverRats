"""
Generate muddy riverbed tile variants for the RiverRats tileset.

All tiles are 32x32 pixels and use restrained pixel details so they read as
walkable terrain rather than props. Variants include a few pebbles, subtle
freshwater shell traces, light drift debris, and darker silt pockets.

Output tiles:
    riverbed1.png  - plain muddy bed with sparse pebbles
    riverbed2.png  - shell-led variation with light stone support
    riverbed3.png  - darker silt-led variation with minimal fragments
    riverbed4.png  - mixed debris and pebble scatter
    riverbed5.png  - plainer open mud with soft pockets
    riverbed6.png  - shell-led muddy bed with restrained stones
    riverbed7.png  - darker muddy pockets with minimal debris
    riverbed8.png  - mixed accents over open mud
    riverbed9.png  - plain mud with offset silt breakup
    riverbed10.png - shell-heavy variation with a darker pocket
    riverbed11.png - darker soft bed with shell fragments
    riverbed12.png - mixed silt and restrained reed debris
    riverbed13.png - broad plain mud with sparse stone accents
    riverbed14.png - shell-heavy variation with subtle silt breakup
    riverbed15.png - darker bed with diagonal pebble rhythm
    riverbed16.png - balanced mixed variant with soft mud pockets

Usage: run this script from the tooling/sprites folder or via the repo root.
"""

from PIL import Image
import os
import shutil

TILE_SIZE = 32

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
OUTPUT_DIR = os.path.join(SCRIPT_DIR, "output", "tiles")
CONTENT_DIR = os.path.join(
    SCRIPT_DIR,
    "..",
    "..",
    "src",
    "RiverRats.Game",
    "Content",
    "Tilesets",
)

os.makedirs(OUTPUT_DIR, exist_ok=True)

# Palette (RGBA)
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
REED = (112, 98, 72, 255)
REED_DARK = (86, 74, 53, 255)


def new_tile():
    """Create a muddy base tile with subtle mottled variation."""
    img = Image.new("RGBA", (TILE_SIZE, TILE_SIZE), MUD_BASE)

    for y in range(TILE_SIZE):
        for x in range(TILE_SIZE):
            value = ((x * 17) + (y * 11) + (x * y * 3)) % 9
            color = MUD_BASE
            if value in (0, 1):
                color = SILT
            elif value == 2:
                color = MUD_SHADE
            elif value == 3 and (x + y) % 5 == 0:
                color = SILT_LIGHT

            img.putpixel((x, y), color)

    return img


def put(img, x, y, color):
    """Set a pixel if within bounds."""
    if 0 <= x < TILE_SIZE and 0 <= y < TILE_SIZE:
        img.putpixel((x, y), color)


def draw_silt_patch(img, x, y):
    """Draw a soft-edged darker silt pocket."""
    points = [
        (0, 0, MUD_DARK),
        (1, 0, MUD_SHADE),
        (2, 0, MUD_DARK),
        (0, 1, MUD_SHADE),
        (1, 1, MUD_DARK),
        (2, 1, MUD_SHADE),
        (1, 2, MUD_SHADE),
    ]

    for dx, dy, color in points:
        put(img, x + dx, y + dy, color)


def draw_pebble(img, x, y, flip=False):
    """Draw a small 3x2 pebble with a highlight."""
    if not flip:
        points = [
            (0, 0, STONE_DARK),
            (1, 0, STONE_MID),
            (2, 0, STONE_DARK),
            (0, 1, STONE_DARK),
            (1, 1, STONE_LIGHT),
            (2, 1, STONE_MID),
        ]
    else:
        points = [
            (0, 0, STONE_DARK),
            (1, 0, STONE_MID),
            (2, 0, STONE_DARK),
            (0, 1, STONE_MID),
            (1, 1, STONE_LIGHT),
            (2, 1, STONE_DARK),
        ]

    for dx, dy, color in points:
        put(img, x + dx, y + dy, color)


def draw_small_stone(img, x, y):
    """Draw a tiny 2x2 stone fleck."""
    put(img, x, y, STONE_DARK)
    put(img, x + 1, y, STONE_MID)
    put(img, x, y + 1, STONE_MID)
    put(img, x + 1, y + 1, STONE_LIGHT)


def draw_clamshell(img, x, y, open_right=True):
    """Draw a small freshwater clamshell or shell fragment."""
    if open_right:
        points = [
            (0, 1, SHELL_DARK),
            (1, 0, SHELL_SHADE),
            (1, 1, SHELL_LIGHT),
            (2, 0, SHELL_LIGHT),
            (2, 1, SHELL_SHADE),
            (3, 1, SHELL_DARK),
        ]
    else:
        points = [
            (0, 1, SHELL_DARK),
            (1, 0, SHELL_LIGHT),
            (1, 1, SHELL_SHADE),
            (2, 0, SHELL_SHADE),
            (2, 1, SHELL_LIGHT),
            (3, 1, SHELL_DARK),
        ]

    for dx, dy, color in points:
        put(img, x + dx, y + dy, color)


def draw_shell_fragment(img, x, y):
    """Draw a tiny broken shell chip."""
    put(img, x, y, SHELL_SHADE)
    put(img, x + 1, y, SHELL_LIGHT)
    put(img, x, y + 1, SHELL_DARK)


def draw_reed_bits(img, x, y):
    """Draw a small piece of water-worn plant debris."""
    put(img, x, y + 1, REED_DARK)
    put(img, x + 1, y, REED)
    put(img, x + 1, y + 1, REED_DARK)
    put(img, x + 2, y, REED)


def make_riverbed1():
    img = new_tile()

    for px, py in [(4, 6), (15, 14)]:
        draw_pebble(img, px, py, flip=(px + py) % 2 == 0)

    for px, py in [(18, 4,)]:
        draw_small_stone(img, px, py)

    for px, py in [(2, 19), (20, 12)]:
        draw_silt_patch(img, px, py)

    return img


def make_riverbed2():
    img = new_tile()

    draw_small_stone(img, 26, 10)

    draw_clamshell(img, 13, 11, open_right=True)
    draw_shell_fragment(img, 22, 5)
    draw_shell_fragment(img, 8, 21)

    for px, py in [(3, 24)]:
        draw_silt_patch(img, px, py)

    return img


def make_riverbed3():
    img = new_tile()

    for px, py in [(5, 5), (16, 8), (24, 13), (20, 23)]:
        draw_silt_patch(img, px, py)

    for px, py in [(11, 4)]:
        draw_small_stone(img, px, py)

    draw_shell_fragment(img, 19, 12)

    return img


def make_riverbed4():
    img = new_tile()

    for px, py in [(3, 8), (26, 19)]:
        draw_pebble(img, px, py, flip=(py % 2 == 0))

    draw_reed_bits(img, 14, 13)
    draw_shell_fragment(img, 7, 16)
    draw_shell_fragment(img, 24, 4)
    draw_silt_patch(img, 1, 27)

    return img


def make_riverbed5():
    img = new_tile()

    for px, py in [(7, 10), (23, 21)]:
        draw_silt_patch(img, px, py)

    draw_small_stone(img, 17, 6)

    return img


def make_riverbed6():
    img = new_tile()

    for px, py in [(5, 8), (25, 24)]:
        draw_small_stone(img, px, py)

    draw_clamshell(img, 15, 5, open_right=True)
    draw_clamshell(img, 18, 20, open_right=False)
    draw_shell_fragment(img, 9, 15)
    draw_silt_patch(img, 2, 26)

    return img


def make_riverbed7():
    img = new_tile()

    for px, py in [(4, 5), (13, 17), (24, 9), (21, 25)]:
        draw_silt_patch(img, px, py)

    draw_reed_bits(img, 8, 24)
    draw_shell_fragment(img, 26, 15)

    return img


def make_riverbed8():
    img = new_tile()

    for px, py in [(6, 6)]:
        draw_small_stone(img, px, py)

    draw_pebble(img, 14, 12, flip=True)
    draw_shell_fragment(img, 18, 25)
    draw_silt_patch(img, 1, 15)

    return img


def make_riverbed9():
    img = new_tile()

    for px, py in [(4, 14), (21, 6), (24, 24)]:
        draw_silt_patch(img, px, py)

    draw_small_stone(img, 18, 11)

    return img


def make_riverbed10():
    img = new_tile()

    for px, py in [(5, 6), (25, 9)]:
        draw_small_stone(img, px, py)

    draw_clamshell(img, 15, 17, open_right=True)
    draw_shell_fragment(img, 22, 24)
    draw_shell_fragment(img, 10, 26)
    draw_silt_patch(img, 1, 26)

    return img


def make_riverbed11():
    img = new_tile()

    for px, py in [(7, 8), (20, 22), (24, 14)]:
        draw_silt_patch(img, px, py)

    draw_shell_fragment(img, 14, 13)
    draw_shell_fragment(img, 25, 6)
    draw_shell_fragment(img, 10, 26)

    return img


def make_riverbed12():
    img = new_tile()

    for px, py in [(3, 4), (11, 18), (24, 12), (19, 26)]:
        draw_silt_patch(img, px, py)

    draw_reed_bits(img, 7, 24)
    draw_pebble(img, 26, 7, flip=True)

    return img


def make_riverbed13():
    img = new_tile()

    for px, py in [(6, 6), (23, 19)]:
        draw_small_stone(img, px, py)

    draw_silt_patch(img, 2, 21)

    return img


def make_riverbed14():
    img = new_tile()

    draw_clamshell(img, 8, 10, open_right=False)
    draw_clamshell(img, 19, 20, open_right=True)
    draw_shell_fragment(img, 25, 8)
    draw_shell_fragment(img, 12, 25)

    for px, py in [(3, 25), (22, 4)]:
        draw_silt_patch(img, px, py)

    return img


def make_riverbed15():
    img = new_tile()

    for px, py in [(4, 7), (11, 14), (18, 21), (25, 28)]:
        draw_pebble(img, px, py, flip=True)

    draw_silt_patch(img, 20, 5)
    draw_silt_patch(img, 7, 24)

    return img


def make_riverbed16():
    img = new_tile()

    for px, py in [(5, 5), (17, 11), (24, 23)]:
        draw_silt_patch(img, px, py)

    draw_pebble(img, 10, 19, flip=False)
    draw_small_stone(img, 21, 8)
    draw_clamshell(img, 14, 26, open_right=False)

    return img


def main():
    tiles = {
        "riverbed1.png": make_riverbed1(),
        "riverbed2.png": make_riverbed2(),
        "riverbed3.png": make_riverbed3(),
        "riverbed4.png": make_riverbed4(),
        "riverbed5.png": make_riverbed5(),
        "riverbed6.png": make_riverbed6(),
        "riverbed7.png": make_riverbed7(),
        "riverbed8.png": make_riverbed8(),
        "riverbed9.png": make_riverbed9(),
        "riverbed10.png": make_riverbed10(),
        "riverbed11.png": make_riverbed11(),
        "riverbed12.png": make_riverbed12(),
        "riverbed13.png": make_riverbed13(),
        "riverbed14.png": make_riverbed14(),
        "riverbed15.png": make_riverbed15(),
        "riverbed16.png": make_riverbed16(),
    }

    for name, img in tiles.items():
        out_path = os.path.join(OUTPUT_DIR, name)
        img.save(out_path)
        print(f"  saved {out_path}")

    for name in tiles:
        src = os.path.join(OUTPUT_DIR, name)
        dst = os.path.join(CONTENT_DIR, name)
        shutil.copy2(src, dst)
        print(f"  copied -> {dst}")


if __name__ == "__main__":
    main()