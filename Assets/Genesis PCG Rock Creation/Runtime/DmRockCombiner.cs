using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Random = System.Random;
using Object = UnityEngine.Object;

namespace GenesisPCG.RockCreation
{
    /// <summary>
    /// Edit-mode "rock combiner": takes a base rock (kit prefab or captured base models) and sticks a seeded
    /// number of kit rocks onto its surface, then shows the result as ONE combined mesh (one sub-mesh per piece,
    /// original UVs/normals/materials). The scene stores only settings + seed; the mesh is rebuilt on load.
    /// Copy/paste, Ctrl+D or Shift+drag rolls a new seed (editor watcher) unless Lock Seed is on.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [AddComponentMenu("Genesis PCG Rock Creation/DM PCG Creator")]
    public sealed class DmRockCombiner : MonoBehaviour, IPcgSeeded, IPcgSinkOffset
    {
        [Serializable]
        public sealed class BaseModel
        {
            [Tooltip("Optional prefab/object: every MeshFilter under it is used. Leave empty to use Mesh + Materials.")]
            public GameObject source;
            public Mesh mesh;
            public Material[] materials;
            [Tooltip("Placement relative to the DM PCG Creator.")]
            public Vector3 localPosition;
            public Quaternion localRotation = Quaternion.identity;
            public Vector3 localScale = Vector3.one;

            public Matrix4x4 Matrix
            {
                get { Sanitize(); return Matrix4x4.TRS(localPosition, localRotation, localScale); }
            }

            /// <summary>
            /// Repairs an entry added with the Inspector's list "+" on an empty list: Unity zero-fills new elements
            /// (field initializers do not run), so localRotation is (0,0,0,0) and localScale is zero. That made
            /// Matrix4x4.TRS fail ("Quaternion To Matrix conversion failed"), collapsed every vertex to the origin and
            /// PhysX then failed to cook the collider ("cleaning the mesh failed"). Returns true when something changed.
            /// </summary>
            public bool Sanitize()
            {
                bool changed = false;
                float qm = localRotation.x * localRotation.x + localRotation.y * localRotation.y + localRotation.z * localRotation.z + localRotation.w * localRotation.w;
                if (float.IsNaN(qm) || float.IsInfinity(qm) || qm < 1e-6f) { localRotation = Quaternion.identity; changed = true; }
                else if (Mathf.Abs(qm - 1f) > 1e-3f) { localRotation = Quaternion.Normalize(localRotation); changed = true; }
                if (localScale == Vector3.zero || float.IsNaN(localScale.x) || float.IsNaN(localScale.y) || float.IsNaN(localScale.z)) { localScale = Vector3.one; changed = true; }
                if (float.IsNaN(localPosition.x) || float.IsNaN(localPosition.y) || float.IsNaN(localPosition.z)) { localPosition = Vector3.zero; changed = true; }
                return changed;
            }
        }

        [SerializeField, Tooltip("Optional preset (kit + style + recipe + material + passage/nook settings). The slots below override it when set.")]
        private DmRockPreset preset;
        [SerializeField, Tooltip("Snap / bake / collider settings. Empty = the preset's recipe.")]
        private DmRockCombineRecipe recipe;
        [SerializeField, Tooltip("Optional. Empty = the preset's kit, else the recipe's kit.")]
        private DmRockKit kit;
        [SerializeField] private int seed = 1;
        [SerializeField, Tooltip("When on, copies keep this exact shape instead of rolling a new one.")]
        private bool lockSeed;
        [SerializeField, HideInInspector] private long placementStamp;

        [SerializeField, Tooltip("Optional base override (captured models, kept exactly). With a Style set, only the debris apron is added around it.")]
        private List<BaseModel> baseModels = new List<BaseModel>();

        [SerializeField, Tooltip("Formation style. Empty = the preset's style, else a default boulder pile.")]
        private DmRockStyle style;
        [SerializeField, Tooltip("Optional: every piece uses this material with its own mesh UVs. Empty = the preset's, else the pack's originals.")]
        private Material materialOverride;

        [SerializeField, Tooltip("Optional 'Genesis PCG/Rock Blend Lit' material used on bake (base = the atlas / override). Empty = the preset's, else none.")]
        private Material blendMaterial;

        // m_ prefix: left out of SettingsHash (a bake stays current when only this flag is serialized for the first time).
        [SerializeField, Tooltip("Match the terrain layer under the rock (base blend / fade / top deposits). A rock without a Blend Material " +
                                 "gets its own per-rock blend (from the Default template) on its next bake or live rebuild.")]
        private bool m_matchTerrain = true;

        [SerializeField, Tooltip("Passage / nook settings (copied from the preset when it is applied).")]
        private DmRockFeatureSettings features = new DmRockFeatureSettings();

        [SerializeField, Tooltip("Extra depth (meters along the rock's up) the user pushed this rock into the ground. Kept by snapping, moves and rebakes.")]
        private float userSinkOffset;

        /// <summary>Where each combined sub-mesh came from (for the planned bake: atlas UV-rect remap per source).</summary>
        public struct SubmeshSource
        {
            public Mesh sourceMesh;
            public int sourceSubmesh;
            public Material sourceMaterial;
            public int pieceIndex; // 0 = base part(s), 1..n = add-on pieces in placement order
            public bool isBase;
            public int vertexStart, vertexCount;
        }

        /// <summary>Saved result of a bake (atlas-mapped single mesh + LODs). Empty when the rock shows the live preview.</summary>
        [Serializable]
        public sealed class BakeState
        {
            public bool baked;
            public Mesh mesh;                          // LOD0, main object of the baked .asset
            public Mesh[] lodMeshes = new Mesh[0];     // LOD1..n (sub-assets)
            public float[] lodHeights = new float[0];  // transition height per LOD incl. LOD0; last = cull
            public Mesh colliderMesh;                  // sub-asset or one of the LOD meshes
            public Material[] materials = new Material[0];
            public DmRockAtlas atlas;
            public bool crossFade = true;
            public bool ownsLodGroup;
            public string settingsHash;
            public string contentStamp;                // layout version + kit pieces + atlas + style at bake time (editor)
            public Vector3 position;
            public Quaternion rotation = Quaternion.identity;
            public Vector3 scale = Vector3.one;
        }

        public enum UnbakeReason { Edited, Moved, MissingAsset, User }

        /// <summary>Raised when a baked rock goes back to the live preview (old LOD0 mesh passed for orphan cleanup).</summary>
        public static event Action<DmRockCombiner, UnbakeReason, Mesh> Unbaked;

        public const string LodContainerName = "LODs";

        [SerializeField, HideInInspector] private BakeState bake = new BakeState();
        private static readonly BakeState s_blankBake = new BakeState();

        [NonSerialized] private readonly List<SubmeshSource> submeshSources = new List<SubmeshSource>();
        [NonSerialized] private Mesh generatedMesh;
        // Undeformed result of the seeded build; the surface-conform pass always starts from these.
        [NonSerialized] private Vector3[] buildVerts;
        [NonSerialized] private float[] buildPieceSize;
        [NonSerialized] private Material[] m_liveMats;

        /// <summary>
        /// Editor hook (set by the editor assembly): maps the live preview's pack materials to blend-material previews when the
        /// rock (or its preset) has a Blend Material. Null in players: the live mesh keeps the pack materials.
        /// </summary>
        public static Func<DmRockCombiner, Material[], Material[]> LiveMaterialHook;

