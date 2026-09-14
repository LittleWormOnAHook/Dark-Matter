"""
Batch-generate 10 tileable Io moon surface terrain sets (Color, Normal, Mask) at 2048.
Toroidal FFT height fields; seams locked on all outputs.
"""
from __future__ import annotations

import argparse
from dataclasses import dataclass
from pathlib import Path

import numpy as np
from PIL import Image


def tileable_noise(n: int, seed: int, falloff_power: float = 1.5) -> np.ndarray:
    rng = np.random.default_rng(seed)
    kx = np.fft.fftfreq(n)
    ky = np.fft.fftfreq(n)
    kxx, kyy = np.meshgrid(kx, ky, indexing="ij")
    radius = np.sqrt(kxx * kxx + kyy * kyy)
    radius[0, 0] = 1.0
    amplitude = 1.0 / np.power(radius, falloff_power)
    amplitude[0, 0] = 0.0
    phase = rng.standard_normal((n, n)) + 1j * rng.standard_normal((n, n))
    field = np.fft.ifft2(spectrum := phase * amplitude).real
    field -= field.min()
    field /= max(field.max(), 1e-8)
    return field.astype(np.float32)


def upsample_tileable_nearest(src: np.ndarray, out_size: int) -> np.ndarray:
    n = src.shape[0]
    if out_size == n:
        return src
    factor = out_size // n
    return np.kron(src, np.ones((factor, factor), dtype=np.float32))


def fbm_tileable(size: int, seed: int, octaves: int = 6) -> np.ndarray:
    total = np.zeros((size, size), dtype=np.float32)
    amp = 1.0
    norm = 0.0
    for o in range(octaves):
        layer_size = size // (2**o)
        if layer_size < 8:
            break
        layer = tileable_noise(layer_size, seed + o * 131, falloff_power=1.35 + o * 0.08)
        if layer_size != size:
            layer = upsample_tileable_nearest(layer, size)
        total += layer * amp
        norm += amp
        amp *= 0.55
    total /= max(norm, 1e-6)
    return np.clip(total, 0.0, 1.0)


def torus_uv(size: int) -> tuple[np.ndarray, np.ndarray]:
    ys, xs = np.mgrid[0:size, 0:size].astype(np.float32)
    u = xs / size * 2.0 * np.pi
    v = ys / size * 2.0 * np.pi
    return u, v


def crack_layer(size: int, seed: int, power: float = 5.5) -> np.ndarray:
    a = fbm_tileable(size, seed + 900, octaves=5)
    b = fbm_tileable(size, seed + 1200, octaves=4)
    ridges = np.power(np.clip(1.0 - np.abs(a - 0.5) * 2.0, 0.0, 1.0), power)
    thin = np.power(np.clip(1.0 - np.abs(b - 0.5) * 2.0, 0.0, 1.0), 12.0)
    return np.clip(ridges * 0.5 + thin * 0.85, 0.0, 1.0)


def lock_seams_2d(arr: np.ndarray) -> None:
    arr[:, -1, ...] = arr[:, 0, ...]
    arr[-1, :, ...] = arr[0, :, ...]


def lock_seams_height(h: np.ndarray) -> None:
    h[:, -1] = h[:, 0]
    h[-1, :] = h[0, :]


def height_to_normal(h: np.ndarray, strength: float = 3.5) -> np.ndarray:
    dx = (np.roll(h, -1, axis=1) - np.roll(h, 1, axis=1)) * 0.5
    dy = (np.roll(h, -1, axis=0) - np.roll(h, 1, axis=0)) * 0.5
    nx = -dx * strength
    ny = -dy * strength
    nz = np.ones_like(h, dtype=np.float32)
    inv_len = 1.0 / np.sqrt(nx * nx + ny * ny + nz * nz + 1e-8)
    nx *= inv_len
    ny *= inv_len
    nz *= inv_len
    normal = np.stack([nx, ny, nz], axis=-1)
    normal = normal * 0.5 + 0.5
    lock_seams_2d(normal)
    return np.clip(normal, 0.0, 1.0)


def build_mask(h: np.ndarray, metallic: float, smooth_base: float, smooth_var: float) -> np.ndarray:
    ao = np.clip(1.0 - h * 0.55 - fbm_tileable(h.shape[0], 777, octaves=3) * 0.15, 0.0, 1.0)
    smooth = np.clip(smooth_base + (1.0 - h) * smooth_var, 0.0, 1.0)
    r = np.full_like(h, metallic, dtype=np.float32)
    g = ao.astype(np.float32)
    b = h.astype(np.float32)
    a = smooth.astype(np.float32)
    mask = np.stack([r, g, b, a], axis=-1)
    lock_seams_2d(mask)
    return mask


