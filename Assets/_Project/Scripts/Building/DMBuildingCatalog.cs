using System;
using System.Collections.Generic;
using Project.Data;
using Project.Inventory;
using Project.Storage;
using UnityEngine;

namespace Project.Building
{
    /// <summary>
    /// First playable catalog. Stone components are the default hotbar.
    /// Known buildings are separate hotbar lists opened from the up panel.
    /// Rock counts as stone until a Stone item exists.
    /// </summary>
    public static class DMBuildingCatalog
    {
        /// <summary>
        /// Floor and ceiling layers. Index 0 is ground (on a foundation).
        /// Indices 1–3 are the three stories above that.
        /// Four levels total. Story height is <see cref="DMBuildingGhostProfile.LargeModuleMeters"/> (default 4 m).
        /// </summary>
        public const int MaxStackLayers = 4;

        public const string FloorId = "stone_floor_4x4";
        public const string CeilingId = "stone_ceiling_4x4";
        public const string WallId = "stone_wall_4x4";
        public const string StoneId = "stone";
        public const string DoorFrameId = "stone_door_frame_4x4";
        public const float DoorWidth = 2.2f;
        public const float DoorHeight = 3.2f;
        public const float DoorDepth = 0.12f;
        public const float FrameOuter = 4f;
        public const float FrameDepth = 0.3f;
        public const float FrameLegWidth = (FrameOuter - DoorWidth) * 0.5f;
        public const float FrameHeader = FrameOuter - DoorHeight;
        /// <summary>Opening is flush to the sill, so the door center sits this far below the frame center.</summary>
        public const float DoorSeatDrop = (FrameOuter - DoorHeight) * 0.5f;
        public const string CommandCenterSeedId = "cc_seed";

        // DM kit 0925: fixed kit dimensions. Snapping reads these, so they stay constants (detail sizes live on the profile).
        public const string FoundationId = "stone_foundation_4x4";
        public const string HatchId = "stone_hatch_4x4";
        public const string PassageId = "stone_passage_4x4";
        public const float ModuleMeters = 4f;
        public const float StoryHeight = 4f;
        public const float WallThickness = 0.3f;
        public const float FoundationHeight = 0.4f;
        public const float SlabThickness = 0.2f;
        public const float HalfWallHeight = 2f;
        public const float RoofRise = 2f;
        public const float RailingHeight = 1f;
        public const float RailingThickness = 0.15f;

        static readonly DMBuildingPiece[] Pieces =
        {
            Piece(FoundationId, "Foundation", StoneId, false, new Vector3(4f, FoundationHeight, 4f), 4, DMBuildingSnap.Ground),
            Piece("stone_foundation_tri_4x4", "Tri Foundation", StoneId, false, new Vector3(4f, FoundationHeight, 4f), 2, DMBuildingSnap.Ground),
            Piece(FloorId, "Floor", StoneId, false, new Vector3(4f, SlabThickness, 4f), 4, DMBuildingSnap.Ground),
            Piece("stone_floor_tri_4x4", "Tri Floor", StoneId, false, new Vector3(4f, SlabThickness, 4f), 2, DMBuildingSnap.Ground),
            Piece(CeilingId, "Ceiling", StoneId, false, new Vector3(4f, SlabThickness, 4f), 4, DMBuildingSnap.Ground),
            Piece(HatchId, "Hatch", StoneId, false, new Vector3(4f, SlabThickness, 4f), 3, DMBuildingSnap.Ground),
            Piece(WallId, "Wall", StoneId, false, new Vector3(4f, StoryHeight, WallThickness), 4, DMBuildingSnap.Edge),
            Piece("stone_wall_half_4x2", "Half Wall", StoneId, false, new Vector3(4f, HalfWallHeight, WallThickness), 2, DMBuildingSnap.Edge),
            Piece("stone_wall_window_4x4", "Window", StoneId, false, new Vector3(4f, StoryHeight, WallThickness), 4, DMBuildingSnap.Edge),
            Piece(DoorFrameId, "Door Frame", StoneId, false, new Vector3(4f, StoryHeight, WallThickness), 4, DMBuildingSnap.Edge),
            Piece("stone_door_basic", "Door", StoneId, false, new Vector3(DoorWidth, DoorHeight, DoorDepth), 2, DMBuildingSnap.Door),
            Piece(PassageId, "Passage", StoneId, false, new Vector3(4f, StoryHeight, WallThickness), 3, DMBuildingSnap.Edge),
            Piece("stone_wall_tri_l_4x2", "Tri Wall L", StoneId, false, new Vector3(4f, RoofRise, WallThickness), 2, DMBuildingSnap.Edge),
            Piece("stone_wall_tri_r_4x2", "Tri Wall R", StoneId, false, new Vector3(4f, RoofRise, WallThickness), 2, DMBuildingSnap.Edge),
            Piece("stone_railing_4x1", "Railing", StoneId, false, new Vector3(4f, RailingHeight, RailingThickness), 1, DMBuildingSnap.Edge),
            Piece("stone_stairs_4x4", "Stairs", StoneId, false, new Vector3(4f, StoryHeight, 4f), 6, DMBuildingSnap.Ground),
            Piece("stone_ramp_4x4", "Ramp", StoneId, false, new Vector3(4f, StoryHeight, 4f), 4, DMBuildingSnap.Ground),
            Piece("stone_roof_4x4", "Roof", StoneId, false, new Vector3(4f, RoofRise, 4f), 3, DMBuildingSnap.Ground),
            Piece("stone_roof_corner_4x4", "Roof Corner", StoneId, false, new Vector3(4f, RoofRise, 4f), 3, DMBuildingSnap.Ground),
            Piece("stone_roof_inner_4x4", "Roof Inner", StoneId, false, new Vector3(4f, RoofRise, 4f), 4, DMBuildingSnap.Ground),
            Piece(CommandCenterSeedId, "CC Seed", CommandCenterSeedId, true, new Vector3(4f, 3f, 4f), 8, DMBuildingSnap.Ground),
        };

