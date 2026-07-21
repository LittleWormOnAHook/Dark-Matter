namespace Project.EditorTools
{
    /// <summary>
    /// Canonical asset paths for Dark Matter Genesis. Update here when reorganizing folders.
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
        public const string Settings = Root + "/Settings";
        public const string SettingsInput = Settings + "/Input";
        public const string World = Root + "/World";
        public const string WorldTerrain = World + "/Terrain";
        public const string ConceptualUiArt = Root + "/Conceptual UI art";
        public const string Documentation = Root + "/Documentation";
        public const string DocumentationArchitecture = Documentation + "/Architecture";
        public const string DocumentationAudits = DocumentationArchitecture + "/Audits";
        public const string Features = Root + "/Features";
        public const string FeaturesGameState = Features + "/GameState";
        public const string FeaturesWorldState = Features + "/WorldState";
        public const string FeaturesCommunications = Features + "/Communications";
        public const string FeaturesDirectors = Features + "/Directors";
        public const string FeaturesValidation = Features + "/Validation";
        public const string Textures = Root + "/Textures";
        public const string TexturesUi = Textures + "/UI";
        public const string MiscToolsAndShaders = Root + "/Misc Tools and Shaders";

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
        public const string PrefabsCompanions = Prefabs + "/Companions";
        public const string PrefabsUi = Prefabs + "/UI";
        public const string PrefabsWorld = Prefabs + "/World";

        public const string Resources = Root + "/Resources";
        public const string ResourcesCrafting = Resources + "/Crafting";
        public const string ResourcesQuests = Resources + "/Quests";
        public const string ResourcesUi = Resources + "/UI";
        public const string ResourcesCombat = Resources + "/Combat";
        public const string ResourcesOptics = Resources + "/Optics";

        public const string Scenes = Root + "/Scenes";
        public const string MainScene = Scenes + "/Pioneer.unity";
        public const string UiPreviewScene = Scenes + "/UI_Preview.unity";
        public const string UiLayoutProfiles = Data + "/UI/LayoutProfiles";
        public const string InputActions = SettingsInput + "/InputSystem_Actions.inputactions";
        public const string Scripts = Root + "/Scripts";
        public const string ScriptsPrototypes = Scripts + "/Prototypes";
        public const string ScriptsAi = Scripts + "/AI";
        public const string Editor = Root + "/Editor";
        public const string EditorDevTools = Editor + "/DevTools";

        public const string PlayerInvectorPrefab = PrefabsPlayers + "/Player_Invector.prefab";
        public const string PioneerCompanionInvectorPrefab = PrefabsCompanions + "/PioneerCompanion_Invector.prefab";
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
