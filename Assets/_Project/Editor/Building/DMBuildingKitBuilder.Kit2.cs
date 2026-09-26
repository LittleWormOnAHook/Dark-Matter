using System.Collections.Generic;
using Project.Building;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace Project.EditorTools.Building
{
    /// <summary>
    /// Kit phase 2 (0926): the extra stone pieces (foundation steps, pillars, stairwell floor, balcony, the new walls,
    /// columns and beams, half stairs, spiral stairs, ladder, steep roof, ridge cap, flat rooftop and hatch lid).
    /// Same rules as the first kit: bottom at y = 0, walls thin along local Z, climbing pieces rise toward local +Z.
    /// </summary>
    public static partial class DMBuildingKitBuilder
    {
        static IEnumerable<KitPiece> BuildKit2Pieces(Material stone, Material door)
        {
            float m = DMBuildingCatalog.ModuleMeters;
            float h = DMBuildingCatalog.StoryHeight;
            float t = DMBuildingCatalog.WallThickness;
            float s = DMBuildingCatalog.SlabThickness;

            // Foundations and floors.
            yield return FoundationSteps(stone);
            yield return Box(prefix + "support_pillar", "Foundations", stone,
                new Vector3(DMBuildingCatalog.PillarWidth, DMBuildingCatalog.PillarLength, DMBuildingCatalog.PillarWidth));
            yield return StairwellFloor(stone);
            yield return Box(prefix + "balcony_4x2", "Floors", stone, new Vector3(m, s, DMBuildingCatalog.OverhangDepth));

            // Walls.
            yield return Box(prefix + "wall_quarter_4x1", "Walls", stone, new Vector3(m, DMBuildingCatalog.QuarterWallHeight, t));
            yield return WindowWall(prefix + "wall_window_wide_4x4", stone, kit.WideWindowWidth, kit.WideWindowHeight, kit.WideWindowSill, glass: true);
            yield return WindowWall(prefix + "wall_slit_4x4", stone, kit.SlitWidth, kit.SlitHeight, kit.SlitSill, glass: false);
            yield return Archway(stone);
            yield return InvTriWall(prefix + "wall_tri_inv_l_4x2", stone, left: true);
            yield return InvTriWall(prefix + "wall_tri_inv_r_4x2", stone, left: false);
            yield return VentWall(stone);

            // Structure.
            float cw = DMBuildingCatalog.ColumnWidth;
            yield return Box(prefix + "column_4m", "Structure", stone, new Vector3(cw, h, cw));
            yield return Box(prefix + "column_half_2m", "Structure", stone, new Vector3(cw, DMBuildingCatalog.HalfRise, cw));
            yield return Box(prefix + "beam_4m", "Structure", stone, new Vector3(m, DMBuildingCatalog.BeamHeight, DMBuildingCatalog.BeamDepth));
            yield return Brace(stone);
            yield return RailingOf(prefix + "railing_half_4x05", stone, DMBuildingCatalog.HalfRailingHeight);

            // Climbing.
            yield return StairsOf(prefix + "stairs_half_4x2", stone, DMBuildingCatalog.HalfRise, Mathf.Max(2, kit.StairSteps / 2));
            yield return SpiralStairs(stone);
            yield return Wedge(prefix + "ramp_half_4x2", "Ramps", stone, DMBuildingCatalog.HalfRise);
            yield return Ladder(stone);

            // Roofs.
            yield return Wedge(prefix + "roof_steep_4x4", "Roofs", stone, DMBuildingCatalog.SteepRoofRise);
            yield return RidgeCap(stone);
            yield return Rooftop(stone);

            // Doors.
            float lid = Mathf.Min(kit.HatchOpening, m - 0.4f) + 0.2f;
            yield return Box(prefix + "hatch_lid", "Doors", door, new Vector3(lid, DMBuildingCatalog.HatchLidThickness, lid));
        }

        static ProBuilderMesh CubeR(Vector3 size, Vector3 center, Vector3 euler)
        {
            ProBuilderMesh mesh = ShapeGenerator.GenerateCube(PivotLocation.Center, size);
            mesh.transform.SetPositionAndRotation(center, Quaternion.Euler(euler));
            return FinishPart(mesh);
        }

        static KitPiece FoundationSteps(Material material)
        {
            float m = DMBuildingCatalog.ModuleMeters;
            float depth = DMBuildingCatalog.OverhangDepth;
            float height = DMBuildingCatalog.FoundationStepsHeight;
            int steps = kit.FoundationStepCount;
            float run = depth / steps;
            float rise = height / steps;
            KitPiece piece = New(prefix + "foundation_steps_4x2", "Foundations", material, ColliderKind.Mesh);
            for (int i = 0; i < steps; i++)
            {
                float top = rise * (i + 1);
                piece.Solid.Add(Cube(new Vector3(m, top, run), new Vector3(0f, top * 0.5f, -depth * 0.5f + run * (i + 0.5f))));
            }

            return piece;
        }

        /// <summary>Floor with an opening that reaches the +Z edge, over the top half of a straight staircase rising to +Z.</summary>
        static KitPiece StairwellFloor(Material material)
        {
            float m = DMBuildingCatalog.ModuleMeters;
            float s = DMBuildingCatalog.SlabThickness;
            float border = 0.2f;
            float open = Mathf.Min(kit.StairwellOpening, m - 0.4f);
            float landing = m - open;
            float y = s * 0.5f;
            KitPiece piece = New(prefix + "floor_stairwell_4x4", "Floors", material, ColliderKind.Mesh);
            piece.Solid.Add(Cube(new Vector3(m, s, landing), new Vector3(0f, y, -m * 0.5f + landing * 0.5f)));
            float sideZ = m * 0.5f - open * 0.5f;
            piece.Solid.Add(Cube(new Vector3(border, s, open), new Vector3(-(m * 0.5f - border * 0.5f), y, sideZ)));
            piece.Solid.Add(Cube(new Vector3(border, s, open), new Vector3(m * 0.5f - border * 0.5f, y, sideZ)));
            return piece;
        }

        static KitPiece WindowWall(string id, Material material, float width, float height, float sill, bool glass)
        {
            float m = DMBuildingCatalog.ModuleMeters;
            float h = DMBuildingCatalog.StoryHeight;
            float t = DMBuildingCatalog.WallThickness;
            float ww = Mathf.Min(width, m - 0.4f);
            sill = Mathf.Min(sill, h - 0.5f);
            float wh = Mathf.Min(height, h - sill - 0.2f);
            float jamb = (m - ww) * 0.5f;
            float head = h - sill - wh;
            KitPiece piece = New(id, "Windows", material, ColliderKind.Mesh);
            piece.Solid.Add(Cube(new Vector3(jamb, h, t), new Vector3(-(ww * 0.5f + jamb * 0.5f), h * 0.5f, 0f)));
            piece.Solid.Add(Cube(new Vector3(jamb, h, t), new Vector3(ww * 0.5f + jamb * 0.5f, h * 0.5f, 0f)));
            piece.Solid.Add(Cube(new Vector3(ww, sill, t), new Vector3(0f, sill * 0.5f, 0f)));
            piece.Solid.Add(Cube(new Vector3(ww, head, t), new Vector3(0f, h - head * 0.5f, 0f)));
            if (glass)
                piece.Glass.Add(Cube(new Vector3(ww, wh, kit.GlassThickness), new Vector3(0f, sill + wh * 0.5f, 0f)));
            return piece;
        }

        /// <summary>Straight legs up to the spring line, then a half-circle arch; the header is cut into slices above the arc.</summary>
        static KitPiece Archway(Material material)
        {
            float m = DMBuildingCatalog.ModuleMeters;
            float h = DMBuildingCatalog.StoryHeight;
            float t = DMBuildingCatalog.WallThickness;
            float w = Mathf.Min(kit.ArchWidth, m - 0.4f);
            float r = w * 0.5f;
            float spring = Mathf.Min(kit.ArchSpring, h - r - 0.2f);
            int segments = kit.ArchSegments;
            float leg = (m - w) * 0.5f;
            KitPiece piece = New(prefix + "wall_arch_4x4", "Walls", material, ColliderKind.Mesh);
            piece.Solid.Add(Cube(new Vector3(leg, h, t), new Vector3(-(r + leg * 0.5f), h * 0.5f, 0f)));
            piece.Solid.Add(Cube(new Vector3(leg, h, t), new Vector3(r + leg * 0.5f, h * 0.5f, 0f)));
            for (int i = 0; i < segments; i++)
            {
                float x0 = -r + w * i / segments;
                float x1 = -r + w * (i + 1) / segments;
                float y0 = spring + Mathf.Sqrt(Mathf.Max(0f, r * r - x0 * x0));
                float y1 = spring + Mathf.Sqrt(Mathf.Max(0f, r * r - x1 * x1));
                piece.Solid.Add(Prism(
                    new[]
                    {
                        new Vector3(x0, y0, -t * 0.5f), new Vector3(x1, y1, -t * 0.5f),
                        new Vector3(x1, h, -t * 0.5f), new Vector3(x0, h, -t * 0.5f),
                    },
                    new Vector3(0f, 0f, t)));
            }

            return piece;
        }

        /// <summary>Upside-down triangle wall: full width at the top, the point at the bottom on one side.</summary>
        static KitPiece InvTriWall(string id, Material material, bool left)
        {
            float half = DMBuildingCatalog.ModuleMeters * 0.5f;
            float t = DMBuildingCatalog.WallThickness;
            float rise = DMBuildingCatalog.RoofRise;
            float footX = left ? -half : half;
            KitPiece piece = New(id, "Walls", material, ColliderKind.Mesh);
            piece.Solid.Add(Prism(
                new[] { new Vector3(-half, rise, -t * 0.5f), new Vector3(half, rise, -t * 0.5f), new Vector3(footX, 0f, -t * 0.5f) },
                new Vector3(0f, 0f, t)));
            return piece;
        }

        static KitPiece VentWall(Material material)
        {
            float m = DMBuildingCatalog.ModuleMeters;
            float h = DMBuildingCatalog.StoryHeight;
            float t = DMBuildingCatalog.WallThickness;
            float vw = Mathf.Min(kit.VentWidth, m - 0.4f);
            float sill = Mathf.Min(kit.VentSill, h - 0.3f);
            float vh = Mathf.Min(kit.VentHeight, h - sill - 0.1f);
            float jamb = (m - vw) * 0.5f;
            float head = h - sill - vh;
            KitPiece piece = New(prefix + "wall_vent_4x4", "Walls", material, ColliderKind.Mesh);
            piece.Solid.Add(Cube(new Vector3(jamb, h, t), new Vector3(-(vw * 0.5f + jamb * 0.5f), h * 0.5f, 0f)));
            piece.Solid.Add(Cube(new Vector3(jamb, h, t), new Vector3(vw * 0.5f + jamb * 0.5f, h * 0.5f, 0f)));
            piece.Solid.Add(Cube(new Vector3(vw, sill, t), new Vector3(0f, sill * 0.5f, 0f)));
            piece.Solid.Add(Cube(new Vector3(vw, head, t), new Vector3(0f, h - head * 0.5f, 0f)));
            int slats = kit.VentSlats;
            float pitch = vh / slats;
            for (int i = 0; i < slats; i++)
            {
                float y = sill + pitch * (i + 0.5f);
                piece.Solid.Add(CubeR(new Vector3(vw, 0.04f, t * 0.8f), new Vector3(0f, y, 0f), new Vector3(-30f, 0f, 0f)));
            }

            return piece;
        }

        /// <summary>Diagonal strut from one bottom corner to the opposite top corner of a 4 x 4 wall bay.</summary>
        static KitPiece Brace(Material material)
        {
            float m = DMBuildingCatalog.ModuleMeters;
            float h = DMBuildingCatalog.StoryHeight;
            float d = DMBuildingCatalog.BraceDepth;
            float angle = Mathf.Atan2(h, m) * Mathf.Rad2Deg;
            float c = Mathf.Cos(angle * Mathf.Deg2Rad);
            float s = Mathf.Sin(angle * Mathf.Deg2Rad);
            // Length so the rotated strut's bounds are exactly m wide.
            float length = Mathf.Max(1f, (m - d * s) / Mathf.Max(0.01f, c));
            KitPiece piece = New(prefix + "brace_4x4", "Structure", material, ColliderKind.Mesh);
            piece.Solid.Add(CubeR(new Vector3(length, d, d), new Vector3(0f, h * 0.5f, 0f), new Vector3(0f, 0f, angle)));
            return piece;
        }

        static KitPiece RailingOf(string id, Material material, float height)
        {
            float m = DMBuildingCatalog.ModuleMeters;
            float rt = DMBuildingCatalog.RailingThickness;
            float rail = Mathf.Min(0.12f, height * 0.25f);
            int posts = kit.RailingPosts;
            KitPiece piece = New(id, "Railings", material, ColliderKind.Box);
            piece.Solid.Add(Cube(new Vector3(m, rail, rt), new Vector3(0f, height - rail * 0.5f, 0f)));
            float span = m - rt;
            for (int i = 0; i < posts; i++)
            {
                float x = -span * 0.5f + span * i / (posts - 1);
                piece.Solid.Add(Cube(new Vector3(rt, height - rail, rt), new Vector3(x, (height - rail) * 0.5f, 0f)));
            }

            return piece;
        }

        static KitPiece StairsOf(string id, Material material, float height, int steps)
        {
            float m = DMBuildingCatalog.ModuleMeters;
            float run = m / steps;
            float rise = height / steps;
            KitPiece piece = New(id, "Stairs", material, ColliderKind.Mesh);
            for (int i = 0; i < steps; i++)
            {
                float top = rise * (i + 1);
                piece.Solid.Add(Cube(new Vector3(m, top, run), new Vector3(0f, top * 0.5f, -m * 0.5f + run * (i + 0.5f))));
            }

            return piece;
        }

        /// <summary>Centre post plus treads turning one full circle inside the 4 x 4 cell, on a thin base pad.</summary>
        static KitPiece SpiralStairs(Material material)
        {
            float m = DMBuildingCatalog.ModuleMeters;
            float h = DMBuildingCatalog.StoryHeight;
            int steps = kit.SpiralSteps;
            float rise = h / steps;
            float inner = 0.15f;
            float outer = m * 0.5f - 0.1f;
            float length = outer - inner;
            float width = Mathf.Min(0.9f, 2f * Mathf.PI * outer / steps + 0.05f);
            float tread = 0.15f;
            KitPiece piece = New(prefix + "stairs_spiral_4x4", "Stairs", material, ColliderKind.Mesh);
            piece.Solid.Add(Cube(new Vector3(m, 0.1f, m), new Vector3(0f, 0.05f, 0f)));
            piece.Solid.Add(Cube(new Vector3(0.3f, h, 0.3f), new Vector3(0f, h * 0.5f, 0f)));
            for (int i = 0; i < steps; i++)
            {
                float a = 360f * i / steps;
                float rad = a * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
                Vector3 center = dir * (inner + length * 0.5f) + Vector3.up * (rise * (i + 1) - tread * 0.5f);
                piece.Solid.Add(CubeR(new Vector3(length, tread, width), center, new Vector3(0f, a - 90f, 0f)));
            }

            return piece;
        }

        static KitPiece Ladder(Material material)
        {
            float h = DMBuildingCatalog.StoryHeight;
            float w = DMBuildingCatalog.LadderWidth;
            float d = DMBuildingCatalog.LadderDepth;
            float railW = 0.08f;
            int rungs = kit.LadderRungs;
            KitPiece piece = New(prefix + "ladder_4m", "Structure", material, ColliderKind.Box);
            float railX = w * 0.5f - railW * 0.5f;
            piece.Solid.Add(Cube(new Vector3(railW, h, d), new Vector3(-railX, h * 0.5f, 0f)));
            piece.Solid.Add(Cube(new Vector3(railW, h, d), new Vector3(railX, h * 0.5f, 0f)));
            for (int i = 0; i < rungs; i++)
            {
                float y = h * (i + 0.5f) / rungs;
                piece.Solid.Add(Cube(new Vector3(w - railW * 2f, 0.05f, d * 0.5f), new Vector3(0f, y, 0f)));
            }

            return piece;
        }

        /// <summary>Triangular cap running 4 m along local X; it straddles the ridge where two roof slopes meet.</summary>
        static KitPiece RidgeCap(Material material)
        {
            float half = DMBuildingCatalog.ModuleMeters * 0.5f;
            float w = DMBuildingCatalog.RidgeCapWidth * 0.5f;
            float ch = DMBuildingCatalog.RidgeCapHeight;
            KitPiece piece = New(prefix + "roof_ridge_4m", "Roofs", material, ColliderKind.Mesh);
            piece.Solid.Add(Prism(
                new[] { new Vector3(-half, 0f, -w), new Vector3(-half, 0f, w), new Vector3(-half, ch, 0f) },
                new Vector3(half * 2f, 0f, 0f)));
            return piece;
        }

        /// <summary>Flat roof slab; the low edge wall goes in the Trim child so the walkable top stays the slab top.</summary>
        static KitPiece Rooftop(Material material)
        {
            float m = DMBuildingCatalog.ModuleMeters;
            float s = DMBuildingCatalog.SlabThickness;
            float e = kit.RooftopEdgeHeight;
            float et = 0.2f;
            KitPiece piece = New(prefix + "rooftop_4x4", "Roofs", material, ColliderKind.Box);
            piece.Solid.Add(Cube(new Vector3(m, s, m), new Vector3(0f, s * 0.5f, 0f)));
            float y = s + e * 0.5f;
            float edge = m * 0.5f - et * 0.5f;
            piece.Trim.Add(Cube(new Vector3(m, e, et), new Vector3(0f, y, edge)));
            piece.Trim.Add(Cube(new Vector3(m, e, et), new Vector3(0f, y, -edge)));
            piece.Trim.Add(Cube(new Vector3(et, e, m - et * 2f), new Vector3(edge, y, 0f)));
            piece.Trim.Add(Cube(new Vector3(et, e, m - et * 2f), new Vector3(-edge, y, 0f)));
            return piece;
        }
    }
}
