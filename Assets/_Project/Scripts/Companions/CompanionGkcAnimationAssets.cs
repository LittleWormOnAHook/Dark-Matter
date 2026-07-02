using Project.Player;
using UnityEngine;

namespace Project.Companions
{
    [CreateAssetMenu(menuName = "Project/Companions/GKC Animation Assets", fileName = "CompanionGkcAnimationAssets")]
    public class CompanionGkcAnimationAssets : ScriptableObject
    {
        public RuntimeAnimatorController animatorController;
        public GkcActionCatalog actionCatalog;
    }
}
