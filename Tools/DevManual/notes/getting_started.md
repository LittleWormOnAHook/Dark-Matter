# Getting Started

- **Genesis Studio:** `Tools/Dark Matter Genesis/Genesis Studio` (`Assets/_Project/Editor/GenesisStudio/GenesisStudioWindow.cs`). Navigation (categories → tabs → asset paths) is defined in `DMStudioRegistry.cs`; adding a tab there makes it appear here on the next generation.
- **All project menus** live under **`Tools/Dark Matter Genesis/`** (normalised 2026-10-25a). Rock tools are in `.../PCG Rocks/`, carve tools in `.../Carve/`. Context menus stay under `GameObject/…`.
- **Profiles** are ScriptableObjects under `Assets/_Project/Resources/**`. Tabs with *Play Mode Save* keep the edits you make in Play.
- **Regenerate this manual:** `python Tools/DevManual/gen_devmanual.py`. Hand-written notes are in `Tools/DevManual/notes/*.md`.
- **Entry template:** What it does → Where (Studio / asset / menu / script) → Key settings (field, default, range, notes) → Presets → Gotchas.
