using Project.Combat;
using Project.EditorTools;
using Project.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Creates combat prefabs, copies them to Resources, and upgrades scene dummy targets.
/// </summary>
public static class CombatSetupUtility
{
    private const string CombatPrefabFolder = "Assets/_Project/Prefabs/Combat";
    private const string UiPrefabFolder = "Assets/_Project/Prefabs/UI";
    private const string ResourcesCombatFolder = "Assets/_Project/Resources/Combat";
    private const string DummyTargetSourcePath = "Assets/Blink/Art/NPCs/Stylized/DummyTarget/DummyTarget.prefab";
    private const string BloodSplatterSourcePath =
        "Assets/Synty/PolygonGeneric/Prefabs/FX/FX_Blood_Splatter_01.prefab";
    private const string TrainingDummyPrefabPath = CombatPrefabFolder + "/TrainingDummy.prefab";
    private const string DamageNumberPrefabPath = UiPrefabFolder + "/FloatingDamageNumber.prefab";
    private const string HealthBarPrefabPath = UiPrefabFolder + "/FloatingTargetHealthBar.prefab";

    [MenuItem(SurvivalPioneerEditorMenus.Combat + "Combat Test Dummy", false, 20)]
    private static void SetupCombatTestDummy()
    {
        EnsureFolder("Assets/_Project/Prefabs");
        EnsureFolder(CombatPrefabFolder);
        EnsureFolder(UiPrefabFolder);
        EnsureFolder("Assets/_Project/Resources");
        EnsureFolder(ResourcesCombatFolder);

        GameObject damagePrefab = CreateOrLoadFloatingDamagePrefab();
        GameObject healthBarPrefab = CreateOrLoadHealthBarPrefab();
        CopyCombatResources(damagePrefab, healthBarPrefab);
        GameObject dummyPrefab = CreateOrLoadTrainingDummyPrefab();
        WireUiManager(damagePrefab);

        GameObject sceneDummy = UpgradeSceneDummyTargets();
        if (sceneDummy == null)
            sceneDummy = PlaceTrainingDummyNearPlayer(dummyPrefab);

        Selection.activeObject = sceneDummy != null ? sceneDummy : dummyPrefab;
        EditorGUIUtility.PingObject(Selection.activeObject);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Combat Setup",
            "Combat test dummy setup complete.\n\n" +
            "- Uses Blink DummyTarget model\n" +
            "- Floating health bar above target\n" +
            "- Damage numbers spawn via Resources/Combat\n\n" +
            "Save the scene after reviewing placement.",
            "OK");
    }

    private static void CopyCombatResources(GameObject damagePrefab, GameObject healthBarPrefab)
    {
        CopyAssetToResources(damagePrefab, ResourcesCombatFolder + "/FloatingDamageNumber.prefab");
        CopyAssetToResources(healthBarPrefab, ResourcesCombatFolder + "/FloatingTargetHealthBar.prefab");

        GameObject dummyTargetSource = AssetDatabase.LoadAssetAtPath<GameObject>(DummyTargetSourcePath);
        if (dummyTargetSource != null)
            CopyAssetToResources(dummyTargetSource, ResourcesCombatFolder + "/DummyTarget.prefab");

        GameObject bloodSplatterSource = AssetDatabase.LoadAssetAtPath<GameObject>(BloodSplatterSourcePath);
        if (bloodSplatterSource != null)
            CopyAssetToResources(bloodSplatterSource, ResourcesCombatFolder + "/FX_Blood_Splatter.prefab");
    }

    private static void CopyAssetToResources(Object source, string destinationPath)
    {
        if (source == null)
            return;

        if (AssetDatabase.LoadAssetAtPath<Object>(destinationPath) != null)
            AssetDatabase.DeleteAsset(destinationPath);

        if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source), destinationPath))
            Debug.LogWarning($"CombatSetupUtility: failed to copy {source.name} to {destinationPath}");
    }

    private static GameObject CreateOrLoadTrainingDummyPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(TrainingDummyPrefabPath) != null)
            AssetDatabase.DeleteAsset(TrainingDummyPrefabPath);

        GameObject root = new GameObject("TrainingDummy");
        try
        {
            GameObject dummyTargetSource = AssetDatabase.LoadAssetAtPath<GameObject>(DummyTargetSourcePath);
            if (dummyTargetSource == null)
            {
                Debug.LogError($"CombatSetupUtility: could not find DummyTarget prefab at {DummyTargetSourcePath}");
                return null;
            }

            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(dummyTargetSource, root.transform);
            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            BoxCollider collider = root.AddComponent<BoxCollider>();
            TrainingDummy dummy = root.AddComponent<TrainingDummy>();
            TrainingDummy.FitBoxColliderFromRenderers(collider);

            Bounds bounds = GetRendererBounds(root);
            CreateAnchor(root.transform, "DamageNumberAnchor", new Vector3(0f, bounds.max.y - root.transform.position.y + 0.05f, 0f));
            Transform healthAnchor = CreateAnchor(root.transform, "HealthBarAnchor", new Vector3(0f, bounds.max.y - root.transform.position.y + 0.05f, 0f));

            SerializedObject serializedDummy = new SerializedObject(dummy);
            serializedDummy.FindProperty("visualRoot").objectReferenceValue = visual.transform;
            serializedDummy.FindProperty("damageNumberAnchor").objectReferenceValue = root.transform.Find("DamageNumberAnchor");
            serializedDummy.FindProperty("healthBarAnchor").objectReferenceValue = healthAnchor;
            serializedDummy.FindProperty("dummyTargetPrefab").objectReferenceValue = dummyTargetSource;
            serializedDummy.ApplyModifiedPropertiesWithoutUndo();

            return PrefabUtility.SaveAsPrefabAsset(root, TrainingDummyPrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static GameObject UpgradeSceneDummyTargets()
    {
        TrainingDummy[] existingDummies = Object.FindObjectsByType<TrainingDummy>(FindObjectsInactive.Include);
        if (existingDummies.Length > 0)
        {
            foreach (TrainingDummy dummy in existingDummies)
                ConfigureDummyRoot(dummy.gameObject);

            return existingDummies[0].gameObject;
        }

        GameObject dummyTarget = null;
        Transform[] sceneTransforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
        foreach (Transform candidate in sceneTransforms)
        {
            if (candidate.name == "DummyTarget" && candidate.GetComponentInChildren<Renderer>() != null)
            {
                dummyTarget = candidate.gameObject;
                break;
            }
        }

        if (dummyTarget != null)
            return ConfigureDummyRoot(dummyTarget);

        return null;
    }

    private static GameObject ConfigureDummyRoot(GameObject candidate)
    {
        GameObject root = candidate;
        Transform visual = null;

        candidate.SetActive(true);

        if (candidate.name == "DummyTarget")
        {
            root = WrapWithTrainingRoot(candidate);
            visual = root.transform.Find("Visual");
        }
        else if (candidate.GetComponent<TrainingDummy>() != null)
        {
            Transform oldVisual = candidate.transform.Find("Visual");
            if (oldVisual != null && oldVisual.GetComponent<MeshFilter>() != null &&
                oldVisual.GetComponent<MeshFilter>().sharedMesh != null &&
                oldVisual.GetComponent<MeshFilter>().sharedMesh.name == "Capsule")
            {
                Object.DestroyImmediate(oldVisual.gameObject);
            }

            visual = candidate.transform.Find("Visual") ?? candidate.transform.Find("DummyTarget");
            if (visual == null)
            {
                GameObject dummyTargetSource = AssetDatabase.LoadAssetAtPath<GameObject>(DummyTargetSourcePath);
                if (dummyTargetSource != null)
                {
                    GameObject visualInstance = (GameObject)PrefabUtility.InstantiatePrefab(dummyTargetSource, candidate.transform);
                    visualInstance.name = "Visual";
                    visual = visualInstance.transform;
                }
            }
        }

        CapsuleCollider legacyCapsule = root.GetComponent<CapsuleCollider>();
        if (legacyCapsule != null)
            Object.DestroyImmediate(legacyCapsule);

        TrainingDummy dummy = root.GetComponent<TrainingDummy>();
        if (dummy == null)
            dummy = root.AddComponent<TrainingDummy>();

        BoxCollider collider = root.GetComponent<BoxCollider>();
        if (collider == null)
            collider = root.AddComponent<BoxCollider>();

        TrainingDummy.FitBoxColliderFromRenderers(collider);

        Bounds bounds = GetRendererBounds(root);
        Transform damageAnchor = EnsureAnchor(root.transform, "DamageNumberAnchor", new Vector3(0f, bounds.max.y - root.transform.position.y + 0.05f, 0f));
        Transform healthAnchor = EnsureAnchor(root.transform, "HealthBarAnchor", new Vector3(0f, bounds.max.y - root.transform.position.y + 0.05f, 0f));

        if (visual == null)
            visual = root.transform;

        SerializedObject serializedDummy = new SerializedObject(dummy);
        serializedDummy.FindProperty("visualRoot").objectReferenceValue = visual;
        serializedDummy.FindProperty("damageNumberAnchor").objectReferenceValue = damageAnchor;
        serializedDummy.FindProperty("healthBarAnchor").objectReferenceValue = healthAnchor;
        serializedDummy.FindProperty("dummyTargetPrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>(DummyTargetSourcePath);
        serializedDummy.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    private static GameObject WrapWithTrainingRoot(GameObject dummyTarget)
    {
        if (dummyTarget.transform.parent != null && dummyTarget.transform.parent.name == "TrainingDummy")
            return dummyTarget.transform.parent.gameObject;

        GameObject root = new GameObject("TrainingDummy");
        root.transform.SetPositionAndRotation(dummyTarget.transform.position, dummyTarget.transform.rotation);
        dummyTarget.transform.SetParent(root.transform, true);
        dummyTarget.name = "Visual";
        return root;
    }

    private static Transform EnsureAnchor(Transform parent, string anchorName, Vector3 localPosition)
    {
        Transform existing = parent.Find(anchorName);
        if (existing != null)
        {
            existing.localPosition = localPosition;
            return existing;
        }

        return CreateAnchor(parent, anchorName, localPosition);
    }

    private static Transform CreateAnchor(Transform parent, string anchorName, Vector3 localPosition)
    {
        GameObject anchor = new GameObject(anchorName);
        anchor.transform.SetParent(parent, false);
        anchor.transform.localPosition = localPosition;
        anchor.transform.localRotation = Quaternion.identity;
        anchor.transform.localScale = Vector3.one;
        return anchor.transform;
    }

    private static Bounds GetRendererBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(root.transform.position, Vector3.one * 2f);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }

    private static GameObject CreateOrLoadFloatingDamagePrefab()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(DamageNumberPrefabPath);
        if (existing != null)
            return existing;

        GameObject root = new GameObject("FloatingDamageNumber", typeof(RectTransform));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(120f, 48f);

        CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        FloatingDamageNumber behaviour = root.AddComponent<FloatingDamageNumber>();

        GameObject textObject = new GameObject("Text", typeof(RectTransform));
        textObject.transform.SetParent(root.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = "18";
        label.fontSize = 34f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.95f, 0.18f, 0.12f, 1f);
        label.raycastTarget = false;

        Outline outline = textObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.75f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        SerializedObject serializedPopup = new SerializedObject(behaviour);
        serializedPopup.FindProperty("label").objectReferenceValue = label;
        serializedPopup.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, DamageNumberPrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateOrLoadHealthBarPrefab()
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(HealthBarPrefabPath);
    }

    private static void WireUiManager(GameObject damagePrefab)
    {
        UIManager uiManager = Object.FindAnyObjectByType<UIManager>();
        if (uiManager == null)
        {
            Debug.LogWarning("CombatSetupUtility: no UIManager found in the open scene.");
            return;
        }

        SerializedObject serializedUi = new SerializedObject(uiManager);
        serializedUi.FindProperty("floatingDamagePrefab").objectReferenceValue = damagePrefab;
        Transform popupParent = uiManager.transform;
        serializedUi.FindProperty("combatPopupParent").objectReferenceValue = popupParent;
        serializedUi.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(uiManager);
    }

    private static GameObject PlaceTrainingDummyNearPlayer(GameObject dummyPrefab)
    {
        Vector3 spawnPosition = Vector3.forward * 3f;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            spawnPosition = player.transform.position + player.transform.forward * 3f;
            spawnPosition.y = player.transform.position.y;
        }

        GameObject existing = GameObject.Find("TrainingDummy");
        if (existing != null)
        {
            existing.transform.position = spawnPosition;
            ConfigureDummyRoot(existing);
            return existing;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(dummyPrefab);
        if (instance != null)
            instance.transform.position = spawnPosition;

        return instance;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string parent = System.IO.Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        string folderName = System.IO.Path.GetFileName(folderPath);
        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folderName))
            return;

        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, folderName);
    }
}
