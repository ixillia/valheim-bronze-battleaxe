"""
Fits the CC-BY "Glass Battleaxe, 1958 Tris, Low Poly" model (leonsavelind, https://skfb.ly/6uKOp) to Valheim's
battleaxe and writes the mod's model + textures to assets/.

    python3 scripts/fit_model.py <downloaded model folder> [--preview preview.png]

Needs Python 3, Pillow and numpy. The downloaded model itself is not stored in the repo.

Geometry: rotated so the shaft runs along +Y with the blade toward +X, scaled to the iron Battleaxe's length and
moved so the origin sits where the iron Battleaxe's does (the grip). That keeps Valheim's own equip transform,
hand position and attack animations working unchanged.
Textures: glass-green blade -> bronze, dark frame -> dark bronze, shaft wood kept, AO baked in.
"""
import argparse
import math
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "assets"

# Valheim's iron Battleaxe mesh space (read from the game's data): shaft along Y, origin at the grip.
TARGET_BUTT_Y = -0.908
TARGET_TOP_Y = 4.237
TEXTURE_SIZE = 1024


def load_obj(path):
    v, vt, vn, faces = [], [], [], []
    for line in open(path, errors="ignore"):
        p = line.split()
        if not p:
            continue
        if p[0] == "v":
            v.append([float(x) for x in p[1:4]])
        elif p[0] == "vt":
            vt.append([float(x) for x in p[1:3]])
        elif p[0] == "vn":
            vn.append([float(x) for x in p[1:4]])
        elif p[0] == "f":
            corners = []
            for c in p[1:]:
                idx = c.split("/")
                corners.append((int(idx[0]) - 1, int(idx[1]) - 1 if len(idx) > 1 and idx[1] else -1,
                                int(idx[2]) - 1 if len(idx) > 2 and idx[2] else -1))
            for i in range(1, len(corners) - 1):  # triangulate the fan
                faces.append((corners[0], corners[i], corners[i + 1]))
    return np.array(v), np.array(vt), np.array(vn), faces


def fit(v, vn):
    centre = v.mean(axis=0)
    # Principal axis of the whole axe, then which end is the head (wider spread across the axis).
    _, _, vh = np.linalg.svd(v[:, :2] - centre[:2], full_matrices=False)
    axis = vh[0]
    t = (v[:, :2] - centre[:2]) @ axis
    lo, hi = t.min(), t.max()
    lateral = np.abs((v[:, :2] - centre[:2]) @ np.array([-axis[1], axis[0]]))
    low_end = lateral[t < lo + 0.25 * (hi - lo)].mean()
    high_end = lateral[t > hi - 0.25 * (hi - lo)].mean()
    if high_end < low_end:  # make +axis point from butt to head
        axis = -axis
        t = -t
        lo, hi = t.min(), t.max()

    # Refine the shaft direction from the butt-side half, which is shaft only.
    shaft = v[t < lo + 0.5 * (hi - lo)][:, :2]
    _, _, vh2 = np.linalg.svd(shaft - shaft.mean(axis=0), full_matrices=False)
    refined = vh2[0] if vh2[0] @ axis > 0 else -vh2[0]

    angle = math.atan2(refined[0], refined[1])  # rotate about Z so `refined` -> +Y
    c, s = math.cos(angle), math.sin(angle)
    rot = np.array([[c, -s, 0], [s, c, 0], [0, 0, 1]])
    v2 = v @ rot.T
    n2 = vn @ rot.T

    # Blade toward +X (Valheim's battleaxe head sticks out toward +X).
    head = v2[v2[:, 1] > v2[:, 1].min() + 0.7 * np.ptp(v2[:, 1])]
    shaft_x = v2[v2[:, 1] < v2[:, 1].min() + 0.3 * np.ptp(v2[:, 1])][:, 0].mean()
    if head[:, 0].mean() < shaft_x:
        flip = np.diag([-1.0, 1.0, -1.0])  # 180 degrees about Y: a proper rotation, winding stays valid
        v2, n2 = v2 @ flip.T, n2 @ flip.T

    shaft_x = v2[v2[:, 1] < v2[:, 1].min() + 0.3 * np.ptp(v2[:, 1])][:, 0].mean()
    scale = (TARGET_TOP_Y - TARGET_BUTT_Y) / np.ptp(v2[:, 1])
    v2 = v2 - np.array([shaft_x, v2[:, 1].min(), v2[:, 2].mean()])
    v2 = v2 * scale + np.array([0.0, TARGET_BUTT_Y, 0.0])
    n2 = n2 / np.linalg.norm(n2, axis=1, keepdims=True)
    return v2, n2, math.degrees(angle), scale


