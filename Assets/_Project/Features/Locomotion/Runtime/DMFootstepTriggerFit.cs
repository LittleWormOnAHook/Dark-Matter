using Invector;
using Invector.vCharacterController;
using Project.Features.Climb;
using UnityEngine;

namespace Project.Features.Locomotion
{
    /// <summary>
    /// 2026-10-24h: runtime fit of the Invector vFootStep trigger spheres (prefab untouched).
    /// Kade v2 (mixamo) triggers sit on the ankle bone origin (~9-10 cm above the sole) with r = 0.1, so they only grazed the
    /// ground (and missed it entirely with any hip lift) -> no OnTriggerEnter -> no footstep audio/FX. Moves the sphere down to the
    /// measured sole (+reach) in the foot bone's frame. Settings: DM_ClimbDashProfile "Footsteps: trigger fit".
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DMFootstepTriggerFit : MonoBehaviour
    {
        private vFootStep _fs;
        private vThirdPersonMotor _motor;
        private float _ankleL = -1f, _ankleR = -1f, _t;
        private int _hash;

        public static void Ensure(GameObject player)
        {
            if (player == null || player.GetComponent<DMFootstepTriggerFit>() != null) return;
            if (player.GetComponentInChildren<vFootStep>(true) == null) return;
            player.AddComponent<DMFootstepTriggerFit>();
        }

        private void Awake()
        {
            _fs = GetComponentInChildren<vFootStep>(true);
            _motor = GetComponent<vThirdPersonMotor>();
        }

        private void LateUpdate()
        {
            var p = DM_ClimbDashProfile.Live;
            if (p == null || !p.bodyWeightFootstepTriggerFit || _fs == null || _fs.leftFootTrigger == null || _fs.rightFootTrigger == null) return;
            // Measure ankle height once while grounded and roughly still (root = capsule bottom on ground).
            if (_ankleL < 0f)
            {
                _t += Time.deltaTime;
                bool still = _motor == null || (_motor.isGrounded && new Vector2(_motor.input.x, _motor.input.z).sqrMagnitude < 0.01f);
                if (!still && _t < 3f) return;
                _ankleL = Measure(_fs.leftFootTrigger.transform);
                _ankleR = Measure(_fs.rightFootTrigger.transform);
                Debug.Log($"[FootstepFit] '{name}' ankle above ground L={_ankleL:0.000} R={_ankleR:0.000} m");
            }
            int h = Hash(p);
            if (h == _hash) return;
            _hash = h;
            Fit(_fs.leftFootTrigger, _ankleL, p);
            Fit(_fs.rightFootTrigger, _ankleR, p);
        }

        private float Measure(Transform trig)
        {
            Transform ft = trig.parent != null ? trig.parent : trig;
            return Mathf.Clamp(ft.position.y - transform.position.y, 0.02f, 0.3f);
        }

        private static int Hash(DM_ClimbDashProfile p) =>
            (p.bodyWeightFootstepTriggerSoleReach, p.bodyWeightFootstepTriggerRadius, p.bodyWeightFootstepSoleOffset).GetHashCode();

        private void Fit(vFootStepTrigger trig, float ankle, DM_ClimbDashProfile p)
        {
            var sc = trig.trigger as SphereCollider;
            if (sc == null) return;
            Transform tt = trig.transform;
            float r = p.bodyWeightFootstepTriggerRadius;
            float down = Mathf.Max(0f, ankle + p.bodyWeightFootstepSoleOffset + p.bodyWeightFootstepTriggerSoleReach - r);
            // Offset expressed in the trigger's frame from the current (grounded, idle) pose so it follows the foot's rotation.
            Vector3 local = tt.InverseTransformVector(-transform.up * down);
            float s = Mathf.Max(1e-4f, Mathf.Abs(tt.lossyScale.x));
            sc.center = local;
            sc.radius = r / s;
        }
    }
}
