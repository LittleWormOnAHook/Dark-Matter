#!/usr/bin/env python3
"""
Unified Io full-moon world map generator (merged PR #13 + #14).

Outputs:
  - ArtReference/WorldMap/ — 4K top, iso, biome mask, height, legend
  - Textures/WorldMap/ — 2048 top + iso for UI/design
  - Resources/UI/GenesisMoonMap.png — runtime map texture

See: Io_Genesis_World_Map_Geography.md, Io_World_Map_Geography_Plan.md
"""

from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]  # Assets/_Project
OUT_ART = ROOT / "Documentation/Design/ArtReference/WorldMap"
OUT_TEXTURES = ROOT / "Textures/WorldMap"
OUT_RESOURCES = ROOT / "Resources/UI"

SIZE_4K = 4096
SIZE_UI = 2048
LEGEND_SIZE = (1536, 1024)

# Biome colors — matches IoWorldMapPalette.cs
BIOMES = {
    0: ("Void", "#1A1A1F"),
    1: ("B1 Sulfur Plains (hot)", "#C9A227"),
    2: ("B2 Geyser Fields (hot)", "#D4B04A"),
    3: ("B3 Ash Flats", "#8B7355"),
    4: ("B4 Lava Calderas (extreme heat)", "#2A1818"),
    5: ("B5 Polar Flats (cold)", "#6B8CAE"),
    6: ("B6 Highlands (hub)", "#4A4540"),
    7: ("B7 Ruin Belt (cold)", "#2A6B6B"),
}

HOT_TINT = np.array([255, 107, 53], dtype=np.float32) / 255.0
COLD_TINT = np.array([74, 111, 165], dtype=np.float32) / 255.0
GRAVE_TINT = np.array([92, 74, 58], dtype=np.float32) / 255.0

COMMAND_CENTER_UV = (0.50, 0.22)


@dataclass
class BiomeSeed:
    id: int
    uv: tuple[float, float]
    radius: float


# Sub-Jovian (low v) = HOT · Anti-Jovian (high v) = COLD · Poles = B5 cold
BIOME_SEEDS = [
    BiomeSeed(6, (0.50, 0.22), 0.14),  # B6 hub — Command Center
    BiomeSeed(4, (0.50, 0.12), 0.10),  # B4 calderas — hottest
    BiomeSeed(2, (0.42, 0.18), 0.09),
    BiomeSeed(2, (0.58, 0.16), 0.08),
    BiomeSeed(1, (0.30, 0.35), 0.16),
    BiomeSeed(1, (0.70, 0.30), 0.14),
    BiomeSeed(3, (0.25, 0.45), 0.12),
    BiomeSeed(3, (0.75, 0.42), 0.11),
    BiomeSeed(5, (0.50, 0.92), 0.11),  # north polar — cold
    BiomeSeed(5, (0.50, 0.08), 0.10),  # south polar — cold
    BiomeSeed(7, (0.50, 0.78), 0.13),  # anti-Jovian ruin belt — cold
]

BREACH_UVS = [
    (0.50, 0.22, "B6 hub tube"),
    (0.50, 0.12, "B4 collapse"),
    (0.30, 0.35, "B1 seep"),
    (0.25, 0.45, "B3 foothill"),
    (0.50, 0.92, "B5 rad tube"),
    (0.50, 0.78, "B7 vault"),
]


def hex_to_rgb(hex_color: str) -> np.ndarray:
    h = hex_color.lstrip("#")
    return np.array([int(h[i : i + 2], 16) for i in (0, 2, 4)], dtype=np.float32) / 255.0


def hex_to_rgb_u8(hex_color: str) -> tuple[int, int, int]:
    h = hex_color.lstrip("#")
    return int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16)


def make_uv_grid(size: int) -> tuple[np.ndarray, np.ndarray, np.ndarray]:
    y, x = np.mgrid[0:size, 0:size]
    u = x / (size - 1)
    v = y / (size - 1)
    v = 1.0 - v  # bottom = sub-Jovian (hot), top = anti-Jovian (cold)
    cx, cy = 0.5, 0.5
    dist = np.sqrt((u - cx) ** 2 + (v - cy) ** 2)
    mask = dist <= 0.48
    return u, v, mask