def write_obj(path, v, vt, vn, faces):
    # Re-index so every (position, uv, normal) combination is one vertex: simple for the plugin to load.
    index, positions, uvs, normals, tris = {}, [], [], [], []
    for tri in faces:
        for key in tri:
            if key not in index:
                index[key] = len(positions)
                positions.append(v[key[0]])
                uvs.append(vt[key[1]])
                normals.append(vn[key[2]])
            tris.append(index[key])
    with open(path, "w") as f:
        f.write("# Bronze Battleaxe model, fitted by scripts/fit_model.py\n")
        f.write("# Based on 'Glass Battleaxe, 1958 Tris, Low Poly' by leonsavelind (https://skfb.ly/6uKOp), CC BY 4.0\n")
        for p in positions:
            f.write("v %.5f %.5f %.5f\n" % tuple(p))
        for u in uvs:
            f.write("vt %.5f %.5f\n" % tuple(u))
        for n in normals:
            f.write("vn %.5f %.5f %.5f\n" % tuple(n))
        for i in range(0, len(tris), 3):
            a, b, c = (t + 1 for t in tris[i:i + 3])
            f.write("f %d/%d/%d %d/%d/%d %d/%d/%d\n" % (a, a, a, b, b, b, c, c, c))
    return len(positions), len(tris) // 3


def write_trail(path, v, faces):
    """
    Anchor points for the swing trail (the vanilla 'equiped/trail/base' and 'tip' transforms), in the prefab's
    'attach' space. The prefab maps mesh space to attach space with a 180 degree turn and 0.331 scale, which for
    an OBJ-convention mesh (+X blade) works out to attach = 0.331 * (x, z, y).
    """
    used = np.unique([c[0] for tri in faces for c in tri])
    blade = v[used][(v[used][:, 0] > 0.05) & (v[used][:, 1] > 2.8)]
    base = blade[blade[:, 1].argmin()]
    tip = blade[blade[:, 1].argmax()]
    to_attach = lambda p: (0.331 * p[0], 0.331 * p[2], 0.331 * p[1])
    with open(path, "w") as f:
        f.write("base %.4f %.4f %.4f\n" % to_attach(base))
        f.write("tip %.4f %.4f %.4f\n" % to_attach(tip))


def preview(v, faces, path):
    size = 600
    xs, ys = v[:, 0], v[:, 1]
    sc = (size - 20) / (ys.max() - ys.min())
    im = Image.new("RGB", (size, size), (30, 30, 30))
    d = ImageDraw.Draw(im)
    for tri in faces:
        poly = [((v[c[0]][0] - xs.min() + 0.3) * sc + 10, size - ((v[c[0]][1] - ys.min()) * sc + 10)) for c in tri]
        d.polygon(poly, fill=(176, 108, 48), outline=(240, 190, 110))
    # grip origin and the iron battleaxe's extent, for reference
    gy = size - ((0 - ys.min()) * sc + 10)
    d.line([(0, gy), (size, gy)], fill=(80, 200, 80))
    im.save(path)


# --- textures ---------------------------------------------------------------------------------------------

BRONZE_RAMP = np.array([  # luminance 0..1 -> colour (0..255)
    [0.00, 28, 14, 6],
    [0.35, 120, 66, 26],
    [0.65, 196, 122, 54],
    [1.00, 250, 206, 130],
])


WOOD_RAMP = np.array([
    [0.0, 34, 20, 10],
    [0.5, 88, 56, 30],
    [1.0, 150, 104, 60],
])


def ramp(y):
    return np.stack([np.interp(y, BRONZE_RAMP[:, 0], BRONZE_RAMP[:, i]) for i in (1, 2, 3)], axis=-1)


