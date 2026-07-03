using System.Collections.Generic;
using System.Text;
using Project.Data;
using Project.EditorTools;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Deterministic, edit-mode grip baking for ranged weapons.
/// - Ensures GripPoint + Muzzle anchors on held prefabs (seeded from current baked grips / bounds).
/// - heldLocal* = inverse of the GripPoint anchor transform (no Play mode needed).
/// - sheathedLocal* = canonical across-back placement scaled by weapon length.
/// - Validates barrel alignment by sampling the Protofactor IdleHold clip on the player rig.
/// </summary>
public static class RangedGripAutoBaker
{
    private const string GripPointName = "GripPoint";
    private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Players/Player.prefab";
    private const string ItemsFolder = "Assets/_Project/Data/Items";

    private const string RifleHoldClipPath =
        "Assets/Animations/PROTOFACTOR/Ultimate Animation Collection/Animations/Assault Rifle Animset/FBX motions/Humanoid@IdleHoldAssaultRifle.FBX";
    private const string PistolHoldClipPath =
        "Assets/Animations/PROTOFACTOR/Ultimate Animation Collection/Animations/2Handed Gun Animset/FBX Motions/Humanoid@IdleHold2HandedGun.FBX";

    private const float BarrelAlignmentToleranceDegrees = 8f;

    [MenuItem(SurvivalPioneerEditorMenus.Equipment + "Auto-Bake All Ranged Grips", false, 20)]
    public static void AutoBakeAllRangedGripsMenu()
    {
        List<ItemData> weapons = FindRangedWeapons();
        if (weapons.Count == 0)
        {
            EditorUtility.DisplayDialog("Auto-Bake Ranged Grips", "No ranged ItemData assets with held prefabs found.", "OK");
            return;
        }

        StringBuilder report = new StringBuilder();
        int baked = 0;
        foreach (ItemData weapon in weapons)
        {
            if (AutoBakeWeapon(weapon, report))
                baked++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"RangedGripAutoBaker: baked {baked}/{weapons.Count} weapon(s)\n{report}");
        EditorUtility.DisplayDialog(
            "Auto-Bake Ranged Grips",
            $"Baked {baked} of {weapons.Count} ranged weapon(s).\n\n{report}",
            "OK");
    }

    [MenuItem(SurvivalPioneerEditorMenus.Equipment + "Auto-Bake Selected Ranged Weapon", false, 21)]
    public static void AutoBakeSelectedRangedWeaponMenu()
    {
        if (Selection.activeObject is not ItemData item || !item.IsRangedWeapon)
        {
            EditorUtility.DisplayDialog(
                "Auto-Bake Ranged Grip",
                "Select a ranged weapon ItemData asset in the Project window first.",
                "OK");
            return;
        }

        StringBuilder report = new StringBuilder();
        bool baked = AutoBakeWeapon(item, report);
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog(
            "Auto-Bake Ranged Grip",
            baked ? $"Baked {item.name}.\n\n{report}" : $"Could not bake {item.name}.\n\n{report}",
            "OK");
    }

    [MenuItem(SurvivalPioneerEditorMenus.Equipment + "Validate Ranged Muzzle Alignment", false, 22)]
    public static void ValidateMuzzleAlignmentMenu()
    {
        List<ItemData> weapons = FindRangedWeapons();
        StringBuilder report = new StringBuilder();
        foreach (ItemData weapon in weapons)
            ValidateBarrelAlignment(weapon, report, applyCorrection: false);

        string message = report.Length > 0 ? report.ToString() : "No ranged weapons found.";
        Debug.Log($"RangedGripAutoBaker validation:\n{message}");
        EditorUtility.DisplayDialog("Ranged Muzzle Alignment", message, "OK");
    }

