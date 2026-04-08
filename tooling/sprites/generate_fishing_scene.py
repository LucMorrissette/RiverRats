"""
Generate the fishing mini-game scene: individual tile images, TSX, and TMX.

Produces a side-view BoF2-style cross-section scene with:
  - Sky at top with scattered clouds
  - Grassy embankment on the left
  - Sloping cliff face going into the water
  - Graduated water (shallow → mid → deep)
  - Lake bottom with sediment, pebbles, seaweed, rocks

Output:
  - Content/Tilesets/fishing/*.png       (individual tile images, 16x16 each)
  - Content/Tilesets/FishingTiles.tsx     (Tiled collection-of-images tileset)
  - Content/Maps/FishingSpot.tmx         (30x17 map, 3 layers + objects)
"""

import os
import math
import random
from PIL import Image, ImageDraw

# --- Constants ---
TILE_SIZE = 16
ATLAS_COLS = 8
ATLAS_ROWS = 3
ATLAS_WIDTH = ATLAS_COLS * TILE_SIZE   # 128 (used for preview atlas only)
ATLAS_HEIGHT = ATLAS_ROWS * TILE_SIZE  # 48  (used for preview atlas only)

MAP_WIDTH = 30   # tiles
MAP_HEIGHT = 17  # tiles  (30x16=480, 17x16=272 -- fits 480x270 virtual res)

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
PROJECT_ROOT = os.path.abspath(os.path.join(SCRIPT_DIR, "..", ".."))
CONTENT_DIR = os.path.join(PROJECT_ROOT, "src", "DogDays.Game", "Content")
TILESETS_DIR = os.path.join(CONTENT_DIR, "Tilesets")
FISHING_TILES_DIR = os.path.join(TILESETS_DIR, "fishing")
MAPS_DIR = os.path.join(CONTENT_DIR, "Maps")
OUTPUT_DIR = os.path.join(SCRIPT_DIR, "output")

# --- Tile IDs (0-based local, GID = local + 1) ---
SKY = 0
CLOUD = 1
GRASS = 2
EARTH = 3
GRASS_EDGE = 4     # green left, transparent right
EARTH_EDGE = 5     # brown left, transparent right
WATER_SURFACE = 6  # blue with white shimmer
SHALLOW_WATER = 7
MID_WATER = 8
DEEP_WATER = 9
BOTTOM = 10        # sandy sediment
BOTTOM_PEBBLES = 11
SEAWEED = 12       # green plant, transparent bg
ROCK = 13          # gray rock, transparent bg
DARK_EARTH = 14
GRASS_DARK = 15    # darker grass variant
SLOPE_A = 16       # 30° slope upper half: earth below diagonal (0,0)→(16,8)
SLOPE_B = 17       # 30° slope lower half: earth below diagonal (0,8)→(16,16)
SLOPE_A_DARK = 18  # same as SLOPE_A with dark earth
SLOPE_B_DARK = 19  # same as SLOPE_B with dark earth
GRASS_EARTH = 20   # grass-to-earth transition (hand-edited)

# Mapping from tile ID to filename (without extension).
TILE_NAMES = {
    SKY: "sky",
    CLOUD: "cloud",
    GRASS: "grass",
    EARTH: "earth",
    GRASS_EDGE: "grass-edge",
    EARTH_EDGE: "earth-edge",
    WATER_SURFACE: "water-surface",
    SHALLOW_WATER: "shallow-water",
    MID_WATER: "mid-water",
    DEEP_WATER: "deep-water",
    BOTTOM: "bottom",
    BOTTOM_PEBBLES: "bottom-pebbles",
    SEAWEED: "seaweed",
    ROCK: "rock",
    DARK_EARTH: "dark-earth",
    GRASS_DARK: "grass-dark",
    SLOPE_A: "slope-a",
    SLOPE_B: "slope-b",
    SLOPE_A_DARK: "slope-a-dark",
    SLOPE_B_DARK: "slope-b-dark",
    GRASS_EARTH: "grass-earth",
}

