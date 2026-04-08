"""
Generate a Mom NPC character sprite sheet.

Character style: pixel art, chibi proportions, ~40 year-old mom from the 90s.
- Brown hair in a short bob.
- Teal/green v-neck top.
- High-waisted blue mom-jeans.
- White sneakers.

Uses 16×27 art inside 32×32 frames (5 pixels taller than the kids' 16×22)
and the same sheet layout as the main character (generate_character.py).

Output:
  - tooling/sprites/output/mom_character_sheet.png
  - src/DogDays.Game/Content/Sprites/mom_character_sheet.png

Layout (4 cols × 4 rows, 32×32 per frame):
  Row 0 = Down   (facing camera)
  Row 1 = Left
  Row 2 = Right  (mirrored Left)
  Row 3 = Up     (facing away)

  Col 0 = Idle
  Col 1 = Walk step A
  Col 2 = Idle   (same as col 0)
  Col 3 = Walk step B
"""

from PIL import Image
import os

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
REPO_ROOT = os.path.abspath(os.path.join(SCRIPT_DIR, "..", ".."))
OUTPUT_DIR = os.path.join(SCRIPT_DIR, "output")
os.makedirs(OUTPUT_DIR, exist_ok=True)

OUTPUT_SHEET_PATH = os.path.join(OUTPUT_DIR, "mom_character_sheet.png")
CONTENT_SHEET_PATH = os.path.join(
    REPO_ROOT,
    "src", "DogDays.Game", "Content", "Sprites", "mom_character_sheet.png",
)

# ---------------------------------------------------------------------------
# Frame / sheet geometry  (must match generate_character.py)
# ---------------------------------------------------------------------------
FRAME_W = 32
FRAME_H = 32
SHEET_COLS = 4
SHEET_ROWS = 4

# Character art within each 32×32 frame (taller than kids' 22 to read as adult)
ART_W = 16
ART_H = 27
ART_OX = (FRAME_W - ART_W) // 2   # 8
ART_OY = FRAME_H - ART_H - 2      # 3

# ---------------------------------------------------------------------------
# Color palette — 90s mom
# ---------------------------------------------------------------------------
TRANSPARENT = (0, 0, 0, 0)
OUTLINE     = (26, 10, 13, 255)

# Brown bob hair
HAIR        = (122, 72, 40, 255)
HAIR_DARK   = (88, 50, 28, 255)
HAIR_LIGHT  = (158, 100, 56, 255)

# Skin — same tones as main character for consistency
SKIN        = (255, 211, 170, 255)
SKIN_MID    = (225, 170, 145, 255)
SKIN_DARK   = (176, 136, 108, 255)

EYE         = (18, 3, 8, 255)

# Raspberry-pink v-neck top
SHIRT       = (220, 100, 130, 255)
SHIRT_MID   = (190, 75, 108, 255)
SHIRT_DARK  = (158, 55, 85, 255)

# Lavender mom jeans
PANTS       = (160, 130, 195, 255)
PANTS_MID   = (135, 108, 172, 255)
PANTS_DARK  = (110, 88, 148, 255)

# Pink sneakers
SHOE        = (240, 195, 210, 255)
SHOE_DARK   = (210, 160, 178, 255)

# ---------------------------------------------------------------------------
# Palette map — character codes → RGBA
# ---------------------------------------------------------------------------
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

