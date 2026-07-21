using UnityEngine;

namespace Project.Progression
{
    public class BuildingXpTrigger : MonoBehaviour
    {
        [SerializeField] private string buildingId = "building_default";
        [SerializeField] private int xpAmount = ProgressionXpDefaults.DiscoveryXp;

        public void GrantOnce()
        {
            ProgressionRewardGranter.GrantXp(xpAmount, XpSource.Building, $"building:{buildingId}");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            GrantOnce();
        }
    }

    public static class BuildingProgressionXp
    {
        public static void GrantBuildingXp(string buildingId, int xpAmount = 30)
        {
            ProgressionRewardGranter.GrantXp(xpAmount, XpSource.Building, $"building:{buildingId}");
        }
    }
}
