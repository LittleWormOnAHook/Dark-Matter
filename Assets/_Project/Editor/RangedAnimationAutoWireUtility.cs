using System.Collections.Generic;
using System.IO;
using Project.EditorTools;
using Project.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Scans rifle/pistol animation folders and ensures ranged fire catalog entries exist.
/// </summary>
public static class RangedAnimationAutoWireUtility
{
    private const string ControllerPath = PlayerAnimatorControllerPaths.GkcControllerPath;
    private const string CatalogPath = "Assets/_Project/Data/Animation/GkcActionCatalog.asset";
    private const string IdleTreeStrafeIdBlendTreeName = "Idle Tree Strafe ID";

    private const string PistolAimIdleAssetPath =
        "Assets/Animations/PROTOFACTOR/Ultimate Animation Collection/Animations/2Handed Gun Animset/FBX Motions/Humanoid@IdleAim2HandedGun.FBX";

    private const string PistolAimIdleClipName = "IdleAim2HandedGun";

    private const string RifleAimIdleAssetPath =
        "Assets/Animations/PROTOFACTOR/Ultimate Animation Collection/Animations/Assault Rifle Animset/FBX motions/Humanoid@IdleAimAssaultRifle.FBX";

    private const string RifleAimIdleClipName = "IdleAimAssaultRifle";

    private static readonly string[] ScanFolders =
    {
        "Assets/Animations/PROTOFACTOR/Ultimate Animation Collection/Animations/2Handed Gun Animset/FBX Motions",
        "Assets/Animations/PROTOFACTOR/Ultimate Animation Collection/Animations/Assault Rifle Animset/FBX motions",
        "Assets/Animations/Mixamo Animations",
        "Assets/Animations/Mixamo Animations/Changed Folder",
        "Assets/Animations/Mixamo Animations/Strafe/Strafe Armed/Rifle",
        "Assets/Animations/Soldier Animations/Animations/AssaultRifle",
        "Assets/Animations/Soldier Animations/Animations/Rifle",
        "Assets/Animations/Soldier Animations/Animations/Handgun"
    };

    private static readonly (string fbxPath, string clipName)[] PreferredFireClips =
    {
        ("Assets/Animations/PROTOFACTOR/Ultimate Animation Collection/Animations/Assault Rifle Animset/FBX motions/Humanoid@ShootPrimaryAssaultRifle.FBX", "ShootPrimaryAssaultRifle"),
        ("Assets/Animations/PROTOFACTOR/Ultimate Animation Collection/Animations/2Handed Gun Animset/FBX Motions/Humanoid@ShootPrimary2HandedGun.FBX", "ShootPrimary2HandedGun"),
        ("Assets/Animations/PROTOFACTOR/Ultimate Animation Collection/Animations/Assault Rifle Animset/FBX motions/Humanoid@ReloadAssaultRifle.fbx", "ReloadAssaultRifle"),
        ("Assets/Animations/PROTOFACTOR/Ultimate Animation Collection/Animations/2Handed Gun Animset/FBX Motions/Humanoid@Reload2HandedGun.fbx", "Reload2HandedGun"),
    };

    [MenuItem(SurvivalPioneerEditorMenus.CombatAnimations + "Scan Ranged Animation Folders", false, 4)]
    public static void ScanRangedAnimationFoldersMenu()
    {
        List<string> discovered = DiscoverRangedClips();
        string summary = discovered.Count == 0
            ? "No rifle/pistol/reload/strafe clips found in configured folders."
            : $"Discovered {discovered.Count} clip(s):\n- " + string.Join("\n- ", discovered);

        Debug.Log($"Ranged animation scan:\n{summary}");
        EditorUtility.DisplayDialog("Ranged Animation Scan", summary, "OK");
    }

