"""
Append directional death-collapse frames to the existing character sprite sheets.

Uses the same palette-mapped pixel-art approach as generate_character.py so the
collapse poses match the actual character proportions and style.

Preserves rows 0-5 (walk, sitting, roll) and writes rows 6-9 with four collapse
frames per facing direction (Down=6, Left=7, Right=8, Up=9):
  - frame 0: buckle (knees bending, body sinking)
  - frame 1: drop to knees (kneeling, torso still up)
  - frame 2: tipping forward (head dropping, very short)
  - frame 3: flat on the ground (lying horizontal)

Outputs (in-place update):
  - src/RiverRats.Game/Content/Sprites/generic_character_sheet.png   (128x320)
  - src/RiverRats.Game/Content/Sprites/companion_character_sheet.png (128x320)
"""

from __future__ import annotations

import os
from PIL import Image


SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
REPO_ROOT = os.path.abspath(os.path.join(SCRIPT_DIR, "..", ".."))
CONTENT_DIR = os.path.join(REPO_ROOT, "src", "RiverRats.Game", "Content", "Sprites")

FRAME_W = 32
FRAME_H = 32
EXISTING_ROWS = 6          # rows 0-5 already authored
COLLAPSE_ROW_START = 6     # rows 6-9 = collapse (Down, Left, Right, Up)
DIRECTION_COUNT = 4
NEW_SHEET_H = (EXISTING_ROWS + DIRECTION_COUNT) * FRAME_H  # 320

# ---------------------------------------------------------------------------
# Character art geometry (must match generate_character.py)
# ---------------------------------------------------------------------------
ART_W = 16
ART_H = 22
ART_OX = (FRAME_W - ART_W) // 2   # 8
ART_OY = FRAME_H - ART_H - 2      # 8

# ---------------------------------------------------------------------------
# Color palette (identical to generate_character.py)
# ---------------------------------------------------------------------------
TRANSPARENT = (0, 0, 0, 0)
OUTLINE     = (26, 10, 13, 255)

HAIR        = (204, 116, 29, 255)
HAIR_DARK   = (159, 84, 19, 255)
HAIR_LIGHT  = (251, 163, 39, 255)

SKIN        = (255, 211, 170, 255)
SKIN_MID    = (225, 170, 145, 255)
SKIN_DARK   = (176, 136, 108, 255)

EYE         = (18, 3, 8, 255)

SHIRT       = (238, 238, 238, 255)
SHIRT_MID   = (212, 212, 212, 255)
SHIRT_DARK  = (189, 189, 189, 255)

PANTS       = (45, 149, 236, 255)
PANTS_MID   = (36, 124, 198, 255)
PANTS_DARK  = (25, 101, 165, 255)

SHOE        = (76, 76, 76, 255)
SHOE_DARK   = (50, 50, 50, 255)

PALETTE = {
    '.': TRANSPARENT,
    'O': OUTLINE,
    'H': HAIR,
    'h': HAIR_DARK,
    'L': HAIR_LIGHT,
    'S': SKIN,
    's': SKIN_MID,
    'd': SKIN_DARK,
    'E': EYE,
    'T': SHIRT,
    't': SHIRT_MID,
    'u': SHIRT_DARK,
    'B': PANTS,
    'b': PANTS_MID,
    'a': PANTS_DARK,
    'G': SHOE,
    'g': SHOE_DARK,
}

# Companion palette swaps.
CAP            = (186, 34, 34, 255)
CAP_DARK       = (132, 20, 20, 255)
CAP_LIGHT      = (232, 76, 76, 255)
SHIRT_RED      = (213, 48, 48, 255)
SHIRT_RED_MID  = (170, 33, 33, 255)
SHIRT_RED_DARK = (129, 21, 21, 255)


# ===================================================================
# DOWN-FACING COLLAPSE (row 6) -- player faces the camera
# ===================================================================

