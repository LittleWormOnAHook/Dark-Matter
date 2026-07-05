using Invector.vCharacterController;
using UnityEngine;

namespace Project.Companions.Invector
{
    /// <summary>
    /// Hybrid locomotion: CompanionFollowController owns translation; this bridge drives Invector animator params.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public class CompanionInvectorMotorBridge : MonoBehaviour
    {
        private const float MoveSpeedThreshold = 0.08f;

        private CompanionFollowController _followController;
        private vThirdPersonController _controller;
        private Rigidbody _body;
        private bool _initialized;

        private void Awake()
        {
            _followController = GetComponent<CompanionFollowController>();
            _controller = GetComponent<vThirdPersonController>();
            _body = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (_controller == null || _followController == null)
                return;

            EnsureControllerReady();
            SyncRigidbodyToTransform();
            ApplyFollowLocomotion();
        }

        private void EnsureControllerReady()
        {
            if (_initialized)
                return;

            _controller.lockMovement = true;
            _controller.useRootMotion = false;
            _controller.isStrafing = false;
            _controller.isGrounded = true;
            _initialized = true;
        }

        private void SyncRigidbodyToTransform()
        {
            if (_body == null || !_body.isKinematic)
                return;

            _body.MovePosition(transform.position);
            _body.MoveRotation(transform.rotation);
        }

        private void ApplyFollowLocomotion()
        {
            float speed = _followController.CurrentSpeed;
            Vector3 worldDirection = _followController.CurrentMoveDirection;
            worldDirection.y = 0f;

            bool isMoving = speed > MoveSpeedThreshold && worldDirection.sqrMagnitude > 0.0001f;
            if (isMoving)
            {
                worldDirection.Normalize();
                _controller.moveDirection = worldDirection;
                _controller.input = transform.InverseTransformDirection(worldDirection);
                _controller.isSprinting = speed >= _followController.RunSpeed * 0.85f;
            }
            else
            {
                _controller.moveDirection = Vector3.zero;
                _controller.input = Vector3.zero;
                _controller.isSprinting = false;
            }

            _controller.isGrounded = true;
            _controller.UpdateMotor();

            var moveSpeed = _controller.isStrafing
                ? _controller.strafeSpeed
                : _controller.freeSpeed;
            _controller.SetAnimatorMoveSpeed(moveSpeed);
            _controller.UpdateAnimator();
        }
    }
}
