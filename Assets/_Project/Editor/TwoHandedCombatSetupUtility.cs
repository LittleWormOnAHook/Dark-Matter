using System;
using Project.Data;
using Project.EditorTools;
using Project.Interaction;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Adds two-handed attack states to the player animator and creates two-handed weapon assets.
/// </summary>
public static class TwoHandedCombatSetupUtility
{
    private const string AnimatorControllerPath =
        "Assets/_Project/Animations/ProjectUnityCharacterController.controller";

    private const string TwoHandAnimFolder =
        "Assets/Animations/Human Animations Melee/Human Animations/Animations/Male/Combat/2H";

    private const string FallbackWeaponPrefabPath =
        "Assets/Scifi Melee Weapons/Prefabs/weap3_sword.prefab";

    private const string HeldPrefabPath = "Assets/_Project/Prefabs/Items/Held/two_handed.prefab";
    private const string ItemDataPath = "Assets/_Project/Data/Items/weap_two_handed.asset";
    private const string SwordTemplatePath = "Assets/_Project/Data/Items/weap2_sword.asset";

    private const string GroundedStateName = "Grounded";
    private const float AttackExitTime = 0.88f;
    private const float AttackTransitionDuration = 0.12f;

    [MenuItem(SurvivalPioneerEditorMenus.CombatAnimations + "Two-Handed Combat Animations", false, 0)]
    private static void SetupTwoHandedCombatAnimations()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
        if (controller == null)
        {
            EditorUtility.DisplayDialog(
                "Two-Handed Combat",
                $"Could not load animator controller at:\n{AnimatorControllerPath}",
                "OK");
            return;
        }

        AnimatorStateMachine baseLayer = controller.layers[0].stateMachine;
        AnimatorState groundedState = FindState(baseLayer, GroundedStateName);
        if (groundedState == null)
        {
            EditorUtility.DisplayDialog(
                "Two-Handed Combat",
                $"Could not find '{GroundedStateName}' state on the base layer.",
                "OK");
            return;
        }