# --- Colors ---
SKY_COLOR = (135, 206, 235, 255)
SKY_LOWER = (160, 215, 240, 255)
CLOUD_WHITE = (245, 250, 255, 200)
GRASS_BASE = (72, 148, 50, 255)
GRASS_DARK_C = (58, 125, 40, 255)
GRASS_LIGHT = (90, 170, 62, 255)
GRASS_BLADE = (95, 180, 65, 255)
EARTH_BASE = (110, 85, 60, 255)
EARTH_DARK = (85, 65, 45, 255)
EARTH_LIGHT = (130, 100, 72, 255)
WATER_SURF_BASE = (60, 130, 200, 255)
WATER_SHIMMER = (200, 230, 255, 220)
SHALLOW_COLOR = (55, 130, 190, 255)
SHALLOW_LIGHT = (70, 145, 200, 255)
MID_COLOR = (35, 90, 160, 255)
DEEP_COLOR = (18, 50, 110, 255)
DEEP_DARKER = (12, 35, 85, 255)
BOTTOM_COLOR = (90, 75, 55, 255)
BOTTOM_DARK = (72, 60, 44, 255)
BOTTOM_LIGHT = (105, 88, 65, 255)
PEBBLE_COLOR = (120, 110, 95, 255)
PEBBLE_DARK = (80, 70, 58, 255)
SEAWEED_BASE = (30, 100, 55, 255)
SEAWEED_LIGHT = (45, 125, 70, 255)
ROCK_COLOR = (95, 90, 82, 255)
ROCK_LIGHT = (115, 108, 98, 255)
ROCK_DARK = (70, 65, 58, 255)
DARK_EARTH_COLOR = (55, 42, 30, 255)
TRANSPARENT = (0, 0, 0, 0)


def lerp_color(c1, c2, t):
    """Linearly interpolate between two RGBA colors."""
    return tuple(int(c1[i] + (c2[i] - c1[i]) * t) for i in range(4))


def noise_pixel(base_color, variation=8):
    """Add subtle per-pixel noise to a base color."""
    r, g, b, a = base_color
    d = random.randint(-variation, variation)
    return (
        max(0, min(255, r + d)),
        max(0, min(255, g + d)),
        max(0, min(255, b + d)),
        a,
    )


def draw_tile_sky(draw, ox, oy):
    """Tile 0: Solid sky with subtle vertical gradient."""
    for y in range(TILE_SIZE):
        t = y / (TILE_SIZE - 1)
        color = lerp_color(SKY_COLOR, SKY_LOWER, t * 0.3)
        for x in range(TILE_SIZE):
            draw.point((ox + x, oy + y), fill=noise_pixel(color, 3))


def draw_tile_cloud(draw, ox, oy):
    """Tile 1: Sky with soft cloud wisps."""
    # Start with sky background.
    draw_tile_sky(draw, ox, oy)
    # Add cloud puffs.
    cloud_positions = [(3, 5), (7, 4), (11, 6), (5, 8), (9, 7)]
    for cx, cy in cloud_positions:
        for dy in range(-2, 3):
            for dx in range(-3, 4):
                dist = abs(dx) * 0.7 + abs(dy)
                if dist < 2.5:
                    px, py = ox + cx + dx, oy + cy + dy
                    if 0 <= cx + dx < TILE_SIZE and 0 <= cy + dy < TILE_SIZE:
                        alpha = int(180 * max(0, 1 - dist / 2.5))
                        draw.point((px, py), fill=(240, 248, 255, alpha))


def draw_tile_grass(draw, ox, oy):
    """Tile 2: Grass with texture and blades."""
    for y in range(TILE_SIZE):
        for x in range(TILE_SIZE):
            color = noise_pixel(GRASS_BASE, 6)
            draw.point((ox + x, oy + y), fill=color)
    # Add darker patches.
    for _ in range(6):
        px = random.randint(0, TILE_SIZE - 2)
        py = random.randint(2, TILE_SIZE - 1)
        draw.point((ox + px, oy + py), fill=noise_pixel(GRASS_DARK_C, 4))
    # Add grass blade tips at the top edge.
    for x in range(0, TILE_SIZE, 2):
        blade_h = random.randint(1, 3)
        for dy in range(blade_h):
            draw.point((ox + x, oy + dy), fill=noise_pixel(GRASS_BLADE, 5))


def draw_tile_grass_dark(draw, ox, oy):
    """Tile 15: Darker grass variant for visual variation."""
    for y in range(TILE_SIZE):
        for x in range(TILE_SIZE):
            color = noise_pixel(GRASS_DARK_C, 5)
            draw.point((ox + x, oy + y), fill=color)


