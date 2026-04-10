"""Generate a 32x32 wooden hockey stick sprite — pixel-by-pixel."""

from pathlib import Path
from PIL import Image

SIZE = 32
OUTPUT_DIR = Path(__file__).resolve().parent / "output"
CONTENT_PATH = (
    Path(__file__).resolve().parents[2]
    / "src" / "DogDays.Game" / "Content" / "Sprites" / "hockey-stick.png"
)

# Wood palette (darkest → lightest).
OUTLINE  = (6, 6, 8, 255)
WOOD_D   = (94, 60, 35, 255)      # dark shadow / underside
WOOD_M   = (138, 92, 52, 255)     # mid-tone body
WOOD_L   = (178, 130, 76, 255)    # light face
WOOD_H   = (198, 152, 100, 255)   # highlight grain streak


def _build_pixels():
    """Return (x, y, color) for every opaque pixel — no draw primitives."""
    px = []
    D, M, L, H = WOOD_D, WOOD_M, WOOD_L, WOOD_H

    # ── Handle knob (rows 1–2) ─────────────────────────────────
    px += [(28, 1, D), (29, 1, D)]
    px += [(28, 2, L), (29, 2, M)]

    # ── Shaft: 2 px-wide diagonal with a softer lower bend ────
    # Left pixel = lit side, right pixel = shadow side.
    grain = [(H, M), (L, M), (M, D)]
    for i in range(16):
        y = 3 + i
        x = 27 - i
        hi, lo = grain[i % 3]
        px += [(x, y, hi), (x + 1, y, lo)]

    # Lower shaft rows start easing into the heel instead of staying rigidly 45°.
    px += [(11, 19, L), (12, 19, M)]
    px += [(10, 20, H), (11, 20, M)]
    px += [(10, 21, L), (11, 21, M)]
    px += [(9, 22, L), (10, 22, D)]

    # ── Heel and blade: longer face with smoother heel ────────
    px += [(8, 23, L), (9, 23, M), (10, 23, D)]
    px += [(6, 24, H), (7, 24, H), (8, 24, L), (9, 24, M), (10, 24, D)]
    px += [
        (1, 25, H), (2, 25, H), (3, 25, H), (4, 25, L),
        (5, 25, L),
        (6, 25, M), (7, 25, M), (8, 25, D), (9, 25, D),
    ]
    px += [(0, 26, H), (1, 26, L), (2, 26, L), (3, 26, M), (4, 26, M), (5, 26, M), (6, 26, D), (7, 26, D), (8, 26, D)]

    return px


def _add_outline(image, color):
    """Add a 1 px dark outline around all opaque pixels."""
    src = image.load()
    out = image.copy()
    dst = out.load()
    for y in range(SIZE):
        for x in range(SIZE):
            if src[x, y][3] != 0:
                continue
            for ny in range(max(0, y - 1), min(SIZE, y + 2)):
                for nx in range(max(0, x - 1), min(SIZE, x + 2)):
                    if (nx, ny) != (x, y) and src[nx, ny][3] != 0:
                        dst[x, y] = color
                        break
                else:
                    continue
                break
    return out


def main() -> None:
    image = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    for x, y, color in _build_pixels():
        image.putpixel((x, y), color)

    image = _add_outline(image, OUTLINE)

    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    image.save(OUTPUT_DIR / "hockey-stick.png")
    print(f"Wrote {OUTPUT_DIR / 'hockey-stick.png'}")

    CONTENT_PATH.parent.mkdir(parents=True, exist_ok=True)
    image.save(CONTENT_PATH)
    print(f"Wrote {CONTENT_PATH}")


if __name__ == "__main__":
    main()