using System.Collections.Generic;
using UnityEngine;
using MalbersAnimations.PathCreation;
using MalbersAnimations.PathCreation.Utility;
using Project.AI;
using Project.Creatures;
using Project.Pet;

namespace Project.World
{
    /// <summary>
    /// Shared path patrol mode. Loop = next anchor in order.
    /// PingPong: enemies/creatures reverse along the path; pets pick a random next anchor.
    /// </summary>
    public enum DMIPathPatrolMode
    {
        Loop,
        PingPong
    }

    [System.Flags]
    public enum DMIPathAgentTypes
    {
        None = 0,
        Pets = 1 << 0,
        Enemies = 1 << 1,
        Creatures = 1 << 2,
        All = Pets | Enemies | Creatures
    }

    /// <summary>
    /// Optional, non-destructive Path Creator wrapper for patrol / path-follow.
    /// Path Creator owns bezier anchors, inspector tabs, and Scene editing tools.
    /// This only reads <see cref="PathCreator.bezierPath"/> anchors — it never edits
    /// editorData, steals SceneGUI, or creates DMI Anchor Handle children.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PathCreator))]
    public class DMIPathFollowProvider : MonoBehaviour
    {
        private const string LegacyHandlesRootName = "DMI Anchor Handles";
        private const string LegacyWaypointsRootName = "DMI Waypoints";

        [Header("Path Source")]
        [Tooltip("Native Path Creator — bezier / vertex path math source of truth. Edit anchors with Path Creator Scene tools only.")]
        [SerializeField] private PathCreator pathCreator;

        [Header("Patrol")]
        [Tooltip("Loop = ordered. PingPong = reverse for enemies/creatures; random next anchor for pets.")]
        [SerializeField] private DMIPathPatrolMode patrolMode = DMIPathPatrolMode.Loop;
        [Tooltip("Arrival distance used by pet path-follow (enemies/creatures use their own stop distances).")]
        [SerializeField] private float arrivalDistance = 0.75f;
        [Tooltip("Seconds pets wait/idle at each Path Creator anchor before moving on. Enemies/creatures use their own wait fields.")]
        [SerializeField] private float patrolWaitDuration = 2f;

        [Header("Agent Types")]
        [SerializeField] private DMIPathAgentTypes allowedAgents = DMIPathAgentTypes.All;

        [Header("Assignment (secondary)")]
        [Tooltip("Optional: agents entering the attract trigger are auto-assigned. Prefer assigning Path Creator on each AI.")]
        [SerializeField] private bool autoAssignInTrigger = false;
        [SerializeField] private float attractRadius = 12f;
        [SerializeField] private bool createAttractTrigger = false;
        [SerializeField] private EnemyAiController[] manualEnemies = System.Array.Empty<EnemyAiController>();
        [SerializeField] private DMICreatureAiController[] manualCreatures = System.Array.Empty<DMICreatureAiController>();
        [SerializeField] private PetController[] manualPets = System.Array.Empty<PetController>();

        [Header("Gizmos")]
        [Tooltip("Draws the Path Creator vertex path polyline only. Anchor spheres are Path Creator's native Scene tools — not duplicated here.")]
        [SerializeField] private bool drawPathGizmos = true;
        [SerializeField] private Color pathGizmoColor = new Color(0.75f, 0.18f, 0.48f, 0.95f);

        private readonly HashSet<EnemyAiController> registeredEnemies = new HashSet<EnemyAiController>();
        private readonly HashSet<DMICreatureAiController> registeredCreatures = new HashSet<DMICreatureAiController>();
        private readonly HashSet<PetController> registeredPets = new HashSet<PetController>();

        private SphereCollider attractCollider;
        private Vector3[] cachedAnchorWorldPoints = System.Array.Empty<Vector3>();

        public PathCreator PathCreator => pathCreator != null ? pathCreator : GetComponent<PathCreator>();
        public DMIPathPatrolMode PatrolMode => patrolMode;
        public float ArrivalDistance => arrivalDistance;
        public float PatrolWaitDuration => patrolWaitDuration;

