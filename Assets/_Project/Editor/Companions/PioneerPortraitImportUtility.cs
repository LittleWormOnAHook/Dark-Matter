#if UNITY_EDITOR
using Project.Pioneers;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Companions
{
    /// <summary>
    /// Ensures portrait PNGs under Resources/Portraits (and Art/UI/Portraits) import as single sprites,
    /// and wires NamedPioneerDefinition.portrait + PioneerPortraitLibrary shared sprites.
    /// </summary>
    public sealed class PioneerPortraitImportUtility : AssetPostprocessor
    {
        private const string ResourcesPortraitsFolder = "Assets/_Project/Resources/Portraits";
        private const string ArtPortraitsFolder = "Assets/_Project/Art/UI/Portraits";

        private void OnPreprocessTexture()
        {
            if (!IsPortraitTexturePath(assetPath))
                return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.sRGBTexture = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.crunchedCompression = false;
        }

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            bool touchPortraits = false;
            for (int i = 0; i < importedAssets.Length; i++)
            {
                if (IsPortraitTexturePath(importedAssets[i]))
                {
                    touchPortraits = true;
                    break;
                }
            }

            if (!touchPortraits)
                return;

            AssignAllPortraits();
        }

        [MenuItem("Dark Matter Genesis/Companions/Reimport Portrait Textures")]
        public static void ReimportPortraitTexturesMenu()
        {
            int count = ReimportAllPortraitTextures();
            AssetDatabase.SaveAssets();
            Debug.Log($"[PioneerPortraits] Reimported {count} portrait texture(s) with bilinear + mipmaps.");
        }

        [MenuItem("Dark Matter Genesis/Companions/Assign Pioneer Portraits")]
        public static void AssignAllPortraitsMenu()
        {
            ReimportAllPortraitTextures();
            AssignAllPortraits();
            AssetDatabase.SaveAssets();
            Debug.Log("[PioneerPortraits] Reimported portrait textures and assigned named + shared ID portraits.");
        }

        private static int ReimportAllPortraitTextures()
        {
            int count = 0;
            ReimportPortraitFolder(ResourcesPortraitsFolder, ref count);
            ReimportPortraitFolder(ArtPortraitsFolder, ref count);
            return count;
        }

        private static void ReimportPortraitFolder(string folder, ref int count)
        {
            if (!AssetDatabase.IsValidFolder(folder))
                return;

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!IsPortraitTexturePath(path))
                    continue;

                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                count++;
            }
        }

        public static void AssignAllPortraits()
        {
            EnsurePortraitLibrary();
            AssignNamedDefinitionPortraits();
            PioneerPortraitResolver.ReloadCache();
            NamedPioneerCatalog.ReloadCache();
        }

        private static void EnsurePortraitLibrary()
        {
            const string libraryPath = "Assets/_Project/Resources/PioneerPortraitLibrary.asset";
            PioneerPortraitLibrary library = AssetDatabase.LoadAssetAtPath<PioneerPortraitLibrary>(libraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<PioneerPortraitLibrary>();
                AssetDatabase.CreateAsset(library, libraryPath);
            }

            Sprite echo = LoadPortraitSprite("portrait_echo_spirit");
            Sprite silhouette = LoadPortraitSprite("portrait_unnamed_silhouette");
            bool dirty = false;
            if (library.echoSpirit != echo)
            {
                library.echoSpirit = echo;
                dirty = true;
            }

            if (library.unnamedSilhouette != silhouette)
            {
                library.unnamedSilhouette = silhouette;
                dirty = true;
            }

            if (dirty)
                EditorUtility.SetDirty(library);
        }

        private static void AssignNamedDefinitionPortraits()
        {
            string[] guids = AssetDatabase.FindAssets("t:NamedPioneerDefinition");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                NamedPioneerDefinition definition = AssetDatabase.LoadAssetAtPath<NamedPioneerDefinition>(path);
                if (definition == null)
                    continue;

                // Skip placeholder junk assets.
                if (definition.displayName == "New Echo")
                    continue;

                Sprite sprite = LoadPortraitSprite(definition.ResolvedId);
                if (sprite == null)
                    continue;

                if (definition.portrait == sprite)
                    continue;

                definition.portrait = sprite;
                EditorUtility.SetDirty(definition);
            }
        }

        private static Sprite LoadPortraitSprite(string resourceFileNameWithoutExtension)
        {
            if (string.IsNullOrWhiteSpace(resourceFileNameWithoutExtension))
                return null;

            string path = $"{ResourcesPortraitsFolder}/{resourceFileNameWithoutExtension}.png";
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static bool IsPortraitTexturePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            string normalized = path.Replace('\\', '/');
            if (!(normalized.StartsWith(ResourcesPortraitsFolder)
                || normalized.StartsWith(ArtPortraitsFolder)))
                return false;

            // Height-derived maps sit next to portraits; do not force Sprite import on them.
            string file = System.IO.Path.GetFileNameWithoutExtension(normalized);
            return !(file.EndsWith("_n")
                || file.EndsWith("_N")
                || file.EndsWith("_normal", System.StringComparison.OrdinalIgnoreCase)
                || file.EndsWith("_Normal"));
        }
    }
}
#endif
