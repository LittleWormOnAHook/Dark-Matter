using UnityEngine;
using UnityEngine.InputSystem;
using Project.UI;

namespace Project.Features.Jetpack
{
    /// <summary>
    /// Jump = single Space/A. Jetpack = double-press then hold Space/A (polled so Jump is not stolen).
    /// After releasing boost mid-air, press+hold Space/A again re-ignites after a 1s cooldown (no new double-tap).
    /// stamp: jetpack-doubletap-wide 0920k
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(440)]
    public sealed class DMJetpackInputBridge : MonoBehaviour
    {
        private const float DoubleTapWindow = 0.58f;
        private const float HoldIgniteSeconds = 0.12f;
        private const float ReigniteCooldownSeconds = 1f;

        [SerializeField] private DMJetpackController jetpack;
        [SerializeField] private Transform characterTransform;
        [SerializeField] private Camera referenceCamera;

        private bool boostHeld;
        private float lastTapAt = -10f;
        private float secondTapDownAt = -10f;
        private bool waitingHoldAfterDouble;
        private bool ignitedThisHold;
        private bool reignitePending;
        private float lastReleaseAt = -10f;

        public Vector2 LocalMoveInput { get; private set; }
        public float LocalVerticalInput => LocalMoveInput.y;
        public float LocalHorizontalInput => LocalMoveInput.x;
        public bool BoostHeld => boostHeld;

        private void Reset()
        {
            jetpack = GetComponent<DMJetpackController>();
            characterTransform = transform;
        }

        private void Awake()
        {
            if (jetpack == null)
                jetpack = GetComponent<DMJetpackController>();
            if (characterTransform == null)
                characterTransform = transform;
            if (referenceCamera == null)
                referenceCamera = Camera.main;
        }

        private void Update()
        {
            TickJetpackGesture();
        }

        private void FixedUpdate()
        {
            if (jetpack == null)
                return;

            if (referenceCamera == null)
                referenceCamera = Camera.main;

            Vector2 raw = DMJetpackMoveInput.ReadPlanarRaw();
            LocalMoveInput = DMJetpackMoveInput.ToCharacterLocal(raw, characterTransform, referenceCamera);
        }

        public bool TryHandleJumpPress()
        {
            return false;
        }

        private void TickJetpackGesture()
        {
            if (jetpack == null)
            {
                boostHeld = false;
                return;
            }

            if (GameplayKeyboardShortcuts.IsGameplayInputLockedByUi())
            {
                ResetGesture(true);
                return;
            }

            // Landed: clear double-tap / reignite state so the next air needs a fresh double-tap.
            if (jetpack.Phase == DMJetpackPhase.Grounded && !jetpack.UsedJetpackThisAir)
            {
                if (waitingHoldAfterDouble || reignitePending || ignitedThisHold)
                    ResetGesture(true);
            }

            bool down = ReadJumpButtonDown();
            bool held = ReadJumpButtonHeld();
            bool up = ReadJumpButtonUp();

            if (down)
            {
                // Same air after a prior boost: single press arms reignite (1s since release), no double-tap.
                if (jetpack.UsedJetpackThisAir && !jetpack.IsBoostingNow)
                {
                    waitingHoldAfterDouble = false;
                    reignitePending = true;
                    ignitedThisHold = false;
                    secondTapDownAt = Time.unscaledTime;
                }
                else if (Time.unscaledTime - lastTapAt <= DoubleTapWindow
                         && lastReleaseAt > lastTapAt - 0.001f
                         && Time.unscaledTime - lastReleaseAt >= 0.05f)
                {
                    // Second tap after a real release within the widened window.
                    waitingHoldAfterDouble = true;
                    reignitePending = false;
                    secondTapDownAt = Time.unscaledTime;
                    ignitedThisHold = false;
                }
                else
                {
                    waitingHoldAfterDouble = false;
                    reignitePending = false;
                    ignitedThisHold = false;
                }

                lastTapAt = Time.unscaledTime;
            }

            if (waitingHoldAfterDouble && held && !ignitedThisHold)
            {
                if (Time.unscaledTime - secondTapDownAt >= HoldIgniteSeconds)
                {
                    if (jetpack.TryIgniteBoostOnJumpPress())
                        ignitedThisHold = true;
                }
            }

            // Re-boost after release: wait ReigniteCooldownSeconds from boost release, then hold Space/A.
            if (reignitePending && held && !ignitedThisHold && jetpack.UsedJetpackThisAir && !jetpack.IsBoostingNow)
            {
                if (jetpack.SecondsSinceBoostReleased >= ReigniteCooldownSeconds)
                {
                    if (jetpack.TryIgniteBoostOnJumpPress())
                    {
                        ignitedThisHold = true;
                        reignitePending = false;
                    }
                }
            }

            boostHeld = ignitedThisHold && held;
            jetpack.SetBoostHeld(boostHeld);

            if (up)
            {
                lastReleaseAt = Time.unscaledTime;
                waitingHoldAfterDouble = false;
                reignitePending = false;
                ignitedThisHold = false;
                boostHeld = false;
                jetpack.SetBoostHeld(false);
            }
        }

        private void ResetGesture(bool clearBoost)
        {
            waitingHoldAfterDouble = false;
            reignitePending = false;
            ignitedThisHold = false;
            if (!clearBoost)
                return;

            boostHeld = false;
            if (jetpack != null)
                jetpack.SetBoostHeld(false);
        }

        private static bool ReadJumpButtonDown()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                return true;
            if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
                return true;
            return false;
        }

        private static bool ReadJumpButtonHeld()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.isPressed)
                return true;
            if (Gamepad.current != null && Gamepad.current.buttonSouth.isPressed)
                return true;
            return false;
        }

        private static bool ReadJumpButtonUp()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasReleasedThisFrame)
                return true;
            if (Gamepad.current != null && Gamepad.current.buttonSouth.wasReleasedThisFrame)
                return true;
            return false;
        }
    }
}
