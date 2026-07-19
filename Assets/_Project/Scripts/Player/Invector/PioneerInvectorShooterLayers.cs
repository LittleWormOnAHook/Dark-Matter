using Invector.vShooter;
using UnityEngine;

namespace Project.Player.Invector
{
    /// <summary>
    /// Keeps Invector shooter raycasts aligned with Pioneer enemy colliders (Default + Enemy layers).
    /// </summary>
    public static class PioneerInvectorShooterLayers
    {
        public static LayerMask ResolveShooterDamageLayers(LayerMask current)
        {
            int mask = current.value;

            mask |= 1 << 0; // Default

            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
                mask |= 1 << playerLayer;

            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0)
                mask |= 1 << enemyLayer;

            int bodyPartLayer = LayerMask.NameToLayer("BodyPart");
            if (bodyPartLayer >= 0)
                mask |= 1 << bodyPartLayer;

            return mask;
        }

        public static void ApplyToShooterManager(vShooterManager manager)
        {
            if (manager == null)
                return;

            manager.damageLayer = ResolveShooterDamageLayers(manager.damageLayer);

            // Enemy shooters must NOT ignore the Player tag — that is a player-side setting
            // so the player's own bullets don't self-hit. Enemies shoot at the player, so
            // remove "Player" from the ignore list.
            manager.ignoreTags.Remove("Player");

            SyncEquippedWeaponLayers(manager);
        }

        public static void SyncEquippedWeaponLayers(vShooterManager manager)
        {
            if (manager == null)
                return;

            if (manager.lWeapon != null)
                manager.lWeapon.hitLayer = manager.damageLayer;

            if (manager.rWeapon != null)
                manager.rWeapon.hitLayer = manager.damageLayer;
        }
    }
}