        /// <summary>
        /// Editor hook: called on every live rebuild so the editor can give the rock its own blend material (terrain match on
        /// and no blend yet, or a copy that still points at the original's per-rock blend). Null in players.
        /// </summary>
        public static Action<DmRockCombiner> BlendCheckHook;

        private Material[] LiveMaterials(Material[] m)
        {
            if (BlendCheckHook != null)
            {
                try { BlendCheckHook(this); }
                catch (Exception e) { Debug.LogException(e, this); }
            }
            if (m == null || BlendMaterial == null || LiveMaterialHook == null) return m;
            try { return LiveMaterialHook(this, m) ?? m; }
            catch (Exception e) { Debug.LogException(e, this); return m; }
        }

        /// <summary>Re-applies the live materials (blend preview on/off, template edits) without rebuilding.</summary>
        public void RefreshLiveMaterials()
        {
            if (m_liveMats == null || HasBakedPresentation()) return;
            var mr = GetComponent<MeshRenderer>();
            if (mr == null) return;
            Material[] a = LiveMaterials(m_liveMats);
            if (!SameMaterials(mr.sharedMaterials, a)) mr.sharedMaterials = a;
            PcgTerrainSplat.Apply(gameObject);
        }

        private void ComputePieceSizes()
        {
            int n = buildVerts != null ? buildVerts.Length : 0;
            buildPieceSize = new float[n];
            Vector3 ls = transform.lossyScale;
            float sc = Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.y), Mathf.Abs(ls.z));
            for (int i = 0; i < n; i++) buildPieceSize[i] = PcgSurfaceSnap.VertexSizeRange;
            var bounds = new Dictionary<int, Bounds>();
            foreach (SubmeshSource ss in submeshSources)
            {
                if (ss.isBase || ss.vertexCount <= 0 || ss.vertexStart + ss.vertexCount > n) continue;
                bool has = bounds.TryGetValue(ss.pieceIndex, out Bounds b);
                for (int v = ss.vertexStart; v < ss.vertexStart + ss.vertexCount; v++)
                {
                    if (!has) { b = new Bounds(buildVerts[v], Vector3.zero); has = true; }
                    else b.Encapsulate(buildVerts[v]);
                }
                bounds[ss.pieceIndex] = b;
            }
            foreach (SubmeshSource ss in submeshSources)
            {
                if (ss.isBase || ss.vertexCount <= 0 || ss.vertexStart + ss.vertexCount > n || !bounds.TryGetValue(ss.pieceIndex, out Bounds b)) continue;
                Vector3 e = b.size;
                float a = Mathf.Max(e.x, Mathf.Max(e.y, e.z)), c = Mathf.Min(e.x, Mathf.Min(e.y, e.z)), mid = e.x + e.y + e.z - a - c;
                float size = Mathf.Sqrt(a * mid) * sc;
                for (int v = ss.vertexStart; v < ss.vertexStart + ss.vertexCount; v++) buildPieceSize[v] = size;
            }
        }
        [NonSerialized] private Vector3[] buildNormals;
        [NonSerialized] private Vector4[] buildTangents;
        [NonSerialized] private int builtHash;

        public DmRockPreset Preset => preset;
        public DmRockCombineRecipe Recipe => recipe != null ? recipe : (preset != null ? preset.recipe : null);
        public DmRockKit Kit => kit != null ? kit : (preset != null && preset.kit != null ? preset.kit : (Recipe != null ? Recipe.kit : null));
        public DmRockStyle Style => style != null ? style : (preset != null ? preset.style : null);
        public Material MaterialOverride => materialOverride != null ? materialOverride
            : (preset != null && preset.materialOverride != null ? preset.materialOverride : (Style != null ? Style.materialOverride : null));
        /// <summary>The combiner's own slots (null = inherited from the preset).</summary>
        public DmRockCombineRecipe OwnRecipe => recipe;
        public DmRockKit OwnKit => kit;
        public DmRockStyle OwnStyle => style;
        public Material OwnMaterialOverride => materialOverride;
        /// <summary>Rock Blend template used by the baker (own slot, else the preset's). Null = plain atlas / override material.</summary>
        public Material BlendMaterial => blendMaterial != null ? blendMaterial : (preset != null ? preset.blendMaterial : null);
        public Material OwnBlendMaterial => blendMaterial;
        public void SetBlendMaterial(Material value) { blendMaterial = value; MarkDirty(); }
        /// <summary>Terrain match for this rock (see m_matchTerrain).</summary>
        public bool MatchTerrain { get => m_matchTerrain; set { m_matchTerrain = value; MarkDirty(); } }
        public DmRockFeatureSettings Features => features ??= new DmRockFeatureSettings();
        public int Seed => seed;
        public bool LockSeed => lockSeed;
        public long PlacementStamp => placementStamp;
        public Mesh GeneratedMesh => generatedMesh;
        public List<BaseModel> BaseModels => baseModels;
        public IReadOnlyList<SubmeshSource> SubmeshSources => submeshSources;
        public float UserSinkOffset { get => userSinkOffset; set => userSinkOffset = value; }
        public PcgSurfaceSnapSettings SnapSettings => Recipe != null && Recipe.snap != null ? Recipe.snap : PcgSurfaceSnapSettings.Default;
        public DmRockBakeSettings BakeSettings => Recipe != null && Recipe.bake != null ? Recipe.bake : DmRockBakeSettings.Default;
        public BakeState BakeData => bake;
        public bool IsBaked => bake != null && bake.baked;
        /// <summary>Baked and still matching the current settings and pose (mesh asset present).</summary>
        public bool IsBakeCurrent => IsBaked && bake.mesh != null && PoseMatches() && SettingsHash() == bake.settingsHash;

        /// <summary>Info about the last build (for the inspector / tests).</summary>
        public string LastBaseName { get; private set; }
        public int LastPieceCount { get; private set; }
        public Vector3 LastPivotOffset { get; private set; }
        public PcgSurfaceSnap.ConformStats LastConform { get; private set; }
        public DmRockAssembler.Result LastAssembly { get; private set; }
        public PcgMeshOps.Stats LastMeshStats { get; private set; }
        /// <summary>Pose of the last bake, kept after it is invalidated (used to tell a vertical-only move from a re-placement).</summary>
        public Vector3 LastBakedPosition { get; private set; }
        public bool HasLastBakedPose { get; private set; }

        public const string NookSocketName = "PCG_NookSocket";

        public void SetRecipe(DmRockCombineRecipe value) { recipe = value; Rebuild(); }
        public void SetKit(DmRockKit value) { kit = value; Rebuild(); }
        public void SetSeed(int value) { seed = value; Rebuild(); }
        /// <summary>Sets the seed without building (for objects that are built when they get enabled).</summary>
        public void InitSeed(int value) { seed = value; }
        public void SetStyle(DmRockStyle value) { style = value; Rebuild(); }
        public void SetMaterialOverride(Material value) { materialOverride = value; Rebuild(); }
        public void SetBaseModels(List<BaseModel> models) { baseModels = models ?? new List<BaseModel>(); Rebuild(); }

        /// <summary>Uses <paramref name="p"/> for every slot (own slot overrides cleared, features copied). Keeps the seed.</summary>
        public void ApplyPreset(DmRockPreset p, bool rebuild = true)
        {
            preset = p;
            recipe = null; kit = null; style = null; materialOverride = null;
            features = p != null && p.features != null ? p.features.Clone() : new DmRockFeatureSettings();
            if (rebuild) Rebuild();
        }

        public void NewPlacementStamp() => placementStamp = MakeStamp();

        private void OnEnable()
        {
            if (placementStamp == 0)
                placementStamp = MakeStamp();
            PcgSeededRegistry.Register(this);
            DmRockCombineRecipe.Changed += OnRecipeChanged;
            DmRockKit.Changed += OnKitChanged;
            DmRockStyle.Changed += OnStyleChanged;
            DmRockPreset.Changed += OnPresetChanged;
#if UNITY_EDITOR
            UnityEditor.Undo.undoRedoPerformed += RebuildIfChanged;
#endif
            if (IsBaked)
            {
                if (IsBakeCurrent)
                {
                    ApplyBakedPresentation(false);
                    transform.hasChanged = false;
                    return;
                }
                InvalidateBake(bake.mesh == null ? UnbakeReason.MissingAsset : UnbakeReason.Edited, true);
            }
            PcgRenderUtil.DisallowGpuDriven(gameObject);
            if (TryAdoptLiveMesh()) return; // domain reload / play-mode enter / exit: the existing preview mesh is still current
            Rebuild();
        }

        // ------------------------------------------------------------------------------------------------
        // Live preview mesh reuse (no rebuild when nothing changed)
        // ------------------------------------------------------------------------------------------------

        private const string GeneratedMeshName = "DmRockCombiner (generated)";
        // Which live rock built / adopted each preview mesh (a duplicated rock references the same mesh: only one may own it).
        private static readonly Dictionary<Mesh, DmRockCombiner> s_meshOwner = new Dictionary<Mesh, DmRockCombiner>();
        private static readonly Dictionary<Object, KeyValuePair<int, string>> s_assetJsonCache = new Dictionary<Object, KeyValuePair<int, string>>();
        [NonSerialized] private bool m_adoptedLive;

        /// <summary>True when the current preview mesh was reused on enable instead of rebuilt (no undeformed build data kept).</summary>
        public bool AdoptedLiveMesh => m_adoptedLive;

        private static string AssetJson(Object a, bool cached)
        {
            if (a == null) return "-";
            if (!(a is ScriptableObject)) return "r"; // materials etc.: the reference in the rock's own JSON is enough
            if (cached && s_assetJsonCache.TryGetValue(a, out var c) && c.Key == Time.frameCount) return c.Value;
            string j = Hash128.Compute(JsonUtility.ToJson(a)).ToString(); // the reference itself is part of the rock's own JSON
            s_assetJsonCache[a] = new KeyValuePair<int, string>(Time.frameCount, j);
            return j;
        }

        /// <summary>
        /// Content key of the live preview: serialized settings (bake blanked) + the recipe / kit / style / preset contents +
        /// pose + scale + layout version. Unlike ComputeHash it holds no NonSerialized edit counters, so it survives a domain reload.
        /// </summary>
        private string LiveKey(bool cached)
        {
            BakeState saved = bake;
            bake = s_blankBake;
            string json;
            try { json = JsonUtility.ToJson(this); }
            finally { bake = saved; }
            Transform t = transform;
            var sb = new System.Text.StringBuilder(json, json.Length + 256);
            sb.Append('|').Append(AssetJson(Recipe, cached)).Append('|').Append(AssetJson(Kit, cached)).Append('|').Append(AssetJson(Style, cached))
              .Append('|').Append(AssetJson(preset, cached)).Append('|').Append(AssetJson(MaterialOverride, cached)).Append('|').Append(LayoutVersion)
              .Append('|').Append(t.position.ToString("R")).Append(t.rotation.ToString("R")).Append(t.lossyScale.ToString("R"));
            return Hash128.Compute(sb.ToString()).ToString();
        }

        // Cheap content signature of the mesh itself: a mesh edited after the build (e.g. carved at runtime) is not adopted.
        private static string MeshSig(Mesh m)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(m.vertexCount).Append('.').Append(m.subMeshCount);
            for (int i = 0; i < m.subMeshCount; i++) sb.Append('.').Append(m.GetIndexCount(i));
            Bounds b = m.bounds;
            sb.Append('.').Append(Hash128.Compute(b.center.ToString("R") + b.size.ToString("R")).ToString().Substring(0, 8));
            return sb.ToString();
        }

        private void StampGeneratedMesh()
        {
            if (generatedMesh == null) return;
            generatedMesh.name = GeneratedMeshName + " #" + LiveKey(false) + "/" + MeshSig(generatedMesh);
            s_meshOwner[generatedMesh] = this;
            m_adoptedLive = false;
        }

        private bool TryAdoptLiveMesh()
        {
            if (flatGroundForPreview || HasBakedPresentation()) return false;
            var mf = GetComponent<MeshFilter>();
            Mesh m = mf != null ? mf.sharedMesh : null;
            if (m == null || (m.hideFlags & HideFlags.DontSave) == 0 || m.vertexCount == 0 || !m.name.StartsWith(GeneratedMeshName + " #", StringComparison.Ordinal))
                return false;
            if (s_meshOwner.TryGetValue(m, out DmRockCombiner owner) && owner != null && owner != this && owner.generatedMesh == m)
                return false; // a duplicate: build its own mesh
            if (m.name != GeneratedMeshName + " #" + LiveKey(true) + "/" + MeshSig(m))
                return false;
            generatedMesh = m;
            s_meshOwner[m] = this;
            m_adoptedLive = true;
            m_builtPos = transform.position;
            m_builtRot = transform.rotation;
            buildVerts = null; buildNormals = null; buildTangents = null; // a later move rebuilds once settled
            transform.hasChanged = false;
            builtHash = ComputeHash();
#if UNITY_EDITOR
            s_keptForEditMode.Remove(m);
#endif
            return true;
        }

