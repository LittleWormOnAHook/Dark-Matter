using System.Collections.Generic;
using UnityEngine;

namespace Project.Building
{
    /// <summary>
    /// 0926-generator: base power. Every base has a power square (Building Studio > Placement, 100 m by default)
    /// centred on its first placed foundation. A generator with fuel powers everything inside the square of the
    /// base it stands in: force fields, lights and equipment.
    /// </summary>
    public static class DMBasePower
    {
        const float RefreshSeconds = 0.25f;

        static readonly List<Vector4> poweredSquares = new List<Vector4>(4);
        static float refreshedAt = -10f;
        static int refreshedFrame = -1;

        struct Consumer
        {
            public Component Owner;
            public bool ForceField;
        }

        static readonly List<Consumer> consumers = new List<Consumer>(32);

        /// <summary>0926-power-load: things that draw power (lights, equipment, force fields) add to their base generator's burn.</summary>
        public static void RegisterConsumer(Component owner, bool forceField)
        {
            if (owner == null)
                return;
            for (int i = 0; i < consumers.Count; i++)
            {
                if (consumers[i].Owner == owner)
                    return;
            }

            consumers.Add(new Consumer { Owner = owner, ForceField = forceField });
        }

        public static void UnregisterConsumer(Component owner)
        {
            for (int i = consumers.Count - 1; i >= 0; i--)
            {
                if (consumers[i].Owner == owner || consumers[i].Owner == null)
                    consumers.RemoveAt(i);
            }
        }

        /// <summary>Force fields and other powered items inside the power square anchored by this foundation.</summary>
        public static void CountLoad(DMBuildingGhost anchor, out int forceFields, out int items)
        {
            forceFields = 0;
            items = 0;
            if (anchor == null)
                return;
            Vector3 c = DMBuildingCreationFx.RestPosition(anchor);
            float half = HalfSize;
            for (int i = consumers.Count - 1; i >= 0; i--)
            {
                Component owner = consumers[i].Owner;
                if (owner == null)
                {
                    consumers.RemoveAt(i);
                    continue;
                }

                Vector3 p = owner.transform.position;
                if (Mathf.Abs(p.x - c.x) > half || Mathf.Abs(p.z - c.z) > half)
                    continue;
                if (consumers[i].ForceField)
                    forceFields++;
                else
                    items++;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            poweredSquares.Clear();
            consumers.Clear();
            refreshedAt = -10f;
            refreshedFrame = -1;
        }

        public static float HalfSize => DMBuildingGhostProfile.BasePowerSquareMeters * 0.5f;

        /// <summary>The first placed foundation whose power square contains this point, or null outside every base.</summary>
        public static DMBuildingGhost BaseAnchorFor(Vector3 point)
        {
            float half = HalfSize;
            DMBuildingGhost best = null;
            DMBuildingGhost[] ghosts = DMBuildingGhost.Snapshot();
            for (int i = 0; i < ghosts.Length; i++)
            {
                DMBuildingGhost ghost = ghosts[i];
                if (ghost == null || !ghost.Built || !DMBuildingCatalog.IsAnyFoundation(ghost.PieceId))
                    continue;
                Vector3 center = DMBuildingCreationFx.RestPosition(ghost);
                if (Mathf.Abs(point.x - center.x) > half || Mathf.Abs(point.z - center.z) > half)
                    continue;
                if (best == null || ghost.BuildOrder < best.BuildOrder)
                    best = ghost;
            }

            return best;
        }

        public static bool IsInsideAnyBase(Vector3 point)
        {
            return BaseAnchorFor(point) != null;
        }

        /// <summary>True when a running generator powers the base square this point is in.</summary>
        public static bool IsPowered(Vector3 point)
        {
            Refresh();
            for (int i = 0; i < poweredSquares.Count; i++)
            {
                Vector4 s = poweredSquares[i];
                if (Mathf.Abs(point.x - s.x) <= s.z && Mathf.Abs(point.z - s.y) <= s.z)
                    return true;
            }

            return false;
        }

        /// <summary>Forces the next IsPowered call to rebuild (refuel, generator placed or removed).</summary>
        public static void MarkDirty()
        {
            refreshedAt = -10f;
            refreshedFrame = -1;
        }

        static void Refresh()
        {
            float now = Time.time;
            if (refreshedFrame == Time.frameCount || now - refreshedAt < RefreshSeconds)
                return;
            refreshedFrame = Time.frameCount;
            refreshedAt = now;
            poweredSquares.Clear();

            IReadOnlyList<DMBaseGenerator> generators = DMBaseGenerator.Active;
            float half = HalfSize;
            for (int i = 0; i < generators.Count; i++)
            {
                DMBaseGenerator generator = generators[i];
                if (generator == null || !generator.IsRunning)
                    continue;
                DMBuildingGhost anchor = BaseAnchorFor(generator.transform.position);
                if (anchor == null)
                    continue;
                Vector3 c = DMBuildingCreationFx.RestPosition(anchor);
                poweredSquares.Add(new Vector4(c.x, c.z, half, 0f));
            }
        }
    }
}

