using System;
using System.Collections.Generic;
using Project.Data;
using Project.Inventory;
using Project.Storage;
using UnityEngine;

namespace Project.Building
{
    /// <summary>
    /// Build hotbar catalog. Every style library (Resources/Building/Styles) is one hotbar list;
    /// known buildings are separate lists opened from the up panel.
    /// Snap and support rules read the part shape, so any style's parts behave like the matching stone piece.
    /// </summary>
    public static class DMBuildingCatalog
    {
        /// <summary>
        /// Floor and ceiling layers. Index 0 is ground (on a foundation).
        /// Indices 1 to 3 are the three stories above that.
        /// </summary>
        public const int MaxStackLayers = 4;

        public const string FloorId = "stone_floor_4x4";
        public const string CeilingId = "stone_ceiling_4x4";
        public const string WallId = "stone_wall_4x4";
        /// <summary>Stone style id. Also the default hotbar.</summary>
        public const string StoneId = DMBuildingStyles.DefaultId;
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

        // Kit phase 2 (0926) snap sizes.
        public const float QuarterWallHeight = 1f;
        public const float HalfRailingHeight = 0.5f;
        public const float HalfRise = 2f;
        public const float SteepRoofRise = 4f;
        /// <summary>Foundation steps and balcony slabs reach this far out past the edge they hang on.</summary>
        public const float OverhangDepth = 2f;
        public const float FoundationStepsHeight = 1.2f;
        public const float PillarWidth = 0.8f;
        public const float PillarLength = 4f;
        public const float ColumnWidth = 0.5f;
        public const float BeamHeight = 0.4f;
        public const float BeamDepth = 0.4f;
        public const float BraceDepth = 0.25f;
        public const float LadderWidth = 0.8f;
        public const float LadderDepth = 0.12f;
        public const float RidgeCapWidth = 1.2f;
        public const float RidgeCapHeight = 0.35f;
        public const float HatchLidThickness = 0.1f;
        // Kit phase 3 (0926): double doors, 8 m gates, half-cell foundations.
        public const float DoubleDoorWidth = 3.2f;
        public const float GateWidth = 8f;
        public const float GateHeight = 8f;
        public const float GateOpeningWidth = 7f;
        public const float GateOpeningHeight = 7f;
        public const float GateLeafDepth = 0.25f;
        public const float HalfCellMeters = 2f;

        /// <summary>Kit template. Suffixes are appended to a style prefix (stone_, iron_, silicate_).</summary>
        public struct KitPartTemplate
        {
            public string Suffix;
            public string DisplayName;
            public DMBuildingShape Shape;
            public int Cost;

            public KitPartTemplate(string suffix, string displayName, DMBuildingShape shape, int cost)
            {
                Suffix = suffix;
                DisplayName = displayName;
                Shape = shape;
                Cost = cost;
            }
        }

