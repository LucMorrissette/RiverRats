from __future__ import annotations

import pathlib
import random

try:
    from PIL import Image
except ImportError as exc:
    raise SystemExit("Pillow is required for cabin floor generation. Install it in the repo virtual environment before running this script.") from exc


TILE_SIZE = 32
WOOD_OUTPUT_NAMES = [
    "cabin-wood-floor-1.png",
    "cabin-wood-floor-2.png",
    "cabin-wood-floor-3.png",
    "cabin-wood-floor-4.png",
]
CHECKER_OUTPUTS = [
    ("cabin-checker-floor-light-1.png", True, 1),
    ("cabin-checker-floor-dark-1.png", False, 2),
    ("cabin-checker-floor-light-2.png", True, 3),
    ("cabin-checker-floor-dark-2.png", False, 4),
]
BASE_DIR = pathlib.Path(__file__).resolve().parents[2]
OUTPUT_DIR = BASE_DIR / "src" / "RiverRats.Game" / "Content" / "Tilesets"


def clamp(value: int) -> int:
    return max(0, min(255, value))


def make_wood_tile(seed: int) -> Image.Image:
    rng = random.Random(seed)
    image = Image.new("RGBA", (TILE_SIZE, TILE_SIZE))
    pixels = image.load()
    plank_widths = [8, 9, 7, 8]
    plank_offsets = [rng.randint(-3, 3) for _ in plank_widths]
    seam_rows_by_plank = []

    plank_edges = []
    cursor = 0
    for plank_index, width in enumerate(plank_widths):
        plank_edges.append((cursor, min(TILE_SIZE, cursor + width)))
        seam_count = 1 + ((seed + plank_index) % 2)
        candidate_rows = list(range(7 + ((seed + plank_index) % 3), TILE_SIZE - 6, 8))
        rng.shuffle(candidate_rows)
        seam_rows = sorted(candidate_rows[:seam_count])
        seam_rows_by_plank.append(seam_rows)
        cursor += width

    for y in range(TILE_SIZE):
        for x in range(TILE_SIZE):
            plank_index = 0
            for index, (left, right) in enumerate(plank_edges):
                if left <= x < right:
                    plank_index = index
                    break

            base_r = 154 + plank_offsets[plank_index] + plank_index
            base_g = 124 + (plank_offsets[plank_index] // 2)
            base_b = 79 + (plank_index // 3)

            grain = ((y * (6 + plank_index)) + x + (seed * 9)) % 13
            grain_adjust = grain - 7

            r = base_r + grain_adjust
            g = base_g + (grain_adjust // 3)
            b = base_b + (grain_adjust // 5)

            if any(abs(x - edge[0]) <= 0 or abs(x - edge[1] + 1) <= 0 for edge in plank_edges[1:]):
                r -= 10
                g -= 8
                b -= 6

            for seam_y in seam_rows_by_plank[plank_index]:
                if abs(y - seam_y) <= 0:
                    r -= 14
                    g -= 11
                    b -= 8
                elif abs(y - seam_y) == 1:
                    r -= 5
                    g -= 4
                    b -= 3

            if (y + (seed * 3) + (plank_index * 5)) % 9 in (0, 1):
                r += 3
                g += 2
                b += 1

            if y in (0, TILE_SIZE - 1):
                r -= 4
                g -= 3
                b -= 2

            pixels[x, y] = (clamp(r), clamp(g), clamp(b), 255)

    return image


def make_checkered_tile(seed: int, is_light: bool) -> Image.Image:
    rng = random.Random((200 if is_light else 400) + seed)
    image = Image.new("RGBA", (TILE_SIZE, TILE_SIZE))
    pixels = image.load()

    if is_light:
        base_r, base_g, base_b = 205, 194, 164
        seam_tint = (-14, -13, -12)
        wear_tint = (-10, -8, -4)
        highlight_tint = (8, 7, 4)
    else:
        base_r, base_g, base_b = 89, 96, 68
        seam_tint = (-10, -10, -8)
        wear_tint = (-12, -11, -8)
        highlight_tint = (6, 7, 5)

    wear_centers = [
        (6 + ((seed * 3) % 6), 7 + ((seed * 5) % 5)),
        (22 + (seed % 4), 20 + ((seed * 7) % 4)),
    ]

    for y in range(TILE_SIZE):
        for x in range(TILE_SIZE):
            edge_distance = min(x, y, TILE_SIZE - 1 - x, TILE_SIZE - 1 - y)
            grain = ((x * 5) + (y * 3) + (seed * 9)) % 9 - 4
            marble = ((x * x) + (y * 7) + (seed * 13)) % 11 - 5

            r = base_r + grain + (marble // 2)
            g = base_g + (grain // 2) + (marble // 3)
            b = base_b + (grain // 3)

            if edge_distance == 0:
                r += seam_tint[0]
                g += seam_tint[1]
                b += seam_tint[2]
            elif edge_distance == 1:
                r += seam_tint[0] // 2
                g += seam_tint[1] // 2
                b += seam_tint[2] // 2
            elif x in (2, 3) and y in (2, 3):
                r += highlight_tint[0]
                g += highlight_tint[1]
                b += highlight_tint[2]

            for center_x, center_y in wear_centers:
                distance = abs(x - center_x) + abs(y - center_y)
                if distance <= 3:
                    r += wear_tint[0] + distance
                    g += wear_tint[1] + distance
                    b += wear_tint[2] + distance

            if (x + y + seed) % 13 == 0:
                r -= 4
                g -= 3
                b -= 2

            if (x * 2 + y + seed) % 17 == 0:
                r += 4
                g += 3
                b += 2

            if rng.random() < 0.035:
                r += rng.randint(-6, 6)
                g += rng.randint(-5, 5)
                b += rng.randint(-4, 4)

            pixels[x, y] = (clamp(r), clamp(g), clamp(b), 255)

    return image


def save_tile(path: pathlib.Path, image: Image.Image) -> None:
    image.save(path, format="PNG")


def main() -> None:
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    for seed, file_name in enumerate(WOOD_OUTPUT_NAMES, start=1):
        save_tile(OUTPUT_DIR / file_name, make_wood_tile(seed))

    for file_name, is_light, seed in CHECKER_OUTPUTS:
        save_tile(OUTPUT_DIR / file_name, make_checkered_tile(seed, is_light))


if __name__ == "__main__":
    main()