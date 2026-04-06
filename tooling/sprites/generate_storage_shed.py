from __future__ import annotations

import pathlib
import random

try:
    from PIL import Image
    from PIL import ImageDraw
except ImportError as exc:
    raise SystemExit("Pillow is required for storage shed generation. Install it in the repo virtual environment before running this script.") from exc


TILE_SIZE = 32
CANVAS_SIZE = TILE_SIZE * 2
BASE_DIR = pathlib.Path(__file__).resolve().parents[2]
OUTPUT_PATH = BASE_DIR / "src" / "RiverRats.Game" / "Content" / "Sprites" / "storage-shed.png"

OUTLINE = (34, 23, 18, 255)
ROOF_DARK = (70, 57, 54, 255)
ROOF_MID = (95, 78, 72, 255)
ROOF_LIGHT = (123, 104, 94, 255)
WOOD_DARK = (101, 71, 46, 255)
WOOD_MID = (136, 96, 61, 255)
WOOD_LIGHT = (165, 121, 78, 255)
TRIM = (190, 158, 116, 255)
TRIM_SHADOW = (129, 96, 65, 255)
STONE_LIGHT = (141, 135, 128, 255)
STONE_DARK = (92, 87, 82, 255)
GLASS = (170, 204, 214, 255)
GLASS_SHADOW = (96, 127, 137, 255)
MOSS = (95, 113, 61, 255)
SHADOW = (0, 0, 0, 70)


def clamp(value: int) -> int:
    return max(0, min(255, value))


def shade(color: tuple[int, int, int, int], delta_r: int, delta_g: int, delta_b: int) -> tuple[int, int, int, int]:
    return (
        clamp(color[0] + delta_r),
        clamp(color[1] + delta_g),
        clamp(color[2] + delta_b),
        color[3],
    )


def put_pixel(image: Image.Image, x: int, y: int, color: tuple[int, int, int, int]) -> None:
    if 0 <= x < CANVAS_SIZE and 0 <= y < CANVAS_SIZE:
        image.putpixel((x, y), color)


def draw_pixel_line(image: Image.Image, start_x: int, end_x: int, y: int, color: tuple[int, int, int, int]) -> None:
    for x in range(start_x, end_x + 1):
        put_pixel(image, x, y, color)


def draw_shadow(image: Image.Image) -> None:
    draw = ImageDraw.Draw(image, "RGBA")
    draw.ellipse((11, 54, 58, 63), fill=SHADOW)


