"""
Generate a companion character sprite sheet from the generic base character.

Style goals:
- Similar silhouette to the main character.
- Red shirt.
- Red baseball cap.

Output:
  - tooling/sprites/output/companion_character_sheet.png
  - src/DogDays.Game/Content/Sprites/companion_character_sheet.png
"""

from PIL import Image
import os


SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
REPO_ROOT = os.path.abspath(os.path.join(SCRIPT_DIR, "..", ".."))

BASE_SHEET_PATH = os.path.join(
    REPO_ROOT,
    "src",
    "DogDays.Game",
    "Content",
    "Sprites",
    "generic_character_sheet.png",
)

OUTPUT_DIR = os.path.join(SCRIPT_DIR, "output")
os.makedirs(OUTPUT_DIR, exist_ok=True)

OUTPUT_SHEET_PATH = os.path.join(OUTPUT_DIR, "companion_character_sheet.png")
CONTENT_SHEET_PATH = os.path.join(
    REPO_ROOT,
    "src",
    "DogDays.Game",
    "Content",
    "Sprites",
    "companion_character_sheet.png",
)

FRAME_W = 32
FRAME_H = 32
ART_OX = 8
ART_OY = 8

# Base palette values from generate_character.py.
HAIR = (204, 116, 29, 255)
HAIR_DARK = (159, 84, 19, 255)
HAIR_LIGHT = (251, 163, 39, 255)
SHIRT = (238, 238, 238, 255)
SHIRT_MID = (212, 212, 212, 255)
SHIRT_DARK = (189, 189, 189, 255)
OUTLINE = (26, 10, 13, 255)

# Companion palette.
CAP = (186, 34, 34, 255)
CAP_DARK = (132, 20, 20, 255)
CAP_LIGHT = (232, 76, 76, 255)
SHIRT_RED = (213, 48, 48, 255)
SHIRT_RED_MID = (170, 33, 33, 255)
SHIRT_RED_DARK = (129, 21, 21, 255)


def recolor(sheet):
    """Swap hair and shirt colors for companion styling."""
    pixels = sheet.load()
    for y in range(sheet.height):
        for x in range(sheet.width):
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


def draw_horizontal_line(pixels, frame_x, frame_y, x0, x1, y, color):
    for x in range(x0, x1 + 1):
        pixels[frame_x + x, frame_y + y] = color


def draw_cap_details(sheet):
    """Add baseball-cap brim cues per facing row on every animation frame."""
    pixels = sheet.load()

    for col in range(4):
        frame_x = col * FRAME_W

        # Row 0: Down-facing brim across forehead.
        frame_y_down = 0
        draw_horizontal_line(pixels, frame_x, frame_y_down, ART_OX + 3, ART_OX + 12, ART_OY + 5, OUTLINE)
        draw_horizontal_line(pixels, frame_x, frame_y_down, ART_OX + 4, ART_OX + 11, ART_OY + 6, CAP_DARK)

        # Row 1: Left-facing brim protrudes to the left.
        frame_y_left = FRAME_H
        draw_horizontal_line(pixels, frame_x, frame_y_left, ART_OX + 1, ART_OX + 5, ART_OY + 6, OUTLINE)
        draw_horizontal_line(pixels, frame_x, frame_y_left, ART_OX + 2, ART_OX + 5, ART_OY + 7, CAP_DARK)

        # Row 2: Right-facing brim protrudes to the right.
        frame_y_right = FRAME_H * 2
        draw_horizontal_line(pixels, frame_x, frame_y_right, ART_OX + 10, ART_OX + 14, ART_OY + 6, OUTLINE)
        draw_horizontal_line(pixels, frame_x, frame_y_right, ART_OX + 10, ART_OX + 13, ART_OY + 7, CAP_DARK)

        # Row 3: Up-facing cap back strap.
        frame_y_up = FRAME_H * 3
        draw_horizontal_line(pixels, frame_x, frame_y_up, ART_OX + 6, ART_OX + 9, ART_OY + 6, CAP_LIGHT)


def main():
    if not os.path.exists(BASE_SHEET_PATH):
        raise FileNotFoundError(f"Base sheet not found: {BASE_SHEET_PATH}")

    sheet = Image.open(BASE_SHEET_PATH).convert("RGBA")
    recolor(sheet)
    draw_cap_details(sheet)

    sheet.save(OUTPUT_SHEET_PATH)
    sheet.save(CONTENT_SHEET_PATH)

    print(f"Generated: {OUTPUT_SHEET_PATH}")
    print(f"Copied to: {CONTENT_SHEET_PATH}")


if __name__ == "__main__":
    main()