#if UNITY_EDITOR
        // Preview meshes of play-mode rocks kept on play exit, so the restored edit-mode rocks can adopt them.
        private static readonly List<Mesh> s_keptForEditMode = new List<Mesh>();
        private static bool s_keptHooked;

        private static void KeepForEditMode(Mesh m)
        {
            if (m == null || s_keptForEditMode.Contains(m)) return;
            s_keptForEditMode.Add(m);
            if (s_keptHooked) return;
            s_keptHooked = true;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChangedForKept;
        }

        private static void OnPlayModeChangedForKept(UnityEditor.PlayModeStateChange st)
        {
            if (st != UnityEditor.PlayModeStateChange.EnteredEditMode) return;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                foreach (Mesh m in s_keptForEditMode)
                {
                    if (m == null) continue;
                    if (s_meshOwner.TryGetValue(m, out DmRockCombiner o) && o != null && o.generatedMesh == m) continue; // adopted
                    s_meshOwner.Remove(m);
                    DestroyImmediate(m);
                }
                s_keptForEditMode.Clear();
            };
        }
#endif

        private void Start()
        {
            // OnEnable can run before the Terrain of the same scene registers in Terrain.activeTerrains: bind again once all
            // objects of the scene are enabled (edit mode too, the component runs in edit mode).
            PcgTerrainSplat.Apply(gameObject);
        }

        private void OnDisable()
        {
            DmRockCombineRecipe.Changed -= OnRecipeChanged;
            DmRockKit.Changed -= OnKitChanged;
            DmRockStyle.Changed -= OnStyleChanged;
            DmRockPreset.Changed -= OnPresetChanged;
#if UNITY_EDITOR
            UnityEditor.Undo.undoRedoPerformed -= RebuildIfChanged;
            UnityEditor.EditorApplication.update -= PollSettledRebuild;
#endif
        }

        private void OnDestroy()
        {
            PcgSeededRegistry.Unregister(this);
            if (generatedMesh == null)
                return;
            if (s_meshOwner.TryGetValue(generatedMesh, out DmRockCombiner owner) && owner == this)
                s_meshOwner.Remove(generatedMesh);
#if UNITY_EDITOR
            // Leaving play mode: the restored edit-mode copy of this rock still references this mesh and adopts it in OnEnable
            // (no full rebuild of every live rock on play exit). Unadopted meshes are destroyed once back in edit mode.
            if (Application.isPlaying && !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                KeepForEditMode(generatedMesh);
                generatedMesh = null;
                return;
            }
#endif
            if (Application.isPlaying) Destroy(generatedMesh);
            else DestroyImmediate(generatedMesh);
            generatedMesh = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // New list entries from the Inspector arrive zero-filled: give them identity rotation / unit scale right away.
            if (baseModels != null)
                foreach (BaseModel bm in baseModels)
                    bm?.Sanitize();
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null && isActiveAndEnabled)
                    RebuildIfChanged();
            };
        }
