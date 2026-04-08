"""
Generate animated fish silhouette sprite sheets for the fishing mini-game.

Produces dark, semi-transparent fish silhouettes in a side-view style.
Each fish type gets a horizontal sprite strip with swim-cycle frames
(body flex + tail wag animation).

Output:
  - src/DogDays.Game/Content/Sprites/fish-silhouettes.png
    A single atlas: rows = fish types, columns = animation frames.

Fish types (top to bottom row):
    0: Small perch     (sleek, streamlined, forked tail)
  1: Medium bass      (deep body, spiny dorsal, slightly forked tail)
  2: Large catfish    (flat head, barbels, rounded tail)
"""

import os
import math
from PIL import Image, ImageDraw

# --- Configuration ---
FRAME_COUNT = 6          # Animation frames per fish
CELL_WIDTH = 34          # Max cell width (all fish centered in same-size cells)
CELL_HEIGHT = 22         # Max cell height
FISH_TYPES = 3           # Number of fish species

# Output path - direct to Content/Sprites/
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
PROJECT_ROOT = os.path.abspath(os.path.join(SCRIPT_DIR, "..", ".."))
OUTPUT_DIR = os.path.join(SCRIPT_DIR, "output")
CONTENT_PATH = os.path.join(
    PROJECT_ROOT, "src", "DogDays.Game", "Content", "Sprites", "fish-silhouettes.png"
)

# Colors - dark silhouettes with slight blue tint for underwater feel.
SILHOUETTE_COLORS = [
    (20, 35, 60, 180),    # Small perch - most transparent
    (25, 40, 65, 200),    # Medium bass - medium opacity
    (15, 30, 50, 210),    # Large catfish - most opaque
]

EYE_COLOR = (60, 80, 110, 220)


def _lerp(a, b, t):
    """Linear interpolation between two points."""
    return (a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t)


def _subdivide_polygon(pts, iterations=2):
    """Chaikin corner-cutting subdivision for smoother polygons."""
    for _ in range(iterations):
        new_pts = []
        n = len(pts)
        for i in range(n):
            p0 = pts[i]
            p1 = pts[(i + 1) % n]
            new_pts.append(_lerp(p0, p1, 0.25))
            new_pts.append(_lerp(p0, p1, 0.75))
        pts = new_pts
    return pts


def _apply_swim_bend(points, cx, bend_amount):
    """
    Apply an S-curve bend to a set of points based on horizontal distance
    from cx. Points further from center (tail end) bend more.
    """
    result = []
    for x, y in points:
        # How far along the body, normalized. Positive = toward tail.
        dist = (x - cx) / max(1, abs(x - cx) + 10)
        # Quadratic bend - tail moves most
        offset = bend_amount * ((x - cx) / 16.0) ** 2 * (1 if x > cx else -0.3)
        result.append((x, y + offset))
    return result


# ---------------------------------------------------------------------------
# Perch – sleek, torpedo-shaped, forked tail
# ---------------------------------------------------------------------------

def _perch_profile():
    """
    Return the raw body outline of a perch (facing left, centered near 0,0).
    Returns list of (x, y) for the upper profile, mirrored for lower.
    """
    # Nose to tail control points (x, half-height at that x)
    # Nose is at x=-7, tail base at x=6
    profile = [
        (-7, 0.0),   # nose tip
        (-6, 1.2),
        (-4, 2.4),
        (-2, 2.8),   # widest point, slightly forward
        (0,  2.6),
        (2,  2.2),
        (4,  1.6),
        (6,  0.8),   # tail peduncle (narrow)
    ]
    return profile