def fractal_noise(u: np.ndarray, v: np.ndarray, octaves: int = 6) -> np.ndarray:
    rng = np.random.default_rng(2160)
    result = np.zeros_like(u, dtype=np.float32)
    amplitude = 1.0
    frequency = 3.0
    total_amp = 0.0
    for _ in range(octaves):
        sx = np.sin((u * frequency + rng.random()) * 12.9898) * 43758.5453
        sy = np.sin((v * frequency + rng.random()) * 78.233) * 43758.5453
        layer = (np.sin(sx) * np.cos(sy) * 0.5 + 0.5).astype(np.float32)
        result += layer * amplitude
        total_amp += amplitude
        amplitude *= 0.5
        frequency *= 2.1
    return result / total_amp


def ridge_noise(u: np.ndarray, v: np.ndarray) -> np.ndarray:
    n = fractal_noise(u, v, octaves=5)
    return 1.0 - np.abs(n * 2.0 - 1.0)


def build_biome_field(u: np.ndarray, v: np.ndarray, mask: np.ndarray) -> np.ndarray:
    warp = fractal_noise(u * 2.5, v * 2.5, octaves=3) * 0.06
    uw = np.clip(u + warp, 0, 1)
    vw = np.clip(v + fractal_noise(v * 2.5, u * 2.5, octaves=3) * 0.06, 0, 1)

    size = u.shape[0]
    weights = np.zeros((size, size, 8), dtype=np.float32)
    for seed in BIOME_SEEDS:
        du = uw - seed.uv[0]
        dv = vw - seed.uv[1]
        d = np.sqrt(du * du + dv * dv)
        w = np.exp(-(d / seed.radius) ** 2 * 3.2)
        weights[:, :, seed.id] += w

    biome = np.argmax(weights, axis=2).astype(np.uint8)
    max_w = weights.max(axis=2)
    biome[max_w < 0.05] = 0
    biome[~mask] = 0
    return biome


def apply_thermal_tint(rgb: np.ndarray, u: np.ndarray, v: np.ndarray, mask: np.ndarray) -> np.ndarray:
    """Sub-Jovian (low v) = hot tint; anti-Jovian (high v) = cold tint; poles extra cold."""
    hot = np.clip(1.0 - v * 1.45, 0, 1)[:, :, np.newaxis]
    cold = np.clip((v - 0.42) * 2.3, 0, 1)[:, :, np.newaxis]
    pole = np.clip(np.maximum(1.0 - v * 2.2, v * 2.2 - 0.72), 0, 1)[:, :, np.newaxis]

    out = rgb.copy()
    out += hot * (HOT_TINT - 0.5) * 0.28
    out += cold * (COLD_TINT - 0.5) * 0.22
    out += pole * (COLD_TINT - 0.5) * 0.18

    grave = ((u > 0.55) & (v > 0.35) & (v < 0.65)).astype(np.float32)[:, :, np.newaxis]
    out += grave * (GRAVE_TINT - 0.5) * 0.12
    out = np.clip(out, 0, 1)
    out[~mask] = 0
    return out


def build_height_meters(u: np.ndarray, v: np.ndarray, mask: np.ndarray, biome: np.ndarray) -> np.ndarray:
    base = fractal_noise(u, v) * 120.0
    ridges = ridge_noise(u * 1.3 + 0.2, v * 1.3) * 450.0
    peaks = ridge_noise(u * 0.7, v * 0.7) ** 2 * 350.0
    height = base + ridges + peaks

    height += (biome == 3).astype(np.float32) * 180.0
    height += (biome == 6).astype(np.float32) * 120.0
    height -= (biome == 1).astype(np.float32) * 40.0
    height -= (biome == 5).astype(np.float32) * 30.0

    d4 = np.sqrt((u - 0.5) ** 2 + (v - 0.12) ** 2)
    height -= np.exp(-(d4 / 0.06) ** 2) * 200.0

    height = np.clip(height, 0, 1000.0)
    height[~mask] = 0
    return height


def height_to_shade(height: np.ndarray, mask: np.ndarray) -> np.ndarray:
    norm = height / 1000.0
    shade = 0.55 + norm * 0.35
    shade = np.clip(shade, 0, 1)
    shade[~mask] = 0
    return shade


