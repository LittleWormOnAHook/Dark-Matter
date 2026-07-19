using System.Collections.Generic;
using Invector.vMelee;
using Invector.vShooter;
using Project.Player.Invector;
using UnityEngine;

namespace Project.AI.Invector
{
    /// <summary>
    /// Configures humanoid enemy outgoing damage to hit the player and expedition pioneers.
    /// </summary>
    public static class EnemyInvectorTargetLayers
    {
        public const string PlayerTag = "Player";
        public const string CompanionTag = "CompanionAI";

        public static void Apply(GameObject root)
        {
            if (root == null)
                return;

            ConfigureMeleeTargets(root.GetComponent<vMeleeManager>());
            ConfigureRangedTargets(root.GetComponent<vShooterManager>());
        }

        private static void ConfigureMeleeTargets(vMeleeManager meleeManager)
        {
            if (meleeManager == null)
                return;

            if (meleeManager.hitProperties == null)
                meleeManager.hitProperties = new HitProperties();

            meleeManager.hitProperties.hitDamageTags = new List<string>
            {
                PlayerTag,
                CompanionTag,
            };
        }

        private static void ConfigureRangedTargets(vShooterManager shooterManager)
        {
            if (shooterManager == null)
                return;

            shooterManager.damageLayer = ResolveEnemyShooterDamageLayers();
            PioneerInvectorShooterLayers.SyncEquippedWeaponLayers(shooterManager);
        }

        public static LayerMask ResolveEnemyShooterDamageLayers()
        {
            int mask = 0;
            mask |= 1 << 0;

            TryAddLayer(ref mask, "Player");
            TryAddLayer(ref mask, "CompanionAI");
            TryAddLayer(ref mask, "BodyPart");
            return mask;
        }

        private static void TryAddLayer(ref int mask, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0)
                mask |= 1 << layer;
        }
    }
}