        public static readonly KitPartTemplate[] KitTemplate =
        {
            new KitPartTemplate("foundation_4x4", "Foundation", DMBuildingShape.Foundation, 4),
            new KitPartTemplate("foundation_tri_4x4", "Tri Foundation", DMBuildingShape.TriFoundation, 2),
            new KitPartTemplate("floor_4x4", "Floor", DMBuildingShape.Floor, 4),
            new KitPartTemplate("floor_tri_4x4", "Tri Floor", DMBuildingShape.TriFloor, 2),
            new KitPartTemplate("ceiling_4x4", "Ceiling", DMBuildingShape.Ceiling, 4),
            new KitPartTemplate("hatch_4x4", "Hatch", DMBuildingShape.Hatch, 3),
            new KitPartTemplate("wall_4x4", "Wall", DMBuildingShape.Wall, 4),
            new KitPartTemplate("wall_half_4x2", "Half Wall", DMBuildingShape.HalfWall, 2),
            new KitPartTemplate("wall_window_4x4", "Window", DMBuildingShape.Window, 4),
            new KitPartTemplate("door_frame_4x4", "Door Frame", DMBuildingShape.DoorFrame, 4),
            new KitPartTemplate("door_basic", "Door", DMBuildingShape.Door, 2),
            new KitPartTemplate("passage_4x4", "Passage", DMBuildingShape.Passage, 3),
            new KitPartTemplate("wall_tri_l_4x2", "Tri Wall L", DMBuildingShape.TriWallLeft, 2),
            new KitPartTemplate("wall_tri_r_4x2", "Tri Wall R", DMBuildingShape.TriWallRight, 2),
            new KitPartTemplate("railing_4x1", "Railing", DMBuildingShape.Railing, 1),
            new KitPartTemplate("stairs_4x4", "Stairs", DMBuildingShape.Stairs, 6),
            new KitPartTemplate("ramp_4x4", "Ramp", DMBuildingShape.Ramp, 4),
            new KitPartTemplate("roof_4x4", "Roof", DMBuildingShape.Roof, 3),
            new KitPartTemplate("roof_corner_4x4", "Roof Corner", DMBuildingShape.RoofCorner, 3),
            new KitPartTemplate("roof_inner_4x4", "Roof Inner", DMBuildingShape.RoofInner, 4),
            // Kit phase 2 (0926)
            new KitPartTemplate("foundation_steps_4x2", "Foundation Steps", DMBuildingShape.FoundationSteps, 2),
            new KitPartTemplate("support_pillar", "Support Pillar", DMBuildingShape.SupportPillar, 2),
            new KitPartTemplate("floor_stairwell_4x4", "Stairwell Floor", DMBuildingShape.StairwellFloor, 3),
            new KitPartTemplate("balcony_4x2", "Balcony", DMBuildingShape.Balcony, 2),
            new KitPartTemplate("wall_quarter_4x1", "Quarter Wall", DMBuildingShape.QuarterWall, 1),
            new KitPartTemplate("wall_window_wide_4x4", "Wide Window", DMBuildingShape.WideWindow, 4),
            new KitPartTemplate("wall_slit_4x4", "Slit Window", DMBuildingShape.SlitWindow, 4),
            new KitPartTemplate("wall_arch_4x4", "Archway", DMBuildingShape.Archway, 4),
            new KitPartTemplate("wall_tri_inv_l_4x2", "Inverted Tri Wall L", DMBuildingShape.InvTriWallLeft, 2),
            new KitPartTemplate("wall_tri_inv_r_4x2", "Inverted Tri Wall R", DMBuildingShape.InvTriWallRight, 2),
            new KitPartTemplate("wall_vent_4x4", "Vent Wall", DMBuildingShape.VentWall, 4),
            new KitPartTemplate("column_4m", "Column", DMBuildingShape.Column, 2),
            new KitPartTemplate("column_half_2m", "Half Column", DMBuildingShape.HalfColumn, 1),
            new KitPartTemplate("beam_4m", "Beam", DMBuildingShape.Beam, 2),
            new KitPartTemplate("brace_4x4", "Angled Brace", DMBuildingShape.Brace, 1),
            new KitPartTemplate("railing_half_4x05", "Half Railing", DMBuildingShape.HalfRailing, 1),
            new KitPartTemplate("stairs_half_4x2", "Half Stairs", DMBuildingShape.HalfStairs, 3),
            new KitPartTemplate("stairs_spiral_4x4", "Spiral Stairs", DMBuildingShape.SpiralStairs, 6),
            new KitPartTemplate("ramp_half_4x2", "Half Ramp", DMBuildingShape.HalfRamp, 2),
            new KitPartTemplate("ladder_4m", "Ladder", DMBuildingShape.Ladder, 1),
            new KitPartTemplate("roof_steep_4x4", "Steep Roof", DMBuildingShape.SteepRoof, 4),
            new KitPartTemplate("roof_ridge_4m", "Ridge Cap", DMBuildingShape.RidgeCap, 1),
            new KitPartTemplate("rooftop_4x4", "Flat Rooftop", DMBuildingShape.Rooftop, 4),
            new KitPartTemplate("hatch_lid", "Hatch Lid", DMBuildingShape.HatchLid, 1),
            // Kit phase 3 (0926)
            new KitPartTemplate("door_frame_double_4x4", "Double Door Frame", DMBuildingShape.DoubleDoorFrame, 4),
            new KitPartTemplate("door_double", "Double Door", DMBuildingShape.DoubleDoor, 3),
            new KitPartTemplate("gate_frame_8x8", "Gate Frame (8 m)", DMBuildingShape.GateFrame, 12),
            new KitPartTemplate("gate_8x8", "Gate (8 m)", DMBuildingShape.Gate, 8),
            new KitPartTemplate("foundation_half_4x2", "Half Foundation", DMBuildingShape.HalfFoundation, 2),
            new KitPartTemplate("foundation_quarter_2x2", "Quarter Foundation", DMBuildingShape.QuarterFoundation, 1),
        };

        static readonly List<DMBuildingPiece> pieces = new List<DMBuildingPiece>();
        static readonly Dictionary<string, DMBuildingPiece> byId = new Dictionary<string, DMBuildingPiece>();
        static int builtVersion = int.MinValue;

        public static IReadOnlyList<DMBuildingPiece> All
        {
            get
            {
                EnsureBuilt();
                return pieces;
            }
        }

        static void EnsureBuilt()
        {
            if (builtVersion == DMBuildingStyles.Version && pieces.Count > 0)
                return;

            builtVersion = DMBuildingStyles.Version;
            pieces.Clear();
            byId.Clear();
            buildSerial++;

            IReadOnlyList<DMBuildingStyleLibrary> styles = DMBuildingStyles.All;
            bool stoneFound = false;
            for (int s = 0; s < styles.Count; s++)
            {
                DMBuildingStyleLibrary style = styles[s];
                if (style.styleId == StoneId)
                    stoneFound = true;
                if (style.parts == null)
                    continue;
                for (int i = 0; i < style.parts.Count; i++)
                {
                    DMBuildingPartEntry part = style.parts[i];
                    if (part == null || !part.enabled || string.IsNullOrEmpty(part.id) || byId.ContainsKey(part.id))
                        continue;
                    Add(FromPart(style, part));
                }
            }

            // Before the Stone library asset exists, the built-in stone kit keeps build mode working.
            if (!stoneFound)
            {
                for (int i = 0; i < KitTemplate.Length; i++)
                {
                    KitPartTemplate template = KitTemplate[i];
                    string id = "stone_" + template.Suffix;
                    if (!byId.ContainsKey(id))
                        Add(Make(id, template.DisplayName, StoneId, template.Shape, template.Cost, null, null, 0.01f, Vector3.zero, true));
                }
            }

            DMBuildingPiece seed = Make(CommandCenterSeedId, "CC Seed", null, DMBuildingShape.Custom, 8, null, null, 0f, new Vector3(4f, 3f, 4f), true);
            seed.HotbarId = CommandCenterSeedId;
            seed.IsBuilding = true;
            Add(seed);
        }

