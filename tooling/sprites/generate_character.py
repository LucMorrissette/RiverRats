"""
Generate a 4-direction walking character sprite sheet.

Character style: pixel art, chibi proportions, orange hair, white shirt, blue jeans.
Matches the reference screenshot in reference_assets/.

Output:
  - generic_character_sheet.png  (128x128 sprite sheet, 4 cols × 4 rows, 32x32 per frame)
  - generic_character_preview.gif (animated walk preview)

Layout:
  Row 0 = Down   (facing camera)
  Row 1 = Left
  Row 2 = Right  (mirrored Left)
  Row 3 = Up     (facing away)

  Col 0 = Idle
  Col 1 = Walk step A  (left foot forward)
  Col 2 = Idle         (same as col 0)
  Col 3 = Walk step B  (right foot forward)
"""

from PIL import Image
import os

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------
SCRIPT_DIR  = os.path.dirname(os.path.abspath(__file__))
OUTPUT_DIR  = os.path.join(SCRIPT_DIR, "output")
os.makedirs(OUTPUT_DIR, exist_ok=True)

# ---------------------------------------------------------------------------
# Frame / sheet geometry
# ---------------------------------------------------------------------------
FRAME_W     = 32
FRAME_H     = 32
SHEET_COLS  = 4   # walk frames per direction
SHEET_ROWS  = 4   # directions

# ---------------------------------------------------------------------------
# Color palette  (sourced from reference_assets/walk_example.gif analysis)
# ---------------------------------------------------------------------------
TRANSPARENT  = (0, 0, 0, 0)
OUTLINE      = (26, 10, 13, 255)

HAIR         = (204, 116, 29, 255)
HAIR_DARK    = (159, 84, 19, 255)
HAIR_LIGHT   = (251, 163, 39, 255)

SKIN         = (255, 211, 170, 255)
SKIN_MID     = (225, 170, 145, 255)
SKIN_DARK    = (176, 136, 108, 255)

EYE          = (18, 3, 8, 255)

SHIRT        = (238, 238, 238, 255)
SHIRT_MID    = (212, 212, 212, 255)
SHIRT_DARK   = (189, 189, 189, 255)

PANTS        = (45, 149, 236, 255)
PANTS_MID    = (36, 124, 198, 255)
PANTS_DARK   = (25, 101, 165, 255)

SHOE         = (76, 76, 76, 255)
SHOE_DARK    = (50, 50, 50, 255)

# ---------------------------------------------------------------------------
# Pixel-map approach: each frame is a list of strings.
# Each character maps to a palette color via PALETTE dict.
# Character art is 16 wide × 22 tall, placed at offset (8, 8) in 32×32 frame.
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

# Character art width/height
ART_W = 16
ART_H = 22

# Placement in 32×32 frame (centered horizontally, bottom-aligned with 2px pad)
ART_OX = (FRAME_W - ART_W) // 2   # 8
ART_OY = FRAME_H - ART_H - 2      # 8

# ===========================
# DOWN-FACING  (front view)
# ===========================
DOWN_IDLE = [
    "......OOOO......",  #  0  hair top
    ".....OHHHhO.....",  #  1
    "....OHHLHHHO....",  #  2  highlight
    "...OHHHHHHHHO...",  #  3
    "..OHHHHHHHHHO...",  #  4  widest hair
    "..OHHsSSSSsHHO..",  #  5  forehead + hair sides
    "..OHsESSSSEsHO..",  #  6  eyes row
    "..OHsSSSSSSSHO..",  #  7  mid face
    "...OsSSSSSSsO...",  #  8  lower face
    "....OdsSSSdO....",  #  9  chin
    "....OsTTTTsO....",  # 10  neck/collar
    "...OuTTttTTuO...",  # 11  shirt
    "..OsTTTttTTTsO..",  # 12  shirt + arms
    "..OdTTTttTTTdO..",  # 13  shirt + arms
    "...OuTTTTTTuO...",  # 14  shirt bottom
    "....OBBBBBbO....",  # 15  pants top
    "....OBBaaBBO....",  # 16  pants
    "....ObO..ObO....",  # 17  legs spread
    "....ObO..ObO....",  # 18  legs
    "...OgGO..OgGO...",  # 19  shoes
    "...OOO....OOO...",  # 20  outline bottom
    "................",  # 21  empty
]

DOWN_WALK_A = [
    "......OOOO......",  #  0  (body bobs up 1px via offset)
    ".....OHHHhO.....",  #  1
    "....OHHLHHHO....",  #  2
    "...OHHHHHHHHO...",  #  3
    "..OHHHHHHHHHO...",  #  4
    "..OHHsSSSSsHHO..",  #  5
    "..OHsESSSSEsHO..",  #  6
    "..OHsSSSSSSSHO..",  #  7
    "...OsSSSSSSsO...",  #  8
    "....OdsSSSdO....",  #  9
    "....OsTTTTsO....",  # 10
    "...OuTTttTTuO...",  # 11
    "..OdTTTttTTTsO..",  # 12  left arm back, right arm forward
    "..OdTTTttTTTsO..",  # 13
    "...OuTTTTTTuO...",  # 14
    "....OBBBBBbO....",  # 15
    "....OBBaaBBO....",  # 16
    "...ObBO...ObO...",  # 17  left leg forward (shifted left+down)
    "...ObO....ObO...",  # 18
    "..OgGO....OgGO..",  # 19  shoes
    "..OOO......OOO..",  # 20
    "................",  # 21
]