DOWN_COLLAPSE_0 = [  # Buckle -- knees bending, body sinking
    "................",
    "................",
    "......OOOO......",
    ".....OHHHhO.....",
    "....OHHLHHHO....",
    "...OHHHHHHHHO...",
    "..OHHHHHHHHHO...",
    "..OHHsSSSSsHHO..",
    "..OHsESSSSEsHO..",
    "..OHsSSSSSSSHO..",
    "...OsSSSSSSsO...",
    "....OdsSSSdO....",
    "....OsTTTTsO....",
    "...OuTTttTTuO...",
    "..OdTTTttTTTdO..",
    "...OBBBBBBBbO...",
    "..OBbO....OBbO..",
    "..OGgO....OGgO..",
    "..OOO......OOO..",
    "................",
    "................",
    "................",
]

DOWN_COLLAPSE_1 = [  # Drop to knees
    "................",
    "................",
    "................",
    "................",
    "......OOOO......",
    ".....OHHHhO.....",
    "....OHHLHHHO....",
    "...OHHHHHHHHO...",
    "..OHHHHHHHHHO...",
    "..OHHsSSSSsHHO..",
    "..OHsESSSSEsHO..",
    "..OHsSSSSSSSHO..",
    "...OsSSSSSSsO...",
    "....OdsSSSdO....",
    "...OuTTttTTuO...",
    "..OdTTTttTTTdO..",
    "..OBBBBaaBBBBO..",
    "..ObGaO..OaGbO..",
    "..OOOO....OOOO..",
    "................",
    "................",
    "................",
]

DOWN_COLLAPSE_2 = [  # Tipping forward -- head dropping
    "................",
    "................",
    "................",
    "................",
    "................",
    "................",
    "................",
    ".....OOOO.......",
    "....OHHHhO......",
    "...OHHLHHHO.....",
    "..OHHHHHHHHO....",
    "..OHHsSSSSsHO...",
    "..OHsESSSSEsO...",
    "...OsSSSSSsO....",
    "...OuTTtTTuO....",
    "..OBBBBaaBBBBO..",
    "..ObGaO..OaGbO..",
    "..OOOO....OOOO..",
    "................",
    "................",
    "................",
    "................",
]

DOWN_COLLAPSE_3 = [  # Flat on ground (face-down, seen from above)
    "................",
    "................",
    "................",
    "................",
    "................",
    "................",
    "................",
    "................",
    "................",
    "................",
    "................",
    "...OOOOOOO......",
    "..OHHLHHHhO.....",
    "..OhHHHHHHhO....",
    "..OhHTTTTTHhO...",
    "...OuTTtTTuO....",
    "...OBBaaBBbO....",
    "...OGgO.OGgO....",
    "...OOO...OOO....",
    "................",
    "................",
    "................",
]


# ===================================================================
# LEFT-FACING COLLAPSE (row 7) -- player faces left
# ===================================================================

LEFT_COLLAPSE_0 = [  # Buckle
    "................",
    "................",
    "......OOOOO.....",
    ".....OHHHHhO....",
    "....OHHLHHHhO...",
    "...OHHHHHHHHhO..",
    "...OHHHHHHHHhO..",
    "...OHSSSSSSHhO..",
    "...OESSSSSShHO..",
    "...OsSSSSSSHhO..",
    "....OSSSSSShO...",
    ".....OdsSSdO....",
    ".....OsTTTsO....",
    "....OuTTtTTuO...",
    "...OdTTTtTTTuO..",
    "...OBBBBBBBbO...",
    "..ObbO...ObbO...",
    "..OGgO...OGgO...",
    "..OOO.....OOO...",
    "................",
    "................",
    "................",
]

LEFT_COLLAPSE_1 = [  # Drop to knees
    "................",
    "................",
    "................",
    "................",
    "......OOOOO.....",
    ".....OHHHHhO....",
    "....OHHLHHHhO...",
    "...OHHHHHHHHhO..",
    "...OHHHHHHHHhO..",
    "...OHSSSSSSHhO..",
    "...OESSSSSShHO..",
    "...OsSSSSSSHhO..",
    "....OSSSSSShO...",
    ".....OdsSSdO....",
    "....OuTTtTTuO...",
    "...OdTTTtTTTdO..",
    "..OBBBBaaBBBBO..",
    "..OGbGaO.ObGGO..",
    "..OOOOO...OOOO..",
    "................",
    "................",
    "................",
]

