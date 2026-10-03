import re
from pathlib import Path

TEX_DIR = Path(r"Assets/Rock and Boulders3/origin/Textures")
MAT_DIR = Path(r"Assets/Rock and Boulders3/origin/Meshes/Materials")
# First 11 materials already wired (ends at Rock05_LOD_a); do not overwrite.
SKIP_MATS = {
    "Rock_Small1_LOD_a.mat",
    "Rock_Small2_LOD_a.mat",
    "Rock01_a.mat",
    "Rock01_LOD_a.mat",
    "Rock02_a.mat",
    "Rock02_LOD_a.mat",
    "Rock03_a.mat",
    "Rock03_LOD_a.mat",
    "Rock04_a.mat",
    "Rock04_LOD_a.mat",
    "Rock05_LOD_a.mat",
}

ALBEDO = {"_Aldebo", "_MainTex", "_RockAlbedo", "_BaseColorMap"}
MASK = {"_MetallicGlossMap", "_RockMetallicGloss", "_MaskMap"}
NORMAL = {"_BumpMap", "_Normalmap", "_RockNormal", "_NormalMap"}
AO = {"_AO", "_Ambient", "_OcclusionMap"}

PROP_LINE = re.compile(r"^\s+- (_\w+):\s*$")
GUID_LINE = re.compile(r"^(\s+m_Texture: \{fileID: 2800000, guid: )([a-f0-9]+)(, type: 3\})$")
EMPTY_TEX_LINE = re.compile(r"^(\s+m_Texture: \{fileID: )0(\})$")


def load_guids(root: Path):
    guids = {}
    tex_dir = root / TEX_DIR
    for meta in tex_dir.glob("*.tif.meta"):
        stem = meta.name.replace(".tif.meta", "")
        text = meta.read_text(encoding="utf-8")
        guid = text.split("guid: ", 1)[1].split("\n", 1)[0].strip()
        if stem.endswith("_a"):
            guids.setdefault(stem[:-2], {})["a"] = guid
        elif stem.endswith("_m"):
            guids.setdefault(stem[:-2], {})["m"] = guid
        elif stem.endswith("_n"):
            guids.setdefault(stem[:-2], {})["n"] = guid
        elif stem.endswith("_ao"):
            guids.setdefault(stem[:-3], {})["ao"] = guid
    return guids


def material_to_base(name: str):
    n = name.replace(".mat", "")
    if n in ("deserta 1",):
        return None
    if n.endswith("_desert"):
        n = n[: -len("_desert")]
    if n.endswith("_a"):
        n = n[:-2]
    n = n.replace("_LOD", "")
    m = re.fullmatch(r"RockGr(\d+)", n)
    if m:
        num = m.group(1)
        n = f"RockGr_{num.zfill(2)}" if len(num) <= 2 else f"RockGr_{num}"
    if "Rock_Small1" in n:
        return "RockSmall01"
    if "Rock_Small2" in n:
        return "RockSmall02"
    if re.fullmatch(r"RockSmall\d+", n):
        return n
    if re.fullmatch(r"Rock\d+", n):
        return n
    if re.fullmatch(r"RockGr_\d+", n):
        return n
    return n


def patch_material(path: Path, tex: dict) -> bool:
    lines = path.read_text(encoding="utf-8").splitlines(keepends=True)
    out = []
    changed = False
    i = 0
    while i < len(lines):
        line = lines[i]
        pm = PROP_LINE.match(line.rstrip("\n"))
        if pm and i + 1 < len(lines):
            prop = pm.group(1)
            gline = lines[i + 1]
            gline_stripped = gline.rstrip("\n")
            gm = GUID_LINE.match(gline_stripped)
            em = EMPTY_TEX_LINE.match(gline_stripped)
            if gm or em:
                new_guid = None
                if prop in ALBEDO:
                    new_guid = tex.get("a")
                elif prop in MASK:
                    new_guid = tex.get("m")
                elif prop in NORMAL:
                    new_guid = tex.get("n")
                elif prop in AO:
                    new_guid = tex.get("ao")
                if new_guid:
                    out.append(line)
                    if gm and gm.group(2) != new_guid:
                        out.append(f"{gm.group(1)}{new_guid}{gm.group(3)}\n")
                        changed = True
                    elif em:
                        out.append(f"        m_Texture: {{fileID: 2800000, guid: {new_guid}, type: 3}}\n")
                        changed = True
                    else:
                        out.append(gline)
                    i += 2
                    continue
        out.append(line)
        i += 1

    if changed:
        path.write_text("".join(out), encoding="utf-8")
    return changed


def main():
    import sys

    include_first_batch = "--all" in sys.argv
    root = Path(__file__).resolve().parents[1]
    guids = load_guids(root)
    mat_dir = root / MAT_DIR
    mats = sorted(mat_dir.glob("*.mat"))

    skipped = []
    updated = []
    missing = []

    for mat_path in mats:
        if not include_first_batch and mat_path.name in SKIP_MATS:
            skipped.append(mat_path.name)
            continue

        base = material_to_base(mat_path.name)
        if not base or base not in guids:
            missing.append((mat_path.name, base))
            continue
        tex = guids[base]
        if not all(k in tex for k in ("a", "m", "n")):
            missing.append((mat_path.name, base, sorted(tex.keys())))
            continue
        if patch_material(mat_path, tex):
            updated.append(mat_path.name)

    if include_first_batch:
        print("Mode: all materials (including Rock01–Rock05_LOD batch)")
    else:
        print(f"Skipped {len(skipped)} (through Rock05_LOD_a batch)")
    print(f"Updated {len(updated)} materials")
    if missing:
        print(f"Missing mapping for {len(missing)} materials:")
        for row in missing:
            print(" ", row)


if __name__ == "__main__":
    main()
