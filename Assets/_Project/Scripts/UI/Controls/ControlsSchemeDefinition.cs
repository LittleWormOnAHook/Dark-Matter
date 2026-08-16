using System;
using UnityEngine;

namespace Project.UI
{
    [CreateAssetMenu(
        fileName = "ControlsScheme",
        menuName = "Dark Matter: Genesis/UI/Controls Scheme")]
    public class ControlsSchemeDefinition : ScriptableObject
    {
        [SerializeField] private string schemeTitle = "Controls";
        [SerializeField] private ControlsSchemePage[] pages;

        public string SchemeTitle => schemeTitle;
        public ControlsSchemePage[] Pages => pages;
    }

    [Serializable]
    public struct ControlsSchemePage
    {
        [SerializeField] private Sprite image;
        [TextArea(2, 4)]
        [SerializeField] private string caption;

        public Sprite Image => image;
        public string Caption => caption;
    }
}
