namespace Project.EditorTools
{
    /// <summary>
    /// Canonical asset paths for Survival Pioneer. Update here when reorganizing folders.
    /// </summary>
    public static class ProjectAssetPaths
    {
        public const string Root = "Assets/_Project";

        public const string Animations = Root + "/Animations";
        public const string AnimationsEnemies = Animations + "/Enemies";
        public const string AnimationsNpcs = Animations + "/NPCs";
        public const string Art = Root + "/Art";
        public const string ArtIcons = Art + "/Icons";
        public const string ArtTextures = Art + "/Textures";
        public const string Audio = Root + "/Audio";
        public const string Materials = Root + "/Materials";
        public const string Shaders = Root + "/Shaders";

        public const string Data = Root + "/Data";
        public const string ItemsData = Data + "/Items";
        public const string EnemiesData = Data + "/Enemies";
        public const string RecipesData = Data + "/Crafting/Recipes";

        public const string Prefabs = Root + "/Prefabs";
        public const string PrefabsCombat = Prefabs + "/Combat";
        public const string PrefabsCrafting = Prefabs + "/Crafting";
        public const string PrefabsCraftingStations = PrefabsCrafting + "/Stations";
        public const string PrefabsItems = Prefabs + "/Items";
        public const string PrefabsItemsHeld = PrefabsItems + "/Held";
        public const string PrefabsItemsWorld = PrefabsItems + "/World";
        public const string PrefabsNpcs = Prefabs + "/NPCs";
        public const string PrefabsPlayers = Prefabs + "/Players";
        public const string PrefabsUi = Prefabs + "/UI";
        public const string PrefabsWorld = Prefabs + "/World";

        public const string Resources = Root + "/Resources";
        public const string ResourcesCrafting = Resources + "/Crafting";
        public const string ResourcesQuests = Resources + "/Quests";
        public const string ResourcesUi = Resources + "/UI";
        public const string ResourcesCombat = Resources + "/Combat";
        public const string ResourcesOptics = Resources + "/Optics";

        public const string Scenes = Root + "/Scenes";
        public const string Scripts = Root + "/Scripts";
        public const string ScriptsAi = Scripts + "/AI";
        public const string Editor = Root + "/Editor";

        public const string PlayerPrefab = PrefabsPlayers + "/Player.prefab";
        public const string QuestGiverPrefab = PrefabsNpcs + "/QuestGiver_PioneerGuide.prefab";
        public const string EnemyPrefab = PrefabsCombat + "/Enemy.prefab";
        public const string InventorySlotPrefab = PrefabsUi + "/InventorySlot.prefab";
        public const string InventorySlotResourcesPrefab = ResourcesUi + "/InventorySlot.prefab";
        public const string RecipeRegistry = ResourcesCrafting + "/RecipeRegistry.asset";
        public const string QuestRegistry = ResourcesQuests + "/QuestRegistry.asset";
        public const string ItemRegistry = Resources + "/ItemRegistry.asset";
        public const string ReflectionProbePrefab = PrefabsWorld + "/ReflectionProbe_Outdoor.prefab";
    }
}
