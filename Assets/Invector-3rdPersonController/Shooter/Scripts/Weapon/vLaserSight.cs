using UnityEngine;
using System.Collections;
namespace Invector.vShooter
{
    [RequireComponent(typeof(LineRenderer))]
    public class vLaserSight : MonoBehaviour
    {
        public LayerMask layerMask;
        public GameObject aimSprite;
        public float aimSpriteOffset;
        public float maxDistance;

        private static readonly RaycastHit[] HitBuffer = new RaycastHit[1];

        LineRenderer line;

        void Start()
        {
            line = GetComponent<LineRenderer>();
        }

        void LateUpdate()
        {
            Vector3 origin = transform.position;
            Vector3 direction = transform.forward.normalized;
            var laserLenght = Vector3.zero;

            int hitCount = Physics.RaycastNonAlloc(origin, direction, HitBuffer, maxDistance, layerMask);
            if (hitCount > 0)
            {
                RaycastHit hit = HitBuffer[0];
                laserLenght.z = transform.InverseTransformPoint(hit.point).z - aimSpriteOffset;
                line.SetPosition(1, laserLenght);
                aimSprite.transform.rotation = Quaternion.LookRotation(hit.normal);
            }
            else
            {
                laserLenght.z = maxDistance - aimSpriteOffset;
                line.SetPosition(1, laserLenght);
                aimSprite.transform.localEulerAngles = Vector3.zero;
            }

            aimSprite.transform.localPosition = laserLenght;
        }
    }
}