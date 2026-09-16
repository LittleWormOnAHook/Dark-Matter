# Vendor install manifest

Record **what each developer must install locally** when folders are gitignored. Update this when adding or removing a pack.

| Pack | Asset Store / source | Import path on disk | In git? | Used for (short) |
|------|----------------------|---------------------|---------|------------------|
| L.V.E Lava and Volcano | NatureManufacture | `Assets/NatureManufacture Assets/L.V.E- Lava and Volcano Environment/` | Ignored | Volcano / lava environment (HDRP support packs local) |
| Planet Pack 02 | Asset Store | `Assets/PlanetPack02/` | Ignored (legacy files may still be tracked — see policy) | Planet / sky props |
| OlegWER Alien Flora Set | Asset Store / author | `Assets/OlegWER/` | Ignored (legacy tracked — see policy) | Flora meshes |
| GDK Edition auto-generated | Xbox GDK / Unity | `Assets/Resources/GDKEditionAutoGen/` | Ignored | Console GDK generated resources |
| Huge FBX Mocap Library (part 1) | Asset Store | Under `Assets/Animations/` (Props Animations subset) | Partial / do not bulk-add mocap trees | Legacy prop / mocap samples |
| PROTOFACTOR | Vendor | `Assets/PROTOFACTOR/` | Ignored | Local only |
| Gaia user data | Procedural Worlds | `Assets/Gaia User Data/` | Ignored | Local terrain authoring |
| Procedural Worlds install cache | Procedural Worlds | `Assets/Procedural Worlds/Packages - Install/` etc. | Ignored | Local PW packages |

**After clone or pull:** install rows marked Ignored, then Unity **Ctrl+R** (Auto Refresh off on this project).

**Version pinning:** When it matters, note Asset Store version or `.unitypackage` filename in the “source” column in a PR or commit message.