# ===========================
# DOWN-FACING  (front view)
# ===========================
DOWN_IDLE = [
    "......OOOO......",  #  0  hair top
    ".....OHHHHO.....",  #  1
    "....OHHLHHHO....",  #  2  highlight
    "...OHHHHHHHHO...",  #  3
    "..OHHHHHHHHhO...",  #  4  widest hair
    "..OHHsSSSSsHHO..",  #  5  forehead + hair sides
    "..OHsESSSSEsHO..",  #  6  eyes row
    "..OHsSSSSSSSHO..",  #  7  mid face
    "...OsSSSSSSsO...",  #  8  lower face
    "....OdsSSSdO....",  #  9  chin
    "....OsTTTTsO....",  # 10  neck/collar (v-neck)
    "...OuTTttTTuO...",  # 11  shirt
    "..OsTTTttTTTsO..",  # 12  shirt + arms
    "..OdTTTttTTTdO..",  # 13  shirt + arms
    "..OdTTTttTTTdO..",  # 14  shirt + arms
    "...OuTTttTTuO...",  # 15  shirt lower
    "...OuTTTTTTuO...",  # 16  shirt bottom / waist
    "....OBBBBBbO....",  # 17  pants top
    "....OBBaaBBO....",  # 18  pants
    "....OBBaaBBO....",  # 19  pants lower
    "....ObO..ObO....",  # 20  legs
    "....ObO..ObO....",  # 21  legs
    "....ObO..ObO....",  # 22  legs
    "....ObO..ObO....",  # 23  legs
    "...OgGO..OgGO...",  # 24  shoes
    "...OOO....OOO...",  # 25  outline bottom
    "................",  # 26
]

DOWN_WALK_A = [
    "......OOOO......",
    ".....OHHHHO.....",
    "....OHHLHHHO....",
    "...OHHHHHHHHO...",
    "..OHHHHHHHHhO...",
    "..OHHsSSSSsHHO..",
    "..OHsESSSSEsHO..",
    "..OHsSSSSSSSHO..",
    "...OsSSSSSSsO...",
    "....OdsSSSdO....",
    "....OsTTTTsO....",
    "...OuTTttTTuO...",
    "..OdTTTttTTTsO..",
    "..OdTTTttTTTsO..",
    "..OdTTTttTTTsO..",
    "...OuTTttTTuO...",
    "...OuTTTTTTuO...",
    "....OBBBBBbO....",
    "....OBBaaBBO....",
    "....OBBaaBBO....",
    "...ObBO...ObO...",
    "...ObO....ObO...",
    "...ObO....ObO...",
    "...ObO....ObO...",
    "..OgGO....OgGO..",
    "..OOO......OOO..",
    "................",
]

DOWN_WALK_B = [
    "......OOOO......",
    ".....OHHHHO.....",
    "....OHHLHHHO....",
    "...OHHHHHHHHO...",
    "..OHHHHHHHHhO...",
    "..OHHsSSSSsHHO..",
    "..OHsESSSSEsHO..",
    "..OHsSSSSSSSHO..",
    "...OsSSSSSSsO...",
    "....OdsSSSdO....",
    "....OsTTTTsO....",
    "...OuTTttTTuO...",
    "..OsTTTttTTTdO..",
    "..OsTTTttTTTdO..",
    "..OsTTTttTTTdO..",
    "...OuTTttTTuO...",
    "...OuTTTTTTuO...",
    "....OBBBBBbO....",
    "....OBBaaBBO....",
    "....OBBaaBBO....",
    "....ObO...ObO...",
    "....ObO...ObO...",
    "....ObO...ObO...",
    "....ObO...ObO...",
    "...OgGO...OgGO..",
    "...OOO....OOO...",
    "................",
]

# ===========================
# UP-FACING  (back view)
# ===========================
UP_IDLE = [
    "......OOOO......",
    ".....OHHHHO.....",
    "....OHHLHHHO....",
    "...OHHHHHHHHO...",
    "..OHHHHHHHHhO...",
    "..OHHHHHHHHHHO..",
    "..OHhHHHHHHhHO..",
    "..OHHhHHHHhHHO..",
    "...OhHHHHHHhO...",
    "....OhHHHHhO....",
    "....OuHHHHuO....",
    "...OuThHhHTuO...",
    "..OsTThHhTTTsO..",
    "..OdTTThTTTTdO..",
    "..OdTTTttTTTdO..",
    "...OuTTttTTuO...",
    "...OuTTTTTTuO...",
    "....OBBBBBbO....",
    "....OBBaaBBO....",
    "....OBBaaBBO....",
    "....ObO..ObO....",
    "....ObO..ObO....",
    "....ObO..ObO....",
    "....ObO..ObO....",
    "...OgGO..OgGO...",
    "...OOO....OOO...",
    "................",
]

