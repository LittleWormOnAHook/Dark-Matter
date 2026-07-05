using Invector;
using Invector.vCharacterController;
using Project.Companions;
using UnityEngine;

namespace Project.Companions.Invector
{
    /// <summary>
    /// Forwards Invector body damage into CompanionHealth so enemies and weapon hits injure pioneers.
    /// </summary>
    [DisallowMultipleComponent]
    public class CompanionInvectorIncomingDamageBridge : MonoBehaviour
    {
        private vThirdPersonController _controller;
        private CompanionHealth _companionHealth;
        private bool _subscribed;

        private void Awake()
        {
            _controller = GetComponent<vThirdPersonController>();
            _companionHealth = GetComponent<CompanionHealth>();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void BindHealth(CompanionHealth health)
        {
            _companionHealth = health;
            Subscribe();
        }

        private void Subscribe()
        {
            if (_subscribed || _controller == null || _companionHealth == null)
                return;

            if (_controller is vHealthController healthController)
                healthController.isImmortal = false;

            _controller.onReceiveDamage.AddListener(ForwardDamage);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || _controller == null)
                return;

            _controller.onReceiveDamage.RemoveListener(ForwardDamage);
            _subscribed = false;
        }

        private void ForwardDamage(vDamage damage)
        {
            if (damage == null || _companionHealth == null || _companionHealth.IsDead)
                return;

            if (damage.damageValue <= 0f)
                return;

            _companionHealth.ApplyDamage(damage.damageValue);
        }
    }
}
