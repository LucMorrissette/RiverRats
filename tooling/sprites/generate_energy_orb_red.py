"""Generate a red glowing energy orb sprite variant for pickups."""

from pathlib import Path
from PIL import Image

SIZE = 24
CENTER = (SIZE - 1) * 0.5
OUT_PATH = Path(__file__).resolve().parents[2] / "src" / "DogDays.Game" / "Content" / "Sprites" / "energy-orb-red.png"


def clamp01(value: float) -> float:
    if value < 0.0:
        return 0.0
    if value > 1.0:
        return 1.0
    return value


def main() -> None:
    image = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    px = image.load()

    for y in range(SIZE):
        for x in range(SIZE):
            dx = x - CENTER
            dy = y - CENTER
            dist = (dx * dx + dy * dy) ** 0.5

            aura = clamp01(1.0 - dist / 11.5)
            aura_alpha = int((aura ** 2.8) * 95)

            core = clamp01(1.0 - dist / 5.2)
            core_alpha = int((core ** 1.2) * 220)

            hot = clamp01(1.0 - dist / 2.4)
            hot_alpha = int((hot ** 0.9) * 255)

            r = int(150 + aura * 80 + core * 85 + hot * 40)
            g = int(30 + aura * 35 + core * 40 + hot * 20)
            b = int(35 + aura * 20 + core * 25 + hot * 15)

            alpha = max(aura_alpha, core_alpha, hot_alpha)
            if alpha == 0:
                continue

            px[x, y] = (min(255, r), min(255, g), min(255, b), alpha)

    OUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    image.save(OUT_PATH)
    print(f"Wrote {OUT_PATH}")


if __name__ == "__main__":
    main()
