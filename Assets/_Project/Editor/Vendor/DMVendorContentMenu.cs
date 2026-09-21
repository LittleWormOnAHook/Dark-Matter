#if UNITY_EDITOR
using System.Collections.Generic;
using Project.Data;
using Project.EditorTools;
using Project.Vendor;
using Project.World.Clock;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Vendor
{
    public static class DMVendorContentMenu
    {
        private const string ClockAssetPath = "Assets/_Project/Resources/World/DM_IoClockProfile.asset";
        private const string VendorFolder = "Assets/_Project/Resources/Vendors";
        private const string CommissaryProfilePath = VendorFolder + "/DM_VendorProfile_Commissary.asset";
        private const string TechProfilePath = VendorFolder + "/DM_VendorProfile_Tech.asset";
        private const string CommissaryCatalogPath = VendorFolder + "/DM_VendorCatalog_Commissary_Camp.asset";
        private const string TechCatalogPath = VendorFolder + "/DM_VendorCatalog_Tech_Camp.asset";
        private const string CommissaryBuyLogPath = VendorFolder + "/DM_VendorBuyLog_Commissary.asset";
        private const string TechBuyLogPath = VendorFolder + "/DM_VendorBuyLog_Tech.asset";

        [MenuItem(DarkMatterGenesisEditorMenus.EnsureIoClockProfile)]
        public static void EnsureIoClockProfile()
        {
            EnsureFolder("Assets/_Project/Resources/World");
            DMIoClockProfile profile = AssetDatabase.LoadAssetAtPath<DMIoClockProfile>(ClockAssetPath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<DMIoClockProfile>();
                AssetDatabase.CreateAsset(profile, ClockAssetPath);
            }

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            Selection.activeObject = profile;
            Debug.Log("[DM] Io clock profile ready at " + ClockAssetPath);
        }

        [MenuItem(DarkMatterGenesisEditorMenus.ApplyDefaultTradeValues)]
        public static void ApplyDefaultTradeValues()
        {
            string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/_Project/Data/Items" });
            int changed = 0;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.Contains("/Nodes/") || path.Contains("/HitMarks/"))
                    continue;

                ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                if (item == null)
                    continue;

                ApplyTrade(item);
                EditorUtility.SetDirty(item);
                changed++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[DM] Applied trade defaults to " + changed + " items.");
        }

        [MenuItem(DarkMatterGenesisEditorMenus.EnsureVendorContent)]
        public static void EnsureVendorContent()
        {
            EnsureIoClockProfile();
            ApplyDefaultTradeValues();
            EnsureFolder(VendorFolder);

            DMVendorCatalog commissaryCatalog = LoadOrCreate<DMVendorCatalog>(CommissaryCatalogPath);
            DMVendorCatalog techCatalog = LoadOrCreate<DMVendorCatalog>(TechCatalogPath);
            if (commissaryCatalog.listings == null || commissaryCatalog.listings.Length == 0)
                commissaryCatalog.listings = BuildListings(DmVendorTradeClass.Commissary);
            if (techCatalog.listings == null || techCatalog.listings.Length == 0)
                techCatalog.listings = AppendArmorPlaceholder(BuildListings(DmVendorTradeClass.TechGear, DmVendorTradeClass.TechUpgrade));
            EditorUtility.SetDirty(commissaryCatalog);
            EditorUtility.SetDirty(techCatalog);

            DMVendorBuyLog commissaryBuy = FillBuyLog(CommissaryBuyLogPath, DMVendorKind.Commissary);
            DMVendorBuyLog techBuy = FillBuyLog(TechBuyLogPath, DMVendorKind.Tech);

            DMVendorProfile commissary = LoadOrCreate<DMVendorProfile>(CommissaryProfilePath);
            commissary.vendorId = "vendor_commissary";
            commissary.displayName = "Commissary";
            commissary.kind = DMVendorKind.Commissary;
            commissary.catalog = commissaryCatalog;
            commissary.buyLog = commissaryBuy;
            commissary.promptText = "Press E — Commissary";
            commissary.purseMin = 500;
            commissary.purseMax = 800;
            EditorUtility.SetDirty(commissary);

            DMVendorProfile tech = LoadOrCreate<DMVendorProfile>(TechProfilePath);
            tech.vendorId = "vendor_tech";
            tech.displayName = "Tech";
            tech.kind = DMVendorKind.Tech;
            tech.catalog = techCatalog;
            tech.buyLog = techBuy;
            tech.promptText = "Press E — Tech";
            tech.purseMin = 500;
            tech.purseMax = 800;
            EditorUtility.SetDirty(tech);

            AssetDatabase.SaveAssets();
            Selection.activeObject = commissary;
            Debug.Log("[DM] Vendor profiles, catalogs, and buy logs are ready under " + VendorFolder);
        }

        [MenuItem(DarkMatterGenesisEditorMenus.FillVendorBuyLogs)]
        public static void FillVendorBuyLogs()
        {
            EnsureFolder(VendorFolder);
            DMVendorBuyLog commissaryBuy = FillBuyLog(CommissaryBuyLogPath, DMVendorKind.Commissary);
            DMVendorBuyLog techBuy = FillBuyLog(TechBuyLogPath, DMVendorKind.Tech);

            DMVendorProfile commissary = AssetDatabase.LoadAssetAtPath<DMVendorProfile>(CommissaryProfilePath);
            if (commissary != null)
            {
                commissary.buyLog = commissaryBuy;
                EditorUtility.SetDirty(commissary);
            }

            DMVendorProfile tech = AssetDatabase.LoadAssetAtPath<DMVendorProfile>(TechProfilePath);
            if (tech != null)
            {
                tech.buyLog = techBuy;
                EditorUtility.SetDirty(tech);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[DM] Vendor buy logs filled. Shop catalogs were not changed.");
        }

        private static void ApplyTrade(ItemData item)
        {
            if (item.acValue <= 0)
                item.acValue = item.InferDefaultAcValue();

            if (item.vendorClass == DmVendorTradeClass.None)
                item.vendorClass = item.ResolveVendorClass();

            if (item.isMiningTool)
            {
                item.canBuy = false;
                item.cannotSellLastCopy = true;
                item.vendorClass = DmVendorTradeClass.TechGear;
                if (item.acValue <= 0)
                    item.acValue = 12;
            }

            if (item.IsStoryBoundName)
            {
                item.rarity = ItemRarity.Unique;
                item.canSell = false;
                item.canBuy = false;
            }

            if (item.itemType == ItemType.Quest
                || item.itemType == ItemType.Vehicle
                || item.itemType == ItemType.WorldDeployable)
            {
                item.canSell = false;
                item.canBuy = false;
                item.vendorClass = DmVendorTradeClass.None;
                item.acValue = 0;
            }
        }

        private static DMVendorBuyLog FillBuyLog(string path, DMVendorKind kind)
        {
            DMVendorBuyLog log = LoadOrCreate<DMVendorBuyLog>(path);
            var list = new List<ItemData>();
            string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/_Project/Data/Items" });
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (SkipBuyLogPath(assetPath))
                    continue;

                ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
                if (!IsVendorPickupItem(item, kind))
                    continue;

                list.Add(item);
            }

            log.items = list.ToArray();
            EditorUtility.SetDirty(log);
            return log;
        }

        private static bool IsVendorPickupItem(ItemData item, DMVendorKind kind)
        {
            if (item == null)
                return false;
            if (item.itemType == ItemType.Quest
                || item.itemType == ItemType.Vehicle
                || item.itemType == ItemType.WorldDeployable)
                return false;
            if (item.name != null && item.name.StartsWith("DMAmmoFx"))
                return false;
            return DMVendorService.AcceptsClass(kind, item.ResolveVendorClass());
        }

        private static bool SkipBuyLogPath(string path)
        {
            return path.Contains("/Nodes/")
                || path.Contains("/HitMarks/")
                || path.Contains("/ammo/DMAmmoFx");
        }

        private static DMVendorListing[] BuildListings(params DmVendorTradeClass[] classes)
        {
            var list = new List<DMVendorListing>();
            string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/_Project/Data/Items" });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.Contains("/Nodes/") || path.Contains("/HitMarks/"))
                    continue;

                ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                if (item == null || !item.ResolveCanBuy())
                    continue;

                DmVendorTradeClass trade = item.ResolveVendorClass();
                bool match = false;
                for (int c = 0; c < classes.Length; c++)
                {
                    if (classes[c] == trade)
                    {
                        match = true;
                        break;
                    }
                }

                if (!match)
                    continue;

                int min = 1;
                int max = 8;
                if (item.IsWeapon || item.itemType == ItemType.Tool)
                {
                    min = 1;
                    max = 3;
                }
                else if (item.IsAmmo || item.CountsAsAmmo)
                {
                    min = 4;
                    max = 16;
                }
                else if (item.componentCategory != ComponentCategory.None)
                {
                    min = 4;
                    max = 12;
                }

                list.Add(new DMVendorListing
                {
                    item = item,
                    stockMin = min,
                    stockMax = max,
                    placeholderLabel = ResolveListingLabel(item, false)
                });
            }

            return list.ToArray();
        }

        private static string ResolveListingLabel(ItemData item, bool armorPlaceholder)
        {
            if (armorPlaceholder)
                return "Suit / Armor — Coming soon";
            if (item == null)
                return "Coming soon";
            if (!string.IsNullOrWhiteSpace(item.itemName)
                && !string.Equals(item.itemName, "New Item", System.StringComparison.OrdinalIgnoreCase))
                return item.itemName.Trim();
            if (!string.IsNullOrWhiteSpace(item.name))
                return item.name.Trim();
            return "Coming soon";
        }

        private static DMVendorListing[] AppendArmorPlaceholder(DMVendorListing[] listings)
        {
            var list = new List<DMVendorListing>(listings);
            list.Add(new DMVendorListing
            {
                armorPlaceholder = true,
                placeholderLabel = "Suit / Armor — Coming soon",
                stockMin = 0,
                stockMax = 0
            });
            return list.ToArray();
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;

            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
