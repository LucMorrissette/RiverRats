"""
Prepare CozyLakeCabin sprite for the game.

Steps:
1. Flood-fill remove white background from edges (handles anti-aliased JPEG edges).
2. Erode alpha edge by 1px to remove halo fringe.
3. Trim transparent borders tightly.
4. Resize to 128px wide (4 tiles × 32px) keeping aspect ratio locked.
5. Save as PNG with transparency.
"""

from PIL import Image, ImageFilter, ImageDraw
import numpy as np
import os
import sys

TILE_SIZE = 32
TARGET_WIDTH = TILE_SIZE * 5  # 160 px

# Flood-fill tolerance: how far a pixel colour can be from white to count as BG.
TOLERANCE = 40


def flood_fill_remove_bg(img: Image.Image) -> Image.Image:
    """Remove background via flood-fill from every edge pixel, treating near-white as BG."""
    img = img.convert("RGBA")
    arr = np.array(img)
    h, w = arr.shape[:2]

    # Boolean mask: True = confirmed background.
    bg_mask = np.zeros((h, w), dtype=bool)

    # Seed pixels: all edge pixels.
    seeds = set()
    for x in range(w):
        seeds.add((x, 0))
        seeds.add((x, h - 1))
    for y in range(h):
        seeds.add((0, y))
        seeds.add((w - 1, y))

    def is_near_white(r, g, b):
        return r > (255 - TOLERANCE) and g > (255 - TOLERANCE) and b > (255 - TOLERANCE)

    # BFS flood-fill from edge seeds.
    queue = []
    for (x, y) in seeds:
        r, g, b = int(arr[y, x, 0]), int(arr[y, x, 1]), int(arr[y, x, 2])
        if is_near_white(r, g, b) and not bg_mask[y, x]:
            bg_mask[y, x] = True
            queue.append((x, y))

    while queue:
        batch = queue
        queue = []
        for (cx, cy) in batch:
            for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
                nx, ny = cx + dx, cy + dy
                if 0 <= nx < w and 0 <= ny < h and not bg_mask[ny, nx]:
                    r, g, b = int(arr[ny, nx, 0]), int(arr[ny, nx, 1]), int(arr[ny, nx, 2])
                    if is_near_white(r, g, b):
                        bg_mask[ny, nx] = True
                        queue.append((nx, ny))

    # Zero out alpha on background pixels.
    arr[bg_mask, 3] = 0
    return Image.fromarray(arr)


def erode_alpha(img: Image.Image, iterations: int = 1) -> Image.Image:
    """Erode the alpha channel to remove the 1-2px anti-alias halo fringe."""
    alpha = img.split()[3]
    for _ in range(iterations):
        alpha = alpha.filter(ImageFilter.MinFilter(size=3))
    img.putalpha(alpha)
    return img


def trim_transparent(img: Image.Image) -> Image.Image:
    """Crop to the bounding box of non-transparent pixels."""
    bbox = img.getbbox()
    if bbox is None:
        return img
    return img.crop(bbox)


def resize_to_width(img: Image.Image, target_w: int) -> Image.Image:
    """Resize keeping aspect ratio."""
    w, h = img.size
    ratio = target_w / w
    target_h = round(h * ratio)
    return img.resize((target_w, target_h), Image.LANCZOS)


def main() -> None:
    src_path = os.path.join(os.path.dirname(__file__), "CozyLakeCabin.jpg")
    if not os.path.isfile(src_path):
        print(f"ERROR: source image not found: {src_path}", file=sys.stderr)
        sys.exit(1)

    img = Image.open(src_path)
    print(f"Source size: {img.size}")

    img = flood_fill_remove_bg(img)
    print("Background flood-fill removed.")

    img = erode_alpha(img, iterations=2)
    print("Alpha halo eroded.")

    img = trim_transparent(img)
    print(f"Trimmed size: {img.size}")

    img = resize_to_width(img, TARGET_WIDTH)
    print(f"Resized to: {img.size}")

    out_dir = os.path.join(
        os.path.dirname(__file__),
        "..",
        "..",
        "src",
        "DogDays.Game",
        "Content",
        "Sprites",
    )
    os.makedirs(out_dir, exist_ok=True)
    out_path = os.path.join(out_dir, "cozy_lake_cabin.png")
    img.save(out_path, "PNG")
    print(f"Saved to: {out_path}")


if __name__ == "__main__":
    main()
