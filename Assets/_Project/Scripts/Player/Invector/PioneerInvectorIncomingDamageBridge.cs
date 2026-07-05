using Invector;
using Invector.vCharacterController;
using Project.Survival;
using UnityEngine;

namespace Project.Player.Invector
{
    /// <summary>
    /// Mirrors Invector body hits into Pioneer SurvivalStats for the HUD / survival systems.
    /// Does not override Invector mortality — fall damage, ragdoll, and vHealthController death
    /// use the prefab defaults on vThirdPersonController.
    /// </summary>
    [DisallowMultipleComponent]
    public class PioneerInvectorIncomingDamageBridge : MonoBehaviour
    {
        private vThirdPersonController _controller;
        private SurvivalStats _survivalStats;
        private bool _subscribed;

        private void Awake()
        {
            _controller = GetComponent<vThirdPersonController>();
            _survivalStats = GetComponent<SurvivalStats>();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_subscribed || _controller == null)
                return;

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
            if (damage == null || _survivalStats == null || _survivalStats.IsDead)
                return;

            if (damage.damageValue <= 0f)
                return;

            string senderName = damage.sender != null ? damage.sender.name : "unknown";
            Debug.Log($"[PlayerDamage] {damage.damageValue} from '{senderName}' → SurvivalStats");
            _survivalStats.ApplyDamage(damage.damageValue, senderName);
        }
    }
}
