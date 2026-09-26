using System;
using System.Collections.Generic;
using Project.Data;
using UnityEngine;

namespace Project.Building
{
    /// <summary>One placed build piece in the game save.</summary>
    [Serializable]
    public class BuiltPieceSaveEntry
    {
        public string pieceId;
        public Vector3 position;
        public Quaternion rotation;
        public string materialVariantId;
        public string paidItemId;
        public int cost;
        /// <summary>Surface items: index of the piece they stick to in the same list, or -1.</summary>
        public int hostIndex = -1;
    }

    /// <summary>
    /// Placed build pieces round-trip through GameSaveSystem (0926), so manual slots, Continue and the autosave all keep them.
    /// Loading a save replaces whatever pieces are standing with the saved ones.
    /// </summary>
    public static class DMBuildingSaveRuntime
    {
        public static BuiltPieceSaveEntry[] BuildSave()
        {
            DMBuildingGhost[] ghosts = DMBuildingGhost.Snapshot();
            var built = new List<DMBuildingGhost>(ghosts.Length);
            var index = new Dictionary<DMBuildingGhost, int>(ghosts.Length);
            for (int i = 0; i < ghosts.Length; i++)
            {
                DMBuildingGhost ghost = ghosts[i];
                if (ghost == null || !ghost.Built || string.IsNullOrEmpty(ghost.PieceId))
                    continue;
                index[ghost] = built.Count;
                built.Add(ghost);
            }

            var entries = new BuiltPieceSaveEntry[built.Count];
            for (int i = 0; i < built.Count; i++)
            {
                DMBuildingGhost ghost = built[i];
                Transform t = ghost.transform;
                entries[i] = new BuiltPieceSaveEntry
                {
                    pieceId = ghost.PieceId,
                    position = DMBuildingCreationFx.RestPosition(ghost),
                    rotation = t.rotation,
                    materialVariantId = ghost.MaterialVariantId,
                    paidItemId = ghost.PaidItem != null ? ghost.PaidItem.name : null,
                    cost = ghost.Cost,
                    hostIndex = ghost.Host != null && index.TryGetValue(ghost.Host, out int host) ? host : -1,
                };
            }

            return entries;
        }

        public static void ApplySave(BuiltPieceSaveEntry[] entries)
        {
            DMBuildingPlacementController.ClearAllPieces();
            if (entries == null || entries.Length == 0)
                return;

            var spawned = new DMBuildingGhost[entries.Length];
            int restored = 0;
            int unknown = 0;
            for (int i = 0; i < entries.Length; i++)
            {
                BuiltPieceSaveEntry entry = entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.pieceId))
                    continue;

                DMBuildingPiece piece = DMBuildingCatalog.Find(entry.pieceId);
                if (piece == null)
                {
                    unknown++;
                    continue;
                }

                ItemData paid = string.IsNullOrEmpty(entry.paidItemId) ? null : ItemRegistry.Resolve(entry.paidItemId);
                Quaternion rotation = entry.rotation;
                float magnitude = rotation.x * rotation.x + rotation.y * rotation.y + rotation.z * rotation.z + rotation.w * rotation.w;
                rotation = magnitude > 0.0001f ? Quaternion.Normalize(rotation) : Quaternion.identity;
                spawned[i] = DMBuildingPlacementController.RestorePiece(piece, entry.position, rotation, paid, entry.cost, entry.materialVariantId);
                if (spawned[i] != null)
                    restored++;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                if (spawned[i] == null || entries[i] == null)
                    continue;
                int host = entries[i].hostIndex;
                if (host >= 0 && host < spawned.Length && host != i)
                    spawned[i].Host = spawned[host];
            }

            Debug.Log("[DM Building] Restored " + restored + " built pieces from the save"
                + (unknown > 0 ? " (" + unknown + " skipped: part no longer in the style libraries)." : "."));
        }
    }
}