    public static bool AutoBakeWeapon(ItemData weapon, StringBuilder report)
    {
        if (weapon == null || !weapon.IsRangedWeapon || weapon.heldPrefab == null)
        {
            report?.AppendLine($"- {weapon?.name ?? "<null>"}: skipped (not ranged or missing held prefab)");
            return false;
        }

        string heldPath = AssetDatabase.GetAssetPath(weapon.heldPrefab);
        if (string.IsNullOrEmpty(heldPath))
        {
            report?.AppendLine($"- {weapon.name}: skipped (held prefab is not an asset)");
            return false;
        }

        // 1. Ensure anchors on the held prefab.
        if (!EnsureAnchors(weapon, heldPath, out Vector3 gripLocalPos, out Quaternion gripLocalRot, report))
            return false;

        // 2. heldLocal* from GripPoint inverse.
        Vector3 heldScale = weapon.heldLocalScale == Vector3.zero ? Vector3.one : weapon.heldLocalScale;
        Quaternion heldRotation = Quaternion.Inverse(gripLocalRot);
        Vector3 heldPosition = -(heldRotation * Vector3.Scale(gripLocalPos, heldScale));

        Undo.RecordObject(weapon, "Auto-Bake Ranged Grip");
        weapon.heldLocalPosition = heldPosition;
        weapon.heldLocalRotation = heldRotation;
        weapon.useHeldLocalRotation = true;
        weapon.heldLocalEuler = heldRotation.eulerAngles;
        weapon.heldLocalScale = heldScale;

        // 3. Holster placement: canonical across-back pose, depth scaled by weapon length.
        ApplyCanonicalHolster(weapon, heldPath);

        // 4. Validate (and correct) barrel alignment against the IdleHold pose.
        ValidateBarrelAlignment(weapon, report, applyCorrection: true);

        EditorUtility.SetDirty(weapon);
        report?.AppendLine(
            $"- {weapon.name}: held pos {Fmt(weapon.heldLocalPosition)} rot {Fmt(weapon.heldLocalRotation.eulerAngles)}, " +
            $"holster pos {Fmt(weapon.sheathedLocalPosition)}");
        return true;
    }

    // ------------------------------------------------------------------
    // Anchors
    // ------------------------------------------------------------------

