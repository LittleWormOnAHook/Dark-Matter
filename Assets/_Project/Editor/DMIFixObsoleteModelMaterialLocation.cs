#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Unity 6 no longer supports ModelImporter MaterialLocation.External.
    /// Migrates remaining External importers to InPrefab to clear import exceptions.
    /// </summary>
    public static class DMIFixObsoleteModelMaterialLocation
    {
        private const string MenuPath = "Dark Matter/Fix/Migrate Model MaterialLocation External → InPrefab";

        [MenuItem(MenuPath)]
        public static void MigrateAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:Model");
            int scanned = 0;
            int fixedCount = 0;

            try
            {
                AssetDatabase.StartAssetEditing();
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                    if (importer == null)
                        continue;

                    scanned++;
                    // External == 0; avoid naming the obsolete enum member (CS0618).
                    if ((int)importer.materialLocation != 0)
                        continue;

                    importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
                    importer.SaveAndReimport();
                    fixedCount++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[DMIFixObsoleteModelMaterialLocation] scanned={scanned} fixed={fixedCount}");
            EditorUtility.DisplayDialog(
                "Model Material Location",
                $"Scanned {scanned} models.\nMigrated {fixedCount} from External → InPrefab.",
                "OK");
        }
    }
}
#endif
