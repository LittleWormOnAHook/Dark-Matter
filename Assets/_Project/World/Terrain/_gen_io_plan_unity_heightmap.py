"""Generate smooth Unity-ready Io plan elevation heightmap (2048^2).

Art shaded-relief heightmap is NOT overwritten.
Normalized 0..1 maps to Unity Terrain Height (primary: 100).
Peaks ~0.25–0.40 → ~25–40 world units at Height=100.
"""
from __future__ import annotations

import subprocess
import sys
from pathlib import Path

import numpy as np
from PIL import Image

SRC_HM = Path(r"A:\Survival Pioneer\Assets\_Project\World\WorldMap\Io_Plan_Heightmap.png")
SRC_BIOME = Path(r"A:\Survival Pioneer\Assets\_Project\World\WorldMap\Io_Plan_BiomeMap_TopDown.png")
OUT_DIR = Path(r"A:\Survival Pioneer\Assets\_Project\World\Terrain")
OUT_PNG = OUT_DIR / "Io_Plan_Heightmap_Unity.png"
OUT_RAW = OUT_DIR / "Io_Plan_Heightmap_Unity.raw"
OUT_PREVIEW = OUT_DIR / "Io_Plan_Heightmap_Unity_Preview8.png"
SIZE = 2048


def ensure_scipy():
    try:
        from scipy.ndimage import distance_transform_edt, gaussian_filter  # noqa: F401
    except ImportError:
        subprocess.check_call([sys.executable, "-m", "pip", "install", "scipy", "-q"])