        static void Add(DMBuildingPiece piece)
        {
            pieces.Add(piece);
            byId[piece.Id] = piece;
        }

        static DMBuildingPiece FromPart(DMBuildingStyleLibrary style, DMBuildingPartEntry part)
        {
            return Make(
                part.id,
                string.IsNullOrEmpty(part.displayName) ? part.id : part.displayName,
                style.styleId,
                part.shape,
                style.ScaledCost(part.cost),
                part.prefab,
                part.icon,
                part.surfaceOffsetMeters,
                part.sizeOverride,
                part.applyStyleFinish,
                part.category,
                part.modelRotation);
        }

        static DMBuildingPiece Make(
            string id,
            string displayName,
            string styleId,
            DMBuildingShape shape,
            int cost,
            GameObject prefab,
            Texture2D icon,
            float surfaceOffset,
            Vector3 sizeOverride,
            bool applyStyleFinish,
            DMBuildingCategory? category = null,
            Vector3 modelRotation = default)
        {
            DMBuildingSnap snap = SnapFor(shape);
            Vector3 size = SizeFor(shape, prefab, sizeOverride);
            bool meshSized = (shape == DMBuildingShape.SurfaceItem || shape == DMBuildingShape.Custom)
                && !(sizeOverride.x > 0.001f && sizeOverride.y > 0.001f && sizeOverride.z > 0.001f);
            if (meshSized && modelRotation.sqrMagnitude > 0.0001f)
                size = RotatedSize(size, modelRotation);
            return new DMBuildingPiece
            {
                Id = id,
                DisplayName = displayName,
                HotbarId = styleId,
                StyleId = styleId,
                IsBuilding = false,
                Shape = shape,
                Category = category ?? DefaultCategory(shape),
                Size = size,
                Cost = cost,
                Snap = snap,
                RequiresDoorFrame = snap == DMBuildingSnap.Door,
                Prefab = prefab,
                Icon = icon,
                SurfaceOffset = surfaceOffset,
                ApplyStyleFinish = applyStyleFinish,
                ModelRotation = modelRotation,
            };
        }

        /// <summary>Axis-aligned size of a box after the model rotation.</summary>
        public static Vector3 RotatedSize(Vector3 size, Vector3 euler)
        {
            Matrix4x4 m = Matrix4x4.Rotate(Quaternion.Euler(euler));
            Vector3 e = size * 0.5f;
            Vector3 r = new Vector3(
                Mathf.Abs(m.m00) * e.x + Mathf.Abs(m.m01) * e.y + Mathf.Abs(m.m02) * e.z,
                Mathf.Abs(m.m10) * e.x + Mathf.Abs(m.m11) * e.y + Mathf.Abs(m.m12) * e.z,
                Mathf.Abs(m.m20) * e.x + Mathf.Abs(m.m21) * e.y + Mathf.Abs(m.m22) * e.z);
            return r * 2f;
        }

        /// <summary>Extra model rotation (degrees) set on the part in the style library.</summary>
        public static Vector3 ModelRotationFor(string pieceId)
        {
            DMBuildingPiece piece = Find(pieceId);
            return piece != null ? piece.ModelRotation : Vector3.zero;
        }

        public static DMBuildingSnap SnapFor(DMBuildingShape shape)
        {
            switch (shape)
            {
                case DMBuildingShape.Wall:
                case DMBuildingShape.HalfWall:
                case DMBuildingShape.Window:
                case DMBuildingShape.DoorFrame:
                case DMBuildingShape.Passage:
                case DMBuildingShape.TriWallLeft:
                case DMBuildingShape.TriWallRight:
                case DMBuildingShape.Railing:
                case DMBuildingShape.QuarterWall:
                case DMBuildingShape.WideWindow:
                case DMBuildingShape.SlitWindow:
                case DMBuildingShape.Archway:
                case DMBuildingShape.InvTriWallLeft:
                case DMBuildingShape.InvTriWallRight:
                case DMBuildingShape.VentWall:
                case DMBuildingShape.Brace:
                case DMBuildingShape.HalfRailing:
                case DMBuildingShape.Ladder:
                case DMBuildingShape.DoubleDoorFrame:
                    return DMBuildingSnap.Edge;
                case DMBuildingShape.GateFrame:
                    return DMBuildingSnap.WideEdge;
                case DMBuildingShape.HalfFoundation:
                case DMBuildingShape.QuarterFoundation:
                    return DMBuildingSnap.HalfCell;
                case DMBuildingShape.SupportPillar:
                    return DMBuildingSnap.Under;
                case DMBuildingShape.Column:
                case DMBuildingShape.HalfColumn:
                    return DMBuildingSnap.Corner;
                case DMBuildingShape.Beam:
                case DMBuildingShape.RidgeCap:
                    return DMBuildingSnap.Beam;
                case DMBuildingShape.FoundationSteps:
                case DMBuildingShape.Balcony:
                    return DMBuildingSnap.Overhang;
                case DMBuildingShape.HatchLid:
                    return DMBuildingSnap.Lid;
                case DMBuildingShape.Door:
                case DMBuildingShape.DoubleDoor:
                case DMBuildingShape.Gate:
                    return DMBuildingSnap.Door;
                case DMBuildingShape.SurfaceItem:
                    return DMBuildingSnap.Surface;
                default:
                    return DMBuildingSnap.Ground;
            }
        }

