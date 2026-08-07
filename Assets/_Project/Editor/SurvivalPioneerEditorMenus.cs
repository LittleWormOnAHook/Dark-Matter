namespace Project.EditorTools
{
    /// <summary>
    /// Shared Unity menu paths for Dark Matter Genesis editor utilities.
    /// </summary>
    public static class SurvivalPioneerEditorMenus
    {
        public const string Root = "Tools/Dark Matter Genesis/";
        public const string ToolsWindow = Root + "Tools Window";

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
        public const string Crafting = Root + "Crafting/";
        /// <summary>Primary crafting/blueprint editor entry point.</summary>
        public const string BlueprintCraftingManager = Crafting + "Blueprint and Crafting Manager";
        public const string Quests = Root + "Quests/";
        public const string Combat = Root + "Combat/";
        public const string CombatAnimations = Combat + "Animations/";
        public const string RebuildEnemyControllerFromShooterMelee = CombatAnimations + "Rebuild Selected Controller from ShooterMelee Base";
        public const string RepairAllHumanoidCombatPrefabs = Combat + "Repair All Humanoid Combat Prefabs";
        public const string AuditAndroidEnemyChecklist = Combat + "Audit Android Enemy Checklist";
        public const string AuditSelectedAndroidEnemyChecklist = Combat + "Audit Selected Android Enemy Checklist";
        public const string RepairSelectedAndroidEnemyChecklist = Combat + "Repair Selected Android Enemy Checklist";
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
        public const string Scene = Root + "Scene/";
        public const string PlaceExposureStarterKit = Scene + "Place Exposure Starter Kit (Open Scene)";
        public const string PlaceExposureStarterKitInPioneer = Scene + "Place Exposure Starter Kit In Pioneer.unity";
        public const string Audio = Root + "Audio/";
        public const string Optics = Root + "Optics/";
        public const string Maintenance = Root + "Maintenance/";
        public const string PlayModeSaver = Root + "Play Mode Saver/";
        public const string PlayModeSaverWindow = PlayModeSaver + "Open Window";
        public const string PlayModeSaverSaveNow = PlayModeSaver + "Save Now %#s";
        public const string PlayModeSaverSaveAndExit = PlayModeSaver + "Save And Exit Play Mode %#&s";
    }
}
