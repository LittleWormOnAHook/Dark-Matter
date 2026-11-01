using System;
using UnityEngine;

namespace Project.World.Sky
{
    /// <summary>31g: scene name -> Io sky profile. Lives in Resources/World/DM_SceneSkyProfiles.asset (no scene edits needed).
    /// Unmapped scenes use Resources/World/DM_IoSkyProfile.</summary>
    [CreateAssetMenu(menuName = "Dark Matter/World/Scene Sky Profile Map", fileName = "DM_SceneSkyProfiles")]
    public sealed class DMSceneSkyProfileMap : ScriptableObject
    {
        [Serializable] public struct Entry { public string sceneName; public DM_IoSkyProfile profile; }
        public Entry[] entries = new Entry[0];
        public DM_IoSkyProfile Find(string sceneName)
        {
            if (entries == null || string.IsNullOrEmpty(sceneName)) return null;
            foreach (var e in entries) if (e.profile != null && string.Equals(e.sceneName, sceneName, StringComparison.OrdinalIgnoreCase)) return e.profile;
            return null;
        }
    }
}
