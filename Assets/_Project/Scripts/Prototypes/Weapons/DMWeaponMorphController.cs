using System.Collections;
using Project.Data;
using Project.Interaction;
using UnityEngine;

namespace Project.Prototypes.Weapons
{
    /// <summary>
    /// Prototype morph weapon on held/world visuals. Y cycles forms while drawn.
    /// Gun: animates child part poses. Blade: swaps blade mesh/type colour and extend scale; hitbox follows length.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DMWeaponMorphController : MonoBehaviour
    {
        [SerializeField] private ItemData boundItem;
        [SerializeField] private DMWeaponMorphKind kind = DMWeaponMorphKind.Ranged;
        [SerializeField] private DM_WeaponMorphProfile[] forms = System.Array.Empty<DM_WeaponMorphProfile>();
        [SerializeField] private int formIndex;
        [SerializeField] private Transform[] parts = System.Array.Empty<Transform>();
        [SerializeField] private Transform bladeRoot;
        [SerializeField] private Renderer bladeRenderer;
        [SerializeField] private WeaponHitbox weaponHitbox;
        [Header("EAU MorphBlade (2026-10-28o)")]
        [Tooltip("Animator on the EAU model (Take 001). When set on a MeleeBlade, key 1 plays frame 0 -> swap frame, swaps the material on swapRenderer, then plays back to 0.")]
        [SerializeField] private Animator morphAnimator;
        [Tooltip("Renderer on the 'Blade' child whose material swaps per form.")]
        [SerializeField] private Renderer swapRenderer;
        [Tooltip("Blade base / tip markers (MB_Hilt / MB_BladeTip); VFX runs between them.")]
        [SerializeField] private Transform bladeBase;
        [SerializeField] private Transform bladeTip;
        private GameObject _vfxInstance;
        private GameObject _vfxPrefab;
        private AudioSource _swapAudio;
        private bool UsesAnimatorSwap => kind == DMWeaponMorphKind.MeleeBlade && morphAnimator != null && swapRenderer != null;
        private Coroutine _morphRoutine;
        private Vector3[] _partStartPos;
        private Quaternion[] _partStartRot;
        private Vector3[] _partStartScale;
        private MaterialPropertyBlock _mpb;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissiveColorId = Shader.PropertyToID("_EmissiveColor");
        private static readonly int UnlitColorId = Shader.PropertyToID("_UnlitColor");

        public ItemData BoundItem => boundItem;
        public bool IsMorphing => _morphRoutine != null;
        public DM_WeaponMorphProfile ActiveProfile =>
            forms != null && forms.Length > 0 ? forms[Mathf.Clamp(formIndex, 0, forms.Length - 1)] : null;
        public float ActiveBodyWeightScale
        {
            get
            {
                DM_WeaponMorphProfile p = ActiveProfile;
                if (p == null) return 1f;
                float w = Mathf.Max(0.1f, p.bodyWeightScale);
                // 2026-10-28m: MorphBlade weight per blade from Studio settings (x profile scale).
                if (kind == DMWeaponMorphKind.MeleeBlade && DM_WeaponMorphStudioSettings.Live != null)
                    w *= Mathf.Max(0.1f, DM_WeaponMorphStudioSettings.Live.WeightForBlade(p.bladeType));
                return w;
            }
        }
        public DM_WeaponMorphProfile[] Forms => forms;
        public int FormIndex => formIndex;
        public DMWeaponMorphKind Kind => kind;

        public void Configure(ItemData item, DMWeaponMorphKind morphKind, DM_WeaponMorphProfile[] profileList)
        {
            boundItem = item;
            kind = morphKind;
            forms = profileList ?? System.Array.Empty<DM_WeaponMorphProfile>();
            formIndex = 0;
            CacheParts();
            ApplyFormImmediate(ActiveProfile);
        }

        private void Awake()
        {
            CacheParts();
            if (weaponHitbox == null)
                weaponHitbox = GetComponentInChildren<WeaponHitbox>(true);
            _mpb = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            DMWeaponMorphRuntime.Register(this);
            // 2026-10-28m: MorphBlade on the player (Drawn_/Holstered_ copies) shares one blade form; draw/holster keep it.
            if (SharesBladeForm())
            {
                DMWeaponMorphRuntime.RegisterBladeForms(boundItem, forms);
                formIndex = Mathf.Clamp(DMWeaponMorphRuntime.SharedBladeFormIndex, 0, forms.Length - 1);
                DMWeaponMorphRuntime.BladeFormChanged -= OnSharedBladeFormChanged;
                DMWeaponMorphRuntime.BladeFormChanged += OnSharedBladeFormChanged;
            }
            ApplyFormImmediate(ActiveProfile);
            if (UsesAnimatorSwap)
                SetAnimFrame(0f); // frame 0 = extended ready sword (drawn and holstered)
        }

        bool SharesBladeForm()
        {
            if (kind != DMWeaponMorphKind.MeleeBlade || boundItem == null || forms == null || forms.Length == 0)
                return false;
            for (Transform p = transform; p != null; p = p.parent)
            {
                string n = p.name;
                if (n.StartsWith("Drawn_", System.StringComparison.Ordinal)
                    || n.StartsWith("Holstered_", System.StringComparison.Ordinal)
                    || n.StartsWith("PioneerVisual_", System.StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        void OnSharedBladeFormChanged()
        {
            if (this == null || IsMorphing || forms == null || forms.Length == 0)
                return;
            int want = Mathf.Clamp(DMWeaponMorphRuntime.SharedBladeFormIndex, 0, forms.Length - 1);
            if (want != formIndex)
                ApplyFormAtIndexImmediate(want);
        }

        private void OnDisable()
        {
            if (_morphRoutine != null)
            {
                StopCoroutine(_morphRoutine);
                _morphRoutine = null;
            }
            DMWeaponMorphRuntime.BladeFormChanged -= OnSharedBladeFormChanged;
            DMWeaponMorphRuntime.Unregister(this);
            if (_vfxInstance != null) Destroy(_vfxInstance);
            _vfxInstance = null; _vfxPrefab = null;
        }

        private void CacheParts()
        {
            if (UsesAnimatorSwap)
            {
                parts = System.Array.Empty<Transform>(); // the Animator owns the EAU transforms
                return;
            }
            if (parts != null && parts.Length > 0)
                return;

            var list = new System.Collections.Generic.List<Transform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform c = transform.GetChild(i);
                if (c == null) continue;
                if (c.name.StartsWith("Part_", System.StringComparison.Ordinal)
                    || c.name.StartsWith("Blade_", System.StringComparison.Ordinal)
                    || c.name == "Blade"
                    || c.name == "Handle"
                    || c.name == "Hilt")
                    list.Add(c);
            }
            parts = list.ToArray();
            if (bladeRoot == null)
            {
                Transform t = transform.Find("Blade");
                if (t == null) t = transform.Find("Blade_Metal");
                bladeRoot = t;
            }
            if (bladeRenderer == null && bladeRoot != null)
                bladeRenderer = bladeRoot.GetComponentInChildren<Renderer>(true);
        }

        public bool TryCycleForm()
        {
            if (forms == null || forms.Length < 2)
                return false;
            if (IsMorphing)
                return true; // consume Y while morphing, do not holster/switch

            int next = (formIndex + 1) % forms.Length;
            BeginMorph(next);
            return true;
        }

        public void BeginMorph(int nextIndex)
        {
            if (forms == null || forms.Length == 0)
                return;
            nextIndex = Mathf.Clamp(nextIndex, 0, forms.Length - 1);
            if (_morphRoutine != null)
                StopCoroutine(_morphRoutine);
            _morphRoutine = StartCoroutine(MorphRoutine(nextIndex));
        }

        private IEnumerator MorphRoutine(int nextIndex)
        {
            if (UsesAnimatorSwap)
                return AnimatorSwapRoutine(nextIndex);
            return LegacyMorphRoutine(nextIndex);
        }

        // 2026-10-28o: frame 0 -> swapFrame, swap material, swapFrame -> 0. IsMorphing stays true the whole time (input locked).
        private IEnumerator AnimatorSwapRoutine(int nextIndex)
        {
            DM_WeaponMorphProfile target = forms[nextIndex];
            DM_WeaponMorphStudioSettings studio = DM_WeaponMorphStudioSettings.Live;
            PlaySwapAudio(studio);
            float half = studio != null ? studio.swapHalfSeconds : 0.667f;
            float frame = studio != null ? studio.swapFrame : 20f;
            float t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                SetAnimFrame(Mathf.Lerp(0f, frame, Mathf.Clamp01(t / half)));
                yield return null;
            }
            formIndex = nextIndex;
            ApplyFormImmediate(target);
            if (SharesBladeForm())
                DMWeaponMorphRuntime.SetSharedBladeForm(nextIndex); // holstered copy + icons
            t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                SetAnimFrame(Mathf.Lerp(frame, 0f, Mathf.Clamp01(t / half)));
                yield return null;
            }
            SetAnimFrame(0f);
            _morphRoutine = null;
        }

        private bool IsHolsteredCopy()
        {
            for (Transform p = transform; p != null; p = p.parent)
                if (p.name.StartsWith("Holstered_", System.StringComparison.Ordinal)) return true;
            return false;
        }

        // 2026-10-28o: per-form blade VFX (Steel none). Play mode only; holstered copy off unless Studio vfxWhenHolstered.
        private void RefreshBladeVfx(DM_WeaponMorphProfile profile)
        {
            DM_WeaponMorphStudioSettings studio = DM_WeaponMorphStudioSettings.Live;
            GameObject prefab = null;
            if (Application.isPlaying && profile != null && isActiveAndEnabled)
            {
                prefab = profile.bladeVfx != null ? profile.bladeVfx : (studio != null ? studio.VfxForBlade(profile.bladeType) : null);
                if (prefab != null && IsHolsteredCopy() && (studio == null || !studio.vfxWhenHolstered))
                    prefab = null;
            }
            if (prefab == _vfxPrefab && (_vfxInstance != null || prefab == null))
                return;
            if (_vfxInstance != null)
                Destroy(_vfxInstance);
            _vfxInstance = null;
            _vfxPrefab = prefab;
            if (prefab == null)
                return;

            Transform b = bladeBase != null ? bladeBase : transform;
            Vector3 basePos = b.position;
            Vector3 tipPos = bladeTip != null ? bladeTip.position : basePos + transform.up;
            Vector3 axis = tipPos - basePos;
            float len = Mathf.Max(0.05f, axis.magnitude);
            _vfxInstance = Instantiate(prefab, transform);
            _vfxInstance.name = "BladeVFX_" + profile.bladeType;
            _vfxInstance.transform.position = basePos + axis * 0.5f;
            _vfxInstance.transform.rotation = Quaternion.LookRotation(axis / len, transform.forward);
            float k = studio != null ? studio.vfxIntensity : 1f;
            ParticleSystem[] ps = _vfxInstance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < ps.Length; i++)
            {
                ParticleSystem.ShapeModule sh = ps[i].shape;
                Vector3 sc = sh.scale; sc.z = len; sh.scale = sc;
                ParticleSystem.EmissionModule em = ps[i].emission;
                em.rateOverTimeMultiplier *= k;
                ps[i].Play(true);
            }
        }

        private void PlaySwapAudio(DM_WeaponMorphStudioSettings studio)
        {
            if (studio == null || studio.swapClip == null || !Application.isPlaying)
                return;
            if (_swapAudio == null)
            {
                _swapAudio = gameObject.AddComponent<AudioSource>();
                _swapAudio.playOnAwake = false;
                _swapAudio.spatialBlend = 1f;
                _swapAudio.minDistance = 1.5f;
                _swapAudio.maxDistance = 25f;
            }
            _swapAudio.pitch = studio.swapPitch + Random.Range(-studio.swapPitchRandom, studio.swapPitchRandom);
            _swapAudio.PlayOneShot(studio.swapClip, studio.swapVolume);
        }

        private void SetAnimFrame(float frame)
        {
            if (morphAnimator == null || morphAnimator.runtimeAnimatorController == null || !morphAnimator.isActiveAndEnabled)
                return;
            AnimationClip[] clips = morphAnimator.runtimeAnimatorController.animationClips;
            if (clips == null || clips.Length == 0 || clips[0] == null)
                return;
            DM_WeaponMorphStudioSettings studio = DM_WeaponMorphStudioSettings.Live;
            float fps = studio != null ? studio.swapClipFps : (clips[0].frameRate > 0f ? clips[0].frameRate : 30f);
            float len = Mathf.Max(0.0001f, clips[0].length);
            string state = studio != null && !string.IsNullOrEmpty(studio.swapAnimatorState) ? studio.swapAnimatorState : clips[0].name;
            morphAnimator.speed = 0f;
            morphAnimator.Play(state, 0, Mathf.Clamp01(frame / fps / len));
            morphAnimator.Update(0f);
        }

        private IEnumerator LegacyMorphRoutine(int nextIndex)
        {
            DM_WeaponMorphProfile target = forms[nextIndex];
            DM_WeaponMorphStudioSettings studio = DM_WeaponMorphStudioSettings.Live;
            float duration = studio != null ? studio.MorphDuration : 0.3f;
            AnimationCurve curve = studio != null && studio.morphCurve != null && studio.morphCurve.keys.Length > 0
                ? studio.morphCurve
                : AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

            CapturePartStarts();
            MorphPartPose[] targetPoses = ResolvePoses(target);

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float u = curve.Evaluate(Mathf.Clamp01(t / duration));
                ApplyPartLerp(targetPoses, u);
                if (kind == DMWeaponMorphKind.MeleeBlade)
                    ApplyBladeVisual(target, u);
                yield return null;
            }

            formIndex = nextIndex;
            ApplyFormImmediate(target);
            _morphRoutine = null;
            if (SharesBladeForm())
                DMWeaponMorphRuntime.SetSharedBladeForm(nextIndex); // mirrors holstered copy + refreshes icons

        }

