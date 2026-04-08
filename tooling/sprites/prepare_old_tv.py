"""
Prepares old-tv.jpg for use as a game prop:
  - Removes white / near-white background via per-pixel luminance gating.
  - Trims fully-transparent border pixels.
  - Resizes to exactly 48 px tall (1.5 tiles), preserving aspect ratio.
  - Saves as old-tv.png in the game's Content/Sprites folder.
"""
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "tooling" / "old-tv.jpg"
DST = ROOT / "src" / "DogDays.Game" / "Content" / "Sprites" / "old-tv.png"

FULL_TRANSPARENT_THRESHOLD = 235
FRINGE_THRESHOLD = 210
TARGET_HEIGHT = 48  # 1.5 tiles tall = 48 px


def remove_white_background(img: Image.Image) -> Image.Image:
    img = img.convert("RGBA")
    pixels = img.load()
    w, h = img.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = pixels[x, y]
            mn = min(r, g, b)
            if mn >= FULL_TRANSPARENT_THRESHOLD:
                pixels[x, y] = (r, g, b, 0)
            elif mn >= FRINGE_THRESHOLD:
                range_size = FULL_TRANSPARENT_THRESHOLD - FRINGE_THRESHOLD
                alpha = int(255 * (FULL_TRANSPARENT_THRESHOLD - mn) / range_size)
                pixels[x, y] = (r, g, b, min(alpha, a))
    return img


def main() -> None:
    img = Image.open(SRC)
    print(f"Source size: {img.size[0]}x{img.size[1]}")

    img = remove_white_background(img)

    bbox = img.getbbox()
    if bbox is None:
        raise ValueError("Image is fully transparent after background removal.")
    img = img.crop(bbox)
    print(f"Trimmed size: {img.size[0]}x{img.size[1]}")

    tw, th = img.size
    scale = TARGET_HEIGHT / th
    new_w = max(1, round(tw * scale))
    img = img.resize((new_w, TARGET_HEIGHT), Image.LANCZOS)
    print(f"Final size: {img.size[0]}x{img.size[1]}")

    img.save(DST)
    print(f"Saved to: {DST}")


if __name__ == "__main__":
    main()
