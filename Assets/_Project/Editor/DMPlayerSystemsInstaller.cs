using Project.Player;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Project.EditorTools
{
    [InitializeOnLoad]
    public static class DMPlayerSystemsInstaller
    {
        static DMPlayerSystemsInstaller()
        {
            EditorApplication.delayCall += EnsureScenePlayer;
        }

        [MenuItem(DarkMatterGenesisEditorMenus.Root + "Player/Add Systems Profile To Player_v7", false, 1)]
        private static void MenuEnsure()
        {
            EnsureScenePlayer();
            EditorUtility.DisplayDialog(
                "Player Systems",
                "DMPlayerSystemsProfile is first on Player_v7 (after Transform). Toggle Climb / Dash / Jetpack / Hero Land there.",
                "OK");
        }

        private static void EnsureScenePlayer()
        {
            GameObject player = GameObject.Find("Player_v7");
            if (player == null)
                return;

            var profile = player.GetComponent<DMPlayerSystemsProfile>();
            if (profile == null)
                profile = Undo.AddComponent<DMPlayerSystemsProfile>(player);

            MoveToTop(profile);
        }

        private static void MoveToTop(Component component)
        {
            if (component == null)
                return;

            // Transform stays first; keep moving up until we sit right under it.
            for (int i = 0; i < 64; i++)
            {
                Component[] all = component.gameObject.GetComponents<Component>();
                int index = System.Array.IndexOf(all, component);
                if (index <= 1)
                    break;
                ComponentUtility.MoveComponentUp(component);
            }

            EditorUtility.SetDirty(component.gameObject);
        }
    }
}