def draw_tile_earth(draw, ox, oy):
    """Tile 3: Earth/dirt with rock texture."""
    for y in range(TILE_SIZE):
        for x in range(TILE_SIZE):
            t = y / (TILE_SIZE - 1)
            base = lerp_color(EARTH_BASE, EARTH_DARK, t * 0.4)
            draw.point((ox + x, oy + y), fill=noise_pixel(base, 6))
    # Add lighter rock streaks.
    for _ in range(4):
        sx = random.randint(0, TILE_SIZE - 3)
        sy = random.randint(0, TILE_SIZE - 1)
        length = random.randint(2, 4)
        for dx in range(length):
            if sx + dx < TILE_SIZE:
                draw.point((ox + sx + dx, oy + sy), fill=noise_pixel(EARTH_LIGHT, 5))


def draw_tile_grass_edge(draw, ox, oy):
    """Tile 4: Grass left half, transparent right half with ragged edge."""
    # Fill left portion with grass.
    edge_x = TILE_SIZE // 2 + 1  # ~9 pixels of grass
    for y in range(TILE_SIZE):
        # Jagged edge offset.
        jag = random.choice([-1, 0, 0, 1])
        actual_edge = edge_x + jag
        for x in range(TILE_SIZE):
            if x < actual_edge:
                draw.point((ox + x, oy + y), fill=noise_pixel(GRASS_BASE, 5))
            else:
                draw.point((ox + x, oy + y), fill=TRANSPARENT)
    # Grass blades at top.
    for x in range(0, edge_x, 2):
        blade_h = random.randint(1, 3)
        for dy in range(blade_h):
            draw.point((ox + x, oy + dy), fill=noise_pixel(GRASS_BLADE, 5))
    # Dark highlight on the edge.
    for y in range(TILE_SIZE):
        jag = random.choice([-1, 0, 0, 1])
        ex = edge_x + jag - 1
        if 0 <= ex < TILE_SIZE:
            draw.point((ox + ex, oy + y), fill=noise_pixel(GRASS_DARK_C, 4))


def draw_tile_earth_edge(draw, ox, oy):
    """Tile 5: Earth left half, transparent right half with rocky edge."""
    edge_x = TILE_SIZE // 2 + 1
    for y in range(TILE_SIZE):
        jag = random.choice([-2, -1, 0, 0, 1])
        actual_edge = edge_x + jag
        for x in range(TILE_SIZE):
            if x < actual_edge:
                t = y / (TILE_SIZE - 1)
                base = lerp_color(EARTH_BASE, EARTH_DARK, t * 0.4)
                draw.point((ox + x, oy + y), fill=noise_pixel(base, 5))
            else:
                draw.point((ox + x, oy + y), fill=TRANSPARENT)
    # Highlight edge.
    for y in range(TILE_SIZE):
        jag = random.choice([-2, -1, 0, 0, 1])
        ex = edge_x + jag - 1
        if 0 <= ex < TILE_SIZE:
            draw.point((ox + ex, oy + y), fill=noise_pixel(EARTH_LIGHT, 6))


def draw_tile_water_surface(draw, ox, oy):
    """Tile 6: Water surface with shimmer line at top."""
    # Water body.
    for y in range(TILE_SIZE):
        t = y / (TILE_SIZE - 1)
        base = lerp_color(WATER_SURF_BASE, SHALLOW_COLOR, t)
        for x in range(TILE_SIZE):
            draw.point((ox + x, oy + y), fill=noise_pixel(base, 3))
    # Shimmer highlight across the top 2 rows.
    for x in range(TILE_SIZE):
        shimmer = WATER_SHIMMER if (x % 3 != 2) else (180, 215, 240, 200)
        draw.point((ox + x, oy), fill=shimmer)
        if x % 2 == 0:
            draw.point((ox + x, oy + 1), fill=(180, 220, 245, 160))


def draw_tile_shallow_water(draw, ox, oy):
    """Tile 7: Shallow water — lighter blue."""
    for y in range(TILE_SIZE):
        t = y / (TILE_SIZE - 1)
        base = lerp_color(SHALLOW_COLOR, MID_COLOR, t * 0.3)
        for x in range(TILE_SIZE):
            draw.point((ox + x, oy + y), fill=noise_pixel(base, 3))
    # Subtle light caustic patterns.
    for _ in range(3):
        cx = random.randint(1, TILE_SIZE - 2)
        cy = random.randint(1, TILE_SIZE - 2)
        draw.point((ox + cx, oy + cy), fill=noise_pixel(SHALLOW_LIGHT, 4))


