"""
Generate a bordered‑circle indicator icon from any SVG source.

This is the standard pattern for world‑space prompt icons that float above
the player (fishing hook, interact, board, etc.).  The output is a square
PNG with:
  • a filled circle background
  • a 1 px border ring
  • the SVG icon rasterised and centered inside the circle

Usage (CLI):
    python generate_indicator_icon.py <icon.svg> --name hook-icon [--size 32]
                                      [--bg 40,40,55] [--border 180,180,200]
                                      [--icon-color 255,255,255] [--padding 5]

    Or pass raw SVG on stdin with "-" as the input argument.

Usage (as library):
    from generate_indicator_icon import generate_indicator
    generate_indicator("path/to/icon.svg", name="hook-icon")

Output:
    tooling/sprites/output/<name>.png
    src/RiverRats.Game/Content/Sprites/<name>.png
"""

import argparse
import io
import math
import os
import shutil

import cairosvg
from PIL import Image, ImageDraw

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
OUTPUT_DIR = os.path.join(SCRIPT_DIR, "output")
PROJECT_ROOT = os.path.abspath(os.path.join(SCRIPT_DIR, "..", ".."))
CONTENT_SPRITES = os.path.join(
    PROJECT_ROOT, "src", "RiverRats.Game", "Content", "Sprites"
)

# ---------------------------------------------------------------------------
# Default palette — dark semi‑transparent disc, light border, white icon
# ---------------------------------------------------------------------------
DEFAULT_BG = (40, 40, 55, 220)           # circle fill
DEFAULT_BORDER = (180, 180, 200, 255)    # border ring
DEFAULT_ICON_COLOR = (255, 255, 255, 255)  # icon recolour


def _parse_color(text: str, default: tuple) -> tuple:
    """Parse 'R,G,B' or 'R,G,B,A' into a tuple; return *default* on None."""
    if text is None:
        return default
    parts = [int(c) for c in text.split(",")]
    if len(parts) == 3:
        parts.append(255)
    return tuple(parts)


def _rasterise_svg(svg_source: str, size: int) -> Image.Image:
    """Return an RGBA PIL Image from a file path or raw SVG string."""
    if os.path.isfile(svg_source):
        data = cairosvg.svg2png(url=svg_source, output_width=size, output_height=size)
    else:
        data = cairosvg.svg2png(
            bytestring=svg_source.encode("utf-8"),
            output_width=size,
            output_height=size,
        )
    return Image.open(io.BytesIO(data)).convert("RGBA")


def _recolor_image(img: Image.Image, color: tuple) -> Image.Image:
    """Replace every non‑transparent pixel with *color*, preserving alpha."""
    r, g, b, _ = color
    out = img.copy()
    px = out.load()
    for y in range(out.height):
        for x in range(out.width):
            _, _, _, a = px[x, y]
            if a > 0:
                px[x, y] = (r, g, b, a)
    return out


def generate_indicator(
    svg_source: str,
    name: str,
    size: int = 32,
    padding: int = 5,
    border_width: int = 1,
    bg_color: tuple = DEFAULT_BG,
    border_color: tuple = DEFAULT_BORDER,
    icon_color: tuple = DEFAULT_ICON_COLOR,
) -> str:
    """Create a bordered‑circle indicator icon and save it.

    Parameters
    ----------
    svg_source : str
        File path to an SVG **or** a raw ``<svg …>`` string.
    name : str
        Output filename stem (e.g. ``"hook-icon"``).
    size : int
        Width and height of the final PNG in pixels.
    padding : int
        Pixels between the circle edge and the icon bounding box.
    border_width : int
        Circle border thickness in pixels.
    bg_color : tuple
        (R, G, B, A) fill for the circle background.
    border_color : tuple
        (R, G, B, A) colour for the circle border.
    icon_color : tuple
        (R, G, B, A) — the SVG will be recoloured to this.

    Returns
    -------
    str
        Absolute path to the PNG copied into Content/Sprites.
    """
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    os.makedirs(CONTENT_SPRITES, exist_ok=True)

    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(canvas)

    # -- Draw border circle (full disc in border colour) -------------------
    cx, cy = size / 2, size / 2
    radius = size / 2 - 1  # keep 1 px margin so AA doesn't clip
    draw.ellipse(
        [cx - radius, cy - radius, cx + radius, cy + radius],
        fill=border_color,
    )

    # -- Draw inner filled circle (background) -----------------------------
    inner_r = radius - border_width
    draw.ellipse(
        [cx - inner_r, cy - inner_r, cx + inner_r, cy + inner_r],
        fill=bg_color,
    )

    # -- Rasterise SVG icon at the inscribed size --------------------------
    icon_size = size - 2 * padding
    icon_img = _rasterise_svg(svg_source, icon_size)
    icon_img = _recolor_image(icon_img, icon_color)

    # Centre the icon on the canvas
    offset_x = (size - icon_size) // 2
    offset_y = (size - icon_size) // 2
    canvas.paste(icon_img, (offset_x, offset_y), icon_img)

    # -- Save --------------------------------------------------------------
    out_path = os.path.join(OUTPUT_DIR, f"{name}.png")
    canvas.save(out_path)
    print(f"Saved {out_path} ({size}x{size})")

    content_path = os.path.join(CONTENT_SPRITES, f"{name}.png")
    shutil.copy2(out_path, content_path)
    print(f"Copied to {content_path}")
    return content_path


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------
def main() -> None:
    parser = argparse.ArgumentParser(
        description="Generate a bordered-circle indicator icon from an SVG."
    )
    parser.add_argument("input", help="SVG file path, or '-' to read stdin")
    parser.add_argument("--name", default="indicator-icon", help="Output filename stem")
    parser.add_argument("--size", type=int, default=32, help="Output size in px (default 32)")
    parser.add_argument("--padding", type=int, default=5, help="Icon inset from circle edge (default 5)")
    parser.add_argument("--border-width", type=int, default=1, help="Border ring thickness (default 1)")
    parser.add_argument("--bg", default=None, help="Background R,G,B[,A] (default 40,40,55,220)")
    parser.add_argument("--border", default=None, help="Border R,G,B[,A] (default 180,180,200,255)")
    parser.add_argument("--icon-color", default=None, help="Icon R,G,B[,A] (default 255,255,255,255)")
    args = parser.parse_args()

    bg = _parse_color(args.bg, DEFAULT_BG)
    border = _parse_color(args.border, DEFAULT_BORDER)
    icon_c = _parse_color(args.icon_color, DEFAULT_ICON_COLOR)

    if args.input == "-":
        import sys
        svg_data = sys.stdin.read()
    else:
        svg_data = args.input

    generate_indicator(
        svg_source=svg_data,
        name=args.name,
        size=args.size,
        padding=args.padding,
        border_width=args.border_width,
        bg_color=bg,
        border_color=border,
        icon_color=icon_c,
    )


if __name__ == "__main__":
    main()
