using System;
using UnityEngine;

namespace Project.Data
{
    /// <summary>
    /// Per-ammo recoil tuning for one-handed vs two-handed weapons.
    /// Author on ammo ItemData assets — weapon grip selects which column applies at fire time.
    /// </summary>
    [Serializable]
    public struct DmAmmoRecoilProfile
    {
        [Tooltip("Vertical camera kick for pistols / one-handed weapons.")]
        public float pistolCameraVertical;
        [Tooltip("Horizontal camera kick half-range (±) for pistols.")]
        public float pistolCameraHorizontal;
        [Tooltip("Shot animation layer weight for pistols. 0 = no animation flinch.")]
        public float pistolAnimationWeight;

        [Tooltip("Vertical camera kick for rifles / two-handed weapons.")]
        public float rifleCameraVertical;
        [Tooltip("Horizontal camera kick half-range (±) for rifles.")]
        public float rifleCameraHorizontal;
        [Tooltip("Shot animation layer weight for rifles. 0 = no animation flinch.")]
        public float rifleAnimationWeight;

        public bool HasAuthoredValues =>
            pistolCameraVertical > 0.001f
            || pistolCameraHorizontal > 0.001f
            || pistolAnimationWeight > 0.001f
            || rifleCameraVertical > 0.001f
            || rifleCameraHorizontal > 0.001f
            || rifleAnimationWeight > 0.001f;

        public void GetCameraKick(bool isRifle, out float vertical, out float horizontal)
        {
            if (isRifle)
            {
                vertical = rifleCameraVertical;
                horizontal = rifleCameraHorizontal;
                return;
            }

            vertical = pistolCameraVertical;
            horizontal = pistolCameraHorizontal;
        }

        public float GetAnimationWeight(bool isRifle) =>
            isRifle ? rifleAnimationWeight : pistolAnimationWeight;
    }
}