def draw_tile_mid_water(draw, ox, oy):
    """Tile 8: Mid-depth water — medium blue."""
    for y in range(TILE_SIZE):
        t = y / (TILE_SIZE - 1)
        base = lerp_color(MID_COLOR, DEEP_COLOR, t * 0.3)
        for x in range(TILE_SIZE):
            draw.point((ox + x, oy + y), fill=noise_pixel(base, 3))


def draw_tile_deep_water(draw, ox, oy):
    """Tile 9: Deep water — dark blue."""
    for y in range(TILE_SIZE):
        t = y / (TILE_SIZE - 1)
        base = lerp_color(DEEP_COLOR, DEEP_DARKER, t * 0.3)
        for x in range(TILE_SIZE):
            draw.point((ox + x, oy + y), fill=noise_pixel(base, 2))


def draw_tile_bottom(draw, ox, oy):
    """Tile 10: Lake bottom sediment — sandy brown."""
    for y in range(TILE_SIZE):
        t = y / (TILE_SIZE - 1)
        base = lerp_color(BOTTOM_COLOR, BOTTOM_DARK, t * 0.3)
        for x in range(TILE_SIZE):
            draw.point((ox + x, oy + y), fill=noise_pixel(base, 5))
    # Some lighter grains.
    for _ in range(5):
        px = random.randint(0, TILE_SIZE - 1)
        py = random.randint(0, TILE_SIZE - 1)
        draw.point((ox + px, oy + py), fill=noise_pixel(BOTTOM_LIGHT, 4))


def draw_tile_bottom_pebbles(draw, ox, oy):
    """Tile 11: Bottom with small pebbles scattered on sediment."""
    draw_tile_bottom(draw, ox, oy)
    # Draw 3-4 small pebbles (2-3px ovals).
    for _ in range(random.randint(3, 5)):
        px = random.randint(1, TILE_SIZE - 3)
        py = random.randint(1, TILE_SIZE - 3)
        w = random.randint(2, 3)
        h = random.randint(1, 2)
        color = random.choice([PEBBLE_COLOR, PEBBLE_DARK])
        draw.ellipse([ox + px, oy + py, ox + px + w, oy + py + h], fill=color)


def draw_tile_seaweed(draw, ox, oy):
    """Tile 12: Underwater seaweed plant on transparent background."""
    # Transparent background (water shows through from layer below).
    for y in range(TILE_SIZE):
        for x in range(TILE_SIZE):
            draw.point((ox + x, oy + y), fill=TRANSPARENT)
    # Draw 2-3 wavy seaweed strands growing upward from bottom.
    num_strands = random.randint(2, 3)
    for s in range(num_strands):
        base_x = 3 + s * 5 + random.randint(-1, 1)
        strand_height = random.randint(8, 13)
        strand_color = random.choice([SEAWEED_BASE, SEAWEED_LIGHT])
        for dy in range(strand_height):
            y = TILE_SIZE - 1 - dy
            wave = math.sin(dy * 0.8 + s * 1.5) * 1.5
            x = base_x + int(wave)
            if 0 <= x < TILE_SIZE and 0 <= y < TILE_SIZE:
                draw.point((ox + x, oy + y), fill=strand_color)
                # Thicken at base.
                if dy < 4 and x + 1 < TILE_SIZE:
                    draw.point((ox + x + 1, oy + y), fill=noise_pixel(strand_color, 8))
                # Leaf tip.
                if dy > 5 and random.random() < 0.3:
                    lx = x + random.choice([-1, 1])
                    if 0 <= lx < TILE_SIZE:
                        draw.point((ox + lx, oy + y), fill=SEAWEED_LIGHT)


def draw_tile_rock(draw, ox, oy):
    """Tile 13: Underwater rock on transparent background."""
    for y in range(TILE_SIZE):
        for x in range(TILE_SIZE):
            draw.point((ox + x, oy + y), fill=TRANSPARENT)
    # Draw a small rocky mound.
    cx, cy = TILE_SIZE // 2, TILE_SIZE - 4
    for dy in range(-4, 5):
        width = int(5 - abs(dy) * 0.8)
        for dx in range(-width, width + 1):
            px, py = cx + dx, cy + dy
            if 0 <= px < TILE_SIZE and 0 <= py < TILE_SIZE:
                dist = abs(dx) + abs(dy)
                if dy < 0:
                    color = noise_pixel(ROCK_LIGHT, 6)
                else:
                    color = noise_pixel(ROCK_DARK, 6) if dist > 4 else noise_pixel(ROCK_COLOR, 6)
                draw.point((ox + px, oy + py), fill=color)


