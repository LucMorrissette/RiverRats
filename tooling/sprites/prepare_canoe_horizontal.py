"""
Prepare the horizontal canoe prop sprite from the raw source image.

Steps:
1) Remove edge-connected near-white background pixels.
2) Crop to visible sprite bounds.
3) Resize to two tiles wide (64 px) with aspect ratio locked.
4) Reduce the palette while preserving alpha.

Usage:
    python tooling/sprites/prepare_canoe_horizontal.py

Output:
    src/DogDays.Game/Content/Sprites/canoe-horizontal.png
"""

from __future__ import annotations

from collections import deque
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[2]
SOURCE_PATH = ROOT / "tooling" / "sprites" / "canoe_horizontal_raw.png"
OUTPUT_PATH = ROOT / "src" / "DogDays.Game" / "Content" / "Sprites" / "canoe-horizontal.png"

TARGET_WIDTH = 64  # 2 tiles at 32 px each
PALETTE_COLORS = 12
WHITE_THRESHOLD = 245
FRINGE_THRESHOLD = 220
ALPHA_CUTOFF = 96
MAX_DIMENSION = 1024


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

        pixels[x, y] = (0, 0, 0, 0)
        queue.append((x + 1, y))
        queue.append((x - 1, y))
        queue.append((x, y + 1))
        queue.append((x, y - 1))

    return rgba


def trim_to_alpha_bounds(image: Image.Image) -> Image.Image:
    alpha_bbox = image.getchannel("A").getbbox()
    if alpha_bbox is None:
        raise ValueError("Image is fully transparent after background removal.")

    return image.crop(alpha_bbox)


def remove_white_fringe(image: Image.Image) -> Image.Image:
    rgba = image.convert("RGBA")
    pixels = rgba.load()
    width, height = rgba.size

    for y in range(height):
        for x in range(width):
            r, g, b, a = pixels[x, y]
            minimum = min(r, g, b)
            if minimum >= WHITE_THRESHOLD:
                pixels[x, y] = (0, 0, 0, 0)
                continue

            if minimum >= FRINGE_THRESHOLD:
                range_size = WHITE_THRESHOLD - FRINGE_THRESHOLD
                alpha = int(255 * (WHITE_THRESHOLD - minimum) / range_size)
                pixels[x, y] = (r, g, b, min(alpha, a))

    return rgba


def clean_alpha(image: Image.Image) -> Image.Image:
    rgba = image.convert("RGBA")
    pixels = rgba.load()
    width, height = rgba.size

    for y in range(height):
        for x in range(width):
            r, g, b, a = pixels[x, y]
            if a < ALPHA_CUTOFF:
                pixels[x, y] = (0, 0, 0, 0)

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
    width, height = source.size
    if max(width, height) > MAX_DIMENSION:
        scale = MAX_DIMENSION / max(width, height)
        resized_width = max(1, round(width * scale))
        resized_height = max(1, round(height * scale))
        source = source.resize((resized_width, resized_height), resample=Image.Resampling.LANCZOS)
        print(f"Downscaled source from {width}x{height} to {source.size[0]}x{source.size[1]}")

    cleaned = remove_edge_connected_white_background(source)

    cropped = trim_to_alpha_bounds(cleaned)
    source_width, source_height = cropped.size
    target_height = max(1, round(source_height * (TARGET_WIDTH / source_width)))
    resized = cropped.resize((TARGET_WIDTH, target_height), resample=Image.Resampling.NEAREST)
    resized = remove_white_fringe(resized)
    resized = clean_alpha(resized)
    resized = trim_to_alpha_bounds(resized)

    final_height = max(1, round(resized.size[1] * (TARGET_WIDTH / resized.size[0])))
    final = resized.resize((TARGET_WIDTH, final_height), resample=Image.Resampling.NEAREST)
    final = remove_white_fringe(final)
    final = clean_alpha(final)
    final = trim_to_alpha_bounds(final)

    if final.size[0] != TARGET_WIDTH:
        corrected_height = max(1, round(final.size[1] * (TARGET_WIDTH / final.size[0])))
        final = final.resize((TARGET_WIDTH, corrected_height), resample=Image.Resampling.NEAREST)
        final = clean_alpha(remove_white_fringe(final))
        final = trim_to_alpha_bounds(final)

    reduced = quantize_preserve_alpha(final, PALETTE_COLORS)

    OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    reduced.save(OUTPUT_PATH)

    print(f"Saved sprite: {OUTPUT_PATH}")
    print(f"Final size: {reduced.size[0]}x{reduced.size[1]}")


if __name__ == "__main__":
    main()