LEFT_COLLAPSE_2 = [  # Tipping sideways
    "................",
    "................",
    "................",
    "................",
    "................",
    "................",
    "................",
    "....OOOOO.......",
    "...OHHHHhO......",
    "..OHHLHHHhO.....",
    "..OHHHHHHHhO....",
    "..OHSSSSSSHhO...",
    "..OESSSSSShO....",
    "...OsSSSSdO.....",
    "...OuTTtTTuO....",
    "..OBBBBaaBBBBO..",
    "..OGbGaO.ObGGO..",
    "..OOOOO...OOOO..",
    "................",
    "................",
    "................",
    "................",
]

LEFT_COLLAPSE_3 = [  # Flat on ground (lying on left side)
    "................",
    "................",
    "................",
    "................",
    "................",
    "................",
    "................",
    "................",
    "................",
    "................",
    "................",
    "..OOOOO.........",
    ".OHHLHhO........",
    "OhHHHHhOOOOOO...",
    "OHSSSSSuTTBBbO..",
    "OESSSSTTtaBBGO..",
    ".OdSSdOuTTaBGgO.",
    "..OOO..OOOOOOOO.",
    "...........OOOO.",
    "................",
    "................",
    "................",
]


# ===================================================================
# UP-FACING COLLAPSE (row 9) -- player faces away from camera
# ===================================================================

UP_COLLAPSE_0 = [  # Buckle
    "................",
    "................",
    "......OOOO......",
    ".....OHHHhO.....",
    "....OHHLHHHO....",
    "...OHHHHHHHHO...",
    "..OHHHHHHHHHO...",
    "..OHHHHHHHHHHO..",
    "..OHhHHHHHHhHO..",
    "..OHHhHHHHhHHO..",
    "...OhHHHHHHhO...",
    "....OhHHHHhO....",
    "....OuTTTTuO....",
    "...OuTTttTTuO...",
    "..OdTTTttTTTdO..",
    "...OBBBBBBBbO...",
    "..OBbO....OBbO..",
    "..OGgO....OGgO..",
    "..OOO......OOO..",
    "................",
    "................",
    "................",
]

UP_COLLAPSE_1 = [  # Drop to knees
    "................",
    "................",
    "................",
    "................",
    "......OOOO......",
    ".....OHHHhO.....",
    "....OHHLHHHO....",
    "...OHHHHHHHHO...",
    "..OHHHHHHHHHO...",
    "..OHHHHHHHHHHO..",
    "..OHhHHHHHHhHO..",
    "..OHHhHHHHhHHO..",
    "...OhHHHHHHhO...",
    "....OhHHHHhO....",
    "...OuTTttTTuO...",
    "..OdTTTttTTTdO..",
    "..OBBBBaaBBBBO..",
    "..ObGaO..OaGbO..",
    "..OOOO....OOOO..",
    "................",
    "................",
    "................",
]

UP_COLLAPSE_2 = [  # Tipping backward
    "................",
    "................",
    "................",
    "................",
    "................",
    "................",
    "................",
    ".....OOOO.......",
    "....OHHHhO......",
    "...OHHLHHHO.....",
    "..OHHHHHHHHO....",
    "..OHhHHHHhHHO...",
    "..OHHhHHhHHhO...",
    "...OhHHHHhO.....",
    "...OuTTtTTuO....",
    "..OBBBBaaBBBBO..",
    "..ObGaO..OaGbO..",
    "..OOOO....OOOO..",
    "................",
    "................",
    "................",
    "................",
]

UP_COLLAPSE_3 = [  # Flat on ground (face-up, face visible from above)
    "................",
    "................",
    "................",
    "................",
    "................",
    "................",
    "................",
    "................",
    "................",
    "................",
    "................",
    "...OOOOOOO......",
    "..OHHLHHHhO.....",
    "..OhSSSSSSHO....",
    "..OhESSSSSHhO...",
    "...OuTTtTTuO....",
    "...OBBaaBBbO....",
    "...OGgO.OGgO....",
    "...OOO...OOO....",
    "................",
    "................",
    "................",
]


