// 2026-10-28 (a): builds DM Ranged data (settings, dm_ranged ItemData, DMRanged_Pickup prefab) and registers it.
// Never touches the scene or Anthony's placed DM Ranged instances.
using Project.Combat.DMRanged;
using Project.Data;
using UnityEditor;
using UnityEngine;

public static class DMRangedBuilder
{
    const string Fbx = "Assets/_Project/Materials/DM Ranged 1.fbx";
    const string ItemPath = "Assets/_Project/Data/Items/Ranged/dm_ranged.asset";
    const string PistolPath = "Assets/_Project/Data/Items/Ranged/sci_fi_pistol.asset";
    const string PickupPath = "Assets/_Project/Prefabs/Weapons/DMRanged_Pickup.prefab";
    const string SettingsDir = "Assets/_Project/Resources/Combat/Morph/DMRanged";
    const string SettingsPath = SettingsDir + "/DM_RangedMorphSettings.asset";

    [MenuItem(Project.EditorTools.DarkMatterGenesisEditorMenus.Combat + "Build DM Ranged", false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Combat_Menu_Band)]
    public static string BuildAll()
    {
        var log = new System.Text.StringBuilder();
        ItemData pistol = AssetDatabase.LoadAssetAtPath<ItemData>(PistolPath);
        if (pistol == null) return "FAIL no sci_fi_pistol";

        // ItemData (clone of sci_fi_pistol so Invector prefab, ammo, icon, held offsets match)
        var item = AssetDatabase.LoadAssetAtPath<ItemData>(ItemPath);
        if (item == null)
        {
            AssetDatabase.CopyAsset(PistolPath, ItemPath);
            item = AssetDatabase.LoadAssetAtPath<ItemData>(ItemPath);
            log.AppendLine("created " + ItemPath);
        }
        item.itemName = "DM Ranged";
        item.weaponGrip = WeaponGrip.OneHanded;
        item.invectorWeaponPrefab = pistol.invectorWeaponPrefab;
        item.compatibleAmmoTypes = (Project.Combat.AmmoType[])System.Enum.GetValues(typeof(Project.Combat.AmmoType));
        item.heldPrefab = null; // drawn/holstered visuals are Anthony's placed DM Ranged instances

        // Settings
        System.IO.Directory.CreateDirectory(SettingsDir);
        AssetDatabase.Refresh();
        var s = AssetDatabase.LoadAssetAtPath<DM_RangedMorphSettings>(SettingsPath);
        if (s == null) { s = ScriptableObject.CreateInstance<DM_RangedMorphSettings>(); AssetDatabase.CreateAsset(s, SettingsPath); log.AppendLine("created " + SettingsPath); }
        s.item = item;
        s.disabledPlayerItem = pistol;
        // Pistol 1 baseline on the item itself (tooltips when not drawn)
        var p1 = s.forms[s.HolsterIndex];
        item.fireRate = p1.fireRate; item.magazineSize = p1.magazineSize; item.reloadTimeSeconds = p1.reloadSeconds;
        item.rangedRange = p1.range; item.projectileSpreadDegrees = p1.spreadDegrees;
        EditorUtility.SetDirty(s);

        // Pickup prefab: DM Ranged 1 mesh posed at frame 40 + pistol's ItemPickup/collider setup
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx);
        GameObject root = new GameObject("DMRanged_Pickup");
        try
        {
            GameObject pistolWorld = pistol.worldPrefab;
            var mesh = (GameObject)PrefabUtility.InstantiatePrefab(model);
            mesh.name = "DM Ranged 1";
            mesh.transform.SetParent(root.transform, false);
            var anim = mesh.GetComponent<Animation>();
            AnimationClip clip = anim != null ? anim.clip : null;
            if (clip == null) foreach (var c in AnimationUtility.GetAnimationClips(mesh)) { clip = c; break; }
            if (clip != null) clip.SampleAnimation(mesh, s.holsterFrame / 30f);
            if (anim != null) anim.playAutomatically = false;

            Bounds b = new Bounds(root.transform.position, Vector3.zero); bool init = false;
            foreach (var r in root.GetComponentsInChildren<Renderer>()) { if (!init) { b = r.bounds; init = true; } else b.Encapsulate(r.bounds); }
            var box = root.AddComponent<BoxCollider>();
            box.center = b.center; box.size = Vector3.Max(b.size, Vector3.one * 0.05f);

            var srcPickup = pistolWorld != null ? pistolWorld.GetComponent("ItemPickup") : null;
            var srcBox = pistolWorld != null ? pistolWorld.GetComponent<BoxCollider>() : null;
            if (srcBox != null) box.isTrigger = srcBox.isTrigger;
            if (srcPickup != null)
            {
                var pick = root.AddComponent(srcPickup.GetType());
                EditorUtility.CopySerialized(srcPickup, pick);
                var so = new SerializedObject(pick);
                var it = so.GetIterator(); it.Next(true);
                while (it.Next(true))
                    if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue == pistol)
                        it.objectReferenceValue = item;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else log.AppendLine("WARN pistol pickup has no ItemPickup");
            PrefabUtility.SaveAsPrefabAsset(root, PickupPath);
            log.AppendLine("saved " + PickupPath);
        }
        finally { Object.DestroyImmediate(root); }