        public void ConfigurePatrol(DMIPathPatrolMode mode, float waitDuration, float arrival = -1f)
        {
            patrolMode = mode;
            patrolWaitDuration = Mathf.Max(0f, waitDuration);
            if (arrival >= 0f)
                arrivalDistance = Mathf.Max(0.05f, arrival);
        }

        /// <summary>World positions of Path Creator bezier anchors (source of truth for patrol points).</summary>
        public Vector3[] AnchorWorldPoints => cachedAnchorWorldPoints;

        private void Reset()
        {
            pathCreator = GetComponent<PathCreator>();
        }

        private void OnEnable()
        {
            EnsurePathCreator();
            SubscribePath();
            // Edit-mode: never DestroyImmediate here — that fought Path Creator Scene tools.
            // Legacy DMI children are stripped on play / via inspector button only.
            if (Application.isPlaying)
                StripLegacyChildren();
            SyncAttractCollider();
            RefreshPath();
            if (Application.isPlaying)
                AssignManualAgents();
        }

        private void OnDisable()
        {
            UnsubscribePath();
        }

        private void Start()
        {
            if (!Application.isPlaying)
                return;

            StripLegacyChildren();
            RefreshPath();
            AssignManualAgents();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            arrivalDistance = Mathf.Max(0.05f, arrivalDistance);
            patrolWaitDuration = Mathf.Max(0f, patrolWaitDuration);
            attractRadius = Mathf.Max(0.5f, attractRadius);
        }
#endif

        private void EnsurePathCreator()
        {
            if (pathCreator == null)
                pathCreator = GetComponent<PathCreator>();
        }

        private void SubscribePath()
        {
            EnsurePathCreator();
            if (pathCreator == null)
                return;

            pathCreator.pathUpdated -= OnPathUpdated;
            pathCreator.pathUpdated += OnPathUpdated;
        }

        private void UnsubscribePath()
        {
            if (pathCreator == null)
                return;

            pathCreator.pathUpdated -= OnPathUpdated;
        }

        private void OnPathUpdated()
        {
            RefreshPath();
            NotifyFollowersPathChanged();
        }

        /// <summary>
        /// Removes obsolete DMI Anchor Handles / DMI Waypoints / orphaned Anchor N children.
        /// Path Creator native bezier anchors (not GameObjects) are untouched.
        /// </summary>
        private void StripLegacyChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child == null || !IsLegacyAnchorChild(child))
                    continue;

                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        private static bool IsLegacyAnchorChild(Transform child)
        {
            string childName = child.name;
            if (childName == LegacyHandlesRootName || childName == LegacyWaypointsRootName)
                return true;

            // Orphaned former DMIPathAnchorHandle children after script delete / root unpack.
            if (childName.StartsWith("Anchor ", System.StringComparison.Ordinal))
                return true;

            return false;
        }

        private void SyncAttractCollider()
        {
            if (!createAttractTrigger)
            {
                if (attractCollider != null)
                    attractCollider.enabled = false;
                return;
            }

            if (attractCollider == null)
                attractCollider = GetComponent<SphereCollider>();

            if (attractCollider == null)
                attractCollider = gameObject.AddComponent<SphereCollider>();

            attractCollider.isTrigger = true;
            attractCollider.radius = attractRadius;
            attractCollider.center = Vector3.zero;
            attractCollider.enabled = autoAssignInTrigger;
        }

        /// <summary>
        /// Rebuilds the cached bezier-anchor world positions from Path Creator.
        /// Call after external path edits if <c>pathUpdated</c> did not fire.
        /// </summary>
        public void RefreshPath(bool rebuildHandles = false)
        {
            // rebuildHandles retained for call-site compatibility; Transform handles are obsolete.
            _ = rebuildHandles;

            EnsurePathCreator();
            // Do not strip children from RefreshPath — Path Creator edits fire pathUpdated often.
            SyncAttractCollider();

            if (pathCreator == null || pathCreator.bezierPath == null)
            {
                cachedAnchorWorldPoints = System.Array.Empty<Vector3>();
                return;
            }

            CacheAnchorWorldPoints();
        }

