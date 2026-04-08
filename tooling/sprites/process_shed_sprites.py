"""
Process garden shed source images for use as game sprites.

Takes the raw JPEG source images, removes the white background,
trims to content bounds, resizes to 64x64 (2x2 tiles), and
cleans up alpha edges from JPEG compression artifacts.

Outputs:
    Content/Sprites/garden-shed-closed.png
    Content/Sprites/garden-shed-open.png
"""

from pathlib import Path

try:
    from PIL import Image, ImageFilter
except ImportError:
    raise SystemExit(
        "Pillow is required.  Install it with:  pip install Pillow"
    )

SCRIPT_DIR = Path(__file__).resolve().parent
CONTENT_DIR = SCRIPT_DIR.parent.parent / "src" / "DogDays.Game" / "Content" / "Sprites"

# Native draw size — 2x2 tiles at 32px/tile, no runtime scaling.
TARGET_SIZE = (64, 64)

# White background threshold — any pixel with all channels above this
# is considered background.  Tuned for JPEG artifacts.
BG_THRESHOLD = 235

# Alpha cleanup threshold — after compositing, pixels with alpha below
# this are forced fully transparent; above are forced fully opaque.
ALPHA_CUTOFF = 80


def remove_white_background(img: Image.Image, threshold: int) -> Image.Image:
    """Convert near-white pixels to transparent."""
    rgba = img.convert("RGBA")
    pixels = rgba.load()
    w, h = rgba.size

    for y in range(h):
        for x in range(w):
            r, g, b, a = pixels[x, y]
            if r >= threshold and g >= threshold and b >= threshold:
                pixels[x, y] = (r, g, b, 0)

    return rgba


def clean_alpha(img: Image.Image, cutoff: int) -> Image.Image:
    """Force semi-transparent pixels to fully opaque or fully transparent."""
    pixels = img.load()
    w, h = img.size

    for y in range(h):
        for x in range(w):
            r, g, b, a = pixels[x, y]
            if a < cutoff:
                pixels[x, y] = (0, 0, 0, 0)
            elif a < 255:
                pixels[x, y] = (r, g, b, 255)

    return img


def union_bbox(a: tuple[int, int, int, int], b: tuple[int, int, int, int]) -> tuple[int, int, int, int]:
    """Return the union bounding box of two (left, upper, right, lower) boxes."""
    return (min(a[0], b[0]), min(a[1], b[1]), max(a[2], b[2]), max(a[3], b[3]))


def prepare_image(src_path: Path) -> Image.Image:
    """Load a source image and remove its white background."""
    print(f"  Loading: {src_path.name}")
    img = Image.open(src_path)
    print(f"    Source size: {img.size}, mode: {img.mode}")
    img = remove_white_background(img, BG_THRESHOLD)
    print(f"    Background removed (threshold={BG_THRESHOLD})")
    return img


def stepdown_resize(img: Image.Image, target: tuple[int, int]) -> Image.Image:
    """Downscale in halving steps with a mild sharpen between each step.

    Doing a single LANCZOS resize from 2048 to 64 loses too much edge
    detail.  Halving iteratively and sharpening preserves line work.
    """
    w, h = img.size
    tw, th = target
    while w > tw * 2 or h > th * 2:
        w = max(tw, w // 2)
        h = max(th, h // 2)
        img = img.resize((w, h), Image.Resampling.LANCZOS)
        # Light sharpen to keep edges crisp through the cascade
        img = img.filter(ImageFilter.SHARPEN)
        print(f"    Step down to: {img.size}")
    # Final resize to exact target
    img = img.resize(target, Image.Resampling.LANCZOS)
    return img


def finalize_image(img: Image.Image, bbox: tuple[int, int, int, int], out_name: str) -> Path:
    """Crop to shared bbox, multi-step resize, clean alpha, and save."""
    img = img.crop(bbox)
    print(f"  Cropped to shared bbox: {img.size}")

    img = stepdown_resize(img, TARGET_SIZE)
    print(f"  Resized to: {img.size}")

    img = clean_alpha(img, ALPHA_CUTOFF)
    print(f"  Alpha cleaned (cutoff={ALPHA_CUTOFF})")

    out_path = CONTENT_DIR / out_name
    out_path.parent.mkdir(parents=True, exist_ok=True)
    img.save(out_path, "PNG")
    print(f"  Saved: {out_path}")
    return out_path


def main():
    sources = [
        (SCRIPT_DIR / "Garden_shed_closed.jpg", "garden-shed-closed.png"),
        (SCRIPT_DIR / "garden_shed_open.jpg", "garden-shed-open.png"),
    ]

    # Phase 1: Load all images and remove backgrounds
    prepared: list[tuple[Image.Image, str]] = []
    for src_path, out_name in sources:
        if not src_path.exists():
            print(f"WARNING: Source not found: {src_path}")
            continue
        prepared.append((prepare_image(src_path), out_name))

    if not prepared:
        print("No source images found!")
        return

    # Phase 2: Compute shared bounding box across ALL frames
    bboxes = [img.getbbox() for img, _ in prepared]
    shared_bbox = bboxes[0]
    for bbox in bboxes[1:]:
        if bbox is not None and shared_bbox is not None:
            shared_bbox = union_bbox(shared_bbox, bbox)
    print(f"\nShared content bbox: {shared_bbox}")

    # Phase 3: Crop, resize, and save each frame using the shared bbox
    for img, out_name in prepared:
        print(f"\nFinalizing: {out_name}")
        finalize_image(img, shared_bbox, out_name)

    print("\nDone!")


if __name__ == "__main__":
    main()
