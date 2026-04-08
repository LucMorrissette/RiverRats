"""
Generate a 16x16 pixel-art fishing hook icon sprite.

The hook is drawn in a warm silver/steel palette with a small eye at the top
and a curved barbed point. Used as a world-space prompt above the player
when standing in a fishing zone.

Usage:
    python tooling/sprites/generate_hook_icon.py

Output:
    src/DogDays.Game/Content/Sprites/hook-icon.png
"""

import os
import shutil
from PIL import Image

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
OUTPUT_DIR = os.path.join(SCRIPT_DIR, "output")
PROJECT_ROOT = os.path.abspath(os.path.join(SCRIPT_DIR, "..", ".."))
CONTENT_SPRITES = os.path.join(
    PROJECT_ROOT, "src", "DogDays.Game", "Content", "Sprites"
)
os.makedirs(OUTPUT_DIR, exist_ok=True)

W, H = 13, 18

# Palette
T = (0, 0, 0, 0)           # transparent
O = (30, 25, 20, 255)       # outline / dark
M = (140, 140, 155, 255)    # metal mid
L = (190, 190, 200, 255)    # metal light / highlight
D = (90, 85, 95, 255)       # metal dark / shadow

# 18 rows x 13 columns — classic J-hook with round eye and wide smooth curve
PIXELS = [
    # Row 0  — eye ring top
    [T, T, T, T, T, O, O, O, T, T, T, T, T],
    # Row 1  — eye ring sides
    [T, T, T, T, O, D, T, D, O, T, T, T, T],
    # Row 2  — eye ring bottom
    [T, T, T, T, T, O, O, O, T, T, T, T, T],
    # Row 3  — shank top
    [T, T, T, T, T, O, M, O, T, T, T, T, T],
    # Row 4
    [T, T, T, T, T, O, L, O, T, T, T, T, T],
    # Row 5
    [T, T, T, T, T, O, M, O, T, T, T, T, T],
    # Row 6
    [T, T, T, T, T, O, L, O, T, T, T, T, T],
    # Row 7
    [T, T, T, T, T, O, M, O, T, T, T, T, T],
    # Row 8  — bend starts
    [T, T, T, T, T, O, D, M, O, T, T, T, T],
    # Row 9  — curving right
    [T, T, T, T, T, T, O, D, M, O, T, T, T],
    # Row 10 — wide right
    [T, T, T, T, T, T, T, O, D, M, O, T, T],
    # Row 11 — bottom right
    [T, T, T, T, T, T, T, O, M, L, O, T, T],
    # Row 12 — bottom of U
    [T, T, T, T, T, T, O, M, L, O, T, T, T],
    # Row 13 — curving back left
    [T, T, T, T, T, O, L, M, O, T, T, T, T],
    # Row 14
    [T, T, T, T, O, L, M, O, T, T, T, T, T],
    # Row 15 — rising left
    [T, T, T, O, L, D, O, T, T, T, T, T, T],
    # Row 16 — barb
    [T, T, O, M, O, T, T, T, T, T, T, T, T],
    # Row 17 — point
    [T, O, L, O, T, T, T, T, T, T, T, T, T],
]


def generate() -> None:
    img = Image.new("RGBA", (W, H), T)
    for y, row in enumerate(PIXELS):
        for x, color in enumerate(row):
            img.putpixel((x, y), color)

    out_path = os.path.join(OUTPUT_DIR, "hook-icon.png")
    img.save(out_path)
    print(f"Saved {out_path} ({W}x{H})")

    content_path = os.path.join(CONTENT_SPRITES, "hook-icon.png")
    shutil.copy2(out_path, content_path)
    print(f"Copied to {content_path}")


if __name__ == "__main__":
    generate()
