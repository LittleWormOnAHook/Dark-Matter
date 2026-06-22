using Project.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    internal sealed class UiButtonSoundMarker : MonoBehaviour
    {
    }

    public static class UiSoundHelper
    {
        public static void BindButton(Button button)
        {
            if (button == null || button.GetComponent<UiButtonSoundMarker>() != null)
                return;

            button.gameObject.AddComponent<UiButtonSoundMarker>();
            button.onClick.AddListener(() => GameAudioManager.Instance?.PlayButtonClick());
        }

        public static void BindButtonsInHierarchy(Transform root)
        {
            if (root == null)
                return;

            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
                BindButton(buttons[i]);
        }
    }
}