#endif

        // Shared-asset edits (recipe / kit / style / preset) rebuild previews; baked rocks keep their saved mesh until rebaked.
        private void OnRecipeChanged(DmRockCombineRecipe r)
        {
            if (r == Recipe && this != null && isActiveAndEnabled && !IsBaked) Rebuild();
        }

        private void OnKitChanged(DmRockKit k)
        {
            if (k == Kit && this != null && isActiveAndEnabled && !IsBaked) Rebuild();
        }

        private void OnStyleChanged(DmRockStyle st)
        {
            if (st == Style && this != null && isActiveAndEnabled && !IsBaked) Rebuild();
        }

        private void OnPresetChanged(DmRockPreset p)
        {
            if (p == preset && this != null && isActiveAndEnabled && !IsBaked) Rebuild();
        }

        [NonSerialized] private Vector3 m_builtPos;
        [NonSerialized] private Quaternion m_builtRot = Quaternion.identity;

        /// <summary>The live layout was assembled at the current position / rotation (the ground under it can differ elsewhere).</summary>
        public bool BuiltForCurrentPose =>
            generatedMesh != null && (transform.position - m_builtPos).sqrMagnitude <= 1e-6f && Quaternion.Angle(transform.rotation, m_builtRot) <= 0.01f;

        /// <summary>Result of the post-assembly grounding pass of the last rebuild.</summary>
        public PcgGroundSettle.Stats LastSettle { get; private set; }

#if UNITY_EDITOR
        [NonSerialized] private double m_settleAt = -1;

        private void Update() => HandleEditorMove();

        /// <summary>
        /// Edit mode: re-conform whenever the rock is moved/rotated/snapped (from the undeformed mesh); once the move
        /// settles, rebuild so the formation follows the ground shape at the new place. Baked rocks are invalidated
        /// (the bake scheduler rebakes them). Called from Update and from the editor when a Transform change is
        /// published (edit-mode Update does not tick while the editor is in the background or for Inspector / script moves).
        /// </summary>
        public void HandleEditorMove()
        {
            if (!Application.isPlaying && this != null && isActiveAndEnabled && transform.hasChanged)
            {
                transform.hasChanged = false;
                if (IsBaked)
                {
                    if (PoseMatches()) return;
                    InvalidateBake(UnbakeReason.Moved, false);
                    Rebuild();
                    return;
                }
                ApplySurfaceConform();
                PcgTerrainSplat.Apply(gameObject); // the faded base follows the terrain under the rock while it is dragged
                if ((transform.position - m_builtPos).sqrMagnitude > 1e-6f || Quaternion.Angle(transform.rotation, m_builtRot) > 0.01f)
                {
                    m_settleAt = UnityEditor.EditorApplication.timeSinceStartup + 0.35;
                    UnityEditor.EditorApplication.update -= PollSettledRebuild;
                    UnityEditor.EditorApplication.update += PollSettledRebuild;
                }
            }
        }

        private void PollSettledRebuild()
        {
            if (this == null || !isActiveAndEnabled) { UnityEditor.EditorApplication.update -= PollSettledRebuild; return; }
            if (UnityEditor.EditorApplication.timeSinceStartup < m_settleAt || GUIUtility.hotControl != 0) return;
            UnityEditor.EditorApplication.update -= PollSettledRebuild;
            if (IsBaked) return;
            if ((transform.position - m_builtPos).sqrMagnitude > 1e-6f || Quaternion.Angle(transform.rotation, m_builtRot) > 0.01f)
                Rebuild();
        }
