#if UNITY_EDITOR
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Output player/enemy variants must save active at root; Invector templates are stored inactive.
    /// </summary>
    public static class DMHumanoidPrefabSaveUtility
    {
        public static void PrepareOutputPrefabForSave(GameObject root, string visualChildName = "Visual")
        {
            if (root == null)
                return;

            root.SetActive(true);

            string childName = string.IsNullOrWhiteSpace(visualChildName) ? "Visual" : visualChildName;
            Transform visual = root.transform.Find(childName);
            if (visual != null)
                visual.gameObject.SetActive(true);

            Transform stockModel = root.transform.Find("3D Model");
            if (stockModel != null)
                stockModel.gameObject.SetActive(true);
        }
    }
}
#endif