# ===================================================================
# Rendering
# ===================================================================

def render_frame(pixel_rows: list[str]) -> Image.Image:
    """Convert a list of palette-character strings into a 32x32 RGBA Image."""
    img = Image.new("RGBA", (FRAME_W, FRAME_H), TRANSPARENT)
    for row_idx, row_str in enumerate(pixel_rows):
        padded = row_str.ljust(ART_W, '.')[:ART_W]
        for col_idx, ch in enumerate(padded):
            color = PALETTE.get(ch, TRANSPARENT)
            if color[3] > 0:
                px = ART_OX + col_idx
                py = ART_OY + row_idx
                if 0 <= px < FRAME_W and 0 <= py < FRAME_H:
                    img.putpixel((px, py), color)
    return img


def mirror_horizontal(img: Image.Image) -> Image.Image:
    """Flip an image horizontally (LEFT -> RIGHT)."""
    return img.transpose(Image.FLIP_LEFT_RIGHT)


def recolor_companion(frame: Image.Image) -> Image.Image:
    """Swap hair and shirt colors to the companion palette."""
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
            elif color == SHIRT_MID:
                pixels[x, y] = SHIRT_RED_MID
            elif color == SHIRT_DARK:
                pixels[x, y] = SHIRT_RED_DARK
    return result


def build_direction_rows() -> list[list[Image.Image]]:
    """Build collapse frame rows ordered as Down, Left, Right, Up."""
    down_frames = [
        render_frame(DOWN_COLLAPSE_0),
        render_frame(DOWN_COLLAPSE_1),
        render_frame(DOWN_COLLAPSE_2),
        render_frame(DOWN_COLLAPSE_3),
    ]
    left_frames = [
        render_frame(LEFT_COLLAPSE_0),
        render_frame(LEFT_COLLAPSE_1),
        render_frame(LEFT_COLLAPSE_2),
        render_frame(LEFT_COLLAPSE_3),
    ]
    right_frames = [mirror_horizontal(f) for f in left_frames]
    up_frames = [
        render_frame(UP_COLLAPSE_0),
        render_frame(UP_COLLAPSE_1),
        render_frame(UP_COLLAPSE_2),
        render_frame(UP_COLLAPSE_3),
    ]
    return [down_frames, left_frames, right_frames, up_frames]


def write_collapse_rows(
    sheet_path: str,
    directional_frames: list[list[Image.Image]],
) -> None:
    """Preserve rows 0-5 and write/overwrite collapse rows 6-9."""
    sheet = Image.open(sheet_path).convert("RGBA")
    existing_h = min(sheet.height, EXISTING_ROWS * FRAME_H)

    new_sheet = Image.new("RGBA", (FRAME_W * 4, NEW_SHEET_H), TRANSPARENT)
    new_sheet.paste(sheet.crop((0, 0, FRAME_W * 4, existing_h)), (0, 0))

    for row_index, frames in enumerate(directional_frames):
        for col, frame in enumerate(frames):
            x = col * FRAME_W
            y = (COLLAPSE_ROW_START + row_index) * FRAME_H
            new_sheet.paste(frame, (x, y))

    new_sheet.save(sheet_path)


def main() -> None:
    print("Generating death-collapse frames (pixel-map style)...")

    generic_rows = build_direction_rows()
    companion_rows = [
        [recolor_companion(f) for f in row] for row in generic_rows
    ]

    generic_path = os.path.join(CONTENT_DIR, "generic_character_sheet.png")
    companion_path = os.path.join(CONTENT_DIR, "companion_character_sheet.png")

    write_collapse_rows(generic_path, generic_rows)
    write_collapse_rows(companion_path, companion_rows)

    print(f"  Updated: {generic_path}")
    print(f"  Updated: {companion_path}")
    print("Done.")


if __name__ == "__main__":
    main()
