"""
Generate a soft smoke puff texture for particles.
Produces a 32x32 PNG with a soft, noisy circular alpha mask.

Usage:
    python tooling/sprites/generate_smoke.py

Output:
    src/DogDays.Game/Content/Sprites/smoke-puff.png
"""

import math
import os
import random
from PIL import Image

SIZE = 32
OUTPUT_PATH = "src/DogDays.Game/Content/Sprites/smoke-puff.png"

def create_smoke_texture():
    os.makedirs(os.path.dirname(OUTPUT_PATH), exist_ok=True)
    
    img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    pixels = img.load()
    
    center = SIZE / 2
    max_radius = SIZE / 2 - 2
    
    for y in range(SIZE):
        for x in range(SIZE):
            dx = x - center
            dy = y - center
            dist = math.sqrt(dx*dx + dy*dy)
            
            if dist < max_radius:
                strength = 1.0 - (dist / max_radius)
                strength = strength * strength  # Softer edges
                
                noise = random.uniform(0.7, 1.0)
                alpha = int(220 * strength * noise)
                
                pixels[x, y] = (255, 255, 255, alpha)
                
    img.save(OUTPUT_PATH)
    print(f"Generated smoke texture at {OUTPUT_PATH}")

if __name__ == "__main__":
    create_smoke_texture()
