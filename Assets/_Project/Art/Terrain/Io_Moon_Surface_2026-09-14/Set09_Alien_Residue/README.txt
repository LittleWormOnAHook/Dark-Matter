Set name: Set09_Alien_Residue
Date: 2026-09-14
Family: Io_Moon_Surface (new folder; does not replace Io_Moon_Surface from 2026-09-12)

Intended use
------------
Terrain layer for Dark Matter Genesis Io moon surface. Tile as ground material
and blend with other Io sets using Mask B (height/blend) and Mask G (cavity).

Resolution: 2048 x 2048
Format: RGB PNG, seamlessly tileable (periodic noise / wrapping Worley)
Normal convention: tangent-space, OpenGL Y+ (Unity-typical)
  R = X (right), G = Y (up), B = Z (out of surface)

Mask channels
-------------
  R  roughness-ish detail / micro variation
     Higher on sulfur frost, orange crust, cracks; lower on glassy basalt.
  G  AO-ish cavities
     Bright = exposed / open crust. Dark = grooves, cell seams, pits.
     Use to darken blends or as a cavity map.
  B  blend / height hint
     Raised mounds and plateaus are brighter.
     Drive height-based layer blending / coverage of overlay terrains.

Color language
--------------
Slightly alien residue overlays (Io-plausible exaggeration): iridescent green-yellow films pooling in cavities and seams over basalt/sulfur.

Generator seed: 20269914