        public static DMBuildingCategory DefaultCategory(DMBuildingShape shape)
        {
            switch (shape)
            {
                case DMBuildingShape.Foundation:
                case DMBuildingShape.TriFoundation:
                case DMBuildingShape.FoundationSteps:
                case DMBuildingShape.SupportPillar:
                case DMBuildingShape.HalfFoundation:
                case DMBuildingShape.QuarterFoundation:
                    return DMBuildingCategory.Foundations;
                case DMBuildingShape.QuarterWall:
                case DMBuildingShape.WideWindow:
                case DMBuildingShape.SlitWindow:
                case DMBuildingShape.Archway:
                case DMBuildingShape.InvTriWallLeft:
                case DMBuildingShape.InvTriWallRight:
                case DMBuildingShape.VentWall:
                    return DMBuildingCategory.Walls;
                case DMBuildingShape.StairwellFloor:
                case DMBuildingShape.Balcony:
                case DMBuildingShape.SteepRoof:
                case DMBuildingShape.RidgeCap:
                case DMBuildingShape.Rooftop:
                    return DMBuildingCategory.FloorsAndRoofs;
                case DMBuildingShape.Column:
                case DMBuildingShape.HalfColumn:
                case DMBuildingShape.Beam:
                case DMBuildingShape.Brace:
                case DMBuildingShape.HalfRailing:
                case DMBuildingShape.HalfStairs:
                case DMBuildingShape.SpiralStairs:
                case DMBuildingShape.HalfRamp:
                case DMBuildingShape.Ladder:
                    return DMBuildingCategory.StructureAndStairs;
                case DMBuildingShape.HatchLid:
                case DMBuildingShape.DoubleDoorFrame:
                case DMBuildingShape.DoubleDoor:
                case DMBuildingShape.GateFrame:
                case DMBuildingShape.Gate:
                    return DMBuildingCategory.Doors;
                case DMBuildingShape.Wall:
                case DMBuildingShape.HalfWall:
                case DMBuildingShape.Window:
                case DMBuildingShape.Passage:
                case DMBuildingShape.TriWallLeft:
                case DMBuildingShape.TriWallRight:
                    return DMBuildingCategory.Walls;
                case DMBuildingShape.Floor:
                case DMBuildingShape.TriFloor:
                case DMBuildingShape.Ceiling:
                case DMBuildingShape.Hatch:
                case DMBuildingShape.Roof:
                case DMBuildingShape.RoofCorner:
                case DMBuildingShape.RoofInner:
                    return DMBuildingCategory.FloorsAndRoofs;
                case DMBuildingShape.DoorFrame:
                case DMBuildingShape.Door:
                    return DMBuildingCategory.Doors;
                case DMBuildingShape.Stairs:
                case DMBuildingShape.Ramp:
                case DMBuildingShape.Railing:
                    return DMBuildingCategory.StructureAndStairs;
                default:
                    return DMBuildingCategory.Decor;
            }
        }

