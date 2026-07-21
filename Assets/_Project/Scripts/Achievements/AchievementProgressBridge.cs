using Project.AI;
using Project.Core;
using Project.Crafting;
using Project.Pet;
using Project.Pioneers;
using Project.Progression;
using Project.UI;
using UnityEngine;

namespace Project.Achievements
{
    public class AchievementProgressBridge : MonoBehaviour
    {
        public static AchievementProgressBridge EnsureExists()
        {
            AchievementProgressBridge existing = FindAnyObjectByType<AchievementProgressBridge>();
            if (existing != null)
                return existing;

            UIManager ui = FindAnyObjectByType<UIManager>();
            if (ui != null)
                return ui.gameObject.AddComponent<AchievementProgressBridge>();

            GameObject player = PlayerLocator.FindPlayerObject();
            if (player != null)
                return player.AddComponent<AchievementProgressBridge>();

            GameObject host = new GameObject("AchievementProgressBridge");
            return host.AddComponent<AchievementProgressBridge>();
        }

        private void Start()
        {
            AchievementManager.EnsureExists();
            BindCrafting();
            BindPets();
            BindProgression();
            BindPioneers();
            BindEnemies();
        }

        private void OnDestroy()
        {
            UnbindCrafting();
            UnbindPets();
            UnbindProgression();
            UnbindPioneers();
            UnbindEnemies();
        }

        private void BindCrafting()
        {
            CraftingManager crafting = CraftingManager.Instance ?? FindAnyObjectByType<CraftingManager>();
            if (crafting != null)
                crafting.OnCrafted += HandleCrafted;
        }

        private void UnbindCrafting()
        {
            CraftingManager crafting = CraftingManager.Instance ?? FindAnyObjectByType<CraftingManager>();
            if (crafting != null)
                crafting.OnCrafted -= HandleCrafted;
        }

        private void HandleCrafted(RecipeDefinition recipe)
        {
            if (recipe == null)
                return;

            AchievementManager manager = AchievementManager.EnsureExists();
            manager?.ReportProgress(AchievementTriggerType.CraftItem, recipe.ResolvedId);
            manager?.ReportProgress(AchievementTriggerType.CraftItem, null);
        }

        private void BindPets()
        {
            PetManager manager = PetManager.EnsureExists();
            if (manager != null)
                manager.OnPetAdopted += HandlePetAdopted;
        }

        private void UnbindPets()
        {
            if (PetManager.Instance != null)
                PetManager.Instance.OnPetAdopted -= HandlePetAdopted;
        }

        private void HandlePetAdopted(PetController pet, bool wasTamed)
        {
            if (pet == null)
                return;

            AchievementManager manager = AchievementManager.EnsureExists();
            if (manager == null)
                return;

            if (wasTamed)
                manager.ReportProgress(AchievementTriggerType.TamePet, pet.PetId);

            manager.ReportProgress(AchievementTriggerType.AdoptPet, pet.PetId);
            manager.ReportProgress(AchievementTriggerType.AdoptPet, null);
        }

        private void BindProgression()
        {
            PlayerProgressionManager progression = PlayerProgressionManager.EnsureExists();
            if (progression != null)
                progression.OnLevelUp += HandleLevelUp;
        }

        private void UnbindProgression()
        {
            if (PlayerProgressionManager.Instance != null)
                PlayerProgressionManager.Instance.OnLevelUp -= HandleLevelUp;
        }

        private void HandleLevelUp(int level, int levelsGained)
        {
            AchievementManager manager = AchievementManager.EnsureExists();
            if (manager == null)
                return;

            for (int i = 0; i < levelsGained; i++)
            {
                int reportedLevel = level - levelsGained + i + 1;
                manager.ReportProgress(AchievementTriggerType.ReachLevel, reportedLevel.ToString(), 1);
                manager.ReportProgress(AchievementTriggerType.ReachLevel, null, 1);
            }

            if (level % 5 == 0)
                DynamicAchievementGenerator.RefreshIfNeeded(level);
        }

        private void BindPioneers()
        {
            PioneerRosterManager roster = PioneerRosterManager.EnsureExists();
            if (roster != null)
                roster.OnTrioChanged += HandleTrioChanged;
        }

        private void UnbindPioneers()
        {
            if (PioneerRosterManager.Instance != null)
                PioneerRosterManager.Instance.OnTrioChanged -= HandleTrioChanged;
        }

        private void HandleTrioChanged()
        {
            PioneerRosterManager roster = PioneerRosterManager.EnsureExists();
            AchievementManager manager = AchievementManager.EnsureExists();
            if (roster == null || manager == null)
                return;

            int assigned = 0;
            for (int i = 0; i < roster.ExpeditionTrioIds.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(roster.ExpeditionTrioIds[i]))
                    assigned++;
            }

            manager.ReportProgress(AchievementTriggerType.AssignTrio, null, assigned, setAbsolute: true);
        }

        private void BindEnemies()
        {
            AchievementEnemyKillRelay.EnsureExists();
        }

        private void UnbindEnemies()
        {
        }
    }
}
