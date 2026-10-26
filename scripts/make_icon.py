"""Draws thunderstore/icon.png (256x256). Run with Python + Pillow: python3 scripts/make_icon.py"""
import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter

S = 1024  # draw large, downsample for smooth edges
OUT = Path(__file__).resolve().parent.parent / "thunderstore" / "icon.png"


def bezier(p0, p1, p2, n=40):
    return [((1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t * t * p2[0],
             (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t * t * p2[1])
            for t in (i / n for i in range(n + 1))]


def blade(side):
    """Crescent blade on one side of the haft (side = -1 left, +1 right), haft at x=512."""
    def X(dx): return 512 + side * dx
    pts = bezier((X(30), 230), (X(170), 100), (X(340), 90))           # top edge, flaring out to the upper tip
    pts += bezier((X(340), 90), (X(210), 335), (X(340), 580))[1:]     # concave cutting edge
    pts += bezier((X(340), 580), (X(170), 570), (X(30), 440))[1:]     # bottom edge back to the haft
    return pts


def gradient(size, top, bottom):
    img = Image.new("RGB", size)
    d = ImageDraw.Draw(img)
    for y in range(size[1]):
        t = y / (size[1] - 1)
        d.line([(0, y), (size[0], y)], fill=tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3)))
    return img


# --- axe, drawn upright on a transparent canvas ---
axe = Image.new("RGBA", (S, S), (0, 0, 0, 0))
d = ImageDraw.Draw(axe)

# haft
d.rounded_rectangle((484, 120, 540, 960), 24, fill=(110, 72, 40, 255), outline=(60, 38, 20, 255), width=8)
d.rectangle((496, 140, 506, 940), fill=(140, 96, 56, 255))
for y in range(260, 900, 70):  # leather wrap
    d.line([(484, y), (540, y + 26)], fill=(70, 44, 24, 255), width=10)

# bronze blades
mask = Image.new("L", (S, S), 0)
md = ImageDraw.Draw(mask)
for side in (-1, 1):
    md.polygon(blade(side), fill=255)
md.rounded_rectangle((470, 200, 554, 470), 30, fill=255)  # socket
bronze = gradient((S, S), (236, 170, 84), (150, 84, 30)).convert("RGBA")
axe.paste(bronze, (0, 0), mask)

d = ImageDraw.Draw(axe)
edge = (74, 40, 14, 255)
for side in (-1, 1):
    d.line(blade(side) + [blade(side)[0]], fill=edge, width=10, joint="curve")
    # highlight along the cutting edge
    pts = bezier((512 + side * 318, 120), (512 + side * 195, 335), (512 + side * 318, 550))
    d.line(pts, fill=(255, 226, 160, 255), width=8)
d.rounded_rectangle((470, 200, 554, 470), 30, outline=edge, width=10)
d.ellipse((492, 315, 532, 355), fill=(90, 50, 20, 255))  # rivet

axe = axe.rotate(35, resample=Image.BICUBIC, center=(512, 540))
size = int(S * 0.74)  # leave a margin so the blade tips don't touch the border
small = axe.resize((size, size), Image.LANCZOS)
axe = Image.new("RGBA", (S, S), (0, 0, 0, 0))
axe.paste(small, ((S - size) // 2, (S - size) // 2 + 20))

# --- background tile ---
bg = gradient((S, S), (58, 50, 44), (28, 24, 22)).convert("RGBA")
corner = Image.new("L", (S, S), 0)
ImageDraw.Draw(corner).rounded_rectangle((24, 24, S - 24, S - 24), 150, fill=255)
tile = Image.new("RGBA", (S, S), (0, 0, 0, 0))
tile.paste(bg, (0, 0), corner)

shadow = Image.new("RGBA", (S, S), (0, 0, 0, 0))
shadow.paste((0, 0, 0, 170), (14, 20), axe.split()[3])
shadow = shadow.filter(ImageFilter.GaussianBlur(14))
tile = Image.alpha_composite(tile, shadow)
tile = Image.alpha_composite(tile, axe)

tile.resize((256, 256), Image.LANCZOS).save(OUT)
print("wrote", OUT)