def render_top_view(
    biome: np.ndarray,
    height: np.ndarray,
    mask: np.ndarray,
    u: np.ndarray,
    v: np.ndarray,
    size: int,
    annotate: bool = True,
) -> Image.Image:
    rgb = np.zeros((*biome.shape, 3), dtype=np.float32)
    for bid, (_, hex_c) in BIOMES.items():
        c = hex_to_rgb(hex_c)
        m = biome == bid
        rgb[m] = c

    shade = height_to_shade(height, mask)[:, :, np.newaxis]
    rgb *= shade
    rgb = apply_thermal_tint(rgb, u, v, mask)

    img = Image.fromarray((rgb * 255).astype(np.uint8), mode="RGB")
    if not annotate:
        return img

    draw = ImageDraw.Draw(img, "RGBA")
    for bu, bv, _ in BREACH_UVS:
        px = int(bu * (size - 1))
        py = int((1.0 - bv) * (size - 1))
        r = max(8, size // 280)
        draw.ellipse(
            (px - r, py - r, px + r, py + r),
            fill=(212, 160, 23, 255),
            outline=(255, 220, 100, 255),
        )

    cx = int(COMMAND_CENTER_UV[0] * (size - 1))
    cy = int((1.0 - COMMAND_CENTER_UV[1]) * (size - 1))
    cr = max(10, size // 200)
    draw.ellipse((cx - cr, cy - cr, cx + cr, cy + cr), outline=(212, 160, 23), width=max(2, size // 512))

    try:
        font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", max(18, size // 110))
        small = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", max(14, size // 140))
    except OSError:
        font = small = ImageFont.load_default()

    draw.text((size // 2 - size // 8, int(size * 0.03)), "ANTI-JOVIAN (dark / cold)", fill=(180, 200, 230), font=font)
    draw.text((size // 2 - size // 8, size - int(size * 0.06)), "SUB-JOVIAN (light / hot)", fill=(255, 180, 120), font=font)
    draw.text((int(size * 0.02), size // 2), "LEADING", fill=(140, 140, 150), font=small)
    draw.text((size - int(size * 0.12), size // 2), "TRAILING", fill=(140, 140, 150), font=small)
    draw.text((cx + cr + 4, cy - 8), "Command Center", fill=(212, 160, 23), font=small)

    return img


def render_iso_view(biome: np.ndarray, height: np.ndarray, mask: np.ndarray, size: int) -> Image.Image:
    grid = max(140, size // 14)
    step = size // grid
    sky = hex_to_rgb_u8("#0D1218")
    img = Image.new("RGB", (size, size), sky)
    draw = ImageDraw.Draw(img)

    tile_w = max(6, size // grid // 2)
    tile_h = max(3, tile_w // 2)
    origin_x = size // 2
    origin_y = int(size * 0.18)
    h_scale = size / 1000.0 * 0.35

    h_grid = np.zeros((grid, grid), dtype=np.float32)
    b_grid = np.zeros((grid, grid), dtype=np.uint8)
    m_grid = np.zeros((grid, grid), dtype=bool)
    for gi in range(grid):
        for gj in range(grid):
            px = min(gi * step + step // 2, size - 1)
            py = min(gj * step + step // 2, size - 1)
            h_grid[gi, gj] = height[py, px]
            b_grid[gi, gj] = biome[py, px]
            m_grid[gi, gj] = mask[py, px]

    def tile_color(bid: int, hh: float) -> tuple[int, int, int]:
        c = hex_to_rgb(BIOMES.get(int(bid), ("", "#333333"))[1])
        lit = 0.45 + 0.55 * (hh / 1000.0)
        return tuple(int(np.clip(ch * lit, 0, 1) * 255) for ch in c)

    order = [(gi, gj) for gj in range(grid) for gi in range(grid)]
    order.sort(key=lambda t: (t[0] + t[1], t[0]), reverse=True)

    for gi, gj in order:
        if not m_grid[gi, gj]:
            continue
        hh = h_grid[gi, gj]
        bb = b_grid[gi, gj]
        sx = origin_x + (gi - gj) * tile_w
        sy = origin_y + (gi + gj) * tile_h - int(hh * h_scale)
        top_c = tile_color(bb, hh)
        side_c = tuple(max(0, c - 35) for c in top_c)
        depth_c = tuple(max(0, c - 70) for c in top_c)
        extrude = max(2, int(hh / 200.0))

        draw.polygon(
            [(sx, sy - tile_h), (sx + tile_w, sy), (sx, sy + tile_h), (sx - tile_w, sy)],
            fill=top_c,
        )
        draw.polygon(
            [(sx - tile_w, sy), (sx, sy + tile_h), (sx, sy + tile_h + extrude), (sx - tile_w, sy + extrude)],
            fill=side_c,
        )
        draw.polygon(
            [(sx + tile_w, sy), (sx, sy + tile_h), (sx, sy + tile_h + extrude), (sx + tile_w, sy + extrude)],
            fill=depth_c,
        )

    glow = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    gdraw = ImageDraw.Draw(glow)
    gdraw.rectangle((0, 0, size, int(size * 0.12)), fill=(74, 111, 165, 70))
    gdraw.rectangle((0, int(size * 0.88), size, size), fill=(255, 107, 53, 85))
    img = Image.alpha_composite(img.convert("RGBA"), glow).convert("RGB")

    try:
        font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", max(22, size // 95))
    except OSError:
        font = ImageFont.load_default()
    draw = ImageDraw.Draw(img)
    draw.text((size // 2 - size // 6, size - int(size * 0.05)), "Io — Isometric (hot south / cold north)", fill=(220, 220, 230), font=font)
    return img


def render_legend() -> Image.Image:
    img = Image.new("RGB", LEGEND_SIZE, hex_to_rgb_u8("#1C2A38"))
    draw = ImageDraw.Draw(img)
    try:
        title_font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 32)
        font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 22)
    except OSError:
        title_font = font = ImageFont.load_default()

    draw.text((40, 30), "Io World Map — Biome Legend", fill=(237, 233, 228), font=title_font)
    y = 90
    for bid in range(1, 8):
        name, hex_c = BIOMES[bid]
        c = tuple(int(x * 255) for x in hex_to_rgb(hex_c))
        draw.rectangle((40, y, 90, y + 36), fill=c)
        draw.text((105, y + 4), name, fill=(237, 233, 228), font=font)
        y += 48

    draw.text((40, y + 20), "Hot: B4 calderas, B2 geysers, B1 sulfur (sub-Jovian)", fill=(255, 180, 120), font=font)
    draw.text((40, y + 55), "Cold: B5 polar caps, B7 ruin belt (anti-Jovian)", fill=(180, 200, 230), font=font)
    draw.text((40, y + 90), "Elevation: valleys 0 m · ridges 350–700 m · peaks ≤ 1000 m", fill=(180, 170, 160), font=font)
    draw.text((40, y + 125), "Gold dots = reserved underground breach mouths (W1 terrain holes)", fill=(212, 160, 23), font=font)
    return img


def downscale(img: Image.Image, target: int) -> Image.Image:
    if img.width == target:
        return img
    return img.resize((target, target), Image.Resampling.LANCZOS)


def main() -> None:
    OUT_ART.mkdir(parents=True, exist_ok=True)
    OUT_TEXTURES.mkdir(parents=True, exist_ok=True)
    OUT_RESOURCES.mkdir(parents=True, exist_ok=True)

    print(f"Generating Io world maps at {SIZE_4K}px...")
    u_4k, v_4k, mask_4k = make_uv_grid(SIZE_4K)
    biome_4k = build_biome_field(u_4k, v_4k, mask_4k)
    height_4k = build_height_meters(u_4k, v_4k, mask_4k, biome_4k)
    top_4k = render_top_view(biome_4k, height_4k, mask_4k, u_4k, v_4k, SIZE_4K, annotate=True)
    iso_4k = render_iso_view(biome_4k, height_4k, mask_4k, SIZE_4K)

    top_4k.save(OUT_ART / "IoWorldMap_TopView_4K.png", optimize=True)
    Image.fromarray(biome_4k, mode="L").save(OUT_ART / "IoWorldMap_TopView_4K_BiomeMask.png")
    height_norm = (height_4k / 1000.0 * 255).astype(np.uint8)
    Image.fromarray(height_norm, mode="L").save(OUT_ART / "IoWorldMap_TopView_4K_Height.png")
    iso_4k.save(OUT_ART / "IoWorldMap_IsoView_4K.png", optimize=True)
    render_legend().save(OUT_ART / "IoWorldMap_Legend.png")

    print(f"Downscaling to {SIZE_UI}px for UI textures...")
    top_ui = downscale(top_4k, SIZE_UI)
    iso_ui = downscale(iso_4k, SIZE_UI)
    top_ui.save(OUT_TEXTURES / "GenesisMoonMap_TopDown.png", optimize=True)
    iso_ui.save(OUT_TEXTURES / "GenesisMoonMap_Isometric.png", optimize=True)
    top_plain_4k = render_top_view(biome_4k, height_4k, mask_4k, u_4k, v_4k, SIZE_4K, annotate=False)
    downscale(top_plain_4k, SIZE_UI).save(OUT_RESOURCES / "GenesisMoonMap.png", optimize=True)

    print("Done.")
    for p in [
        OUT_ART / "IoWorldMap_TopView_4K.png",
        OUT_ART / "IoWorldMap_IsoView_4K.png",
        OUT_TEXTURES / "GenesisMoonMap_TopDown.png",
        OUT_TEXTURES / "GenesisMoonMap_Isometric.png",
        OUT_RESOURCES / "GenesisMoonMap.png",
    ]:
        print(f"  {p}")


if __name__ == "__main__":
    main()