    private static bool EnsureAnchors(
        ItemData weapon,
        string heldPath,
        out Vector3 gripLocalPos,
        out Quaternion gripLocalRot,
        StringBuilder report)
    {
        gripLocalPos = Vector3.zero;
        gripLocalRot = Quaternion.identity;

        GameObject contents = PrefabUtility.LoadPrefabContents(heldPath);
        try
        {
            bool dirty = false;

            Transform grip = FindDeepChild(contents.transform, GripPointName);
            if (grip == null)
            {
                grip = new GameObject(GripPointName).transform;
                grip.SetParent(contents.transform, false);
                SeedGripFromCurrentBake(weapon, grip);
                dirty = true;
            }

            gripLocalPos = ResolveLocalToRoot(contents.transform, grip, out gripLocalRot);

            string muzzleName = string.IsNullOrWhiteSpace(weapon.muzzleSocketName) ? "Muzzle" : weapon.muzzleSocketName;
            Transform muzzle = FindDeepChild(contents.transform, muzzleName);
            if (muzzle == null)
            {
                muzzle = new GameObject(muzzleName).transform;
                muzzle.SetParent(contents.transform, false);
                dirty = true;
            }

            // Weapon meshes are not guaranteed to run along local +Z (the rifle runs
            // along +X), so derive the barrel axis from the dominant bounds extent
            // relative to the grip and point the muzzle's +Z down the barrel.
            Vector3 barrelAxis = ComputeBarrelAxis(contents, gripLocalPos, out Vector3 tipLocal);
            Quaternion muzzleRotation = Quaternion.LookRotation(barrelAxis);
            if ((muzzle.localPosition - tipLocal).sqrMagnitude > 0.0001f
                || Quaternion.Angle(muzzle.localRotation, muzzleRotation) > 0.5f)
            {
                muzzle.localPosition = tipLocal;
                muzzle.localRotation = muzzleRotation;
                dirty = true;
            }

            if (dirty)
                PrefabUtility.SaveAsPrefabAsset(contents, heldPath);

            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    /// <summary>
    /// Seeds the GripPoint so that inverting it reproduces the current baked grip exactly.
    /// Editing the anchor afterwards moves the grip predictably.
    /// </summary>
    private static void SeedGripFromCurrentBake(ItemData weapon, Transform grip)
    {
        Quaternion heldRot = weapon.useHeldLocalRotation
            ? weapon.heldLocalRotation
            : Quaternion.Euler(weapon.heldLocalEuler);
        Vector3 heldPos = weapon.heldLocalPosition;
        Vector3 scale = weapon.heldLocalScale == Vector3.zero ? Vector3.one : weapon.heldLocalScale;

        // heldRot = inverse(gripRot) ; heldPos = -(heldRot * (gripPos ⊙ scale))
        Quaternion gripRot = Quaternion.Inverse(heldRot);
        Vector3 scaledGripPos = gripRot * (-heldPos);
        grip.localRotation = gripRot;
        grip.localPosition = new Vector3(
            SafeDivide(scaledGripPos.x, scale.x),
            SafeDivide(scaledGripPos.y, scale.y),
            SafeDivide(scaledGripPos.z, scale.z));
    }

    private static Vector3 ResolveLocalToRoot(Transform root, Transform anchor, out Quaternion rotation)
    {
        if (anchor.parent == root)
        {
            rotation = anchor.localRotation;
            return anchor.localPosition;
        }

        rotation = Quaternion.Inverse(root.rotation) * anchor.rotation;
        return root.InverseTransformPoint(anchor.position);
    }

    // ------------------------------------------------------------------
    // Holster
    // ------------------------------------------------------------------

    private static void ApplyCanonicalHolster(ItemData weapon, string heldPath)
    {
        bool isRifle = weapon.ResolveGkcWeaponKind() == GkcWeaponKind.Rifle
            || weapon.weaponGrip == WeaponGrip.TwoHanded;

        float weaponLength = MeasureWeaponLength(heldPath);
        // Longer weapons sit deeper behind the spine so they clear the shoulders.
        float depth = Mathf.Clamp(0.14f + weaponLength * 0.12f, 0.16f, 0.3f);

        weapon.sheathedLocalPosition = isRifle
            ? new Vector3(0.04f, 0.18f, -depth)
            : new Vector3(0.08f, 0.12f, -depth);
        weapon.sheathedLocalEuler = isRifle
            ? new Vector3(75f, 90f, 90f)
            : new Vector3(70f, 90f, 90f);
        weapon.useSheathedLocalRotation = false;
        weapon.sheathedLocalRotation = Quaternion.identity;
        if (weapon.sheathedLocalScale == Vector3.zero)
            weapon.sheathedLocalScale = Vector3.one;
        weapon.sheatheSocketName = "Spine";
    }

    private static float MeasureWeaponLength(string heldPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(heldPath);
        if (prefab == null)
            return 0.6f;

        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return 0.6f;

        Bounds combined = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            combined.Encapsulate(renderers[i].bounds);

        return Mathf.Max(combined.size.x, combined.size.y, combined.size.z);
    }

    // ------------------------------------------------------------------
    // Clip-sampled barrel validation
    // ------------------------------------------------------------------

    private static void ValidateBarrelAlignment(ItemData weapon, StringBuilder report, bool applyCorrection)
    {
        if (weapon == null || !weapon.IsRangedWeapon)
            return;

        bool isRifle = weapon.ResolveGkcWeaponKind() == GkcWeaponKind.Rifle
            || weapon.weaponGrip == WeaponGrip.TwoHanded;
        string clipPath = isRifle ? RifleHoldClipPath : PistolHoldClipPath;
        AnimationClip holdClip = LoadClip(clipPath);

        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (playerPrefab == null || holdClip == null)
        {
            report?.AppendLine($"- {weapon.name}: alignment check skipped (missing player prefab or hold clip)");
            return;
        }

        GameObject temp = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        temp.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            Animator animator = temp.GetComponentInChildren<Animator>(true);
            GameObject animRoot = animator != null ? animator.gameObject : temp;

            AnimationMode.StartAnimationMode();
            try
            {
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(animRoot, holdClip, 0f);
                AnimationMode.EndSampling();

                Transform hand = FindDeepChild(temp.transform, string.IsNullOrWhiteSpace(weapon.equipSocketName)
                    ? "RightHand"
                    : weapon.equipSocketName);
                if (hand == null)
                {
                    report?.AppendLine($"- {weapon.name}: alignment check skipped (RightHand socket not found)");
                    return;
                }

                Quaternion heldRot = weapon.useHeldLocalRotation
                    ? weapon.heldLocalRotation
                    : Quaternion.Euler(weapon.heldLocalEuler);
                Vector3 barrelAxisLocal = ResolveBarrelAxisLocal(weapon);
                Vector3 barrelWorld = hand.rotation * (heldRot * barrelAxisLocal);
                Vector3 desired = temp.transform.forward;

                float deviation = Vector3.Angle(barrelWorld, desired);
                if (deviation <= BarrelAlignmentToleranceDegrees)
                {
                    report?.AppendLine($"- {weapon.name}: barrel deviation {deviation:F1}° (ok)");
                    return;
                }

                if (!applyCorrection)
                {
                    report?.AppendLine($"- {weapon.name}: barrel deviation {deviation:F1}° (exceeds {BarrelAlignmentToleranceDegrees}°)");
                    return;
                }

                Quaternion worldCorrection = Quaternion.FromToRotation(barrelWorld, desired);
                Quaternion corrected = Quaternion.Inverse(hand.rotation) * worldCorrection * hand.rotation * heldRot;

                weapon.heldLocalRotation = corrected;
                weapon.useHeldLocalRotation = true;
                weapon.heldLocalEuler = corrected.eulerAngles;
                report?.AppendLine($"- {weapon.name}: barrel deviation {deviation:F1}° → corrected to face forward");
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }
        }
        finally
        {
            Object.DestroyImmediate(temp);
        }
    }

    /// <summary>
    /// Barrel direction in weapon-root space, taken from the authored Muzzle
    /// anchor's +Z. Falls back to root +Z when no anchor exists.
    /// </summary>
    private static Vector3 ResolveBarrelAxisLocal(ItemData weapon)
    {
        if (weapon.heldPrefab == null)
            return Vector3.forward;

        string muzzleName = string.IsNullOrWhiteSpace(weapon.muzzleSocketName) ? "Muzzle" : weapon.muzzleSocketName;
        Transform muzzle = FindDeepChild(weapon.heldPrefab.transform, muzzleName);
        if (muzzle == null)
            return Vector3.forward;

        Transform root = weapon.heldPrefab.transform;
        Vector3 axis = Quaternion.Inverse(root.rotation) * muzzle.forward;
        return axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.forward;
    }

    private static AnimationClip LoadClip(string fbxPath)
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                return clip;
        }

