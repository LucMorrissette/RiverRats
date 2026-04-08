"""Generate a 16x16 pixel hatchet sprite oriented blade-right for orbital rotation."""

from PIL import Image
import os


def generate_hatchet():
    """Create a 16x16 hatchet with blade pointing right and handle pointing left.

    When rotated by the sweep angle, the blade will point outward from the orbit.
    """
    img = Image.new("RGBA", (16, 16), (0, 0, 0, 0))

    # Handle colors
    handle_dark = (101, 67, 33, 255)
    handle_mid = (139, 90, 43, 255)
    handle_light = (160, 110, 60, 255)

    # Blade colors
    blade_dark = (120, 120, 130, 255)
    blade_mid = (170, 170, 180, 255)
    blade_light = (210, 210, 220, 255)
    blade_edge = (90, 90, 100, 255)

    # Leather wrap
    wrap = (80, 55, 30, 255)

    # --- Handle: horizontal, left side, centered vertically at y=7-8 ---
    for x in range(0, 8):
        img.putpixel((x, 7), handle_mid)
        img.putpixel((x, 8), handle_dark)
    # Handle highlight
    for x in range(1, 7):
        img.putpixel((x, 7), handle_light)

    # Handle butt (left end)
    img.putpixel((0, 7), handle_dark)
    img.putpixel((0, 8), handle_dark)

    # Leather wrap near blade
    img.putpixel((7, 7), wrap)
    img.putpixel((7, 8), wrap)
    img.putpixel((8, 7), wrap)
    img.putpixel((8, 8), wrap)

    # --- Axe head: right side, vertically spanning y=3 to y=12 ---
    # Blade back (where it meets the handle)
    for y in range(5, 11):
        img.putpixel((9, y), blade_dark)

    # Blade body
    for y in range(4, 12):
        img.putpixel((10, y), blade_mid)
    for y in range(3, 13):
        img.putpixel((11, y), blade_mid)
    for y in range(3, 13):
        img.putpixel((12, y), blade_mid)

    # Blade highlights (center)
    for y in range(5, 11):
        img.putpixel((11, y), blade_light)
    for y in range(6, 10):
        img.putpixel((12, y), blade_light)

    # Cutting edge (rightmost column)
    for y in range(4, 12):
        img.putpixel((13, y), blade_edge)
    img.putpixel((13, 3), blade_dark)
    img.putpixel((13, 12), blade_dark)

    # Blade tip curves
    img.putpixel((12, 3), blade_dark)
    img.putpixel((12, 12), blade_dark)
    img.putpixel((11, 3), blade_dark)
    img.putpixel((11, 12), blade_dark)

    output_dir = os.path.join(os.path.dirname(__file__), "output")
    os.makedirs(output_dir, exist_ok=True)
    out_path = os.path.join(output_dir, "hatchet.png")
    img.save(out_path)
    print(f"Saved hatchet sprite to {out_path}")

    content_dir = os.path.join(
        os.path.dirname(__file__), "..", "..", "src", "DogDays.Game", "Content", "Sprites"
    )
    content_path = os.path.join(content_dir, "hatchet.png")
    img.save(content_path)
    print(f"Copied to {content_path}")


if __name__ == "__main__":
    generate_hatchet()