DOWN_WALK_B = [
    "......OOOO......",  #  0
    ".....OHHHhO.....",  #  1
    "....OHHLHHHO....",  #  2
    "...OHHHHHHHHO...",  #  3
    "..OHHHHHHHHHO...",  #  4
    "..OHHsSSSSsHHO..",  #  5
    "..OHsESSSSEsHO..",  #  6
    "..OHsSSSSSSSHO..",  #  7
    "...OsSSSSSSsO...",  #  8
    "....OdsSSSdO....",  #  9
    "....OsTTTTsO....",  # 10
    "...OuTTttTTuO...",  # 11
    "..OsTTTttTTTdO..",  # 12  right arm back, left arm forward
    "..OsTTTttTTTdO..",  # 13
    "...OuTTTTTTuO...",  # 14
    "....OBBBBBbO....",  # 15
    "....OBBaaBBO....",  # 16
    "....ObO...ObO...",  # 17  right leg forward (shifted right+down)
    "....ObO...ObO...",  # 18
    "...OgGO...OgGO..",  # 19
    "...OOO....OOO...",  # 20
    "................",  # 21
]

# ===========================
# UP-FACING  (back view — no face, hair covers head)
# ===========================
UP_IDLE = [
    "......OOOO......",  #  0
    ".....OHHHhO.....",  #  1
    "....OHHLHHHO....",  #  2
    "...OHHHHHHHHO...",  #  3
    "..OHHHHHHHHHO...",  #  4
    "..OHHHHHHHHHHO..",  #  5  full hair (no face)
    "..OHhHHHHHHhHO..",  #  6  hair texture
    "..OHHhHHHHhHHO..",  #  7
    "...OhHHHHHHhO...",  #  8  lower hair
    "....OhHHHHhO....",  #  9  nape
    "....OuTTTTuO....",  # 10  collar
    "...OuTTttTTuO...",  # 11  shirt
    "..OsTTTttTTTsO..",  # 12  shirt + arms
    "..OdTTTttTTTdO..",  # 13
    "...OuTTTTTTuO...",  # 14
    "....OBBBBBbO....",  # 15
    "....OBBaaBBO....",  # 16
    "....ObO..ObO....",  # 17
    "....ObO..ObO....",  # 18
    "...OgGO..OgGO...",  # 19
    "...OOO....OOO...",  # 20
    "................",  # 21
]

UP_WALK_A = [
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
    "..OdTTTttTTTsO..",
    "..OdTTTttTTTsO..",
    "...OuTTTTTTuO...",
    "....OBBBBBbO....",
    "....OBBaaBBO....",
    "...ObBO...ObO...",
    "...ObO....ObO...",
    "..OgGO....OgGO..",
    "..OOO......OOO..",
    "................",
]

UP_WALK_B = [
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
    "..OsTTTttTTTdO..",
    "..OsTTTttTTTdO..",
    "...OuTTTTTTuO...",
    "....OBBBBBbO....",
    "....OBBaaBBO....",
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
    "......OOOOO.....",  #  0
    ".....OHHHHhO....",  #  1
    "....OHHLHHHhO...",  #  2
    "...OHHHHHHHHhO..",  #  3
    "...OHHHHHHHHhO..",  #  4
    "...OHSSSSSSHhO..",  #  5  face visible on right
    "...OESSSSSShHO..",  #  6  one eye on left edge
    "...OsSSSSSSHhO..",  #  7
    "....OSSSSSShO...",  #  8
    ".....OdsSSdO....",  #  9  chin
    ".....OsTTTsO....",  # 10
    "....OuTTtTTuO...",  # 11
    "...OsTTTtTTTuO..",  # 12  arms (front arm visible)
    "...OdTTTtTTTuO..",  # 13
    "....OuTTTTTuO...",  # 14
    ".....OBBBBbO....",  # 15
    ".....OBBaaBbO...",  # 16
    ".....ObbObbO....",  # 17
    ".....ObbObbO....",  # 18
    "....OgGOOgGO....",  # 19
    "....OOO..OOO....",  # 20
    "................",  # 21
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
    "....OSSSSSShO...",
    ".....OdsSSdO....",
    ".....OsTTTsO....",
    "....OuTTtTTuO...",
    "...OdTTTtTTTuO..",  # back arm up
    "...OdTTTtTTTuO..",
    "....OuTTTTTuO...",
    ".....OBBBBbO....",
    "....OBBaaBbO....",  # front leg forward
    "...ObbO..ObbO...",
    "..ObbO...ObbO...",  # stride
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
    "....OSSSSSShO...",
    ".....OdsSSdO....",
    ".....OsTTTsO....",
    "....OuTTtTTuO...",
    "...OsTTTtTTTdO..",  # front arm up
    "...OsTTTtTTTdO..",
    "....OuTTTTTuO...",
    ".....OBBBBbO....",
    ".....OBBaaBbO...",  # back leg forward
    ".....ObbO.ObbO..",
    ".....ObbO..ObbO.",
    ".....OGgO..OGgO.",
    ".....OOO....OOO.",
    "................",
]