UP_WALK_A = [
    "......OOOO......",
    ".....OHHHHO.....",
    "....OHHLHHHO....",
    "...OHHHHHHHHO...",
    "..OHHHHHHHHhO...",
    "..OHHHHHHHHHHO..",
    "..OHhHHHHHHhHO..",
    "..OHHhHHHHhHHO..",
    "...OhHHHHHHhO...",
    "....OhHHHHhO....",
    "....OuHHHHuO....",
    "...OuThHhHTuO...",
    "..OdTThHhTTTsO..",
    "..OdTTThTTTTsO..",
    "..OdTTTttTTTsO..",
    "...OuTTttTTuO...",
    "...OuTTTTTTuO...",
    "....OBBBBBbO....",
    "....OBBaaBBO....",
    "....OBBaaBBO....",
    "...ObBO...ObO...",
    "...ObO....ObO...",
    "...ObO....ObO...",
    "...ObO....ObO...",
    "..OgGO....OgGO..",
    "..OOO......OOO..",
    "................",
]

UP_WALK_B = [
    "......OOOO......",
    ".....OHHHHO.....",
    "....OHHLHHHO....",
    "...OHHHHHHHHO...",
    "..OHHHHHHHHhO...",
    "..OHHHHHHHHHHO..",
    "..OHhHHHHHHhHO..",
    "..OHHhHHHHhHHO..",
    "...OhHHHHHHhO...",
    "....OhHHHHhO....",
    "....OuHHHHuO....",
    "...OuThHhHTuO...",
    "..OsTTThHhTTdO..",
    "..OsTTTThTTTdO..",
    "..OsTTTttTTTdO..",
    "...OuTTttTTuO...",
    "...OuTTTTTTuO...",
    "....OBBBBBbO....",
    "....OBBaaBBO....",
    "....OBBaaBBO....",
    "....ObO...ObO...",
    "....ObO...ObO...",
    "....ObO...ObO...",
    "....ObO...ObO...",
    "...OgGO...OgGO..",
    "...OOO....OOO...",
    "................",
]

# ===========================
# LEFT-FACING  (side view)
# ===========================
LEFT_IDLE = [
    "......OOOOO.....",
    ".....OHHHHhO....",
    "....OHHLHHHhO...",
    "...OHHHHHHHHhO..",
    "...OHHHHHHHHhO..",
    "...OHSSSSSSHhO..",
    "...OESSSSSShHO..",
    "...OsSSSSSSHhO..",
    "....OSSSSSShHO..",
    ".....OdsSSdhO...",
    ".....OsTTTshO...",
    "....OuTTtTThO...",
    "...OsTTTtTTTuO..",
    "...OdTTTtTTTuO..",
    "...OdTTTtTTTuO..",
    "....OuTTtTTuO...",
    "....OuTTTTTuO...",
    ".....OBBBBbO....",
    ".....OBBaaBbO...",
    ".....OBBaaBbO...",
    ".....ObbObbO....",
    ".....ObbObbO....",
    ".....ObbObbO....",
    ".....ObbObbO....",
    "....OgGOOgGO....",
    "....OOO..OOO....",
    "................",
]

