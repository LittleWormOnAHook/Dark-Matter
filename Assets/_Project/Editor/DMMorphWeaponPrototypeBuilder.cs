using System.IO;
using Project.Data;
using Project.EditorTools;
using Project.Interaction;
using Project.Prototypes.Weapons;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the prototype Morph Blade pickups (Morph Gun removed 20261028a; DM Ranged replaces it) from Unity primitives under Prefabs/Weapons/Prototypes.
/// Does not touch existing weapons, player, enemy, or FORGE3D prefabs. Does not save scenes.
/// </summary>
public static class DMMorphWeaponPrototypeBuilder
{
    const string PrefabRoot = "Assets/_Project/Prefabs/Weapons/Prototypes";
    const string ProfileRoot = "Assets/_Project/Resources/Combat/Morph";
    const string FormRoot = "Assets/_Project/Resources/Combat/Morph/Forms";
    const string MatRoot = "Assets/_Project/Prefabs/Weapons/Prototypes/Materials";
    const string BladeItemPath = "Assets/_Project/Data/Items/Melee/MorphBlade.asset";

    [MenuItem(DarkMatterGenesisEditorMenus.Combat + "Build Morph Weapon Prototypes", false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Combat_Menu_Band)]
    public static void BuildAll()
    {
        EnsureFolders();
        DM_WeaponMorphStudioSettings studio = LoadOrCreateStudio();
        ItemData sword = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/Melee/Sword of Fear.asset");

        var bladeForms = BuildBladeForms(studio, sword);

        ItemData bladeItem = BuildBladeItem(sword);

        GameObject bladeHeld = BuildBladeVisual("MorphBlade_Held", bladeItem, bladeForms, worldPickup: false);
        GameObject bladeWorld = BuildBladeVisual("MorphBlade_Pickup", bladeItem, bladeForms, worldPickup: true);

        bladeItem.heldPrefab = bladeHeld;
        bladeItem.worldPrefab = bladeWorld;
        EditorUtility.SetDirty(bladeItem);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[DMMorph] Prototypes ready under " + PrefabRoot +
                  "\nDrop MorphBlade_Pickup into the scene. Y cycles forms while drawn.");
    }

    static void EnsureFolders()
    {
        Directory.CreateDirectory(PrefabRoot.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(FormRoot.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(MatRoot.Replace('/', Path.DirectorySeparatorChar));
        AssetDatabase.Refresh();
    }

    static DM_WeaponMorphStudioSettings LoadOrCreateStudio()
    {
        string path = ProfileRoot + "/DM_WeaponMorphStudioSettings.asset";
        var s = AssetDatabase.LoadAssetAtPath<DM_WeaponMorphStudioSettings>(path);
        if (s == null)
        {
            s = ScriptableObject.CreateInstance<DM_WeaponMorphStudioSettings>();
            s.ApplyPreset(DM_WeaponMorphStudioSettings.Preset.Default);
            AssetDatabase.CreateAsset(s, path);
        }
        return s;
    }

    static DM_WeaponMorphProfile CreateForm(string fileName, System.Action<DM_WeaponMorphProfile> setup)
    {
        string path = FormRoot + "/" + fileName + ".asset";
        var p = AssetDatabase.LoadAssetAtPath<DM_WeaponMorphProfile>(path);
        if (p == null)
        {
            p = ScriptableObject.CreateInstance<DM_WeaponMorphProfile>();
            AssetDatabase.CreateAsset(p, path);
        }
        setup(p);
        EditorUtility.SetDirty(p);
        return p;
    }




    static MorphPartPose Pose(string n, Vector3 pos, Vector3 euler, Vector3 scale) =>
        new MorphPartPose { partName = n, localPosition = pos, localEuler = euler, localScale = scale };

    static DM_WeaponMorphProfile[] BuildBladeForms(DM_WeaponMorphStudioSettings studio, ItemData sword)
    {
        float baseDmg = sword != null ? sword.meleeDamage : 15f;
        return new[]
        {
            BladeForm("MorphBlade_Metal", DMWeaponMorphBladeType.Metal, studio.metalColor, studio.metalLength, 0f, baseDmg, 1f),
            BladeForm("MorphBlade_Light", DMWeaponMorphBladeType.Light, studio.lightColor, studio.lightLength, 3f, baseDmg + 2f, 1.05f),
            BladeForm("MorphBlade_Plasma", DMWeaponMorphBladeType.Plasma, studio.plasmaColor, studio.plasmaLength, 4f, baseDmg + 4f, 1.1f),
            BladeForm("MorphBlade_Ion", DMWeaponMorphBladeType.Ion, studio.ionColor, studio.ionLength, 3.5f, baseDmg + 3f, 1.05f),
            BladeForm("MorphBlade_Laser", DMWeaponMorphBladeType.Laser, studio.laserColor, studio.laserLength, 5f, baseDmg + 5f, 1.2f),
            BladeForm("MorphBlade_Cryo", DMWeaponMorphBladeType.Cryo, studio.cryoColor, studio.cryoLength, 2.5f, baseDmg + 2f, 1.0f),
        };
    }

    static DM_WeaponMorphProfile BladeForm(string file, DMWeaponMorphBladeType type, Color col, float len, float emission, float dmg, float weight)
    {
        return CreateForm(file, p =>
        {
            p.displayName = type.ToString();
            p.kind = DMWeaponMorphKind.MeleeBlade;
            p.bladeType = type;
            p.bladeColor = col;
            p.bladeEmission = emission;
            p.meleeBladeLength = len;
            p.meleeDamage = dmg;
            p.meleeDamageRandomRange = 4f;
            p.meleeRange = 2.0f + len * 0.35f;
            p.meleeCooldown = 0.55f;
            p.criticalChance = 0.12f;
            p.criticalDamageMultiplier = 2f;
            p.meleeStaminaCost = 8f;
            p.bodyWeightScale = weight;
            p.bladeLengthScale = 1f;
        });
    }


    static ItemData BuildBladeItem(ItemData sword)
    {
        var item = AssetDatabase.LoadAssetAtPath<ItemData>(BladeItemPath);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<ItemData>();
            AssetDatabase.CreateAsset(item, BladeItemPath);
        }
        item.itemName = "Morph Blade (Prototype)";
        item.itemType = ItemType.MeleeWeapon;
        item.maxStack = 1;
        item.weaponGrip = WeaponGrip.OneHanded;
        item.invectorWeaponPrefab = null;
        if (sword != null)
        {
            item.meleeDamage = sword.meleeDamage;
            item.meleeDamageRandomRange = sword.meleeDamageRandomRange;
            item.meleeRange = sword.meleeRange;
            item.meleeBladeLength = sword.meleeBladeLength;
            item.meleeCooldown = sword.meleeCooldown;
            item.criticalChance = sword.criticalChance;
            item.criticalDamageMultiplier = sword.criticalDamageMultiplier;
            item.meleeStaminaCost = sword.meleeStaminaCost;
            item.heldLocalPosition = sword.heldLocalPosition;
            item.heldLocalEuler = sword.heldLocalEuler;
            item.useHeldLocalRotation = sword.useHeldLocalRotation;
            item.heldLocalRotation = sword.heldLocalRotation;
            item.equipSocketName = string.IsNullOrEmpty(sword.equipSocketName) ? "meleeHandler" : sword.equipSocketName;
            item.sheatheSocketName = sword.sheatheSocketName;
            item.icon = sword.icon;
        }
        EditorUtility.SetDirty(item);
        return item;
    }


    static GameObject BuildBladeVisual(string name, ItemData item, DM_WeaponMorphProfile[] forms, bool worldPickup)
    {
        var root = new GameObject(name);
        try
        {
            Material hiltMat = GetOrCreateMat("MorphBlade_Hilt", new Color(0.2f, 0.2f, 0.22f));
            AddPart(root.transform, "Hilt", PrimitiveType.Cylinder, hiltMat).localScale = new Vector3(0.035f, 0.08f, 0.035f);
            AddPart(root.transform, "Handle", PrimitiveType.Cube, hiltMat).localScale = new Vector3(0.04f, 0.12f, 0.04f);

            CreateBladeChild(root.transform, "Blade_Metal", PrimitiveType.Cube, forms[0]);
            CreateBladeChild(root.transform, "Blade_Light", PrimitiveType.Capsule, forms[1]);
            CreateBladeChild(root.transform, "Blade_Plasma", PrimitiveType.Cylinder, forms[2]);
            CreateBladeChild(root.transform, "Blade_Ion", PrimitiveType.Sphere, forms[3]);
            CreateBladeChild(root.transform, "Blade_Laser", PrimitiveType.Cylinder, forms[4]);
            CreateBladeChild(root.transform, "Blade_Cryo", PrimitiveType.Cube, forms[5]);

            var morph = root.AddComponent<DMWeaponMorphController>();
            morph.Configure(item, DMWeaponMorphKind.MeleeBlade, forms);

            string path = PrefabRoot + "/" + name + ".prefab";
            if (worldPickup)
            {
                // ItemPickup [RequireComponent(Collider)] needs a collider on the ROOT, not only children.
                if (root.GetComponent<Collider>() == null)
                    root.AddComponent<BoxCollider>();
                var built = WeaponPrefabBuilder.BuildWorldInstance(root, name, item, WeaponPrefabBuilder.DefaultPickupOptions, configureHitbox: false);
                Object.DestroyImmediate(root);
                var prefab = PrefabUtility.SaveAsPrefabAsset(built, path);
                Object.DestroyImmediate(built);
                return prefab;
            }

            WeaponHitbox.SetupPrefabRoot(root, item);
            StripCollidersKeepHitbox(root);
            var held = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return held;
        }
        catch
        {
            Object.DestroyImmediate(root);
            throw;
        }
    }

    static Transform CreateBladeChild(Transform parent, string name, PrimitiveType type, DM_WeaponMorphProfile form)
    {
        Transform t = AddPart(parent, name, type, GetOrCreateMat(name, form != null ? form.bladeColor : Color.gray));
        float len = form != null ? form.meleeBladeLength : 0.85f;
        t.localPosition = new Vector3(0f, 0.12f + len * 0.5f, 0f);
        switch (type)
        {
            case PrimitiveType.Cube:
                t.localScale = name.Contains("Cryo") ? new Vector3(0.06f, len, 0.02f) : new Vector3(0.035f, len, 0.012f);
                break;
            case PrimitiveType.Capsule:
                t.localScale = new Vector3(0.04f, len * 0.5f, 0.04f);
                break;
            case PrimitiveType.Cylinder:
                t.localScale = name.Contains("Laser") ? new Vector3(0.02f, len * 0.5f, 0.02f) : new Vector3(0.04f, len * 0.5f, 0.04f);
                break;
            case PrimitiveType.Sphere:
                t.localScale = new Vector3(0.05f, len, 0.05f);
                break;
        }
        t.gameObject.SetActive(name.EndsWith("Metal"));
        return t;
    }

    static Transform AddPart(Transform parent, string name, PrimitiveType type, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        var col = go.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);
        var rend = go.GetComponent<Renderer>();
        if (rend != null && mat != null) rend.sharedMaterial = mat;
        return go.transform;
    }

    static void StripColliders(GameObject root)
    {
        foreach (var c in root.GetComponentsInChildren<Collider>(true))
            Object.DestroyImmediate(c);
    }

    static void StripCollidersKeepHitbox(GameObject root)
    {
        foreach (var c in root.GetComponentsInChildren<Collider>(true))
        {
            if (c.gameObject.name == WeaponHitbox.HitColliderChildName)
                continue;
            Object.DestroyImmediate(c);
        }
        WeaponHitbox.SetupPrefabRoot(root, null);
    }

    static Material GetOrCreateMat(string name, Color color)
    {
        string path = MatRoot + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("HDRP/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            mat.name = name;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(mat);
        }
        return mat;
    }
}
