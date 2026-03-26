import struct
import zlib
from pathlib import Path


OUTPUT_PATH = Path(__file__).resolve().parents[2] / "src" / "RiverRats.Game" / "Content" / "Sprites" / "projectile-arrow.png"
WIDTH = 12
HEIGHT = 12
TRANSPARENT = (0, 0, 0, 0)
SHAFT = (120, 78, 42, 255)
FLETCHING = (230, 233, 238, 255)
HEAD = (94, 103, 115, 255)
OUTLINE = (30, 24, 18, 255)


def build_pixels():
    pixels = [[TRANSPARENT for _ in range(WIDTH)] for _ in range(HEIGHT)]

    def put(x, y, color):
        if 0 <= x < WIDTH and 0 <= y < HEIGHT:
            pixels[y][x] = color

    # Fletching (tail feathers) — V-shape at the left end
    put(0, 3, FLETCHING)
    put(0, 8, FLETCHING)
    put(1, 4, FLETCHING)
    put(1, 7, FLETCHING)

    # Shaft — 2 px tall, centered at y=5,6
    for x in range(2, 7):
        put(x, 5, SHAFT)
        put(x, 6, SHAFT)

    # Arrowhead — right-pointing triangle, wide at base, tapering to tip
    for y in range(3, 9):   # x=7: 6 px tall (base)
        put(7, y, HEAD)
    for y in range(4, 8):   # x=8: 4 px tall
        put(8, y, HEAD)
    put(9, 5, HEAD)          # x=9: 2 px tall (tip)
    put(9, 6, HEAD)

    outline_offsets = (
        (-1, 0),
        (1, 0),
        (0, -1),
        (0, 1),
    )

    filled = [(x, y) for y in range(HEIGHT) for x in range(WIDTH) if pixels[y][x][3] > 0]
    for x, y in filled:
        for dx, dy in outline_offsets:
            nx = x + dx
            ny = y + dy
            if 0 <= nx < WIDTH and 0 <= ny < HEIGHT and pixels[ny][nx][3] == 0:
                pixels[ny][nx] = OUTLINE

    return pixels


def png_chunk(chunk_type, data):
    return (
        struct.pack(">I", len(data))
        + chunk_type
        + data
        + struct.pack(">I", zlib.crc32(chunk_type + data) & 0xFFFFFFFF)
    )


def write_png(path, pixels):
    raw = bytearray()
    for row in pixels:
        raw.append(0)
        for r, g, b, a in row:
            raw.extend((r, g, b, a))

    ihdr = struct.pack(">IIBBBBB", WIDTH, HEIGHT, 8, 6, 0, 0, 0)
    compressed = zlib.compress(bytes(raw), level=9)

    png = bytearray(b"\x89PNG\r\n\x1a\n")
    png.extend(png_chunk(b"IHDR", ihdr))
    png.extend(png_chunk(b"IDAT", compressed))
    png.extend(png_chunk(b"IEND", b""))

    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(png)


def main():
    write_png(OUTPUT_PATH, build_pixels())
    print(f"Wrote {OUTPUT_PATH}")


if __name__ == "__main__":
    main()