        return null;
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static List<ItemData> FindRangedWeapons()
    {
        List<ItemData> results = new List<ItemData>();
        foreach (string guid in AssetDatabase.FindAssets("t:ItemData", new[] { ItemsFolder }))
        {
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid));
            if (item != null && item.IsRangedWeapon && item.heldPrefab != null)
                results.Add(item);
        }

        return results;
    }

    /// <summary>
    /// Finds the dominant axis of the combined renderer bounds in root-local space,
    /// signs it away from the grip, and returns the far-end tip position.
    /// </summary>
    private static Vector3 ComputeBarrelAxis(GameObject instance, Vector3 gripLocalPos, out Vector3 tipLocal)
    {
        tipLocal = ComputeBarrelTipLocalPosition(instance);

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return Vector3.forward;

        Transform root = instance.transform;
        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;
        foreach (Renderer renderer in renderers)
        {
            Bounds bounds = renderer.bounds;
            for (int xi = -1; xi <= 1; xi += 2)
            for (int yi = -1; yi <= 1; yi += 2)
            for (int zi = -1; zi <= 1; zi += 2)
            {
                Vector3 corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3(xi, yi, zi));
                Vector3 local = root.InverseTransformPoint(corner);
                min = Vector3.Min(min, local);
                max = Vector3.Max(max, local);
            }
        }

        Vector3 size = max - min;
        int axis = 0;
        if (size.y > size[axis]) axis = 1;
        if (size.z > size[axis]) axis = 2;

        Vector3 direction = Vector3.zero;
        // The barrel points away from the grip along the dominant axis.
        float towardMax = max[axis] - gripLocalPos[axis];
        float towardMin = gripLocalPos[axis] - min[axis];
        direction[axis] = towardMax >= towardMin ? 1f : -1f;

        Vector3 center = (min + max) * 0.5f;
        tipLocal = center;
        tipLocal[axis] = direction[axis] > 0f ? max[axis] + 0.02f : min[axis] - 0.02f;
        return direction;
    }

    private static Vector3 ComputeBarrelTipLocalPosition(GameObject instance)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Vector3(0f, 0.05f, 0.35f);

        Transform root = instance.transform;
        float maxForward = float.NegativeInfinity;
        const float tolerance = 0.03f;
        float sumX = 0f, sumY = 0f;
        int count = 0;

        foreach (Renderer renderer in renderers)
        {
            Bounds bounds = renderer.bounds;
            for (int xi = -1; xi <= 1; xi += 2)
            for (int yi = -1; yi <= 1; yi += 2)
            for (int zi = -1; zi <= 1; zi += 2)
            {
                Vector3 corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3(xi, yi, zi));
                maxForward = Mathf.Max(maxForward, root.InverseTransformPoint(corner).z);
            }
        }

        if (float.IsNegativeInfinity(maxForward))
            return new Vector3(0f, 0.05f, 0.35f);

        foreach (Renderer renderer in renderers)
        {
            Bounds bounds = renderer.bounds;
            for (int xi = -1; xi <= 1; xi += 2)
            for (int yi = -1; yi <= 1; yi += 2)
            for (int zi = -1; zi <= 1; zi += 2)
            {
                Vector3 corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3(xi, yi, zi));
                Vector3 local = root.InverseTransformPoint(corner);
                if (local.z < maxForward - tolerance)
                    continue;

                sumX += local.x;
                sumY += local.y;
                count++;
            }
        }

        return count > 0
            ? new Vector3(sumX / count, sumY / count, maxForward + 0.02f)
            : new Vector3(0f, 0.05f, maxForward + 0.02f);
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent.name == childName)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindDeepChild(parent.GetChild(i), childName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static float SafeDivide(float value, float divisor)
    {
        return Mathf.Abs(divisor) < 0.0001f ? value : value / divisor;
    }

    private static string Fmt(Vector3 v) => $"({v.x:F3}, {v.y:F3}, {v.z:F3})";
}
