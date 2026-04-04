"""
Append commando-roll frames to the existing character sprite sheets.

This expands the current 128x160 walk+sitting sheets to 128x192 by adding
one new row of four roll frames at row 5 (0-based). The roll frames are
direction-agnostic spinning bundles that are rotated at runtime to match the
dash direction.

Outputs (in-place update):
  - src/RiverRats.Game/Content/Sprites/generic_character_sheet.png   (128x192)
  - src/RiverRats.Game/Content/Sprites/companion_character_sheet.png (128x192)
"""

from __future__ import annotations

import math
import os
from PIL import Image


SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
REPO_ROOT = os.path.abspath(os.path.join(SCRIPT_DIR, "..", ".."))
CONTENT_DIR = os.path.join(REPO_ROOT, "src", "RiverRats.Game", "Content", "Sprites")

FRAME_W = 32
FRAME_H = 32
BASE_SHEET_H = 160
ROLL_ROW_Y = BASE_SHEET_H
ART_W = 16
ART_H = 16
ART_OX = (FRAME_W - ART_W) // 2
ART_OY = FRAME_H - ART_H - 3

TRANSPARENT = (0, 0, 0, 0)
OUTLINE = (26, 10, 13, 255)
HAIR = (204, 116, 29, 255)
HAIR_DARK = (159, 84, 19, 255)
HAIR_LIGHT = (251, 163, 39, 255)
SKIN = (255, 211, 170, 255)
SHIRT = (238, 238, 238, 255)
SHIRT_DARK = (189, 189, 189, 255)
PANTS = (45, 149, 236, 255)
PANTS_DARK = (25, 101, 165, 255)
SHOE = (76, 76, 76, 255)
SHOE_DARK = (50, 50, 50, 255)

CAP = (186, 34, 34, 255)
CAP_DARK = (132, 20, 20, 255)
CAP_LIGHT = (232, 76, 76, 255)
SHIRT_RED = (213, 48, 48, 255)
SHIRT_RED_DARK = (129, 21, 21, 255)


def build_roll_frame(phase_index: int) -> Image.Image:
    """Create one 32x32 roll frame using a compact spinning bundle silhouette."""
    image = Image.new("RGBA", (FRAME_W, FRAME_H), TRANSPARENT)
    pixels = image.load()

    center_x = 7.5
    center_y = 7.5
    outer_rx = 6.1
    outer_ry = 5.1
    inner_rx = 5.0
    inner_ry = 4.0

    sector_colors = [HAIR, SHIRT, PANTS, SHOE]
    sector_dark_colors = [HAIR_DARK, SHIRT_DARK, PANTS_DARK, SHOE_DARK]

    for art_y in range(ART_H):
        for art_x in range(ART_W):
            dx = art_x - center_x
            dy = art_y - center_y
            outer_dist = (dx * dx) / (outer_rx * outer_rx) + (dy * dy) / (outer_ry * outer_ry)
            if outer_dist > 1.0:
                continue

            pixel_x = ART_OX + art_x
            pixel_y = ART_OY + art_y

            inner_dist = (dx * dx) / (inner_rx * inner_rx) + (dy * dy) / (inner_ry * inner_ry)
            if inner_dist > 1.0:
                pixels[pixel_x, pixel_y] = OUTLINE
                continue

            angle = math.atan2(dy, dx) - phase_index * (math.pi * 0.5)
            normalized_angle = (angle + math.pi) / (math.pi * 0.5)
            sector_index = int(normalized_angle) % 4
            radial = math.sqrt(inner_dist)

            color = sector_colors[sector_index]
            if radial > 0.7:
                color = sector_dark_colors[sector_index]

            if sector_index == 0 and art_x >= 9 and art_y <= 7:
                color = HAIR_LIGHT

            pixels[pixel_x, pixel_y] = color

    # Face/hand flash that rotates with the roll to keep it readable as a character.
    skin_orbits = [
        [(11, 6), (11, 7), (10, 7)],
        [(8, 11), (7, 11), (8, 10)],
        [(4, 8), (4, 7), (5, 8)],
        [(7, 4), (8, 4), (8, 5)],
    ]
    for art_x, art_y in skin_orbits[phase_index % 4]:
        pixels[ART_OX + art_x, ART_OY + art_y] = SKIN

    # A couple of trailing shoe pixels help sell the spin.
    shoe_trails = [
        [(3, 10), (4, 10)],
        [(5, 12), (5, 13)],
        [(12, 5), (11, 5)],
        [(10, 3), (10, 4)],
    ]
    for art_x, art_y in shoe_trails[phase_index % 4]:
        pixels[ART_OX + art_x, ART_OY + art_y] = SHOE_DARK

    # A center outline crease gives the bundle a tucked-body read.
    crease_points = [
        [(7, 7), (8, 8), (9, 8)],
        [(8, 7), (7, 8), (7, 9)],
        [(8, 8), (7, 7), (6, 7)],
        [(7, 8), (8, 7), (8, 6)],
    ]
    for art_x, art_y in crease_points[phase_index % 4]:
        pixels[ART_OX + art_x, ART_OY + art_y] = OUTLINE

    return image


def recolor_companion(frame: Image.Image) -> Image.Image:
    """Convert the generic roll frame to the companion palette."""
    result = frame.copy()
    pixels = result.load()

    for y in range(result.height):
        for x in range(result.width):
            color = pixels[x, y]
            if color == HAIR:
                pixels[x, y] = CAP
            elif color == HAIR_DARK:
                pixels[x, y] = CAP_DARK
            elif color == HAIR_LIGHT:
                pixels[x, y] = CAP_LIGHT
            elif color == SHIRT:
                pixels[x, y] = SHIRT_RED
            elif color == SHIRT_DARK:
                pixels[x, y] = SHIRT_RED_DARK

    return result


def append_roll_row(sheet_path: str, frames: list[Image.Image]) -> Image.Image:
    """Expand a sprite sheet to include the roll row, preserving existing rows 0-4."""
    sheet = Image.open(sheet_path).convert("RGBA")
    new_sheet = Image.new("RGBA", (FRAME_W * 4, BASE_SHEET_H + FRAME_H), TRANSPARENT)
    new_sheet.paste(sheet.crop((0, 0, FRAME_W * 4, BASE_SHEET_H)), (0, 0))

    for column, frame in enumerate(frames):
        new_sheet.paste(frame, (column * FRAME_W, ROLL_ROW_Y))

    new_sheet.save(sheet_path)
    return new_sheet


def main() -> None:
    print("Generating commando-roll frames...")

    generic_frames = [build_roll_frame(i) for i in range(4)]
    companion_frames = [recolor_companion(frame) for frame in generic_frames]

    generic_sheet_path = os.path.join(CONTENT_DIR, "generic_character_sheet.png")
    companion_sheet_path = os.path.join(CONTENT_DIR, "companion_character_sheet.png")

    append_roll_row(generic_sheet_path, generic_frames)
    append_roll_row(companion_sheet_path, companion_frames)

    print(f"  Updated: {generic_sheet_path}")
    print(f"  Updated: {companion_sheet_path}")
    print("Done.")


if __name__ == "__main__":
    main()