        /// <summary>Snap footprint. Kit shapes use the fixed lattice sizes; custom and surface items use their mesh.</summary>
        public static Vector3 SizeFor(DMBuildingShape shape, GameObject prefab, Vector3 sizeOverride)
        {
            switch (shape)
            {
                case DMBuildingShape.Foundation:
                case DMBuildingShape.TriFoundation:
                    return new Vector3(ModuleMeters, FoundationHeight, ModuleMeters);
                case DMBuildingShape.Floor:
                case DMBuildingShape.TriFloor:
                case DMBuildingShape.Ceiling:
                case DMBuildingShape.Hatch:
                    return new Vector3(ModuleMeters, SlabThickness, ModuleMeters);
                case DMBuildingShape.Wall:
                case DMBuildingShape.Window:
                case DMBuildingShape.DoorFrame:
                case DMBuildingShape.Passage:
                    return new Vector3(ModuleMeters, StoryHeight, WallThickness);
                case DMBuildingShape.HalfWall:
                    return new Vector3(ModuleMeters, HalfWallHeight, WallThickness);
                case DMBuildingShape.Door:
                    return new Vector3(DoorWidth, DoorHeight, DoorDepth);
                case DMBuildingShape.TriWallLeft:
                case DMBuildingShape.TriWallRight:
                    return new Vector3(ModuleMeters, RoofRise, WallThickness);
                case DMBuildingShape.Railing:
                    return new Vector3(ModuleMeters, RailingHeight, RailingThickness);
                case DMBuildingShape.Stairs:
                case DMBuildingShape.Ramp:
                    return new Vector3(ModuleMeters, StoryHeight, ModuleMeters);
                case DMBuildingShape.Roof:
                case DMBuildingShape.RoofCorner:
                case DMBuildingShape.RoofInner:
                    return new Vector3(ModuleMeters, RoofRise, ModuleMeters);
                // Kit phase 2 (0926)
                case DMBuildingShape.FoundationSteps:
                    return new Vector3(ModuleMeters, FoundationStepsHeight, OverhangDepth);
                case DMBuildingShape.SupportPillar:
                    return new Vector3(PillarWidth, PillarLength, PillarWidth);
                case DMBuildingShape.StairwellFloor:
                case DMBuildingShape.Rooftop:
                    return new Vector3(ModuleMeters, SlabThickness, ModuleMeters);
                case DMBuildingShape.Balcony:
                    return new Vector3(ModuleMeters, SlabThickness, OverhangDepth);
                case DMBuildingShape.QuarterWall:
                    return new Vector3(ModuleMeters, QuarterWallHeight, WallThickness);
                case DMBuildingShape.WideWindow:
                case DMBuildingShape.SlitWindow:
                case DMBuildingShape.Archway:
                case DMBuildingShape.VentWall:
                    return new Vector3(ModuleMeters, StoryHeight, WallThickness);
                case DMBuildingShape.InvTriWallLeft:
                case DMBuildingShape.InvTriWallRight:
                    return new Vector3(ModuleMeters, RoofRise, WallThickness);
                case DMBuildingShape.Column:
                    return new Vector3(ColumnWidth, StoryHeight, ColumnWidth);
                case DMBuildingShape.HalfColumn:
                    return new Vector3(ColumnWidth, HalfRise, ColumnWidth);
                case DMBuildingShape.Beam:
                    return new Vector3(ModuleMeters, BeamHeight, BeamDepth);
                case DMBuildingShape.Brace:
                    return new Vector3(ModuleMeters, StoryHeight, BraceDepth);
                case DMBuildingShape.HalfRailing:
                    return new Vector3(ModuleMeters, HalfRailingHeight, RailingThickness);
                case DMBuildingShape.HalfStairs:
                case DMBuildingShape.HalfRamp:
                    return new Vector3(ModuleMeters, HalfRise, ModuleMeters);
                case DMBuildingShape.SpiralStairs:
                    return new Vector3(ModuleMeters, StoryHeight, ModuleMeters);
                case DMBuildingShape.Ladder:
                    return new Vector3(LadderWidth, StoryHeight, LadderDepth);
                case DMBuildingShape.SteepRoof:
                    return new Vector3(ModuleMeters, SteepRoofRise, ModuleMeters);
                case DMBuildingShape.RidgeCap:
                    return new Vector3(ModuleMeters, RidgeCapHeight, RidgeCapWidth);
                case DMBuildingShape.HatchLid:
                    return new Vector3(1.6f, HatchLidThickness, 1.6f);
                // Kit phase 3 (0926)
                case DMBuildingShape.DoubleDoorFrame:
                    return new Vector3(ModuleMeters, StoryHeight, WallThickness);
                case DMBuildingShape.DoubleDoor:
                    return new Vector3(DoubleDoorWidth, DoorHeight, DoorDepth);
                case DMBuildingShape.GateFrame:
                    return new Vector3(GateWidth, GateHeight, WallThickness);
                case DMBuildingShape.Gate:
                    return new Vector3(GateOpeningWidth, GateOpeningHeight, GateLeafDepth);
                case DMBuildingShape.HalfFoundation:
                    return new Vector3(ModuleMeters, FoundationHeight, HalfCellMeters);
                case DMBuildingShape.QuarterFoundation:
                    return new Vector3(HalfCellMeters, FoundationHeight, HalfCellMeters);
            }

            if (sizeOverride.x > 0.001f && sizeOverride.y > 0.001f && sizeOverride.z > 0.001f)
                return sizeOverride;
            Vector3 mesh = PrefabMeshSize(prefab);
            if (mesh.sqrMagnitude > 0.0001f)
                return Vector3.Max(mesh, Vector3.one * 0.05f);
            return shape == DMBuildingShape.SurfaceItem ? new Vector3(0.4f, 0.4f, 0.4f) : new Vector3(ModuleMeters, StoryHeight, ModuleMeters);
        }

        /// <summary>Combined mesh bounds of a prefab asset in its root space (works on un-instantiated prefabs).</summary>
        public static Vector3 PrefabMeshSize(GameObject prefab)
        {
            if (prefab == null)
                return Vector3.zero;

            Matrix4x4 toRoot = prefab.transform.worldToLocalMatrix;
            bool any = false;
            Bounds total = default;
            MeshFilter[] filters = prefab.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < filters.Length; i++)
            {
                Mesh mesh = filters[i].sharedMesh;
                if (mesh == null)
                    continue;
                Matrix4x4 m = toRoot * filters[i].transform.localToWorldMatrix;
                Bounds b = mesh.bounds;
                Vector3 c = b.center;
                Vector3 e = b.extents;
                for (int k = 0; k < 8; k++)
                {
                    Vector3 corner = c + new Vector3((k & 1) == 0 ? -e.x : e.x, (k & 2) == 0 ? -e.y : e.y, (k & 4) == 0 ? -e.z : e.z);
                    Vector3 p = m.MultiplyPoint3x4(corner);
                    if (!any)
                    {
                        total = new Bounds(p, Vector3.zero);
                        any = true;
                    }
                    else
                        total.Encapsulate(p);
                }
            }

            return any ? total.size : Vector3.zero;
        }

        // ---- lookups ----

        public static DMBuildingPiece Find(string pieceId)
        {
            if (string.IsNullOrEmpty(pieceId))
                return null;
            EnsureBuilt();
            return byId.TryGetValue(pieceId, out DMBuildingPiece piece) ? piece : null;
        }

