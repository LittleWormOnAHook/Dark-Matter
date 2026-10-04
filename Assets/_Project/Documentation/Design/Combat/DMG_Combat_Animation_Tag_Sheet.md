# Combat animation tag sheet (P02)

Phase 1 close — Oct 3, 2026. Keyword scan of `.anim` / `.fbx` under `Assets/` (audit Sep 30, 2026).

| Tag | Approx. count | Plan use |
|-----|---------------|----------|
| attack | 321 | Melee/ranged attack clips |
| combo | 121 | Combo chains |
| heavy | 150 | Heavy attacks |
| finisher | 3 | Momentum finishers (gap: need library growth) |
| dodge / roll | 113 | Dodge i-frame validation |
| hit react | 354 | Hit reactions, poise stagger |
| block / parry | 120 | Block/parry (parry not wired) |
| stun / stagger | 5 | Poise break, stun (gap) |
| death | 127 | Death / loot workflow |
| draw / holster | 30 | Weapon swap |
| kick | 55 | Unarmed / finisher variants |
| sword | 334 | Blade branch |
| knife | 17 | Blade / Infiltrator |
| baton / club | 2 | Control branch |
| puke | 0 | Sick Stick / finisher eligibility (missing) |

**Known finisher clips:** `Human_SwordOneHand_Finisher1.fbx` (+ 2 other finisher-tagged assets).

**Next:** Skill nodes and enemy animation profiles should reference clips by stable tag + profile id, not hardcoded paths.