        private void CapturePartStarts()
        {
            CacheParts();
            int n = parts != null ? parts.Length : 0;
            _partStartPos = new Vector3[n];
            _partStartRot = new Quaternion[n];
            _partStartScale = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                Transform p = parts[i];
                if (p == null) continue;
                _partStartPos[i] = p.localPosition;
                _partStartRot[i] = p.localRotation;
                _partStartScale[i] = p.localScale;
            }
        }

        private MorphPartPose[] ResolvePoses(DM_WeaponMorphProfile profile)
        {
            if (profile != null && profile.partPoses != null && profile.partPoses.Length > 0)
                return profile.partPoses;
            return System.Array.Empty<MorphPartPose>();
        }

        private void ApplyPartLerp(MorphPartPose[] targetPoses, float u)
        {
            if (parts == null) return;
            for (int i = 0; i < parts.Length; i++)
            {
                Transform p = parts[i];
                if (p == null) continue;
                if (!TryFindPose(targetPoses, p.name, out MorphPartPose pose))
                    continue;
                Vector3 toPos = pose.localPosition;
                Quaternion toRot = Quaternion.Euler(pose.localEuler);
                Vector3 toScale = pose.localScale == Vector3.zero ? Vector3.one : pose.localScale;
                p.localPosition = Vector3.LerpUnclamped(_partStartPos[i], toPos, u);
                p.localRotation = Quaternion.SlerpUnclamped(_partStartRot[i], toRot, u);
                p.localScale = Vector3.LerpUnclamped(_partStartScale[i], toScale, u);
            }
        }

        private static bool TryFindPose(MorphPartPose[] poses, string name, out MorphPartPose pose)
        {
            pose = default;
            if (poses == null) return false;
            for (int i = 0; i < poses.Length; i++)
            {
                if (poses[i].partName == name)
                {
                    pose = poses[i];
                    return true;
                }
            }
            return false;
        }

        private void ApplyFormImmediate(DM_WeaponMorphProfile profile)
        {
            if (profile == null) return;
            MorphPartPose[] poses = ResolvePoses(profile);
            if (parts != null)
            {
                for (int i = 0; i < parts.Length; i++)
                {
                    Transform p = parts[i];
                    if (p == null) continue;
                    if (!TryFindPose(poses, p.name, out MorphPartPose pose))
                        continue;
                    p.localPosition = pose.localPosition;
                    p.localRotation = Quaternion.Euler(pose.localEuler);
                    p.localScale = pose.localScale == Vector3.zero ? Vector3.one : pose.localScale;
                }
            }

            if (kind == DMWeaponMorphKind.MeleeBlade)
            {
                ApplyBladeVisual(profile, 1f);
                SyncHitboxLength(profile);
                // Soft-apply melee stats onto bound ItemData only in play mode; restored is not needed for prototype
                // because we read overrides via DMWeaponMorphRuntime for damage. Blade length field is used by hitbox rebuild.
                if (Application.isPlaying && boundItem != null)
                    boundItem.meleeBladeLength = profile.meleeBladeLength;
            }

            if (kind == DMWeaponMorphKind.Ranged && Application.isPlaying && boundItem != null)
            {
                // Grip drives DMScopeMode.IsRifle for Sniper/Rifle forms. Runtime-only; avoid dirtying asset in edit mode.
                boundItem.weaponGrip = profile.enableScopeAsRifle || profile.gunForm != DMWeaponMorphGunForm.Pistol
                    ? WeaponGrip.TwoHanded
                    : profile.ResolveGrip();
            }
        }

        private void ApplyBladeVisual(DM_WeaponMorphProfile profile, float extend01)
        {
            if (profile == null) return;
            if (UsesAnimatorSwap)
            {
                Material m = profile.bladeMaterial;
                if (m == null && DM_WeaponMorphStudioSettings.Live != null)
                    m = DM_WeaponMorphStudioSettings.Live.MaterialForBlade(profile.bladeType);
                if (m != null && swapRenderer.sharedMaterial != m)
                    swapRenderer.sharedMaterial = m;
                RefreshBladeVfx(profile);
                return;
            }
            DM_WeaponMorphStudioSettings studio = DM_WeaponMorphStudioSettings.Live;
            Color col = profile.bladeColor;
            if (studio != null && profile.bladeColor.a < 0.01f)
                col = studio.ColorForBlade(profile.bladeType);

            float length = profile.meleeBladeLength > 0.01f
                ? profile.meleeBladeLength
                : (studio != null ? studio.LengthForBlade(profile.bladeType) : 0.85f);
            length *= Mathf.Max(0.05f, profile.bladeLengthScale);

            // Show matching blade child if present (Blade_Metal, Blade_Plasma, ...)
            string want = "Blade_" + profile.bladeType;
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform c = transform.GetChild(i);
                if (c == null || !c.name.StartsWith("Blade_", System.StringComparison.Ordinal))
                    continue;
                bool on = c.name == want || (profile.bladeType == DMWeaponMorphBladeType.Metal && c.name == "Blade");
                c.gameObject.SetActive(on);
                if (on)
                {
                    bladeRoot = c;
                    bladeRenderer = c.GetComponentInChildren<Renderer>(true);
                    Vector3 s = c.localScale;
                    s.y = Mathf.Lerp(0.05f, length, Mathf.Clamp01(extend01));
                    c.localScale = s;
                }
            }

            if (bladeRoot != null && (bladeRoot.name == "Blade" || !bladeRoot.name.StartsWith("Blade_")))
            {
                Vector3 s = bladeRoot.localScale;
                s.y = Mathf.Lerp(0.05f, length, Mathf.Clamp01(extend01));
                bladeRoot.localScale = s;
            }

            if (bladeRenderer != null)
            {
                if (_mpb == null) _mpb = new MaterialPropertyBlock();
                bladeRenderer.GetPropertyBlock(_mpb);
                _mpb.SetColor(BaseColorId, col);
                _mpb.SetColor(UnlitColorId, col);
                float e = profile.bladeEmission;
                if (profile.bladeType != DMWeaponMorphBladeType.Metal && e < 0.01f)
                    e = 2.5f;
                _mpb.SetColor(EmissiveColorId, col * e);
                bladeRenderer.SetPropertyBlock(_mpb);
            }
        }

        private void SyncHitboxLength(DM_WeaponMorphProfile profile)
        {
            if (weaponHitbox == null)
                weaponHitbox = GetComponentInChildren<WeaponHitbox>(true);
            if (weaponHitbox == null || profile == null)
                return;
            weaponHitbox.RebuildHitCollider(boundItem);
        }

        /// <summary>Edit-mode / audit: snap to form index with no coroutine.</summary>
        public void ApplyFormAtIndexImmediate(int index)
        {
            if (forms == null || forms.Length == 0)
                return;
            formIndex = Mathf.Clamp(index, 0, forms.Length - 1);
            CacheParts();
            if (_mpb == null)
                _mpb = new MaterialPropertyBlock();
            ApplyFormImmediate(forms[formIndex]);
        }

        public int FormCount => forms != null ? forms.Length : 0;

        public string FormDisplayName(int index)
        {
            if (forms == null || index < 0 || index >= forms.Length || forms[index] == null)
                return "Form" + index;
            return string.IsNullOrEmpty(forms[index].displayName) ? forms[index].name : forms[index].displayName;
        }
    }
}
