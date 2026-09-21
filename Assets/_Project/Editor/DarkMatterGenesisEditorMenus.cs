namespace Project.EditorTools
{
    /// <summary>
    /// Shared Unity menu paths for Dark Matter Genesis editor utilities.
    /// </summary>
    public static class DarkMatterGenesisEditorMenus
    {
        public const string Root = "Tools/Dark Matter Genesis/";
        public const string ToolsWindow = Root + "Tools Window";
        public const string KeepProfilesAfterPlay = Root + "Keep Profiles After Play";
        public const string Climb = Root + "Climb/";
        public const string Jetpack = Root + "Jetpack/";
        public const string JetpackPresets = Jetpack + "Presets/";
        public const string Player = Root + "Player/";
        public const string Companions = Root + "Companions/";
        public const string Items = Root + "Items/";
        public const string ApplyDefaultTradeValues = Items + "Apply Default Trade Values";
        public const string EnsureVendorContent = Items + "Ensure Vendor Content";
        public const string World = Root + "World/";
        public const string EnsureIoClockProfile = World + "Ensure Io Clock Profile";
        public const string WorldGaia = World + "Gaia/";
        public const string SplineCreatorWindowPrimary = Root + "Spline Creator Window";
        public const string SplineCreatorWindow = World + "Spline Creator Window";
        public const string CreateElectricalLineSpline = World + "Create Electrical Line Spline";
        public const string CreateObjectPlacerSpline = World + "Create Object Placer Spline";
        public const string CreateScatterPlacer = World + "Create Scatter Placer";
        public const string Scanner = Root + "Scanner/";
        public const string Debug = Root + "Debug/";
        public const string Diagnostics = Root + "Diagnostics/";

        public const string PrefabCreator = Root + "Prefab Creator/";

        // Item, equipment, and pickup prefabs
        /// <summary>Legacy redirect ? opens Blueprint and Crafting Manager (Item Data tab).</summary>
        public const string ItemDataCreator = PrefabCreator + "Item Data Creator";
        /// <summary>Legacy redirect ? opens Blueprint and Crafting Manager (Crafting Item tab).</summary>
        public const string CraftingItemCreator = PrefabCreator + "Crafting Item Creator";
        public const string GrenadeItemCreator = PrefabCreator + "Grenade / Throwable Item Creator";
        public const string ResourceManager = PrefabCreator + "Resource Manager";
        /// <summary>Legacy alias ? prefer <see cref="ResourceManager"/>.</summary>
        public const string ResourceItemCreator = ResourceManager;
        public const string EquipmentItemCreator = PrefabCreator + "Equipment Item Creator";
        public const string EquipmentItemCreatorFromSelection = PrefabCreator + "Equipment Item Creator From Selection";

        // Combat prefabs
        public const string WeaponPrefabCreator = PrefabCreator + "Weapon Prefab Creator";
        public const string WeaponPrefabCreatorFromSelection = PrefabCreator + "Weapon Prefab Creator From Selection";
        /// <summary>Legacy redirect — opens Blueprint and Crafting Manager (Ammo tab).</summary>
        public const string ProjectileAmmoCreator = PrefabCreator + "Projectile + Ammo Creator";
        public const string EnemyPrefabCreator = PrefabCreator + "Enemy Prefab Creator";
        public const string PlayerPrefabCreator = PrefabCreator + "Player Prefab Creator";
        public const string CreatureManager = Root + "Creatures/Creature Manager";
        public const string LegacyCreatures = Root + "Creatures/Legacy/";
        public const string BuildSulfurHoundCreature = LegacyCreatures + "Build Sulfur Hound Prefab (Malbers OnWolf)";
        public const string BuildSulfurHoundV2Creature = LegacyCreatures + "Build Sulfur Hound V2 Rigged Prefab";
        public const string BuildSulfurHoundBrain = LegacyCreatures + "Build Sulfur Hound Brain Graph";
        public const string RegisterSulfurHoundEncounter = LegacyCreatures + "Register Sulfur Hound In B1 Encounter Table";
        public const string ValidateSulfurHoundSetup = LegacyCreatures + "Validate Sulfur Hound NavMesh + Collider";
        public const string RebuildSulfurHoundReskin = LegacyCreatures + "Rebuild Sulfur Hound (OnWolf / Houndv3)";
        public const string TwoHandedWeaponFromScene = PrefabCreator + "Two-Handed Weapon From Scene";

        // Character, companion, and pet prefabs
        public const string Pets = Root + "Pets/";
        public const string PetManager = Pets + "Pet Manager";
        public const string PetManagerFromSelection = Pets + "Pet Manager From Selection";
        /// <summary>Legacy alias ? prefer <see cref="PetManager"/>.</summary>
        public const string PetPrefabCreator = PetManager;
        /// <summary>Legacy alias ? prefer <see cref="PetManagerFromSelection"/>.</summary>
        public const string PetPrefabCreatorFromSelection = PetManagerFromSelection;
        public const string PetPrefabFoxCubDemo = Pets + "Pet Prefab (Fox Cub Demo)";
        public const string PioneerCompanionInvectorPrefab = Combat + "Build PioneerCompanion_Invector Prefab";
        public const string CompanionPrefabTool = Root + "Companion Prefab Tool";

        // UI prefabs
        public const string InventorySlotPrefab = PrefabCreator + "Inventory Slot Prefab";
        public const string ExposureZonePrefabCreator = PrefabCreator + "Exposure Zone Creator";
        /// <summary>Legacy redirect ? opens Blueprint and Crafting Manager (Pickup Prefabs tab).</summary>
        public const string RecipePrefabCreator = PrefabCreator + "Blueprint Prefab Creator";
        public const string CameraShakeEmitterCreator = PrefabCreator + "Camera Shake Emitter Creator";
        public const string CameraShakeEmitterCreateAllPresets = PrefabCreator + "Create All Camera Shake Emitter Prefabs";

        public const string Project = Root + "Project/";
        public const string Content = Root + "Content/";
        public const string Profiles = Root + "Profiles/";
        /// <summary>Unified profile studio (player, world, combat, crafting, UI, audio).</summary>
        public const string GenesisStudio = Profiles + "Genesis Studio";
        public const string Crafting = Root + "Crafting/";
        /// <summary>Primary crafting/blueprint editor entry point.</summary>
        public const string BlueprintCraftingManager = Crafting + "Blueprint and Crafting Manager";
        public const string Quests = Root + "Quests/";
        public const string Combat = Root + "Combat/";
        public const string CombatAnimations = Combat + "Animations/";
        public const string RebuildEnemyControllerFromShooterMelee = CombatAnimations + "Rebuild Selected Controller from ShooterMelee Base";
        public const string RepairAllHumanoidCombatPrefabs = Combat + "Repair All Humanoid Combat Prefabs";
        public const string RepairCompanionEchoHumanoidAnims =
            Combat + "Repair Companion And Echo Humanoid Anims (Player_v7)";
        public const string AddWeaponHitboxToSelectedPrefab = Combat + "Add Weapon Hitbox To Selected Prefab";
        public const string RefreshAllWeaponHitboxes = Combat + "Refresh All Weapon Hitboxes";
        public const string Equipment = Root + "Equipment/";
        public const string InvectorWeaponGrip = Equipment + "Invector Weapon Grip/";
        public const string BakeInvectorDrawnGrip = InvectorWeaponGrip + "Bake Drawn Grip (Live Player)";
        public const string BakeInvectorHolsteredGrip = InvectorWeaponGrip + "Bake Holstered Grip (Live Player)";
        public const string PreviewInvectorHolsteredWeapon = InvectorWeaponGrip + "Preview Holstered On Player";
        public const string EndInvectorHolsterPreview = InvectorWeaponGrip + "End Holster Preview";
        public const string ResetInvectorWeaponGrips = InvectorWeaponGrip + "Reset Grips On Selected Item";
        public const string OpenInvectorWeaponGripWindow = InvectorWeaponGrip + "Grip Bake Window";
        public const string Ui = Root + "UI/";
        public const string UiGameIcons = Ui + "Game Icons/";
        public const string UiHotCross = Ui + "Hot Cross/";
        public const string Scene = Root + "Scene/";
        public const string Ppt = Root + "PPT/";
        public const string PlayableScene = "Assets/_Project/Scenes/Dark Matter Genesis v1.6.3.1.unity";
        public const string RefreshAllMapMarkers = Scene + "Refresh All Map Markers";
        public const string PlaceExposureStarterKit = Scene + "Place Exposure Starter Kit (Open Scene)";
        public const string PlaceExposureStarterKitInPioneer = Scene + "Place Exposure Starter Kit In Playable Scene";
        public const string Audio = Root + "Audio/";
        public const string Optics = Root + "Optics/";
        public const string Maintenance = Root + "Maintenance/";
        public const string TextureStreaming = Root + "Texture Streaming/";
        public const string TextureStreamingPreview = TextureStreaming + "Preview Gameplay Streaming";
        public const string TextureStreamingApply = TextureStreaming + "Apply Gameplay Streaming";
        public const string Hdrp = Root + "HDRP/";
        public const string Hierarchy = Root + "Hierarchy/";
        public const string HierarchySortChildren = Hierarchy + "Sort Children/";
        public const string PlayModeSaver = Root + "Play Mode Saver/";
        public const string PlayModeSaverWindow = PlayModeSaver + "Open Window";
        public const string PlayModeSaverSaveNow = PlayModeSaver + "Save Now %#s";
        public const string PlayModeSaverSaveAndExit = PlayModeSaver + "Save And Exit Play Mode %#&s";

        public const string ClimbProbeBaker = Climb + "Probe Baker";
        public const string SyncGaiaTerrainNavMesh = WorldGaia + "Sync NavMesh Surfaces To Gaia Terrains";
        public const string BindGaiaTerrainScenes = WorldGaia + "Bind DM Genesis Terrain Scenes";
        public const string BindPlayerTerrainLoader = WorldGaia + "Bind Player_v7 Terrain Loader";
        public const string RemoveStamperTerrainLayers = WorldGaia + "Remove Stamper Terrain Layers";
        public const string RebuildSplatmapsFromHeight = WorldGaia + "Rebuild Splatmaps From Height (Like Gaia)";
        public const string StripTreesFromAllTerrains = WorldGaia + "Strip Trees From All Terrains";
        public const string SetPixelError25OnAllTerrains = WorldGaia + "Set Pixel Error 25 On All Terrains";
        public const string StitchCenterTileSplatEdges = WorldGaia + "Stitch Center Tile Splat Edges";
        public const string CreateTerrainContentScenes = WorldGaia + "Create Terrain Content Scenes";
        public const string EditorFourTerrainsAndImpostors = WorldGaia + "Editor: 4 Terrains + Impostors";
        public const string BakeTlmImpostors = WorldGaia + "Bake TLM Impostors";
        public const string BakeBorderFence = World + "Bake Border Fence";
        public const string WireSelectedSlidingDoor = World + "Wire Selected Sliding Door";
        public const string SetupWalkerDrillInScene = World + "Setup Walker Drill In Scene";
        public const string BuildWalkerDrillAnimatorController = World + "Build Walker Drill Animator Controller";
        public const string SaveWalkerDrillPrefab = World + "Save Walker Drill Prefab";
        public const string BuildLaserBurnMarkPrefab = Combat + "Build Laser Burn Mark Prefab";
        public const string AuditConsole = Diagnostics + "Audit Console";
        public const string AuditResourcesPaths = Diagnostics + "Audit Resources Paths";
        public const string DebugToggleSulfurCrisisHud = Debug + "Toggle Sulfur Crisis HUD";
        public const string DebugShowEchoRescueReveal = Debug + "Show Echo Rescue Reveal (Test)";
        public const string DebugSpawnTestEchoSignal = Debug + "Spawn Test Echo Signal";
        public const string DebugRefreshExpeditionTrio = Debug + "Refresh Expedition Trio Companions";
        public const string ImportGameIconsAsSprites = UiGameIcons + "Import as Sprites";
        public const string RewireItemAndRecipeIcons = UiGameIcons + "Rewire Item + Recipe Icons";
        public const string ImportHotCrossIconsAsSprites = UiHotCross + "Import Cutout Icons as Sprites";
        public const string RebuildHotCrossIconRegistry = UiHotCross + "Rebuild Icon Registry";
        public const string ReimportCompanionPortraitTextures = Companions + "Reimport Portrait Textures";
        public const string AssignPioneerPortraits = Companions + "Assign Pioneer Portraits";
        public const string CreateDefaultScannerHighlightProfile = Scanner + "Create Default Highlight Profile";
        public const string RemoveInvectorHiveInstances = Scene + "Remove Invector Hive Instances";
    }
}