LEFT_WALK_A = [
    "......OOOOO.....",
    ".....OHHHHhO....",
    "....OHHLHHHhO...",
    "...OHHHHHHHHhO..",
    "...OHHHHHHHHhO..",
    "...OHSSSSSSHhO..",
    "...OESSSSSShHO..",
    "...OsSSSSSSHhO..",
    "....OSSSSSShHO..",
    ".....OdsSSdhO...",
    ".....OsTTTshO...",
    "....OuTTtTThO...",
    "...OdTTTtTTTuO..",
    "...OdTTTtTTTuO..",
    "...OdTTTtTTTuO..",
    "....OuTTtTTuO...",
    "....OuTTTTTuO...",
    ".....OBBBBbO....",
    "....OBBaaBbO....",
    "....OBBaaBbO....",
    "...ObbO..ObbO...",
    "..ObbO...ObbO...",
    "..ObbO...ObbO...",
    "..ObbO...ObbO...",
    "..OGgO...OGgO...",
    "..OOO.....OOO...",
    "................",
]

LEFT_WALK_B = [
    "......OOOOO.....",
    ".....OHHHHhO....",
    "....OHHLHHHhO...",
    "...OHHHHHHHHhO..",
    "...OHHHHHHHHhO..",
    "...OHSSSSSSHhO..",
    "...OESSSSSShHO..",
    "...OsSSSSSSHhO..",
    "....OSSSSSShHO..",
    ".....OdsSSdhO...",
    ".....OsTTTshO...",
    "....OuTTtTThO...",
    "...OsTTTtTTTdO..",
    "...OsTTTtTTTdO..",
    "...OsTTTtTTTdO..",
    "....OuTTtTTuO...",
    "....OuTTTTTuO...",
    ".....OBBBBbO....",
    ".....OBBaaBbO...",
    ".....OBBaaBbO...",
    ".....ObbO.ObbO..",
    ".....ObbO..ObbO.",
    ".....ObbO..ObbO.",
    ".....ObbO..ObbO.",
    ".....OGgO..OGgO.",
    ".....OOO....OOO.",
    "................",
]


def render_frame(pixel_rows):
    """Convert a list of palette-character strings into a 32×32 RGBA Image."""
    img = Image.new('RGBA', (FRAME_W, FRAME_H), TRANSPARENT)
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


def mirror_horizontal(img):
    """Flip an image horizontally (for LEFT → RIGHT)."""
    return img.transpose(Image.FLIP_LEFT_RIGHT)


def build_sprite_sheet():
    """Build the full 4×4 sprite sheet."""
    sheet_w = SHEET_COLS * FRAME_W   # 128
    sheet_h = SHEET_ROWS * FRAME_H   # 128
    sheet = Image.new('RGBA', (sheet_w, sheet_h), TRANSPARENT)

    rows = []

    # Row 0: Down
    down_frames = [DOWN_IDLE, DOWN_WALK_A, DOWN_IDLE, DOWN_WALK_B]
    row_down = [render_frame(f) for f in down_frames]
    rows.append(row_down)

    # Row 1: Left
    left_frames = [LEFT_IDLE, LEFT_WALK_A, LEFT_IDLE, LEFT_WALK_B]
    row_left = [render_frame(f) for f in left_frames]
    rows.append(row_left)

    # Row 2: Right (mirror of left)
    row_right = [mirror_horizontal(f) for f in row_left]
    rows.append(row_right)

    # Row 3: Up
    up_frames = [UP_IDLE, UP_WALK_A, UP_IDLE, UP_WALK_B]
    row_up = [render_frame(f) for f in up_frames]
    rows.append(row_up)

    for row_idx, row_images in enumerate(rows):
        for col_idx, frame_img in enumerate(row_images):
            x = col_idx * FRAME_W
            y = row_idx * FRAME_H
            sheet.paste(frame_img, (x, y))

    return sheet


def main():
    sheet = build_sprite_sheet()
    sheet.save(OUTPUT_SHEET_PATH)
    sheet.save(CONTENT_SHEET_PATH)
    print(f"Generated: {OUTPUT_SHEET_PATH}")
    print(f"Copied to: {CONTENT_SHEET_PATH}")


if __name__ == "__main__":
    main()
