"""Generate a small glowing energy orb sprite for pickups."""

from pathlib import Path
from PIL import Image

SIZE = 24
CENTER = (SIZE - 1) * 0.5
OUT_PATH = Path(__file__).resolve().parents[2] / "src" / "RiverRats.Game" / "Content" / "Sprites" / "energy-orb.png"


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

            # Soft aura ring.
            aura = clamp01(1.0 - dist / 11.5)
            aura_alpha = int((aura ** 2.8) * 95)

            # Bright inner core.
            core = clamp01(1.0 - dist / 5.2)
            core_alpha = int((core ** 1.2) * 220)

            # Tiny hot center.
            hot = clamp01(1.0 - dist / 2.4)
            hot_alpha = int((hot ** 0.9) * 255)

            r = int(30 + aura * 40 + core * 95 + hot * 60)
            g = int(110 + aura * 70 + core * 95 + hot * 50)
            b = int(190 + aura * 55 + core * 35 + hot * 20)

            alpha = max(aura_alpha, core_alpha, hot_alpha)
            if alpha == 0:
                continue

            px[x, y] = (min(255, r), min(255, g), min(255, b), alpha)

    OUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    image.save(OUT_PATH)
    print(f"Wrote {OUT_PATH}")


if __name__ == "__main__":
    main()
