"""
Generate a 32x32 pixel-art fishing icon showing a rod with a dangling fish.

The design mirrors the reference silhouette: an L-shaped rod (vertical shaft
bending right then angling down), a thin line dropping from the rod tip, and
a stylised fish at the end of the line. Drawn as a dark navy silhouette on a
transparent background.

Usage:
    python tooling/sprites/generate_fishing_icon.py

Output:
    tooling/sprites/output/fishing-icon.png
    src/DogDays.Game/Content/Sprites/fishing-icon.png
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
# Canvas
# ---------------------------------------------------------------------------
W, H = 32, 32

# ---------------------------------------------------------------------------
# Palette
# ---------------------------------------------------------------------------
T = (0, 0, 0, 0)                # transparent
NAVY = (25, 35, 60, 255)        # dark navy fill (matches reference)


def generate() -> None:
    img = Image.new("RGBA", (W, H), T)
    draw = ImageDraw.Draw(img)

    # -----------------------------------------------------------------------
    # Rod — vertical shaft, horizontal top, angled tip
    # -----------------------------------------------------------------------
    rod_thickness = 3

    # Vertical shaft: bottom-left area going up
    shaft_x = 7
    shaft_bottom = 28
    shaft_top = 5
    draw.rectangle(
        [shaft_x, shaft_top, shaft_x + rod_thickness - 1, shaft_bottom],
        fill=NAVY,
    )

    # Horizontal top bar: extends right from the shaft top
    bar_y = shaft_top
    bar_right = 19
    draw.rectangle(
        [shaft_x, bar_y, bar_right, bar_y + rod_thickness - 1],
        fill=NAVY,
    )

    # Angled tip: angles downward from the end of the bar to a tip point
    tip_x = 22
    tip_y = 12
    # Draw as a filled polygon (tapered arm going right-downward)
    draw.polygon(
        [
            (bar_right - 1, bar_y),
            (bar_right - 1, bar_y + rod_thickness - 1),
            (tip_x + 1, tip_y + 1),
            (tip_x + 1, tip_y - 1),
        ],
        fill=NAVY,
    )

    # Small reel nub on the shaft just below the bend
    reel_y = shaft_top + rod_thickness + 1
    draw.rectangle(
        [shaft_x - 2, reel_y, shaft_x - 1, reel_y + 3],
        fill=NAVY,
    )

    # -----------------------------------------------------------------------
    # Fishing line — thin vertical line from the rod tip down to the fish
    # -----------------------------------------------------------------------
    line_x = tip_x + 1
    line_top = tip_y + 1
    line_bottom = 17
    draw.line(
        [(line_x, line_top), (line_x, line_bottom)],
        fill=NAVY,
        width=1,
    )

    # -----------------------------------------------------------------------
    # Fish — stylised teardrop body + tail fins
    # -----------------------------------------------------------------------
    # Body: a filled ellipse (slightly taller than wide, pointed at top)
    fish_cx = line_x
    fish_top = line_bottom + 1
    fish_body_w = 4   # half-width
    fish_body_h = 7   # full height of body ellipse
    draw.ellipse(
        [
            fish_cx - fish_body_w,
            fish_top,
            fish_cx + fish_body_w,
            fish_top + fish_body_h,
        ],
        fill=NAVY,
    )

    # Tail fin — two small triangles flaring out at the bottom
    tail_top_y = fish_top + fish_body_h - 1
    tail_bot_y = tail_top_y + 5
    # Left fin
    draw.polygon(
        [
            (fish_cx, tail_top_y),
            (fish_cx - 4, tail_bot_y),
            (fish_cx, tail_bot_y - 1),
        ],
        fill=NAVY,
    )
    # Right fin
    draw.polygon(
        [
            (fish_cx, tail_top_y),
            (fish_cx + 4, tail_bot_y),
            (fish_cx, tail_bot_y - 1),
        ],
        fill=NAVY,
    )

    # Eye — a single transparent pixel punched out of the body
    eye_x = fish_cx
    eye_y = fish_top + 3
    img.putpixel((eye_x, eye_y), T)

    # -----------------------------------------------------------------------
    # Save
    # -----------------------------------------------------------------------
    out_path = os.path.join(OUTPUT_DIR, "fishing-icon.png")
    img.save(out_path)
    print(f"Saved {out_path} ({W}x{H})")

    content_path = os.path.join(CONTENT_SPRITES, "fishing-icon.png")
    os.makedirs(CONTENT_SPRITES, exist_ok=True)
    shutil.copy2(out_path, content_path)
    print(f"Copied to {content_path}")


if __name__ == "__main__":
    generate()
