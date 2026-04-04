"""
Add sitting frames to character sprite sheets (all 4 directions).

Appends Row 4 with sitting poses for each direction to the existing
4×4 walk-cycle sprite sheets, producing a 128×160 sheet.

Row 4 layout matches the walk rows:
  Col 0 = Down  (facing camera, seated)
  Col 1 = Left  (side view, seated)
  Col 2 = Right (mirrored Left, seated)
  Col 3 = Up    (back view, seated)

Input:
  - tooling/sprites/output/generic_character_sheet_backup.png   (original 128×128)
  - tooling/sprites/output/companion_character_sheet_backup.png  (original 128×128)

Output (in-place update):
  - src/RiverRats.Game/Content/Sprites/generic_character_sheet.png   (128×160)
  - src/RiverRats.Game/Content/Sprites/companion_character_sheet.png (128×160)
"""

from PIL import Image
import os

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
REPO_ROOT = os.path.abspath(os.path.join(SCRIPT_DIR, "..", ".."))
CONTENT_DIR = os.path.join(REPO_ROOT, "src", "RiverRats.Game", "Content", "Sprites")
OUTPUT_DIR = os.path.join(SCRIPT_DIR, "output")
os.makedirs(OUTPUT_DIR, exist_ok=True)

FRAME_W = 32
FRAME_H = 32
ART_W = 16
ART_H = 22
ART_OX = (FRAME_W - ART_W) // 2   # 8
ART_OY = FRAME_H - ART_H - 2      # 8

# Palette from generate_character.py
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

# ===========================
# SITTING DOWN  (facing camera / front view)
# ===========================
# Upper body from DOWN_IDLE, shifted down 2 rows.
# Lower body: legs together, shortened, conveying seated position.
SITTING_DOWN = [
    "................",  #  0  empty
    "................",  #  1  empty
    "......OOOO......",  #  2  hair top
    ".....OHHHhO.....",  #  3
    "....OHHLHHHO....",  #  4  highlight
    "...OHHHHHHHHO...",  #  5
    "..OHHHHHHHHHO...",  #  6  widest hair
    "..OHHsSSSSsHHO..",  #  7  forehead + hair sides
    "..OHsESSSSEsHO..",  #  8  eyes row
    "..OHsSSSSSSSHO..",  #  9  mid face
    "...OsSSSSSSsO...",  # 10  lower face
    "....OdsSSSdO....",  # 11  chin
    "....OsTTTTsO....",  # 12  neck/collar
    "...OuTTttTTuO...",  # 13  shirt
    "..OsTTTttTTTsO..",  # 14  shirt + arms on lap
    "..OdTTTttTTTdO..",  # 15  shirt + arms
    "...OuBBBBBBuO...",  # 16  shirt→pants transition
    "...OBBBBBBBO....",  # 17  pants (legs together, seated)
    "...OBBaaBBBO....",  # 18  pants crease
    "...OgGOOOgGO....",  # 19  shoes/feet together
    "...OOOO.OOOO....",  # 20  outline bottom
    "................",  # 21  empty
]

# ===========================
# SITTING LEFT  (side view, facing left)
# ===========================
# Upper body from LEFT_IDLE, shifted down 2 rows.
# Lower body: knees bent forward (leftward), sitting profile.
SITTING_LEFT = [
    "................",  #  0  empty
    "................",  #  1  empty
    "......OOOOO.....",  #  2  hair top
    ".....OHHHHhO....",  #  3
    "....OHHLHHHhO...",  #  4
    "...OHHHHHHHHhO..",  #  5
    "...OHHHHHHHHhO..",  #  6
    "...OHSSSSSSHhO..",  #  7  face visible
    "...OESSSSSShHO..",  #  8  eye
    "...OsSSSSSSHhO..",  #  9
    "....OSSSSSShO...",  # 10
    ".....OdsSSdO....",  # 11  chin
    ".....OsTTTsO....",  # 12  neck
    "....OuTTtTTuO...",  # 13  shirt
    "...OsTTTtTTTuO..",  # 14  shirt + arms
    "...OdTTTtTTTuO..",  # 15  shirt + arms
    "....OuBBBBuO....",  # 16  shirt→pants
    "...OBBBBaBBO....",  # 17  pants top, knees forward
    "...OBBaBBBO.....",  # 18  pants, legs bent left
    "...OgGgGOO......",  # 19  feet together (side)
    "...OOOOO........",  # 20  outline bottom
    "................",  # 21  empty
]

# ===========================
# SITTING UP  (back view, facing away)
# ===========================
# Upper body from UP_IDLE, shifted down 2 rows.
# Lower body: same as sitting down but with hair covering head.
SITTING_UP = [
    "................",  #  0  empty
    "................",  #  1  empty
    "......OOOO......",  #  2  hair top
    ".....OHHHhO.....",  #  3
    "....OHHLHHHO....",  #  4
    "...OHHHHHHHHO...",  #  5
    "..OHHHHHHHHHO...",  #  6
    "..OHHHHHHHHHHO..",  #  7  full hair (no face)
    "..OHhHHHHHHhHO..",  #  8  hair texture
    "..OHHhHHHHhHHO..",  #  9
    "...OhHHHHHHhO...",  # 10  lower hair
    "....OhHHHHhO....",  # 11  nape
    "....OuTTTTuO....",  # 12  collar
    "...OuTTttTTuO...",  # 13  shirt
    "..OsTTTttTTTsO..",  # 14  shirt + arms
    "..OdTTTttTTTdO..",  # 15  shirt + arms
    "...OuBBBBBBuO...",  # 16  shirt→pants
    "...OBBBBBBBO....",  # 17  pants (legs together)
    "...OBBaaBBBO....",  # 18  pants crease
    "...OgGOOOgGO....",  # 19  shoes
    "...OOOO.OOOO....",  # 20  outline bottom
    "................",  # 21  empty
]