def draw_tile_dark_earth(draw, ox, oy):
    """Tile 14: Very dark earth for deep underground."""
    for y in range(TILE_SIZE):
        for x in range(TILE_SIZE):
            draw.point((ox + x, oy + y), fill=noise_pixel(DARK_EARTH_COLOR, 4))


def _draw_slope_tile(draw, ox, oy, base_color, dark_color, light_color, y_offset):
    """Draw a ~30° slope tile.

    The diagonal goes from (0, y_offset) to (16, y_offset + 8).
    Earth fills below the diagonal; transparency above.
    """
    for y in range(TILE_SIZE):
        for x in range(TILE_SIZE):
            # Line equation: y_line = y_offset + 0.5 * x
            # Earth below: pixel_y >= y_line → x <= 2*(pixel_y - y_offset)
            threshold = 2 * (y - y_offset)
            if x <= threshold:
                # Earth pixel — add slight depth gradient.
                t = y / (TILE_SIZE - 1)
                color = lerp_color(base_color, dark_color, t * 0.4)
                draw.point((ox + x, oy + y), fill=noise_pixel(color, 5))
            else:
                draw.point((ox + x, oy + y), fill=TRANSPARENT)

    # Highlight along the slope edge for a rocky look.
    for x in range(TILE_SIZE):
        edge_y = int(y_offset + 0.5 * x)
        if 0 <= edge_y < TILE_SIZE:
            draw.point((ox + x, oy + edge_y), fill=noise_pixel(light_color, 6))
            if edge_y + 1 < TILE_SIZE:
                draw.point((ox + x, oy + edge_y + 1), fill=noise_pixel(base_color, 4))


def draw_tile_slope_a(draw, ox, oy):
    """Tile 16: 30° slope, upper half. Diagonal (0,0)→(16,8), earth below."""
    _draw_slope_tile(draw, ox, oy, EARTH_BASE, EARTH_DARK, EARTH_LIGHT, 0)


def draw_tile_slope_b(draw, ox, oy):
    """Tile 17: 30° slope, lower half. Diagonal (0,8)→(16,16), earth below."""
    _draw_slope_tile(draw, ox, oy, EARTH_BASE, EARTH_DARK, EARTH_LIGHT, 8)


def draw_tile_slope_a_dark(draw, ox, oy):
    """Tile 18: 30° slope, upper half, dark earth."""
    _draw_slope_tile(draw, ox, oy, DARK_EARTH_COLOR, (40, 30, 20, 255), EARTH_DARK, 0)


def draw_tile_slope_b_dark(draw, ox, oy):
    """Tile 19: 30° slope, lower half, dark earth."""
    _draw_slope_tile(draw, ox, oy, DARK_EARTH_COLOR, (40, 30, 20, 255), EARTH_DARK, 8)


# Tile drawer lookup -- maps tile ID to draw function.
TILE_DRAWERS = {
    SKY: draw_tile_sky,
    CLOUD: draw_tile_cloud,
    GRASS: draw_tile_grass,
    EARTH: draw_tile_earth,
    GRASS_EDGE: draw_tile_grass_edge,
    EARTH_EDGE: draw_tile_earth_edge,
    WATER_SURFACE: draw_tile_water_surface,
    SHALLOW_WATER: draw_tile_shallow_water,
    MID_WATER: draw_tile_mid_water,
    DEEP_WATER: draw_tile_deep_water,
    BOTTOM: draw_tile_bottom,
    BOTTOM_PEBBLES: draw_tile_bottom_pebbles,
    SEAWEED: draw_tile_seaweed,
    ROCK: draw_tile_rock,
    DARK_EARTH: draw_tile_dark_earth,
    GRASS_DARK: draw_tile_grass_dark,
    SLOPE_A: draw_tile_slope_a,
    SLOPE_B: draw_tile_slope_b,
    SLOPE_A_DARK: draw_tile_slope_a_dark,
    SLOPE_B_DARK: draw_tile_slope_b_dark,
}


