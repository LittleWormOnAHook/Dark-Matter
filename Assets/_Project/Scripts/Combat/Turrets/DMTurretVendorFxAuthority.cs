using System;
using UnityEngine;

namespace Project.Combat.Turrets
{
    /// <summary>
    /// Per-prefab FORGE3D <see cref="FORGE3D.F3DFXController"/> rate overrides on <see cref="DMTurretRuntime"/>.
    /// When a toggle is off, the linked controller keeps its prefab value.
    /// </summary>
    [Serializable]
    public sealed class DMTurretVendorFxAuthority
    {
        [Tooltip("When on, VulcanFireRate below is pushed to F3DFXController on Awake / Apply.")]
        public bool overrideVulcanFireRate;
        public float vulcanFireRate = 0.07f;

        public bool Any => overrideVulcanFireRate;
    }
}