        item.worldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PickupPath);
        EditorUtility.SetDirty(item);

        // Registries
        log.AppendLine(AddToList(Resources.Load("ItemRegistry"), "items", item, null));
        log.AppendLine(AddToList(Resources.Load("DMHotCrossIconRegistry"), "entries", item, pistol));
        AssetDatabase.SaveAssets();
        return log.ToString();
    }

    /// Adds item to a registry list. When template is set, duplicates the element that references it.
    static string AddToList(Object reg, string listName, ItemData item, ItemData template)
    {
        if (reg == null) return "WARN missing registry for " + listName;
        var so = new SerializedObject(reg);
        var list = so.FindProperty(listName);
        for (int i = 0; i < list.arraySize; i++)
            if (RefersTo(list.GetArrayElementAtIndex(i), item)) return reg.name + ": already has dm_ranged";
        int src = -1;
        if (template != null)
            for (int i = 0; i < list.arraySize; i++) if (RefersTo(list.GetArrayElementAtIndex(i), template)) { src = i; break; }
        if (template != null && src < 0) return "WARN " + reg.name + ": no template entry";
        if (src >= 0) { list.InsertArrayElementAtIndex(src); src = src + 1; list.MoveArrayElement(src, list.arraySize - 1); }
        else list.InsertArrayElementAtIndex(list.arraySize);
        var el = list.GetArrayElementAtIndex(list.arraySize - 1);
        if (el.propertyType == SerializedPropertyType.ObjectReference) el.objectReferenceValue = item;
        else
        {
            var it = el.Copy(); var end = el.GetEndProperty();
            it.Next(true);
            while (!SerializedProperty.EqualContents(it, end))
            {
                if (it.propertyType == SerializedPropertyType.ObjectReference && (it.objectReferenceValue == template || it.objectReferenceValue == null) && it.type.Contains("ItemData"))
                    it.objectReferenceValue = item;
                if (!it.Next(true)) break;
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(reg);
        return reg.name + ": added dm_ranged (n=" + list.arraySize + ")";
    }

    static bool RefersTo(SerializedProperty p, Object target)
    {
        if (p.propertyType == SerializedPropertyType.ObjectReference) return p.objectReferenceValue == target;
        var it = p.Copy(); var end = p.GetEndProperty();
        if (!it.Next(true)) return false;
        while (!SerializedProperty.EqualContents(it, end))
        {
            if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue == target) return true;
            if (!it.Next(true)) break;
        }
        return false;
    }
}
