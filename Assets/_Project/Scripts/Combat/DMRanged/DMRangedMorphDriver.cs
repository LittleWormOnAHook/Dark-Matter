// 2026-10-28 (a): DM Ranged morphing weapon. One mesh (DM Ranged 1.fbx), one legacy clip, one ranged slot,
// five forms that each behave as a different weapon (name, HUD label, stats, sound).
using System;
using System.Collections.Generic;
using System.Reflection;
using Project.Data;
using Project.Inventory;
using UnityEngine;

namespace Project.Combat.DMRanged
{
    /// <summary>
    /// Added at runtime by the weapon bridge to "DM Ranged*" visuals that carry a legacy Animation.
    /// Never touches the instance's own transform; scrubs the clip at speed 0.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DMRangedMorphDriver : MonoBehaviour
    {
        const float Fps = 30f;
        Animation anim;
        AnimationState state;
        bool holsterInstance;
        float frame;
        float targetFrame;
        int targetIndex;
        int currentIndex;
        EquipmentController equipment;
        Transform[] muzzles;
        Component shooterTarget;
        Dictionary<FieldInfo, object> shooterSnapshot;

        public bool IsMorphing => Mathf.Abs(frame - targetFrame) > 0.01f;
        public int CurrentFormIndex => currentIndex;
        public ItemData DrawnItem => equipment != null && equipment.IsWeaponDrawn ? equipment.DrawnWeaponItem : null;
        public bool IsDrawnDMRanged => !holsterInstance && DMRangedMorphRuntime.IsDMRanged(DrawnItem);
        public bool IsLiveFor(ItemData item) => !holsterInstance && item != null && DrawnItem == item;
        public bool IsHolsterInstance => holsterInstance;
        /// <summary>2026-10-28e: authored per-form muzzle name (edit-mode child; Studio > DM Ranged > Create/Find Muzzle Points).</summary>
        public const string MuzzlePrefix = "DMR_Muzzle_";
        public Transform CurrentMuzzle => muzzles != null && currentIndex >= 0 && currentIndex < muzzles.Length ? muzzles[currentIndex] : null;

        DM_RangedMorphSettings S => DM_RangedMorphSettings.Instance;

        void Awake()
        {
            anim = GetComponent<Animation>();
            for (Transform p = transform.parent; p != null; p = p.parent)
                if (p.name.StartsWith("Holstered_", StringComparison.Ordinal)) { holsterInstance = true; break; }
            if (anim != null)
            {
                anim.playAutomatically = false;
                anim.Stop();
                foreach (AnimationState s in anim) { state = s; if (anim.clip != null && s.clip == anim.clip) break; }
            }
            if (!holsterInstance) BuildMuzzles();
        }

        void OnEnable()
        {
            equipment = FindEquipment();
            int holster = DMRangedMorphRuntime.RestIndex; // 2026-10-28g: rest in the kept form (Pistol 1 before the first draw)
            frame = FrameOf(holster);
            currentIndex = holster;
            targetIndex = holster;
            targetFrame = frame;
            Sample(frame);
            if (holsterInstance) return;
            DMRangedMorphRuntime.Register(this);
            if (IsDrawnDMRanged) BeginDraw();
        }

        void OnDisable()
        {
            RestoreShooter();
            if (!holsterInstance)
            {
                frame = targetFrame = FrameOf(DMRangedMorphRuntime.RestIndex); // 28g
                Sample(frame);
            }
            DMRangedMorphRuntime.Unregister(this);
        }

        float HolsterFrame => S != null ? S.holsterFrame : 40f;
        float FrameOf(int i) => S != null && i >= 0 && i < S.FormCount ? S.forms[i].frame : HolsterFrame;

        bool drawnApplied;
        void BeginDraw()
        {
            drawnApplied = true;
            ApplyShooterFromPistol();
            // 2026-10-28g: draw always starts in Pistol 1 (holster frame, no morph) unless the setting is off.
            // 2026-10-28g: first draw after pickup / new game = Pistol 1 (no morph); later draws morph 40 -> the form it was holstered in.
            if (S != null && S.drawStartsInPistol1 && DMRangedMorphRuntime.FirstDrawPending)
            {
                DMRangedMorphRuntime.LastFormIndex = S.HolsterIndex;
                DMRangedMorphRuntime.FirstDrawPending = false;
                SnapTo(S.HolsterIndex);
            }
            else Retarget(DMRangedMorphRuntime.LastFormIndex);
            DMRangedMorphRuntime.RaiseFormChanged();
        }

        void SnapTo(int index)
        {
            Retarget(index);
            frame = targetFrame; currentIndex = index;
            Sample(frame);
            ApplyMuzzle();
        }

        public void Cycle(int dir)
        {
            if (S == null || S.FormCount == 0 || holsterInstance) return;
            int n = S.FormCount;
            int next = ((targetIndex + (dir >= 0 ? 1 : -1)) % n + n) % n;
            DMRangedMorphRuntime.LastFormIndex = next;
            PlayStepSound();
            Retarget(next);
            DMRangedMorphRuntime.RaiseFormChanged(); // re-press mid-morph retargets from the current time
        }

        AudioSource stepSource;

        // 2026-10-28b: one mechanical transition sound per form step, slight pitch jitter.
        void PlayStepSound()
        {
            if (S == null || S.stepClip == null || S.stepVolume <= 0f) return;
            if (stepSource == null)
            {
                // Dedicated source on the weapon (added component only) so jitter never bends the gun's shot pitch;
                // routing/3D settings are copied from the weapon's own AudioSource when it has one.
                AudioSource weaponSrc = GetComponentInChildren<AudioSource>(true);
                if (weaponSrc == null) weaponSrc = GetComponentInParent<AudioSource>();
                stepSource = gameObject.AddComponent<AudioSource>();
                stepSource.playOnAwake = false;
                stepSource.spatialBlend = weaponSrc != null ? weaponSrc.spatialBlend : 1f;
                if (weaponSrc != null) { stepSource.outputAudioMixerGroup = weaponSrc.outputAudioMixerGroup; stepSource.minDistance = weaponSrc.minDistance; stepSource.maxDistance = weaponSrc.maxDistance; stepSource.rolloffMode = weaponSrc.rolloffMode; }
            }
            stepSource.pitch = 1f + UnityEngine.Random.Range(-S.stepPitchJitter, S.stepPitchJitter);
            stepSource.PlayOneShot(S.stepClip, S.stepVolume);
        }

        void Retarget(int index)
        {
            targetIndex = index;
            targetFrame = FrameOf(index);
        }

        void Update()
        {
            if (S == null || anim == null) return;
            if (holsterInstance) { float rf = FrameOf(DMRangedMorphRuntime.RestIndex); if (!Mathf.Approximately(frame, rf)) { frame = rf; Sample(frame); } return; } // holster instance rests at frame 40

            bool drawnDM = IsDrawnDMRanged;
            if (drawnDM && !drawnApplied) BeginDraw();
            if (!drawnDM)
            {
                drawnApplied = false;
                if (shooterSnapshot != null && equipment != null && equipment.IsWeaponDrawn) RestoreShooter();
                Retarget(DMRangedMorphRuntime.RestIndex); // holster morphs back to frame 40; last form stays remembered
            }

            if (drawnDM) { UpdatePose(); UpdateFireMode(); }
            if (!IsMorphing) return;
            float framesPerSecond = 10f / Mathf.Max(0.01f, S.stepSeconds); // one form (10 frames) per step
            frame = Mathf.MoveTowards(frame, targetFrame, framesPerSecond * Time.deltaTime);
            Sample(frame);
            int prevIndex = currentIndex;
            if (!IsMorphing) { currentIndex = targetIndex; ApplyMuzzle(); }
            else currentIndex = NearestIndex(frame);
            if (drawnDM) UpdatePose();
            if (currentIndex != prevIndex) { ApplyMuzzle(); DMRangedMorphRuntime.RaiseFormChanged(); }
        }

        int NearestIndex(float f)
        {
            int best = 0; float d = float.MaxValue;
            for (int i = 0; i < S.FormCount; i++) { float x = Mathf.Abs(S.forms[i].frame - f); if (x < d) { d = x; best = i; } }
            return best;
        }

        void Sample(float f)
        {
            if (anim == null || state == null) return;
            state.enabled = true;
            state.weight = 1f;
            state.speed = 0f;
            state.time = Mathf.Clamp(f / Fps, 0f, state.length);
            anim.Sample();
            state.enabled = false;
        }

        EquipmentController FindEquipment()
        {
            var e = GetComponentInParent<EquipmentController>(true);
            if (e == null) e = transform.root.GetComponentInChildren<EquipmentController>(true);
            return e;
        }

        // ---- per-form muzzle anchors following the animated barrel ----
        void BuildMuzzles()
        {
            var s = S;
            if (s == null) return;
            muzzles = FindAuthoredMuzzles();
            if (Array.TrueForAll(muzzles, m => m != null) || anim == null || state == null) return;
            var renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;

            Sample(HolsterFrame);
            Bounds lb = new Bounds();
            bool init = false;
            foreach (var r in renderers)
            {
                if (r is ParticleSystemRenderer) continue;
                Vector3 c = transform.InverseTransformPoint(r.bounds.center);
                Vector3 e = transform.InverseTransformVector(r.bounds.extents);
                e = new Vector3(Mathf.Abs(e.x), Mathf.Abs(e.y), Mathf.Abs(e.z));
                var b = new Bounds(c, e * 2f);
                if (!init) { lb = b; init = true; } else lb.Encapsulate(b);
            }
            Vector3 sz = lb.size;
            Vector3 axisL = sz.x >= sz.y && sz.x >= sz.z ? Vector3.right : sz.y >= sz.z ? Vector3.up : Vector3.forward;
            Transform refMuzzle = FindSiblingMuzzle();
            if (refMuzzle != null)
            {
                Vector3 toM = transform.InverseTransformPoint(refMuzzle.position) - lb.center;
                if (Vector3.Dot(toM, axisL) < 0f) axisL = -axisL;
            }

            muzzles = FindAuthoredMuzzles();
            for (int i = 0; i < s.FormCount; i++)
            {
                if (muzzles[i] != null) continue; // 2026-10-28e: authored muzzle used as-is, never repositioned
                Sample(s.forms[i].frame);
                Vector3 axisW = transform.TransformDirection(axisL).normalized;
                Renderer best = null; float bestProj = float.MinValue; Vector3 bestCenter = Vector3.zero;
                foreach (var r in renderers)
                {
                    if (r == null || r is ParticleSystemRenderer || !r.gameObject.activeInHierarchy) continue;
                    Bounds b = r.bounds;
                    float proj = Vector3.Dot(b.center, axisW) + Mathf.Abs(b.extents.x * axisW.x) + Mathf.Abs(b.extents.y * axisW.y) + Mathf.Abs(b.extents.z * axisW.z);
                    if (proj > bestProj) { bestProj = proj; best = r; bestCenter = b.center; }
                }
                if (best == null) continue;
                Vector3 p = bestCenter + axisW * (bestProj - Vector3.Dot(bestCenter, axisW));
                var go = new GameObject(MuzzlePrefix + s.forms[i].id + "_runtime");
                go.transform.SetParent(best.transform, false);
                go.transform.position = p;
                go.transform.rotation = Quaternion.LookRotation(axisW, transform.up);
                muzzles[i] = go.transform;
            }
            Sample(frame);
        }

        Transform[] FindAuthoredMuzzles()
        {
            var s = S;
            var result = new Transform[s != null ? s.FormCount : 0];
            if (s == null) return result;
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.StartsWith(MuzzlePrefix, StringComparison.Ordinal) || t.name.EndsWith("_runtime", StringComparison.Ordinal)) continue;
                string id = t.name.Substring(MuzzlePrefix.Length);
                for (int i = 0; i < s.FormCount; i++)
                    if (result[i] == null && string.Equals(s.forms[i].id, id, StringComparison.OrdinalIgnoreCase)) result[i] = t;
            }
            return result;
        }

        Transform FindSiblingMuzzle()
        {
            for (Transform p = transform.parent; p != null; p = p.parent)
            {
                foreach (Transform c in p.GetComponentsInChildren<Transform>(true))
                    if (c.name == "Muzzle" && !c.IsChildOf(transform)) return c;
                if (p.name.StartsWith("Drawn_", StringComparison.Ordinal)) break;
            }
            return null;
        }

        void ApplyMuzzle()
        {
            Transform m = CurrentMuzzle;
            if (m == null || shooterTarget == null) return;
            var f = shooterTarget.GetType().GetField("muzzle", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f == null || f.FieldType != typeof(Transform)) return;
            // 2026-10-28e: remember the slot's own muzzle once so the rifle gets it back when DM Ranged is put away.
            if (!originalMuzzleSaved) { originalMuzzle = f.GetValue(shooterTarget) as Transform; originalMuzzleSaved = true; }
            f.SetValue(shooterTarget, m);
        }

        // ---- reuse Sci_fi_pistol's vShooterWeapon code/settings on this slot ----
        void ApplyShooterFromPistol()
        {
            shooterTarget = FindShooter(transform.parent, true);
            if (shooterTarget == null) return;
            Transform root = equipment != null ? equipment.transform.root : transform.root;
            Transform src = FindDeep(root, "Drawn_Sci_Fi_Pistol");
            Component source = src != null ? FindShooter(src, false) : null;
            if (source != null && source != shooterTarget && source.GetType() == shooterTarget.GetType())
            {
                shooterSnapshot = new Dictionary<FieldInfo, object>();
                foreach (var fi in CopyableFields(shooterTarget.GetType()))
                {
                    shooterSnapshot[fi] = fi.GetValue(shooterTarget);
                    fi.SetValue(shooterTarget, fi.GetValue(source));
                }
            }
            ApplyMuzzle();
            CapturePose();
        }

        // ---- 2026-10-28c: rifle hold/aim for the Rifle form (frame 0; renamed from Scoped Rifle in 28i) ----
        // The drawn slot is Drawn_Survival_Rifle (Rifle, upperBodyID 2, leftHandIK on the rifle grip). Pistol forms use the
        // Sci-fi pistol pose (Pistol, 1). The rifle-scope form restores the slot's own rifle pose: weaponCategory drives
        // vShooterManager IK adjust (UpdateWeaponIK), upperBodyID drives the Animator UpperBody_ID (Invector blends it).
        // Switch at the START of a morph toward the rifle form and at the END of a morph away from it, so the pose
        // change is hidden under the morph. handIKTarget is never touched (it stays the slot's rifle leftHandIK).
        bool rifleApplied;
        string pistolCategory, rifleCategory; float pistolUpperBody, rifleUpperBody; bool poseCaptured;
        FieldInfo catField, ubField;

        bool FormIsRifle(int i) => S != null && i >= 0 && i < S.FormCount && S.forms[i].useRifleScope;

        void CapturePose()
        {
            poseCaptured = false; rifleApplied = false;
            if (shooterTarget == null || shooterSnapshot == null) return;
            catField = ubField = null;
            foreach (var fi in shooterSnapshot.Keys)
            {
                if (fi.Name == "weaponCategory") catField = fi;
                else if (fi.Name == "upperBodyID") ubField = fi;
            }
            if (catField == null || ubField == null) return;
            rifleCategory = shooterSnapshot[catField] as string;
            rifleUpperBody = Convert.ToSingle(shooterSnapshot[ubField]);
            pistolCategory = catField.GetValue(shooterTarget) as string;
            pistolUpperBody = Convert.ToSingle(ubField.GetValue(shooterTarget));
            if (string.Equals(rifleCategory, "Pistol", StringComparison.OrdinalIgnoreCase)) { rifleCategory = "Rifle"; rifleUpperBody = 2f; }
            poseCaptured = true;
        }

        void UpdatePose()
        {
            if (!poseCaptured) return;
            bool wantRifle = FormIsRifle(targetIndex) || (rifleApplied && IsMorphing); // on early, off late
            if (wantRifle != rifleApplied) SetPose(wantRifle);
        }

        void SetPose(bool rifle)
        {
            rifleApplied = rifle;
            catField.SetValue(shooterTarget, rifle ? rifleCategory : pistolCategory);
            ubField.SetValue(shooterTarget, ubField.FieldType == typeof(int) ? (object)(int)(rifle ? rifleUpperBody : pistolUpperBody) : (rifle ? rifleUpperBody : pistolUpperBody));
            Transform root = equipment != null ? equipment.transform.root : transform.root;
            foreach (var c in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (c == null) continue;
                for (Type ty = c.GetType(); ty != null && ty != typeof(MonoBehaviour); ty = ty.BaseType)
                    if (ty.Name == "vShooterManager")
                    {
                        c.GetType().GetMethod("UpdateWeaponIK", BindingFlags.Instance | BindingFlags.Public)?.Invoke(c, null);
                        return;
                    }
            }
        }

        // 2026-10-28h: per-form fire mode. Automatic forms (Rifle, SMR) = old Survival rifle (hold full auto, tap semi);
        // scoped (Scope mode) is always single shot. The pistol snapshot restores the original value on holster.
        FieldInfo autoField; bool autoFieldSearched;
        void UpdateFireMode()
        {
            if (shooterTarget == null || shooterSnapshot == null || S == null) return;
            if (!autoFieldSearched)
            {
                autoFieldSearched = true;
                for (Type ty = shooterTarget.GetType(); ty != null && autoField == null; ty = ty.BaseType)
                    autoField = ty.GetField("automaticWeapon", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            }
            if (autoField == null || autoField.FieldType != typeof(bool)) return;
            int i = currentIndex;
            bool want = i >= 0 && i < S.FormCount && S.forms[i].automatic && !Project.Combat.DMScopeMode.IsScoped;
            if ((bool)autoField.GetValue(shooterTarget) != want) autoField.SetValue(shooterTarget, want);
        }

        Transform originalMuzzle; bool originalMuzzleSaved;

        void RestoreShooter()
        {
            if (originalMuzzleSaved && shooterTarget != null)
            {
                var mf = shooterTarget.GetType().GetField("muzzle", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (mf != null) mf.SetValue(shooterTarget, originalMuzzle);
            }
            originalMuzzleSaved = false;
            poseCaptured = false; rifleApplied = false;
            if (shooterSnapshot != null && shooterTarget != null)
                foreach (var kv in shooterSnapshot) kv.Key.SetValue(shooterTarget, kv.Value);
            shooterSnapshot = null;
        }

        static Component FindShooter(Transform from, bool upwards)
        {
            for (Transform p = from; p != null; p = upwards ? p.parent : null)
            {
                foreach (var c in p.GetComponents<Component>())
                    if (c != null && c.GetType().Name == "vShooterWeapon") return c;
                if (!upwards) break;
            }
            return null;
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        static IEnumerable<FieldInfo> CopyableFields(Type t)
        {
            for (; t != null && t != typeof(MonoBehaviour); t = t.BaseType)
            {
                foreach (var fi in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (fi.IsInitOnly || fi.IsLiteral) continue;
                    bool serial = fi.IsPublic || fi.GetCustomAttribute<SerializeField>() != null;
                    if (!serial) continue;
                    Type ft = fi.FieldType;
                    bool ok = ft.IsPrimitive || ft.IsEnum || ft == typeof(string) || ft == typeof(Vector2) || ft == typeof(Vector3)
                        || ft == typeof(Color) || ft == typeof(AudioClip) || ft == typeof(AnimationCurve);
                    if (ok) yield return fi;
                }
            }
        }
    }
}