def draw_front_wall(image: Image.Image, rng: random.Random) -> None:
    draw = ImageDraw.Draw(image)
    draw.rectangle((12, 25, 44, 54), fill=WOOD_MID, outline=OUTLINE)
    draw.polygon(((44, 25), (53, 29), (53, 54), (44, 54)), fill=shade(WOOD_DARK, -2, -1, -1), outline=OUTLINE)

    for y in range(28, 54, 4):
        tone_shift = ((y // 4) % 3) - 1
        board_color = shade(WOOD_MID, tone_shift * 4, tone_shift * 3, tone_shift * 2)
        draw.rectangle((13, y, 43, min(y + 2, 53)), fill=board_color)
        draw_pixel_line(image, 45, 52, y + 1, shade(WOOD_DARK, tone_shift * 3, tone_shift * 2, tone_shift * 2))

    for x in (16, 23, 30, 37):
        draw.line((x, 26, x, 53), fill=shade(WOOD_DARK, -8, -6, -4))

    draw.line((44, 26, 44, 53), fill=TRIM_SHADOW)
    draw.line((46, 30, 46, 53), fill=shade(WOOD_DARK, -14, -10, -8))
    draw.line((49, 31, 49, 52), fill=shade(WOOD_LIGHT, -30, -20, -14))
    draw.line((52, 33, 52, 53), fill=shade(WOOD_DARK, -12, -9, -7))

    for offset in range(0, 8, 2):
        put_pixel(image, 47 + (offset // 2), 33 + offset, shade(TRIM, -28, -24, -18))
        put_pixel(image, 50 - (offset // 2), 33 + offset, shade(TRIM, -34, -28, -20))

    for _ in range(24):
        x = rng.randint(14, 42)
        y = rng.randint(28, 52)
        if (26 <= x <= 37) and (34 <= y <= 53):
            continue

        if rng.random() < 0.5:
            put_pixel(image, x, y, shade(WOOD_LIGHT, rng.randint(-8, 10), rng.randint(-8, 10), rng.randint(-6, 8)))
        else:
            put_pixel(image, x, y, shade(WOOD_DARK, rng.randint(-5, 8), rng.randint(-5, 8), rng.randint(-4, 6)))

    draw.line((13, 54, 43, 54), fill=shade(TRIM_SHADOW, -10, -8, -6))
    draw.line((45, 54, 53, 54), fill=shade(TRIM_SHADOW, -14, -12, -10))

    for y in range(35, 53, 6):
        put_pixel(image, 28, y, shade(WOOD_DARK, -18, -14, -10))
        put_pixel(image, 34, y + 1, shade(WOOD_DARK, -18, -14, -10))


def draw_roof(image: Image.Image, rng: random.Random) -> None:
    draw = ImageDraw.Draw(image)
    draw.polygon(((7, 25), (31, 10), (49, 20), (53, 29), (44, 29), (11, 29)), fill=ROOF_MID, outline=OUTLINE)
    draw.polygon(((44, 20), (55, 24), (53, 29), (44, 29)), fill=ROOF_DARK, outline=OUTLINE)
    draw.line((11, 29, 44, 29), fill=shade(ROOF_DARK, -10, -8, -6))
    draw.line((31, 10, 44, 20), fill=ROOF_LIGHT)
    draw.line((30, 11, 13, 23), fill=shade(ROOF_LIGHT, -6, -4, -4))
    draw.line((10, 26, 30, 13), fill=shade(ROOF_DARK, -18, -14, -12))

    for y in range(14, 29, 3):
        left = max(9, 8 + ((29 - y) // 2))
        right = min(47, 47 - ((29 - y) // 3))
        shingle_color = shade(ROOF_MID, ((y // 3) % 3) * 3 - 3, ((y // 3) % 3) * 2 - 2, ((y // 3) % 3) * 2 - 2)
        draw_pixel_line(image, left, right, y, shingle_color)
        for x in range(left + 1, right, 5):
            put_pixel(image, x, y + 1, shade(ROOF_DARK, -4, -3, -3))

    for x in range(14, 46, 3):
        if rng.random() < 0.18:
            put_pixel(image, x, 27, MOSS)
            put_pixel(image, x + 1, 27, shade(MOSS, 10, 12, 5))

    draw.rectangle((28, 17, 34, 21), fill=shade(TRIM_SHADOW, -10, -8, -6), outline=OUTLINE)
    draw.rectangle((29, 18, 33, 20), fill=shade(TRIM, -18, -14, -10))
    put_pixel(image, 30, 19, shade(TRIM, 18, 14, 10))
    put_pixel(image, 32, 19, shade(TRIM, 18, 14, 10))


def draw_trim_and_window(image: Image.Image) -> None:
    draw = ImageDraw.Draw(image)

    draw.line((12, 25, 12, 54), fill=TRIM_SHADOW)
    draw.line((44, 25, 44, 54), fill=TRIM_SHADOW)
    draw.line((13, 25, 43, 25), fill=TRIM)
    draw.line((45, 30, 52, 30), fill=shade(TRIM_SHADOW, -10, -8, -6))

    draw.rectangle((16, 33, 25, 41), fill=GLASS, outline=OUTLINE)
    draw.rectangle((17, 34, 24, 40), fill=GLASS)
    draw.line((20, 34, 20, 40), fill=GLASS_SHADOW)
    draw.line((17, 37, 24, 37), fill=GLASS_SHADOW)
    draw.line((16, 32, 25, 32), fill=TRIM)
    draw.line((15, 33, 15, 41), fill=TRIM_SHADOW)
    draw.line((26, 33, 26, 41), fill=TRIM_SHADOW)
    draw.line((16, 42, 25, 42), fill=shade(TRIM_SHADOW, -14, -12, -8))

    draw.rectangle((48, 34, 51, 39), fill=shade(GLASS_SHADOW, -6, -2, 0), outline=OUTLINE)
    put_pixel(image, 49, 35, GLASS)
    put_pixel(image, 50, 35, GLASS)
    put_pixel(image, 49, 37, shade(GLASS, -25, -25, -20))
    put_pixel(image, 50, 38, shade(GLASS, -35, -30, -24))

    draw.rectangle((35, 34, 39, 38), fill=shade(TRIM, -20, -16, -10), outline=OUTLINE)
    draw.line((36, 36, 38, 36), fill=shade(TRIM_SHADOW, -20, -16, -10))


def draw_foundation(image: Image.Image, rng: random.Random) -> None:
    draw = ImageDraw.Draw(image)
    stones = [
        (13, 55, 19, 58),
        (20, 55, 27, 59),
        (28, 55, 34, 58),
        (35, 55, 42, 59),
        (43, 55, 51, 58),
    ]

    for left, top, right, bottom in stones:
        base_color = STONE_LIGHT if ((left // 2) % 2 == 0) else shade(STONE_LIGHT, -12, -10, -8)
        draw.rectangle((left, top, right, bottom), fill=base_color, outline=OUTLINE)
        for x in range(left + 1, right):
            if rng.random() < 0.2:
                put_pixel(image, x, top + 1, shade(STONE_DARK, rng.randint(-3, 6), rng.randint(-3, 6), rng.randint(-3, 6)))

    draw.line((12, 55, 52, 55), fill=shade(STONE_DARK, -10, -10, -8))
    put_pixel(image, 15, 59, MOSS)
    put_pixel(image, 16, 59, shade(MOSS, 8, 12, 5))
    put_pixel(image, 44, 59, MOSS)


def add_weathering(image: Image.Image, rng: random.Random) -> None:
    for _ in range(30):
        x = rng.randint(13, 52)
        y = rng.randint(24, 53)
        current = image.getpixel((x, y))
        if current[3] == 0 or current == OUTLINE:
            continue

        modifier = rng.randint(-10, 10)
        image.putpixel(
            (x, y),
            (
                clamp(current[0] + modifier),
                clamp(current[1] + modifier // 2),
                clamp(current[2] + modifier // 3),
                current[3],
            ),
        )

    for x in range(27, 37):
        if x % 2 == 0:
            put_pixel(image, x, 31, shade(WOOD_LIGHT, 6, 5, 4))

    for x in range(46, 53):
        put_pixel(image, x, 53, shade(WOOD_DARK, -16, -12, -10))

    for y in range(26, 53, 7):
        put_pixel(image, 42, y, shade(TRIM_SHADOW, -16, -14, -10))
        put_pixel(image, 43, y + 1, shade(TRIM, -28, -22, -16))


def main() -> None:
    rng = random.Random(6412)
    OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)

    image = Image.new("RGBA", (CANVAS_SIZE, CANVAS_SIZE), (0, 0, 0, 0))
    draw_shadow(image)
    draw_front_wall(image, rng)
    draw_roof(image, rng)
    draw_trim_and_window(image)
    draw_foundation(image, rng)
    add_weathering(image, rng)

    image.save(OUTPUT_PATH, format="PNG")
    print(f"Saved storage shed to {OUTPUT_PATH}")


if __name__ == "__main__":
    main()