        private void CacheAnchorWorldPoints()
        {
            BezierPath bezier = pathCreator.bezierPath;
            int anchorCount = bezier.NumAnchorPoints;
            if (anchorCount <= 0)
            {
                cachedAnchorWorldPoints = System.Array.Empty<Vector3>();
                return;
            }

            var points = new Vector3[anchorCount];
            for (int a = 0; a < anchorCount; a++)
            {
                int bezierIndex = a * 3;
                points[a] = MathUtility.TransformPoint(bezier[bezierIndex], pathCreator.transform, bezier.Space);
            }

            cachedAnchorWorldPoints = points;
        }

        /// <summary>Assign all serialized manual agent references (play mode).</summary>
        public void AssignManualAgents()
        {
            if (!Application.isPlaying)
                return;

            if ((allowedAgents & DMIPathAgentTypes.Enemies) != 0)
            {
                for (int i = 0; i < manualEnemies.Length; i++)
                    TryAssignEnemy(manualEnemies[i]);
            }

            if ((allowedAgents & DMIPathAgentTypes.Creatures) != 0)
            {
                for (int i = 0; i < manualCreatures.Length; i++)
                    TryAssignCreature(manualCreatures[i]);
            }

            if ((allowedAgents & DMIPathAgentTypes.Pets) != 0)
            {
                for (int i = 0; i < manualPets.Length; i++)
                    TryAssignPet(manualPets[i]);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!Application.isPlaying || !autoAssignInTrigger || other == null)
                return;

            if ((allowedAgents & DMIPathAgentTypes.Enemies) != 0)
            {
                EnemyAiController enemy = other.GetComponentInParent<EnemyAiController>();
                if (enemy != null)
                    TryAssignEnemy(enemy);
            }

            if ((allowedAgents & DMIPathAgentTypes.Creatures) != 0)
            {
                DMICreatureAiController creature = other.GetComponentInParent<DMICreatureAiController>();
                if (creature != null)
                    TryAssignCreature(creature);
            }

            if ((allowedAgents & DMIPathAgentTypes.Pets) != 0)
            {
                PetController pet = other.GetComponentInParent<PetController>();
                if (pet != null)
                    TryAssignPet(pet);
            }
        }

        public bool TryAssignEnemy(EnemyAiController enemy)
        {
            if (enemy == null || (allowedAgents & DMIPathAgentTypes.Enemies) == 0)
                return false;

            RefreshPath();
            if (cachedAnchorWorldPoints.Length < 2)
                return false;

            enemy.ConfigurePatrolRoute((Vector3[])cachedAnchorWorldPoints.Clone(), ToEnemyPatrolMode(patrolMode));
            registeredEnemies.Add(enemy);
            return true;
        }

        public bool TryAssignCreature(DMICreatureAiController creature)
        {
            if (creature == null || (allowedAgents & DMIPathAgentTypes.Creatures) == 0)
                return false;

            RefreshPath();
            if (cachedAnchorWorldPoints.Length < 2)
                return false;

            creature.SetPatrolRoute((Vector3[])cachedAnchorWorldPoints.Clone(), ToCreaturePatrolMode(patrolMode));
            registeredCreatures.Add(creature);
            return true;
        }

        public bool TryAssignPet(PetController pet)
        {
            if (pet == null || (allowedAgents & DMIPathAgentTypes.Pets) == 0)
                return false;

            RefreshPath();
            if (cachedAnchorWorldPoints.Length < 2)
                return false;

            pet.AssignPathFollow(
                (Vector3[])cachedAnchorWorldPoints.Clone(),
                patrolMode,
                arrivalDistance,
                patrolWaitDuration);
            registeredPets.Add(pet);
            return true;
        }