def main() -> None:
    ensure_scipy()
    from scipy.ndimage import (
        binary_dilation,
        distance_transform_edt,
        gaussian_filter,
        grey_closing,
        grey_opening,
    )

    OUT_DIR.mkdir(parents=True, exist_ok=True)

    hm = np.array(Image.open(SRC_HM), dtype=np.float32)
    biome = np.array(Image.open(SRC_BIOME).convert("RGB"), dtype=np.float32)
    h, w = hm.shape

    land = hm > 6.0
    ocean = ~land
    dist = distance_transform_edt(land).astype(np.float32)

    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    cx, cy = (w - 1) * 0.5, (h - 1) * 0.5
    r = np.sqrt(((xx - cx) / cx) ** 2 + ((yy - cy) / cy) ** 2)

    r_ch, g_ch, b_ch = biome[:, :, 0], biome[:, :, 1], biome[:, :, 2]
    value = (r_ch + g_ch + b_ch) / 3.0
    chroma = np.maximum(np.maximum(r_ch, g_ch), b_ch) - np.minimum(
        np.minimum(r_ch, g_ch), b_ch
    )
    label = ((value > 210) & (chroma > 25)) | ((chroma > 55) & (value > 160))
    label = binary_dilation(label, iterations=3)
    usable = land & ~label

    orange = np.clip((r_ch - b_ch) / 80.0, 0, 1) * np.clip((r_ch - g_ch) / 40.0, 0, 1)
    yellow = np.clip((r_ch - b_ch) / 60.0, 0, 1) * np.clip((g_ch - b_ch) / 50.0, 0, 1)
    south = np.clip((yy - cy) / cy, 0, 1)
    north = np.clip((cy - yy) / cy, 0, 1)
    west = np.clip((cx - xx) / cx, 0, 1)
    east = np.clip((xx - cx) / cx, 0, 1)

    w_b6 = (np.clip(1.0 - r / 0.26, 0, 1) ** 1.15) * usable
    w_b7 = np.exp(-((r - 0.36) ** 2) / (2 * 0.10**2)) * usable * (1.0 - 0.85 * w_b6)
    w_b4 = np.clip(0.3 * orange + 0.7 * orange * (0.35 + 0.65 * south), 0, 1)
    w_b4 = w_b4 * usable * (1.0 - 0.9 * w_b6)
    w_b5 = np.clip(0.6 * north, 0, 1) * usable * (1.0 - w_b6) * (1.0 - 0.65 * w_b4)
    w_b3 = np.clip(0.5 * west, 0, 1) * usable * (1.0 - w_b6) * (1.0 - 0.5 * w_b4) * (
        1.0 - 0.4 * w_b5
    )
    w_b1 = np.clip(yellow * (0.4 + 0.6 * north * west), 0, 1)
    w_b1 = w_b1 * usable * (1.0 - w_b6) * (1.0 - 0.65 * w_b4)
    w_b2 = np.clip(0.55 * east, 0, 1) * usable * (1.0 - w_b6) * (1.0 - 0.5 * w_b4) * (
        1.0 - 0.35 * w_b5
    )

    w_b6 = gaussian_filter(w_b6, 40)
    w_b7 = gaussian_filter(w_b7, 35)
    w_b4 = gaussian_filter(w_b4, 35)
    w_b5 = gaussian_filter(w_b5, 40)
    w_b3 = gaussian_filter(w_b3, 40)
    w_b1 = gaussian_filter(w_b1, 40)
    w_b2 = gaussian_filter(w_b2, 40)

    stack = np.maximum(np.stack([w_b1, w_b2, w_b3, w_b4, w_b5, w_b6, w_b7], 0), 0)
    stack = stack / np.maximum(stack.sum(0), 1e-6)
    for i in range(7):
        stack[i] *= land
    w_b1, w_b2, w_b3, w_b4, w_b5, w_b6, w_b7 = stack
    w_plains = np.maximum(land.astype(np.float32) * (1.0 - stack.sum(0)), 0)

    e_b5, e_b1, e_b2, e_plains = 0.09, 0.11, 0.12, 0.10
    e_b3, e_b7, e_b4, e_b6 = 0.18, 0.24, 0.22, 0.34

    base = (
        w_b5 * e_b5
        + w_b1 * e_b1
        + w_b2 * e_b2
        + w_b3 * e_b3
        + w_b4 * e_b4
        + w_b7 * e_b7
        + w_b6 * e_b6
        + w_plains * e_plains
    )
    coast = np.clip(dist / 110.0, 0, 1)
    coast = coast * coast * (3.0 - 2.0 * coast)
    base *= coast

    macro = gaussian_filter(hm, 100)
    ml = macro.copy()
    ml[ocean] = 0
    mmin, mmax = np.percentile(ml[land], [5, 95])
    macro_n = np.clip((ml - mmin) / max(mmax - mmin, 1e-3), 0, 1)
    macro_n[ocean] = 0

    dome = w_b6 * (0.05 * np.clip(1.0 - (r / 0.26) ** 2, 0, 1))

    hm_s = gaussian_filter(hm, 12)
    opened = grey_opening(hm_s, size=(55, 55))
    closed = grey_closing(hm_s, size=(55, 55))
    rim = gaussian_filter(np.clip((closed - hm_s) / 70.0, 0, 1), 18)
    bowl = gaussian_filter(np.clip((hm_s - opened) / 70.0, 0, 1), 18)
    caldera = w_b4 * (0.04 * rim - 0.03 * bowl)

    elev = base + 0.06 * macro_n * land + dome + caldera
    elev[ocean] = 0.0

    for s in (18, 12, 8, 6):
        elev = gaussian_filter(elev, s)
        elev[ocean] = 0.0
    elev *= np.where(land, np.clip(dist / 85.0, 0, 1), 0.0)
    elev = gaussian_filter(elev, 5)
    elev[ocean] = 0.0

    p99 = float(np.percentile(elev[land], 99.7))
    elev_n = np.clip(elev / max(p99, 1e-4), 0, 1) * 0.38
    elev_n[ocean] = 0.0
    near = land & (dist < 30)
    elev_n[near] = np.maximum(elev_n[near], 0.025 * np.clip(dist[near] / 30.0, 0, 1))
    elev_n[land] = np.maximum(elev_n[land], 0.04 * coast[land])
    elev_n = gaussian_filter(elev_n, 6)
    elev_n[ocean] = 0.0

    print(
        "elev land mean/p95/max",
        float(elev_n[land].mean()),
        float(np.percentile(elev_n[land], 95)),
        float(elev_n[land].max()),
    )

    img16 = Image.fromarray((elev_n * 65535.0).astype(np.uint16))
    img16 = img16.resize((SIZE, SIZE), Image.Resampling.LANCZOS)
    arr = np.array(img16, dtype=np.float32) / 65535.0
    for s in (4.5, 3.0, 2.0):
        arr = gaussian_filter(arr, s)
    arr[arr < 0.01] = 0.0
    arr = np.clip(arr, 0.0, 0.40)

    arr16 = (arr * 65535.0).astype(np.uint16)
    Image.fromarray(arr16).save(OUT_PNG)
    arr16.astype("<u2").tofile(OUT_RAW)
    vis = np.clip(arr / 0.40, 0, 1)
    Image.fromarray((vis * 255.0).astype(np.uint8), mode="L").save(OUT_PREVIEW)

    land8 = arr > 0.01
    print("Wrote", OUT_PNG, arr16.shape)
    print("Wrote", OUT_RAW, OUT_RAW.stat().st_size, "ok", OUT_RAW.stat().st_size == SIZE * SIZE * 2)
    if land8.any():
        print(
            "final land mean/p95/max Height100 ~",
            float(arr[land8].mean()) * 100,
            float(np.percentile(arr[land8], 95)) * 100,
            float(arr.max()) * 100,
        )


if __name__ == "__main__":
    main()