def make_textures(src_dir, out_dir):
    tex = Path(src_dir) / "textures"
    size = (TEXTURE_SIZE, TEXTURE_SIZE)

    def load(name, mode=None):
        im = Image.open(tex / name)
        if mode:
            im = im.convert(mode)
        return im.resize(size, Image.LANCZOS)

    albedo = np.asarray(load("Axw_galss_albedo.png", "RGB"), dtype=np.float32)
    ao = np.asarray(load("Axw_galss_ao.png", "L"), dtype=np.float32) / 255.0
    metal = np.asarray(load("Axw_galss_metallic.png", "LA"), dtype=np.float32)

    r, g, b = albedo[..., 0], albedo[..., 1], albedo[..., 2]
    lum = (0.299 * r + 0.587 * g + 0.114 * b) / 255.0
    blade = (g > r + 25) & (g > b + 8)                       # the green "glass" panels
    wood = (r > g + 8) & (r > b + 8) & (r < 130) & ~blade    # shaft
    frame = ~blade & ~wood                                   # dark frame and padding

    out = albedo.copy()
    # Blade: stretch its luminance by percentile so the facets read as bright, hammered bronze.
    lo, hi = np.percentile(lum[blade], [3, 97])
    y_blade = 0.30 + 0.48 * np.clip((lum - lo) / (hi - lo), 0, 1)
    out[blade] = ramp(y_blade[blade])
    # Frame: same metal, one step darker and duller (forged, not polished).
    flo, fhi = np.percentile(lum[frame], [3, 97])
    y_frame = 0.12 + 0.46 * np.clip((lum - flo) / (fhi - flo), 0, 1)
    out[frame] = ramp(y_frame[frame])
    # Shaft: re-map to warm brown wood / leather.
    wlo, whi = np.percentile(lum[wood], [3, 97])
    y_wood = np.clip((lum - wlo) / (whi - wlo), 0, 1)
    out[wood] = np.stack([np.interp(y_wood[wood], WOOD_RAMP[:, 0], WOOD_RAMP[:, i]) for i in (1, 2, 3)], axis=-1)

    out *= (0.55 + 0.45 * ao)[..., None]  # Valheim's material has no occlusion slot: bake it in
    Image.fromarray(np.clip(out, 0, 255).astype(np.uint8), "RGB").save(out_dir / "bronze_battleaxe_albedo.png", optimize=True)

    # Valheim's _MetallicGlossMap: R = metallic, A = smoothness. Bronze is metal almost everywhere except the wood.
    metallic = np.where(wood, 0.0, np.clip(metal[..., 0] / 255.0 * 0.5 + 0.55, 0, 1))
    smooth = np.where(wood, 0.25, np.clip(metal[..., 1] / 255.0 * 0.7, 0.2, 0.85))
    m = np.stack([metallic, metallic, metallic, smooth], axis=-1) * 255
    Image.fromarray(m.astype(np.uint8), "RGBA").save(out_dir / "bronze_battleaxe_metallic.png", optimize=True)

    # Normal map: plain tangent-space RGB (OpenGL / +Y, as xNormal writes it), alpha opaque so Unity's
    # RG-or-AG unpacking reads it correctly either way.
    n = load("Axw_galss_normal.png", "RGB")
    n.convert("RGBA").save(out_dir / "bronze_battleaxe_normal.png", optimize=True)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("model_dir", help="folder of the downloaded model (contains source/ and textures/)")
    ap.add_argument("--preview", help="write a front-view silhouette PNG here")
    args = ap.parse_args()

    obj = next(Path(args.model_dir).glob("source/**/*.obj"))
    v, vt, vn, faces = load_obj(obj)
    v2, n2, angle, scale = fit(v, vn)
    OUT.mkdir(exist_ok=True)
    verts, tris = write_obj(OUT / "bronze_battleaxe.obj", v2, vt, n2, faces)
    print("rotated %.1f deg about Z, scale %.4f, %d vertices, %d triangles" % (angle, scale, verts, tris))
    print("bounds x[%.3f, %.3f] y[%.3f, %.3f] z[%.3f, %.3f]" % tuple(
        x for a in range(3) for x in (v2[:, a].min(), v2[:, a].max())))
    write_trail(OUT / "bronze_battleaxe.trail", v2, faces)
    if args.preview:
        preview(v2, faces, args.preview)
    make_textures(args.model_dir, OUT)
    print("wrote", OUT)


if __name__ == "__main__":
    main()
