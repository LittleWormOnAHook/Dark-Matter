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
        public const string MaterialsCreatures = Materials + "/Creatures";
        public const string Meshes = Root + "/Meshes";
        public const string MeshesCreatures = Meshes + "/Creatures";
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
        public const string ItemsMelee = ItemsData + "/Melee";
        public const string ItemsRanged = ItemsData + "/Ranged";
        public const string ItemsAmmo = ItemsData + "/Ammo";
        public const string ItemsResources = ItemsData + "/Resources";
        /// <summary>Laser-mined ore / mineral yield ItemData.</summary>
        public const string ItemsResourcesMining = ItemsResources + "/Mining";
        /// <summary>Hold-E plant harvest yield ItemData.</summary>
        public const string ItemsResourcesHarvest = ItemsResources + "/Harvest";
        /// <summary>Salvage / craft components (scrap). Not mined from ResourceNodes.</summary>
        public const string ItemsComponents = ItemsData + "/Components";
        /// <summary>Building / inventory modules (e.g. storage row unlock).</summary>
        public const string ItemsModules = ItemsData + "/Modules";
        /// <summary>Operational fuels and similar non-harvest resources (Plasma Fuel).</summary>
        public const string ItemsOperations = ItemsData + "/Operations";
        public const string ItemsTools = ItemsData + "/Tools";
        public const string ItemsConsumables = ItemsData + "/Consumables";
        /// <summary>Throwable consumables (grenades). Keep itemType Consumable; combat lives on throw prefabs.</summary>
        public const string ItemsThrowables = ItemsData + "/Throwables";
        public const string ItemsVehicles = ItemsData + "/Vehicles";
        /// <summary>
        /// ResourceNodeDefinition ScriptableObjects. Runtime nodes live as prefabs under PrefabsWorldResources.
        /// </summary>
        public const string ItemsNodes = ItemsData + "/Nodes";
        public const string EnemiesData = Data + "/Enemies";
        public const string PlayersData = Data + "/Players";
        public const string CreaturesData = Data + "/Creatures";
        public const string CreaturesBrainData = CreaturesData + "/Brain";
        public const string CreaturesBrainTasks = CreaturesBrainData + "/Tasks";
        public const string CreaturesBrainDecisions = CreaturesBrainData + "/Decisions";
        public const string EncountersData = Data + "/Encounters";
        /// <summary>Blueprint ScriptableObject folder (formerly Recipes).</summary>
        public const string BlueprintsData = Data + "/Crafting/Blueprints";
        /// <summary>Legacy alias for <see cref="BlueprintsData"/>.</summary>
        public const string RecipesData = BlueprintsData;
        public const string BlueprintsWeapons = BlueprintsData + "/Weapons";
        public const string BlueprintsConsumables = BlueprintsData + "/Consumables";
        public const string BlueprintsAmmo = BlueprintsData + "/Ammo";
        public const string BlueprintsResources = BlueprintsData + "/Resources";
        public const string BlueprintsModules = BlueprintsData + "/Modules";
        public const string RecipesWeapons = BlueprintsWeapons;
        public const string RecipesConsumables = BlueprintsConsumables;
        public const string RecipesAmmo = BlueprintsAmmo;
        public const string RecipesResources = BlueprintsResources;
        public const string RecipesModules = BlueprintsModules;

        public const string Prefabs = Root + "/Prefabs";
        public const string PrefabsBuildings = Prefabs + "/Buildings";
        public const string PrefabsCombat = Prefabs + "/Combat";
        public const string PrefabsCombatEnemies = PrefabsCombat + "/Enemies";
        public const string PrefabsCreatures = Prefabs + "/Creatures";
        public const string PrefabsParticles = Prefabs + "/Particles";
        public const string PrefabsCombatProjectiles = PrefabsCombat + "/Projectiles";
        public const string PrefabsCombatVfx = PrefabsCombat + "/VFX";
        public const string PrefabsCrafting = Prefabs + "/Crafting";
        public const string PrefabsCraftingStations = PrefabsCrafting + "/Stations";
        public const string PrefabsEnvironment = Prefabs + "/Environment";
        public const string PrefabsEnvironmentCameraShake = PrefabsEnvironment + "/CameraShake";
        public const string PrefabsEnvironmentExposure = PrefabsEnvironment + "/Exposure";
        public const string PrefabsItems = Prefabs + "/Items";
        public const string PrefabsItemsHeld = PrefabsItems + "/Held";
        public const string PrefabsItemsWorld = PrefabsItems + "/World";
        public const string PrefabsItemsAmmo = PrefabsItems + "/Ammo";
        public const string PrefabsWeapons = Prefabs + "/Weapons";
        public const string PrefabsWeaponsMelee = PrefabsWeapons + "/Melee";
        public const string PrefabsWeaponsRanged = PrefabsWeapons + "/Ranged";
        public const string PrefabsTools = Prefabs + "/Tools";
        public const string PrefabsNpcs = Prefabs + "/NPCs";
        public const string PrefabsPets = Prefabs + "/Pets";
        public const string PrefabsPlayers = Prefabs + "/Players";
        public const string PrefabsCompanions = Prefabs + "/Companions";
        public const string PrefabsUi = Prefabs + "/UI";
        public const string PrefabsVehicles = Prefabs + "/Vehicles";
        public const string PrefabsWorld = Prefabs + "/World";
        /// <summary>World ResourceNode prefabs (boulders, plants). Data stubs: ItemsNodes.</summary>
        public const string PrefabsWorldResources = PrefabsWorld + "/Resources";
        /// <summary>Artist / Meshy source kits. Do not relocate casually.</summary>
        public const string PrefabsModels = Prefabs + "/Models";

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
        public const string EnemyPrefab = PrefabsCombatEnemies + "/Enemy.prefab";
        public const string HumanoidEnemyPrefab = PrefabsCombatEnemies + "/HumanoidEnemy_Invector.prefab";
        public const string GongoPrefab = PrefabsCombatEnemies + "/Gongo.prefab";
        public const string SparksLongPrefab = PrefabsCombatVfx + "/SparksLong.prefab";
        /// <summary>Default one-shot when mine/harvest loot arrives at the player.</summary>
        public const string LootCompleteVfxPrefab = PrefabsParticles + "/Flash Effect 1.prefab";
        public const string AudioPickUp = "Assets/Audio/Pickups/pickUp.wav";
        public const string AudioBreakStone = "Assets/Audio/Others/Break Stone.wav";
        public const string AudioBreakWood = "Assets/Audio/Others/Break Wood Effect.wav";
        public const string InventorySlotPrefab = PrefabsUi + "/InventorySlot.prefab";
        public const string InventorySlotResourcesPrefab = ResourcesUi + "/InventorySlot.prefab";
        public const string BlueprintRegistry = ResourcesCrafting + "/BlueprintRegistry.asset";
        /// <summary>Legacy alias — same asset path as <see cref="BlueprintRegistry"/> after rename.</summary>
        public const string RecipeRegistry = BlueprintRegistry;
        public const string QuestRegistry = ResourcesQuests + "/QuestRegistry.asset";
        public const string ItemRegistry = Resources + "/ItemRegistry.asset";
        public const string ReflectionProbePrefab = PrefabsWorld + "/ReflectionProbe_Outdoor.prefab";
        public const string BoulderNodeTemplate =
            PrefabsEnvironment + "/Nodes Minerals and Plants/Minerial Node Boulder Variant.prefab";
        public const string SulfurNeedleTuftGlb =
            PrefabsEnvironment + "/PlantLife/Needle Plant/Sulfur Needle Tuft.glb";
        public const string BrimstoneFanPlantPrefab =
            PrefabsEnvironment + "/PlantLife/brimstone_fan_plant.fbx/Brimestome Fan Plant.prefab";
        public const string MiningToolItem =
            ItemsRanged + "/DM_Mining_Tool.asset";
    }
}