#endif

        /// <summary>Re-applies the conform-to-surface pass (from the undeformed build) and refreshes the collider.</summary>
        public void ApplySurfaceConform()
        {
            if (generatedMesh == null || buildVerts == null)
                return;
            if (flatGroundForPreview)
                return; // palette thumbnails: isolated preview, never raycast the open scenes
            LastConform = PcgSurfaceSnap.ConformMesh(generatedMesh, buildVerts, buildNormals, buildTangents, transform, ConformSettings(), null, buildPieceSize);
            var mc = GetComponent<MeshCollider>();
            if (mc != null && mc.sharedMesh == generatedMesh)
            {
                mc.sharedMesh = null;
                if (IsColliderCookable(generatedMesh)) mc.sharedMesh = generatedMesh;
            }
        }

        /// <summary>
        /// False for meshes PhysX cannot cook (fewer than one triangle, or every vertex collapsed into a point / line).
        /// Such a mesh is left off the collider instead of logging "[Physics.PhysX] cleaning the mesh failed".
        /// </summary>
        public static bool IsColliderCookable(Mesh m)
        {
            if (m == null || m.vertexCount < 3) return false;
            Vector3 size = m.bounds.size;
            if (float.IsNaN(size.x) || float.IsNaN(size.y) || float.IsNaN(size.z)) return false;
            int axes = (size.x > 1e-5f ? 1 : 0) + (size.y > 1e-5f ? 1 : 0) + (size.z > 1e-5f ? 1 : 0);
            if (axes < 2) return false;
            for (int i = 0; i < m.subMeshCount; i++)
                if (m.GetIndexCount(i) >= 3) return true;
            return false;
        }

        [NonSerialized] private PcgSurfaceSnapSettings m_noConform;

        private PcgSurfaceSnapSettings ConformSettings()
        {
            PcgSurfaceSnapSettings s = SnapSettings;
            if (LastAssembly == null || !LastAssembly.noConform) return s;
            m_noConform ??= new PcgSurfaceSnapSettings();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(s), m_noConform);
            m_noConform.conformBaseToSurface = false;
            return m_noConform;
        }

        public void RebuildIfChanged()
        {
            if (this == null || !isActiveAndEnabled)
                return;
            if (IsBaked)
            {
                if (IsBakeCurrent)
                {
                    ApplyBakedPresentation(false);
                    return;
                }
                InvalidateBake(bake.mesh == null ? UnbakeReason.MissingAsset : UnbakeReason.Edited, false);
                Rebuild();
                return;
            }
            if (generatedMesh == null || ComputeHash() != builtHash || HasBakedPresentation() || !BuiltForCurrentPose)
                Rebuild();
        }

        private int ComputeHash()
        {
            unchecked
            {
                BakeState saved = bake;
                bake = s_blankBake;
                string json;
                try { json = JsonUtility.ToJson(this); }
                finally { bake = saved; }
                int h = json.GetHashCode();
                h = h * 31 + (Recipe != null ? Recipe.Version : 0);
                h = h * 31 + (Kit != null ? Kit.Version : 0);
                h = h * 31 + (Style != null ? Style.Version : 0);
                h = h * 31 + (preset != null ? preset.Version : 0);
                return h;
            }
        }

        // ------------------------------------------------------------------------------------------------
        // Build
        // ------------------------------------------------------------------------------------------------

        private static DmRockStyle s_defaultStyle;
        private static readonly Dictionary<DmRockKit, KeyValuePair<int, List<DmRockPieceInfo>>> s_kitPieces = new Dictionary<DmRockKit, KeyValuePair<int, List<DmRockPieceInfo>>>();

        /// <summary>Default when no style is set: a boulder pile with the research numbers.</summary>
        public static DmRockStyle DefaultStyle
        {
            get
            {
                if (s_defaultStyle == null)
                {
                    s_defaultStyle = ScriptableObject.CreateInstance<DmRockStyle>();
                    s_defaultStyle.hideFlags = HideFlags.HideAndDontSave;
                    s_defaultStyle.name = "Default Boulder Pile";
                }
                return s_defaultStyle;
            }
        }

        /// <summary>Measured pieces of a kit (the kit's cached analysis, else a bounds-only fallback).</summary>
        public static List<DmRockPieceInfo> KitPieces(DmRockKit k)
        {
            if (k == null) return new List<DmRockPieceInfo>();
            if (k.pieces != null && k.pieces.Count > 0)
            {
                var valid = new List<DmRockPieceInfo>(k.pieces.Count);
                foreach (DmRockPieceInfo p in k.pieces) if (p != null && p.prefab != null) valid.Add(p);
                if (valid.Count > 0) return valid;
            }
            if (s_kitPieces.TryGetValue(k, out var cached) && cached.Key == k.Version) return cached.Value;
            var list = new List<DmRockPieceInfo>();
            var seen = new HashSet<GameObject>();
            foreach (List<GameObject> src in new[] { k.baseCandidates, k.addOnCandidates })
                if (src != null)
                    foreach (GameObject g in src)
                        if (g != null && seen.Add(g))
                        {
                            DmRockPieceInfo info = DmRockPieceAnalyzer.FromBounds(g, k);
                            if (info != null) list.Add(info);
                        }
            s_kitPieces[k] = new KeyValuePair<int, List<DmRockPieceInfo>>(k.Version, list);
            return list;
        }

        /// <summary>
        /// Ground height in combiner-local space relative to the pivot (0 at the pivot), sampled lazily on a 0.5 m grid.
        /// Flat when nothing is under the rock (previews).
        /// </summary>
        public Func<float, float, float> MakeGroundFunction() => MakeGroundFunction(out _);

        /// <summary>
        /// Ground height under local (x, z) relative to the ground at the pivot (the layout's base plane). <paramref name="origin"/>
        /// is that pivot ground in local space (sink depth + user sink offset), so real ground = f(x, z) + origin.
        /// </summary>
        public Func<float, float, float> MakeGroundFunction(out float origin)
        {
            PcgSurfaceSnapSettings s = SnapSettings;
            Transform t = transform;
            Matrix4x4 l2w = t.localToWorldMatrix, w2l = t.worldToLocalMatrix;
            int mask = s.surfaceLayerMask;
            if (s.excludeOwnLayer) mask &= ~(1 << t.gameObject.layer);
            var ignore = new List<Transform> { t };
            var cache = new Dictionary<long, float>();
            const float cell = 0.5f;
            Physics.SyncTransforms();
            // Ground height along the rock's LOCAL vertical through (i, j). The rock is usually tilted to the terrain normal,
            // so a world-vertical ray from the local grid point lands at a different local xz; a few fixed-point steps walk
            // the sample along the local vertical until the hit is under the requested local point.
            float Raw(int i, int j)
            {
                long key = ((long)i << 32) ^ (uint)j;
                if (cache.TryGetValue(key, out float y)) return y;
                y = float.NaN;
                float ly = 0f;
                for (int it = 0; it < 4; it++)
                {
                    Vector3 w = l2w.MultiplyPoint3x4(new Vector3(i * cell, ly, j * cell));
                    if (!PcgSurfaceSnap.SampleGroundY(w, 40f, 60f, mask, s, ignore, out float gy))
                        break;
                    float ny = w2l.MultiplyPoint3x4(new Vector3(w.x, gy, w.z)).y;
                    y = ny;
                    if (Mathf.Abs(ny - ly) < 0.005f) break;
                    ly = ny;
                }
                cache[key] = y;
                return y;
            }
            float org = Raw(0, 0);
            origin = float.IsNaN(org) ? 0f : org;
            if (float.IsNaN(org)) return (x, z) => 0f;
            return (x, z) =>
            {
                float fx = x / cell, fz = z / cell;
                int i = Mathf.FloorToInt(fx), j = Mathf.FloorToInt(fz);
                float u = fx - i, v = fz - j;
                float a = Raw(i, j), b = Raw(i + 1, j), c = Raw(i, j + 1), d = Raw(i + 1, j + 1);
                float fallback = !float.IsNaN(a) ? a : (!float.IsNaN(b) ? b : (!float.IsNaN(c) ? c : (!float.IsNaN(d) ? d : org)));
                if (float.IsNaN(a)) a = fallback; if (float.IsNaN(b)) b = fallback; if (float.IsNaN(c)) c = fallback; if (float.IsNaN(d)) d = fallback;
                return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v) - org;
            };
        }

        /// <summary>Optional editor hook: flat ground for previews (palette thumbnails).</summary>
        [NonSerialized] public bool flatGroundForPreview;

        public void Rebuild()
        {
            // Rebuilding (reseed / edit / explicit rebuild) always goes back to the live preview.
            if (IsBaked)
                InvalidateBake(UnbakeReason.Edited, false);
            if (HasBakedPresentation())
                RemoveBakedPresentation(false);
            if (generatedMesh == null)
                generatedMesh = new Mesh { name = GeneratedMeshName, hideFlags = HideFlags.DontSave };
            else
                generatedMesh.name = GeneratedMeshName; // not current until the build below completes
            m_adoptedLive = false;

            PcgRenderUtil.DisallowGpuDriven(gameObject);
            var mf = GetComponent<MeshFilter>();
            var mr = GetComponent<MeshRenderer>();
            DmRockKit k = Kit;
            LastBaseName = null;
            LastPieceCount = 0;
            LastPivotOffset = Vector3.zero;
            m_builtPos = transform.position;
            m_builtRot = transform.rotation;

            // Captured base models (kept as-is).
            var fixedParts = new List<PcgPlacedPart>();
            if (baseModels != null)
                foreach (BaseModel bm in baseModels)
                {
                    if (bm == null) continue;
                    var tmp = new List<Part>();
                    if (bm.source != null) AddObject(tmp, bm.source, bm.Matrix);
                    else if (bm.mesh != null) AddPart(tmp, bm.mesh, bm.materials, bm.Matrix);
                    foreach (Part p in tmp)
                        fixedParts.Add(new PcgPlacedPart { mesh = p.mesh, materials = p.materials, rawMatrix = p.matrix, isBase = true });
                }

            float groundOrigin = 0f;
            Func<float, float, float> ground = flatGroundForPreview ? ((x, z) => 0f) : MakeGroundFunction(out groundOrigin);
            DmRockAssembler.Result result;
            if (fixedParts.Count > 0)
            {
                LastBaseName = "baseModels x" + baseModels.Count;
                if (Style != null)
                    result = new DmRockAssembler(Style, Features, KitPieces(k), ground, seed).Assemble(fixedParts);
                else
                {
                    result = new DmRockAssembler.Result();
                    result.parts.AddRange(fixedParts);
                }
            }
            else
            {
                result = new DmRockAssembler(Style != null ? Style : DefaultStyle, Features, KitPieces(k), ground, seed).Assemble();
                LastBaseName = result.parts.Count > 0 && result.parts[0].info != null ? result.parts[0].info.prefab.name : null;
            }
            // Grounding pass: every loose piece sits on the ground under its own footprint (not the formation's base plane).
            LastSettle = default;
            if (!flatGroundForPreview && result.parts.Count > 0)
            {
                // Settle against the REAL ground (layout ground + pivot origin): a user lift / sink moves the formation,
                // but loose pieces still sit on the terrain and nothing is left hovering.
                float o = groundOrigin;
                LastSettle = PcgGroundSettle.Settle(result.parts, (x, z) => ground(x, z) + o, Style != null ? Style : DefaultStyle, result.noConform);
            }
            LastAssembly = result;
            LastPieceCount = result.parts.Count;

            var mats = new List<Material>();
            var sources = new List<PcgMeshOps.Source>();
            var mstats = new PcgMeshOps.Stats();
            PcgMeshOps.Combine(generatedMesh, result.parts, MaterialOverride, mats, sources, mstats);
            LastMeshStats = mstats;
            submeshSources.Clear();
            foreach (PcgMeshOps.Source src in sources)
                submeshSources.Add(new SubmeshSource
                {
                    sourceMesh = src.mesh, sourceSubmesh = src.submesh, sourceMaterial = src.material,
                    pieceIndex = src.pieceIndex, isBase = src.isBase, vertexStart = src.vertexStart, vertexCount = src.vertexCount,
                });

            buildVerts = generatedMesh.vertices;
            buildNormals = generatedMesh.normals;
            buildTangents = generatedMesh.tangents;
            ComputePieceSizes();

            mf.sharedMesh = generatedMesh;
            Material[] matArray = mats.ToArray();
            m_liveMats = matArray;
            matArray = LiveMaterials(matArray);
            if (!SameMaterials(mr.sharedMaterials, matArray))
                mr.sharedMaterials = matArray;

            bool wantCollider = Recipe == null || Recipe.addMeshCollider;
            var mc = GetComponent<MeshCollider>();
            if (wantCollider)
            {
                if (mc == null) mc = gameObject.AddComponent<MeshCollider>();
                // Detach first: toggling convex re-cooks whatever mesh is still assigned (possibly a stale / degenerate one).
                mc.sharedMesh = null;
                mc.convex = false;
                mc.sharedMesh = IsColliderCookable(generatedMesh) ? generatedMesh : null;
            }

            ApplySurfaceConform(); // also rebakes the collider
            PcgTerrainSplat.Apply(gameObject);
            UpdateNookSockets(result);
            transform.hasChanged = false;
            builtHash = ComputeHash();
            StampGeneratedMesh();
        }

        /// <summary>Child 'PCG_NookSocket' transforms (not baked into the mesh), optional seeded prefab per socket.</summary>
        private void UpdateNookSockets(DmRockAssembler.Result result)
        {
            var existing = new List<Transform>();
            foreach (Transform ch in transform)
                if (ch.name.StartsWith(NookSocketName, StringComparison.Ordinal)) existing.Add(ch);
            int count = result != null ? result.nooks.Count : 0;
            DmRockFeatureSettings f = Features;
            for (int i = 0; i < count; i++)
            {
                DmRockAssembler.NookInfo n = result.nooks[i];
                string nm = i == 0 ? NookSocketName : NookSocketName + "_" + (i + 1);
                Transform s = i < existing.Count ? existing[i] : null;
                if (s == null)
                {
                    var go = new GameObject(nm);
                    go.layer = gameObject.layer;
                    s = go.transform;
                    s.SetParent(transform, false);
                }
                if (s.name != nm) s.name = nm;
                s.localPosition = n.position;
                s.localRotation = n.rotation;
                s.localScale = Vector3.one;
                var rng = new Random(seed * 7919 + i * 104729);
                GameObject want = null;
                if (f.nookPrefabs != null && f.nookPrefabs.Length > 0 && rng.NextDouble() < f.nookSpawnChance)
                    want = f.nookPrefabs[rng.Next(f.nookPrefabs.Length)];
                SyncNookProp(s, want);
            }
            for (int i = count; i < existing.Count; i++)
                if (existing[i] != null) DestroyNow(existing[i].gameObject, false);
        }

        private static void SyncNookProp(Transform socket, GameObject prefab)
        {
            Transform cur = socket.childCount > 0 ? socket.GetChild(0) : null;
#if UNITY_EDITOR
            GameObject curSrc = cur != null ? UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(cur.gameObject) : null;
            if (cur != null && prefab != null && curSrc == prefab) return;
#else
            if (cur != null && prefab != null) return;
#endif
            for (int i = socket.childCount - 1; i >= 0; i--) DestroyNow(socket.GetChild(i).gameObject, false);
            if (prefab == null) return;
            GameObject inst;
#if UNITY_EDITOR
            inst = Application.isPlaying ? Instantiate(prefab) : (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, socket.gameObject.scene);
#else
            inst = Instantiate(prefab);
#endif
            if (inst == null) return;
            inst.transform.SetParent(socket, false);
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
        }

        private struct Part
        {
            public Mesh mesh;
            public Material[] materials;
            public Matrix4x4 matrix;
        }
        // ------------------------------------------------------------------------------------------------
        // Bake state (the editor baker creates the assets; this only swaps what the scene object shows)
        // ------------------------------------------------------------------------------------------------

        /// <summary>
        /// Stable hash of this combiner's own settings (seed, stamp, overrides, base models...), bake state excluded.
        /// Shared recipe / kit / style edits are not part of it: baked rocks keep their mesh until rebaked.
        /// </summary>
        public string SettingsHash()
        {
#if UNITY_EDITOR
            // Only this component's own serialized settings (bake state excluded). Object references hash by
            // asset GUID + local id, so the value is stable across sessions, scene reloads and copies of the rock
            // (instance ids and the owning GameObject are deliberately left out).
            var sb = new System.Text.StringBuilder(512);
            using (var so = new UnityEditor.SerializedObject(this))
            {
                UnityEditor.SerializedProperty it = so.GetIterator();
                bool enter = true;
                while (it.Next(enter))
                {
                    enter = true;
                    if (it.depth == 0 && (it.name == nameof(bake) || it.name == nameof(userSinkOffset) || it.name.StartsWith("m_", StringComparison.Ordinal)))
                    {
                        enter = false;
                        continue;
                    }
                    switch (it.propertyType)
                    {
                        case UnityEditor.SerializedPropertyType.Integer:
                        case UnityEditor.SerializedPropertyType.ArraySize:
                        case UnityEditor.SerializedPropertyType.LayerMask:
                            sb.Append(it.propertyPath).Append('=').Append(it.longValue).Append(';'); break;
                        case UnityEditor.SerializedPropertyType.Boolean:
                            sb.Append(it.propertyPath).Append('=').Append(it.boolValue ? '1' : '0').Append(';'); break;
                        case UnityEditor.SerializedPropertyType.Float:
                            sb.Append(it.propertyPath).Append('=').Append(it.doubleValue.ToString("R", System.Globalization.CultureInfo.InvariantCulture)).Append(';'); break;
                        case UnityEditor.SerializedPropertyType.String:
                            sb.Append(it.propertyPath).Append('=').Append(it.stringValue).Append(';'); break;
                        case UnityEditor.SerializedPropertyType.Enum:
                            sb.Append(it.propertyPath).Append('=').Append(it.enumValueIndex).Append(';'); break;
                        case UnityEditor.SerializedPropertyType.ObjectReference:
                            sb.Append(it.propertyPath).Append('=').Append(RefId(it.objectReferenceValue)).Append(';'); break;
                    }
                }
            }
            return Hash128.Compute(sb.ToString()).ToString();
#else
            return bake != null ? bake.settingsHash : null;
#endif
        }

