#!/usr/bin/env python3
"""Remap L.V.E Built-in Standard lava material shader GUIDs to NM HDRP equivalents."""

from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
LVE = ROOT / "Assets/NatureManufacture Assets/L.V.E- Lava and Volcano Environment"

FIXED = {
    "3ef0cb5a2319771478ac44698ebb6ef9": "ce11364fad77f7c4f83f61151424ddaa",  # Standard UV Free
    "55ba1fa7c44f29242ae2c0f1fcb0d494": "ce11364fad77f7c4f83f61151424ddaa",  # Specular UV Free
    "c8416289398e93647aa94743c97939a8": "ce11364fad77f7c4f83f61151424ddaa",  # Frozen River
    "4e059242ef121004cb8a66e0fd817e9c": "b5e906738b549f14f86d395bb883ce5d",  # Standard Metallic
    "4d02fc391a20121438e42845fcb3e7c5": "b5e906738b549f14f86d395bb883ce5d",  # CutOut
    "9b6be94e5e62b844491511eaec06d8c1": "b5e906738b549f14f86d395bb883ce5d",  # Specular
    "b52ea6be2e7c8954ab7ca92c21d90837": "b5e906738b549f14f86d395bb883ce5d",  # Specular CutOut
    "d5aff4267d23a1344aaf15b1633043dc": "32a65825244304d4ca196f347ee6ca97",  # Vertex Color Only
    "83adc62d3a7ed22439f449a848283984": "fe2bc8314ee86e740978756ecc0b7275",  # Vulcano Smoke
}

RIVER = {
    "951e551eb5a54334ab498637e84fe777": (
        "bc527e0817d82d4489cd35462d4e68b0",
        "224b880707fc836458b721e4d5a86eb9",
    ),
    "a078bba5a980e3447a3b8f0ef47227d2": (
        "f8ee384eec2163d44a375a36b7b4726a",
        "e3c21255a9f890b4c97d777b47f9178e",
    ),
    "c0dfe728864ec2447a8672eec7e6bdd0": (
        "32a65825244304d4ca196f347ee6ca97",
        "7e3717c777e06bf4da27fec119bd3465",
    ),
}

SHADER_LINE = re.compile(
    r"(m_Shader: \{fileID: 4800000, guid: )([0-9a-f]{32})(, type: 3\})"
)


def target_guid(source: str, material_name: str) -> str | None:
    if source in FIXED:
        return FIXED[source]
    if source in RIVER:
        plain, tess = RIVER[source]
        if "tesseled" in material_name.lower() or "tessell" in material_name.lower():
            return tess
        return plain
    return None


def main() -> None:
    if not LVE.is_dir():
        raise SystemExit(f"L.V.E folder not found: {LVE}")

    changed: list[str] = []
    for mat in LVE.rglob("*.mat"):
        text = mat.read_text(encoding="utf-8")
        match = SHADER_LINE.search(text)
        if not match:
            continue
        source = match.group(2)
        new = target_guid(source, mat.stem)
        if not new or new == source:
            continue
        updated = SHADER_LINE.sub(rf"\g<1>{new}\3", text, count=1)
        if updated != text:
            mat.write_text(updated, encoding="utf-8")
            changed.append(f"{mat.relative_to(ROOT)}  {source} -> {new}")

    print(f"Remapped {len(changed)} materials")
    for line in changed:
        print(f"  {line}")


if __name__ == "__main__":
    main()
