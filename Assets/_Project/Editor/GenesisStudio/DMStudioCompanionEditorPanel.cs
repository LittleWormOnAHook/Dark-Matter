#if UNITY_EDITOR
using System;
using Project.EditorTools.Companions;
using UnityEngine;

namespace Project.EditorTools.GenesisStudio
{
    /// <summary>
    /// Embeds the Companion Prefab Tool (chassis, catalog, prefab bake) inside Genesis Studio.
    /// </summary>
    internal sealed class DMStudioCompanionEditorPanel : IDisposable
    {
        private CompanionPrefabToolWindow host;

        public void Draw()
        {
            if (host == null)
            {
                host = ScriptableObject.CreateInstance<CompanionPrefabToolWindow>();
                host.hideFlags = HideFlags.HideAndDontSave;
            }

            host.DrawEmbedded();
        }

        public void Dispose()
        {
            if (host == null)
                return;

            UnityEngine.Object.DestroyImmediate(host);
            host = null;
        }
    }
}
#endif
