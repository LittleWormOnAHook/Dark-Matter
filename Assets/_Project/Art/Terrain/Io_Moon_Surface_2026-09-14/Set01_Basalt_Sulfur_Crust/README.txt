Set name: Set01_Basalt_Sulfur_Crust
Date: 2026-09-14
Family: Io_Moon_Surface (new folder; does not replace Io_Moon_Surface from 2026-09-12)

Intended use
------------
Base terrain layer for Dark Matter Genesis Io moon surface. Tile this as the
primary ground material and blend later sulfur-plain / patera / silicate sets
on top using the Mask B (height/blend) and Mask G (cavity) channels.

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
     Raised pahoehoe mounds and sulfur plateaus are brighter.
     Drive height-based layer blending / coverage of overlay terrains.

Color language
--------------
Hyper-real Io: dark basaltic pahoehoe pillows, burnt-orange oxidized crust,
saturated sulfur yellows (green-yellow allotrope patches, hot orange-yellow
stains). Structure is cooled / bubbly pahoehoe, not cracked-earth voronoi.

Generator seed: 20260914