def generate_individual_tiles():
    """Generate individual tile images (16x16 each) and save to fishing/ dir."""
    random.seed(42)  # Deterministic output.
    os.makedirs(FISHING_TILES_DIR, exist_ok=True)

    tiles = {}  # tile_id -> PIL Image
    for tile_id, drawer in TILE_DRAWERS.items():
        img = Image.new("RGBA", (TILE_SIZE, TILE_SIZE), (0, 0, 0, 0))
        draw = ImageDraw.Draw(img)
        drawer(draw, 0, 0)
        tiles[tile_id] = img

        name = TILE_NAMES[tile_id]
        path = os.path.join(FISHING_TILES_DIR, f"{name}.png")
        img.save(path)

    return tiles


def generate_preview_atlas(tiles):
    """Generate a preview atlas image from individual tiles (for visual reference only)."""
    atlas = Image.new("RGBA", (ATLAS_WIDTH, ATLAS_HEIGHT), (0, 0, 0, 0))
    for tile_id, tile_img in tiles.items():
        col = tile_id % ATLAS_COLS
        row = tile_id // ATLAS_COLS
        atlas.paste(tile_img, (col * TILE_SIZE, row * TILE_SIZE))
    return atlas


def gid(local_id):
    """Convert 0-based local tile ID to 1-based TMX GID (firstgid=1)."""
    return local_id + 1


def build_background_layer():
    """Build the background layer data (sky + water gradient, full coverage)."""
    data = [[0] * MAP_WIDTH for _ in range(MAP_HEIGHT)]

    # Cloud positions (row, col).
    clouds = [(1, 11), (1, 12), (2, 20), (2, 21), (3, 15), (4, 25)]

    for row in range(MAP_HEIGHT):
        for col in range(MAP_WIDTH):
            if row <= 5:
                # Sky region.
                if (row, col) in clouds:
                    data[row][col] = gid(CLOUD)
                else:
                    data[row][col] = gid(SKY)
            elif row == 6:
                data[row][col] = gid(WATER_SURFACE)
            elif row == 7:
                data[row][col] = gid(SHALLOW_WATER)
            elif row in (8, 9):
                data[row][col] = gid(MID_WATER)
            elif row in (10, 11, 12, 13):
                data[row][col] = gid(DEEP_WATER)
            elif row >= 14:
                # Bottom rows — mostly sediment with scattered pebbles.
                if (row, col) in [(14, 8), (14, 18), (15, 12), (15, 24), (16, 6), (16, 20)]:
                    data[row][col] = gid(BOTTOM_PEBBLES)
                else:
                    data[row][col] = gid(BOTTOM)

    return data


def build_shore_layer():
    """Build the shore/land overlay layer with a gradual ~30° underwater slope."""
    data = [[0] * MAP_WIDTH for _ in range(MAP_HEIGHT)]

    # --- Above water: grass embankment ---
    for row in range(6):
        for col in range(4):
            data[row][col] = gid(GRASS)
        data[row][4] = gid(GRASS_EDGE)

    # --- Underwater slope ---
    # Each row: solid earth fills to (slope_start - 1), then SLOPE_A, SLOPE_B.
    # The slope moves 2 columns right per row, creating a ~27° angle.
    # Tuple: (solid_fill_end, slope_a_col, slope_b_col, fill_tile, slope_a_tile, slope_b_tile)
    slope_profile = {
        6:  (4, 4, 5,   EARTH,      SLOPE_A,      SLOPE_B),
        7:  (6, 6, 7,   EARTH,      SLOPE_A,      SLOPE_B),
        8:  (8, 8, 9,   EARTH,      SLOPE_A,      SLOPE_B),
        9:  (10, 10, 11, EARTH,     SLOPE_A,      SLOPE_B),
        10: (12, 12, 13, DARK_EARTH, SLOPE_A_DARK, SLOPE_B_DARK),
        11: (14, 14, 15, DARK_EARTH, SLOPE_A_DARK, SLOPE_B_DARK),
        12: (16, 16, 17, DARK_EARTH, SLOPE_A_DARK, SLOPE_B_DARK),
        13: (18, 18, 19, DARK_EARTH, SLOPE_A_DARK, SLOPE_B_DARK),
    }

    for row, (fill_end, sa_col, sb_col, fill_tile, sa_tile, sb_tile) in slope_profile.items():
        for col in range(fill_end):
            data[row][col] = gid(fill_tile)
        data[row][sa_col] = gid(sa_tile)
        if sb_col < MAP_WIDTH:
            data[row][sb_col] = gid(sb_tile)

    # --- Bottom rows: earth extends under the slope foot ---
    for col in range(20):
        data[14][col] = gid(DARK_EARTH)
    for col in range(21):
        data[15][col] = gid(DARK_EARTH)
    for col in range(22):
        data[16][col] = gid(DARK_EARTH)

    return data