def render_frame(pixel_rows):
    """Convert a list of palette-character strings into a 32×32 RGBA Image."""
    img = Image.new('RGBA', (FRAME_W, FRAME_H), TRANSPARENT)
    for row_idx, row_str in enumerate(pixel_rows):
        # Pad or trim to ART_W just in case
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
    """Build the full 4×4 sprite sheet and return it."""
    sheet_w = SHEET_COLS * FRAME_W   # 128
    sheet_h = SHEET_ROWS * FRAME_H   # 128
    sheet = Image.new('RGBA', (sheet_w, sheet_h), TRANSPARENT)

    # Row 0: Down  (idle, walkA, idle, walkB)
    down_frames = [DOWN_IDLE, DOWN_WALK_A, DOWN_IDLE, DOWN_WALK_B]
    # Row 1: Left
    left_frames = [LEFT_IDLE, LEFT_WALK_A, LEFT_IDLE, LEFT_WALK_B]
    # Row 2: Right (mirror of left)
    # Row 3: Up
    up_frames = [UP_IDLE, UP_WALK_A, UP_IDLE, UP_WALK_B]

    rows = []

    # Down
    row_down = [render_frame(f) for f in down_frames]
    rows.append(row_down)

    # Left
    row_left = [render_frame(f) for f in left_frames]
    rows.append(row_left)

    # Right (mirror of left)
    row_right = [mirror_horizontal(f) for f in row_left]
    rows.append(row_right)

    # Up
    row_up = [render_frame(f) for f in up_frames]
    rows.append(row_up)

    for row_idx, row_images in enumerate(rows):
        for col_idx, frame_img in enumerate(row_images):
            x = col_idx * FRAME_W
            y = row_idx * FRAME_H
            sheet.paste(frame_img, (x, y))

    return sheet, rows


def build_preview_gif(rows):
    """Build an animated GIF showing the walk cycle for each direction."""
    # Show down walk cycle, then left, right, up — 4 frames each at 150ms
    gif_frames = []
    scale = 4  # upscale for visibility

    for row_images in rows:
        for frame_img in row_images:
            scaled = frame_img.resize(
                (FRAME_W * scale, FRAME_H * scale),
                Image.NEAREST
            )
            # Convert to RGBA for GIF (need to handle transparency)
            gif_frames.append(scaled)

    # Save as GIF
    # PIL GIF doesn't support RGBA directly — convert with a background
    bg_color = (200, 200, 200)
    gif_rgb_frames = []
    for f in gif_frames:
        bg = Image.new('RGB', f.size, bg_color)
        bg.paste(f, mask=f.split()[3])
        gif_rgb_frames.append(bg)

    gif_path = os.path.join(OUTPUT_DIR, "generic_character_preview.gif")
    gif_rgb_frames[0].save(
        gif_path,
        save_all=True,
        append_images=gif_rgb_frames[1:],
        duration=150,
        loop=0
    )
    print(f"  Preview GIF: {gif_path}")


def main():
    print("Generating character sprite sheet...")

    sheet, rows = build_sprite_sheet()

    # Save sprite sheet
    sheet_path = os.path.join(OUTPUT_DIR, "generic_character_sheet.png")
    sheet.save(sheet_path)
    print(f"  Sprite sheet: {sheet_path}")

    # Save individual frames for inspection
    direction_names = ["down", "left", "right", "up"]
    frame_names = ["idle", "walk_a", "idle2", "walk_b"]
    frames_dir = os.path.join(OUTPUT_DIR, "frames")
    os.makedirs(frames_dir, exist_ok=True)

    for row_idx, row_images in enumerate(rows):
        for col_idx, frame_img in enumerate(row_images):
            fname = f"{direction_names[row_idx]}_{frame_names[col_idx]}.png"
            frame_img.save(os.path.join(frames_dir, fname))

    # Also save 4x upscaled individual frames
    scaled_dir = os.path.join(OUTPUT_DIR, "frames_4x")
    os.makedirs(scaled_dir, exist_ok=True)
    for row_idx, row_images in enumerate(rows):
        for col_idx, frame_img in enumerate(row_images):
            fname = f"{direction_names[row_idx]}_{frame_names[col_idx]}_4x.png"
            scaled = frame_img.resize(
                (FRAME_W * 4, FRAME_H * 4),
                Image.NEAREST
            )
            scaled.save(os.path.join(scaled_dir, fname))

    # Generate preview GIF
    build_preview_gif(rows)

    print("Done!")


if __name__ == "__main__":
    main()
