using System;

namespace FORGE3D
{
    /// <summary>
    /// Dark Matter hooks from FORGE3D Turret Editor without a hard assembly reference to _Project.
    /// </summary>
    public static class F3DTurretEditorCallbacks
    {
        public static Action<int> OnAddPlaceableToScene;
        public static Action<int> OnBakePlaceablePrefab;
        public static Action OnBakeSelectedSceneTurret;

        public static void RequestAddPlaceableToScene(int templateIndex)
        {
            OnAddPlaceableToScene?.Invoke(templateIndex);
        }

        public static void RequestBakePlaceablePrefab(int templateIndex)
        {
            OnBakePlaceablePrefab?.Invoke(templateIndex);
        }

        public static void RequestBakeSelectedSceneTurret()
        {
            OnBakeSelectedSceneTurret?.Invoke();
        }
    }
}
