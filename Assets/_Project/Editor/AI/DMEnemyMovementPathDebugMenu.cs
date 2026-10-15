using Project.AI;
using Project.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.AI
{
    public static class DMEnemyMovementPathDebugMenu
    {
        private const string MenuPath = "Tools/Dark Matter Genesis/Combat/Enemy Movement Path Debug (Play)";

        [MenuItem(MenuPath, true)]
        private static bool Validate()
        {
            return Application.isPlaying && Selection.activeGameObject != null
                && Selection.activeGameObject.GetComponentInParent<EnemyAiController>() != null;
        }

        [MenuItem(MenuPath, false, 210)]
        private static void AddToSelection()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Enemy path debug", "Enter Play mode first.", "OK");
                return;
            }

            GameObject go = Selection.activeGameObject;
            EnemyAiController ai = go != null ? go.GetComponentInParent<EnemyAiController>() : null;
            if (ai == null)
            {
                EditorUtility.DisplayDialog("Enemy path debug", "Select an enemy with EnemyAiController.", "OK");
                return;
            }

            if (ai.GetComponent<DMEnemyMovementPathDebug>() == null)
                ai.gameObject.AddComponent<DMEnemyMovementPathDebug>();

            Debug.Log(
                "[EnemyPathDebug] Added DMEnemyMovementPathDebug (draw-only; does not drive AI). "
                + "Solid gold = history when selected or enableMovementPathDebug on brain profile. "
                + "Optional: Genesis → AI & awareness → Draw Virtual Steer Path. Gizmos on in Scene/Game.",
                ai);
        }
    }
}
