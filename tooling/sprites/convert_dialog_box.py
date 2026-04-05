"""Generate a clean pixel-art 9-slice dialog box texture.

The original JPG was too large and blurry when downscaled.  This script draws
a crisp 24x24 pixel-art frame from scratch:

  Layout (corner = 6px):
    ┌──────┬──────────┬──────┐
    │  TL  │ top edge │  TR  │  6 px
    ├──────┼──────────┼──────┤
    │  L   │  center  │  R   │  12 px (stretch)
    ├──────┼──────────┼──────┤
    │  BL  │ bot edge │  BR  │  6 px
    └──────┴──────────┴──────┘
     6 px    12 px      6 px

  Palette (warm parchment/wood to match the cabin):
    - Outer border:  #3A2A1A  (dark brown)
    - Inner border:  #7B5B3A  (medium wood)
    - Highlight:     #C89E6E  (warm tan)
    - Fill:          #DCC8A0  (parchment)

Output: Content/Sprites/dialog_box_9slice.png (24x24, RGBA)
"""
from PIL import Image
import numpy as np
import os

SIZE = 24
CORNER = 6

# Palette
DARK    = (58,  42,  26, 255)   # outer border
MEDIUM  = (123, 91,  58, 255)   # inner border / frame
HIGHLIGHT = (200, 158, 110, 255) # bevel highlight
FILL    = (220, 200, 160, 255)  # parchment interior

arr = np.zeros((SIZE, SIZE, 4), dtype=np.uint8)

# 1. Fill everything with DARK (outermost border)
arr[:, :] = DARK

# 2. MEDIUM frame inset by 1px
arr[1:-1, 1:-1] = MEDIUM

# 3. Highlight strip inset by 2px (top + left bevel)
arr[2:-2, 2:-2] = HIGHLIGHT

# 4. MEDIUM inner frame at 3px inset (shadow side)
arr[3:-2, 3:-2] = MEDIUM

# 5. Fill interior from 3px inset
arr[3:-3, 3:-3] = FILL

# 6. Add a subtle inner highlight at the top/left of the fill
arr[3, 3:-3] = HIGHLIGHT
arr[3:-3, 3] = HIGHLIGHT

# 7. Round the outer corners by clearing the very corner pixels to transparent
corners = [(0, 0), (0, SIZE-1), (SIZE-1, 0), (SIZE-1, SIZE-1)]
for y, x in corners:
    arr[y, x] = (0, 0, 0, 0)

img = Image.fromarray(arr)
dst = os.path.join(
    os.path.dirname(__file__),
    "..", "..", "src", "RiverRats.Game", "Content", "Sprites",
    "dialog_box_9slice.png",
)
img.save(dst)
print(f"Saved {img.size[0]}x{img.size[1]} to {os.path.abspath(dst)}")

