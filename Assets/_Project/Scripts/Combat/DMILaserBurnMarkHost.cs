using System.Collections.Generic;
using Project.Core;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Tracks laser burn marks attached to a harvestable resource so they can be
    /// returned to the pool when the node depletes (avoids floating orphan scorches).
    /// </summary>
    [DisallowMultipleComponent]
    public class DMILaserBurnMarkHost : MonoBehaviour
    {
        private readonly List<DMILaserBurnMark> _marks = new List<DMILaserBurnMark>(16);
        private bool _released;

        public static DMILaserBurnMarkHost GetOrCreate(Transform target)
        {
            if (target == null)
                return null;

            DMILaserBurnMarkHost host = target.GetComponent<DMILaserBurnMarkHost>();
            if (host == null)
                host = target.gameObject.AddComponent<DMILaserBurnMarkHost>();
            return host;
        }

        public void Register(DMILaserBurnMark mark)
        {
            if (mark == null)
                return;

            if (!_marks.Contains(mark))
                _marks.Add(mark);
        }

        public void Unregister(DMILaserBurnMark mark)
        {
            if (mark == null)
                return;

            _marks.Remove(mark);
        }

        /// <summary>
        /// Invalidates leases and returns all tracked marks to the pool (safe before Destroy).
        /// </summary>
        public void ReleaseAll()
        {
            if (_released)
                return;

            _released = true;

            for (int i = _marks.Count - 1; i >= 0; i--)
            {
                DMILaserBurnMark mark = _marks[i];
                if (mark == null)
                    continue;

                mark.InvalidateLease();
                PoolManager.Release(mark.gameObject);
            }

            _marks.Clear();
        }

        private void OnDestroy()
        {
            // Backup if the node is destroyed outside FinishGatherAndDestroy.
            ReleaseAll();
        }
    }
}
