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
using Project.Inventory;
using UnityEngine.Events;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

namespace Project.EditorTools.Audit
{
    public static partial class DMEnemyRuntimeAudit
    {
        // =============================================================== silent failures
        // Things a player notices as "wrong" without any error: missing scripts, error-pink materials, broken animator/avatar.
        private static void SilentChecks(Live L, DMAuditEnemyResult R)
        {
            try
            {
                GameObject go = L.go;
                int missing = 0;
                foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
                    missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                R.Add("Silent", "silent.missingscripts", "No missing scripts on the spawned enemy", missing == 0 ? "PASS" : "FAIL", missing + " missing MonoBehaviour reference(s)",
                    missing == 0 ? "" : "prefab references a deleted/renamed script: that behaviour silently does nothing (effects, sounds, AI parts). Re-add or remove the component on the prefab");

                var pink = new List<string>();
                foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
                {
                    if (r == null) continue;
                    foreach (Material m in r.sharedMaterials)
                    {
                        if (m == null) { pink.Add(r.name + ":null material"); break; }
                        if (m.shader == null || m.shader.name.IndexOf("InternalError", StringComparison.OrdinalIgnoreCase) >= 0) { pink.Add(r.name + ":" + m.name); break; }
                    }
                }

                R.Add("Silent", "silent.materials", "No error-pink / null materials", pink.Count == 0 ? "PASS" : "FAIL", pink.Count + " renderer(s) with broken material",
                    pink.Count == 0 ? "" : "broken visuals: " + string.Join(", ", pink.Take(5)) + " (shader missing for the HDRP pipeline or material slot empty)");

                Animator a = L.anim;
                var bad = new List<string>();
                if (a == null) bad.Add("no Animator");
                else
                {
                    if (a.runtimeAnimatorController == null) bad.Add("no animator controller");
                    if (a.avatar == null) bad.Add("no avatar");
                    else if (!a.avatar.isValid) bad.Add("avatar invalid");
                    else if (!a.isHuman) bad.Add("avatar not humanoid");
                    if (a.cullingMode != AnimatorCullingMode.AlwaysAnimate) bad.Add("cullingMode " + a.cullingMode + " (animations/events stop when off-screen)");
                }

                R.Add("Silent", "silent.animator", "Animator controller + humanoid avatar valid", bad.Count == 0 ? "PASS" : "FAIL", bad.Count == 0 ? "ok" : string.Join("; ", bad),
                    bad.Count == 0 ? "" : "T-pose/no animation, frozen attack events, or enemy that only acts while visible");

                var hb = go.GetComponentsInChildren<Collider>(true);
                int hitLayer = hb.Count(c => c != null && c.gameObject.layer == 26);
                R.Add("Silent", "silent.hitboxes", "Body hitbox rig present (layer 26)", hitLayer > 0 ? "PASS" : "WARN", $"{hitLayer} collider(s) on the hitbox layer, {hb.Length} colliders total",
                    hitLayer > 0 ? "" : "no per-bone hitboxes: hits resolve against the capsule only (marks stick to the capsule, no limb damage)");
            }
            catch (Exception e)
            {
                R.Add("Silent", "silent.error", "Silent-failure checks", "WARN", e.GetType().Name + ": " + e.Message, "");
            }
        }
    }
}
#endif
