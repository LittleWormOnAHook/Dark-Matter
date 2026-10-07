# Combat — Moving melee layer policy

**Wiki page:** `Combat-Moving-Melee-Layer-Policy`  
**Repo canonical copy:** `Assets/_Project/Documentation/Design/Combat/DM_Melee_Locomotion_Layer_Policy.md`  
**Also in:** `DM_Melee_Animation_Library_Plan.md` **§10**

When editing the [GitHub wiki](https://github.com/LittleWormOnAHook/Dark-Matter/wiki), paste or sync from the canonical file above so disk truth and wiki stay aligned.

---

## Summary

Phase 2 Jetpack melee uses **Base** for locomotion only, **UpperBody** for moving / Hold E + LMB combos that must not hip-sink, and **FullBody** for standing and hybrid moving heavies (light tap, charge hold, charged release A/B/C). **`DM_MeleeUpper` is removed — do not bring it back.** Prior foot-slide and “attacks not playing” issues were bad layer weights, Base-layer crossfades, and missing exits — not proof that moving melee is impossible.

**Strong B/C clips:** Axe Standing Melee Combo Ver. 1 / Axe Standing Melee Attack 360 Low. **Parry:** RightHand@Parry01 / Parry01_Hit. **Charged release:** weighted random A/B/C (favor A); A/C hit earlier, B later.

**Agents:** `.cursor/rules/dark-matter-genesis-studio-system-edit-recall.mdc` + `unity-agent-workflow.mdc` before/after Combat Studio and controller edits.

Full tables, failure modes, and verification checklist → **canonical file** in repo.
