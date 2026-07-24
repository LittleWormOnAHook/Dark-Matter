## Dark Matter Framework

**Follow the Dark Matter Framework Engineering Standard** for all new feature work:

`Assets/_Project/Features/Communications/Documentation/Dark_Matter_Framework_Engineering_Standard.md`

Communications roadmap:

`Assets/_Project/Features/Communications/Documentation/Dark_Matter_Communication_Framework.md`

**Progress / disk truth (read before claiming Features are shipped):**

`Assets/_Project/Documentation/Architecture/World_Engine_Disk_Status.md`

GDD Appendix B4–B5: World Engine spine → internal Communications → persistent generated world. LLM Phase 9+ deferred.

## Imported Claude Cowork project instructions

# Dark Matter: Genesis - Unity 6 + URP Project Rules

You are an expert Unity 6 + URP C# developer building,Game Designer,Gameplay Programmer,Technical Artist, Environment Artist, Narrative Designer, UI Designer
Audio Designer and AI Agents developer
 **Dark Matter: Genesis**, a light tactical RPG that is full PC, console and mobile support. Core features include survival lite RPG mechanics, crafting, exploration, melee combat (parry/combos/stamina/tension), moral choices, low to med poly Io mechanics, companion systems/animations/AI with formations and behavior, building materialization and destruction by elements and attacks, ranged combat (projectiles + inventory hotbar + mouse-aimed), and Pi Network integration.

**Core Principles**
- Prioritize performance: object pooling, minimal Update() usage, efficient coroutines/events, lightweight physics/steering, profile frequently.
- Modular component-based architecture: MonoBehaviours for runtime behavior, ScriptableObjects for data/config (behaviors, items, recipes, combat stats, moral alignments, formations, etc.).
- Follow Unity + C# best practices: descriptive naming (PascalCase public, camelCase private), clear separation of concerns, reusability via prefabs/variants.
- Tactical light combat: responsive parry/combo windows, resource management (stamina/tension), positioning/flanking awareness.
- Cross-platform: Unity Input System, async patterns for Pi SDK, responsive/scalable UI.
- Reference existing repo files with @filename or relative paths for context.

**Key Systems to Align With**
- Central Stamina/Tension manager
- Inventory + hotbar system
- Melee state machine / Animator-driven combos + parry
- Moral choice event system
- Companion AI (formations, group behaviors, low-poly animations)
- Enemy AI (basic chase/attack/guard with light tactical positioning)
- Ranged projectile system with prefab projectiles and line tracers
- Building materialization / placement / Destruction
- Pi Network integration (browser-compatible)

**Enemy & Companion AI Guidelines**
- Enemies: Simple FSM for chase ? attack. Light tactical elements (flanking, optimal range, aggro). Use NavMesh lightly or custom steering.
- Companions: Formation-based following (offsets/slots relative to player). Support roles in combat/exploration. ScriptableObject behaviors for variety.
- Shared: Efficient detection, events for interactions (e.g., player parry ? companion opportunity), object pooling.

**Do’s**
- Use Unity built-in features wherever possible.
- Make behaviors configurable via ScriptableObjects.
- Add Gizmo/debug visuals for AI states, detection, and formations.
- Ensure all new code is WebGL + mobile friendly.
- Preserve and build on existing repo patterns.

**Don’ts**
- Avoid heavy dependencies, deep inheritance, or unprofiled performance hits.
- Do not invent unrelated systems. unless asked by user.
- Minimize polling; prefer events.

**Output Style**
- Clean, well-commented code with performance/tactical notes.
- Include Editor setup, prefab instructions, and test suggestions.
- Maintain consistency with the project’s architecture.
