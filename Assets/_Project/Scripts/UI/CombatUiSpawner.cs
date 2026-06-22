using Project.AI;
using Project.Combat;
using UnityEngine;

namespace Project.UI
{
    /// <summary>
    /// Spawns combat UI from Resources so it works without manual scene wiring.
    /// </summary>
    public static class CombatUiSpawner
    {
        private const string DamagePrefabResourcePath = "Combat/FloatingDamageNumber";
        private const string HealthBarPrefabResourcePath = "Combat/FloatingTargetHealthBar";

        private static GameObject damagePrefab;
        private static GameObject healthBarPrefab;

        public static void ShowDamage(float damage, Vector3 worldPosition, bool isCritical = false)
        {
            Color color = isCritical
                ? new Color(1f, 0.82f, 0.12f, 1f)
                : new Color(0.95f, 0.18f, 0.12f, 1f);
            WorldFloatingDamageNumber.Spawn(damage, worldPosition, color);
        }

        public static FloatingTargetHealthBar SpawnHealthBar(TrainingDummy dummy)
        {
            EnsurePrefabsLoaded();
            Transform canvasRoot = GetCanvasRoot();
            if (healthBarPrefab == null || canvasRoot == null || dummy == null)
            {
                Debug.LogWarning("CombatUiSpawner: could not spawn health bar (missing prefab or canvas).");
                return null;
            }

            GameObject instance = Object.Instantiate(healthBarPrefab, canvasRoot);
            instance.transform.SetAsLastSibling();

            FloatingTargetHealthBar bar = instance.GetComponent<FloatingTargetHealthBar>();
            bar?.Bind(dummy);
            return bar;
        }

        public static FloatingTargetHealthBar SpawnHealthBar(EnemyHealth health, Vector3 worldOffset)
        {
            EnsurePrefabsLoaded();
            Transform canvasRoot = GetCanvasRoot();
            if (healthBarPrefab == null || canvasRoot == null || health == null)
            {
                Debug.LogWarning("CombatUiSpawner: could not spawn enemy health bar (missing prefab or canvas).");
                return null;
            }

            GameObject instance = Object.Instantiate(healthBarPrefab, canvasRoot);
            instance.transform.SetAsLastSibling();

            FloatingTargetHealthBar bar = instance.GetComponent<FloatingTargetHealthBar>();
            bar?.Bind(health, worldOffset);
            return bar;
        }

        private static void EnsurePrefabsLoaded()
        {
            if (damagePrefab == null)
                damagePrefab = Resources.Load<GameObject>(DamagePrefabResourcePath);

            if (healthBarPrefab == null)
                healthBarPrefab = Resources.Load<GameObject>(HealthBarPrefabResourcePath);
        }

        private static Transform GetCanvasRoot()
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            return canvas != null ? canvas.transform : null;
        }
    }
}
