#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Invector;
using Invector.vCharacterController;
using Invector.vMelee;
using Invector.vShooter;
using Project.AI;
using Project.AI.Invector;
using Project.Combat;
using Project.Core;
using Project.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

namespace Project.EditorTools.Audit
{
    public static partial class DMEnemyRuntimeAudit
    {
        // =============================================================== animation
        private static readonly (string cat, string[] keys)[] ClipCats =
        {
            ("draw/equip", new[] { "draw", "equip", "unequip", "holster", "sheath", "unsheath", "pull" }),
            ("attack", new[] { "attack", "slash", "swing", "strike", "shoot", "fire", "stab" }),
            ("stagger/hit", new[] { "stagger", "hit", "hurt", "react", "flinch", "damage", "impact" }),
            ("death", new[] { "death", "die", "dead", "dying" }),
        };

        private static void RecordClips(Live L, DMAuditEnemyResult R)
        {
            if (L.anim == null || L.anim.runtimeAnimatorController == null)
            {
                R.Add("Animation", "anim", "Animation clip timings", "WARN", "no Animator/controller", "");
                return;
            }

            var seen = new HashSet<string>();
            foreach (AnimationClip c in L.anim.runtimeAnimatorController.animationClips)
            {
                if (c == null || !seen.Add(c.name)) continue;
                string lower = c.name.ToLowerInvariant();
                foreach (var cat in ClipCats)
                {
                    if (cat.keys.Any(k => lower.Contains(k)))
                    {
                        R.clips.Add(new DMAuditClip { category = cat.cat, clip = c.name, length = c.length, observed = L.clipSeen.ContainsKey(c.name) });
                        break;
                    }
                }
            }

            int obs = R.clips.Count(c => c.observed);
            string counts = string.Join(", ", ClipCats.Select(cat => cat.cat + " " + R.clips.Count(c => c.category == cat.cat)));
            bool noAttack = R.clips.Count(c => c.category == "attack") == 0;
            bool noDeath = R.clips.Count(c => c.category == "death") == 0;
            R.Add("Animation", "anim", "Animation clip timings", (noAttack || noDeath) ? "WARN" : "INFO",
                $"{counts}; {obs} clip(s) observed playing; {L.clipSeen.Count} distinct clips seen in total",
                noAttack ? "no attack-named clip in the controller" : (noDeath ? "no death-named clip in the controller" : ""));
        }

        // =============================================================== diagnostics
        private static string DescribeVisibility(Live L)
        {
            var drawn = new List<string>();
            var hol = new List<string>();
            foreach (Transform tr in L.go.GetComponentsInChildren<Transform>(false))
            {
                if (tr.name.StartsWith("Drawn_", StringComparison.Ordinal)) drawn.Add(tr.name);
                else if (tr.name.StartsWith("Holstered_", StringComparison.Ordinal)) hol.Add(tr.name);
            }

            string active = L.bridge != null && L.bridge.ActiveItem != null ? L.bridge.ActiveItem.name : "none";
            return $"active item {active}; drawn [{string.Join(",", drawn)}]; holstered [{string.Join(",", hol)}]";
        }