def render_frame(pixel_rows):
    """Convert a list of palette-character strings into a 32x32 RGBA Image."""
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


def apply_companion_recolor(frame):
    """Swap hair→cap and shirt colors for companion palette."""
    CAP = (186, 34, 34, 255)
    CAP_DARK = (132, 20, 20, 255)
    CAP_LIGHT = (232, 76, 76, 255)
    SHIRT_RED = (213, 48, 48, 255)
    SHIRT_RED_MID = (170, 33, 33, 255)
    SHIRT_RED_DARK = (129, 21, 21, 255)

    pixels = frame.load()
    for y in range(frame.height):
        for x in range(frame.width):
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
    return frame


def add_cap_brim_down(frame):
    """Add baseball-cap brim on the down-facing sitting frame (forehead area)."""
    pixels = frame.load()
    brim_y = ART_OY + 7
    for bx in range(ART_OX + 3, ART_OX + 13):
        if 0 <= bx < FRAME_W and 0 <= brim_y < FRAME_H:
            pixels[bx, brim_y] = OUTLINE


def add_cap_brim_left(frame):
    """Add cap brim on left-facing sitting frame."""
    pixels = frame.load()
    brim_y = ART_OY + 7
    for bx in range(ART_OX + 2, ART_OX + 8):
        if 0 <= bx < FRAME_W and 0 <= brim_y < FRAME_H:
            pixels[bx, brim_y] = OUTLINE


def add_cap_brim_right(frame):
    """Add cap brim on right-facing sitting frame (mirror of left)."""
    pixels = frame.load()
    brim_y = ART_OY + 7
    for bx in range(ART_OX + 8, ART_OX + 14):
        if 0 <= bx < FRAME_W and 0 <= brim_y < FRAME_H:
            pixels[bx, brim_y] = OUTLINE


def build_sitting_row():
    """Build all 4 sitting frames: Down, Left, Right, Up."""
    sit_down = render_frame(SITTING_DOWN)
    sit_left = render_frame(SITTING_LEFT)
    sit_right = mirror_horizontal(sit_left)
    sit_up = render_frame(SITTING_UP)
    return [sit_down, sit_left, sit_right, sit_up]


def build_companion_sitting_row():
    """Build companion sitting frames with recoloring and cap details."""
    frames = build_sitting_row()
    for f in frames:
        apply_companion_recolor(f)
    add_cap_brim_down(frames[0])
    add_cap_brim_left(frames[1])
    add_cap_brim_right(frames[2])
    # No brim for up-facing (back view, cap brim hidden)
    return frames


def add_sitting_row_to_sheet(base_sheet_path, frames, output_path):
    """Expand a 128×128 sheet to 128×160 by appending a full sitting row."""
    sheet = Image.open(base_sheet_path).convert('RGBA')
    original_h = sheet.size[1]

    new_sheet = Image.new('RGBA', (128, original_h + FRAME_H), TRANSPARENT)
    # If sheet was already expanded (re-running), crop back to original 4 rows.
    new_sheet.paste(sheet.crop((0, 0, 128, min(original_h, 128))), (0, 0))

    # Place sitting frames in row 4: col 0=Down, 1=Left, 2=Right, 3=Up
    for col, frame in enumerate(frames):
        new_sheet.paste(frame, (col * FRAME_W, 4 * FRAME_H))

    new_sheet.save(output_path)
    return new_sheet


def main():
    print("Generating 4-directional sitting frames for character sheets...")

    # --- Generic character ---
    generic_frames = build_sitting_row()

    # Use backup if available (original 128×128), otherwise use current
    generic_backup = os.path.join(OUTPUT_DIR, "generic_character_sheet_backup.png")
    generic_content = os.path.join(CONTENT_DIR, "generic_character_sheet.png")

    if not os.path.exists(generic_backup):
        # First run — save backup of original 128×128
        orig = Image.open(generic_content)
        orig.crop((0, 0, 128, 128)).save(generic_backup)
        print(f"  Backup: {generic_backup}")

    add_sitting_row_to_sheet(generic_backup, generic_frames, generic_content)
    print(f"  Updated: {generic_content}")

    # --- Companion character ---
    companion_frames = build_companion_sitting_row()

    companion_backup = os.path.join(OUTPUT_DIR, "companion_character_sheet_backup.png")
    companion_content = os.path.join(CONTENT_DIR, "companion_character_sheet.png")

    if not os.path.exists(companion_backup):
        orig = Image.open(companion_content)
        orig.crop((0, 0, 128, 128)).save(companion_backup)
        print(f"  Backup: {companion_backup}")

    add_sitting_row_to_sheet(companion_backup, companion_frames, companion_content)
    print(f"  Updated: {companion_content}")

    # --- Save individual frames for inspection ---
    dir_names = ["down", "left", "right", "up"]
    for i, name in enumerate(dir_names):
        generic_frames[i].save(os.path.join(OUTPUT_DIR, f"sitting_{name}_generic.png"))
        companion_frames[i].save(os.path.join(OUTPUT_DIR, f"sitting_{name}_companion.png"))
        generic_frames[i].resize((FRAME_W * 4, FRAME_H * 4), Image.NEAREST).save(
            os.path.join(OUTPUT_DIR, f"sitting_{name}_generic_4x.png"))
        companion_frames[i].resize((FRAME_W * 4, FRAME_H * 4), Image.NEAREST).save(
            os.path.join(OUTPUT_DIR, f"sitting_{name}_companion_4x.png"))

    print("Done! Character sheets updated with 4-directional sitting frames.")


if __name__ == "__main__":
    main()
