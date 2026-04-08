"""
Prepare 3 small bush prop sprites from a sprite sheet.

Steps:
1) Crop each bush from the source sprite sheet using known column/row ranges.
2) Remove the white background by flood-filling near-white pixels connected to the image edge.
3) Resize to target widths (aspect-locked) using nearest-neighbor resampling.
4) Reduce palette to ~16-20 colors while preserving alpha.

Usage:
    python tooling/sprites/prepare_small_bushes.py

Output:
    src/DogDays.Game/Content/Sprites/bush1.png
    src/DogDays.Game/Content/Sprites/bush2.png
    src/DogDays.Game/Content/Sprites/bush3.png
"""

from __future__ import annotations

from collections import deque
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[2]
SOURCE_PATH = ROOT / "tooling" / "sprites" / "sprite_sheet_small_bushes.png"
OUTPUT_DIR = ROOT / "src" / "DogDays.Game" / "Content" / "Sprites"

# (col_start, col_end, row_start, row_end, target_width)
BUSH_SPECS = [
    (214, 1439, 447, 1624, 32),   # Bush 1 — small round bush
    (1583, 3312, 687, 1624, 64),   # Bush 2 — wide bush (2 tiles)
    (3431, 4608, 374, 1600, 32),   # Bush 3 — small tall bush
]

PALETTE_COLORS = 20
WHITE_THRESHOLD = 245


def is_near_white(r: int, g: int, b: int) -> bool:
    return r >= WHITE_THRESHOLD and g >= WHITE_THRESHOLD and b >= WHITE_THRESHOLD


def remove_edge_connected_white_background(image: Image.Image) -> Image.Image:
    rgba = image.convert("RGBA")
    pixels = rgba.load()
    width, height = rgba.size

    visited = [[False for _ in range(width)] for _ in range(height)]
    queue: deque[tuple[int, int]] = deque()

    for x in range(width):
        queue.append((x, 0))
        queue.append((x, height - 1))
    for y in range(height):
        queue.append((0, y))
        queue.append((width - 1, y))

    while queue:
        x, y = queue.popleft()
        if x < 0 or x >= width or y < 0 or y >= height:
            continue
        if visited[y][x]:
            continue

        visited[y][x] = True
        r, g, b, _ = pixels[x, y]
        if not is_near_white(r, g, b):
            continue

        pixels[x, y] = (r, g, b, 0)
        queue.append((x + 1, y))
        queue.append((x - 1, y))
        queue.append((x, y + 1))
        queue.append((x, y - 1))

    return rgba


def quantize_preserve_alpha(image: Image.Image, color_count: int) -> Image.Image:
    rgba = image.convert("RGBA")
    alpha = rgba.getchannel("A")
    rgb = rgba.convert("RGB")
    quantized = rgb.quantize(colors=color_count, method=Image.Quantize.FASTOCTREE)
    reduced_rgb = quantized.convert("RGB")
    reduced_rgba = reduced_rgb.convert("RGBA")
    reduced_rgba.putalpha(alpha)
    return reduced_rgba


def main() -> None:
    if not SOURCE_PATH.exists():
        raise FileNotFoundError(f"Missing source sprite sheet: {SOURCE_PATH}")

    source = Image.open(SOURCE_PATH)
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

    for index, (col_start, col_end, row_start, row_end, target_width) in enumerate(BUSH_SPECS, start=1):
        # Crop to content region (left, upper, right+1, lower+1 for PIL crop)
        cropped = source.crop((col_start, row_start, col_end + 1, row_end + 1))

        # Remove white background via edge-connected flood fill
        cleaned = remove_edge_connected_white_background(cropped)

        # Tight crop to visible pixels
        bbox = cleaned.getbbox()
        if bbox is None:
            raise ValueError(f"Bush {index}: background removal produced an empty image.")
        cleaned = cleaned.crop(bbox)

        # Resize to target width, aspect-locked
        source_width, source_height = cleaned.size
        target_height = max(1, round(source_height * (target_width / source_width)))
        resized = cleaned.resize((target_width, target_height), resample=Image.Resampling.NEAREST)

        # Reduce palette
        reduced = quantize_preserve_alpha(resized, PALETTE_COLORS)

        output_path = OUTPUT_DIR / f"bush{index}.png"
        reduced.save(output_path)
        print(f"Bush {index}: saved {output_path}")
        print(f"  Final size: {reduced.size[0]}x{reduced.size[1]}")


if __name__ == "__main__":
    main()
