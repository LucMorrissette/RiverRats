"""
Prepare the pine tree prop sprite from a source JPG.

Steps:
1) Remove background by flood-filling near-white pixels connected to the image edge.
2) Crop to visible sprite bounds.
3) Resize to four tiles high (128 px) with aspect ratio locked using nearest-neighbor.
4) Reduce palette size while preserving alpha.

Usage:
    python tooling/sprites/prepare_pine_tree.py

Output:
    src/RiverRats.Game/Content/Sprites/pine-tree.png
"""

from __future__ import annotations

import os
from collections import deque
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[2]
SOURCE_PATH = ROOT / "tooling" / "sprites" / "PineTreeOne.jpg"
OUTPUT_PATH = ROOT / "src" / "RiverRats.Game" / "Content" / "Sprites" / "pine-tree.png"

TARGET_HEIGHT = 128  # 4 tiles at 32px each
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
        raise FileNotFoundError(f"Missing source sprite: {SOURCE_PATH}")

    source = Image.open(SOURCE_PATH)
    cleaned = remove_edge_connected_white_background(source)

    bbox = cleaned.getbbox()
    if bbox is None:
        raise ValueError("Background removal produced an empty image.")
    cropped = cleaned.crop(bbox)

    source_width, source_height = cropped.size
    target_width = max(1, round(source_width * (TARGET_HEIGHT / source_height)))
    resized = cropped.resize((target_width, TARGET_HEIGHT), resample=Image.Resampling.NEAREST)

    reduced = quantize_preserve_alpha(resized, PALETTE_COLORS)

    OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    reduced.save(OUTPUT_PATH)

    print(f"Saved sprite: {OUTPUT_PATH}")
    print(f"Final size: {reduced.size[0]}x{reduced.size[1]}")


if __name__ == "__main__":
    main()
