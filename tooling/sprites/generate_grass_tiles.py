"""
Generate grass tile variants for the RiverRats tileset.

All tiles are 32×32 pixels. The base green matches the existing palette.
Decorations are kept small relative to the character (16×22 art in a 32×32 frame),
so grass blades, sprouts, and flowers look like realistic ground-level detail.

Output tiles:
  grass1.png  — plain grass (solid fill)
  grass2.png  — subtle short grass blades
  grass3.png  — a few more grass blades
  grass4.png  — small leaf/sprout cluster
  grass5.png  — small flower with leaves

Color palette (matches existing tiles):
  Base grass:   (83, 138, 11)
  Dark blades:  (75, 91, 47)
  Light leaf:   (161, 223, 80)
  Pale leaf:    (197, 246, 132)
  Flower:       (217, 87, 99)
"""

from PIL import Image
import os
import shutil

TILE_SIZE = 32

# Colors
BASE        = (83, 138, 11, 255)
DARK_BLADE  = (75, 91, 47, 255)
LIGHT_LEAF  = (161, 223, 80, 255)
PALE_LEAF   = (197, 246, 132, 255)
FLOWER      = (217, 87, 99, 255)

SCRIPT_DIR  = os.path.dirname(os.path.abspath(__file__))
OUTPUT_DIR  = os.path.join(SCRIPT_DIR, "output", "tiles")
CONTENT_DIR = os.path.join(SCRIPT_DIR, "..", "..", "src", "RiverRats.Game",
                           "Content", "Tilesets")

os.makedirs(OUTPUT_DIR, exist_ok=True)


def new_tile():
    """Create a 32×32 image filled with the base grass color."""
    img = Image.new("RGBA", (TILE_SIZE, TILE_SIZE), BASE)
    return img


def put(img, x, y, color):
    """Set a pixel if within bounds."""
    if 0 <= x < TILE_SIZE and 0 <= y < TILE_SIZE:
        img.putpixel((x, y), color)


def draw_blade(img, x, y, color=DARK_BLADE):
    """Draw a single 1×2 or 1×3 grass blade anchored at (x, y) going up."""
    put(img, x, y, color)
    put(img, x, y - 1, color)


def draw_tall_blade(img, x, y, color=DARK_BLADE):
    """Draw a 1×3 grass blade."""
    put(img, x, y, color)
    put(img, x, y - 1, color)
    put(img, x, y - 2, color)


def draw_v_blade(img, x, y, color=DARK_BLADE):
    """Draw a tiny 3×2 V-shaped grass mark."""
    put(img, x, y, color)
    put(img, x + 2, y, color)
    put(img, x + 1, y - 1, color)


# ---------------------------------------------------------------------------
# grass1 : plain solid fill
# ---------------------------------------------------------------------------
def make_grass1():
    return new_tile()


# ---------------------------------------------------------------------------
# grass2 : a few subtle short blades scattered around
# ---------------------------------------------------------------------------
def make_grass2():
    img = new_tile()
    # Scatter some short blades
    positions = [(6, 28), (14, 25), (22, 30), (9, 18), (26, 20), (3, 12), (19, 14)]
    for bx, by in positions:
        draw_blade(img, bx, by)
    return img


# ---------------------------------------------------------------------------
# grass3 : more blades + a couple of V-marks
# ---------------------------------------------------------------------------
def make_grass3():
    img = new_tile()
    blade_positions = [
        (5, 29), (11, 26), (20, 28), (27, 24), (8, 16), (23, 13),
        (3, 22), (15, 19), (29, 18), (17, 10),
    ]
    for bx, by in blade_positions:
        draw_blade(img, bx, by)
    # A couple of V-marks
    draw_v_blade(img, 12, 30)
    draw_v_blade(img, 24, 22)
    return img


# ---------------------------------------------------------------------------
# grass4 : small sprout / leaf cluster (4-6px tall)
# ---------------------------------------------------------------------------
def make_grass4():
    img = new_tile()
    # Background blades (sparse)
    for bx, by in [(5, 29), (25, 27), (8, 16), (28, 14)]:
        draw_blade(img, bx, by)

    # Small sprout centered around (15, 26): a tiny plant ~5px tall
    # Stem (2px tall)
    put(img, 15, 28, DARK_BLADE)
    put(img, 15, 27, DARK_BLADE)
    put(img, 15, 26, DARK_BLADE)
    # Small leaves
    put(img, 14, 27, LIGHT_LEAF)
    put(img, 16, 26, LIGHT_LEAF)
    put(img, 14, 26, PALE_LEAF)

    # Another small sprout at a different spot
    put(img, 24, 20, DARK_BLADE)
    put(img, 24, 19, DARK_BLADE)
    put(img, 23, 19, LIGHT_LEAF)
    put(img, 25, 20, LIGHT_LEAF)

    return img


# ---------------------------------------------------------------------------
# grass5 : small flower with tiny leaves (~6px tall)
# ---------------------------------------------------------------------------
def make_grass5():
    img = new_tile()
    # Background blades
    for bx, by in [(4, 28), (22, 26), (10, 15), (28, 18)]:
        draw_blade(img, bx, by)

    # Small flower at (14, 25): stem + leaves + flower bud
    # Stem
    put(img, 14, 28, DARK_BLADE)
    put(img, 14, 27, DARK_BLADE)
    put(img, 14, 26, DARK_BLADE)
    put(img, 14, 25, DARK_BLADE)
    # Leaves
    put(img, 13, 27, LIGHT_LEAF)
    put(img, 15, 26, LIGHT_LEAF)
    put(img, 13, 26, PALE_LEAF)
    # Flower head (small 3×2 cluster)
    put(img, 13, 24, FLOWER)
    put(img, 14, 24, FLOWER)
    put(img, 15, 24, FLOWER)
    put(img, 14, 23, FLOWER)

    # A second smaller flower bud
    put(img, 24, 21, DARK_BLADE)
    put(img, 24, 20, DARK_BLADE)
    put(img, 24, 19, DARK_BLADE)
    put(img, 23, 20, LIGHT_LEAF)
    put(img, 25, 19, LIGHT_LEAF)
    put(img, 24, 18, FLOWER)
    put(img, 23, 18, FLOWER)

    return img


# ---------------------------------------------------------------------------
# Generate and save
# ---------------------------------------------------------------------------
def main():
    tiles = {
        "grass1.png": make_grass1(),
        "grass2.png": make_grass2(),
        "grass3.png": make_grass3(),
        "grass4.png": make_grass4(),
        "grass5.png": make_grass5(),
    }

    for name, img in tiles.items():
        out_path = os.path.join(OUTPUT_DIR, name)
        img.save(out_path)
        print(f"  saved {out_path}")

    # Copy to Content/Tilesets
    for name in tiles:
        src = os.path.join(OUTPUT_DIR, name)
        dst = os.path.join(CONTENT_DIR, name)
        shutil.copy2(src, dst)
        print(f"  copied -> {dst}")

    print("\nDone. All 5 grass tiles regenerated.")


if __name__ == "__main__":
    main()
