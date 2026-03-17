"""
Generate a 32×32 horizontally-flowing water tile.

The tile is tiled-friendly: band spacing divides 32 and the horizontal phase
uses a full-period sine so left/right edges match. Output is saved to
`output/tiles/water-horizontal.png` and copied into the game's
`Content/Tilesets` folder.

Usage: run this script from the tooling/sprites folder or via the repo root.
"""
from PIL import Image
import os
import shutil
import math

TILE_SIZE = 32
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
OUTPUT_DIR = os.path.join(SCRIPT_DIR, "output", "tiles")
CONTENT_DIR = os.path.join(SCRIPT_DIR, "..", "..", "src", "RiverRats.Game",
                           "Content", "Tilesets")
os.makedirs(OUTPUT_DIR, exist_ok=True)

# Palette (RGBA)
BASE = (88, 166, 220, 255)       # deep water
BAND = (113, 192, 242, 255)      # lighter flowing band
HIGHLIGHT = (169, 227, 255, 255) # bright highlight for band centers
FOAM = (230, 245, 255, 255)

# Band layout (divides TILE_SIZE so it tiles vertically)
BAND_SPACING = 8   # pixels between band centers (32 % 8 == 0)
BAND_THICKNESS = 3 # how wide the band effect spreads (px)
AMPLITUDE = 2      # how much bands shift vertically based on X
PHASES = 1         # number of sine periods across the tile horizontally


def lerp(a, b, t):
    return tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(4))


def make_tile():
    img = Image.new("RGBA", (TILE_SIZE, TILE_SIZE), BASE)
    px = img.load()

    for x in range(TILE_SIZE):
        # horizontal phase that wraps exactly across the tile
        phase = math.sin(2.0 * math.pi * (x / TILE_SIZE) * PHASES)
        y_offset = AMPLITUDE * phase

        for y in range(TILE_SIZE):
            # shifted vertical coordinate used to place bands
            y_shifted = y + y_offset

            # find distance to nearest band center (bands at 0, BAND_SPACING, 2*BAND_SPACING...)
            band_index = round(y_shifted / BAND_SPACING)
            band_center = band_index * BAND_SPACING
            dist = abs(y_shifted - band_center)

            if dist <= BAND_THICKNESS:
                # stronger effect near band center
                t = max(0.0, 1.0 - (dist / BAND_THICKNESS))
                # blend base -> band -> highlight by using t^0.7 for a softer peak
                t_peak = t ** 0.7
                color = lerp(BASE, BAND, t_peak * 0.9)
                # small additional highlight at the very center
                if dist < 0.6:
                    color = lerp(color, HIGHLIGHT, 0.8)
            else:
                # slight vertical banding shading to add depth
                shade = 0.02 * math.cos(2.0 * math.pi * (y / TILE_SIZE))
                color = tuple(min(255, max(0, int(c * (1.0 + shade)))) for c in BASE)
                color = (color[0], color[1], color[2], 255)

            px[x, y] = color

    # Add a few deterministic foam pixels along band centers for texture
    # Use a simple hash so output is reproducible and tileable
    for x in range(TILE_SIZE):
        for band_y in range(0, TILE_SIZE, BAND_SPACING):
            y_center = int((band_y + AMPLITUDE * math.sin(2.0 * math.pi * (x / TILE_SIZE) * PHASES)) )
            # place a foam pixel every 6 pixels horizontally
            if (x + band_y) % 6 == 0:
                y = (y_center) % TILE_SIZE
                px[x, y] = FOAM
                # tiny streak
                px[x, (y + 1) % TILE_SIZE] = lerp(FOAM, BAND, 0.6)

    return img


def main():
    img = make_tile()
    out_name = "water-horizontal.png"
    out_path = os.path.join(OUTPUT_DIR, out_name)
    img.save(out_path)
    print(f" saved {out_path}")

    # copy into Content/Tilesets
    dst = os.path.join(CONTENT_DIR, out_name)
    shutil.copy2(out_path, dst)
    print(f" copied -> {dst}")


if __name__ == '__main__':
    main()
