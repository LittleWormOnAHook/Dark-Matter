using System.Collections.Generic;
using Project.Building;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace Project.EditorTools.Building
{
    /// <summary>
    /// Kit phase 3 (0926): half and quarter foundations, the double door and its 4 m frame, and the 8 m gate and frame.
    /// Same rules as the first kit: bottom at y = 0, walls thin along local Z. Door leaves are saved as hidden
    /// "Leaf" children pivoted on their hinge line so DMBuildingDoor can swing each one.
    /// </summary>
    public static partial class DMBuildingKitBuilder
    {
        static IEnumerable<KitPiece> BuildKit3Pieces(Material stone, Material door)
        {
            float module = DMBuildingCatalog.ModuleMeters;
            float slab = DMBuildingCatalog.FoundationHeight;
            float cell = DMBuildingCatalog.HalfCellMeters;

            yield return Box(prefix + "foundation_half_4x2", "Foundations", stone, new Vector3(module, slab, cell));
            yield return Box(prefix + "foundation_quarter_2x2", "Foundations", stone, new Vector3(cell, slab, cell));
            yield return Opening(prefix + "door_frame_double_4x4", "DoorFrames", stone, DMBuildingCatalog.DoubleDoorWidth, DMBuildingCatalog.DoorHeight);
            yield return LeafDoor(prefix + "door_double", door, DMBuildingCatalog.DoubleDoorWidth, DMBuildingCatalog.DoorHeight, DMBuildingCatalog.DoorDepth, 0);
            yield return GateFrame(prefix + "gate_frame_8x8", stone);
            yield return LeafDoor(prefix + "gate_8x8", door, DMBuildingCatalog.GateOpeningWidth, DMBuildingCatalog.GateOpeningHeight, DMBuildingCatalog.GateLeafDepth, kit.GateCrossbars);
        }

        /// <summary>Two leaves hinged on the outer edges, meeting in the middle with a small gap.</summary>
        static KitPiece LeafDoor(string id, Material material, float width, float height, float depth, int crossbars)
        {
            float gap = kit.DoorLeafGap;
            float leafWidth = Mathf.Max(0.2f, (width - gap) * 0.5f);
            KitPiece piece = New(id, "Doors", material, ColliderKind.Box);
            for (int side = 0; side < 2; side++)
            {
                float sign = side == 0 ? -1f : 1f;
                float centerX = sign * (gap * 0.5f + leafWidth * 0.5f);
                var leafParts = new List<ProBuilderMesh>();
                AddLeafParts(piece.Solid, centerX, leafWidth, height, depth, crossbars);
                AddLeafParts(leafParts, centerX, leafWidth, height, depth, crossbars);
                piece.Leaves.Add(leafParts);
                piece.LeafHinges.Add(new Vector3(sign * width * 0.5f, 0f, 0f));
            }

            return piece;
        }

        static void AddLeafParts(List<ProBuilderMesh> parts, float centerX, float leafWidth, float height, float depth, int crossbars)
        {
            parts.Add(Cube(new Vector3(leafWidth, height, depth), new Vector3(centerX, height * 0.5f, 0f)));
            if (crossbars <= 0)
                return;

            float barHeight = Mathf.Min(0.3f, height * 0.06f);
            float barDepth = 0.08f;
            float barZ = depth * 0.5f + barDepth * 0.5f;
            for (int b = 0; b < crossbars; b++)
            {
                float y = height * (b + 1) / (crossbars + 1);
                Vector3 size = new Vector3(leafWidth * 0.92f, barHeight, barDepth);
                parts.Add(Cube(size, new Vector3(centerX, y, barZ)));
                parts.Add(Cube(size, new Vector3(centerX, y, -barZ)));
            }
        }

        /// <summary>8 x 8 m stone gate frame with a 7 x 7 m opening, plinths at the leg bases and a keystone.</summary>
        static KitPiece GateFrame(string id, Material material)
        {
            float width = DMBuildingCatalog.GateWidth;
            float height = DMBuildingCatalog.GateHeight;
            float thick = DMBuildingCatalog.WallThickness;
            float openWidth = DMBuildingCatalog.GateOpeningWidth;
            float openHeight = DMBuildingCatalog.GateOpeningHeight;
            float leg = (width - openWidth) * 0.5f;
            float header = height - openHeight;

            KitPiece piece = New(id, "DoorFrames", material, ColliderKind.Mesh);
            for (int side = 0; side < 2; side++)
            {
                float sign = side == 0 ? -1f : 1f;
                float legX = sign * (openWidth * 0.5f + leg * 0.5f);
                piece.Solid.Add(Cube(new Vector3(leg, height, thick), new Vector3(legX, height * 0.5f, 0f)));
                piece.Trim.Add(Cube(new Vector3(leg + 0.2f, 0.6f, thick + 0.2f), new Vector3(legX, 0.3f, 0f)));
            }

            piece.Solid.Add(Cube(new Vector3(openWidth, header, thick), new Vector3(0f, height - header * 0.5f, 0f)));
            piece.Trim.Add(Cube(new Vector3(0.8f, header * 0.8f, thick + 0.12f), new Vector3(0f, height - header * 0.5f, 0f)));
            return piece;
        }
    }
}
