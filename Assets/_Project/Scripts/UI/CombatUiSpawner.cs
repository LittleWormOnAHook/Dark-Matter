using Project.AI;
using Project.Combat;
using Project.Companions;
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

        /// <summary>Set by the player melee receiver right before EnemyHealth.TakeDamage; consumed by ShowEnemyDamage.</summary>
        public static bool NextEnemyDamageHeavy;

        /// <summary>Slice 1 / A3: damage number on an enemy, tiered normal / crit / heavy from DM_CombatCoreProfile.</summary>
        public static void ShowEnemyDamage(float damage, Vector3 worldPosition, bool isCritical)
        {
            bool heavy = NextEnemyDamageHeavy;
            NextEnemyDamageHeavy = false;
            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            if (p == null || !p.useDamageNumberTiers)
            {
                ShowDamage(damage, worldPosition, isCritical);
                return;
            }

            Color color = heavy ? p.damageNumberHeavyColor : (isCritical ? p.damageNumberCritColor : p.damageNumberNormalColor);
            float scale = heavy ? p.damageNumberHeavyScale : (isCritical ? p.damageNumberCritScale : 1f);
            WorldFloatingDamageNumber.Spawn(damage, worldPosition, color, scale, isCritical || heavy);
        }

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

        public static FloatingTargetHealthBar SpawnHealthBar(CompanionHealth health, Vector3 worldOffset)
        {
            EnsurePrefabsLoaded();
            Transform canvasRoot = GetCanvasRoot();
            if (healthBarPrefab == null || canvasRoot == null || health == null)
            {
                Debug.LogWarning("CombatUiSpawner: could not spawn companion health bar (missing prefab or canvas).");
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
            return MainMenuController.ResolveCombatHudRoot();
        }
    }
}
