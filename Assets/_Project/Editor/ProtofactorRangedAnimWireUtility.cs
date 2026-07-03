using System;
using System.Collections.Generic;
using Project.EditorTools;
using Project.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Wires PROTOFACTOR 2Handed Gun (pistol) and Assault Rifle animsets into GKC fire-weapon blend trees.
/// </summary>
public static class ProtofactorRangedAnimWireUtility
{
    private const string ControllerPath = PlayerAnimatorControllerPaths.GkcControllerPath;
    private const string CatalogPath = "Assets/_Project/Data/Animation/GkcActionCatalog.asset";
    private const string DrawKeepStateMachineName = "Drawk-Keep Fire Weapons";

    private const string PistolFolder =
        "Assets/Animations/PROTOFACTOR/Ultimate Animation Collection/Animations/2Handed Gun Animset/FBX Motions";

    private const string RifleFolder =
        "Assets/Animations/PROTOFACTOR/Ultimate Animation Collection/Animations/Assault Rifle Animset/FBX motions";

    private static readonly string[] FireWeaponRootNames = { "Fire Weapons", "Fire Weapons Idle" };

    private static readonly (Vector2 position, string clipStem)[] WeaponIdClipStems =
    {
        (new Vector2(0f, 0f), "IdleAim"),
        (new Vector2(1f, 0f), "WalkRightAiming"),
        (new Vector2(-1f, 0f), "WalkLeftAiming"),
        (new Vector2(2f, 0f), "WalkRightAiming"),
        (new Vector2(-2f, 0f), "WalkLeftAiming"),
    };

    private static readonly (Vector2 position, string clipStem)[] WalkStrafeClipStems =
    {
        (new Vector2(0f, 0f), "IdleAim"),
        (new Vector2(0f, 1f), "WalkForwardAiming"),
        (new Vector2(0f, -1f), "WalkBackwardsAiming"),
        (new Vector2(1f, 0f), "WalkRightAiming"),
        (new Vector2(-1f, 0f), "WalkLeftAiming"),
    };

    private static readonly (Vector2 position, string clipStem)[] RunClipStems =
    {
        (new Vector2(0f, 1f), "RunForwardAiming"),
        (new Vector2(1f, 0f), "RunForwardAiming"),
        (new Vector2(-1f, 0f), "RunForwardAiming"),
        (new Vector2(0f, -1f), "RunBackwardsAiming"),
    };

    // Hip-hold (non-aim) locomotion. Protofactor only ships forward hold clips;
    // laterals/backwards fall back to the aiming variants.
    private static readonly (Vector2 position, string clipStem)[] HipWeaponIdClipStems =
    {
        (new Vector2(0f, 0f), "IdleHold"),
        (new Vector2(1f, 0f), "WalkRightAiming"),
        (new Vector2(-1f, 0f), "WalkLeftAiming"),
        (new Vector2(2f, 0f), "WalkRightAiming"),
        (new Vector2(-2f, 0f), "WalkLeftAiming"),
    };

    private static readonly (Vector2 position, string clipStem)[] HipWalkStrafeClipStems =
    {
        (new Vector2(0f, 0f), "IdleHold"),
        (new Vector2(0f, 1f), "WalkForward"),
        (new Vector2(0f, -1f), "WalkBackwardsAiming"),
        (new Vector2(1f, 0f), "WalkRightAiming"),
        (new Vector2(-1f, 0f), "WalkLeftAiming"),
        (new Vector2(1.2f, 0f), "WalkRightAiming"),
        (new Vector2(-1.2f, 0f), "WalkLeftAiming"),
    };

    private static readonly (Vector2 position, string clipStem)[] HipRunClipStems =
    {
        (new Vector2(0f, 1f), "RunForward"),
        (new Vector2(1f, 0f), "RunForward"),
        (new Vector2(-1f, 0f), "RunForward"),
        (new Vector2(0f, -1f), "RunBackwardsAiming"),
    };

    private const string HipTreeSuffix = " Hip";

    [MenuItem(SurvivalPioneerEditorMenus.CombatAnimations + "Wire Protofactor Ranged Animsets", false, 3)]
    public static void WireProtofactorRangedAnimsetsMenu()
    {
        WireProtofactorRangedAnimsets(showDialog: true);
    }

