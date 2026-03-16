"""
Generate pixel art sprites using Google Gemini (AI Studio).

Loads the API key from .env at the repo root. Sends a style-primed prompt
to Gemini and saves the resulting image(s) to tooling/sprites/output/.

Usage:
    python generate_ai_sprite.py "A small unlit campfire pit" --name campfire
    python generate_ai_sprite.py "A wooden treasure chest, closed" --name chest --count 4
    python generate_ai_sprite.py "A red potion bottle" --name potion --size 16x16

Options:
    --name   Base filename for output (default: "sprite")
    --count  Number of variants to generate (default: 1)
    --size   Target pixel dimensions WxH (default: 16x32)
    --raw    Save the raw Gemini output without downscaling
"""

import argparse
import os
import sys
import pathlib

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
REPO_ROOT = os.path.abspath(os.path.join(SCRIPT_DIR, "..", ".."))
OUTPUT_DIR = os.path.join(SCRIPT_DIR, "output", "ai")

# ---------------------------------------------------------------------------
# Load environment
# ---------------------------------------------------------------------------
try:
    from dotenv import load_dotenv
except ImportError:
    print("Missing dependency: pip install python-dotenv")
    sys.exit(1)

load_dotenv(os.path.join(REPO_ROOT, ".env"))

api_key = os.environ.get("GOOGLE_API_KEY")
if not api_key:
    print("ERROR: GOOGLE_API_KEY not found in .env or environment.")
    sys.exit(1)

# ---------------------------------------------------------------------------
# Lazy imports (fail fast with helpful messages)
# ---------------------------------------------------------------------------
try:
    from google import genai
    from google.genai import types
except ImportError:
    print("Missing dependency: pip install google-genai")
    sys.exit(1)

try:
    from PIL import Image
    import io
except ImportError:
    print("Missing dependency: pip install Pillow")
    sys.exit(1)

# ---------------------------------------------------------------------------
# Style primer
# ---------------------------------------------------------------------------
STYLE_PRIMER = (
    "STYLE PRIMER: pixel art game item icon, 16-bit SNES style, "
    "3/4 top-down perspective, centered single object, clear silhouette, "
    "limited palette (12-20 colors), crisp hard edges, no anti-aliasing, "
    "no dithering, no gradients, no shadows, no drop shadow, no cast shadow, "
    "no text, no UI, no extra objects, "
    "plain white background, suitable for display at {width}x{height} pixels."
)


def build_prompt(subject: str, width: int, height: int) -> str:
    """Combine the style primer with a subject description."""
    primer = STYLE_PRIMER.format(width=width, height=height)
    return f"{primer}\n\nSUBJECT: {subject}"


def generate_sprite(client, prompt: str) -> Image.Image | None:
    """Call Gemini and return the first image from the response, or None."""
    response = client.models.generate_content(
        model="gemini-2.5-flash-image",
        contents=prompt,
        config=types.GenerateContentConfig(
            response_modalities=["TEXT", "IMAGE"]
        ),
    )

    for part in response.candidates[0].content.parts:
        if part.inline_data is not None:
            image_bytes = part.inline_data.data
            if isinstance(image_bytes, str):
                import base64
                image_bytes = base64.b64decode(image_bytes)
            return Image.open(io.BytesIO(image_bytes))
        elif part.text is not None:
            print(f"  Model note: {part.text}")

    return None


def remove_background(img: Image.Image, tolerance: int = 30) -> Image.Image:
    """Replace near-white background pixels with transparency."""
    img = img.convert("RGBA")
    pixels = img.load()
    w, h = img.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = pixels[x, y]
            # Treat near-white pixels as background
            if r >= 255 - tolerance and g >= 255 - tolerance and b >= 255 - tolerance:
                pixels[x, y] = (0, 0, 0, 0)
    return img


def auto_crop(img: Image.Image) -> Image.Image:
    """Crop to the bounding box of non-transparent pixels."""
    bbox = img.getbbox()  # returns (left, upper, right, lower) or None
    if bbox is None:
        return img  # fully transparent — return as-is
    return img.crop(bbox)


def downscale(img: Image.Image, target_w: int, target_h: int) -> Image.Image:
    """Downscale to target size using nearest-neighbor (preserves hard edges)."""
    return img.resize((target_w, target_h), Image.NEAREST)


def main():
    parser = argparse.ArgumentParser(
        description="Generate pixel art sprites via Google Gemini."
    )
    parser.add_argument(
        "subject",
        help='Subject description, e.g. "A small unlit campfire pit"',
    )
    parser.add_argument(
        "--name", default="sprite", help="Base filename (default: sprite)"
    )
    parser.add_argument(
        "--count", type=int, default=1, help="Number of variants (default: 1)"
    )
    parser.add_argument(
        "--size",
        default="16x32",
        help="Target WxH in pixels (default: 16x32)",
    )
    parser.add_argument(
        "--raw",
        action="store_true",
        help="Save raw output without downscaling",
    )
    args = parser.parse_args()

    # Parse size
    try:
        width, height = (int(d) for d in args.size.lower().split("x"))
    except ValueError:
        print(f"ERROR: Invalid size format '{args.size}'. Use WxH, e.g. 16x32")
        sys.exit(1)

    os.makedirs(OUTPUT_DIR, exist_ok=True)

    client = genai.Client(api_key=api_key)
    prompt = build_prompt(args.subject, width, height)

    print(f"Prompt:\n{prompt}\n")
    print(f"Target: {width}x{height} | Variants: {args.count} | Raw: {args.raw}")
    print(f"Output: {OUTPUT_DIR}\n")

    for i in range(args.count):
        variant = f"_{i+1}" if args.count > 1 else ""
        label = f"{args.name}{variant}"
        print(f"Generating {label}...")

        img = generate_sprite(client, prompt)
        if img is None:
            print(f"  FAILED — no image returned for {label}")
            continue

        # Always save the raw version
        raw_path = os.path.join(OUTPUT_DIR, f"{label}_raw.png")
        img.save(raw_path)
        print(f"  Raw saved: {raw_path} ({img.size[0]}x{img.size[1]})")

        # Post-process: remove background and crop
        processed = remove_background(img)
        processed = auto_crop(processed)
        proc_path = os.path.join(OUTPUT_DIR, f"{label}_clean.png")
        processed.save(proc_path)
        print(f"  Clean saved: {proc_path} ({processed.size[0]}x{processed.size[1]})")

        if not args.raw:
            scaled = downscale(processed, width, height)
            scaled_path = os.path.join(OUTPUT_DIR, f"{label}.png")
            scaled.save(scaled_path)
            print(f"  Scaled saved: {scaled_path} ({width}x{height})")

    print("\nDone!")


if __name__ == "__main__":
    main()
