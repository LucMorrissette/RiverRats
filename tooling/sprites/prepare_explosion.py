"""
Prepare a 4-frame explosion sprite sheet from an irregularly spaced source.

Steps:
1) Auto-detect 4 frames by finding non-transparent column clusters.
2) Crop each frame to its content bounding box.
3) Pad all frames to the largest frame size, centred.
4) Pack into a single-row horizontal sprite sheet with uniform cell size.

Usage:
    python tooling/sprites/prepare_explosion.py

Output:
    src/RiverRats.Game/Content/Sprites/explosion.png
"""

from __future__ import annotations

from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SOURCE_PATH = ROOT / "tooling" / "sprites" / "simple-explosion.png"
OUTPUT_PATH = ROOT / "src" / "RiverRats.Game" / "Content" / "Sprites" / "explosion.png"

# Minimum gap (in transparent columns) to consider two blobs separate frames.
MIN_GAP = 2


def find_frame_column_ranges(image: Image.Image, alpha_threshold: int = 10) -> list[tuple[int, int]]:
    """Find contiguous column ranges that contain non-transparent pixels."""
    width, height = image.size
    pixels = image.load()

    # Build a mask of which columns have any visible pixel.
    col_has_content = []
    for x in range(width):
        has = False
        for y in range(height):
            if pixels[x, y][3] > alpha_threshold:
                has = True
                break
        col_has_content.append(has)

    # Group runs of content columns, allowing small transparent gaps.
    ranges: list[tuple[int, int]] = []
    start = None
    gap = 0
    for x, has in enumerate(col_has_content):
        if has:
            if start is None:
                start = x
            gap = 0
        else:
            if start is not None:
                gap += 1
                if gap > MIN_GAP:
                    ranges.append((start, x - gap))
                    start = None
                    gap = 0
    if start is not None:
        ranges.append((start, width - 1))

    return ranges


def crop_frame(image: Image.Image, col_start: int, col_end: int) -> Image.Image:
    """Crop a sub-region by column range, then trim to content bbox."""
    sub = image.crop((col_start, 0, col_end + 1, image.height))
    bbox = sub.getbbox()
    if bbox is None:
        return sub
    return sub.crop(bbox)


def main() -> None:
    if not SOURCE_PATH.exists():
        raise FileNotFoundError(f"Missing source sprite: {SOURCE_PATH}")

    source = Image.open(SOURCE_PATH).convert("RGBA")
    print(f"Source: {source.size[0]}x{source.size[1]}")

    # Detect frame column ranges.
    ranges = find_frame_column_ranges(source)
    print(f"Detected {len(ranges)} frame regions: {ranges}")

    if len(ranges) != 4:
        raise ValueError(f"Expected 4 frames, found {len(ranges)}. Adjust MIN_GAP or check source image.")

    # Crop each frame to its content.
    frames = [crop_frame(source, start, end) for start, end in ranges]
    for i, f in enumerate(frames):
        print(f"  Frame {i}: {f.size[0]}x{f.size[1]}")

    # Find the largest dimensions to use as uniform cell size.
    cell_w = max(f.width for f in frames)
    cell_h = max(f.height for f in frames)
    print(f"Uniform cell size: {cell_w}x{cell_h}")

    # Pack into a single-row sheet, each frame centred in its cell.
    sheet = Image.new("RGBA", (cell_w * len(frames), cell_h), (0, 0, 0, 0))
    for i, frame in enumerate(frames):
        offset_x = (cell_w - frame.width) // 2
        offset_y = (cell_h - frame.height) // 2
        sheet.paste(frame, (i * cell_w + offset_x, offset_y), frame)

    OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(OUTPUT_PATH)
    print(f"Saved: {OUTPUT_PATH.name} ({sheet.size[0]}x{sheet.size[1]}, cell: {cell_w}x{cell_h})")


if __name__ == "__main__":
    main()
