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
        public const string ItemDataCreator = PrefabCreator + "Item Data Creator";
        public const string CraftingItemCreator = PrefabCreator + "Crafting Item Creator";
        public const string ResourceItemCreator = PrefabCreator + "Resource Item Creator";
        public const string EquipmentItemCreator = PrefabCreator + "Equipment Item Creator";
        public const string EquipmentItemCreatorFromSelection = PrefabCreator + "Equipment Item Creator From Selection";

        // Combat prefabs
        public const string WeaponPrefabCreator = PrefabCreator + "Weapon Prefab Creator";
        public const string WeaponPrefabCreatorFromSelection = PrefabCreator + "Weapon Prefab Creator From Selection";
        public const string ProjectileAmmoCreator = PrefabCreator + "Projectile + Ammo Creator";
        public const string EnemyPrefabCreator = PrefabCreator + "Enemy Prefab Creator";
        public const string TwoHandedWeaponFromScene = PrefabCreator + "Two-Handed Weapon From Scene";

        // Character, companion, and pet prefabs
        public const string PetPrefabCreator = PrefabCreator + "Pet Prefab Creator";
        public const string PetPrefabCreatorFromSelection = PrefabCreator + "Pet Prefab Creator From Selection";
        public const string PetPrefabFoxCubDemo = PrefabCreator + "Pet Prefab (Fox Cub Demo)";
        public const string PioneerCompanionInvectorPrefab = Combat + "Build PioneerCompanion_Invector Prefab";
        public const string CompanionPrefabTool = Root + "Companion Prefab Tool";

        // UI prefabs
        public const string InventorySlotPrefab = PrefabCreator + "Inventory Slot Prefab";
        public const string ExposureZonePrefabCreator = PrefabCreator + "Exposure Zone Creator";
        public const string RecipePrefabCreator = PrefabCreator + "Recipe Prefab Creator";

        public const string Project = Root + "Project/";
        public const string Content = Root + "Content/";
        public const string Crafting = Root + "Crafting/";
        public const string Quests = Root + "Quests/";
        public const string Combat = Root + "Combat/";
        public const string CombatAnimations = Combat + "Animations/";
        public const string RebuildEnemyControllerFromShooterMelee = CombatAnimations + "Rebuild Selected Controller from ShooterMelee Base";
        public const string RepairAllHumanoidCombatPrefabs = Combat + "Repair All Humanoid Combat Prefabs";
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
