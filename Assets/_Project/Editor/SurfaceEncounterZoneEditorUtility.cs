#if UNITY_EDITOR
using Project.AI;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    public static class SurfaceEncounterZoneEditorUtility
    {
        [MenuItem("Tools/Dark Matter Genesis/Combat/Create Surface Encounter Zone")]
        public static void CreateSurfaceEncounterZone()
        {
            GameObject root = new GameObject("SurfaceEncounterZone");
            Undo.RegisterCreatedObjectUndo(root, "Create Surface Encounter Zone");

            BoxCollider zoneCollider = root.AddComponent<BoxCollider>();
            zoneCollider.isTrigger = true;
            zoneCollider.size = new Vector3(40f, 8f, 40f);
            zoneCollider.center = new Vector3(0f, 4f, 0f);

            root.AddComponent<SurfaceEncounterZone>();

            GameObject anchorsRoot = new GameObject("Anchors");
            anchorsRoot.transform.SetParent(root.transform, false);

            for (int i = 0; i < 3; i++)
            {
                float angle = (Mathf.PI * 2f / 3f) * i;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * 12f, 0f, Mathf.Sin(angle) * 12f);

                GameObject anchorObject = new GameObject($"SpawnAnchor_{i + 1:00}");
                anchorObject.transform.SetParent(anchorsRoot.transform, false);
                anchorObject.transform.localPosition = offset;
                anchorObject.AddComponent<SurfaceEncounterSpawnAnchor>();

                GameObject routeObject = new GameObject("PatrolRoute");
                routeObject.transform.SetParent(anchorObject.transform, false);
                SurfacePatrolRoute route = routeObject.AddComponent<SurfacePatrolRoute>();

                for (int pointIndex = 0; pointIndex < 3; pointIndex++)
                {
                    float pointAngle = (Mathf.PI * 2f / 3f) * pointIndex;
                    Vector3 pointOffset = new Vector3(
                        Mathf.Cos(pointAngle) * 6f,
                        0f,
                        Mathf.Sin(pointAngle) * 6f);

                    GameObject pointObject = new GameObject($"Waypoint_{pointIndex + 1:00}");
                    pointObject.transform.SetParent(routeObject.transform, false);
                    pointObject.transform.localPosition = pointOffset;
                }

                SerializedObject anchorSerialized = new SerializedObject(anchorObject.GetComponent<SurfaceEncounterSpawnAnchor>());
                anchorSerialized.FindProperty("patrolRoute").objectReferenceValue = route;
                anchorSerialized.ApplyModifiedPropertiesWithoutUndo();
            }

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
        }
    }
}
#endif
