using System.Collections.Generic;
using Project.Companions.Invector;
using UnityEngine;

namespace Project.Building
{
    /// <summary>
    /// Force field door (0926-force-fields). The solid box stops bullets, enemies and weather; it switches off while the player
    /// or a companion is passing through, then closes again. Crossing the field plays a ripple, a glow pulse and an electric crackle.
    /// Look, sound and behaviour come from Building Studio > Creation Effects > Force fields.
    /// 0927-ff-corners: a small block sits in each corner, with a glowing strip sandwiched through its middle. With no base
    /// power the field material and collider are off and the strips glow red; once a generator powers it they turn green.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DMForceField : MonoBehaviour
    {
        public const string DefaultMaterialResource = "Building/DM_ForceField";
        public const string CornerMaterialResource = "Building/DM_ForceFieldCorner";
        const string CornerRootName = "FF_Corners";

        const float ActorRefreshSeconds = 0.5f;
        const int RippleSlots = 4;

        static readonly List<Collider> Actors = new List<Collider>();
        static float actorsRefreshedAt = -10f;
        static int actorsFrame = -1;
        static AudioClip generatedCrackle;
        static AudioClip generatedHum;
        static Material defaultMaterial;

        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int EdgeColorId = Shader.PropertyToID("_EdgeColor");
        static readonly int OpacityId = Shader.PropertyToID("_Opacity");
        static readonly int EdgeGlowId = Shader.PropertyToID("_EdgeGlow");
        static readonly int EdgeWidthId = Shader.PropertyToID("_EdgeWidth");
        static readonly int PatternScaleId = Shader.PropertyToID("_PatternScale");
        static readonly int ScrollSpeedId = Shader.PropertyToID("_ScrollSpeed");
        static readonly int PulseId = Shader.PropertyToID("_Pulse");
        static readonly int RippleSpeedId = Shader.PropertyToID("_RippleSpeed");
        static readonly int RippleWidthId = Shader.PropertyToID("_RippleWidth");
        static readonly int RippleLifeId = Shader.PropertyToID("_RippleLife");
        static readonly int[] RippleIds =
        {
            Shader.PropertyToID("_Ripple0"),
            Shader.PropertyToID("_Ripple1"),
            Shader.PropertyToID("_Ripple2"),
            Shader.PropertyToID("_Ripple3"),
        };

        static readonly int EmissiveColorId = Shader.PropertyToID("_EmissiveColor");
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        static readonly int EmissiveExposureWeightId = Shader.PropertyToID("_EmissiveExposureWeight");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int UnlitColorId = Shader.PropertyToID("_UnlitColor");
        static Mesh cubeMesh;
        static Material generatedCornerMaterial;
        static Material generatedStripSource;
        static Material stripOff;
        static Material stripOn;
        static Material stripBuiltFrom;
        static Color stripBuiltOffColor;
        static Color stripBuiltOnColor;
        static float stripBuiltGlow = -1f;
        static int stripFrame = -1;

        BoxCollider solid;
        Renderer body;
        Transform cornerRoot;
        readonly Transform[] corners = new Transform[4];
        readonly Renderer[] cornerShells = new Renderer[8];
        readonly Renderer[] cornerStrips = new Renderer[4];
        Vector4 cornerLayoutKey = new Vector4(-1f, -1f, -1f, -1f);
        Vector3 cornerFitPos = new Vector3(float.NaN, 0f, 0f);
        Quaternion cornerFitRot = Quaternion.identity;
        bool cornerFitted;
        int cornerFitTries;
        float cornerNextFitAt;
        static readonly RaycastHit[] FitHits = new RaycastHit[16];
        Mesh sheetSource;
        Mesh sheetFitted;
        AudioSource passSource;
        AudioSource humSource;
        MaterialPropertyBlock block;
        readonly Dictionary<Collider, float> insideSide = new Dictionary<Collider, float>();
        readonly Dictionary<Collider, float> nextSound = new Dictionary<Collider, float>();
        readonly List<Collider> stale = new List<Collider>();
        readonly Vector4[] ripples = new Vector4[RippleSlots];
        int nextRipple;
        float pulse;
        float closeAt;

        public bool IsOpen => solid != null && !solid.enabled;

        void Awake()
        {
            BoxCollider[] boxes = GetComponents<BoxCollider>();
            for (int i = 0; i < boxes.Length; i++)
            {
                if (!boxes[i].isTrigger)
                {
                    solid = boxes[i];
                    break;
                }
            }

            body = GetComponent<Renderer>();
            block = new MaterialPropertyBlock();
            for (int i = 0; i < RippleSlots; i++)
                ripples[i] = new Vector4(0f, 0f, 0f, -100f);

            passSource = gameObject.AddComponent<AudioSource>();
            SetupSource(passSource);
            ApplyLook();
        }

        void Start()
        {
            ApplyLook();
            BuildCorners();
        }

        void OnEnable()
        {
            DMBasePower.RegisterConsumer(this, true);
        }

        void OnDestroy()
        {
            if (sheetFitted != null)
                Destroy(sheetFitted);
        }

        void OnDisable()
        {
            DMBasePower.UnregisterConsumer(this);
            if (solid != null)
            {
                solid.enabled = true;
                solid.isTrigger = false;
            }
            if (body != null)
                body.enabled = true;
            unpowered = false;
            insideSide.Clear();
            if (humSource != null)
                humSource.Stop();
        }

        /// <summary>Puts the force field material back (after build paint, material swaps, or a Studio change).</summary>
        public void ApplyLook()
        {
            if (body == null)
                return;
            Material target = ResolveMaterial(DMBuildingCreationFxProfile.Live);
            if (target != null)
            {
                Material[] current = body.sharedMaterials;
                bool same = current.Length == 1 && current[0] == target;
                if (!same)
                    body.sharedMaterials = new[] { target };
            }

            PushBlock(DMBuildingCreationFxProfile.Live);
        }

        public static Material ResolveMaterial(DMBuildingCreationFxProfile fx)
        {
            if (fx != null && fx.forceFieldMaterial != null)
                return fx.forceFieldMaterial;
            if (defaultMaterial == null)
                defaultMaterial = Resources.Load<Material>(DefaultMaterialResource);
            return defaultMaterial;
        }

        bool unpowered;

        /// <summary>0926-generator: with no powered generator in the base the field shuts off and anyone can walk through.</summary>
        bool UpdatePower()
        {
            bool powered = !DMBuildingGhostProfile.ForceFieldsNeedPower || DMBasePower.IsPowered(transform.position);
            if (powered == !unpowered)
                return powered;
            unpowered = !powered;
            // 0927-ff-corners: an unpowered field is fully off (no field material, no collider). The corner blocks stay
            // visible with red strips so you can see it was built; build mode still finds it by the nearest-piece fallback.
            if (solid != null)
            {
                solid.isTrigger = false;
                solid.enabled = powered;
            }
            if (body != null)
                body.enabled = powered;
            if (powered)
                pulse = Mathf.Max(pulse, 0.8f);
            PushBlock(DMBuildingCreationFxProfile.Live);
            insideSide.Clear();
            if (!powered && humSource != null)
                humSource.Stop();
            return powered;
        }

        void Update()
        {
            DMBuildingCreationFxProfile fx = DMBuildingCreationFxProfile.Live;
            bool powered = UpdatePower();
            UpdateCorners(fx, powered);
            if (!powered)
            {
                // Build fx, moves or hover highlights can switch the renderer back on; keep the field off until power returns.
                if (body != null && body.enabled)
                    body.enabled = false;
                if (solid != null && solid.enabled)
                    solid.enabled = false;
                return;
            }
            float now = Time.time;
            bool letThrough = fx == null || fx.forceFieldLetFriendliesThrough;
            bool anyInside = false;

            if (solid != null && letThrough)
            {
                RefreshActors(now);
                Vector3 size = solid.size;
                Vector3 half = size * 0.5f;
                int thin = ThinAxis(size);
                float depth = fx != null ? fx.forceFieldSenseDepthMeters : 0.9f;
                Matrix4x4 toLocal = transform.worldToLocalMatrix;

                stale.Clear();
                foreach (KeyValuePair<Collider, float> pair in insideSide)
                    stale.Add(pair.Key);

                for (int i = 0; i < Actors.Count; i++)
                {
                    Collider actor = Actors[i];
                    if (actor == null || !actor.enabled || !actor.gameObject.activeInHierarchy)
                        continue;

                    Bounds bounds = actor.bounds;
                    Vector3 local = toLocal.MultiplyPoint3x4(bounds.center) - solid.center;
                    Vector3 e = bounds.extents;
                    Vector3 limit = half;
                    for (int k = 0; k < 3; k++)
                        limit[k] += Mathf.Abs(toLocal[k, 0]) * e.x + Mathf.Abs(toLocal[k, 1]) * e.y + Mathf.Abs(toLocal[k, 2]) * e.z;
                    limit[thin] += depth;
                    if (Mathf.Abs(local.x) > limit.x || Mathf.Abs(local.y) > limit.y || Mathf.Abs(local.z) > limit.z)
                        continue;

                    anyInside = true;
                    Collider id = actor;
                    stale.Remove(id);
                    float side = Mathf.Sign(local[thin]);
                    if (!insideSide.TryGetValue(id, out float lastSide))
                    {
                        insideSide[id] = side;
                        pulse = Mathf.Max(pulse, 0.35f);
                    }
                    else if (side != lastSide)
                    {
                        insideSide[id] = side;
                        OnCrossed(id, local, half, thin, fx, now);
                    }
                }

                for (int i = 0; i < stale.Count; i++)
                    insideSide.Remove(stale[i]);
            }
            else if (insideSide.Count > 0)
            {
                insideSide.Clear();
            }

            if (solid != null)
            {
                float closeDelay = fx != null ? fx.forceFieldCloseDelaySeconds : 0.35f;
                if (anyInside)
                {
                    solid.enabled = false;
                    closeAt = now + closeDelay;
                }
                else if (!solid.enabled && now >= closeAt)
                {
                    solid.enabled = true;
                }
            }

            float pulseSeconds = fx != null ? fx.forceFieldPulseSeconds : 0.6f;
            pulse = Mathf.MoveTowards(pulse, 0f, Time.deltaTime / Mathf.Max(0.05f, pulseSeconds));
            UpdateHum(fx);
            PushBlock(fx);
        }

        void OnCrossed(Collider id, Vector3 local, Vector3 half, int thin, DMBuildingCreationFxProfile fx, float now)
        {
            Vector3 onPlane = local;
            for (int k = 0; k < 3; k++)
                onPlane[k] = Mathf.Clamp(onPlane[k], -half[k], half[k]);
            onPlane[thin] = 0f;
            Vector3 world = transform.TransformPoint(onPlane + solid.center);
            ripples[nextRipple] = new Vector4(world.x, world.y, world.z, Time.timeSinceLevelLoad);
            nextRipple = (nextRipple + 1) % RippleSlots;
            pulse = 1f;

            float cooldown = fx != null ? fx.forceFieldSoundCooldown : 0.4f;
            if (nextSound.TryGetValue(id, out float allowedAt) && now < allowedAt)
                return;
            nextSound[id] = now + cooldown;

            AudioClip clip = fx != null && fx.forceFieldPassClip != null ? fx.forceFieldPassClip : Crackle();
            float volume = fx != null ? fx.forceFieldPassVolume : 0.35f;
            float jitter = fx != null ? fx.forceFieldPitchJitter : 0.08f;
            if (clip == null || volume <= 0f || passSource == null)
                return;
            passSource.transform.position = transform.position;
            passSource.maxDistance = fx != null ? fx.forceFieldAudioMaxDistance : 14f;
            passSource.pitch = 1f + Random.Range(-jitter, jitter);
            passSource.PlayOneShot(clip, volume);
        }

        // ---------- 0927-ff-corners ----------

        /// <summary>Creates the four corner blocks (two shell halves and a glowing strip each) as children of the field.</summary>
        void BuildCorners()
        {
            if (cornerRoot != null)
                return;
            DMBuildingGhost ghost = GetComponent<DMBuildingGhost>();
            if (ghost != null && !ghost.Built)
                return;

            Transform old = transform.Find(CornerRootName);
            if (old != null)
                Destroy(old.gameObject);

            var rootGo = new GameObject(CornerRootName) { layer = gameObject.layer };
            cornerRoot = rootGo.transform;
            cornerRoot.SetParent(transform, false);
            for (int i = 0; i < 4; i++)
            {
                var corner = new GameObject("Corner" + i) { layer = gameObject.layer };
                corners[i] = corner.transform;
                corners[i].SetParent(cornerRoot, false);
                cornerShells[i * 2] = MakeCube("ShellFront", corners[i], true);
                cornerShells[i * 2 + 1] = MakeCube("ShellBack", corners[i], true);
                cornerStrips[i] = MakeCube("Strip", corners[i], false);
            }

            cornerLayoutKey = new Vector4(-1f, -1f, -1f, -1f);
            UpdateCorners(DMBuildingCreationFxProfile.Live, !unpowered);
        }

        Renderer MakeCube(string name, Transform parent, bool shadows)
        {
            var go = new GameObject(name) { layer = gameObject.layer };
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = CubeMesh();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = shadows;
            return renderer;
        }

        static Mesh CubeMesh()
        {
            if (cubeMesh != null)
                return cubeMesh;
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubeMesh = temp.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(temp);
            return cubeMesh;
        }

        void UpdateCorners(DMBuildingCreationFxProfile fx, bool powered)
        {
            if (cornerRoot == null)
            {
                BuildCorners();
                if (cornerRoot == null)
                    return;
            }

            bool show = fx == null || fx.forceFieldCorners;
            if (cornerRoot.gameObject.activeSelf != show)
                cornerRoot.gameObject.SetActive(show);
            if (!show)
                return;

            LayoutCorners(fx);
            Material shell = CornerMaterial(fx);
            Material strip = StripMaterial(fx, powered);
            for (int i = 0; i < cornerShells.Length; i++)
            {
                if (cornerShells[i] != null && shell != null && cornerShells[i].sharedMaterial != shell)
                    cornerShells[i].sharedMaterial = shell;
            }

            for (int i = 0; i < cornerStrips.Length; i++)
            {
                if (cornerStrips[i] != null && strip != null && cornerStrips[i].sharedMaterial != strip)
                    cornerStrips[i].sharedMaterial = strip;
            }
        }

        /// <summary>Places a block in each corner of the field plane, sized in metres whatever the piece scale.</summary>
        void LayoutCorners(DMBuildingCreationFxProfile fx)
        {
            float size = fx != null ? fx.forceFieldCornerSize : 0.18f;
            float depth = fx != null ? fx.forceFieldCornerDepth : 0.24f;
            float strip = Mathf.Min(fx != null ? fx.forceFieldStripThickness : 0.035f, depth * 0.8f);
            Vector3 lossy = transform.lossyScale;
            var key = new Vector4(size, depth, strip, lossy.x + lossy.y * 7f + lossy.z * 13f);
            // 0927-ff-corner-fit: refit when the piece moves, and retry a few times while the frame's colliders settle.
            bool moved = !((transform.position - cornerFitPos).sqrMagnitude < 0.0001f) || Quaternion.Angle(transform.rotation, cornerFitRot) > 0.1f;
            if (moved)
                cornerFitTries = 0;
            bool retry = !cornerFitted && cornerFitTries < 8 && Time.time >= cornerNextFitAt;
            if (key == cornerLayoutKey && !moved && !retry)
                return;
            cornerLayoutKey = key;
            cornerFitPos = transform.position;
            cornerFitRot = transform.rotation;

            Bounds plane;
            MeshFilter filter = GetComponent<MeshFilter>();
            // 0927-ff-sheet-fit: remember the authored sheet mesh; the fitted copy is only ever derived from it.
            if (filter != null && sheetSource == null && filter.sharedMesh != null && filter.sharedMesh != sheetFitted)
                sheetSource = filter.sharedMesh;
            if (sheetSource != null)
                plane = sheetSource.bounds;
            else if (filter != null && filter.sharedMesh != null)
                plane = filter.sharedMesh.bounds;
            else if (solid != null)
                plane = new Bounds(solid.center, solid.size);
            else
                plane = new Bounds(Vector3.zero, Vector3.one);

            int thin = ThinAxis(plane.size);
            int a = thin == 0 ? 1 : 0;
            int b = thin == 2 ? 1 : 2;
            Vector3 half = plane.extents;
            Vector3 inv = new Vector3(SafeInverse(lossy.x), SafeInverse(lossy.y), SafeInverse(lossy.z));
            float shellDepth = Mathf.Max(0.005f, (depth - strip) * 0.5f);
            float lip = Mathf.Min(0.02f, size * 0.1f);

            // 0927-ff-corner-fit: measure the real opening around the field (header, floor, legs and leg plinths) with short rays
            // along the field plane, so the blocks sit in the frame's inside corners even when the field is seated a little off.
            Vector3 worldCenter = transform.TransformPoint(plane.center);
            Vector3 dirA = AxisDir(a);
            Vector3 dirB = AxisDir(b);
            float halfA = half[a] * Mathf.Abs(lossy[a]);
            float halfB = half[b] * Mathf.Abs(lossy[b]);
            float bLo = -halfB;
            float bHi = halfB;
            bool fitted = false;
            Physics.SyncTransforms();
            if (TryFitEdge(worldCenter, -dirB, halfB, size, out float d))
            {
                bLo = -d;
                fitted = true;
            }
            if (TryFitEdge(worldCenter, dirB, halfB, size, out d))
            {
                bHi = d;
                fitted = true;
            }

            // 0927-ff-sheet-fit: the glowing sheet spans the same opening (legs at mid height, header and floor).
            float aLo = -halfA;
            float aHi = halfA;
            if (TryFitEdge(worldCenter, -dirA, halfA, size, out d))
            {
                aLo = -d;
                fitted = true;
            }
            if (TryFitEdge(worldCenter, dirA, halfA, size, out d))
            {
                aHi = d;
                fitted = true;
            }
            FitSheet(filter, plane, a, b, aLo, aHi, bLo, bHi, lossy);

            for (int i = 0; i < 4; i++)
            {
                if (corners[i] == null)
                    continue;
                float sa = (i & 1) == 0 ? -1f : 1f;
                float sb = (i & 2) == 0 ? -1f : 1f;
                float rowB = bHi - bLo > size ? (sb < 0f ? bLo + size * 0.5f : bHi - size * 0.5f) : (bLo + bHi) * 0.5f;
                Vector3 rowStart = worldCenter + dirB * rowB;
                float edgeA = halfA;
                if (TryFitEdge(rowStart, dirA * sa, halfA, size, out d))
                {
                    edgeA = d;
                    fitted = true;
                }
                Vector3 world = rowStart + dirA * (sa * Mathf.Max(0f, edgeA - size * 0.5f));
                corners[i].localPosition = transform.InverseTransformPoint(world);
                corners[i].localRotation = Quaternion.identity;
                corners[i].localScale = inv;

                Vector3 shellScale = Vector3.zero;
                shellScale[a] = size;
                shellScale[b] = size;
                shellScale[thin] = shellDepth;
                Vector3 offset = Vector3.zero;
                offset[thin] = strip * 0.5f + shellDepth * 0.5f;
                SetBox(cornerShells[i * 2], offset, shellScale);
                SetBox(cornerShells[i * 2 + 1], -offset, shellScale);

                Vector3 stripScale = Vector3.zero;
                stripScale[a] = size + lip;
                stripScale[b] = size + lip;
                stripScale[thin] = strip;
                SetBox(cornerStrips[i], Vector3.zero, stripScale);
            }

            cornerFitted = fitted;
            if (!fitted)
            {
                cornerFitTries++;
                cornerNextFitAt = Time.time + 0.5f;
            }
        }

        /// <summary>0927-ff-sheet-fit: stretches a copy of the sheet mesh so its edges land on the measured opening
        /// (offsets in metres from the plane centre). Collider, UVs and normals are left as authored.</summary>
        void FitSheet(MeshFilter filter, Bounds plane, int a, int b, float aLo, float aHi, float bLo, float bHi, Vector3 lossy)
        {
            if (filter == null || sheetSource == null || !sheetSource.isReadable)
                return;
            float la = Mathf.Abs(lossy[a]) < 0.0001f ? 1f : lossy[a];
            float lb = Mathf.Abs(lossy[b]) < 0.0001f ? 1f : lossy[b];
            float newMinA = plane.center[a] + Mathf.Min(aLo / la, aHi / la);
            float newMaxA = plane.center[a] + Mathf.Max(aLo / la, aHi / la);
            float newMinB = plane.center[b] + Mathf.Min(bLo / lb, bHi / lb);
            float newMaxB = plane.center[b] + Mathf.Max(bLo / lb, bHi / lb);
            float oldMinA = plane.min[a], oldMaxA = plane.max[a];
            float oldMinB = plane.min[b], oldMaxB = plane.max[b];
            const float tolerance = 0.005f;
            if (Mathf.Abs(newMinA - oldMinA) < tolerance && Mathf.Abs(newMaxA - oldMaxA) < tolerance
                && Mathf.Abs(newMinB - oldMinB) < tolerance && Mathf.Abs(newMaxB - oldMaxB) < tolerance)
            {
                if (filter.sharedMesh != sheetSource)
                    filter.sharedMesh = sheetSource;
                return;
            }

            if (sheetFitted == null)
            {
                sheetFitted = Instantiate(sheetSource);
                sheetFitted.name = sheetSource.name + " (fit)";
                sheetFitted.hideFlags = HideFlags.DontSave;
            }

            Vector3[] vertices = sheetSource.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 v = vertices[i];
                v[a] = Remap(v[a], oldMinA, oldMaxA, newMinA, newMaxA);
                v[b] = Remap(v[b], oldMinB, oldMaxB, newMinB, newMaxB);
                vertices[i] = v;
            }

            sheetFitted.vertices = vertices;
            sheetFitted.RecalculateBounds();
            if (filter.sharedMesh != sheetFitted)
                filter.sharedMesh = sheetFitted;
        }