        public static IReadOnlyList<DMBuildingPiece> All => Pieces;

        public static bool IsVerticalSupport(string pieceId)
        {
            return pieceId == WallId
                || pieceId == "stone_wall_window_4x4"
                || pieceId == DoorFrameId
                || pieceId == PassageId;
        }

        public static bool IsFoundation(string pieceId)
        {
            return pieceId == FoundationId;
        }

        public static bool IsFloor(string pieceId)
        {
            return pieceId == FloorId;
        }

        /// <summary>Only the first foundation uses terrain aim. Further pieces must snap to built modules.</summary>
        public static bool IsFreestanding(string pieceId)
        {
            return IsFoundation(pieceId) && !HasBuiltBaseForCatalog();
        }

        static bool HasBuiltBaseForCatalog()
        {
            if (!Application.isPlaying)
                return false;

            DMBuildingGhost[] ghosts = UnityEngine.Object.FindObjectsByType<DMBuildingGhost>(FindObjectsInactive.Exclude);
            for (int i = 0; i < ghosts.Length; i++)
            {
                DMBuildingGhost ghost = ghosts[i];
                if (ghost != null && ghost.Built && IsFoundation(ghost.PieceId))
                    return true;
            }

            return false;
        }

        public static bool IsCeiling(string pieceId)
        {
            return pieceId == CeilingId || pieceId == HatchId;
        }

        /// <summary>Flat walkable slabs above the foundation layer (floor, ceiling, hatch, tri floor).</summary>
        public static bool IsSlab(string pieceId)
        {
            return pieceId == FloorId || pieceId == CeilingId || pieceId == HatchId || pieceId == "stone_floor_tri_4x4";
        }

        /// <summary>Wall-like pieces another wall can stack on or a slab can sit on.</summary>
        public static bool IsStackableWall(string pieceId)
        {
            return IsVerticalSupport(pieceId) || pieceId == "stone_wall_half_4x2";
        }

        public static bool IsStackLayerAllowed(int layer)
        {
            return layer >= 0 && layer < MaxStackLayers;
        }

        public static bool IsEdgeSupport(string pieceId)
        {
            return pieceId == FoundationId || pieceId == FloorId || pieceId == CeilingId || pieceId == HatchId;
        }

        public static List<DMBuildingPiece> PiecesFor(string hotbarId)
        {
            var list = new List<DMBuildingPiece>();
            if (string.IsNullOrEmpty(hotbarId))
                hotbarId = StoneId;

            for (int i = 0; i < Pieces.Length; i++)
            {
                if (Pieces[i].HotbarId == hotbarId)
                    list.Add(Pieces[i]);
            }

            return list;
        }