        /// <summary>Shape of a piece id. Unknown ids fall back to the stone suffix so old scenes still snap.</summary>
        public static DMBuildingShape ShapeOf(string pieceId)
        {
            DMBuildingPiece piece = Find(pieceId);
            if (piece != null)
                return piece.Shape;
            if (!string.IsNullOrEmpty(pieceId))
            {
                for (int i = 0; i < KitTemplate.Length; i++)
                {
                    if (pieceId.EndsWith("_" + KitTemplate[i].Suffix, StringComparison.Ordinal))
                        return KitTemplate[i].Shape;
                }
            }

            return DMBuildingShape.Custom;
        }

        public static DMBuildingStyleLibrary StyleOf(string pieceId)
        {
            DMBuildingPiece piece = Find(pieceId);
            string styleId = piece != null && !string.IsNullOrEmpty(piece.StyleId) ? piece.StyleId : DMBuildingStyles.DefaultStyleId;
            return DMBuildingStyles.Find(styleId);
        }

        public static GameObject PrefabFor(string pieceId)
        {
            DMBuildingPiece piece = Find(pieceId);
            return piece != null ? piece.Prefab : null;
        }

        /// <summary>Stone id with the same shape. The piece factory builds this runtime mesh when a part has no prefab yet.</summary>
        public static string FallbackMeshId(string pieceId)
        {
            DMBuildingShape shape = ShapeOf(pieceId);
            for (int i = 0; i < KitTemplate.Length; i++)
            {
                if (KitTemplate[i].Shape == shape)
                    return "stone_" + KitTemplate[i].Suffix;
            }

            return pieceId;
        }

        // ---- shape rules ----

        public static bool IsVerticalSupport(string pieceId)
        {
            DMBuildingShape shape = ShapeOf(pieceId);
            return shape == DMBuildingShape.Wall
                || shape == DMBuildingShape.Window
                || shape == DMBuildingShape.DoorFrame
                || shape == DMBuildingShape.Passage
                || shape == DMBuildingShape.WideWindow
                || shape == DMBuildingShape.SlitWindow
                || shape == DMBuildingShape.Archway
                || shape == DMBuildingShape.VentWall
                || shape == DMBuildingShape.DoubleDoorFrame;
        }

        public static bool IsFoundation(string pieceId)
        {
            return ShapeOf(pieceId) == DMBuildingShape.Foundation;
        }

        public static bool IsFloor(string pieceId)
        {
            return ShapeOf(pieceId) == DMBuildingShape.Floor;
        }

        public static bool IsDoor(string pieceId)
        {
            DMBuildingShape shape = ShapeOf(pieceId);
            return shape == DMBuildingShape.Door
                || shape == DMBuildingShape.HatchLid
                || shape == DMBuildingShape.DoubleDoor
                || shape == DMBuildingShape.Gate;
        }

        public static bool IsDoorFrame(string pieceId)
        {
            return ShapeOf(pieceId) == DMBuildingShape.DoorFrame;
        }

        /// <summary>Kit phase 3: the frame each door piece seats in (door, double door, 8 m gate).</summary>
        public static bool FrameFits(string doorPieceId, string framePieceId)
        {
            DMBuildingShape frame = ShapeOf(framePieceId);
            switch (ShapeOf(doorPieceId))
            {
                case DMBuildingShape.DoubleDoor:
                    return frame == DMBuildingShape.DoubleDoorFrame;
                case DMBuildingShape.Gate:
                    return frame == DMBuildingShape.GateFrame;
                default:
                    return frame == DMBuildingShape.DoorFrame;
            }
        }

        /// <summary>Kit phase 3: 4x2 and 2x2 foundations that hang flush off a foundation edge.</summary>
        public static bool IsHalfCellFoundation(string pieceId)
        {
            DMBuildingShape shape = ShapeOf(pieceId);
            return shape == DMBuildingShape.HalfFoundation || shape == DMBuildingShape.QuarterFoundation;
        }

        /// <summary>Full or half-cell foundation. Half-cell pieces can hang off any of these.</summary>
        public static bool IsFoundationLike(string pieceId)
        {
            return IsFoundation(pieceId) || IsHalfCellFoundation(pieceId);
        }

        public static bool IsSurfaceItem(string pieceId)
        {
            return ShapeOf(pieceId) == DMBuildingShape.SurfaceItem;
        }

        /// <summary>Same footprint rule for overlap: a stone wall and an iron wall cannot share a cell.</summary>
        public static bool SameShape(string a, string b)
        {
            if (a == b)
                return true;
            DMBuildingShape shape = ShapeOf(a);
            return shape != DMBuildingShape.Custom && shape != DMBuildingShape.SurfaceItem && shape == ShapeOf(b);
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

            DMBuildingGhost[] ghosts = DMBuildingPlacementController.BuiltGhosts();
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
            DMBuildingShape shape = ShapeOf(pieceId);
            return shape == DMBuildingShape.Ceiling
                || shape == DMBuildingShape.Hatch
                || shape == DMBuildingShape.StairwellFloor
                || shape == DMBuildingShape.Rooftop;
        }