@dataclass
class SurfaceSet:
    index: int
    folder: str
    name: str


SETS = [
    SurfaceSet(1, "Set01_Basalt_Sulfur_Dust", "Basalt_Sulfur_Dust"),
    SurfaceSet(2, "Set02_Sulfur_Plains", "Sulfur_Plains"),
    SurfaceSet(3, "Set03_Geyser_Sinter", "Geyser_Sinter"),
    SurfaceSet(4, "Set04_Lava_Cooled_Glass", "Lava_Cooled_Glass"),
    SurfaceSet(5, "Set05_Ash_Regolith", "Ash_Regolith"),
    SurfaceSet(6, "Set06_Brimstone_Crystal", "Brimstone_Crystal"),
    SurfaceSet(7, "Set07_Radial_Flow_Bands", "Radial_Flow_Bands"),
    SurfaceSet(8, "Set08_Radiation_Scorched", "Radiation_Scorched"),
    SurfaceSet(9, "Set09_Alien_Residue", "Alien_Residue"),
    SurfaceSet(10, "Set10_Anthropogenic_Trace", "Anthropogenic_Trace"),
]


def recipe_height(size: int, seed: int, set_index: int) -> np.ndarray:
    macro = fbm_tileable(size, seed, octaves=6)
    detail = fbm_tileable(size, seed + 17, octaves=7)
    micro = fbm_tileable(size, seed + 31, octaves=4)
    u, v = torus_uv(size)
    h = macro * 0.5 + detail * 0.35 + micro * 0.15

    if set_index == 7:
        bands = 0.5 + 0.5 * np.sin(u * 5.0 + macro * 3.0) * np.cos(v * 3.0 + detail * 2.0)
        h = h * 0.55 + bands * 0.45

    if set_index == 10:
        tread = np.sin(u * 28.0) * 0.04 + np.sin(v * 22.0) * 0.03
        scorch = np.exp(-((np.sin(u * 3.0) ** 2 + np.cos(v * 3.0) ** 2) * 2.5)) * 0.08
        alien_grid = (np.sin(u * 48.0) * np.sin(v * 48.0)) * 0.025
        h = h + tread + scorch + alien_grid

    if set_index == 9:
        veins = np.power(np.clip(1.0 - np.abs(fbm_tileable(size, seed + 444, 4) - 0.5) * 2.0, 0.0, 1.0), 8.0)
        h = h + veins * 0.12

    h = np.power(np.clip(h, 0.0, 1.0), 1.02)
    lock_seams_height(h)
    return h.astype(np.float32)


def recipe_color(h: np.ndarray, seed: int, set_index: int) -> np.ndarray:
    size = h.shape[0]
    cracks = crack_layer(size, seed)
    detail = fbm_tileable(size, seed + 11, octaves=5)

    if set_index == 1:
        base = np.stack([0.32 + h * 0.18, 0.29 + h * 0.16, 0.26 + h * 0.14], axis=-1)
        sulfur = np.stack([0.72, 0.68, 0.38], dtype=np.float32)
        rgb = base * (1.0 - cracks[..., None] * 0.35) + sulfur * (cracks[..., None] * 0.35)
    elif set_index == 2:
        rgb = np.stack(
            [0.78 + detail * 0.12 + h * 0.05, 0.70 + detail * 0.10, 0.28 + h * 0.08],
            axis=-1,
        )
        rgb -= cracks[..., None] * 0.15
    elif set_index == 3:
        rgb = np.stack(
            [0.82 + h * 0.08, 0.80 + h * 0.06, 0.76 + h * 0.05],
            axis=-1,
        )
        stain = np.stack([0.55, 0.32, 0.22], dtype=np.float32)
        rgb = rgb * 0.85 + stain * (cracks[..., None] * 0.25)
    elif set_index == 4:
        rgb = np.stack(
            [0.08 + h * 0.06, 0.12 + h * 0.08, 0.10 + h * 0.05],
            axis=-1,
        )
        rgb += np.stack([0.02, 0.06, 0.03], axis=-1) * (1.0 - h)[..., None]
    elif set_index == 5:
        rgb = np.stack([0.42 + h * 0.08, 0.39 + h * 0.07, 0.36 + h * 0.06], axis=-1)
    elif set_index == 6:
        rgb = np.stack(
            [0.88 + detail * 0.06, 0.84 + detail * 0.05, 0.55 + h * 0.1],
            axis=-1,
        )
    elif set_index == 7:
        dark = np.stack([0.14, 0.12, 0.11], dtype=np.float32)
        edge = np.stack([0.62, 0.55, 0.28], dtype=np.float32)
        t = np.clip(detail * 0.6 + cracks * 0.4, 0, 1)[..., None]
        rgb = dark * (1.0 - t) + edge * t + h[..., None] * 0.08
    elif set_index == 8:
        rgb = np.stack(
            [0.22 + h * 0.12, 0.16 + h * 0.08, 0.18 + h * 0.10],
            axis=-1,
        )
        rgb += np.stack([0.08, 0.02, 0.06], axis=-1) * cracks[..., None]
    elif set_index == 9:
        base = np.stack([0.28 + h * 0.1, 0.32 + h * 0.12, 0.30 + h * 0.1], axis=-1)
        alien = np.stack([0.35, 0.55, 0.42], dtype=np.float32)
        vein = crack_layer(size, seed + 333, power=7.0)
        rgb = base * (1.0 - vein[..., None] * 0.4) + alien * (vein[..., None] * 0.4)
    elif set_index == 10:
        base = np.stack([0.36 + h * 0.1, 0.33 + h * 0.09, 0.30 + h * 0.08], axis=-1)
        scorch_t = np.clip(1.0 - h, 0, 1)[..., None]
        scorch = np.stack([0.12, 0.10, 0.09], dtype=np.float32)
        alien_t = (np.abs(fbm_tileable(size, seed + 888, 3) - 0.5) < 0.04)[..., None].astype(np.float32)
        alien = np.stack([0.40, 0.52, 0.48], dtype=np.float32)
        rgb = base * (1.0 - scorch_t * 0.35) + scorch * (scorch_t * 0.35)
        rgb = rgb * (1.0 - alien_t * 0.15) + alien * (alien_t * 0.15)
    else:
        rgb = np.stack([h, h, h], axis=-1)

    speck = (fbm_tileable(size, seed + 501, octaves=3)[..., None] - 0.5) * 0.035
    rgb = np.clip(rgb + speck, 0.0, 1.0)
    lock_seams_2d(rgb)
    return rgb