    [MenuItem(SurvivalPioneerEditorMenus.CombatAnimations + "Ensure Ranged Fire Catalog Entries", false, 5)]
    public static void EnsureRangedFireCatalogEntriesMenu()
    {
        GkcActionCatalog catalog = AssetDatabase.LoadAssetAtPath<GkcActionCatalog>(CatalogPath);
        if (catalog == null)
        {
            EditorUtility.DisplayDialog("Ranged Animations", $"Missing catalog at {CatalogPath}", "OK");
            return;
        }

        List<GkcActionCatalogEntry> entries = new List<GkcActionCatalogEntry>(catalog.Entries);
        int added = 0;
        added += UpsertEntry(entries, GkcActionCatalogClassifier.BuildManualSeedEntries()
            .Find(entry => entry.combatAction == GkcCombatAction.RifleFire));
        added += UpsertEntry(entries, GkcActionCatalogClassifier.BuildManualSeedEntries()
            .Find(entry => entry.combatAction == GkcCombatAction.PistolFire));

        if (added > 0)
        {
            catalog.SetEntries(entries);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        EditorUtility.DisplayDialog(
            "Ranged Animations",
            added > 0
                ? $"Added/updated {added} ranged fire catalog entries."
                : "Rifle/pistol fire catalog entries are already present.",
            "OK");
    }

    [MenuItem(SurvivalPioneerEditorMenus.CombatAnimations + "Log Preferred Ranged Clips", false, 6)]
    public static void LogPreferredRangedClipsMenu()
    {
        var lines = new List<string>();
        for (int i = 0; i < PreferredFireClips.Length; i++)
        {
            (string fbxPath, string clipName) = PreferredFireClips[i];
            AnimationClip clip = LoadClip(fbxPath, clipName);
            lines.Add(clip != null ? $"{clipName} @ {fbxPath}" : $"MISSING: {clipName} @ {fbxPath}");
        }

        Debug.Log("Preferred ranged clips:\n" + string.Join("\n", lines));
        EditorUtility.DisplayDialog("Preferred Ranged Clips", string.Join("\n", lines), "OK");
    }

    [MenuItem(SurvivalPioneerEditorMenus.CombatAnimations + "Wire Ranged Aim Idle Clips", false, 7)]
    public static void WireRangedAimIdleClipsMenu()
    {
        int wired = WireRangedAimIdleClips(showDialog: true);
        if (wired < 0)
            return;

        EditorUtility.DisplayDialog(
            "Ranged Aim Idle",
            wired > 0
                ? $"Wired {wired} aim-idle clip(s) into '{IdleTreeStrafeIdBlendTreeName}'."
                : "Ranged aim-idle clips are already wired in the Idle Tree Strafe ID blend tree.",
            "OK");
    }

    public static int WireRangedAimIdleClips(bool showDialog)
    {
        AnimationClip rifleClip = LoadClip(RifleAimIdleAssetPath, RifleAimIdleClipName);
        AnimationClip pistolClip = LoadClip(PistolAimIdleAssetPath, PistolAimIdleClipName);

        if (rifleClip == null || pistolClip == null)
        {
            string message = "Missing aim-idle clip(s):\n"
                + (rifleClip == null ? $"- {RifleAimIdleClipName} @ {RifleAimIdleAssetPath}\n" : string.Empty)
                + (pistolClip == null ? $"- {PistolAimIdleClipName} @ {PistolAimIdleAssetPath}" : string.Empty);

            if (showDialog)
                EditorUtility.DisplayDialog("Ranged Aim Idle", message.TrimEnd(), "OK");
            else
                Debug.LogError(message);

            return -1;
        }

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            if (showDialog)
                EditorUtility.DisplayDialog("Ranged Aim Idle", $"Missing controller at {ControllerPath}", "OK");
            return -1;
        }

        if (!TryFindBlendTree(controller, IdleTreeStrafeIdBlendTreeName, out BlendTree idleTree))
        {
            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Ranged Aim Idle",
                    $"Could not find blend tree '{IdleTreeStrafeIdBlendTreeName}' in {ControllerPath}.",
                    "OK");
            }

