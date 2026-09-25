using System;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace Project.Building
{
    /// <summary>
    /// Stone kit meshes. Prefabs win when the library has them; otherwise ProBuilder builds the same piece.
    /// </summary>
    public static class DMBuildingPieceFactory
    {
        public static GameObject Create(string pieceId)
        {
            GameObject prefab = DMBuildingLibrary.PrefabFor(pieceId);
            GameObject instance = prefab != null
                ? UnityEngine.Object.Instantiate(prefab)
                : CreateRuntimeMesh(pieceId);
            CenterPivot(instance);
            return instance;
        }

        public static GameObject CreateRuntimeMesh(string pieceId)
        {
            switch (pieceId)
            {
                case "stone_foundation_4x4":
                    return Finish(ShapeGenerator.GenerateCube(PivotLocation.Center, new Vector3(4f, 0.4f, 4f)), pieceId, meshCollider: false);
                case "stone_floor_4x4":
                    return Finish(ShapeGenerator.GenerateCube(PivotLocation.Center, new Vector3(4f, 0.2f, 4f)), pieceId, meshCollider: false);
                case "stone_ceiling_4x4":
                    return Finish(ShapeGenerator.GenerateCube(PivotLocation.Center, new Vector3(4f, 0.2f, 4f)), pieceId, meshCollider: false);
                case "stone_wall_4x4":
                    return Finish(ShapeGenerator.GenerateCube(PivotLocation.Center, new Vector3(4f, 4f, 0.3f)), pieceId, meshCollider: false);
                case "stone_slope_4x4":
                    return Finish(ShapeGenerator.GeneratePrism(PivotLocation.Center, new Vector3(4f, 4f, 4f)), pieceId, meshCollider: false);
                case "stone_door_frame_4x4":
                    return Finish(ShapeGenerator.GenerateDoor(
                        PivotLocation.Center,
                        DMBuildingCatalog.FrameOuter,
                        DMBuildingCatalog.FrameOuter,
                        DMBuildingCatalog.FrameHeader,
                        DMBuildingCatalog.FrameLegWidth,
                        DMBuildingCatalog.FrameDepth), pieceId, meshCollider: true);
                case "stone_door_basic":
                    return Finish(ShapeGenerator.GenerateCube(
                        PivotLocation.Center,
                        new Vector3(DMBuildingCatalog.DoorWidth, DMBuildingCatalog.DoorHeight, DMBuildingCatalog.DoorDepth)), pieceId, meshCollider: false);
                case "stone_wall_window_4x4":
                    return CreateWindowWall();
                default:
                    return Finish(ShapeGenerator.GenerateCube(PivotLocation.Center, Vector3.one * 2f), pieceId, meshCollider: false);
            }
        }

        static GameObject CreateWindowWall()
        {
            var root = new GameObject("stone_wall_window_4x4");
            AddWindowFrame(root.transform, "WindowJambLeft", new Vector3(1f, 4f, 0.3f), new Vector3(-1.5f, 0f, 0f));
            AddWindowFrame(root.transform, "WindowJambRight", new Vector3(1f, 4f, 0.3f), new Vector3(1.5f, 0f, 0f));
            AddWindowFrame(root.transform, "WindowHead", new Vector3(2f, 1f, 0.3f), new Vector3(0f, 1.5f, 0f));
            AddWindowFrame(root.transform, "WindowSill", new Vector3(2f, 1f, 0.3f), new Vector3(0f, -1.5f, 0f));

            ProBuilderMesh pane = ShapeGenerator.GenerateCube(PivotLocation.Center, new Vector3(2f, 2f, 0.04f));
            pane.ToMesh();
            pane.Refresh();
            pane.gameObject.name = "GlassPane";
            pane.transform.SetParent(root.transform, false);

            BoxCollider box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(4f, 4f, 0.3f);
            return root;
        }

        static void AddWindowFrame(Transform parent, string pieceName, Vector3 size, Vector3 localPosition)
        {
            ProBuilderMesh piece = ShapeGenerator.GenerateCube(PivotLocation.Center, size);
            piece.ToMesh();
            piece.Refresh();
            piece.gameObject.name = pieceName;
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = localPosition;
        }

        static GameObject Finish(ProBuilderMesh mesh, string pieceId, bool meshCollider)
        {
            mesh.ToMesh();
            mesh.Refresh();
            GameObject root = mesh.gameObject;
            root.name = pieceId;

            Mesh shared = root.GetComponent<MeshFilter>() != null
                ? root.GetComponent<MeshFilter>().sharedMesh
                : null;

            if (meshCollider && shared != null)
            {
                MeshCollider collider = root.AddComponent<MeshCollider>();
                collider.sharedMesh = shared;
                collider.convex = false;
            }
            else if (shared != null)
            {
                BoxCollider box = root.AddComponent<BoxCollider>();
                box.center = shared.bounds.center;
                box.size = shared.bounds.size;
            }

            return root;
        }

        /// <summary>
        /// Moves mesh content so the combined renderer bounds sit on the root origin.
        /// Placement then rotates around that geometric center for every piece.
        /// </summary>
        public static void CenterPivot(GameObject root)
        {
            if (root == null)
                return;

            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    bounds.Encapsulate(renderers[i].bounds);
            }

            Vector3 delta = bounds.center;
            if (delta.sqrMagnitude < 0.000001f)
                return;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                if (renderer.transform == root.transform)
                {
                    MeshFilter filter = root.GetComponent<MeshFilter>();
                    if (filter != null && filter.sharedMesh != null)
                    {
                        Mesh mesh = UnityEngine.Object.Instantiate(filter.sharedMesh);
                        Vector3[] vertices = mesh.vertices;
                        for (int v = 0; v < vertices.Length; v++)
                            vertices[v] -= delta;
                        mesh.vertices = vertices;
                        mesh.RecalculateBounds();
                        filter.sharedMesh = mesh;
                    }
                }
                else
                {
                    renderer.transform.position -= delta;
                }
            }

            BoxCollider box = root.GetComponent<BoxCollider>();
            if (box != null)
                box.center -= delta;

            MeshCollider meshCollider = root.GetComponent<MeshCollider>();
            MeshFilter rootFilter = root.GetComponent<MeshFilter>();
            if (meshCollider != null && rootFilter != null)
                meshCollider.sharedMesh = rootFilter.sharedMesh;
        }
    }
}