        /// <summary>Flat walkable slabs above the foundation layer (floor, ceiling, hatch, tri floor).</summary>
        public static bool IsSlab(string pieceId)
        {
            DMBuildingShape shape = ShapeOf(pieceId);
            return shape == DMBuildingShape.Floor
                || shape == DMBuildingShape.Ceiling
                || shape == DMBuildingShape.Hatch
                || shape == DMBuildingShape.TriFloor
                || shape == DMBuildingShape.StairwellFloor
                || shape == DMBuildingShape.Rooftop;
        }

        /// <summary>Wall-like pieces another wall can stack on or a slab can sit on.</summary>
        public static bool IsStackableWall(string pieceId)
        {
            DMBuildingShape shape = ShapeOf(pieceId);
            return IsVerticalSupport(pieceId) || shape == DMBuildingShape.HalfWall || shape == DMBuildingShape.QuarterWall;
        }

        public static bool IsStackLayerAllowed(int layer)
        {
            return layer >= 0 && layer < MaxStackLayers;
        }

        public static bool IsEdgeSupport(string pieceId)
        {
            DMBuildingShape shape = ShapeOf(pieceId);
            return shape == DMBuildingShape.Foundation
                || shape == DMBuildingShape.Floor
                || shape == DMBuildingShape.Ceiling
                || shape == DMBuildingShape.Hatch
                || shape == DMBuildingShape.StairwellFloor
                || shape == DMBuildingShape.Rooftop;
        }

        public static bool IsColumn(string pieceId)
        {
            DMBuildingShape shape = ShapeOf(pieceId);
            return shape == DMBuildingShape.Column || shape == DMBuildingShape.HalfColumn;
        }

        public static bool IsHatch(string pieceId)
        {
            return ShapeOf(pieceId) == DMBuildingShape.Hatch;
        }

        /// <summary>Roof slopes with a straight ridge along local +Z (ridge caps seat on these).</summary>
        public static bool IsRidgeRoof(string pieceId)
        {
            DMBuildingShape shape = ShapeOf(pieceId);
            return shape == DMBuildingShape.Roof || shape == DMBuildingShape.SteepRoof;
        }

        // ---- hotbar lists ----

        static int buildSerial;
        static int hotbarCacheSerial = -1;
        static readonly Dictionary<string, List<DMBuildingPiece>> hotbarCache = new Dictionary<string, List<DMBuildingPiece>>();

        /// <summary>
        /// Pieces on one hotbar. 0926-perf: cached until the catalog rebuilds, because this runs several times a frame
        /// (selected piece, hotbar, aim). Callers must not modify the returned list.
        /// </summary>
        public static List<DMBuildingPiece> PiecesFor(string hotbarId)
        {
            EnsureBuilt();
            if (string.IsNullOrEmpty(hotbarId))
                hotbarId = DMBuildingStyles.DefaultStyleId;

            if (hotbarCacheSerial != buildSerial)
            {
                hotbarCacheSerial = buildSerial;
                hotbarCache.Clear();
            }

            if (hotbarCache.TryGetValue(hotbarId, out List<DMBuildingPiece> cached))
                return cached;

            var list = new List<DMBuildingPiece>();
            for (int i = 0; i < pieces.Count; i++)
            {
                if (pieces[i].HotbarId == hotbarId)
                    list.Add(pieces[i]);
            }

            hotbarCache[hotbarId] = list;
            return list;
        }

