# Unity Safe Mode recovery (World Engine Run 1)

Unity enters **Safe Mode** when scripts fail to compile. After pulling this branch, use this checklist.

## 1. Prefer git Features over local ChatGPT stubs

If you previously created incomplete `Features/GameState`, `WorldState`, `Directors`, etc. locally (outside git), they can conflict with this Run 1 implementation.

1. Close Unity.
2. From the project root, ensure you are on `cursor/world-engine-docs-honesty-782b` (or merged `main` once PR lands).
3. Delete any **local-only** duplicate Feature folders that are not from git, then:
   ```
   git checkout -- Assets/_Project/Features
   git clean -fd Assets/_Project/Features
   ```
4. Reopen Unity and let it reimport.

## 2. Expected assemblies after reimport

Console should show (after play or domain reload):

- `Project.Features.GameState`
- `Project.Features.WorldState`
- `Project.Features.Directors`
- `Project.Features.Validation`

Adapters under `Features/*/Adapters/` compile into **Assembly-CSharp** (no asmdef on Adapters folders).

## 3. Smoke (Play Mode, Pioneer scene)

After `CompanionSystemsBootstrap` runs:

- **F9** — `[WorldState]` one-line summary  
- **F10** — `[Directors] trigger=ManualDebug directors=7`  
- **F11** — cycles storm phase → crisis HUD  

## 4. If Safe Mode persists

Open the Safe Mode console, copy the **first** `error CS****` line, and fix that file. Common causes:

- Duplicate class names from old ChatGPT stubs still on disk
- Missing `.meta` / broken asmdef references (reimport Assets/_Project/Features)
- Mixing Input Manager-only code (this project uses Input System — smoke uses `Keyboard.current`)