    public static int WireProtofactorRangedAnimsets(bool showDialog)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            if (showDialog)
                EditorUtility.DisplayDialog("Protofactor Ranged", $"Missing controller at {ControllerPath}", "OK");
            return -1;
        }

        ProtofactorWeaponSet pistol = ProtofactorWeaponSet.Load(
            PistolFolder,
            "2HandedGun",
            GkcAnimatorConstants.WeaponIdPistol);
        ProtofactorWeaponSet rifle = ProtofactorWeaponSet.Load(
            RifleFolder,
            "AssaultRifle",
            GkcAnimatorConstants.WeaponIdRifle);

        if (pistol == null || rifle == null)
        {
            string message = "Missing PROTOFACTOR clips. Expected folders:\n"
                + $"- {PistolFolder}\n"
                + $"- {RifleFolder}";
            if (showDialog)
                EditorUtility.DisplayDialog("Protofactor Ranged", message, "OK");
            else
                Debug.LogError(message);
            return -1;
        }

        int wired = 0;
        wired += RangedAnimationAutoWireUtility.WireRangedAimIdleClips(showDialog: false);
        wired += WireFireWeaponLocomotion(controller, pistol);
        wired += WireFireWeaponLocomotion(controller, rifle);
        wired += EnsureHipVariants(controller, pistol, GkcAnimatorConstants.WeaponIdPistolHip);
        wired += EnsureHipVariants(controller, rifle, GkcAnimatorConstants.WeaponIdRifleHip);
        wired += EnsureDrawKeepShootStates(controller, pistol, rifle);
        wired += UpdateFireCatalogEntries(pistol, rifle);

        if (wired > 0)
        {
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
        }

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Protofactor Ranged",
                wired > 0
                    ? $"Wired {wired} PROTOFACTOR clip assignment(s) for pistol + rifle (aim, move, fire)."
                    : "Protofactor pistol/rifle animsets are already wired.",
                "OK");
        }

        return wired;
    }

    private static int WireFireWeaponLocomotion(AnimatorController controller, ProtofactorWeaponSet set)
    {
        int wired = 0;
        for (int i = 0; i < FireWeaponRootNames.Length; i++)
        {
            if (!TryFindBlendTree(controller, FireWeaponRootNames[i], out BlendTree root))
                continue;

            ChildMotion[] children = root.children;
            for (int c = 0; c < children.Length; c++)
            {
                if (!Mathf.Approximately(children[c].threshold, set.WeaponId))
                    continue;

                if (children[c].motion is BlendTree weaponRoot)
                    wired += WireWeaponHierarchy(weaponRoot, set);
            }
        }

        return wired;
    }

    /// <summary>
    /// Clones each weapon's aim subtree into a hip variant at threshold <paramref name="hipWeaponId"/>
    /// and rewires it with the non-aim hold clips. The runtime driver selects
    /// WeaponId 1/2 while aiming and 11/12 while hip-holding.
    /// Every Weapon ID-driven tree gets a hip child so 11/12 never clamps to
    /// another weapon's clips ("Fire Weapons", "Fire Weapons Idle", "Walk", "Run Strafe", ...).
    /// </summary>
    private static int EnsureHipVariants(AnimatorController controller, ProtofactorWeaponSet set, float hipWeaponId)
    {
        int wired = 0;
        string controllerPath = AssetDatabase.GetAssetPath(controller);
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(controllerPath))
        {
            if (asset is not BlendTree root
                || root.blendType != BlendTreeType.Simple1D
                || root.blendParameter != "Weapon ID"
                || root.name.EndsWith(HipTreeSuffix, StringComparison.Ordinal))
            {
                continue;
            }

            ChildMotion[] children = root.children;
            BlendTree aimSource = null;
            BlendTree hipTree = null;

            for (int c = 0; c < children.Length; c++)
            {
                if (Mathf.Approximately(children[c].threshold, set.WeaponId)
                    && children[c].motion is BlendTree aimTree)
                {
                    aimSource = aimTree;
                }

                if (Mathf.Approximately(children[c].threshold, hipWeaponId)
                    && children[c].motion is BlendTree existingHip)
                {
                    hipTree = existingHip;
                }
            }

            if (aimSource == null)
                continue;

            if (hipTree == null)
            {
                hipTree = CloneBlendTree(aimSource, controller, HipTreeSuffix);
                AppendChild(root, hipTree, hipWeaponId);
                wired++;
            }

            wired += WireHipHierarchy(hipTree, set, ResolveHipStemsForRoot(root.name));
        }

        return wired;
    }

    private static (Vector2 position, string clipStem)[] ResolveHipStemsForRoot(string rootName)
    {
        if (rootName.StartsWith("Run", StringComparison.Ordinal))
            return HipRunClipStems;
        if (rootName.StartsWith("Walk", StringComparison.Ordinal))
            return HipWalkStrafeClipStems;
        return HipWeaponIdClipStems;
    }

    private static BlendTree CloneBlendTree(BlendTree source, AnimatorController controller, string suffix)
    {
        BlendTree clone = new BlendTree
        {
            name = source.name + suffix,
            blendType = source.blendType,
            blendParameter = source.blendParameter,
            blendParameterY = source.blendParameterY,
            minThreshold = source.minThreshold,
            maxThreshold = source.maxThreshold,
            useAutomaticThresholds = false,
            hideFlags = HideFlags.HideInHierarchy,
        };
        AssetDatabase.AddObjectToAsset(clone, controller);

        ChildMotion[] children = source.children;
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].motion is BlendTree nested)
                children[i].motion = CloneBlendTree(nested, controller, suffix);
        }

        clone.children = children;
        EditorUtility.SetDirty(clone);
        return clone;
    }

    private static void AppendChild(BlendTree root, BlendTree child, float threshold)
    {
        ChildMotion[] children = root.children;
        ChildMotion[] expanded = new ChildMotion[children.Length + 1];
        children.CopyTo(expanded, 0);
        expanded[children.Length] = new ChildMotion
        {
            motion = child,
            threshold = threshold,
            timeScale = 1f,
            directBlendParameter = root.blendParameter,
        };

        bool wasAutomatic = root.useAutomaticThresholds;
        root.useAutomaticThresholds = false;
        root.children = expanded;
        if (wasAutomatic)
            Debug.LogWarning($"ProtofactorRangedAnimWireUtility: disabled automatic thresholds on '{root.name}' to append hip variant.");
        EditorUtility.SetDirty(root);
    }

    private static int WireHipHierarchy(
        BlendTree root,
        ProtofactorWeaponSet set,
        (Vector2 position, string clipStem)[] weaponIdStems)
    {
        int wired = 0;
        var visited = new HashSet<BlendTree>();
        WalkHipHierarchy(root, set, weaponIdStems, visited, ref wired);
        return wired;
    }

    private static void WalkHipHierarchy(
        BlendTree tree,
        ProtofactorWeaponSet set,
        (Vector2 position, string clipStem)[] weaponIdStems,
        HashSet<BlendTree> visited,
        ref int wired)
    {
        if (tree == null || !visited.Add(tree))
            return;

        if (tree.name.StartsWith("Weapon ID 1", StringComparison.Ordinal)
            || tree.name.StartsWith("Weapon ID 2", StringComparison.Ordinal))
        {
            wired += ApplyStemClips(tree, set, weaponIdStems);
        }
        else if (tree.name.StartsWith("Walk Strafe", StringComparison.Ordinal))
        {
            wired += ApplyStemClips(tree, set, HipWalkStrafeClipStems);
        }
        else if (tree.name.StartsWith("Run", StringComparison.Ordinal))
        {
            wired += ApplyStemClips(tree, set, HipRunClipStems);
        }

        ChildMotion[] children = tree.children;
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].motion is BlendTree nested)
                WalkHipHierarchy(nested, set, weaponIdStems, visited, ref wired);
        }
    }

    private static int WireWeaponHierarchy(BlendTree root, ProtofactorWeaponSet set)
    {
        int wired = 0;
        var visited = new HashSet<BlendTree>();
        WalkWeaponHierarchy(root, set, visited, ref wired);
        return wired;
    }

    private static void WalkWeaponHierarchy(
        BlendTree tree,
        ProtofactorWeaponSet set,
        HashSet<BlendTree> visited,
        ref int wired)
    {
        if (tree == null || !visited.Add(tree))
            return;

        if (tree.name is "Weapon ID 1" or "Weapon ID 2")
            wired += ApplyStemClips(tree, set, WeaponIdClipStems);
        else if (tree.name == "Walk Strafe")
            wired += ApplyStemClips(tree, set, WalkStrafeClipStems);
        else if (tree.name == "Run")
            wired += ApplyStemClips(tree, set, RunClipStems);

        ChildMotion[] children = tree.children;
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].motion is BlendTree nested)
                WalkWeaponHierarchy(nested, set, visited, ref wired);
        }
    }

    private static int ApplyStemClips(
        BlendTree tree,
        ProtofactorWeaponSet set,
        (Vector2 position, string clipStem)[] stems)
    {
        ChildMotion[] children = tree.children;
        int wired = 0;
        for (int i = 0; i < children.Length; i++)
        {
            AnimationClip clip = ResolveStemClip(set, stems, children[i].position);
            if (clip == null || children[i].motion == clip)
                continue;

            children[i].motion = clip;
            wired++;
        }

        if (wired > 0)
        {
            tree.children = children;
            EditorUtility.SetDirty(tree);
        }

        return wired;
    }

    private static AnimationClip ResolveStemClip(
        ProtofactorWeaponSet set,
        (Vector2 position, string clipStem)[] stems,
        Vector2 childPosition)
    {
        for (int i = 0; i < stems.Length; i++)
        {
            (Vector2 position, string clipStem) = stems[i];
            if ((childPosition - position).sqrMagnitude > 0.02f)
                continue;

            return set.LoadClip(clipStem);
        }

        return null;
    }

    private static int EnsureDrawKeepShootStates(
        AnimatorController controller,
        ProtofactorWeaponSet pistol,
        ProtofactorWeaponSet rifle)
    {
        if (!TryFindStateMachine(controller, DrawKeepStateMachineName, out AnimatorStateMachine stateMachine))
            return 0;

        int wired = 0;
        wired += UpsertStateClip(stateMachine, "Shoot Primary Pistol", pistol.LoadClip("ShootPrimary"));
        wired += UpsertStateClip(stateMachine, "Shoot Primary Rifle", rifle.LoadClip("ShootPrimary"));
        return wired;
    }

    private static int UpsertStateClip(AnimatorStateMachine stateMachine, string stateName, AnimationClip clip)
    {
        if (stateMachine == null || clip == null)
            return 0;

        AnimatorState state = FindState(stateMachine, stateName);
        if (state == null)
        {
            state = stateMachine.AddState(stateName, new Vector3(940f, 880f + stateMachine.states.Length * 40f, 0f));
            EditorUtility.SetDirty(stateMachine);
        }

        if (state.motion == clip)
            return 0;

        state.motion = clip;
        EditorUtility.SetDirty(state);
        return 1;
    }

    private static int UpdateFireCatalogEntries(ProtofactorWeaponSet pistol, ProtofactorWeaponSet rifle)
    {
        GkcActionCatalog catalog = AssetDatabase.LoadAssetAtPath<GkcActionCatalog>(CatalogPath);
        if (catalog == null)
            return 0;

        List<GkcActionCatalogEntry> entries = new List<GkcActionCatalogEntry>(catalog.Entries);
        int wired = 0;
        wired += UpsertFireEntry(
            entries,
            GkcCombatAction.PistolFire,
            $"{DrawKeepStateMachineName}.Shoot Primary Pistol",
            pistol.LoadClip("ShootPrimary"),
            0.18f);
        wired += UpsertFireEntry(
            entries,
            GkcCombatAction.RifleFire,
            $"{DrawKeepStateMachineName}.Shoot Primary Rifle",
            rifle.LoadClip("ShootPrimary"),
            0.22f);

        if (wired <= 0)
            return 0;

        catalog.SetEntries(entries);
        EditorUtility.SetDirty(catalog);
        return wired;
    }

    private static int UpsertFireEntry(
        List<GkcActionCatalogEntry> entries,
        GkcCombatAction action,
        string stateName,
        AnimationClip clip,
        float duration)
    {
        if (clip == null)
            return 0;

        GkcActionCatalogEntry seed = GkcActionCatalogClassifier.BuildManualSeedEntries()
            .Find(entry => entry.combatAction == action);
        if (seed == null)
            return 0;

        seed.stateName = stateName;
        seed.defaultDuration = duration;

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].combatAction != action)
                continue;

            if (entries[i].stateName == seed.stateName
                && Mathf.Approximately(entries[i].defaultDuration, seed.defaultDuration))
            {
                return 0;
            }

            entries[i] = seed;
            return 1;
        }

        entries.Add(seed);
        return 1;
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

    private static bool TryFindStateMachine(
        AnimatorController controller,
        string stateMachineName,
        out AnimatorStateMachine found)
    {
        found = null;
        if (controller == null)
            return false;

        for (int i = 0; i < controller.layers.Length; i++)
        {
            if (TryFindStateMachineRecursive(controller.layers[i].stateMachine, stateMachineName, ref found))
                return true;
        }

        return false;
    }

    private static bool TryFindStateMachineRecursive(
        AnimatorStateMachine stateMachine,
        string stateMachineName,
        ref AnimatorStateMachine found)
    {
        if (stateMachine == null)
            return false;

        if (stateMachine.name == stateMachineName)
        {
            found = stateMachine;
            return true;
        }

        for (int i = 0; i < stateMachine.stateMachines.Length; i++)
        {
            if (TryFindStateMachineRecursive(stateMachine.stateMachines[i].stateMachine, stateMachineName, ref found))
                return true;
        }

        return false;
    }

    private static bool TryFindBlendTree(
        AnimatorController controller,
        string blendTreeName,
        out BlendTree blendTree)
    {
        blendTree = null;
        if (controller == null)
            return false;

        for (int i = 0; i < controller.layers.Length; i++)
        {
            if (TryFindBlendTreeRecursive(controller.layers[i].stateMachine, blendTreeName, ref blendTree))
                return true;
        }

        return blendTree != null;
    }

    private static bool TryFindBlendTreeRecursive(
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

            if (child.state?.motion is BlendTree nested
                && WalkBlendTreeRecursive(nested, blendTreeName, ref found))
            {
                return true;
            }
        }

        for (int i = 0; i < stateMachine.stateMachines.Length; i++)
        {
            if (TryFindBlendTreeRecursive(stateMachine.stateMachines[i].stateMachine, blendTreeName, ref found))
                return true;
        }

        return false;
    }

    private static bool WalkBlendTreeRecursive(BlendTree root, string blendTreeName, ref BlendTree found)
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
            if (children[i].motion is BlendTree nested && WalkBlendTreeRecursive(nested, blendTreeName, ref found))
                return true;
        }

        return false;
    }

    private sealed class ProtofactorWeaponSet
    {
        private readonly string folder;
        private readonly string suffix;
        private readonly Dictionary<string, AnimationClip> cache = new Dictionary<string, AnimationClip>();

        public float WeaponId { get; }

        private ProtofactorWeaponSet(string folder, string suffix, float weaponId)
        {
            this.folder = folder;
            this.suffix = suffix;
            WeaponId = weaponId;
        }

        public static ProtofactorWeaponSet Load(string folder, string suffix, float weaponId)
        {
            var set = new ProtofactorWeaponSet(folder, suffix, weaponId);
            return set.LoadClip("IdleAim") != null && set.LoadClip("ShootPrimary") != null ? set : null;
        }

        public AnimationClip LoadClip(string stem)
        {
            if (cache.TryGetValue(stem, out AnimationClip cached))
                return cached;

            string clipName = stem + suffix;
            AnimationClip clip = LoadClipFromFolder(folder, clipName);
            cache[stem] = clip;
            return clip;
        }

        private static AnimationClip LoadClipFromFolder(string folder, string clipName)
        {
            string[] extensions = { ".FBX", ".fbx" };
            for (int i = 0; i < extensions.Length; i++)
            {
                string path = $"{folder}/Humanoid@{clipName}{extensions[i]}";
                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                for (int a = 0; a < assets.Length; a++)
                {
                    if (assets[a] is AnimationClip clip && clip.name == clipName)
                        return clip;
                }
            }

            return null;
        }
    }
}
