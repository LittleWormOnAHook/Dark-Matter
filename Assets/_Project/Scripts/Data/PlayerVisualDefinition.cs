using UnityEngine;

namespace Project.Data
{
    /// <summary>
    /// Setup asset for the Player Prefab Creator — clones Meshy/custom humanoid visuals
    /// onto a new player prefab variant. Player_Invector is the template source only;
    /// Create/Rebuild writes to prefabFileName under Prefabs/Players.
    /// </summary>
    [CreateAssetMenu(
        fileName = "PlayerVisualDefinition",
        menuName = "Dark Matter Genesis/Player Visual Definition")]
    public class PlayerVisualDefinition : ScriptableObject
    {
        [Tooltip("Display label in the Player Prefab Creator list.")]
        public string displayName = "Player";

        [Tooltip("Output prefab file name under Assets/_Project/Prefabs/Players (e.g. Player_MeshyAndroid). " +
                 "Must not be Player_Invector — that prefab is the protected template.")]
        public string prefabFileName = "Player_Custom";

        [Tooltip("Optional override of the clone template. Leave empty to use Player_Invector.prefab.")]
        public GameObject templatePrefab;

        [Tooltip("Created/rebuilt output prefab (filled by the creator). Not the template.")]
        public GameObject playerPrefab;

        [Tooltip("Child name used when nesting a Meshy/custom model under the player root.")]
        public string visualChildName = "Visual";

        [Tooltip("Last Model FBX / prefab applied (reference only; re-assign in the creator to apply).")]
        public GameObject lastModelSource;

        [TextArea(2, 4)]
        public string notes;
    }
}
