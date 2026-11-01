// 2026-10-30f: Pre-Prologue Set (Mars / AAPS-07) set-dressing spawner.
// Everything spawns under ONE root "PrePrologue_Set" (zones: Fence, Landing, Maintenance, Junk, O2_Terraform, Props), so Clear = fully reversible.
// Area = measured building footprint (x -976..-758, z -402..-280) + margin; huge-bounds MARS bundles are ignored.
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.GenesisStudio
{
    public sealed class DMPrePrologueSetTool : EditorWindow
    {
        public const string RootName = "PrePrologue_Set";
        static readonly string[] Zones = { "Fence", "Landing", "Maintenance", "Junk", "O2_Terraform", "Props" };

        // 30k: NO PolygonSciFiWorlds (Anthony). Creepy Cat / Buildings_constructor / SciFi_Industrial only.
        const string CC = "Assets/_Creepy_Cat/_3D Scifi Kit Vol 3/Prefabs/Props/";
        const string BC = "Assets/Buildings_constructor/Prefabs/";
        const string SIK = "Assets/SciFi_Industrial_LevelKit_2.0/Models/prefabs/v 2.0/";
        const string CU = CC + "_Update 1.00-First build/", CR = CC + "_Update 1.04-Mars Rover & DropShip/_Props/", CM = CC + "_Machines & Computers/";
        const float FenceScale = 3f; // BC Fence_07/08 are ~1 m tall -> ~3 m perimeter
        const float PadScale = 7f;   // BC Pad_04 is 3 x 3 m -> 21 m landing pad

        public static readonly string[] ShuttleOptions =
        {
            CC + "_Update 1.04-Mars Rover & DropShip/_Props/P_Drop_Ship_Mark_01.prefab",
            CC + "_Update 1.05-Spaceship Akorn/_Spaceship/P_Spaceship_Akorn_Mark_1.prefab",
        };
        static readonly string[] ShuttleNames = { "P_Drop_Ship_Mark_01 (default)", "Akorn Mark 1" };

        // Settings (EditorPrefs-backed)
        public static int Seed { get => EditorPrefs.GetInt("DMPPS.seed", 2160); set => EditorPrefs.SetInt("DMPPS.seed", value); }
        public static float Density { get => EditorPrefs.GetFloat("DMPPS.density", 1f); set => EditorPrefs.SetFloat("DMPPS.density", value); }
        public static int Shuttle { get => EditorPrefs.GetInt("DMPPS.shuttle", 0); set => EditorPrefs.SetInt("DMPPS.shuttle", value); }

        // Fence rectangle (world XZ) + gates
        public static float XMin = -1000f, XMax = -735f, ZMin = -430f, ZMax = -215f;
        public static float SouthGateX = -867f, WestGateZ = -322f;
        // Built footprint (keep-out for scatter)
        static readonly Rect Built = Rect.MinMaxRect(-980f, -406f, -754f, -276f);

        static System.Random rng;
        static readonly Dictionary<string, Vector3> sizeCache = new Dictionary<string, Vector3>();

        [MenuItem("Tools/Genesis Studio/Pre-Prologue Set")]
        public static void Open() { GetWindow<DMPrePrologueSetTool>("Pre-Prologue Set"); }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Pre-Prologue Set (Mars AAPS-07)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Spawns under '" + RootName + "' only. Clear removes just that root/zone. Save the scene yourself.", MessageType.Info);
            Seed = EditorGUILayout.IntField("Seed", Seed);
            Density = EditorGUILayout.Slider("Density", Density, 0.1f, 4f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Subtle (0.5)")) Density = 0.5f;
            if (GUILayout.Button("Default (1.0)")) Density = 1f;
            if (GUILayout.Button("Extreme (2.0)")) Density = 2f;
            EditorGUILayout.EndHorizontal();
            Shuttle = EditorGUILayout.Popup("Shuttle", Mathf.Clamp(Shuttle, 0, ShuttleOptions.Length - 1), ShuttleNames);
            EditorGUILayout.Space();
            foreach (var z in Zones)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(z, GUILayout.Width(110));
                if (GUILayout.Button("Spawn")) SpawnZone(z);
                if (GUILayout.Button("Clear")) ClearZone(z);
                EditorGUILayout.LabelField(CountZone(z) + " objs", GUILayout.Width(70));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Spawn All")) SpawnAll();
            if (GUILayout.Button("Clear All")) ClearAll();
            EditorGUILayout.EndHorizontal();
        }

        // ---------- public API ----------
        public static string SpawnAll()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var z in Zones) sb.Append(z).Append('=').Append(SpawnZone(z)).Append(' ');
            return sb.ToString();
        }

        public static void ClearAll()
        {
            var r = GameObject.Find(RootName);
            if (r != null) Undo.DestroyObjectImmediate(r);
        }

        public static int CountZone(string zone)
        {
            var r = GameObject.Find(RootName); if (r == null) return 0;
            var t = r.transform.Find(zone); return t == null ? 0 : t.childCount;
        }

        public static void ClearZone(string zone)
        {
            var r = GameObject.Find(RootName); if (r == null) return;
            var t = r.transform.Find(zone); if (t != null) Undo.DestroyObjectImmediate(t.gameObject);
        }

        public static int SpawnZone(string zone)
        {
            ClearZone(zone);
            Transform parent = ZoneRoot(zone);
            rng = new System.Random(Seed * 31 + zone.GetHashCode());
            switch (zone)
            {
                case "Fence": SpawnFence(parent); break;
                case "Landing": SpawnLanding(parent); break;
                case "Maintenance": SpawnMaintenance(parent); break;
                case "Junk": SpawnJunk(parent); break;
                case "O2_Terraform": SpawnO2(parent); break;
                case "Props": SpawnProps(parent); break;
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(parent.gameObject.scene);
            return parent.childCount;
        }

        // ---------- zones ----------
        static void SpawnFence(Transform p)
        {
            string[] wall = { BC + "Fence/Fence_07.prefab", BC + "Fence/Fence_08.prefab" };
            string post = CU + "_Props/P_Wall_Fence_02.prefab"; // 3.2 m post: gate posts
            string tower = CM + "P_Radar_Pylon_01.prefab";     // ~22 m light/sensor pylon every ~60 m + corners
            Vector2 sw = new Vector2(XMin, ZMin), se = new Vector2(XMax, ZMin), ne = new Vector2(XMax, ZMax), nw = new Vector2(XMin, ZMax);
            Side(p, sw, se, wall, tower, post, SouthGateX - XMin, FenceScale);
            Side(p, se, ne, wall, tower, null, -1f, FenceScale);
            Side(p, ne, nw, wall, tower, null, -1f, FenceScale);
            Side(p, nw, sw, wall, tower, post, ZMax - WestGateZ, FenceScale);
            string[] lt = { BC + "Fence/Fence_01.prefab", BC + "Fence/Fence_02.prefab" }; // junk-yard ring
            Side(p, new Vector2(-947f, -405f), new Vector2(-947f, -428f), lt, null, null, -1f, 2f);
            Side(p, new Vector2(-995f, -405f), new Vector2(-947f, -405f), lt, null, null, -1f, 2f);
            Place(tower, p, sw.x, sw.y, 0f, 0.6f); Place(tower, p, se.x, se.y, 90f, 0.6f); Place(tower, p, ne.x, ne.y, 180f, 0.6f); Place(tower, p, nw.x, nw.y, 270f, 0.6f);
        }

        // Walks a side laying measured pieces; gate centred at gateAt metres from 'a'; a tower every ~60 m.
        static void Side(Transform p, Vector2 a, Vector2 b, string[] pieces, string tower, string gatePost, float gateAt, float sc = 1f)
        {
            Vector2 d = b - a; float len = d.magnitude; Vector2 u = d / len;
            float yaw = Mathf.Atan2(u.x, u.y) * Mathf.Rad2Deg;
            float pos = 4f, nextTower = 60f;
            const float gateLen = 10f; // open gate gap with a post each side
            bool gateDone = gatePost == null;
            int i = 0;
            while (pos < len - 4f)
            {
                if (!gateDone && pos + 8f >= gateAt - gateLen * 0.5f)
                {
                    Vector2 g0 = a + u * (gateAt - gateLen * 0.5f), g1 = a + u * (gateAt + gateLen * 0.5f);
                    Place(gatePost, p, g0.x, g0.y, yaw, 1f); Place(gatePost, p, g1.x, g1.y, yaw, 1f);
                    pos = gateAt + gateLen * 0.5f; gateDone = true; continue;
                }
                string pc = pieces[i++ % pieces.Length];
                float L = Mathf.Max(0.5f, LongAxis(pc, out bool alongZ) * sc);
                if (pos + L > len - 2f) break;
                Vector2 m = a + u * (pos + L * 0.5f);
                Place(pc, p, m.x, m.y, yaw + (alongZ ? 0f : -90f), sc);
                pos += L;
                if (tower != null && pos >= nextTower && pos < len - 20f) { Vector2 t = a + u * (pos + 9f); Place(tower, p, t.x - u.y * 10f, t.y + u.x * 10f, yaw, 0.6f); nextTower += 60f; }
            }
        }

        static void SpawnLanding(Transform p)
        {
            string pad = BC + "Pads/Pad_04.prefab";
            float x = -905f;
            for (int i = 0; i < 3; i++)
            {
                float w = Mathf.Max(12f, Size(pad).x * PadScale);
                Place(pad, p, x + w * 0.5f, -245f, 0f, PadScale);
                if (i < 2) Place(ShuttleOptions[Mathf.Clamp(Shuttle, 0, ShuttleOptions.Length - 1)], p, x + w * 0.5f, -245f, 180f, ShuttleScale());
                x += w + 6f;
            }
            Place(CM + "P_Radar_Pylon_01.prefab", p, -920f, -232f, 90f); // control / traffic tower
            Scatter(p, new[] { CU + "_Props/P_Container_01.prefab", CU + "_Props/P_Container_03.prefab", CU + "_Props/P_Crate_Simple_01.prefab" }, Rect.MinMaxRect(-925f, -265f, -815f, -255f), N(6));
        }

        static void SpawnMaintenance(Transform p)
        {
            Place(CR + "P_Rover_Mark_01.prefab", p, -968f, -296f, 200f);
            Place(CR + "P_Rover_Mark_02.prefab", p, -952f, -296f, 160f);
            Place(CR + "P_Rover_Body_01.prefab", p, -987f, -305f, 30f);
            Place(CR + "P_Rover_Body_02.prefab", p, -985f, -337f, 90f); // replaces old buggies
            Place(CR + "P_Rover_Big_Wheel_Left_01.prefab", p, -982f, -300f, 70f);
            Place(CR + "P_Rover_Big_Wheel_Right_01.prefab", p, -990f, -311f, 10f);
            Place(CR + "P_Landing_Gear_01.prefab", p, -986f, -350f, 40f);
            Place(BC + "Extras/Extras_02_pad.prefab", p, -970f, -365f, 0f); // replaces hover pad
            Place(CC + "_Update 1.05-Spaceship Akorn/_Spaceship/P_Spaceship_Akorn_Mark_2.prefab", p, -972f, -380f, 120f); // grounded shuttle (QM.8)
            Scatter(p, new[] { CU + "_Props/P_Crate_Simple_01.prefab", CU + "_Props/P_Crate_Simple_02.prefab", CU + "_Props/P_Char_Crate_01.prefab", CU + "_Props/P_Barrel_01.prefab" }, Rect.MinMaxRect(-995f, -395f, -978f, -290f), N(8));
        }

        static void SpawnJunk(Transform p)
        {
            var junk = new List<string>();
            for (int i = 1; i <= 8; i++) junk.Add(BC + "Boxes/box" + i + ".prefab");
            for (int i = 1; i <= 3; i++) { junk.Add(CU + "_Builds/P_Pipe_Tube_0" + i + ".prefab"); junk.Add(CU + "_Props/P_Barrel_0" + i + ".prefab"); }
            for (int i = 0; i <= 4; i++) junk.Add(SIK + "Pipes/Pipe_A_1mts_0" + i + ".prefab");
            junk.Add(CR + "P_Rover_Body_02.prefab"); junk.Add(CR + "P_Landing_Gear_01.prefab"); junk.Add(CR + "P_Reactor_01_B.prefab");
            Scatter(p, junk.ToArray(), Rect.MinMaxRect(-995f, -427f, -950f, -408f), N(18));
            Scatter(p, new[] { CU + "_Props/P_Container_02.prefab", CU + "_Props/P_Container_04.prefab", CU + "_Props/P_Char_Barrels_01.prefab" }, Rect.MinMaxRect(-995f, -427f, -950f, -408f), N(6));
        }

        static void SpawnO2(Transform p)
        {
            int silos = Mathf.Clamp(N(6), 2, 12);
            for (int i = 0; i < silos; i++)
                Place(CU + "_Props/P_Big_Oxygen_Tank_01.prefab", p, -824f + (i % 5) * 13f, -414f - (i / 5) * 13f, 0f); // ~11 m O2 tanks
            Place(CR + "P_Reactor_01.prefab", p, -752f, -418f, 0f);
            Place(CR + "P_Reactor_03.prefab", p, -752f, -403f, 0f);
            for (int i = 0; i < 3; i++) Place(CR + "P_Reactor_02.prefab", p, -820f + i * 14f, -427f, 90f);
            for (int i = 0; i < 4; i++) Place(CU + "_Builds/P_Pipe_Tube_0" + (1 + i % 3) + ".prefab", p, -815f + i * 12f, -398f, 90f);
            Place(SIK + "_Prebuilt Modules/Buildings/Big/Big TOWER 1.prefab", p, -770f, -425f, 0f, 0.35f);
            Place(SIK + "_Prebuilt Modules/Buildings/Big/Big TOWER 2.prefab", p, -790f, -425f, 0f, 0.3f);
            Place(CM + "P_Big_Radar_01.prefab", p, -745f, -385f, 0f); // comms dish
            Scatter(p, new[] { CU + "_Builds/P_Solar_Panel_01.prefab", CU + "_Builds/P_Solar_Panel_02.prefab", CU + "_Builds/P_Solar_Panel_04.prefab" }, Rect.MinMaxRect(-790f, -428f, -765f, -408f), N(4));
        }

        static void SpawnProps(Transform p)
        {
            var list = new List<string>();
            for (int i = 1; i <= 8; i++) list.Add(BC + "Boxes/box" + i + ".prefab");
            foreach (var n in new[] { "P_Crate_Simple_01", "P_Crate_Simple_02", "P_Char_Crate_01", "P_Container_01", "P_Container_02", "P_Barrel_01", "P_Barrel_02", "P_Char_Barrels_01", "P_Antena_01", "P_Antena_02", "P_Radar_Base_01", "P_Radar_Base_02" })
                list.Add(CU + "_Props/" + n + ".prefab");
            int want = N(30), tries = 0, made = 0;
            while (made < want && tries++ < want * 30)
            {
                float x = Rf(XMin + 6f, XMax - 6f), z = Rf(ZMin + 6f, ZMax - 6f);
                if (Built.Contains(new Vector2(x, z))) continue;
                if (z > -270f) continue;
                if (x < -945f && z < -290f) continue;
                if (x > -830f && z < -395f) continue;
                if (Place(list[rng.Next(list.Count)], p, x, z, Rf(0f, 360f)) != null) made++;
            }
        }

        // ---------- helpers ----------
        static float ShuttleScale() { float l = Mathf.Max(Size(ShuttleOptions[Mathf.Clamp(Shuttle, 0, ShuttleOptions.Length - 1)]).x, Size(ShuttleOptions[Mathf.Clamp(Shuttle, 0, ShuttleOptions.Length - 1)]).z); return Mathf.Clamp(20f / Mathf.Max(1f, l), 0.2f, 1.5f); } // fit ~20 m on a 22 m pad
        static int N(int baseCount) { return Mathf.Max(1, Mathf.RoundToInt(baseCount * Density)); }
        static float Rf(float a, float b) { return a + (float)rng.NextDouble() * (b - a); }

        static void Scatter(Transform p, string[] list, Rect r, int count)
        {
            int tries = 0, made = 0;
            while (made < count && tries++ < count * 20)
                if (Place(list[rng.Next(list.Length)], p, Rf(r.xMin, r.xMax), Rf(r.yMin, r.yMax), Rf(0f, 360f)) != null) made++;
        }

        static Transform ZoneRoot(string zone)
        {
            var r = GameObject.Find(RootName);
            if (r == null) { r = new GameObject(RootName); Undo.RegisterCreatedObjectUndo(r, "Pre-Prologue Set"); }
            var t = r.transform.Find(zone);
            if (t == null) { var g = new GameObject(zone); Undo.RegisterCreatedObjectUndo(g, "Pre-Prologue Set"); g.transform.SetParent(r.transform, false); t = g.transform; }
            return t;
        }

        static float Ground(float x, float z)
        {
            var t = Terrain.activeTerrain;
            return t != null ? t.SampleHeight(new Vector3(x, 0f, z)) + t.transform.position.y : 0f;
        }

        public static GameObject Place(string path, Transform parent, float x, float z, float yaw, float scale = 1f)
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (pf == null) { Debug.LogWarning("[PrePrologueSet] missing prefab " + path); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, parent);
            Undo.RegisterCreatedObjectUndo(go, "Pre-Prologue Set");
            go.transform.SetPositionAndRotation(new Vector3(x, Ground(x, z), z), Quaternion.Euler(0f, yaw, 0f));
            if (!Mathf.Approximately(scale, 1f)) go.transform.localScale = pf.transform.localScale * scale;
            SnapBottom(go.transform); // 30k
            DMStudioPerf30m.ApplyToSpawned(go); // 30m spawn defaults: small shadows off + Static
            return go;
        }

        // 2026-10-30k: robust renderer bounds (particles + strays > maxDist m from pivot ignored).
        public static bool RobustBounds(Transform t, out Bounds b, float maxDist = 60f)
        {
            bool has = false; b = default;
            Vector2 p0 = new Vector2(t.position.x, t.position.z);
            foreach (var r in t.GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer || !r.enabled) continue;
                Bounds rb = r.bounds;
                if (Vector2.Distance(new Vector2(rb.center.x, rb.center.z), p0) > maxDist + rb.extents.magnitude * 0.5f && Vector2.Distance(new Vector2(rb.center.x, rb.center.z), p0) > maxDist) continue;
                if (!has) { b = rb; has = true; } else b.Encapsulate(rb);
            }
            return has;
        }

        // 30k: ground under footprint = mean of centre + 4 inset corners; bounds bottom placed on it. Returns delta Y.
        public static float SnapBottom(Transform t)
        {
            if (!RobustBounds(t, out Bounds b)) return 0f;
            float ex = b.extents.x * 0.7f, ez = b.extents.z * 0.7f, c = b.center.x, d = b.center.z;
            float g = (Ground(c, d) * 2f + Ground(c - ex, d - ez) + Ground(c + ex, d - ez) + Ground(c - ex, d + ez) + Ground(c + ex, d + ez)) / 6f;
            float dy = g - b.min.y;
            if (Mathf.Abs(dy) > 0.001f) { Undo.RecordObject(t, "Align to terrain"); t.position += new Vector3(0f, dy, 0f); }
            return dy;
        }

        public static float GroundAt(float x, float z) { return Ground(x, z); }

        static void _unused30k()
        {
        }

        // Measured prefab size (renderer bounds at identity), cached.
        public static Vector3 Size(string path)
        {
            if (sizeCache.TryGetValue(path, out var s)) return s;
            s = Vector3.one;
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (pf != null)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(pf);
                go.hideFlags = HideFlags.HideAndDontSave;
                go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                bool has = false; Bounds b = default;
                foreach (var rr in go.GetComponentsInChildren<Renderer>())
                { if (rr is ParticleSystemRenderer) continue; if (!has) { b = rr.bounds; has = true; } else b.Encapsulate(rr.bounds); }
                if (has) s = b.size;
                DestroyImmediate(go);
            }
            sizeCache[path] = s;
            return s;
        }

        static float LongAxis(string path, out bool alongZ)
        {
            var s = Size(path); alongZ = s.z > s.x; return alongZ ? s.z : s.x;
        }
    }
}