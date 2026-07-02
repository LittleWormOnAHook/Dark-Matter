namespace Project.Player
{
    /// <summary>
    /// Asset paths for player animator controllers used by runtime utilities and editor setup tools.
    /// </summary>
    public static class PlayerAnimatorControllerPaths
    {
        public const string LegacyControllerPath =
            "Assets/_Project/Animations/ProjectUnityCharacterController.controller";

        public const string GkcControllerPath =
            "Assets/_Project/Animations/ProjectGKCCharacterController.controller";

        public const string GkcControllerSourcePath =
            "Assets/Animations/Third Person Character/Animator/Third Person Animator Controller GKC.controller";

        /// <summary>
        /// Pristine GKC controller sub-asset count; project copy below this is treated as corrupted.
        /// </summary>
        public const int GkcControllerMinSubAssetCount = 3620;

        public const string LocomotionOverridePath =
            "Assets/_Project/Animations/ProjectUnityCharacterLocomotion.overrideController";
    }
}