def build_details_layer():
    """Build the details layer (seaweed + rocks scattered in underwater area)."""
    data = [[0] * MAP_WIDTH for _ in range(MAP_HEIGHT)]

    # Seaweed positions (in open water, past the slope edge).
    seaweed_positions = [
        (12, 20), (12, 25), (12, 28),
        (13, 22), (13, 27),
    ]
    for row, col in seaweed_positions:
        data[row][col] = gid(SEAWEED)

    # Rock positions (on lake bottom, past the slope foot).
    rock_positions = [
        (14, 22), (14, 27),
        (15, 24), (15, 28),
    ]
    for row, col in rock_positions:
        data[row][col] = gid(ROCK)

    return data


def layer_to_csv(data):
    """Convert 2D tile data array to TMX CSV string."""
    lines = []
    for row_idx, row in enumerate(data):
        line = ",".join(str(v) for v in row)
        if row_idx < len(data) - 1:
            line += ","
        lines.append(line)
    return "\n".join(lines)


def generate_tsx():
    """Generate a Tiled collection-of-images tileset definition file."""
    tile_count = len(TILE_NAMES)
    lines = [
        '<?xml version="1.0" encoding="UTF-8"?>',
        f'<tileset version="1.10" tiledversion="1.12.0" name="FishingTiles"'
        f' tilewidth="{TILE_SIZE}" tileheight="{TILE_SIZE}"'
        f' tilecount="{tile_count}" columns="0">',
        ' <grid orientation="orthogonal" width="1" height="1"/>',
    ]
    for tile_id in sorted(TILE_NAMES.keys()):
        name = TILE_NAMES[tile_id]
        lines.append(f' <tile id="{tile_id}">')
        lines.append(f'  <image source="fishing/{name}.png"'
                     f' width="{TILE_SIZE}" height="{TILE_SIZE}"/>')
        lines.append(' </tile>')
    lines.append('</tileset>')
    lines.append('')  # trailing newline
    return '\n'.join(lines)


def generate_tmx():
    """Generate the fishing spot TMX map file."""
    bg = build_background_layer()
    shore = build_shore_layer()
    details = build_details_layer()

    bg_csv = layer_to_csv(bg)
    shore_csv = layer_to_csv(shore)
    details_csv = layer_to_csv(details)

    # SwimBounds polygon: follows the slope contour.
    # Origin at the top of the open water (just right of the surface slope edge).
    # The polygon tracks the slope rightward and down, then across the bottom,
    # up the right side, and back across the top.
    origin_x = 96   # col 6, just past row 6's slope
    origin_y = 100   # just below water surface
    right_edge = MAP_WIDTH * TILE_SIZE - 4
    bottom_y = 224   # row 14 top

    # Polygon points relative to origin — follows the slope.
    # Each row the slope is 2 tiles (32px) further right.
    pts = [
        "0,0",                                          # top of open water
        f"{right_edge - origin_x},0",                    # top-right
        f"{right_edge - origin_x},{bottom_y - origin_y}", # bottom-right
        f"{20*16 - origin_x},{bottom_y - origin_y}",     # bottom at slope foot (col 20)
        # Walk back up the slope (2 cols per row, each row is 16px)
        f"{19*16 + 8 - origin_x},{13*16 - origin_y}",   # row 13 edge (~col 19.5)
        f"{17*16 + 8 - origin_x},{12*16 - origin_y}",   # row 12
        f"{15*16 + 8 - origin_x},{11*16 - origin_y}",   # row 11
        f"{13*16 + 8 - origin_x},{10*16 - origin_y}",   # row 10
        f"{11*16 + 8 - origin_x},{9*16 - origin_y}",    # row 9
        f"{9*16 + 8 - origin_x},{8*16 - origin_y}",     # row 8
        f"{7*16 + 8 - origin_x},{7*16 - origin_y}",     # row 7
        f"{5*16 + 8 - origin_x},{6*16 - origin_y}",     # row 6 (surface)
    ]
    points_str = " ".join(pts)

    return f'''<?xml version="1.0" encoding="UTF-8"?>
<map version="1.10" tiledversion="1.12.0" orientation="orthogonal" renderorder="right-down" width="{MAP_WIDTH}" height="{MAP_HEIGHT}" tilewidth="{TILE_SIZE}" tileheight="{TILE_SIZE}" infinite="0" nextlayerid="5" nextobjectid="2">
 <tileset firstgid="1" source="../Tilesets/FishingTiles.tsx"/>
 <layer id="1" name="Background" width="{MAP_WIDTH}" height="{MAP_HEIGHT}">
  <data encoding="csv">
{bg_csv}
</data>
 </layer>
 <layer id="2" name="Shore" width="{MAP_WIDTH}" height="{MAP_HEIGHT}">
  <data encoding="csv">
{shore_csv}
</data>
 </layer>
 <layer id="3" name="Details" width="{MAP_WIDTH}" height="{MAP_HEIGHT}">
  <data encoding="csv">
{details_csv}
</data>
 </layer>
 <objectgroup id="4" name="SwimBounds">
  <object id="1" name="SwimBounds" x="{origin_x}" y="{origin_y}">
   <polygon points="{points_str}"/>
  </object>
 </objectgroup>
</map>
'''


