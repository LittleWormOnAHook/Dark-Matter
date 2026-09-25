using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Building
{
    [CreateAssetMenu(menuName = "Dark Matter/Building/Piece Library")]
    public sealed class DMBuildingLibrary : ScriptableObject
    {
        public const string ResourcePath = "Building/DM_BuildingLibrary";

        public List<Entry> pieces = new List<Entry>();

        static DMBuildingLibrary live;

        public static GameObject PrefabFor(string id)
        {
            if (live == null)
                live = Resources.Load<DMBuildingLibrary>(ResourcePath);
            if (live == null || live.pieces == null || string.IsNullOrEmpty(id))
                return null;

            for (int i = 0; i < live.pieces.Count; i++)
            {
                if (live.pieces[i] != null && live.pieces[i].id == id)
                    return live.pieces[i].prefab;
            }

            return null;
        }

        [Serializable]
        public sealed class Entry
        {
            public string id;
            public GameObject prefab;
        }
    }
}
