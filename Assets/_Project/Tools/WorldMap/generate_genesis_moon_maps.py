#!/usr/bin/env python3
"""Generate Io Genesis moon map placeholder art (top-down + isometric).

See: Assets/_Project/Documentation/Design/Io_Genesis_World_Map_Geography.md
"""

from __future__ import annotations

import math
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]  # Assets/_Project
OUT_TEXTURES = ROOT / "Textures" / "WorldMap"
OUT_RESOURCES = ROOT / "Resources" / "UI"

RESOLUTION = 2048
SEED = 2160

# Biome palette tokens (RGB 0-255)
PALETTES = {
    "sulfur-amber": {"base": (139, 105, 20), "accent": (212, 160, 23)},
    "ash-bronze": {"base": (74, 64, 56), "accent": (140, 115, 98)},
    "heat-obsidian": {"base": (26, 18, 18), "accent": (92, 46, 46)},
    "polar-rad": {"base": (42, 48, 64), "accent": (107, 127, 168)},
    "aether-teal": {"base": (30, 74, 74), "accent": (61, 139, 139)},
    "vacuum": {"base": (10, 10, 18), "accent": (10, 10, 18)},
}

# (name, u, v, radius, palette, elevation_bias)
BIOMES = [
    ("B6 Basalt Highlands", 0.52, 0.58, 0.18, "ash-bronze", 0.55),
    ("B1 Sulfur Plains", 0.72, 0.42, 0.16, "sulfur-amber", 0.12),
    ("B2 Geyser Fields", 0.78, 0.62, 0.14, "sulfur-amber", 0.18),
    ("B3 Ash Flats & Ridges", 0.48, 0.28, 0.17, "ash-bronze", 0.35),
    ("B4 Lava Calderas", 0.68, 0.52, 0.13, "heat-obsidian", 0.48),
    ("B5 Polar Radiation Flats", 0.22, 0.50, 0.15, "polar-rad", 0.08),
    ("B7 Precursor Ruin Belt", 0.50, 0.22, 0.12, "aether-teal", 0.22),
]

COMMAND_CENTER_UV = (0.50, 0.56)


def hex_to_rgb(hex_color: str) -> tuple[int, int, int]:
    hex_color = hex_color.lstrip("#")
    return tuple(int(hex_color[i : i + 2], 16) for i in (0, 2, 4))