        public static List<DMBuildingPiece> KnownBuildings()
        {
            var list = new List<DMBuildingPiece>();
            for (int i = 0; i < Pieces.Length; i++)
            {
                if (Pieces[i].IsBuilding)
                    list.Add(Pieces[i]);
            }

            return list;
        }

        public static DMBuildingPiece Get(string hotbarId, int index)
        {
            List<DMBuildingPiece> list = PiecesFor(hotbarId);
            if (index < 0 || index >= list.Count)
                return null;
            return list[index];
        }

        public static int CountStone()
        {
            return CountStoneInInventory() + DMStorageCrateRuntime.CountMatching(IsStoneItem);
        }

        static int CountStoneInInventory()
        {
            InventorySystem inventory = UnityEngine.Object.FindAnyObjectByType<InventorySystem>();
            if (inventory == null || inventory.slots == null)
                return 0;

            int count = 0;
            for (int i = 0; i < inventory.slots.Count; i++)
            {
                InventorySystem.InventorySlot slot = inventory.slots[i];
                if (slot == null || slot.item == null || slot.amount <= 0)
                    continue;
                if (IsStoneItem(slot.item))
                    count += slot.amount;
            }

            return count;
        }

        public static bool HasStone(int amount)
        {
            return amount <= 0 || CountStone() >= amount;
        }

        public static bool TrySpendStone(int amount, out ItemData paid)
        {
            paid = null;
            if (!HasStone(amount))
                return false;
            if (amount <= 0)
                return true;

            InventorySystem inventory = UnityEngine.Object.FindAnyObjectByType<InventorySystem>();
            if (inventory == null)
                return false;

            int remaining = amount;
            for (int i = 0; i < inventory.slots.Count && remaining > 0; i++)
            {
                InventorySystem.InventorySlot slot = inventory.slots[i];
                if (slot == null || slot.item == null || slot.amount <= 0 || !IsStoneItem(slot.item))
                    continue;

                int take = Mathf.Min(remaining, slot.amount);
                ItemData item = slot.item;
                if (!inventory.RemoveItem(item, take))
                    continue;

                if (paid == null)
                    paid = item;
                remaining -= take;
            }

            if (remaining > 0)
                remaining -= DMStorageCrateRuntime.RemoveMatching(IsStoneItem, remaining);

            return remaining <= 0;
        }

        public static void RefundStone(ItemData item, int amount)
        {
            if (amount <= 0)
                return;

            if (item == null)
                item = FindStoneItemAsset();
            if (item == null)
                return;

            InventorySystem inventory = UnityEngine.Object.FindAnyObjectByType<InventorySystem>();
            if (inventory == null)
                return;

            inventory.AddItem(item, amount, false);
        }

        static ItemData FindStoneItemAsset()
        {
            ItemData[] items = ItemRegistry.GetAllItems();
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null && IsStoneItem(items[i]))
                    return items[i];
            }

            return null;
        }

        static bool IsStoneItem(ItemData item)
        {
            string name = item.itemName;
            if (string.IsNullOrEmpty(name))
                name = item.name;
            return name.Equals("Rock", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Stone", StringComparison.OrdinalIgnoreCase);
        }

        static DMBuildingPiece Piece(
            string id,
            string displayName,
            string hotbarId,
            bool isBuilding,
            Vector3 size,
            int stoneCost,
            DMBuildingSnap snap)
        {
            return new DMBuildingPiece
            {
                Id = id,
                DisplayName = displayName,
                HotbarId = hotbarId,
                IsBuilding = isBuilding,
                Size = size,
                StoneCost = stoneCost,
                Snap = snap,
                RequiresDoorFrame = snap == DMBuildingSnap.Door,
            };
        }
    }

    public enum DMBuildingSnap
    {
        Ground,
        Edge,
        Door
    }

    public sealed class DMBuildingPiece
    {
        public string Id;
        public string DisplayName;
        public string HotbarId;
        public bool IsBuilding;
        public Vector3 Size;
        public int StoneCost;
        public bool RequiresDoorFrame;
        public DMBuildingSnap Snap;
    }
}