            return -1;
        }

        int wired = 0;
        wired += AssignStrafeIdClip(idleTree, GkcAnimatorConstants.StrafeIdRifle, rifleClip);
        wired += AssignStrafeIdClip(idleTree, GkcAnimatorConstants.StrafeIdPistol, pistolClip);
        wired += AssignFireWeaponsAimIdleClips(controller, rifleClip, pistolClip);

        if (wired > 0)
        {
            EditorUtility.SetDirty(idleTree);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
        }

        return wired;
    }

    private static List<string> DiscoverRangedClips()
    {
        var discovered = new List<string>();
        for (int i = 0; i < ScanFolders.Length; i++)
        {
            string folder = ScanFolders[i];
            if (!AssetDatabase.IsValidFolder(folder))
                continue;

            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { folder });
            for (int g = 0; g < guids.Length; g++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[g]);
                string fileName = Path.GetFileNameWithoutExtension(path);
                if (!LooksLikeRangedClip(fileName))
                    continue;

                discovered.Add(fileName);
            }
        }

        discovered.Sort();
        return discovered;
    }

    private static bool LooksLikeRangedClip(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        string lower = fileName.ToLowerInvariant();
        return lower.Contains("rifle")
            || lower.Contains("pistol")
            || lower.Contains("handgun")
            || lower.Contains("2handedgun")
            || lower.Contains("assaultrifle")
            || lower.Contains("reload")
            || lower.Contains("strafe armed")
            || lower.Contains("aiming")
            || lower.Contains("shot")
            || lower.Contains("shoot");
    }

    private static int UpsertEntry(List<GkcActionCatalogEntry> entries, GkcActionCatalogEntry candidate)
    {
        if (candidate == null)
            return 0;

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].combatAction != candidate.combatAction)
                continue;

            entries[i] = candidate;
            return 1;
        }

        entries.Add(candidate);
        return 1;
    }

    private static AnimationClip LoadClip(string assetPath, string clipName)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is AnimationClip clip && clip.name == clipName)
                return clip;
        }

        return null;
    }

    private static bool TryFindBlendTree(
        AnimatorController controller,
        string blendTreeName,
        out BlendTree blendTree)
    {
        blendTree = null;
        if (controller == null || controller.layers == null)
            return false;

        for (int i = 0; i < controller.layers.Length; i++)
        {
            if (TryFindBlendTree(controller.layers[i].stateMachine, blendTreeName, ref blendTree))
                return true;
        }

        return blendTree != null;
    }

    private static bool TryFindBlendTree(
        AnimatorStateMachine stateMachine,
        string blendTreeName,
        ref BlendTree found)
    {
        if (stateMachine == null)
            return false;

        foreach (ChildAnimatorState child in stateMachine.states)
        {
            if (child.state?.motion is BlendTree tree && tree.name == blendTreeName)
            {
                found = tree;
                return true;
            }

            if (child.state?.motion is BlendTree nestedTree
                && TryFindBlendTreeRecursive(nestedTree, blendTreeName, ref found))
            {
                return true;
            }
        }

        for (int i = 0; i < stateMachine.stateMachines.Length; i++)
        {
            if (TryFindBlendTree(stateMachine.stateMachines[i].stateMachine, blendTreeName, ref found))
                return true;
        }

        return false;
    }

    private static bool TryFindBlendTreeRecursive(
        BlendTree root,
        string blendTreeName,
        ref BlendTree found)
    {
        if (root == null)
            return false;

        if (root.name == blendTreeName)
        {
            found = root;
            return true;
        }

        ChildMotion[] children = root.children;
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].motion is BlendTree nested
                && TryFindBlendTreeRecursive(nested, blendTreeName, ref found))
            {
                return true;
            }
        }

        return false;
    }

    private static int AssignStrafeIdClip(BlendTree blendTree, float strafeId, AnimationClip clip)
    {
        if (blendTree == null || clip == null)
            return 0;

        ChildMotion[] children = blendTree.children;
        bool changed = false;
        for (int i = 0; i < children.Length; i++)
        {
            if (!Mathf.Approximately(children[i].threshold, strafeId))
                continue;

            if (children[i].motion == clip)
                return 0;

            children[i].motion = clip;
            changed = true;
            break;
        }

        if (!changed)
            return 0;

        blendTree.children = children;
        return 1;
    }

    private static int AssignFireWeaponsAimIdleClips(
        AnimatorController controller,
        AnimationClip rifleClip,
        AnimationClip pistolClip)
    {
        int wired = 0;
        wired += AssignWeaponIdCenterClip(controller, "Fire Weapons Idle", GkcAnimatorConstants.WeaponIdPistol, pistolClip);
        wired += AssignWeaponIdCenterClip(controller, "Fire Weapons Idle", GkcAnimatorConstants.WeaponIdRifle, rifleClip);
        wired += AssignWeaponIdCenterClip(controller, "Fire Weapons", GkcAnimatorConstants.WeaponIdPistol, pistolClip);
        wired += AssignWeaponIdCenterClip(controller, "Fire Weapons", GkcAnimatorConstants.WeaponIdRifle, rifleClip);
        return wired;
    }

    private static int AssignWeaponIdCenterClip(
        AnimatorController controller,
        string parentBlendTreeName,
        float weaponId,
        AnimationClip clip)
    {
        if (controller == null || clip == null || string.IsNullOrWhiteSpace(parentBlendTreeName))
            return 0;

        if (!TryFindBlendTree(controller, parentBlendTreeName, out BlendTree parentTree))
            return 0;

        string childTreeName = weaponId == GkcAnimatorConstants.WeaponIdPistol
            ? "Weapon ID 1"
            : weaponId == GkcAnimatorConstants.WeaponIdRifle
                ? "Weapon ID 2"
                : null;

        if (childTreeName == null)
            return 0;

        ChildMotion[] parentChildren = parentTree.children;
        for (int i = 0; i < parentChildren.Length; i++)
        {
            if (!Mathf.Approximately(parentChildren[i].threshold, weaponId))
                continue;

            if (parentChildren[i].motion is not BlendTree weaponTree
                || weaponTree.name != childTreeName)
            {
                continue;
            }

            ChildMotion[] weaponChildren = weaponTree.children;
            if (weaponChildren.Length == 0)
                return 0;

            if (weaponChildren[0].motion == clip)
                return 0;

            weaponChildren[0].motion = clip;
            weaponTree.children = weaponChildren;
            parentChildren[i].motion = weaponTree;
            parentTree.children = parentChildren;
            EditorUtility.SetDirty(weaponTree);
            return 1;
        }

        return 0;
    }
}
