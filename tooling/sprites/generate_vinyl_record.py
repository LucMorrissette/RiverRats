"""Generate 16x16 vinyl record sprites with split two-tone labels."""

from pathlib import Path
from PIL import Image

SIZE = 16
CENTER = (SIZE - 1) * 0.5
OUT_DIR = Path(__file__).resolve().parents[2] / "src" / "DogDays.Game" / "Content" / "Sprites"

# Radii (in pixels from center).
OUTER_RADIUS = 7.5        # Disc outer edge — fits perfectly in 16x16.
LABEL_RADIUS = 3.2        # Two-tone label radius.
GROOVE_INNER = LABEL_RADIUS
SPINDLE_RADIUS = 0.9      # Tiny center hole.

LABEL_VARIANTS = (
    ("vinyl-record", (192, 36, 32), (224, 194, 62)),
    ("vinyl-record-blue-orange", (48, 112, 206), (232, 126, 58)),
    ("vinyl-record-green-cream", (52, 150, 86), (232, 218, 176)),
    ("vinyl-record-magenta-cyan", (198, 62, 146), (84, 214, 220)),
    ("vinyl-record-indigo-mint", (78, 88, 194), (152, 226, 184)),
    ("vinyl-record-orange-teal", (214, 108, 38), (58, 180, 168)),
)


def lerp_channel(start: int, end: int, t: float) -> int:
    return int(round(start + ((end - start) * t)))


def shade_label(base_color: tuple[int, int, int], t: float) -> tuple[int, int, int]:
    inner = tuple(min(255, int(channel + ((255 - channel) * 0.06))) for channel in base_color)
    outer = tuple(max(0, int(channel * 0.86)) for channel in base_color)
    return tuple(lerp_channel(inner[i], outer[i], t) for i in range(3))


def create_record_image(
    top_label_color: tuple[int, int, int],
    bottom_label_color: tuple[int, int, int]) -> Image.Image:
    image = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    px = image.load()

    for y in range(SIZE):
        for x in range(SIZE):
            dx = x - CENTER
            dy = y - CENTER
            dist = (dx * dx + dy * dy) ** 0.5

            # Outside the disc — transparent.
            if dist > OUTER_RADIUS:
                continue

            # Spindle hole.
            if dist <= SPINDLE_RADIUS:
                px[x, y] = (20, 20, 20, 255)
                continue

            # Two-tone label area.
            if dist <= LABEL_RADIUS:
                t = dist / LABEL_RADIUS
                label_color = top_label_color if dy < 0.0 else bottom_label_color
                px[x, y] = (*shade_label(label_color, t), 255)
                continue

            # Vinyl groove area — dark with subtle concentric-ring shading.
            # Simulate groove ridges with a simple sine-ish pattern.
            ring = ((dist * 2.5) % 1.0)
            shade = 28 + int(ring * 14)
            # Slight blue-ish tint common in vinyl.
            r = shade
            g = shade
            b = shade + 4
            px[x, y] = (r, g, min(255, b), 255)

    return image


def main() -> None:
    OUT_DIR.mkdir(parents=True, exist_ok=True)

    for file_stem, top_label_color, bottom_label_color in LABEL_VARIANTS:
        out_path = OUT_DIR / f"{file_stem}.png"
        create_record_image(top_label_color, bottom_label_color).save(out_path)
        print(f"Wrote {out_path}  ({SIZE}x{SIZE})")


if __name__ == "__main__":
    main()