        int addedOrUpdated = 0;
        addedOrUpdated += UpsertAttackState(
            baseLayer,
            groundedState,
            "TwoHandAttack1",
            $"{TwoHandAnimFolder}/HumanM@Attack2H01.fbx",
            "HumanM@Attack2H01",
            0.85f,
            new Vector3(980f, -540f, 0f));
        addedOrUpdated += UpsertAttackState(
            baseLayer,
            groundedState,
            "TwoHandAttack2",
            $"{TwoHandAnimFolder}/HumanM@Attack2H02.fbx",
            "HumanM@Attack2H02",
            0.85f,
            new Vector3(980f, -600f, 0f));
        addedOrUpdated += UpsertAttackState(
            baseLayer,
            groundedState,
            "TwoHandAttack3",
            $"{TwoHandAnimFolder}/HumanM@Attack2H03.fbx",
            "HumanM@Attack2H03",
            0.85f,
            new Vector3(980f, -660f, 0f));
        addedOrUpdated += UpsertAttackState(
            baseLayer,
            groundedState,
            "TwoHandAttack4",
            $"{TwoHandAnimFolder}/HumanM@Attack2H04.fbx",
            "HumanM@Attack2H04",
            0.85f,
            new Vector3(980f, -720f, 0f));
        addedOrUpdated += UpsertAttackState(
            baseLayer,
            groundedState,
            "TwoHandPowerHit",
            $"{TwoHandAnimFolder}/HumanM@Attack2H04.fbx",
            "HumanM@Attack2H04",
            0.9f,
            new Vector3(980f, -780f, 0f));

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Two-Handed Combat",
            $"Two-handed attack states are ready on the base layer ({addedOrUpdated} states added or updated).\n\n" +
            "States: TwoHandAttack1-4, TwoHandPowerHit\n" +
            "Tagged Attack, exit to Grounded at 0.88.",
            "OK");
    }

    [MenuItem(SurvivalPioneerEditorMenus.Combat + "Create Two-Handed Weapon From Scene")]
    private static void CreateTwoHandedWeaponFromScene()
    {
        GameObject source = ResolveWeaponSourceObject();
        if (source == null)
        {
            EditorUtility.DisplayDialog(
                "Two-Handed Weapon",
                "Select a weapon object, or place one named \"2 handed\" in the open scene.",
                "OK");
            return;
        }

        EnsureFolder("Assets/_Project/Prefabs/Items");
        EnsureFolder("Assets/_Project/Prefabs/Items/Held");
        EnsureFolder("Assets/_Project/Data/Items");

        GameObject heldRoot = BuildHeldPrefabRoot(source);
        GameObject heldPrefab = SaveHeldPrefab(heldRoot, HeldPrefabPath);
        UnityEngine.Object.DestroyImmediate(heldRoot);

        GameObject worldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FallbackWeaponPrefabPath);
        if (worldPrefab == null)
        {
            EditorUtility.DisplayDialog(
                "Two-Handed Weapon",
                $"Fallback world prefab not found at:\n{FallbackWeaponPrefabPath}",
                "OK");
            return;
        }

        ItemData item = LoadOrCreateItemData();
        ConfigureTwoHandedItemData(item, worldPrefab, heldPrefab);

        if (TryBakeGripFromScene(source, item))
            Debug.Log("TwoHandedCombatSetupUtility: baked held grip from scene object relative to RightHand.");

        EditorUtility.SetDirty(item);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = item;
        EditorGUIUtility.PingObject(item);

        EditorUtility.DisplayDialog(
            "Two-Handed Weapon",
            $"Created/updated two-handed weapon assets.\n\n" +
            $"ItemData: {ItemDataPath}\n" +
            $"Held prefab: {HeldPrefabPath}\n" +
            $"World prefab: {FallbackWeaponPrefabPath}",
            "OK");
    }

    private static int UpsertAttackState(
        AnimatorStateMachine stateMachine,
        AnimatorState groundedState,
        string stateName,
        string fbxPath,
        string clipName,
        float speed,
        Vector3 position)
    {
        AnimationClip clip = LoadAnimationClip(fbxPath, clipName);
        if (clip == null)
        {
            Debug.LogWarning($"TwoHandedCombatSetupUtility: missing clip '{clipName}' at {fbxPath}");
            return 0;
        }

        AnimatorState state = FindState(stateMachine, stateName);
        if (state == null)
        {
            state = stateMachine.AddState(stateName, position);
        }
        else
        {
            ChildAnimatorState[] children = stateMachine.states;
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].state != state)
                    continue;

                stateMachine.states[i] = new ChildAnimatorState
                {
                    state = state,
                    position = position
                };
                break;
            }
        }

        state.motion = clip;
        state.speed = speed;
        state.tag = "Attack";
        EnsureGroundedTransition(state, groundedState);
        return 1;
    }

    private static void EnsureGroundedTransition(AnimatorState attackState, AnimatorState groundedState)
    {
        foreach (AnimatorStateTransition transition in attackState.transitions)
        {
            if (transition.destinationState == groundedState &&
                transition.hasExitTime &&
                Mathf.Approximately(transition.exitTime, AttackExitTime))
                return;
        }

        AnimatorStateTransition newTransition = attackState.AddTransition(groundedState);
        newTransition.hasExitTime = true;
        newTransition.exitTime = AttackExitTime;
        newTransition.duration = AttackTransitionDuration;
        newTransition.hasFixedDuration = true;
        newTransition.canTransitionToSelf = false;
    }

    private static AnimationClip LoadAnimationClip(string assetPath, string clipName)
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        foreach (UnityEngine.Object asset in assets)
        {
            if (asset is AnimationClip clip &&
                clip.name == clipName &&
                !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                return clip;
        }

        return null;
    }

    private static AnimatorState FindState(AnimatorStateMachine stateMachine, string stateName)
    {
        foreach (ChildAnimatorState child in stateMachine.states)
        {
            if (child.state != null && child.state.name == stateName)
                return child.state;
        }

        return null;
    }

    private static GameObject ResolveWeaponSourceObject()
    {
        if (Selection.activeGameObject != null)
            return Selection.activeGameObject;

        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid())
            return null;

        foreach (GameObject root in activeScene.GetRootGameObjects())
        {
            Transform found = FindDeepChildByName(root.transform, "2 handed");
            if (found != null)
                return found.gameObject;
        }

        return null;
    }

    private static Transform FindDeepChildByName(Transform parent, string targetName)
    {
        if (parent.name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindDeepChildByName(parent.GetChild(i), targetName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static GameObject BuildHeldPrefabRoot(GameObject source)
    {
        GameObject instance = PrefabUtility.IsPartOfPrefabAsset(source)
            ? PrefabUtility.InstantiatePrefab(source) as GameObject
            : UnityEngine.Object.Instantiate(source);

        if (instance == null)
            throw new InvalidOperationException("TwoHandedCombatSetupUtility: could not instantiate weapon source.");

        instance.name = "two_handed";
        instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        instance.transform.localScale = Vector3.one;
        StripHeldComponents(instance);
        return instance;
    }

    private static GameObject SaveHeldPrefab(GameObject root, string prefabPath)
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existing != null)
            AssetDatabase.DeleteAsset(prefabPath);

        return PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
    }

    private static ItemData LoadOrCreateItemData()
    {
        ItemData existing = AssetDatabase.LoadAssetAtPath<ItemData>(ItemDataPath);
        if (existing != null)
            return existing;

        ItemData item = ScriptableObject.CreateInstance<ItemData>();
        AssetDatabase.CreateAsset(item, ItemDataPath);
        return item;
    }

    private static void ConfigureTwoHandedItemData(ItemData item, GameObject worldPrefab, GameObject heldPrefab)
    {
        ItemData template = AssetDatabase.LoadAssetAtPath<ItemData>(SwordTemplatePath);

        item.itemName = "Two-Handed Sword";
        item.maxStack = 1;
        item.itemType = ItemType.MeleeWeapon;
        item.weaponGrip = WeaponGrip.TwoHanded;
        item.worldPrefab = worldPrefab;
        item.heldPrefab = heldPrefab;
        item.icon = template != null ? template.icon : item.icon;

        item.meleeDamage = 28f;
        item.meleeDamageRandomRange = 12f;
        item.criticalDamageMultiplier = 3.5f;
        item.meleeRange = 3.4f;
        item.meleeCooldown = 0.85f;
        item.gatherPower = 2;
        item.swingEulerAngles = new Vector3(-90f, 0f, 0f);
        item.tooltipDescription = "A heavy two-handed blade. Cannot block while equipped.";

        if (template == null)
            return;

        item.equipSocketName = template.equipSocketName;
        item.sheatheSocketName = template.sheatheSocketName;
        item.sheathedLocalPosition = template.sheathedLocalPosition;
        item.sheathedLocalEuler = template.sheathedLocalEuler;
        item.useSheathedLocalRotation = template.useSheathedLocalRotation;
        item.sheathedLocalRotation = template.sheathedLocalRotation;
        item.sheathedLocalScale = template.sheathedLocalScale;
        item.heldLocalPosition = template.heldLocalPosition;
        item.heldLocalEuler = template.heldLocalEuler;
        item.useHeldLocalRotation = template.useHeldLocalRotation;
        item.heldLocalRotation = template.heldLocalRotation;
        item.heldLocalScale = template.heldLocalScale;
    }

    private static bool TryBakeGripFromScene(GameObject weaponObject, ItemData item)
    {
        Transform hand = FindPlayerHandSocket("RightHand");
        if (hand == null)
            return false;

        Vector3 localPosition = hand.InverseTransformPoint(weaponObject.transform.position);
        Quaternion localRotation = Quaternion.Inverse(hand.rotation) * weaponObject.transform.rotation;
        Vector3 localScale = weaponObject.transform.lossyScale;
        if (hand.lossyScale.x != 0f && hand.lossyScale.y != 0f && hand.lossyScale.z != 0f)
        {
            localScale = new Vector3(
                weaponObject.transform.lossyScale.x / hand.lossyScale.x,
                weaponObject.transform.lossyScale.y / hand.lossyScale.y,
                weaponObject.transform.lossyScale.z / hand.lossyScale.z);
        }

        item.equipSocketName = "RightHand";
        item.heldLocalPosition = localPosition;
        item.heldLocalRotation = localRotation;
        item.useHeldLocalRotation = true;
        item.heldLocalEuler = localRotation.eulerAngles;
        item.heldLocalScale = localScale == Vector3.zero ? Vector3.one : localScale;
        return true;
    }

    private static Transform FindPlayerHandSocket(string socketName)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return null;

        return FindDeepChildExact(player.transform, socketName);
    }

    private static Transform FindDeepChildExact(Transform parent, string childName)
    {
        if (parent.name == childName)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindDeepChildExact(parent.GetChild(i), childName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static void StripHeldComponents(GameObject root)
    {
        foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.DestroyImmediate(collider);

        foreach (Rigidbody body in root.GetComponentsInChildren<Rigidbody>(true))
            UnityEngine.Object.DestroyImmediate(body);

        foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour is ItemPickup || behaviour is ResourceNode)
                UnityEngine.Object.DestroyImmediate(behaviour);
        }
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
