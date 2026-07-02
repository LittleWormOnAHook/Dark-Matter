namespace Project.Player
{
    /// <summary>
    /// GKC animator parameter hashes and layer names shared by the native driver.
    /// </summary>
    public static class GkcAnimatorConstants
    {
        public const string UpperBodyCombatLayer = "Upper Body With Movement";
        public const string UpperBodyChargeLayer = "Upper Body";
        public const string UpperBodyLayer = "Upper Body";
        public const string ArmsLayer = "Arms";
        public const string DeathLayer = "Death";
        public const string GroundedState = "Grounded";

        public const float PlayerModeFreeMovement = 0f;
        public const float PlayerModeTankMovement = 1f;
        public const float PlayerStatusNormal = 0f;
        public const float PlayerStatusInjured = 1f;
        public const float PlayerStatusInjuredLegs = 2f;
        public const float IdleIdDefault = 0f;
        public const float IdleIdCloseCombat = 1f;
        public const float WeaponIdUnarmed = 0f;

        public const float DefaultActionDuration = 1.05f;
        public const float DefaultPowerActionDuration = 1.45f;
        public const float DefaultBlockActionDuration = 8f;
        public const float DefaultHitReactionDuration = 0.65f;
        public const float DefaultHitReactionOverlayDuration = 0.45f;
        public const float MovingSpeedThreshold = 0.15f;

        public const string HitReactionOverlayState =
            "Block With Melee Weapons.Block Hit Reaction Sword 2 Hands";

        public static readonly int MovementSpeed = UnityEngine.Animator.StringToHash("Movement Speed");
        public static readonly int Moving = UnityEngine.Animator.StringToHash("Moving");
        public static readonly int Dead = UnityEngine.Animator.StringToHash("Dead");
        public static readonly int GetUpFromBack = UnityEngine.Animator.StringToHash("Get Up From Back");
        public static readonly int ShieldActive = UnityEngine.Animator.StringToHash("Shield Active");
        public static readonly int PlayerModeId = UnityEngine.Animator.StringToHash("Player Mode ID");
        public static readonly int PlayerStatusId = UnityEngine.Animator.StringToHash("Player Status ID");
        public static readonly int MovementInputActive = UnityEngine.Animator.StringToHash("Movement Input Active");
        public static readonly int CarryingWeapon = UnityEngine.Animator.StringToHash("Carrying Weapon");
        public static readonly int IdleId = UnityEngine.Animator.StringToHash("Idle ID");
        public static readonly int WeaponId = UnityEngine.Animator.StringToHash("Weapon ID");
        public static readonly int ActionActive = UnityEngine.Animator.StringToHash("Action Active");
        public static readonly int ActionActiveUpperBody = UnityEngine.Animator.StringToHash("Action Active Upper Body");
        public static readonly int ActionId = UnityEngine.Animator.StringToHash("Action ID");
        public static readonly int Horizontal = UnityEngine.Animator.StringToHash("Horizontal");
        public static readonly int Vertical = UnityEngine.Animator.StringToHash("Vertical");
        public static readonly int HorizontalStrafe = UnityEngine.Animator.StringToHash("Horizontal Strafe");
        public static readonly int VerticalStrafe = UnityEngine.Animator.StringToHash("Vertical Strafe");
        public static readonly int StrafeModeActive = UnityEngine.Animator.StringToHash("Strafe Mode Active");
        public static readonly int RightArmId = UnityEngine.Animator.StringToHash("Right Arm ID");
        public static readonly int LeftArmId = UnityEngine.Animator.StringToHash("Left Arm ID");
        public static readonly int MovementRelativeToCamera = UnityEngine.Animator.StringToHash("Movement Relative To Camera");
        public static readonly int MovementId = UnityEngine.Animator.StringToHash("Movement ID");
        public static readonly int Forward = UnityEngine.Animator.StringToHash("Forward");
        public static readonly int Turn = UnityEngine.Animator.StringToHash("Turn");
        public static readonly int Ground = UnityEngine.Animator.StringToHash("OnGround");
        public static readonly int Crouch = UnityEngine.Animator.StringToHash("Crouch");
        public static readonly int Jump = UnityEngine.Animator.StringToHash("Jump");
        public static readonly int JumpLeg = UnityEngine.Animator.StringToHash("JumpLeg");
        public static readonly int AttackAnimSpeed = UnityEngine.Animator.StringToHash("AttackAnimSpeed");
        public static readonly int AimingModeActive = UnityEngine.Animator.StringToHash("Aiming Mode Active");
        public static readonly int StrafeId = UnityEngine.Animator.StringToHash("Strafe ID");

        public const float StrafeIdMeleeOneHand = 6f;
        public const float StrafeIdMeleeTwoHand = 10f;
    }
}