        static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            float span = fromMax - fromMin;
            if (Mathf.Abs(span) < 0.0001f)
                return value + (toMin + toMax - fromMin - fromMax) * 0.5f;
            return toMin + (value - fromMin) / span * (toMax - toMin);
        }

        Vector3 AxisDir(int axis)
        {
            return axis == 0 ? transform.right : axis == 1 ? transform.up : transform.forward;
        }

        /// <summary>0927-ff-corner-fit: distance from origin along dir to the nearest frame, floor or terrain surface, ignoring this
        /// field, other force fields, triggers and moving bodies (players, companions). Hits close to the middle are ignored.</summary>
        bool TryFitEdge(Vector3 origin, Vector3 dir, float half, float size, out float distance)
        {
            distance = half;
            if (half <= 0.01f)
                return false;
            float reach = half + Mathf.Max(0.75f, half * 0.5f);
            int count = Physics.RaycastNonAlloc(origin, dir.normalized, FitHits, reach, ~0, QueryTriggerInteraction.Ignore);
            float best = float.PositiveInfinity;
            for (int h = 0; h < count; h++)
            {
                Collider c = FitHits[h].collider;
                if (c == null || c.transform.IsChildOf(transform))
                    continue;
                if (FitHits[h].distance < Mathf.Max(size, half * 0.5f))
                    continue;
                if (c.GetComponentInParent<DMForceField>() != null)
                    continue;
                bool building = c.GetComponentInParent<DMBuildingGhost>() != null;
                if (!building && (c.attachedRigidbody != null || c is CharacterController))
                    continue;
                if (FitHits[h].distance < best)
                    best = FitHits[h].distance;
            }
            if (float.IsPositiveInfinity(best))
                return false;
            distance = best;
            return true;
        }

        static float SafeInverse(float v)
        {
            return Mathf.Abs(v) < 0.0001f ? 1f : 1f / v;
        }

        static void SetBox(Renderer renderer, Vector3 position, Vector3 scale)
        {
            if (renderer == null)
                return;
            Transform t = renderer.transform;
            t.localPosition = position;
            t.localRotation = Quaternion.identity;
            t.localScale = scale;
        }

        static Material CornerMaterial(DMBuildingCreationFxProfile fx)
        {
            if (fx != null && fx.forceFieldCornerMaterial != null)
                return fx.forceFieldCornerMaterial;
            if (generatedCornerMaterial == null)
            {
                generatedCornerMaterial = Resources.Load<Material>(CornerMaterialResource);
                if (generatedCornerMaterial == null)
                    generatedCornerMaterial = CreateDefaultCornerMaterial();
            }

            return generatedCornerMaterial;
        }

        /// <summary>Plain dark metal HDRP/Lit for the corner blocks (Building Studio saves one as DM_ForceFieldCorner to edit).</summary>
        public static Material CreateDefaultCornerMaterial()
        {
            Shader shader = Shader.Find("HDRP/Lit");
            if (shader == null)
                return null;
            var material = new Material(shader) { name = "DM_ForceFieldCorner (generated)", hideFlags = HideFlags.DontSave };
            material.SetColor(BaseColorId, new Color(0.16f, 0.17f, 0.19f, 1f));
            material.SetFloat("_Metallic", 0.85f);
            material.SetFloat("_Smoothness", 0.5f);
            UnityEngine.Rendering.HighDefinition.HDMaterial.ValidateMaterial(material);
            return material;
        }

        static Material StripMaterial(DMBuildingCreationFxProfile fx, bool powered)
        {
            RefreshStripMaterials(fx);
            return powered ? stripOn : stripOff;
        }

        /// <summary>One red and one green copy of the strip material, shared by every field and rebuilt when the Studio values change.</summary>
        static void RefreshStripMaterials(DMBuildingCreationFxProfile fx)
        {
            if (stripFrame == Time.frameCount && stripOn != null && stripOff != null)
                return;
            stripFrame = Time.frameCount;

            Material source = fx != null && fx.forceFieldStripMaterial != null ? fx.forceFieldStripMaterial : GeneratedStripSource();
            Color off = fx != null ? fx.forceFieldStripUnpoweredColor : new Color(1f, 0.05f, 0.03f, 1f);
            Color on = fx != null ? fx.forceFieldStripPoweredColor : new Color(0.1f, 1f, 0.2f, 1f);
            float glow = fx != null ? fx.forceFieldStripGlow : 4f;
            if (stripOn != null && stripOff != null && source == stripBuiltFrom
                && off == stripBuiltOffColor && on == stripBuiltOnColor && Mathf.Approximately(glow, stripBuiltGlow))
                return;

            if (stripOff != null)
                Destroy(stripOff);
            if (stripOn != null)
                Destroy(stripOn);
            stripBuiltFrom = source;
            stripBuiltOffColor = off;
            stripBuiltOnColor = on;
            stripBuiltGlow = glow;
            stripOff = MakeStrip(source, off, glow, "Unpowered");
            stripOn = MakeStrip(source, on, glow, "Powered");
        }

        static Material GeneratedStripSource()
        {
            if (generatedStripSource != null)
                return generatedStripSource;
            Shader shader = Shader.Find("HDRP/Lit");
            if (shader == null)
                return null;
            generatedStripSource = new Material(shader) { name = "DM_ForceFieldStrip (generated)", hideFlags = HideFlags.DontSave };
            generatedStripSource.SetColor(BaseColorId, Color.black);
            generatedStripSource.SetFloat("_Smoothness", 0.6f);
            UnityEngine.Rendering.HighDefinition.HDMaterial.ValidateMaterial(generatedStripSource);
            return generatedStripSource;
        }

        static Material MakeStrip(Material source, Color color, float glow, string state)
        {
            if (source == null)
                return null;
            var material = new Material(source) { name = "DM_ForceFieldStrip (" + state + ")", hideFlags = HideFlags.DontSave };
            var emit = new Color(color.r * glow, color.g * glow, color.b * glow, 1f);
            var tint = new Color(Mathf.Clamp01(color.r), Mathf.Clamp01(color.g), Mathf.Clamp01(color.b), 1f);
            if (material.HasProperty(EmissiveColorId))
                material.SetColor(EmissiveColorId, emit);
            // Glow the same on screen in daylight or at night instead of vanishing under HDRP auto exposure.
            if (material.HasProperty(EmissiveExposureWeightId))
                material.SetFloat(EmissiveExposureWeightId, 0f);
            if (material.HasProperty(EmissionColorId))
            {
                material.SetColor(EmissionColorId, emit);
                material.EnableKeyword("_EMISSION");
            }

            if (material.HasProperty(UnlitColorId))
                material.SetColor(UnlitColorId, emit);
            if (material.HasProperty(BaseColorId))
                material.SetColor(BaseColorId, tint * 0.25f);
            if (material.HasProperty(ColorId))
                material.SetColor(ColorId, tint);
            return material;
        }

        void UpdateHum(DMBuildingCreationFxProfile fx)
        {
            float volume = fx != null ? fx.forceFieldIdleHumVolume : 0f;
            if (volume <= 0f)
            {
                if (humSource != null && humSource.isPlaying)
                    humSource.Stop();
                return;
            }

            if (humSource == null)
            {
                humSource = gameObject.AddComponent<AudioSource>();
                SetupSource(humSource);
                humSource.loop = true;
            }

            AudioClip clip = fx.forceFieldIdleHumClip != null ? fx.forceFieldIdleHumClip : Hum();
            if (humSource.clip != clip)
            {
                humSource.clip = clip;
                humSource.Stop();
            }

            humSource.maxDistance = fx.forceFieldAudioMaxDistance;
            humSource.volume = volume * (1f + pulse * 0.5f);
            if (!humSource.isPlaying)
            {
                humSource.time = Random.Range(0f, Mathf.Max(0f, clip.length - 0.05f));
                humSource.Play();
            }
        }

        void PushBlock(DMBuildingCreationFxProfile fx)
        {
            if (body == null)
                return;
            // Private non-serialized state is lost on a play-mode script reload (Renderer survives, the block does not).
            if (block == null)
                block = new MaterialPropertyBlock();
            body.GetPropertyBlock(block);
            if (fx != null)
            {
                block.SetColor(ColorId, fx.forceFieldColor);
                block.SetColor(EdgeColorId, fx.forceFieldEdgeColor);
                float dim = unpowered ? 0.3f : 1f;
                block.SetFloat(OpacityId, fx.forceFieldOpacity * dim);
                block.SetFloat(EdgeGlowId, fx.forceFieldEdgeGlow * dim);
                block.SetFloat(EdgeWidthId, fx.forceFieldEdgeWidth);
                block.SetFloat(PatternScaleId, fx.forceFieldPatternScale);
                block.SetFloat(ScrollSpeedId, unpowered ? 0f : fx.forceFieldScrollSpeed);
                block.SetFloat(RippleSpeedId, fx.forceFieldRippleSpeed);
                block.SetFloat(RippleWidthId, fx.forceFieldRippleWidth);
                block.SetFloat(RippleLifeId, fx.forceFieldPulseSeconds);
                block.SetFloat(PulseId, pulse * fx.forceFieldPulseBrightness);
            }
            else
            {
                block.SetFloat(PulseId, pulse * 1.5f);
            }

            for (int i = 0; i < RippleSlots; i++)
                block.SetVector(RippleIds[i], ripples[i]);
            body.SetPropertyBlock(block);
        }

        static void SetupSource(AudioSource source)
        {
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1.5f;
            source.maxDistance = 14f;
            source.dopplerLevel = 0f;
        }

        static int ThinAxis(Vector3 size)
        {
            if (size.x <= size.y && size.x <= size.z)
                return 0;
            return size.y <= size.z ? 1 : 2;
        }

        /// <summary>Player plus every companion, cached for all fields and refreshed twice a second.</summary>
        static void RefreshActors(float now)
        {
            if (actorsFrame == Time.frameCount || now - actorsRefreshedAt < ActorRefreshSeconds)
                return;
            actorsFrame = Time.frameCount;
            actorsRefreshedAt = now;
            Actors.Clear();

            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            for (int i = 0; i < players.Length; i++)
                AddActor(players[i]);

            CompanionInvectorBootstrap[] companions = FindObjectsByType<CompanionInvectorBootstrap>(FindObjectsInactive.Exclude);
            for (int i = 0; i < companions.Length; i++)
                AddActor(companions[i].gameObject);
        }

        static void AddActor(GameObject go)
        {
            if (go == null)
                return;
            Collider best = go.GetComponent<CapsuleCollider>();
            if (best == null || best.isTrigger)
                best = go.GetComponent<CharacterController>();
            if (best == null)
            {
                Collider[] all = go.GetComponentsInChildren<Collider>();
                for (int i = 0; i < all.Length; i++)
                {
                    if (!all[i].isTrigger)
                    {
                        best = all[i];
                        break;
                    }
                }
            }

            if (best != null && !Actors.Contains(best))
                Actors.Add(best);
        }

        /// <summary>About half a second of electric crackle built in code: a buzz, fizzing noise and random spark pops.</summary>
        static AudioClip Crackle()
        {
            if (generatedCrackle != null)
                return generatedCrackle;

            const int rate = 44100;
            int count = Mathf.RoundToInt(rate * 0.5f);
            var data = new float[count];
            var rng = new System.Random(7331);
            float hp = 0f;
            float previous = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                float envelope = Mathf.Clamp01(t / 0.006f) * Mathf.Exp(-t * 7f);
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                hp = 0.85f * (hp + white - previous);
                previous = white;
                float wobble = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 11f * t);
                float buzz = (Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.35f + Mathf.Sin(2f * Mathf.PI * 240f * t) * 0.2f
                              + Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 60f * t)) * 0.08f) * wobble;
                data[i] = (hp * 0.3f + buzz * 0.3f) * envelope;
            }

            for (int s = 0; s < 28; s++)
            {
                int start = rng.Next(0, count - 900);
                float amplitude = (0.35f + (float)rng.NextDouble() * 0.65f) * (1f - start / (float)count);
                float decay = 40f + (float)rng.NextDouble() * 120f;
                for (int j = 0; j < 900; j++)
                    data[start + j] += amplitude * (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-j / decay);
            }

            Normalize(data, 0.9f);
            generatedCrackle = AudioClip.Create("DM_ForceFieldCrackle", count, 1, rate, false);
            generatedCrackle.SetData(data, 0);
            return generatedCrackle;
        }

        /// <summary>A two-second seamless loop of low electrical hum for the optional idle sound.</summary>
        static AudioClip Hum()
        {
            if (generatedHum != null)
                return generatedHum;

            const int rate = 44100;
            int count = rate * 2;
            var data = new float[count];
            var rng = new System.Random(4242);
            float lp = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += (white - lp) * 0.05f;
                float swell = 0.8f + 0.2f * Mathf.Sin(2f * Mathf.PI * 0.5f * t);
                data[i] = (Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.3f
                           + Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.1f + lp * 0.4f) * swell;
            }

            Normalize(data, 0.6f);
            generatedHum = AudioClip.Create("DM_ForceFieldHum", count, 1, rate, false);
            generatedHum.SetData(data, 0);
            return generatedHum;
        }

        static void Normalize(float[] data, float peak)
        {
            float max = 0.0001f;
            for (int i = 0; i < data.Length; i++)
                max = Mathf.Max(max, Mathf.Abs(data[i]));
            float gain = peak / max;
            for (int i = 0; i < data.Length; i++)
                data[i] *= gain;
        }
    }
}