        public void UnassignPet(PetController pet)
        {
            if (pet == null)
                return;

            registeredPets.Remove(pet);
            pet.ClearPathFollow();
        }

        public void UnregisterEnemy(EnemyAiController enemy)
        {
            if (enemy != null)
                registeredEnemies.Remove(enemy);
        }

        public void UnregisterCreature(DMICreatureAiController creature)
        {
            if (creature != null)
                registeredCreatures.Remove(creature);
        }

        public void UnregisterPet(PetController pet)
        {
            if (pet != null)
                registeredPets.Remove(pet);
        }

        /// <summary>Obsolete: Transform anchors removed. Strips any leftover DMI Anchor Handles children.</summary>
        public void RemoveTransformAnchors()
        {
            StripLegacyChildren();
        }

        /// <summary>Legacy alias for <see cref="RemoveTransformAnchors"/>.</summary>
        public void RemoveLegacyAnchorHandles() => RemoveTransformAnchors();

        /// <summary>Obsolete no-op: use Path Creator Scene tools to edit anchors, then Refresh Path.</summary>
        public void RebuildTransformAnchors()
        {
            StripLegacyChildren();
            RefreshPath();
        }

        private void NotifyFollowersPathChanged()
        {
            if (!Application.isPlaying)
                return;

            Vector3[] anchors = cachedAnchorWorldPoints.Length >= 2
                ? (Vector3[])cachedAnchorWorldPoints.Clone()
                : null;

            foreach (EnemyAiController enemy in registeredEnemies)
            {
                if (enemy != null && anchors != null)
                    enemy.ConfigurePatrolRoute(anchors, ToEnemyPatrolMode(patrolMode));
            }

            foreach (DMICreatureAiController creature in registeredCreatures)
            {
                if (creature != null && anchors != null)
                    creature.SetPatrolRoute(anchors, ToCreaturePatrolMode(patrolMode));
            }

            foreach (PetController pet in registeredPets)
            {
                if (pet != null && anchors != null)
                {
                    pet.AssignPathFollow(anchors, patrolMode, arrivalDistance, patrolWaitDuration);
                }
            }
        }

        private static EnemyPatrolMode ToEnemyPatrolMode(DMIPathPatrolMode mode)
        {
            return mode == DMIPathPatrolMode.PingPong ? EnemyPatrolMode.PingPong : EnemyPatrolMode.Loop;
        }

        private static DMICreaturePatrolMode ToCreaturePatrolMode(DMIPathPatrolMode mode)
        {
            return mode == DMIPathPatrolMode.PingPong ? DMICreaturePatrolMode.PingPong : DMICreaturePatrolMode.Loop;
        }

        private void OnDrawGizmos()
        {
            if (!drawPathGizmos)
                return;

            EnsurePathCreator();
            if (pathCreator == null || pathCreator.path == null)
                return;

            VertexPath path = pathCreator.path;
            path.UpdateTransform(pathCreator.transform);
            if (path.NumPoints < 2)
                return;

            // Polyline only — do not draw spheres at anchors (Path Creator owns those Scene handles).
            Gizmos.color = pathGizmoColor;
            for (int i = 0; i < path.NumPoints - 1; i++)
                Gizmos.DrawLine(path.GetPoint(i), path.GetPoint(i + 1));

            if (path.isClosedLoop)
                Gizmos.DrawLine(path.GetPoint(path.NumPoints - 1), path.GetPoint(0));

            if (createAttractTrigger && autoAssignInTrigger)
            {
                Gizmos.color = new Color(0.75f, 0.18f, 0.48f, 0.12f);
                Gizmos.DrawSphere(transform.position, attractRadius);
                Gizmos.color = new Color(0.75f, 0.18f, 0.48f, 0.45f);
                Gizmos.DrawWireSphere(transform.position, attractRadius);
            }
        }
    }
}