def draw_perch(draw, cx, cy, tail_angle, color, eye_color):
    """Draw a small, sleek perch with a forked tail."""
    profile = _perch_profile()

    # Build upper + lower body outline
    upper = [(cx + x, cy - h) for x, h in profile]
    lower = [(cx + x, cy + h) for x, h in reversed(profile)]
    body_pts = upper + lower

    # Smooth the body
    body_pts = _subdivide_polygon(body_pts, iterations=2)

    # Apply swim bend
    bend = math.sin(tail_angle) * 2.5
    body_pts = _apply_swim_bend(body_pts, cx, bend)

    draw.polygon(body_pts, fill=color)

    # --- Forked tail ---
    peduncle_x = cx + 6
    wag_y = math.sin(tail_angle) * 1.8

    # Upper fork
    draw.polygon([
        (peduncle_x, cy - 0.5 + wag_y * 0.4),
        (peduncle_x + 4, cy - 3.0 + wag_y),
        (peduncle_x + 3, cy - 0.2 + wag_y * 0.7),
    ], fill=color)
    # Lower fork
    draw.polygon([
        (peduncle_x, cy + 0.5 + wag_y * 0.4),
        (peduncle_x + 4, cy + 3.0 + wag_y),
        (peduncle_x + 3, cy + 0.2 + wag_y * 0.7),
    ], fill=color)

    # --- Dorsal fin (small, triangular) ---
    draw.polygon([
        (cx - 1, cy - 2.6),
        (cx + 2, cy - 2.5),
        (cx + 0.5, cy - 4.5),
    ], fill=color)

    # --- Anal fin ---
    draw.polygon([
        (cx + 1, cy + 2.4),
        (cx + 3, cy + 2.2),
        (cx + 2, cy + 3.5),
    ], fill=color)

    # --- Eye ---
    eye_x = cx - 5
    eye_y = cy - 0.8
    draw.ellipse([eye_x - 1, eye_y - 1, eye_x + 1, eye_y + 1], fill=eye_color)


# ---------------------------------------------------------------------------
# Bass – deep-bodied, prominent spiny dorsal, slightly forked tail
# ---------------------------------------------------------------------------

def _bass_profile():
    """Bass body profile – deeper body, more oval."""
    profile = [
        (-10, 0.0),   # snout tip
        (-9,  1.5),
        (-7,  3.5),   # head rises quickly
        (-5,  4.8),
        (-3,  5.4),   # deepest point (forward of center)
        (-1,  5.2),
        (1,   4.8),
        (3,   4.0),
        (5,   3.0),
        (7,   1.8),
        (9,   1.0),   # tail peduncle
    ]
    return profile


def draw_bass(draw, cx, cy, tail_angle, color, eye_color):
    """Draw a medium bass with a deep body and spiny dorsal."""
    profile = _bass_profile()

    upper = [(cx + x, cy - h) for x, h in profile]
    lower = [(cx + x, cy + h * 0.85) for x, h in reversed(profile)]  # belly slightly flatter
    body_pts = upper + lower

    body_pts = _subdivide_polygon(body_pts, iterations=2)

    bend = math.sin(tail_angle) * 3.0
    body_pts = _apply_swim_bend(body_pts, cx, bend)

    draw.polygon(body_pts, fill=color)

    wag_y = math.sin(tail_angle) * 2.5

    # --- Forked tail (broader than perch) ---
    ped_x = cx + 9
    draw.polygon([
        (ped_x, cy - 0.8 + wag_y * 0.3),
        (ped_x + 5, cy - 4.0 + wag_y),
        (ped_x + 3.5, cy + 0.0 + wag_y * 0.6),
    ], fill=color)
    draw.polygon([
        (ped_x, cy + 0.8 + wag_y * 0.3),
        (ped_x + 5, cy + 4.0 + wag_y),
        (ped_x + 3.5, cy + 0.0 + wag_y * 0.6),
    ], fill=color)

    # --- Spiny dorsal fin (larger, jagged) ---
    dorsal_base_pts = [
        (cx - 5, cy - 4.7),
        (cx - 3, cy - 6.5),  # first spine
        (cx - 1, cy - 5.0),
        (cx + 0, cy - 7.0),  # tallest spine
        (cx + 2, cy - 5.5),
        (cx + 3, cy - 6.2),
        (cx + 5, cy - 4.0),
    ]
    draw.polygon(dorsal_base_pts, fill=color)

    # --- Soft dorsal (rear, rounded) ---
    draw.polygon([
        (cx + 4, cy - 3.8),
        (cx + 7, cy - 3.0),
        (cx + 6, cy - 5.0),
    ], fill=color)

    # --- Anal fin ---
    draw.polygon([
        (cx + 3, cy + 4.0),
        (cx + 7, cy + 3.0),
        (cx + 5, cy + 5.5),
    ], fill=color)

    # --- Pectoral fin ---
    pec_angle = tail_angle * 0.3
    draw.polygon([
        (cx - 5, cy + 1.5),
        (cx - 3, cy + 4.0 + math.sin(pec_angle) * 1.0),
        (cx - 6, cy + 3.5),
    ], fill=color)

    # --- Eye ---
    eye_x = cx - 7.5
    eye_y = cy - 1.5
    r = 1.2
    draw.ellipse([eye_x - r, eye_y - r, eye_x + r, eye_y + r], fill=eye_color)

    # --- Mouth line ---
    mouth_c = (max(0, color[0] - 8), max(0, color[1] - 8), max(0, color[2] - 8), min(255, color[3] + 20))
    draw.line([(cx - 10, cy + 0.5), (cx - 8, cy + 0.5)], fill=mouth_c, width=1)


