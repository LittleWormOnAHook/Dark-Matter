"""AAA display-demo grade on a Unity screenshot. Same pixels, exhibition post."""
from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter


def to_float(img: Image.Image) -> np.ndarray:
    arr = np.asarray(img.convert("RGB"), dtype=np.float32) / 255.0
    return np.clip(arr, 0.0, 1.0)


def to_image(arr: np.ndarray) -> Image.Image:
    u8 = np.clip(arr * 255.0 + 0.5, 0, 255).astype(np.uint8)
    return Image.fromarray(u8, "RGB")


def srgb_to_linear(c: np.ndarray) -> np.ndarray:
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def linear_to_srgb(c: np.ndarray) -> np.ndarray:
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * np.power(np.clip(c, 0, None), 1.0 / 2.4) - 0.055)


def s_curve(x: np.ndarray, contrast: float) -> np.ndarray:
    # contrast 0 = identity, 1 = hard S
    x = np.clip(x, 0.0, 1.0)
    return np.clip(x + contrast * (x - x * x) * (x - 0.5) * 4.0, 0.0, 1.0)


def gaussian_blur(arr: np.ndarray, radius: float) -> np.ndarray:
    img = to_image(arr).filter(ImageFilter.GaussianBlur(radius=radius))
    return to_float(img)


def grade(src: Path, dst: Path) -> None:
    rgb = to_float(Image.open(src))
    h, w, _ = rgb.shape
    lin = srgb_to_linear(rgb)

    # Exposure + cinematic S-curve
    lin *= 1.12
    rgb = linear_to_srgb(np.clip(lin, 0.0, 4.0))
    rgb = s_curve(rgb, 0.38)

    # Split-tone: cool-violet shadows, amber sulfur mids/highlights
    luma = rgb @ np.array([0.2126, 0.7152, 0.0722], dtype=np.float32)
    shadow = np.clip(1.0 - luma * 1.8, 0.0, 1.0)[..., None]
    mid = (1.0 - np.abs(luma - 0.45) * 2.2).clip(0.0, 1.0)[..., None]
    high = np.clip((luma - 0.55) * 2.0, 0.0, 1.0)[..., None]

    rgb = rgb + shadow * np.array([0.035, 0.01, 0.07], dtype=np.float32)
    rgb = rgb + mid * np.array([0.085, 0.028, -0.03], dtype=np.float32)
    rgb = rgb + high * np.array([0.06, 0.03, -0.015], dtype=np.float32)

    # Vibrance: boost low-sat oranges/yellows, keep skin-ish neutrals
    mean = rgb.mean(axis=2, keepdims=True)
    sat = rgb.max(axis=2, keepdims=True) - rgb.min(axis=2, keepdims=True)
    vibrance = (1.0 - sat) * 0.42
    rgb = mean + (rgb - mean) * (1.0 + vibrance)
    rgb[..., 0] = np.clip(rgb[..., 0] * 1.06, 0, 1)
    rgb[..., 1] = np.clip(rgb[..., 1] * 1.02, 0, 1)

    # Warm horizon wash / cool zenith (simple vertical grade)
    yy = np.linspace(0.0, 1.0, h, dtype=np.float32)[:, None, None]
    horizon = np.exp(-((yy - 0.42) ** 2) / 0.045)
    zenith = np.clip(1.0 - yy * 1.15, 0.0, 1.0)
    rgb = rgb + horizon * np.array([0.07, 0.025, 0.0], dtype=np.float32)
    rgb = rgb + zenith * np.array([0.02, 0.0, 0.055], dtype=np.float32) * 0.65
    rgb = np.clip(rgb, 0.0, 1.0)

    # Bloom on highlights (Jupiter / gold HUD / bright dust)
    glow_mask = np.clip((luma - 0.62) * 3.2, 0.0, 1.0)[..., None]
    glow = rgb * glow_mask
    bloom = gaussian_blur(glow, 18.0) * 0.85 + gaussian_blur(glow, 42.0) * 0.45
    rgb = np.clip(rgb + bloom * 0.55, 0.0, 1.0)

    # Soft atmospheric haze toward the upper mid (tower/lander distance)
    haze = gaussian_blur(rgb, 8.0)
    haze_amt = (0.12 + 0.18 * (1.0 - yy)) * np.clip(0.25 + luma * 0.4, 0, 1)[..., None]
    rgb = rgb * (1.0 - haze_amt * 0.55) + haze * haze_amt * 0.55 + haze_amt * np.array(
        [0.55, 0.42, 0.48], dtype=np.float32
    ) * 0.08

    # Unsharp
    sharp = rgb + (rgb - gaussian_blur(rgb, 1.2)) * 0.55
    rgb = np.clip(sharp, 0.0, 1.0)

    # Mild chromatic aberration at edges
    xs = np.linspace(-1.0, 1.0, w, dtype=np.float32)
    ys = np.linspace(-1.0, 1.0, h, dtype=np.float32)
    xx, yy2 = np.meshgrid(xs, ys)
    radial = np.sqrt(xx * xx + yy2 * yy2)
    shift = (radial * 2.2).astype(np.int32)
    r = np.zeros_like(rgb[..., 0])
    b = np.zeros_like(rgb[..., 0])
    for y in range(h):
        sx = np.clip(np.arange(w) + shift[y], 0, w - 1)
        bx = np.clip(np.arange(w) - shift[y], 0, w - 1)
        r[y] = rgb[y, sx, 0]
        b[y] = rgb[y, bx, 2]
    rgb[..., 0] = r
    rgb[..., 2] = b

    # Vignette
    vig = 1.0 - np.clip((radial - 0.35) / 1.15, 0.0, 1.0) ** 1.35 * 0.42
    rgb *= vig[..., None]

    # Film grain
    rng = np.random.default_rng(2160)
    grain = rng.normal(0.0, 0.018, rgb.shape).astype(np.float32)
    rgb = np.clip(rgb + grain * (0.65 + 0.35 * (1.0 - luma[..., None])), 0.0, 1.0)

    dst.parent.mkdir(parents=True, exist_ok=True)
    to_image(rgb).save(dst, "PNG", optimize=True)
    print(dst)


if __name__ == "__main__":
    src = Path(sys.argv[1])
    dst = Path(sys.argv[2])
    grade(src, dst)