def recipe_mask(h: np.ndarray, set_index: int) -> np.ndarray:
    presets = {
        1: (0.05, 0.22, 0.12),
        2: (0.03, 0.18, 0.08),
        3: (0.04, 0.28, 0.35),
        4: (0.12, 0.35, 0.55),
        5: (0.02, 0.15, 0.05),
        6: (0.06, 0.20, 0.18),
        7: (0.07, 0.24, 0.15),
        8: (0.08, 0.22, 0.10),
        9: (0.10, 0.25, 0.28),
        10: (0.14, 0.20, 0.22),
    }
    metal, smooth_base, smooth_var = presets[set_index]
    return build_mask(h, metal, smooth_base, smooth_var)


def save_rgb(path: Path, rgb: np.ndarray) -> None:
    img = (rgb * 255.0 + 0.5).astype(np.uint8)
    Image.fromarray(img, mode="RGB").save(path, compress_level=6)


def save_rgba(path: Path, rgba: np.ndarray) -> None:
    img = (rgba * 255.0 + 0.5).astype(np.uint8)
    Image.fromarray(img, mode="RGBA").save(path, compress_level=6)


def generate_set(out_root: Path, surface: SurfaceSet, size: int, base_seed: int) -> None:
    seed = base_seed + surface.index * 10007
    folder = out_root / surface.folder
    folder.mkdir(parents=True, exist_ok=True)
    idx = f"{surface.index:02d}"

    h = recipe_height(size, seed, surface.index)
    color = recipe_color(h, seed, surface.index)
    normal = height_to_normal(h, strength=4.0 if surface.index in (4, 6) else 3.2)
    mask = recipe_mask(h, surface.index)

    prefix = f"Io_Set{idx}"
    save_rgb(folder / f"{prefix}_Color_Periodic_{size}.png", color)
    save_rgb(folder / f"{prefix}_Normal_Periodic_{size}.png", normal)
    save_rgba(folder / f"{prefix}_Mask_Periodic_{size}.png", mask)
    print(f"Set {idx} {surface.name} -> {folder.name}/ ({size}px)")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--size", type=int, default=2048)
    parser.add_argument("--seed", type=int, default=2160)
    parser.add_argument(
        "--out-dir",
        type=Path,
        default=Path("Assets/_Project/Art/Terrain/Io_Moon_Surface"),
    )
    args = parser.parse_args()
    repo = Path(__file__).resolve().parents[1]
    out_root = args.out_dir if args.out_dir.is_absolute() else repo / args.out_dir
    out_root.mkdir(parents=True, exist_ok=True)

    for surface in SETS:
        generate_set(out_root, surface, args.size, args.seed)

    print(f"Done: {len(SETS)} sets x 3 maps under {out_root}")


if __name__ == "__main__":
    main()