def smoothstep(edge0: float, edge1: float, x: np.ndarray) -> np.ndarray:
    t = np.clip((x - edge0) / (edge1 - edge0 + 1e-8), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def fbm(shape: tuple[int, int], octaves: int = 5, seed: int = SEED) -> np.ndarray:
    rng = np.random.default_rng(seed)
    height = np.zeros(shape, dtype=np.float32)
    amplitude = 1.0
    frequency = 1.0
    total = 0.0
    for _ in range(octaves):
        grid = rng.random((max(2, int(shape[0] / frequency)), max(2, int(shape[1] / frequency))))
        upsampled = np.array(
            Image.fromarray((grid * 255).astype(np.uint8)).resize((shape[1], shape[0]), Image.BILINEAR),
            dtype=np.float32,
        ) / 255.0
        height += upsampled * amplitude
        total += amplitude
        amplitude *= 0.5
        frequency *= 2.0
    return height / total


def build_uv_grids(size: int) -> tuple[np.ndarray, np.ndarray, np.ndarray]:
    y, x = np.mgrid[0:size, 0:size].astype(np.float32)
    u = x / (size - 1)
    v = 1.0 - y / (size - 1)
    cx, cy = 0.5, 0.5
    dist = np.sqrt((u - cx) ** 2 + (v - cy) ** 2)
    disc_mask = smoothstep(0.50, 0.48, dist)
    return u, v, disc_mask


def biome_weights(u: np.ndarray, v: np.ndarray, noise: np.ndarray) -> list[np.ndarray]:
    weights = []
    for _name, bu, bv, radius, _palette, _elev in BIOMES:
        du = u - bu
        dv = v - bv
        dist = np.sqrt(du * du + dv * dv)
        w = smoothstep(radius, radius * 0.35, dist)
        w *= 0.75 + 0.25 * noise
        weights.append(w)
    stack = np.stack(weights, axis=-1)
    stack = np.maximum(stack, 0.0)
    total = np.sum(stack, axis=-1, keepdims=True) + 1e-6
    return [stack[..., i] / total[..., 0] for i in range(stack.shape[-1])]


def thermal_gradient(u: np.ndarray) -> np.ndarray:
    """Sub-Jovian hot (east) → anti-Jovian cold (west)."""
    return smoothstep(0.15, 0.85, u)


def terminator_shade(u: np.ndarray, v: np.ndarray) -> np.ndarray:
    """NW cold/dark to SE hot/light bias."""
    diag = (u + v) * 0.5
    return smoothstep(0.25, 0.75, diag)


def compose_topdown(size: int = RESOLUTION) -> np.ndarray:
    u, v, disc_mask = build_uv_grids(size)
    terrain_noise = fbm((size, size), octaves=6, seed=SEED + 1)
    detail_noise = fbm((size, size), octaves=4, seed=SEED + 2)
    weights = biome_weights(u, v, terrain_noise)

    rgb = np.zeros((size, size, 3), dtype=np.float32)
    elevation = np.zeros((size, size), dtype=np.float32)

    for i, (_name, _bu, _bv, _radius, palette_name, elev_bias) in enumerate(BIOMES):
        palette = PALETTES[palette_name]
        base = np.array(palette["base"], dtype=np.float32)
        accent = np.array(palette["accent"], dtype=np.float32)
        blend = 0.35 + 0.65 * detail_noise
        color = base * (1.0 - blend[..., None]) + accent * blend[..., None]
        rgb += color * weights[i][..., None]
        elevation += weights[i] * (elev_bias + terrain_noise * (0.35 if palette_name != "heat-obsidian" else 0.55))

    # Scale elevation to 0-1 with 1000m cap represented as full white in height shade.
    elevation = np.clip(elevation, 0.0, 1.0)

    hot = thermal_gradient(u)[..., None]
    term = terminator_shade(u, v)[..., None]
    warm_tint = np.array([1.08, 1.02, 0.92], dtype=np.float32)
    cold_tint = np.array([0.88, 0.92, 1.08], dtype=np.float32)
    rgb *= cold_tint * (1.0 - hot) + warm_tint * hot
    rgb *= 0.82 + 0.18 * term

    # Height shading: peaks lighter on bronze, darker on obsidian flats.
    height_shade = 0.75 + 0.35 * elevation
    rgb *= height_shade[..., None]

    # Geyser vent speckles in B2 region.
    vent_mask = weights[2] > 0.35
    vent_speckle = (detail_noise > 0.72) & vent_mask
    rgb[vent_speckle] = np.minimum(rgb[vent_speckle] * 1.4 + 30, 255)

    # Polar rad shimmer on B5.
    polar_mask = weights[5] > 0.3
    shimmer = (np.sin(u * 80 + v * 60) * 0.5 + 0.5)[polar_mask]
    rgb[polar_mask] += np.stack([shimmer * 8, shimmer * 12, shimmer * 20], axis=-1)

    # Ruin resonance pulse on B7.
    ruin_mask = weights[6] > 0.3
    pulse = (np.sin((u + v) * 40) * 0.5 + 0.5)[ruin_mask]
    rgb[ruin_mask] += np.stack([pulse * 6, pulse * 14, pulse * 14], axis=-1)

    vacuum = np.array(PALETTES["vacuum"]["base"], dtype=np.float32)
    rgb = rgb * disc_mask[..., None] + vacuum * (1.0 - disc_mask[..., None])

    return np.clip(rgb, 0, 255).astype(np.uint8)


def compose_isometric(topdown: np.ndarray) -> np.ndarray:
    """Project top-down color field into a simple 3/4 isometric relief view."""
    size = topdown.shape[0]
    gray = topdown.astype(np.float32).mean(axis=2) / 255.0
    elevation_hint = fbm((size, size), octaves=5, seed=SEED + 3) * 0.6 + gray * 0.4

    iso_u = np.linspace(-1.0, 1.0, size, dtype=np.float32)
    iso_v = np.linspace(-1.0, 1.0, size, dtype=np.float32)
    u_grid, v_grid = np.meshgrid(iso_u, iso_v)

    su = 0.5 + u_grid * 0.42 + v_grid * 0.21
    sv = 0.5 + v_grid * 0.42 - u_grid * 0.12
    off_disc = (su < 0.0) | (su > 1.0) | (sv < 0.0) | (sv > 1.0)

    sx = np.clip((su * (size - 1)).astype(np.int32), 0, size - 1)
    sy = np.clip(((1.0 - sv) * (size - 1)).astype(np.int32), 0, size - 1)

    sampled = topdown[sy, sx]
    elev = elevation_hint[sy, sx]
    shade = 0.55 + 0.45 * elev + 0.08 * u_grid
    out = np.clip(sampled.astype(np.float32) * shade[..., None], 0, 255).astype(np.uint8)

    vacuum = np.array(PALETTES["vacuum"]["base"], dtype=np.uint8)
    out[off_disc] = vacuum
    return out


def annotate_map(image: Image.Image, title: str) -> Image.Image:
    """Add subtle title + biome legend for design reference."""
    draw = ImageDraw.Draw(image)
    try:
        font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 22)
        small = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 14)
    except OSError:
        font = ImageFont.load_default()
        small = font

    draw.rectangle((24, 24, 520, 64), fill=(28, 42, 56, 200))
    draw.text((36, 32), title, fill=(237, 233, 228), font=font)

    y = 80
    for name, _u, _v, _r, palette_name, _e in BIOMES:
        color = PALETTES[palette_name]["accent"]
        draw.rectangle((24, y, 44, y + 16), fill=color)
        draw.text((52, y), name, fill=(200, 195, 190), font=small)
        y += 22

    # Command Center marker.
    cx = int(COMMAND_CENTER_UV[0] * image.width)
    cy = int((1.0 - COMMAND_CENTER_UV[1]) * image.height)
    r = 10
    draw.ellipse((cx - r, cy - r, cx + r, cy + r), outline=(212, 160, 23), width=3)
    draw.text((cx + 14, cy - 8), "Command Center", fill=(212, 160, 23), font=small)
    return image


def save_png(array: np.ndarray, path: Path, annotate: bool = False, title: str = "") -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    image = Image.fromarray(array, mode="RGB")
    if annotate:
        image = annotate_map(image, title)
    image.save(path, format="PNG", optimize=True)
    print(f"Wrote {path} ({array.shape[1]}x{array.shape[0]})")


def main() -> None:
    print("Generating Io Genesis moon maps...")
    topdown = compose_topdown(RESOLUTION)
    isometric = compose_isometric(topdown)

    topdown_path = OUT_TEXTURES / "GenesisMoonMap_TopDown.png"
    iso_path = OUT_TEXTURES / "GenesisMoonMap_Isometric.png"
    runtime_path = OUT_RESOURCES / "GenesisMoonMap.png"

    save_png(topdown, topdown_path, annotate=True, title="Io Genesis — Top-Down (B1–B7)")
    save_png(isometric, iso_path, annotate=True, title="Io Genesis — Isometric (B1–B7)")
    save_png(topdown, runtime_path, annotate=False)

    print("Done.")


if __name__ == "__main__":
    main()
