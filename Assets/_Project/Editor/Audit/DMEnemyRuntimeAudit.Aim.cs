#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Invector.vCamera;
using Project.AI;
using Project.Combat;
using Project.Data;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Project.EditorTools.Audit
{
    public static partial class DMEnemyRuntimeAudit
    {
        // =============================================================== aim tracking + body-part targets
        // Closed-loop aim: every frame the free-look camera (which is the player's crosshair while aiming) is nudged until the target bone
        // sits at screen centre, so the aim follows a MOVING enemy the way a player's mouse would. Targets: chest (centre mass), head, arms, legs.
        private static float CamNum(vThirdPersonCamera c, string name)
        {
            object v = Mem(c, name);
            return v is float f ? f : 0f;
        }

        /// <summary>One tracking step toward `target` (world). Returns the remaining angular error in degrees.</summary>
        private static float TrackAim(Vector3 target)
        {
            try
            {
                vThirdPersonCamera c = TpCam();
                Camera cam = Camera.main;
                if (c == null || cam == null) return 999f;
                Vector3 dir = target - cam.transform.position;
                Vector3 flatCam = cam.transform.forward; flatCam.y = 0f;
                Vector3 flatDir = dir; flatDir.y = 0f;
                if (flatCam.sqrMagnitude < 1e-4f || flatDir.sqrMagnitude < 1e-4f) return 999f;
                float yawErr = Vector3.SignedAngle(flatCam, flatDir, Vector3.up);
                float desiredDown = -Mathf.Asin(Mathf.Clamp(dir.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;
                float camDown = Mathf.DeltaAngle(0f, cam.transform.eulerAngles.x);
                float pitchErr = desiredDown - camDown;
                float yaw = CamNum(c, "mouseX"), pitch = CamNum(c, "mouseY");
                SetCamAngles(c, yaw + yawErr * 0.6f, pitch + pitchErr * 0.6f);
                return Mathf.Sqrt(yawErr * yawErr + pitchErr * pitchErr);
            }
            catch { return 999f; }
        }

        /// <summary>Re-seat the player only if it was pushed off its spot (a teleport every swing cancels the attack animation).</summary>
        private static void KeepAt(Vector3 pos, Vector3 lookAt)
        {
            if (_player == null) return;
            Vector3 d = _player.transform.position - pos; d.y = 0f;
            if (d.sqrMagnitude > 0.35f * 0.35f) PlacePlayer(pos, lookAt);
        }

        /// <summary>Hold RMB (aim), track the enemy's chest with the camera the whole time, click left, keep tracking through the gap.</summary>
        private static IEnumerator AimedShot(Live L, float aimWait, float hold, float gap)
        {
            Animator an = L.go != null ? L.go.GetComponentInChildren<Animator>() : null;
            Transform bone = AimBone(an, "chest") ?? (L.go != null ? L.go.transform : null);
            SetMouse(MouseRight);
            float t = Time.time + aimWait;
            while (Time.time < t) { if (bone != null) TrackAim(bone.position + (bone == L.go.transform ? Vector3.up * 1.2f : Vector3.zero)); yield return null; }
            SetMouse((ushort)(MouseRight | MouseLeft));
            t = Time.time + hold;
            while (Time.time < t) { if (bone != null) TrackAim(bone.position + (bone == L.go.transform ? Vector3.up * 1.2f : Vector3.zero)); yield return null; }
            SetMouse(MouseRight);
            t = Time.time + gap;
            while (Time.time < t) { if (bone != null) TrackAim(bone.position + (bone == L.go.transform ? Vector3.up * 1.2f : Vector3.zero)); yield return null; }
        }
        private static void RefillEnemyHp(EnemyHealth h)
        {
            try { FieldInfo f = typeof(EnemyHealth).GetField("currentHealth", BFA); if (f != null && h != null && !h.IsDead) f.SetValue(h, h.MaxHealth); }
            catch { /* best effort */ }
        }

        private static Transform AimBone(Animator a, string part)
        {
            if (a == null || !a.isHuman) return null;
            switch (part)
            {
                case "chest": return a.GetBoneTransform(HumanBodyBones.UpperChest) ?? a.GetBoneTransform(HumanBodyBones.Chest) ?? a.GetBoneTransform(HumanBodyBones.Spine);
                case "head": return a.GetBoneTransform(HumanBodyBones.Head);
                case "arm": return a.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                case "leg": return a.GetBoneTransform(HumanBodyBones.RightUpperLeg);
                case "foot": return a.GetBoneTransform(HumanBodyBones.LeftFoot);
            }

            return null;
        }

        /// <summary>The DM hitbox under the camera centre ray (what the crosshair is on): zone + bone name.</summary>
        private static string CrosshairHitbox(out string zone)
        {
            zone = "none";
            Camera cam = Camera.main;
            if (cam == null) return "no camera";
            Physics.SyncTransforms();
            Ray r = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(r, out RaycastHit h, 60f, DMEnemyHitQuery.HitboxMask, QueryTriggerInteraction.Collide)
                && DMEnemyHitQuery.TryGetHitbox(h.collider, out DMEnemyHitbox hb))
            {
                zone = hb.Zone.ToString();
                return hb.Bone != null ? hb.Bone.name : h.collider.name;
            }

            return "none";
        }

        private static HashSet<Transform> MarkRoots(Live L)
        {
            var set = new HashSet<Transform>();
            if (L == null || L.go == null) return set;
            foreach (Transform t in L.go.GetComponentsInChildren<Transform>(true))
                if (t != null && t.name.StartsWith("DM_HitMark_", StringComparison.Ordinal) && t.gameObject.activeInHierarchy) set.Add(t);
            return set;
        }

        private static IEnumerator ScnAim(GameObject prefab, DMAuditEnemyResult R)
        {
            ItemData weapon = AllWeaponItems().FirstOrDefault(w => w.IsRangedWeapon && !w.isMiningTool && w.name == "sci_fi_pistol")
                              ?? AllWeaponItems().FirstOrDefault(w => w.IsRangedWeapon && !w.isMiningTool && w.defaultAmmoItem != null);
            if (weapon == null) { R.Add("Aim", "aim.setup", "Ranged weapon available", "WARN", "no ranged weapon item", ""); yield break; }
            Cleanup();
            Live L = SpawnMatrixEnemy(prefab, "aim");
            yield return SpawnReady(L);
            L.ai.enabled = false; L.trackPlayer = false;
            Animator anim = L.go.GetComponentInChildren<Animator>();
            yield return EquipAfterTeleport(weapon, Ground(L.go.transform.position + FlatForward(L.go.transform) * 7f), L.go.transform.position);
            string err = EquippedOk(weapon) ? "" : "could not equip after teleport";
            if (err.Length > 0) { R.Add("Aim", "aim.setup", "Player equips the weapon", "FAIL", err, ""); yield break; }
            ItemData ammo = weapon.defaultAmmoItem;
            if (ammo != null) { LoadAmmoLikeHotCross(weapon, ammo, GiveAmmo(ammo, 120)); FillClip(weapon); }
            yield return new WaitGame(0.4f);

            _camFollow = false;
            Vector3 center = L.go.transform.position;
            Vector3 right = Vector3.Cross(Vector3.up, FlatForward(L.go.transform)).normalized;
            Vector3 pPos = Ground(center + FlatForward(L.go.transform) * 7f);
            PlacePlayer(pPos, center);
            CameraBehind();
            yield return new WaitGame(0.8f);

            string[] parts = { "chest", "head", "arm", "leg", "foot" };
            float chestDmg = -1f;
            var table = new List<string>();
            float clock = 0f;
            foreach (string part in parts)
            {
                Transform bone = AimBone(anim, part);
                if (bone == null) { R.Add("Aim", "aim." + part, $"Aim at {part}", "WARN", "no humanoid bone for this part", "enemy Animator is not humanoid or the bone is unmapped"); continue; }
                int shots = 3, landed = 0;
                float dmgSum = 0f, errSum = 0f;
                var zones = new List<string>(); var bones = new List<string>(); var markBones = new List<string>();
                for (int s = 0; s < shots; s++)
                {
                    SetMouse(MouseRight);
                    float tEnd = Time.time + 0.9f;
                    float lastErr = 999f;
                    while (Time.time < tEnd)
                    {
                        clock += Time.deltaTime;
                        // the enemy keeps strafing, so the aim has to keep following it
                        L.go.transform.position = Ground(center + right * (Mathf.Sin(clock * 1.4f) * 1.6f));
                        Physics.SyncTransforms();
                        lastErr = TrackAim(bone.position);
                        yield return null;
                    }

                    errSum += lastErr;
                    string zone;
                    string aimedBone = CrosshairHitbox(out zone);
                    float h0 = EHp(L);
                    var marks0 = MarkRoots(L);
                    int q0 = Loaded(weapon);
                    SetMouse((ushort)(MouseRight | MouseLeft));
                    float tFire = Time.time + 0.25f;
                    while (Time.time < tFire) { L.go.transform.position = Ground(center + right * (Mathf.Sin((clock += Time.deltaTime) * 1.4f) * 1.6f)); TrackAim(bone.position); yield return null; }
                    SetMouse(MouseRight);
                    float tAfter = Time.time + 0.55f;
                    while (Time.time < tAfter) { L.go.transform.position = Ground(center + right * (Mathf.Sin((clock += Time.deltaTime) * 1.4f) * 1.6f)); TrackAim(bone.position); yield return null; }
                    float lost = h0 - EHp(L);
                    var newMarks = MarkRoots(L); newMarks.ExceptWith(marks0);
                    foreach (Transform m in newMarks) markBones.Add(m.parent != null ? m.parent.name : m.name);
                    if (Loaded(weapon) < q0) { landed++; }
                    if (lost > 0f) dmgSum += lost;
                    zones.Add(zone); bones.Add(aimedBone);
                    EnemyTopUpNow(L);
                    SetMouse(0);
                    yield return new WaitGame(0.2f);
                }

                float avgDmg = dmgSum / Mathf.Max(1, shots);
                if (part == "chest") chestDmg = avgDmg;
                float mult = chestDmg > 0.01f ? avgDmg / chestDmg : float.NaN;
                string zoneSummary = string.Join("/", zones.Distinct());
                bool onTarget = zones.Any(z => z != "none");
                table.Add($"{part}: zone {zoneSummary}, avg dmg {avgDmg:0.0}, x{(float.IsNaN(mult) ? "n/a" : mult.ToString("0.00", CultureInfo.InvariantCulture))} vs chest");
                R.Add("Aim", "aim." + part, $"Aim follows the moving enemy: {part}", onTarget && avgDmg > 0f ? "PASS" : (landed > 0 ? "WARN" : "FAIL"),
                    $"{shots} shots, {landed} fired, aim error before firing avg {errSum / shots:0.0}deg; crosshair hitbox zone {zoneSummary} (bone {string.Join("/", bones.Distinct())}); hit-mark bones {(markBones.Count == 0 ? "none" : string.Join(", ", markBones.Distinct()))}; avg damage {avgDmg:0.0} (x{(float.IsNaN(mult) ? "n/a" : mult.ToString("0.00", CultureInfo.InvariantCulture))} chest)",
                    onTarget && avgDmg > 0f ? "" : (!onTarget ? "crosshair never rested on a DM hitbox of this body part: aim tracking lagging the strafing enemy, or the enemy has no hitbox rig/bone" : "aimed correctly but no damage registered (ammo empty or projectile missed a moving target)"));
            }

            R.Add("Aim", "aim.table", "Body-part damage table (for later headshot/limb multipliers)", "INFO", string.Join(" | ", table),
                "damage multipliers per zone come from DMCombatBodyPartUtility.ApplyZoneDamageMultiplier (DM_EnemyHitMarkProfile); equal ratios here mean no zone multiplier is configured");
            SetMouse(0);
            _camFollow = true;
        }
    }
}
#endif