#if UNITY_EDITOR
        private static string RefId(UnityEngine.Object o)
        {
            if (o == null) return "0";
            if (UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o, out string guid, out long local))
                return guid + ":" + local;
            return UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(o).ToString();
        }
#endif

        private bool PoseMatches()
        {
#if UNITY_EDITOR
            if (bake == null) return false;
            return (transform.position - bake.position).sqrMagnitude < 1e-8f
                   && Quaternion.Angle(transform.rotation, bake.rotation) < 0.01f
                   && (transform.lossyScale - bake.scale).sqrMagnitude < 1e-10f;
#else
            return true;
#endif
        }

        /// <summary>Called by the editor baker: show the saved meshes instead of the preview.</summary>
        public void ApplyBake(Mesh mesh, Mesh[] lodMeshes, float[] lodHeights, Mesh colliderMesh, Material[] materials, DmRockAtlas atlas, bool crossFade)
            => ApplyBake(mesh, lodMeshes, lodHeights, colliderMesh, materials, atlas, crossFade, null);

        /// <summary>Layout algorithm version; bakes made with another version are reported out of date (not auto-unbaked).</summary>
        public const int LayoutVersion = 6; // 5: flush terrain clip + texture arrays; 4: bake drops tiny cull-disconnected remnants; 3: tiny plane-cut fragments dropped; vertex B/A masks

        public void ApplyBake(Mesh mesh, Mesh[] lodMeshes, float[] lodHeights, Mesh colliderMesh, Material[] materials, DmRockAtlas atlas, bool crossFade, string contentStamp)
        {
            if (mesh == null)
                throw new ArgumentNullException(nameof(mesh));
            var state = new BakeState
            {
                baked = true,
                mesh = mesh,
                lodMeshes = lodMeshes ?? new Mesh[0],
                lodHeights = lodHeights ?? new float[0],
                colliderMesh = colliderMesh,
                materials = materials ?? new Material[0],
                atlas = atlas,
                crossFade = crossFade,
                position = transform.position,
                rotation = transform.rotation,
                scale = transform.lossyScale,
            };
            state.settingsHash = SettingsHash();
            state.contentStamp = contentStamp;
            bake = state;
            ApplyBakedPresentation(true);

            // The DontSave preview mesh is no longer drawn by anything.
            if (generatedMesh != null)
            {
                if (Application.isPlaying) Destroy(generatedMesh); else DestroyImmediate(generatedMesh);
                generatedMesh = null;
            }
            buildVerts = null; buildNormals = null; buildTangents = null;
            submeshSources.Clear();
            transform.hasChanged = false;
            MarkDirty();
        }

        /// <summary>Back to the live (seeded) preview. The saved assets stay until the cleanup removes unused ones.</summary>
        public void Unbake()
        {
            if (IsBaked)
                InvalidateBake(UnbakeReason.User, false);
            Rebuild();
        }

        private void InvalidateBake(UnbakeReason reason, bool deferDestroy)
        {
            if (!IsBaked)
                return;
            Mesh old = bake.mesh;
            LastBakedPosition = bake.position;
            HasLastBakedPose = true;
            RemoveBakedPresentation(deferDestroy);
            bake = new BakeState();
            PcgRenderUtil.DisallowGpuDriven(gameObject);
            MarkDirty();
            Unbaked?.Invoke(this, reason, old);
        }

        private bool HasBakedPresentation()
        {
            return transform.Find(LodContainerName) != null;
        }

        private void ApplyBakedPresentation(bool removeDisallowComponent)
        {
            var mf = GetComponent<MeshFilter>();
            var mr = GetComponent<MeshRenderer>();
            if (mf.sharedMesh != bake.mesh) mf.sharedMesh = bake.mesh;
            if (bake.materials != null && bake.materials.Length > 0 && !SameMaterials(mr.sharedMaterials, bake.materials))
                mr.sharedMaterials = bake.materials;

            bool wantCollider = Recipe == null || Recipe.addMeshCollider;
            var mc = GetComponent<MeshCollider>();
            if (wantCollider)
            {
                if (mc == null) mc = gameObject.AddComponent<MeshCollider>();
                Mesh col = bake.colliderMesh != null ? bake.colliderMesh : bake.mesh;
                if (mc.convex) { mc.sharedMesh = null; mc.convex = false; }
                if (!IsColliderCookable(col)) col = null;
                if (mc.sharedMesh != col) mc.sharedMesh = col;
            }

            // Saved asset meshes are safe for the GPU Resident Drawer (the BatchDrawCommand error came from DontSave edit meshes).
            PcgRenderUtil.AllowGpuDriven(gameObject, removeDisallowComponent);
            EnsureLodPresentation(mr);
            PcgTerrainSplat.Apply(gameObject); // per-pixel terrain under the rock (blend materials that want it)
        }

        private void EnsureLodPresentation(MeshRenderer root)
        {
            Mesh[] lods = bake.lodMeshes;
            Transform container = transform.Find(LodContainerName);
            if (lods == null || lods.Length == 0)
            {
                if (container != null || bake.ownsLodGroup) RemoveBakedPresentation(false);
                return;
            }
            if (container == null)
            {
                var go = new GameObject(LodContainerName);
                go.layer = gameObject.layer;
                container = go.transform;
                container.SetParent(transform, false);
#if UNITY_EDITOR
                UnityEditor.GameObjectUtility.SetStaticEditorFlags(go, UnityEditor.GameObjectUtility.GetStaticEditorFlags(gameObject));
#endif
            }

            var renderers = new Renderer[lods.Length + 1];
            renderers[0] = root;
            int noShadowFrom = lods.Length - Mathf.Max(0, BakeSettings.lastLodsWithoutShadows); // index into lods[] (LOD i+1)
            for (int i = 0; i < lods.Length; i++)
            {
                string n = "LOD" + (i + 1);
                Transform child = container.Find(n);
                if (child == null)
                {
                    var go = new GameObject(n, typeof(MeshFilter), typeof(MeshRenderer));
                    go.layer = gameObject.layer;
                    child = go.transform;
                    child.SetParent(container, false);
#if UNITY_EDITOR
                    UnityEditor.GameObjectUtility.SetStaticEditorFlags(go, UnityEditor.GameObjectUtility.GetStaticEditorFlags(gameObject));
#endif
                }
                child.SetSiblingIndex(i);
                var cmf = child.GetComponent<MeshFilter>();
                if (cmf == null) cmf = child.gameObject.AddComponent<MeshFilter>();
                var cmr = child.GetComponent<MeshRenderer>();
                if (cmr == null) cmr = child.gameObject.AddComponent<MeshRenderer>();
                if (cmf.sharedMesh != lods[i]) cmf.sharedMesh = lods[i];
                if (!SameMaterials(cmr.sharedMaterials, root.sharedMaterials)) cmr.sharedMaterials = root.sharedMaterials;
                cmr.shadowCastingMode = i >= noShadowFrom ? UnityEngine.Rendering.ShadowCastingMode.Off : root.shadowCastingMode;
                cmr.receiveShadows = root.receiveShadows;
                cmr.lightProbeUsage = root.lightProbeUsage;
                cmr.reflectionProbeUsage = root.reflectionProbeUsage;
                cmr.renderingLayerMask = root.renderingLayerMask;
                renderers[i + 1] = cmr;
            }
            for (int i = container.childCount - 1; i >= lods.Length; i--)
                DestroyNow(container.GetChild(i).gameObject, false);

            var lg = GetComponent<LODGroup>();
            if (lg == null) lg = gameObject.AddComponent<LODGroup>();
            bake.ownsLodGroup = true;
            var lodArr = new LOD[renderers.Length];
            float prev = 1f;
            for (int i = 0; i < renderers.Length; i++)
            {
                float h = bake.lodHeights != null && i < bake.lodHeights.Length ? bake.lodHeights[i] : prev * 0.5f;
                h = Mathf.Clamp(h, 0.0001f, prev - 0.0001f);
                lodArr[i] = new LOD(h, new[] { renderers[i] });
                prev = h;
            }
            if (!SameLods(lg.GetLODs(), lodArr))
            {
                lg.SetLODs(lodArr);
                lg.RecalculateBounds();
            }
            LODFadeMode fade = bake.crossFade ? LODFadeMode.CrossFade : LODFadeMode.None;
            if (lg.fadeMode != fade) lg.fadeMode = fade;
            if (lg.animateCrossFading != bake.crossFade) lg.animateCrossFading = bake.crossFade;
        }

        private static bool SameLods(LOD[] a, LOD[] b)
        {
            if (a == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (Mathf.Abs(a[i].screenRelativeTransitionHeight - b[i].screenRelativeTransitionHeight) > 1e-5f) return false;
                if (a[i].renderers == null || a[i].renderers.Length != 1 || a[i].renderers[0] != b[i].renderers[0]) return false;
            }
            return true;
        }

        private void RemoveBakedPresentation(bool deferDestroy)
        {
            Transform container = transform.Find(LodContainerName);
            var lg = GetComponent<LODGroup>();
            if (lg != null && (container != null || (bake != null && bake.ownsLodGroup)))
                DestroyNow(lg, deferDestroy);
            if (container != null)
                DestroyNow(container.gameObject, deferDestroy);
            if (bake != null) bake.ownsLodGroup = false;
        }

        private static void DestroyNow(Object o, bool defer)
        {
            if (o == null) return;
            if (Application.isPlaying) { Destroy(o); return; }
#if UNITY_EDITOR
            if (defer)
            {
                UnityEditor.EditorApplication.delayCall += () => { if (o != null) DestroyImmediate(o); };
                return;
            }
#endif
            DestroyImmediate(o);
        }

        private void MarkDirty()
        {
#if UNITY_EDITOR
            if (Application.isPlaying) return;
            UnityEditor.EditorUtility.SetDirty(this);
            if (gameObject.scene.IsValid())
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
        }

        // ------------------------------------------------------------------------------------------------

        private static void AddObject(List<Part> parts, GameObject go, Matrix4x4 m)
        {
            Matrix4x4 rootInv = go.transform.worldToLocalMatrix;
            foreach (MeshFilter f in go.GetComponentsInChildren<MeshFilter>(true))
            {
                if (f.sharedMesh == null || f.GetComponentInParent<IPcgSeeded>(true) != null) continue;
                var r = f.GetComponent<MeshRenderer>();
                AddPart(parts, f.sharedMesh, r != null ? r.sharedMaterials : null, m * rootInv * f.transform.localToWorldMatrix);
            }
        }

        private static void AddPart(List<Part> parts, Mesh mesh, Material[] mats, Matrix4x4 m)
        {
            if (mesh == null || mesh.vertexCount == 0) return;
            parts.Add(new Part { mesh = mesh, materials = mats, matrix = m });
        }

        private static bool Readable(Mesh m)
        {
#if UNITY_EDITOR
            return m != null;
#else
            return m != null && m.isReadable;
#endif
        }

        private static Bounds PartsBounds(List<Part> parts, Matrix4x4 pre)
        {
            bool first = true;
            Bounds b = default;
            foreach (Part p in parts)
            {
                Bounds pb = TransformBounds(pre * p.matrix, p.mesh.bounds);
                if (first) { b = pb; first = false; } else b.Encapsulate(pb);
            }
            return b;
        }

        private static Bounds TransformBounds(Matrix4x4 m, Bounds b)
        {
            Vector3 c = m.MultiplyPoint3x4(b.center);
            Vector3 e = b.extents;
            Vector3 ax = m.MultiplyVector(new Vector3(e.x, 0, 0));
            Vector3 ay = m.MultiplyVector(new Vector3(0, e.y, 0));
            Vector3 az = m.MultiplyVector(new Vector3(0, 0, e.z));
            Vector3 ext = new Vector3(
                Mathf.Abs(ax.x) + Mathf.Abs(ay.x) + Mathf.Abs(az.x),
                Mathf.Abs(ax.y) + Mathf.Abs(ay.y) + Mathf.Abs(az.y),
                Mathf.Abs(ax.z) + Mathf.Abs(ay.z) + Mathf.Abs(az.z));
            return new Bounds(c, ext * 2f);
        }

        private static float OverlapFraction(Bounds a, Bounds b)
        {
            Vector3 mn = Vector3.Max(a.min, b.min), mx = Vector3.Min(a.max, b.max);
            Vector3 d = mx - mn;
            if (d.x <= 0 || d.y <= 0 || d.z <= 0) return 0f;
            float inter = d.x * d.y * d.z;
            float va = a.size.x * a.size.y * a.size.z, vb = b.size.x * b.size.y * b.size.z;
            return inter / Mathf.Max(1e-6f, Mathf.Min(va, vb));
        }

        private static bool SameMaterials(Material[] a, Material[] b)
        {
            if (a == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }

        private static GameObject Pick(Random rng, List<GameObject> list)
        {
            if (list == null || list.Count == 0) return null;
            return list[rng.Next(list.Count)];
        }

        private static float Lerp(Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);
        private static float MaxComp(Vector3 v) => Mathf.Max(v.x, Mathf.Max(v.y, v.z));

        private static long MakeStamp()
        {
            long s = Guid.NewGuid().GetHashCode();
            s = (s << 32) ^ DateTime.UtcNow.Ticks;
            return s == 0 ? 1 : s;
        }
    }
}