def main():
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    os.makedirs(TILESETS_DIR, exist_ok=True)
    os.makedirs(FISHING_TILES_DIR, exist_ok=True)
    os.makedirs(MAPS_DIR, exist_ok=True)

    # 1. Generate individual tile images.
    tiles = generate_individual_tiles()
    print(f"Tile images:   {FISHING_TILES_DIR}/ ({len(tiles)} tiles, {TILE_SIZE}x{TILE_SIZE} each)")

    # Also save a 4x atlas preview (for reference only, not used at runtime).
    atlas = generate_preview_atlas(tiles)
    preview = atlas.resize((atlas.width * 4, atlas.height * 4), Image.NEAREST)
    preview_path = os.path.join(OUTPUT_DIR, "fishing-tiles-4x.png")
    preview.save(preview_path)
    print(f"4x preview:    {preview_path}")

    # 2. Generate TSX (collection-of-images format).
    tsx_path = os.path.join(TILESETS_DIR, "FishingTiles.tsx")
    with open(tsx_path, "w", encoding="utf-8") as f:
        f.write(generate_tsx())
    print(f"Tileset def:   {tsx_path}")

    # 3. Generate TMX.
    tmx_path = os.path.join(MAPS_DIR, "FishingSpot.tmx")
    with open(tmx_path, "w", encoding="utf-8") as f:
        f.write(generate_tmx())
    print(f"Map file:      {tmx_path}")

    # 4. Also save a rendered preview of the full map.
    preview_map = Image.new("RGBA", (MAP_WIDTH * TILE_SIZE, MAP_HEIGHT * TILE_SIZE), (0, 0, 0, 255))
    bg = build_background_layer()
    shore = build_shore_layer()
    details = build_details_layer()

    for layer_data in [bg, shore, details]:
        for row in range(MAP_HEIGHT):
            for col in range(MAP_WIDTH):
                tile_gid = layer_data[row][col]
                if tile_gid == 0:
                    continue
                local_id = tile_gid - 1
                if local_id in tiles:
                    tile_img = tiles[local_id]
                    dest_x = col * TILE_SIZE
                    dest_y = row * TILE_SIZE
                    preview_map.paste(tile_img, (dest_x, dest_y), tile_img)

    map_preview_path = os.path.join(OUTPUT_DIR, "fishing-spot-preview.png")
    preview_map.save(map_preview_path)
    print(f"Map preview:   {map_preview_path}")

    scaled = preview_map.resize((preview_map.width * 3, preview_map.height * 3), Image.NEAREST)
    scaled_path = os.path.join(OUTPUT_DIR, "fishing-spot-preview-3x.png")
    scaled.save(scaled_path)
    print(f"3x map:        {scaled_path}")

    # 5. Remove old single-image atlas if present.
    old_atlas = os.path.join(TILESETS_DIR, "fishing-tiles.png")
    if os.path.exists(old_atlas):
        os.remove(old_atlas)
        print(f"Removed old:   {old_atlas}")


if __name__ == "__main__":
    main()