        public static List<DMBuildingPiece> KnownBuildings()
        {
            EnsureBuilt();
            var list = new List<DMBuildingPiece>();
            for (int i = 0; i < pieces.Count; i++)
            {
                if (pieces[i].IsBuilding)
                    list.Add(pieces[i]);
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

        // ---- cost: each style pays with its own item (Stone = Rock, Iron = Iron Ore, Silicate = Silicate Ore) ----

        static Func<ItemData, bool> MatcherFor(DMBuildingPiece piece)
        {
            DMBuildingStyleLibrary style = piece != null
                ? DMBuildingStyles.Find(string.IsNullOrEmpty(piece.StyleId) ? DMBuildingStyles.DefaultStyleId : piece.StyleId)
                : null;
            if (style != null && (style.costItem != null || (style.costItemNames != null && style.costItemNames.Count > 0)))
                return item => item != null && style.MatchesCostItem(item);
            return IsLegacyStoneItem;
        }

        public static string CostItemName(DMBuildingPiece piece)
        {
            DMBuildingStyleLibrary style = piece != null
                ? DMBuildingStyles.Find(string.IsNullOrEmpty(piece.StyleId) ? DMBuildingStyles.DefaultStyleId : piece.StyleId)
                : null;
            return style != null ? style.CostItemName : "Rock";
        }

        // 0926-perf: the aim preview and every hotbar slot ask "can I afford this?" each frame. The inventory is cached
        // and the count is worked out once per style per frame instead of a scene search per call.
        static InventorySystem cachedInventory;
        static float nextInventorySearch;
        static int costCountFrame = -1;
        static readonly Dictionary<string, int> costCountCache = new Dictionary<string, int>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetRuntimeCaches()
        {
            cachedInventory = null;
            nextInventorySearch = 0f;
            costCountFrame = -1;
            costCountCache.Clear();
        }

        /// <summary>The player inventory. Cached; while none exists the scene is searched at most twice a second unless forced.</summary>
        public static InventorySystem Inventory(bool force = false)
        {
            if (cachedInventory != null)
                return cachedInventory;

            float now = Time.unscaledTime;
            if (!force && Application.isPlaying && now < nextInventorySearch)
                return null;

            nextInventorySearch = now + 0.5f;
            cachedInventory = UnityEngine.Object.FindAnyObjectByType<InventorySystem>();
            return cachedInventory;
        }

        static void InvalidateCostCounts()
        {
            costCountCache.Clear();
        }

        public static int CountCost(DMBuildingPiece piece)
        {
            string key = piece != null && !string.IsNullOrEmpty(piece.StyleId) ? piece.StyleId : DMBuildingStyles.DefaultStyleId;
            bool useCache = Application.isPlaying;
            if (useCache)
            {
                int frame = Time.frameCount;
                if (frame != costCountFrame)
                {
                    costCountFrame = frame;
                    costCountCache.Clear();
                }

                if (costCountCache.TryGetValue(key, out int known))
                    return known;
            }

            Func<ItemData, bool> match = MatcherFor(piece);
            int count = CountInInventory(match) + DMStorageCrateRuntime.CountMatching(match);
            if (useCache)
                costCountCache[key] = count;
            return count;
        }

        static int CountInInventory(Func<ItemData, bool> match)
        {
            InventorySystem inventory = Inventory();
            if (inventory == null || inventory.slots == null)
                return 0;

            int count = 0;
            for (int i = 0; i < inventory.slots.Count; i++)
            {
                InventorySystem.InventorySlot slot = inventory.slots[i];
                if (slot == null || slot.item == null || slot.amount <= 0)
                    continue;
                if (match(slot.item))
                    count += slot.amount;
            }

            return count;
        }

        public static bool HasCost(DMBuildingPiece piece)
        {
            return piece != null && (piece.Cost <= 0 || CountCost(piece) >= piece.Cost);
        }

        public static bool TrySpend(DMBuildingPiece piece, out ItemData paid)
        {
            paid = null;
            if (!HasCost(piece))
                return false;
            if (piece.Cost <= 0)
                return true;

            InventorySystem inventory = Inventory(true);
            if (inventory == null)
                return false;

            InvalidateCostCounts();

            Func<ItemData, bool> match = MatcherFor(piece);
            int remaining = piece.Cost;
            for (int i = 0; i < inventory.slots.Count && remaining > 0; i++)
            {
                InventorySystem.InventorySlot slot = inventory.slots[i];
                if (slot == null || slot.item == null || slot.amount <= 0 || !match(slot.item))
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
                remaining -= DMStorageCrateRuntime.RemoveMatching(item => match(item), remaining);

            return remaining <= 0;
        }

        public static void Refund(ItemData item, int amount, string pieceId)
        {
            if (amount <= 0)
                return;

            if (item == null)
                item = FindCostItemAsset(Find(pieceId));
            if (item == null)
                return;

            InventorySystem inventory = Inventory(true);
            if (inventory == null)
                return;

            InvalidateCostCounts();

            inventory.AddItem(item, amount, false);
        }

        static ItemData FindCostItemAsset(DMBuildingPiece piece)
        {
            DMBuildingStyleLibrary style = piece != null ? DMBuildingStyles.Find(piece.StyleId) : null;
            if (style != null && style.costItem != null)
                return style.costItem;

            Func<ItemData, bool> match = MatcherFor(piece);
            ItemData[] items = ItemRegistry.GetAllItems();
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null && match(items[i]))
                    return items[i];
            }

            return null;
        }

        static bool IsLegacyStoneItem(ItemData item)
        {
            if (item == null)
                return false;
            string name = item.itemName;
            if (string.IsNullOrEmpty(name))
                name = item.name;
            return name.Equals("Rock", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Stone", StringComparison.OrdinalIgnoreCase);
        }
    }

    public enum DMBuildingSnap
    {
        Ground,
        Edge,
        Door,
        /// <summary>Sticks to the face of a built piece (lights, decorations).</summary>
        Surface,
        /// <summary>Kit phase 2: hangs under a foundation (support pillar).</summary>
        Under,
        /// <summary>Kit phase 2: stands on a grid corner point (columns).</summary>
        Corner,
        /// <summary>Kit phase 2: lies along a wall top, between column tops, or on a roof ridge.</summary>
        Beam,
        /// <summary>Kit phase 2: hangs off the outside of a foundation or floor edge (foundation steps, balcony).</summary>
        Overhang,
        /// <summary>Kit phase 2: lies in a built hatch opening (hatch lid).</summary>
        Lid,
        /// <summary>Kit phase 3: an edge seat that spans two grid edges (8 m gate frame).</summary>
        WideEdge,
        /// <summary>Kit phase 3: half-cell foundation hanging flush off a foundation edge.</summary>
        HalfCell
    }

    public sealed class DMBuildingPiece
    {
        public string Id;
        public string DisplayName;
        public string HotbarId;
        public string StyleId;
        public bool IsBuilding;
        public DMBuildingShape Shape;
        public DMBuildingCategory Category;
        public Vector3 Size;
        public int Cost;
        public bool RequiresDoorFrame;
        public DMBuildingSnap Snap;
        public GameObject Prefab;
        public Texture2D Icon;
        public float SurfaceOffset;
        public bool ApplyStyleFinish = true;
        public Vector3 ModelRotation;
    }
}