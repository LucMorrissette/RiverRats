"""
Convert an SVG file (or inline SVG string) to a pixel-art-ready PNG sprite.

Rasterises the SVG at 32×32 (or custom size) using cairosvg, then optionally
recolours the fill to a single palette colour.

Usage:
    python convert_svg_to_sprite.py <input.svg> [--size 32] [--name fishing-icon]

    Or import and call convert() from another script.

Output:
    tooling/sprites/output/<name>.png
    src/RiverRats.Game/Content/Sprites/<name>.png
"""

import argparse
import os
import shutil
import io

import cairosvg
from PIL import Image

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
OUTPUT_DIR = os.path.join(SCRIPT_DIR, "output")
PROJECT_ROOT = os.path.abspath(os.path.join(SCRIPT_DIR, "..", ".."))
CONTENT_SPRITES = os.path.join(
    PROJECT_ROOT, "src", "RiverRats.Game", "Content", "Sprites"
)


def convert(
    svg_source: str,
    name: str,
    size: int = 32,
    recolor: tuple | None = None,
) -> str:
    """Rasterise *svg_source* (file path or raw SVG string) to a PNG sprite.

    Parameters
    ----------
    svg_source : str
        Path to an SVG file, or a raw ``<svg …>`` string.
    name : str
        Base filename (without extension) for the output PNG.
    size : int
        Target width **and** height in pixels.
    recolor : tuple | None
        If provided, an (R, G, B) tuple.  Every non-transparent pixel will be
        replaced with this colour (alpha is preserved).

    Returns
    -------
    str
        Absolute path to the generated PNG in the Content/Sprites folder.
    """
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    os.makedirs(CONTENT_SPRITES, exist_ok=True)

    # Determine whether svg_source is a file path or raw SVG markup.
    if os.path.isfile(svg_source):
        png_bytes = cairosvg.svg2png(
            url=svg_source,
            output_width=size,
            output_height=size,
        )
    else:
        png_bytes = cairosvg.svg2png(
            bytestring=svg_source.encode("utf-8"),
            output_width=size,
            output_height=size,
        )

    img = Image.open(io.BytesIO(png_bytes)).convert("RGBA")

    # Optional single-colour recolour (keeps alpha channel intact).
    if recolor is not None:
        r, g, b = recolor
        pixels = img.load()
        for y in range(img.height):
            for x in range(img.width):
                _, _, _, a = pixels[x, y]
                if a > 0:
                    pixels[x, y] = (r, g, b, a)

    out_path = os.path.join(OUTPUT_DIR, f"{name}.png")
    img.save(out_path)
    print(f"Saved {out_path} ({size}x{size})")

    content_path = os.path.join(CONTENT_SPRITES, f"{name}.png")
    shutil.copy2(out_path, content_path)
    print(f"Copied to {content_path}")
    return content_path


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Convert an SVG to a game-ready PNG sprite."
    )
    parser.add_argument("input", help="SVG file path or '-' to read stdin")
    parser.add_argument("--size", type=int, default=32, help="Output size (default 32)")
    parser.add_argument("--name", default="svg-sprite", help="Output filename stem")
    parser.add_argument(
        "--recolor",
        default=None,
        help="Recolour all pixels to R,G,B (e.g. '25,35,60')",
    )
    args = parser.parse_args()

    recolor = None
    if args.recolor:
        recolor = tuple(int(c) for c in args.recolor.split(","))

    if args.input == "-":
        import sys
        svg_data = sys.stdin.read()
        convert(svg_data, args.name, args.size, recolor)
    else:
        convert(args.input, args.name, args.size, recolor)


if __name__ == "__main__":
    main()
