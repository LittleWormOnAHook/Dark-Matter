using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace FORGE3D
{
    public class F3DShotgun : MonoBehaviour
    {
        private readonly List<ParticleCollisionEvent> _collisionEvents = new List<ParticleCollisionEvent>();
        private ParticleSystem _ps;

        private void Start()
        {
            _ps = GetComponent<ParticleSystem>();
        }

        private void OnEnable()
        {
            Project.Combat.Turrets.DMTurretWeaponContext.NotifySpawned(this);
        }

        // On particle collision
        private void OnParticleCollision(GameObject other)
        {
            var numCollisionEvents = _ps.GetCollisionEvents(other, _collisionEvents);

            // Play collision sound and apply force to the rigidbody was hit
            for (var j = 0; j < numCollisionEvents; j++)
            {
                F3DAudioController.instance.ShotGunHit(_collisionEvents[j].intersection);

                var rb = other.GetComponent<Rigidbody>();
                var pos = _collisionEvents[j].intersection;
                var force = _collisionEvents[j].velocity.normalized * 50f;
                var col = other.GetComponent<Collider>();
                if (Project.Combat.Turrets.DMForge3DHitBridge.TryHandleImpact(
                        this, col, pos, force.normalized, force.magnitude, rb))
                    continue;
                if (!rb) continue;
                rb.AddForce(force, ForceMode.VelocityChange);
            }
        }
    }
}