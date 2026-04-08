"""
Generate a Grandpa NPC character sprite sheet.

Character style: pixel art, chibi proportions, elderly man ~70 years old.
- Partially bald on top, grey hair on sides.
- Grey bushy mustache.
- Dark-green flannel shirt (rolled sleeves showing forearms).
- Brown work trousers.
- Dark brown boots.

Uses 18×29 art inside 32×32 frames (slightly wider and taller than Mom's
16×27) and the same sheet layout as the main character (generate_character.py).

Output:
  - tooling/sprites/output/grandpa_character_sheet.png
  - src/DogDays.Game/Content/Sprites/grandpa_character_sheet.png

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

OUTPUT_SHEET_PATH = os.path.join(OUTPUT_DIR, "grandpa_character_sheet.png")
CONTENT_SHEET_PATH = os.path.join(
    REPO_ROOT,
    "src", "DogDays.Game", "Content", "Sprites", "grandpa_character_sheet.png",
)

# ---------------------------------------------------------------------------
# Frame / sheet geometry
# ---------------------------------------------------------------------------
FRAME_W = 32
FRAME_H = 32
SHEET_COLS = 4
SHEET_ROWS = 4

# Character art within each 32×32 frame (wider/taller than Mom's 16×27)
ART_W = 18
ART_H = 29
ART_OX = (FRAME_W - ART_W) // 2   # 7
ART_OY = FRAME_H - ART_H - 1      # 2

# ---------------------------------------------------------------------------
# Color palette — elderly grandpa
# ---------------------------------------------------------------------------
TRANSPARENT = (0, 0, 0, 0)
OUTLINE     = (26, 10, 13, 255)

# Grey hair (sides + back, partially bald on top)
HAIR        = (170, 165, 160, 255)
HAIR_DARK   = (130, 125, 120, 255)
HAIR_LIGHT  = (200, 195, 190, 255)

# Bald scalp — pinkish skin tone
SCALP       = (235, 195, 170, 255)
SCALP_DARK  = (210, 175, 150, 255)

# Skin — slightly weather-worn
SKIN        = (240, 200, 165, 255)
SKIN_MID    = (210, 170, 140, 255)
SKIN_DARK   = (176, 136, 108, 255)

EYE         = (18, 3, 8, 255)

# Grey mustache
MUSTACHE    = (150, 145, 140, 255)

# Dark green flannel shirt
SHIRT       = (60, 100, 65, 255)
SHIRT_MID   = (45, 78, 50, 255)
SHIRT_DARK  = (32, 60, 38, 255)

# Brown work trousers
PANTS       = (120, 90, 60, 255)
PANTS_MID   = (100, 72, 48, 255)
PANTS_DARK  = (80, 58, 38, 255)

# Dark brown boots
SHOE        = (75, 52, 35, 255)
SHOE_DARK   = (55, 38, 25, 255)

# ---------------------------------------------------------------------------
# Palette map — character codes → RGBA
# ---------------------------------------------------------------------------
PALETTE = {
    '.': TRANSPARENT,
    'O': OUTLINE,
    'H': HAIR,
    'h': HAIR_DARK,
    'L': HAIR_LIGHT,
    'K': SCALP,         # bald scalp
    'k': SCALP_DARK,
    'S': SKIN,
    's': SKIN_MID,
    'd': SKIN_DARK,
    'E': EYE,
    'M': MUSTACHE,      # mustache
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
# 18 px wide, 29 px tall
DOWN_IDLE = [
    "......OOOOOO......",  #  0  bald scalp top
    ".....OKKKKKkO.....",  #  1  scalp
    "....OKKKKKKKkO....",  #  2  scalp
    "...OKKKKKKKKKkO...",  #  3  scalp wider
    "..OHHKKKKKKKKHhO..",  #  4  hair sides, bald center
    "..OHHsSSSSSSsHhO..",  #  5  forehead + hair sides
    "..OHsESSSSSSEsHO..",  #  6  eyes row
    "..OHsSSSSSSSSSHO..",  #  7  mid face
    "...OsSSMMMSSsO....",  #  8  mustache row
    "....OdsMMMSdO.....",  #  9  mustache + chin
    "....OsSSSSSsO.....",  # 10  neck
    "...OuTTTtTTTuO....",  # 11  shirt collar
    "..OsTTTTtTTTTsO...",  # 12  shirt + arms
    "..OdTTTTtTTTTdO...",  # 13  shirt + arms
    "..OdTTTTtTTTTdO...",  # 14  shirt + arms
    "..OdTTTTtTTTTdO...",  # 15  shirt + arms
    "...OuTTTtTTTuO....",  # 16  shirt lower
    "...OuTTTTTTTuO....",  # 17  shirt bottom / waist
    "....OBBBBBBbO.....",  # 18  pants top
    "....OBBBaaBBbO....",  # 19  pants
    "....OBBBaaBBbO....",  # 20  pants
    "....OBBBaaBBbO....",  # 21  pants lower
    ".....ObO..ObO.....",  # 22  legs
    ".....ObO..ObO.....",  # 23  legs
    ".....ObO..ObO.....",  # 24  legs
    "....OgGO..OgGO....",  # 25  boots
    "....OgGO..OgGO....",  # 26  boots lower
    "....OOO....OOO....",  # 27  outline bottom
    "..................",  # 28
]

DOWN_WALK_A = [
    "......OOOOOO......",
    ".....OKKKKKkO.....",
    "....OKKKKKKKkO....",
    "...OKKKKKKKKKkO...",
    "..OHHKKKKKKKKHhO..",
    "..OHHsSSSSSSsHhO..",
    "..OHsESSSSSSEsHO..",
    "..OHsSSSSSSSSSHO..",
    "...OsSSMMMSSsO....",
    "....OdsMMMSdO.....",
    "....OsSSSSSsO.....",
    "...OuTTTtTTTuO....",
    "..OdTTTTtTTTTsO...",
    "..OdTTTTtTTTTsO...",
    "..OdTTTTtTTTTsO...",
    "..OdTTTTtTTTTsO...",
    "...OuTTTtTTTuO....",
    "...OuTTTTTTTuO....",
    "....OBBBBBBbO.....",
    "....OBBBaaBBbO....",
    "....OBBBaaBBbO....",
    "...OBBBaaBBbO.....",
    "...ObBO....ObO....",
    "...ObO.....ObO....",
    "...ObO.....ObO....",
    "..OgGO.....OgGO...",
    "..OgGO.....OgGO...",
    "..OOO.......OOO...",
    "..................",
]

DOWN_WALK_B = [
    "......OOOOOO......",
    ".....OKKKKKkO.....",
    "....OKKKKKKKkO....",
    "...OKKKKKKKKKkO...",
    "..OHHKKKKKKKKHhO..",
    "..OHHsSSSSSSsHhO..",
    "..OHsESSSSSSEsHO..",
    "..OHsSSSSSSSSSHO..",
    "...OsSSMMMSSsO....",
    "....OdsMMMSdO.....",
    "....OsSSSSSsO.....",
    "...OuTTTtTTTuO....",
    "..OsTTTTtTTTTdO...",
    "..OsTTTTtTTTTdO...",
    "..OsTTTTtTTTTdO...",
    "..OsTTTTtTTTTdO...",
    "...OuTTTtTTTuO....",
    "...OuTTTTTTTuO....",
    "....OBBBBBBbO.....",
    "....OBBBaaBBbO....",
    "....OBBBaaBBbO....",
    ".....OBBBaaBBbO...",
    ".....ObO....ObO...",
    ".....ObO....ObO...",
    ".....ObO....ObO...",
    "....OgGO...OgGO...",
    "....OgGO...OgGO...",
    "....OOO.....OOO...",
    "..................",
]

# ===========================
# UP-FACING  (back view)
# ===========================
UP_IDLE = [
    "......OOOOOO......",
    ".....OKKKKKkO.....",
    "....OKKKKKKKkO....",
    "...OKKKKKKKKKkO...",
    "..OHHKKKKKKKKHhO..",
    "..OHHHHHHHHHHHhO..",
    "..OHhHHHHHHHHhHO..",
    "...OhHHHHHHHHhO...",
    "....OhsSSSSshO....",
    "....OdsSSSsdO.....",
    "....OuTTTTTTuO....",
    "...OuTTTtTTTTuO...",
    "..OsTTTTtTTTTTsO..",
    "..OdTTTTtTTTTTdO..",
    "..OdTTTTtTTTTTdO..",
    "..OdTTTTtTTTTTdO..",
    "...OuTTTtTTTuO....",
    "...OuTTTTTTTuO....",
    "....OBBBBBBbO.....",
    "....OBBBaaBBbO....",
    "....OBBBaaBBbO....",
    "....OBBBaaBBbO....",
    ".....ObO..ObO.....",
    ".....ObO..ObO.....",
    ".....ObO..ObO.....",
    "....OgGO..OgGO....",
    "....OgGO..OgGO....",
    "....OOO....OOO....",
    "..................",
]

UP_WALK_A = [
    "......OOOOOO......",
    ".....OKKKKKkO.....",
    "....OKKKKKKKkO....",
    "...OKKKKKKKKKkO...",
    "..OHHKKKKKKKKHhO..",
    "..OHHHHHHHHHHHhO..",
    "..OHhHHHHHHHHhHO..",
    "...OhHHHHHHHHhO...",
    "....OhsSSSSshO....",
    "....OdsSSSsdO.....",
    "....OuTTTTTTuO....",
    "...OuTTTtTTTTuO...",
    "..OdTTTTtTTTTTsO..",
    "..OdTTTTtTTTTTsO..",
    "..OdTTTTtTTTTTsO..",
    "..OdTTTTtTTTTTsO..",
    "...OuTTTtTTTuO....",
    "...OuTTTTTTTuO....",
    "....OBBBBBBbO.....",
    "....OBBBaaBBbO....",
    "....OBBBaaBBbO....",
    "...OBBBaaBBbO.....",
    "...ObBO....ObO....",
    "...ObO.....ObO....",
    "...ObO.....ObO....",
    "..OgGO.....OgGO...",
    "..OgGO.....OgGO...",
    "..OOO.......OOO...",
    "..................",
]

UP_WALK_B = [
    "......OOOOOO......",
    ".....OKKKKKkO.....",
    "....OKKKKKKKkO....",
    "...OKKKKKKKKKkO...",
    "..OHHKKKKKKKKHhO..",
    "..OHHHHHHHHHHHhO..",
    "..OHhHHHHHHHHhHO..",
    "...OhHHHHHHHHhO...",
    "....OhsSSSSshO....",
    "....OdsSSSsdO.....",
    "....OuTTTTTTuO....",
    "...OuTTTtTTTTuO...",
    "..OsTTTTtTTTTTdO..",
    "..OsTTTTtTTTTTdO..",
    "..OsTTTTtTTTTTdO..",
    "..OsTTTTtTTTTTdO..",
    "...OuTTTtTTTuO....",
    "...OuTTTTTTTuO....",
    "....OBBBBBBbO.....",
    "....OBBBaaBBbO....",
    "....OBBBaaBBbO....",
    ".....OBBBaaBBbO...",
    ".....ObO....ObO...",
    ".....ObO....ObO...",
    ".....ObO....ObO...",
    "....OgGO...OgGO...",
    "....OgGO...OgGO...",
    "....OOO.....OOO...",
    "..................",
]

# ===========================
# LEFT-FACING  (side view)
# ===========================
LEFT_IDLE = [
    ".......OOOOO......",
    "......OKKKKhO.....",
    ".....OKKKKKKhO....",
    "....OKKKKKKKKhO...",
    "....OHKKKKKKKhO...",
    "....OHSSSSSSKhO...",
    "....OESSSSSSHhO...",
    "....OsSSSSSSShO...",
    ".....OSSMMMShO....",
    "......OdMMMdhO....",
    "......OsSSSshO....",
    ".....OuTTTtTThO...",
    "....OsTTTTtTTTuO..",
    "....OdTTTTtTTTuO..",
    "....OdTTTTtTTTuO..",
    "....OdTTTTtTTTuO..",
    ".....OuTTTtTTuO...",
    ".....OuTTTTTTuO...",
    "......OBBBBBbO....",
    "......OBBaaBBbO...",
    "......OBBaaBBbO...",
    "......OBBaaBBbO...",
    "......ObbOObbO....",
    "......ObbOObbO....",
    "......ObbOObbO....",
    ".....OgGOOOgGO....",
    ".....OgGO.OgGO....",
    ".....OOO..OOO.....",
    "..................",
]

LEFT_WALK_A = [
    ".......OOOOO......",
    "......OKKKKhO.....",
    ".....OKKKKKKhO....",
    "....OKKKKKKKKhO...",
    "....OHKKKKKKKhO...",
    "....OHSSSSSSKhO...",
    "....OESSSSSSHhO...",
    "....OsSSSSSSShO...",
    ".....OSSMMMShO....",
    "......OdMMMdhO....",
    "......OsSSSshO....",
    ".....OuTTTtTThO...",
    "....OdTTTTtTTTuO..",
    "....OdTTTTtTTTuO..",
    "....OdTTTTtTTTuO..",
    "....OdTTTTtTTTuO..",
    ".....OuTTTtTTuO...",
    ".....OuTTTTTTuO...",
    "......OBBBBBbO....",
    ".....OBBaaBBbO....",
    ".....OBBaaBBbO....",
    "....OBBaaBBbO.....",
    "....ObbO...ObbO...",
    "...ObbO....ObbO...",
    "...ObbO....ObbO...",
    "...OGgO....OGgO...",
    "...OGgO....OGgO...",
    "...OOO......OOO...",
    "..................",
]

LEFT_WALK_B = [
    ".......OOOOO......",
    "......OKKKKhO.....",
    ".....OKKKKKKhO....",
    "....OKKKKKKKKhO...",
    "....OHKKKKKKKhO...",
    "....OHSSSSSSKhO...",
    "....OESSSSSSHhO...",
    "....OsSSSSSSShO...",
    ".....OSSMMMShO....",
    "......OdMMMdhO....",
    "......OsSSSshO....",
    ".....OuTTTtTThO...",
    "....OsTTTTtTTTdO..",
    "....OsTTTTtTTTdO..",
    "....OsTTTTtTTTdO..",
    "....OsTTTTtTTTdO..",
    ".....OuTTTtTTuO...",
    ".....OuTTTTTTuO...",
    "......OBBBBBbO....",
    "......OBBaaBBbO...",
    "......OBBaaBBbO...",
    ".......OBBaaBBbO..",
    "......ObbO..ObbO..",
    "......ObbO...ObbO.",
    "......ObbO...ObbO.",
    "......OGgO...OGgO.",
    "......OGgO...OGgO.",
    "......OOO.....OOO.",
    "..................",
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
