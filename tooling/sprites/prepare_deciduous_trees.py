"""
Prepare four deciduous tree prop sprites from a single sprite sheet JPG.

Steps:
1) Split the 4800x3584 sheet into a 2x2 grid (4 quadrants).
2) For each quadrant, remove the white background via edge-connected flood-fill.
3) Crop to visible sprite bounds.
4) Resize to four tiles high (128 px) with aspect ratio locked using nearest-neighbor.
5) Reduce palette size while preserving alpha.

Usage:
    python tooling/sprites/prepare_deciduous_trees.py

Output:
    src/DogDays.Game/Content/Sprites/deciduous-tree1.png
    src/DogDays.Game/Content/Sprites/deciduous-tree2.png
    src/DogDays.Game/Content/Sprites/deciduous-tree3.png
    src/DogDays.Game/Content/Sprites/deciduous-tree4.png
"""

from __future__ import annotations

from collections import deque
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[2]
SOURCE_PATH = ROOT / "tooling" / "sprites" / "sprite_sheet_deciduous_trees.jpg"
OUTPUT_DIR = ROOT / "src" / "DogDays.Game" / "Content" / "Sprites"

TARGET_HEIGHT = 128  # 4 tiles at 32 px
PALETTE_COLORS = 20
WHITE_THRESHOLD = 240
MAX_QUADRANT_DIM = 1000  # Downscale quadrants above this before flood-fill


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

    sheet = Image.open(SOURCE_PATH)
    sheet_w, sheet_h = sheet.size
    half_w = sheet_w // 2
    half_h = sheet_h // 2

    # Split into 2x2 grid: top-left, top-right, bottom-left, bottom-right
    quadrants = [
        sheet.crop((0, 0, half_w, half_h)),
        sheet.crop((half_w, 0, sheet_w, half_h)),
        sheet.crop((0, half_h, half_w, sheet_h)),
        sheet.crop((half_w, half_h, sheet_w, sheet_h)),
    ]

    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

    for i, quad in enumerate(quadrants):
        w, h = quad.size
        if max(w, h) > MAX_QUADRANT_DIM:
            scale = MAX_QUADRANT_DIM / max(w, h)
            new_w = max(1, round(w * scale))
            new_h = max(1, round(h * scale))
            quad = quad.resize((new_w, new_h), resample=Image.Resampling.LANCZOS)
            print(f"Tree {i + 1}: downscaled quadrant from {w}x{h} to {quad.size[0]}x{quad.size[1]}")

        cleaned = remove_edge_connected_white_background(quad)
        bbox = cleaned.getbbox()
        if bbox is None:
            raise ValueError(f"Tree {i + 1}: background removal produced an empty image.")
        cropped = cleaned.crop(bbox)
        print(f"Tree {i + 1}: cropped to {cropped.size[0]}x{cropped.size[1]}")

        src_w, src_h = cropped.size
        scale_factor = TARGET_HEIGHT / src_h
        target_w = max(1, round(src_w * scale_factor))
        resized = cropped.resize((target_w, TARGET_HEIGHT), resample=Image.Resampling.NEAREST)

        reduced = quantize_preserve_alpha(resized, PALETTE_COLORS)

        output_path = OUTPUT_DIR / f"deciduous-tree{i + 1}.png"
        reduced.save(output_path)
        print(f"Tree {i + 1}: saved {output_path.name} ({reduced.size[0]}x{reduced.size[1]})")

    print("\nDone — all four deciduous tree sprites saved.")


if __name__ == "__main__":
    main()
