"""Generate a small topwater frog lure sprite (8x6 px)."""

from pathlib import Path
from PIL import Image

# Frog lure at game resolution — 8 wide x 6 tall.
WIDTH = 8
HEIGHT = 6
OUT_PATH = (
    Path(__file__).resolve().parents[2]
    / "src"
    / "DogDays.Game"
    / "Content"
    / "Sprites"
    / "frog_lure.png"
)

# Palette
DARK_GREEN = (30, 90, 20, 255)
MID_GREEN = (60, 150, 40, 255)
LIGHT_GREEN = (100, 190, 70, 255)
BELLY = (180, 190, 80, 255)      # yellowish underbelly highlight
EYE = (220, 200, 40, 255)        # bright yellow eye
PUPIL = (20, 20, 20, 255)
LEG = (40, 110, 30, 255)
TRANSPARENT = (0, 0, 0, 0)


def main() -> None:
    img = Image.new("RGBA", (WIDTH, HEIGHT), TRANSPARENT)
    px = img.load()

    # Row 0 (top): head bump / eyes
    #   ..oOOo..   (o=mid green, O=eye spots)
    px[2, 0] = MID_GREEN
    px[3, 0] = EYE
    px[4, 0] = EYE
    px[5, 0] = MID_GREEN

    # Row 1: upper body
    #  .oGGGGo.
    px[1, 1] = MID_GREEN
    px[2, 1] = LIGHT_GREEN
    px[3, 1] = LIGHT_GREEN
    px[4, 1] = LIGHT_GREEN
    px[5, 1] = LIGHT_GREEN
    px[6, 1] = MID_GREEN

    # Row 2: widest body + front legs
    # lGGGGGGl
    px[0, 2] = LEG
    px[1, 2] = MID_GREEN
    px[2, 2] = LIGHT_GREEN
    px[3, 2] = LIGHT_GREEN
    px[4, 2] = LIGHT_GREEN
    px[5, 2] = LIGHT_GREEN
    px[6, 2] = MID_GREEN
    px[7, 2] = LEG

    # Row 3: belly
    # .GBBBBG.
    px[1, 3] = MID_GREEN
    px[2, 3] = BELLY
    px[3, 3] = BELLY
    px[4, 3] = BELLY
    px[5, 3] = BELLY
    px[6, 3] = MID_GREEN

    # Row 4: lower body + hind legs
    # l.GGGG.l
    px[0, 4] = LEG
    px[2, 4] = DARK_GREEN
    px[3, 4] = MID_GREEN
    px[4, 4] = MID_GREEN
    px[5, 4] = DARK_GREEN
    px[7, 4] = LEG

    # Row 5 (bottom): hind feet
    # l..GG..l
    px[0, 5] = LEG
    px[3, 5] = DARK_GREEN
    px[4, 5] = DARK_GREEN
    px[7, 5] = LEG

    img.save(OUT_PATH)
    print(f"Saved frog lure sprite to {OUT_PATH} ({WIDTH}x{HEIGHT})")


if __name__ == "__main__":
    main()
