using UnityEngine;

namespace Project.Pioneers
{
    /// <summary>
    /// Shared ID-badge portraits: Echo spirit form and unnamed black silhouette.
    /// Named companions use <see cref="NamedPioneerDefinition.portrait"/> (or Resources fallback).
    /// </summary>
    [CreateAssetMenu(
        fileName = "PioneerPortraitLibrary",
        menuName = "Dark Matter Genesis/Companions/Pioneer Portrait Library")]
    public class PioneerPortraitLibrary : ScriptableObject
    {
        [Tooltip("Shared Neural Echo spirit ID — black hooded tattered cloak; darker translucent purple spirit highlights in eye sockets / cloak gaps, mostly shadowed.")]
        public Sprite echoSpirit;

        [Tooltip("Fallback shared ID for unnamed recruits — gold-ring black silhouette. Prefer JrPioneerSilhouetteCatalog variants.")]
        public Sprite unnamedSilhouette;
    }
}
