using Project.Building;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Building
{
    public static class DMBuildingMaterialLibraryBuilder
    {
        const string Folder = "Assets/_Project/Prefabs/Buildings/Library/Stone/Materials";

        public static void EnsureStoneFinishes()
        {
            EnsureFolder("Assets/_Project/Prefabs");
            EnsureFolder("Assets/_Project/Prefabs/Buildings");
            EnsureFolder("Assets/_Project/Prefabs/Buildings/Library");
            EnsureFolder("Assets/_Project/Prefabs/Buildings/Library/Stone");
            EnsureFolder(Folder);

            Material rough = EnsureMaterial(Folder + "/StoneRough.mat", new Color(0.45f, 0.42f, 0.38f, 1f));
            Material cut = EnsureMaterial(Folder + "/StoneCut.mat", new Color(0.62f, 0.58f, 0.52f, 1f));
            Material door = EnsureMaterial(Folder + "/Door.mat", new Color(0.32f, 0.24f, 0.16f, 1f));
            Material glass = EnsureMaterial(Folder + "/WindowGlass.mat", new Color(0.75f, 0.88f, 0.92f, 0.35f), transparent: true);

            if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources/Building"))
                AssetDatabase.CreateFolder("Assets/_Project/Resources", "Building");

            DMBuildingMaterialLibrary library = AssetDatabase.LoadAssetAtPath<DMBuildingMaterialLibrary>(DMBuildingMaterialLibrary.AssetPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<DMBuildingMaterialLibrary>();
                AssetDatabase.CreateAsset(library, DMBuildingMaterialLibrary.AssetPath);
            }

            Upsert(library, "stone_rough", "Rough Stone", rough);
            Upsert(library, "stone_cut", "Cut Stone", cut);
            Upsert(library, "door", "Door", door);
            Upsert(library, "window_glass", "Window Glass", glass);

            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
        }

        static void Upsert(DMBuildingMaterialLibrary library, string id, string displayName, Material material)
        {
            if (library.variants == null)
                library.variants = new System.Collections.Generic.List<DMBuildingMaterialVariant>();

            for (int i = 0; i < library.variants.Count; i++)
            {
                DMBuildingMaterialVariant variant = library.variants[i];
                if (variant == null || variant.id != id)
                    continue;

                if (string.IsNullOrEmpty(variant.displayName))
                    variant.displayName = displayName;
                if (variant.finishedMaterial == null)
                    variant.finishedMaterial = material;
                return;
            }

            library.variants.Add(new DMBuildingMaterialVariant
            {
                id = id,
                displayName = displayName,
                tier = DMBuildingMaterialTier.Stone,
                finishedMaterial = material
            });
        }

        static Material EnsureMaterial(string path, Color color, bool transparent = false)
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;

            Shader shader = Shader.Find("HDRP/Lit") ?? Shader.Find("HDRP/Unlit") ?? Shader.Find("Unlit/Color");
            if (shader == null)
                return null;
            var material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_UnlitColor"))
                material.SetColor("_UnlitColor", color);
            material.color = color;
            if (transparent && material.HasProperty("_SurfaceType"))
            {
                material.SetFloat("_SurfaceType", 1f);
                if (material.HasProperty("_BlendMode"))
                    material.SetFloat("_BlendMode", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = 3000;
            }

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            if (!string.IsNullOrEmpty(parent))
                AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
