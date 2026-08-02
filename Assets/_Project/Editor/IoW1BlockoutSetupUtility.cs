#if UNITY_EDITOR
using Project.EditorTools.Map;
using Project.Map;
using Project.Survival.Exposure;
using Project.Survival.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.EditorTools.World
{
    /// <summary>
    /// One-click IO-W1-01 greybox: heightmap terrain + colony/B6 anchors +
    /// mixed exposure volume + vehicle path tags + fog reveal hook.
    /// </summary>
    public static class IoW1BlockoutSetupUtility
    {
        private const string ScenePath = "Assets/_Project/Scenes/Io_MainMap_W1.unity";
        private const string MixedHazardPrefab =
            "Assets/_Project/Prefabs/Environment/Exposure/Mixed_Hazard.prefab";

        [MenuItem(SurvivalPioneerEditorMenus.World + "W1 Build Main Map Blockout (IO-W1-01)", false, 30)]
        public static void BuildW1Blockout()
        {
            BiomeRegionDataUtility.CreateBiomeRegionAssets();

            Scene scene = EnsureW1Scene();
            if (!IoPlanHeightmapTerrainImporter.TryImportHeightmapToTerrainData(
                    out string importMessage,
                    placeInScene: true))
            {
                EditorUtility.DisplayDialog("W1 Blockout", importMessage, "OK");
                return;
            }

            TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(
                IoPlanHeightmapTerrainImporter.TerrainDataPath);
            Terrain terrain = IoPlanHeightmapTerrainImporter.EnsureTerrainInOpenScene(data);
            if (terrain == null)
            {
                EditorUtility.DisplayDialog("W1 Blockout", "Terrain create failed.\n" + importMessage, "OK");
                return;
            }

            Transform root = FindOrCreateRoot();
            Transform colony = EnsureAnchor(
                root,
                "CommandCenter_Colony",
                IoSurfaceWorldScale.CommandCenterMapUv,
                sampleTerrainY: true);
            Transform hub = EnsureAnchor(
                root,
                "B6_BasaltHighlands_Hub",
                IoSurfaceWorldScale.BasaltHighlandsHubMapUv,
                sampleTerrainY: true);

            PlaceColonyShelter(root, colony.position);
            PlaceB6MixedExposure(root, hub.position);
            Transform[] pathTags = PlaceVehiclePathTags(root, colony.position, hub.position);

            IoW1BlockoutMarkers markers = root.GetComponent<IoW1BlockoutMarkers>();
            if (markers == null)
                markers = Undo.AddComponent<IoW1BlockoutMarkers>(root.gameObject);
            markers.EditorBindAnchors(colony, hub, pathTags);
            EditorUtility.SetDirty(markers);

            EnsureMapMarker(colony, "Command Center", new Color(0.83f, 0.63f, 0.09f));
            EnsureMapMarker(hub, "B6 Highlands Hub", new Color(0.55f, 0.45f, 0.38f));

            WorldMapProvider provider = terrain.GetComponent<WorldMapProvider>();
            if (provider == null)
                provider = Undo.AddComponent<WorldMapProvider>(terrain.gameObject);

            SerializedObject serializedProvider = new SerializedObject(provider);
            serializedProvider.FindProperty("terrain").objectReferenceValue = terrain;
            serializedProvider.FindProperty("useTerrainBounds").boolValue = true;
            serializedProvider.FindProperty("preferTerrainGeneratedMap").boolValue = true;
            serializedProvider.FindProperty("buildTerrainTextureAtRuntime").boolValue = true;
            serializedProvider.ApplyModifiedPropertiesWithoutUndo();
            provider.RefreshWorldBounds();
            EditorUtility.SetDirty(provider);

            MapTerrainSyncUtility.SyncActiveSceneMapToTerrain();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = root.gameObject;

            EditorUtility.DisplayDialog(
                "W1 Blockout",
                "IO-W1-01 greybox built:\n" +
                $"• Terrain {IoSurfaceWorldScale.MainMapSpanMeters:0} m / {IoSurfaceWorldScale.MaxTerrainHeightMeters:0} m height\n" +
                "• BiomeRegionData B1–B7 refreshed to plan-map UVs\n" +
                "• Command Center + B6 hub anchors\n" +
                "• B6 Mixed Hazard exposure (stub)\n" +
                "• Vehicle path tags colony → B6\n" +
                "• Fog reveal hook on IoW1BlockoutMarkers\n\n" +
                $"Scene: {ScenePath}\n" +
                "Play and open map to verify colony + B6 fog sector.",
                "OK");
        }

        private static Scene EnsureW1Scene()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.IsValid() && active.path == ScenePath)
                return active;

            if (System.IO.File.Exists(ScenePath))
                return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EnsureAssetFolder("Assets/_Project/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            return scene;
        }

        private static Transform FindOrCreateRoot()
        {
            GameObject existing = GameObject.Find(IoW1BlockoutMarkers.RootName);
            if (existing != null)
                return existing.transform;

            GameObject root = new GameObject(IoW1BlockoutMarkers.RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create W1 Blockout Root");
            return root.transform;
        }

        private static Transform EnsureAnchor(Transform root, string name, Vector2 mapUv, bool sampleTerrainY)
        {
            Transform existing = root.Find(name);
            GameObject go = existing != null ? existing.gameObject : new GameObject(name);
            if (existing == null)
            {
                Undo.RegisterCreatedObjectUndo(go, "Create W1 Anchor");
                go.transform.SetParent(root, false);
            }

            Vector3 pos = IoSurfaceWorldScale.MapUvToWorld(mapUv);
            if (sampleTerrainY)
            {
                Terrain terrain = Object.FindAnyObjectByType<Terrain>();
                if (terrain != null)
                    pos.y = terrain.SampleHeight(pos) + terrain.transform.position.y;
            }

            go.transform.position = pos;
            return go.transform;
        }

        private static void PlaceColonyShelter(Transform root, Vector3 colonyPos)
        {
            const string name = "Colony_ShelterSafe";
            Transform existing = root.Find(name);
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            ExposureZoneProfile shelter = AssetDatabase.LoadAssetAtPath<ExposureZoneProfile>(
                "Assets/_Project/Data/Exposure/Exposure_Shelter_Safe.asset");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/Environment/Exposure/Shelter_Safe_Zone.prefab");

            GameObject instance;
            if (prefab != null)
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            }
            else
            {
                instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.DestroyImmediate(instance.GetComponent<Collider>());
                BoxCollider box = instance.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(40f, 20f, 40f);
                ExposureZoneVolume volume = instance.AddComponent<ExposureZoneVolume>();
                SerializedObject so = new SerializedObject(volume);
                so.FindProperty("profile").objectReferenceValue = shelter;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            instance.name = name;
            instance.transform.SetParent(root, true);
            instance.transform.position = colonyPos + Vector3.up * 2f;
            Undo.RegisterCreatedObjectUndo(instance, "Place Colony Shelter");
        }

        private static void PlaceB6MixedExposure(Transform root, Vector3 hubPos)
        {
            const string name = "B6_MixedHazard_Exposure";
            Transform existing = root.Find(name);
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MixedHazardPrefab);
            GameObject instance;
            if (prefab != null)
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            }
            else
            {
                ExposureZoneProfile mixed = AssetDatabase.LoadAssetAtPath<ExposureZoneProfile>(
                    "Assets/_Project/Data/Exposure/Exposure_Mixed_Hazard.asset");
                instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.DestroyImmediate(instance.GetComponent<MeshRenderer>());
                BoxCollider box = instance.GetComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(220f, 40f, 220f);
                ExposureZoneVolume volume = instance.AddComponent<ExposureZoneVolume>();
                SerializedObject so = new SerializedObject(volume);
                so.FindProperty("profile").objectReferenceValue = mixed;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            instance.name = name;
            instance.transform.SetParent(root, true);
            instance.transform.position = hubPos + Vector3.up * 8f;
            instance.transform.localScale = Vector3.one;

            BoxCollider trigger = instance.GetComponentInChildren<BoxCollider>();
            if (trigger != null)
            {
                trigger.isTrigger = true;
                if (trigger.size.x < 100f)
                    trigger.size = new Vector3(220f, 40f, 220f);
            }

            Undo.RegisterCreatedObjectUndo(instance, "Place B6 Mixed Exposure");
        }

        private static Transform[] PlaceVehiclePathTags(Transform root, Vector3 colony, Vector3 hub)
        {
            Transform folder = root.Find("VehiclePathTags");
            if (folder != null)
                Object.DestroyImmediate(folder.gameObject);

            GameObject folderGo = new GameObject("VehiclePathTags");
            Undo.RegisterCreatedObjectUndo(folderGo, "Create Vehicle Path Tags");
            folderGo.transform.SetParent(root, false);

            Vector3[] points =
            {
                colony,
                Vector3.Lerp(colony, hub, 0.33f),
                Vector3.Lerp(colony, hub, 0.66f),
                hub
            };

            Transform[] tags = new Transform[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                GameObject tag = new GameObject($"PathTag_{i:00}");
                tag.transform.SetParent(folderGo.transform, false);
                Vector3 p = points[i];
                Terrain terrain = Object.FindAnyObjectByType<Terrain>();
                if (terrain != null)
                    p.y = terrain.SampleHeight(p) + terrain.transform.position.y + 0.5f;
                tag.transform.position = p;
                tags[i] = tag.transform;
            }

            return tags;
        }

        private static void EnsureMapMarker(Transform anchor, string label, Color color)
        {
            if (anchor == null)
                return;

            MapMarker marker = anchor.GetComponent<MapMarker>();
            if (marker == null)
                marker = Undo.AddComponent<MapMarker>(anchor.gameObject);

            SerializedObject so = new SerializedObject(marker);
            so.FindProperty("label").stringValue = label;
            so.FindProperty("color").colorValue = color;
            so.FindProperty("showOnMinimap").boolValue = true;
            so.FindProperty("showOnFullMap").boolValue = true;
            so.FindProperty("requiresScanDiscovery").boolValue = false;
            so.FindProperty("requiresFogReveal").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(marker);
        }

        private static void EnsureAssetFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;

            string[] parts = folder.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }
}
#endif
