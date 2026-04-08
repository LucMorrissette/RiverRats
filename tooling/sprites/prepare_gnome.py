"""
Prepare the garden gnome prop sprite from a source JPG.

Steps:
1) Remove background by flood-filling near-white pixels connected to the image edge.
2) Crop to visible sprite bounds.
3) Resize to one tile high (32 px) with aspect ratio locked using nearest-neighbor.
4) Reduce palette size while preserving alpha.

Usage:
    python tooling/sprites/prepare_gnome.py

Output:
    src/DogDays.Game/Content/Sprites/garden-gnome.png
"""

from __future__ import annotations

from collections import deque
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[2]
SOURCE_PATH = ROOT / "tooling" / "sprites" / "gnome.jpg"
OUTPUT_PATH = ROOT / "src" / "DogDays.Game" / "Content" / "Sprites" / "garden-gnome.png"

TARGET_HEIGHT = 16  # 1/2 tile (32 / 2 = 16 px)
PALETTE_COLORS = 20
WHITE_THRESHOLD = 240
MAX_DIMENSION = 1000


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

    w, h = source.size
    if max(w, h) > MAX_DIMENSION:
        scale = MAX_DIMENSION / max(w, h)
        new_w = max(1, round(w * scale))
        new_h = max(1, round(h * scale))
        source = source.resize((new_w, new_h), resample=Image.Resampling.LANCZOS)
        print(f"Downscaled source from {w}x{h} to {source.size[0]}x{source.size[1]}")

    cleaned = remove_edge_connected_white_background(source)

    bbox = cleaned.getbbox()
    if bbox is None:
        raise ValueError("Background removal produced an empty image.")
    cropped = cleaned.crop(bbox)
    print(f"Cropped to {cropped.size[0]}x{cropped.size[1]}")

    src_w, src_h = cropped.size
    scale_factor = TARGET_HEIGHT / src_h
    target_w = max(1, round(src_w * scale_factor))
    resized = cropped.resize((target_w, TARGET_HEIGHT), resample=Image.Resampling.NEAREST)

    reduced = quantize_preserve_alpha(resized, PALETTE_COLORS)

    OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    reduced.save(OUTPUT_PATH)

    print(f"Saved sprite: {OUTPUT_PATH.name} ({reduced.size[0]}x{reduced.size[1]})")


if __name__ == "__main__":
    main()