# ---------------------------------------------------------------------------
# Catfish – flat head, whiskers (barbels), rounded/squared tail
# ---------------------------------------------------------------------------

def _catfish_profile():
    """Catfish body profile – flatter head, wider body, tapers to tail."""
    profile = [
        (-13, 0.5),   # flat snout
        (-12, 2.0),
        (-10, 3.5),   # head fairly flat on top
        (-8,  4.5),
        (-5,  5.5),   # deepest
        (-2,  5.4),
        (1,   5.0),
        (4,   4.2),
        (7,   3.0),
        (10,  1.8),
        (12,  1.2),   # tail peduncle
    ]
    return profile


def draw_catfish(draw, cx, cy, tail_angle, color, eye_color):
    """Draw a large catfish with flat head, barbels, and rounded tail."""
    profile = _catfish_profile()

    # Catfish: flatter on top, rounder belly
    upper = [(cx + x, cy - h * 0.8) for x, h in profile]
    lower = [(cx + x, cy + h) for x, h in reversed(profile)]
    body_pts = upper + lower

    body_pts = _subdivide_polygon(body_pts, iterations=2)

    bend = math.sin(tail_angle) * 3.5
    body_pts = _apply_swim_bend(body_pts, cx, bend)

    draw.polygon(body_pts, fill=color)

    wag_y = math.sin(tail_angle) * 3.0

    # --- Rounded tail fin ---
    ped_x = cx + 12
    tail_pts = [
        (ped_x, cy - 1.0 + wag_y * 0.3),
        (ped_x + 3, cy - 3.5 + wag_y),
        (ped_x + 5, cy - 2.0 + wag_y),
        (ped_x + 5.5, cy + 0 + wag_y * 0.8),
        (ped_x + 5, cy + 2.0 + wag_y),
        (ped_x + 3, cy + 3.5 + wag_y),
        (ped_x, cy + 1.0 + wag_y * 0.3),
    ]
    tail_pts = _subdivide_polygon(tail_pts, iterations=1)
    draw.polygon(tail_pts, fill=color)

    # --- Small dorsal fin (catfish have a small, forward dorsal) ---
    draw.polygon([
        (cx - 6, cy - 3.5),
        (cx - 3, cy - 3.3),
        (cx - 5, cy - 6.0),
    ], fill=color)

    # --- Adipose fin (small bump near tail, characteristic of catfish) ---
    draw.polygon([
        (cx + 6, cy - 2.5),
        (cx + 9, cy - 2.2),
        (cx + 7.5, cy - 3.5),
    ], fill=color)

    # --- Anal fin (long, runs along belly) ---
    draw.polygon([
        (cx + 1, cy + 5.0),
        (cx + 8, cy + 3.5),
        (cx + 5, cy + 6.5),
        (cx + 2, cy + 6.0),
    ], fill=color)

    # --- Pectoral fins (catfish have prominent ones with spines) ---
    pec_angle = tail_angle * 0.25
    draw.polygon([
        (cx - 8, cy + 2.0),
        (cx - 5, cy + 5.5 + math.sin(pec_angle) * 1.5),
        (cx - 9, cy + 4.5),
    ], fill=color)

    # --- Barbels (whiskers) – the catfish signature ---
    whisker_c = (color[0] + 5, color[1] + 5, color[2] + 5, color[3])
    snout_x = cx - 13
    # Upper barbels (long)
    whisker_wave = math.sin(tail_angle * 0.5) * 1.0
    draw.line([
        (snout_x, cy - 0.5),
        (snout_x - 4, cy - 2.0 + whisker_wave),
        (snout_x - 6, cy - 1.5 + whisker_wave),
    ], fill=whisker_c, width=1)
    # Lower barbels (shorter, pair)
    draw.line([
        (snout_x, cy + 1.5),
        (snout_x - 3, cy + 2.5 - whisker_wave * 0.5),
    ], fill=whisker_c, width=1)
    draw.line([
        (snout_x + 1, cy + 2.0),
        (snout_x - 2, cy + 3.5 - whisker_wave * 0.5),
    ], fill=whisker_c, width=1)

    # --- Eye (small, catfish have small eyes) ---
    eye_x = cx - 10
    eye_y = cy - 1.5
    draw.ellipse([eye_x - 1, eye_y - 1, eye_x + 1, eye_y + 1], fill=eye_color)

    # --- Mouth (wider, flat) ---
    mouth_c = (max(0, color[0] - 8), max(0, color[1] - 8), max(0, color[2] - 8), min(255, color[3] + 20))
    draw.line([(cx - 13, cy + 0.5), (cx - 10, cy + 1.0)], fill=mouth_c, width=1)