        private static string DescribeMeleeSetup(Live L, ItemData melee, out bool tagOk, out bool layerOk)
        {
            tagOk = true; layerOk = true;
            if (melee == null || L.mm == null) return "";
            var parts = new List<string>();
            var tags = L.mm.hitProperties != null ? L.mm.hitProperties.hitDamageTags : null;
            tagOk = tags != null && _player != null && tags.Contains(_player.tag);
            parts.Add("hitDamageTags=[" + (tags != null ? string.Join(",", tags) : "null") + "] vs player tag '" + (_player != null ? _player.tag : "?") + "'");
            GameObject drawn = L.bridge != null ? L.bridge.ActiveDrawnInstance : null;
            var w = drawn != null ? drawn.GetComponentInChildren<vMeleeWeapon>(true) : (L.mm.rightWeapon != null ? L.mm.rightWeapon : null);
            if (w == null) parts.Add("no vMeleeWeapon on the drawn/bound weapon");
            else
            {
                int hb = w.hitBoxes != null ? w.hitBoxes.Count : 0;
                parts.Add($"weapon {w.name} hitBoxes={hb}, vDamage={(w.damage != null ? w.damage.damageValue.ToString("0.#") : "null")}");
                if (w.hitBoxes != null)
                    foreach (var h in w.hitBoxes)
                    {
                        if (h == null) { parts.Add("null hitbox ref"); continue; }
                        var c = h.GetComponent<Collider>();
                        if (c == null) { parts.Add(h.name + ": no collider"); continue; }
                        if (Physics.GetIgnoreLayerCollision(h.gameObject.layer, _player != null ? _player.layer : 8)) layerOk = false;
                        parts.Add($"{h.name}: layer {LayerMask.LayerToName(h.gameObject.layer)}, trigger={c.isTrigger}, type={h.triggerType}");
                    }
            }

            return string.Join("; ", parts);
        }

        private static string DiagnoseMelee(Live L, ItemData melee, EnemyDefinition def, bool tagOk, bool layerOk)
        {
            var causes = new List<string>();
            if (melee.meleeDamage <= 0f) causes.Add($"ItemData '{melee.name}'.meleeDamage = {melee.meleeDamage} (weapon item itself deals 0)");
            if (def != null && def.attackDamage <= 0f) causes.Add("definition attackDamage = 0");
            if (L.attackStarts == 0 && L.attackStateFrames == 0)
                causes.Add($"enemy never entered an attack in 14s with the player 1.5m away (AI states {string.Join("/", L.phaseStates)}, target '{L.targetName}'; min distance {L.minDist:0.0}m)");
            else if (L.hitboxFrames == 0 && L.unarmedHitboxFrames == 0)
                causes.Add("attacks started but the weapon damage window never opened (hitbox canApplyDamage stayed false): attack animation event (ActiveDamage/OnEnableAttack) never fires or vMeleeManager.rightWeapon is not the drawn weapon" + (L.rightWeaponStaleFrames > 0 ? $" - rightWeapon was stale for {L.rightWeaponStaleFrames} samples" : ""));
            else if (L.meleeHitEvents == 0)
            {
                if (!tagOk) causes.Add("hitProperties.hitDamageTags does not contain the player's tag - melee hitboxes ignore the player");
                else if (!layerOk) causes.Add("Physics layer collision matrix ignores weapon hitbox layer vs Player layer");
                else causes.Add("damage window opened but no hit registered: hitbox collider missing/too small/not overlapping the player, or the player's receiver filters it");
            }
            else causes.Add("hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=" + L.missFrames + " frames, ItemData roll) or the player blocked/ignored it");
            if (L.missFrames > 0 && L.attackStarts > 0) causes.Add($"intentional-miss active for {L.missFrames} frames (definition missRate {(def != null ? def.missRate.ToString("0.##") : "?")})");
            return string.Join(" | ", causes);
        }

        private static string DescribeRangedSetup(Live L, ItemData ranged, EnemyDefinition def, EnemyInvectorCombatBridge cb)
        {
            var parts = new List<string>();
            var shooter = L.go.GetComponent<vShooterManager>();
            parts.Add("vShooterManager " + (shooter != null ? "present, rightWeapon=" + (shooter.rWeapon != null ? shooter.rWeapon.name : "null") : "MISSING"));
            parts.Add("ProjectileBridge " + (L.go.GetComponent<EnemyInvectorProjectileBridge>() != null ? "present" : "MISSING"));
            if (def != null) parts.Add($"def rangedEngageRange={def.rangedEngageRange:0.#}, rangedCooldown={def.rangedAttackCooldown:0.##}, missRate={def.missRate:0.##}");
            return string.Join("; ", parts);
        }

        private static string DiagnoseRanged(Live L, ItemData ranged, EnemyDefinition def, EnemyInvectorCombatBridge cb)
        {
            var causes = new List<string>();
            var shooter = L.go.GetComponent<vShooterManager>();
            if (ranged.rangedDamage <= 0f) causes.Add($"ItemData '{ranged.name}'.rangedDamage = {ranged.rangedDamage}");
            if (shooter == null) causes.Add("no vShooterManager on the enemy");
            else if (shooter.rWeapon == null) causes.Add("vShooterManager.rightWeapon is null: ranged weapon not bound/drawn");
            if (L.go.GetComponent<EnemyInvectorProjectileBridge>() == null) causes.Add("EnemyInvectorProjectileBridge missing: no projectile is spawned from onShot");
            if (L.projectileSpawns == 0)
            {
                if (L.attackStateFrames == 0 && L.attackStarts == 0)
                    causes.Add($"enemy never reached an attack state at {L.minDist:0.0}m (AI states {string.Join("/", L.phaseStates)}, target '{L.targetName}'): standoff/hold/engage-range logic");
                else causes.Add("attack state reached but no CombatProjectile spawned: weapon never fired (aim not ready, shooter weapon without ammo/clip, or onShot not raised)");
            }
            else causes.Add($"{L.projectileSpawns} projectile(s) spawned but none reduced player hp: projectile collision mask/Player layer, CombatHitResolver target filter, or damage scaled to 0 (missRate {(def != null ? def.missRate.ToString("0.##") : "?")}, intentional-miss frames {L.missFrames})");
            return string.Join(" | ", causes);
        }
    }
}
#endif