# --- Fish drawing dispatch ---
FISH_DRAW_FUNCS = [draw_perch, draw_bass, draw_catfish]


def generate_fish_atlas():
    """Generate the complete fish silhouette sprite atlas."""
    atlas_width = CELL_WIDTH * FRAME_COUNT
    atlas_height = CELL_HEIGHT * FISH_TYPES
    atlas = Image.new("RGBA", (atlas_width, atlas_height), (0, 0, 0, 0))

    for fish_idx, draw_func in enumerate(FISH_DRAW_FUNCS):
        color = SILHOUETTE_COLORS[fish_idx]
        for frame_idx in range(FRAME_COUNT):
            frame = Image.new("RGBA", (CELL_WIDTH, CELL_HEIGHT), (0, 0, 0, 0))
            draw = ImageDraw.Draw(frame)

            # Tail wag: oscillate through a full cycle over FRAME_COUNT frames
            phase = (frame_idx / FRAME_COUNT) * 2 * math.pi
            tail_angle = math.sin(phase) * 0.7

            cx = CELL_WIDTH // 2
            cy = CELL_HEIGHT // 2

            draw_func(draw, cx, cy, tail_angle, color, EYE_COLOR)

            atlas.paste(frame, (frame_idx * CELL_WIDTH, fish_idx * CELL_HEIGHT))

    return atlas


def generate_preview_gif(atlas):
    """Generate an animated GIF preview of all fish types."""
    frames = []
    for frame_idx in range(FRAME_COUNT):
        preview = Image.new("RGBA", (CELL_WIDTH, CELL_HEIGHT * FISH_TYPES), (40, 80, 140, 255))
        for fish_idx in range(FISH_TYPES):
            cell = atlas.crop((
                frame_idx * CELL_WIDTH,
                fish_idx * CELL_HEIGHT,
                (frame_idx + 1) * CELL_WIDTH,
                (fish_idx + 1) * CELL_HEIGHT,
            ))
            preview.paste(cell, (0, fish_idx * CELL_HEIGHT), cell)
        # Scale up 4x for visibility
        preview_big = preview.resize(
            (CELL_WIDTH * 4, CELL_HEIGHT * FISH_TYPES * 4),
            Image.Resampling.NEAREST,
        )
        frames.append(preview_big)

    return frames


def main():
    os.makedirs(OUTPUT_DIR, exist_ok=True)

    print("Generating fish silhouette sprite atlas...")
    atlas = generate_fish_atlas()

    # Save to content folder
    os.makedirs(os.path.dirname(CONTENT_PATH), exist_ok=True)
    atlas.save(CONTENT_PATH)
    print(f"  Atlas saved to: {CONTENT_PATH}")
    print(f"  Atlas size: {atlas.width}x{atlas.height} ({FRAME_COUNT} frames × {FISH_TYPES} types)")
    print(f"  Cell size: {CELL_WIDTH}x{CELL_HEIGHT}")

    # Save preview GIF
    preview_frames = generate_preview_gif(atlas)
    gif_path = os.path.join(OUTPUT_DIR, "fish-silhouettes-preview.gif")
    preview_frames[0].save(
        gif_path,
        save_all=True,
        append_images=preview_frames[1:],
        duration=120,
        loop=0,
    )
    print(f"  Preview GIF: {gif_path}")

    # Also save a 4x scaled atlas for inspection
    scaled = atlas.resize(
        (atlas.width * 4, atlas.height * 4),
        Image.Resampling.NEAREST,
    )
    scaled_path = os.path.join(OUTPUT_DIR, "fish-silhouettes-4x.png")
    scaled.save(scaled_path)
    print(f"  4x scaled: {scaled_path}")


if __name__ == "__main__":
    main()
