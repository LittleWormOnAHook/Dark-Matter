using System.Collections;
using System.Collections.Generic;
using Project.Combat;
using Project.Core;
using Project.Player;
using UnityEngine;

namespace Project.Environment.Doors
{
    /// <summary>
    /// Sci-Fi (and other) sliding or rotating double doors driven by a box trigger.
    /// Stays open while the player occupies the trigger; closes after a delay on exit.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(200)]
    public sealed class DMSlidingDoorController : MonoBehaviour
    {
        [SerializeField] private string profileId = DMSlidingDoorProfileRegistry.DefaultProfileId;
        [SerializeField] private DMSlidingDoorProfile profileOverride;

        [SerializeField] private Transform leftPanel;
        [SerializeField] private Transform rightPanel;
        [SerializeField] private Transform approachTrigger;
        [SerializeField] private Transform vfxAnchor;

        [Header("Additional panel sets (B, C, …)")]
        [SerializeField] private DMSlidingDoorPanelSet[] additionalPanelSets;

        [Header("Optional instance overrides (when profileOverride is null)")]
        [SerializeField] private DMSlidingDoorMotionMode motionMode = DMSlidingDoorMotionMode.Slide;
        [SerializeField] private DMSlidingDoorLocalAxis localAxis = DMSlidingDoorLocalAxis.Z;
        [SerializeField] private DMSlidingDoorOpenDirection openDirection = DMSlidingDoorOpenDirection.PanelSymmetric;
        [SerializeField] private float slideDistanceMeters = 1.45f;
        [SerializeField] private float rotateAngleDegrees = 90f;
        [SerializeField] private float leftPanelSign = -1f;
        [SerializeField] private float rightPanelSign = 1f;
        [SerializeField] private float moveDurationSeconds = 0.75f;
        [SerializeField] private float closeDelaySeconds = 2f;
        [SerializeField] private string playerTag = "Player";

        [Header("Optional instance VFX / audio")]
        [SerializeField] private GameObject openVfxPrefab;
        [SerializeField] private GameObject closeVfxPrefab;
        [SerializeField] private float vfxLifetimeSeconds = 2f;
        [SerializeField] private AudioClip openClip;
        [SerializeField] private AudioClip closeClip;
        [SerializeField] private float sfxVolume = 1f;

        private struct PanelPose
        {
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
        }

        private sealed class PanelSetRuntime
        {
            public Transform Left;
            public Transform Right;
            public float OpenDelaySeconds;
            public PanelPose LeftClosed;
            public PanelPose RightClosed;
            public PanelPose LeftOpen;
            public PanelPose RightOpen;
            public float Amount;
            public float Velocity;
        }

        private readonly List<PanelSetRuntime> _panelSets = new List<PanelSetRuntime>(4);

        private readonly HashSet<int> _occupantRoots = new HashSet<int>();
        private float _openAmount;
        private float _openVelocity;
        private bool _wasFullyClosed = true;
        private bool _wasOpening;
        private float _openSequenceStartTime;
        private Coroutine _closeDelayRoutine;
        private AudioSource _audioSource;
        private float _lastOpenFeedbackTime = -999f;
        private float _lastCloseFeedbackTime = -999f;

        private const float FeedbackDebounceSeconds = 0.4f;
        private const float MotionFeedbackThreshold = 0.02f;

        private DMSlidingDoorProfile ActiveProfile =>
            profileOverride != null
                ? profileOverride
                : DMSlidingDoorProfileRegistry.Resolve(profileId);

        private void Awake()
        {
            if (!ResolveSingleControllerForDoor())
                return;

            AutoWireCoreIfNeeded();
            RebuildPanelSets();
            EnsurePanelsRuntimeMovable();
            CacheClosedPoses();
            ComputeOpenPoses();
            EnsureAudioSource();
            EnsureTriggerRelay();
            EnsureTriggerPhysics();
            DisableLegacyKitTriggerDrivers();
            DisableLegacyDoorAnimations();
        }

        private void Start()
        {
            EnsureTriggerRelay();
            EnsureTriggerPhysics();
            RebuildPanelSets();
            CacheClosedPoses();
            ComputeOpenPoses();
        }

        private void OnValidate()
        {
            if (leftPanel == null || rightPanel == null)
                AutoWireCoreIfNeeded();
        }

        private void Update()
        {
            float target = _occupantRoots.Count > 0 || _closeDelayRoutine != null ? 1f : 0f;
            bool opening = target > 0.5f;
            if (opening && !_wasOpening)
                _openSequenceStartTime = Time.time;

            float duration = GetMoveDuration();
            float smoothTime = duration * 0.35f;
            _openAmount = Mathf.SmoothDamp(_openAmount, target, ref _openVelocity, smoothTime);

            for (int i = 0; i < _panelSets.Count; i++)
            {
                PanelSetRuntime set = _panelSets[i];
                float setTarget = target;
                if (opening && set.OpenDelaySeconds > 0f)
                {
                    setTarget = Time.time >= _openSequenceStartTime + set.OpenDelaySeconds ? 1f : 0f;
                }

                set.Amount = Mathf.SmoothDamp(set.Amount, setTarget, ref set.Velocity, smoothTime);
            }

            if (opening && _wasFullyClosed && _openAmount > MotionFeedbackThreshold)
                TryPlayOpenFeedback();

            if (!opening && _wasOpening && _openAmount > MotionFeedbackThreshold)
                TryPlayCloseFeedback();

            _wasFullyClosed = _openAmount <= MotionFeedbackThreshold;
            _wasOpening = opening;
        }

        private void LateUpdate()
        {
            if (_panelSets.Count == 0)
            {
                ApplyPose(leftPanel, default, default, _openAmount);
                return;
            }

            for (int i = 0; i < _panelSets.Count; i++)
            {
                PanelSetRuntime set = _panelSets[i];
                float amount = i == 0 ? _openAmount : set.Amount;
                ApplyPose(set.Left, set.LeftClosed, set.LeftOpen, amount);
                ApplyPose(set.Right, set.RightClosed, set.RightOpen, amount);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (ControllerHostsApproachTrigger())
                NotifyPlayerEntered(other);
        }

        private void OnTriggerStay(Collider other)
        {
            if (ControllerHostsApproachTrigger())
                NotifyPlayerStay(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (ControllerHostsApproachTrigger())
                NotifyPlayerExited(other);
        }

        internal void NotifyPlayerEntered(Collider other)
        {
            if (!IsPlayerCollider(other))
                return;

            if (!_occupantRoots.Add(GetPlayerRootId(other)))
            {
                CancelCloseDelay();
                return;
            }

            CancelCloseDelay();
        }

        internal void NotifyPlayerStay(Collider other)
        {
            if (!IsPlayerCollider(other))
                return;

            if (_occupantRoots.Contains(GetPlayerRootId(other)))
                CancelCloseDelay();
        }

        internal void NotifyPlayerExited(Collider other)
        {
            if (!IsPlayerCollider(other))
                return;

            if (!_occupantRoots.Remove(GetPlayerRootId(other)))
                return;

            if (_occupantRoots.Count == 0)
                ScheduleCloseDelay();
        }

        private void ScheduleCloseDelay()
        {
            CancelCloseDelay();
            _closeDelayRoutine = StartCoroutine(CloseAfterDelay());
        }

        private IEnumerator CloseAfterDelay()
        {
            yield return new WaitForSeconds(GetCloseDelay());
            _closeDelayRoutine = null;
        }

        private void CancelCloseDelay()
        {
            if (_closeDelayRoutine != null)
            {
                StopCoroutine(_closeDelayRoutine);
                _closeDelayRoutine = null;
            }
        }

        private bool IsPlayerCollider(Collider other)
        {
            if (other == null)
                return false;

            // Foot sensors, head tracks, etc. — only the solid body capsule should drive the door.
            if (other.isTrigger)
                return false;

            string tag = GetPlayerTag();
            if (!string.IsNullOrEmpty(tag))
            {
                if (other.CompareTag(tag))
                    return true;

                Transform root = other.transform.root;
                if (root != null && root.CompareTag(tag))
                    return true;
            }

            if (other.GetComponentInParent<PlayerController>() != null)
                return true;

            Transform player = PlayerReference.ResolveTransform();
            return player != null && other.transform.IsChildOf(player);
        }

        private static int GetPlayerRootId(Collider other)
        {
            if (other == null)
                return 0;

            Transform root = other.transform.root;
            return root != null
                ? root.GetEntityId().GetHashCode()
                : other.GetEntityId().GetHashCode();
        }

        private Transform ResolveDoorHierarchyRoot()
        {
            Transform best = transform;
            Transform walk = transform;
            while (walk != null)
            {
                if (FindChildByName(walk, "DOOR Horizontal") != null
                    || FindChildByName(walk, "Door_Big_TRIGGER") != null)
                {
                    best = walk;
                }

                if (walk.name.IndexOf("DOOR_Horz", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    best = walk;

                walk = walk.parent;
            }

            return best;
        }

        private bool ResolveSingleControllerForDoor()
        {
            Transform doorRoot = ResolveDoorHierarchyRoot();
            DMSlidingDoorController[] all =
                doorRoot.GetComponentsInChildren<DMSlidingDoorController>(true);

            if (all.Length <= 1)
                return true;

            DMSlidingDoorController keep = null;
            for (int i = 0; i < all.Length; i++)
            {
                DMSlidingDoorController candidate = all[i];
                if (candidate == null)
                    continue;

                if (IsApproachTriggerName(candidate.gameObject.name))
                    continue;

                keep = candidate;
                break;
            }

            if (keep == null)
            {
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] != null)
                    {
                        keep = all[i];
                        break;
                    }
                }
            }

            if (keep == null)
                keep = this;

            if (keep != this)
            {
                enabled = false;
                return false;
            }

            for (int i = 0; i < all.Length; i++)
            {
                DMSlidingDoorController other = all[i];
                if (other == null || other == this)
                    continue;

                other.enabled = false;
            }

            return true;
        }

        private void EnsurePanelsRuntimeMovable()
        {
            for (int i = 0; i < _panelSets.Count; i++)
            {
                PanelSetRuntime set = _panelSets[i];
                ClearStaticForMotion(set.Left);
                ClearStaticForMotion(set.Right);
            }
        }

        private void RebuildPanelSets()
        {
            _panelSets.Clear();

            if (leftPanel != null || rightPanel != null)
            {
                _panelSets.Add(new PanelSetRuntime
                {
                    Left = leftPanel,
                    Right = rightPanel,
                    OpenDelaySeconds = 0f
                });
            }

            if (additionalPanelSets == null)
                return;

            DMSlidingDoorProfile profile = ActiveProfile;
            for (int i = 0; i < additionalPanelSets.Length; i++)
            {
                DMSlidingDoorPanelSet entry = additionalPanelSets[i];
                if (entry == null)
                    continue;

                if (entry.leftPanel == null && entry.rightPanel == null)
                    continue;

                float delay = entry.openDelaySeconds;
                if (delay <= 0f
                    && profile != null
                    && profile.additionalSetTimings != null
                    && i < profile.additionalSetTimings.Length)
                {
                    delay = profile.additionalSetTimings[i].openDelaySeconds;
                }

                _panelSets.Add(new PanelSetRuntime
                {
                    Left = entry.leftPanel,
                    Right = entry.rightPanel,
                    OpenDelaySeconds = Mathf.Max(0f, delay)
                });
            }
        }

        private static void ClearStaticForMotion(Transform panel)
        {
            if (panel == null)
                return;

            Transform[] transforms = panel.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform t = transforms[i];
                if (t == null)
                    continue;

                t.gameObject.isStatic = false;
            }
        }

        private void DisableLegacyDoorAnimations()
        {
            Transform[] all = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == null)
                    continue;

                Animation animation = t.GetComponent<Animation>();
                if (animation != null)
                    animation.enabled = false;

                Animator animator = t.GetComponent<Animator>();
                if (animator != null && animator.gameObject != gameObject)
                    animator.enabled = false;
            }
        }

        private void TryPlayOpenFeedback()
        {
            if (Time.time - _lastOpenFeedbackTime < FeedbackDebounceSeconds)
                return;

            _lastOpenFeedbackTime = Time.time;
            PlayDoorSfx(true);
            SpawnDoorVfx(GetOpenVfx());
        }

        private void TryPlayCloseFeedback()
        {
            if (Time.time - _lastCloseFeedbackTime < FeedbackDebounceSeconds)
                return;

            _lastCloseFeedbackTime = Time.time;
            PlayDoorSfx(false);
            SpawnDoorVfx(GetCloseVfx());
        }

        private void PlayDoorSfx(bool opening)
        {
            AudioClip clip = opening ? GetOpenClip() : GetCloseClip();
            if (clip == null || _audioSource == null)
                return;

            float master = GameSettings.MasterVolume * GameSettings.SfxVolume * GetSfxVolume();
            _audioSource.PlayOneShot(clip, master);
        }

        private void SpawnDoorVfx(GameObject prefab)
        {
            if (prefab == null)
                return;

            Transform anchor = vfxAnchor != null ? vfxAnchor : approachTrigger != null ? approachTrigger : transform;
            Vector3 offset = ActiveProfile != null ? ActiveProfile.vfxLocalOffset : Vector3.zero;
            Vector3 worldPos = anchor.TransformPoint(offset);
            Quaternion worldRot = anchor.rotation;

            GameObject instance = PoolManager.Spawn(prefab, worldPos, worldRot);
            if (instance == null)
                return;

            CombatVfxUtility.PreparePooledOneShotVfx(instance, GetVfxLifetime());
        }

        private void CacheClosedPoses()
        {
            for (int i = 0; i < _panelSets.Count; i++)
            {
                PanelSetRuntime set = _panelSets[i];
                if (set.Left != null)
                {
                    set.LeftClosed.LocalPosition = set.Left.localPosition;
                    set.LeftClosed.LocalRotation = set.Left.localRotation;
                }

                if (set.Right != null)
                {
                    set.RightClosed.LocalPosition = set.Right.localPosition;
                    set.RightClosed.LocalRotation = set.Right.localRotation;
                }
            }
        }

        private void ComputeOpenPoses()
        {
            DMSlidingDoorProfile profile = ActiveProfile;
            DMSlidingDoorMotionMode mode = profile != null ? profile.motionMode : motionMode;
            DMSlidingDoorLocalAxis axis = profile != null ? profile.localAxis : localAxis;
            Vector3 axisVector = DMSlidingDoorProfile.AxisVector(axis);

            float leftSign;
            float rightSign;
            if (profile != null)
                profile.ResolvePanelSigns(out leftSign, out rightSign);
            else
                ResolveInstancePanelSigns(out leftSign, out rightSign);

            float slideDist = profile != null ? profile.slideDistanceMeters : slideDistanceMeters;
            float angle = profile != null ? profile.rotateAngleDegrees : rotateAngleDegrees;

            for (int i = 0; i < _panelSets.Count; i++)
            {
                PanelSetRuntime set = _panelSets[i];
                set.LeftOpen = set.LeftClosed;
                set.RightOpen = set.RightClosed;

                if (mode == DMSlidingDoorMotionMode.Rotate)
                {
                    if (set.Left != null)
                    {
                        set.LeftOpen.LocalRotation = set.LeftClosed.LocalRotation
                            * Quaternion.AngleAxis(angle * leftSign, axisVector);
                    }

                    if (set.Right != null)
                    {
                        set.RightOpen.LocalRotation = set.RightClosed.LocalRotation
                            * Quaternion.AngleAxis(angle * rightSign, axisVector);
                    }
                }
                else
                {
                    if (set.Left != null)
                    {
                        set.LeftOpen.LocalPosition = set.LeftClosed.LocalPosition
                            + axisVector * (slideDist * leftSign);
                    }

                    if (set.Right != null)
                    {
                        set.RightOpen.LocalPosition = set.RightClosed.LocalPosition
                            + axisVector * (slideDist * rightSign);
                    }
                }
            }
        }

        private void ResolveInstancePanelSigns(out float leftSign, out float rightSign)
        {
            switch (openDirection)
            {
                case DMSlidingDoorOpenDirection.SameDirection:
                    leftSign = 1f;
                    rightSign = 1f;
                    break;
                case DMSlidingDoorOpenDirection.Custom:
                    leftSign = leftPanelSign;
                    rightSign = rightPanelSign;
                    break;
                default:
                    leftSign = -1f;
                    rightSign = 1f;
                    break;
            }
        }

        private static void ApplyPose(Transform panel, PanelPose closed, PanelPose open, float t)
        {
            if (panel == null)
                return;

            panel.localPosition = Vector3.Lerp(closed.LocalPosition, open.LocalPosition, t);
            panel.localRotation = Quaternion.Slerp(closed.LocalRotation, open.LocalRotation, t);
        }

        private void AutoWireCoreIfNeeded()
        {
            if (approachTrigger == null)
            {
                if (IsApproachTriggerName(gameObject.name))
                    approachTrigger = transform;
                else
                {
                    Transform t = FindChildByName(transform, "Door_Big_TRIGGER");
                    if (t != null)
                        approachTrigger = t;
                }
            }
            else if (approachTrigger.GetComponent<Collider>() == null
                && !IsApproachTriggerName(approachTrigger.name))
            {
                Transform t = FindChildByName(transform, "Door_Big_TRIGGER");
                if (t != null)
                    approachTrigger = t;
            }

            if (leftPanel == null)
                leftPanel = FindHorizLeaf('A', preferDuplicateSuffix: false);

            if (rightPanel == null)
                rightPanel = FindHorizLeaf('A', preferDuplicateSuffix: true);
        }

#if UNITY_EDITOR
        /// <summary>Editor-only: discovers B/C/D leaves and expands Additional Panel Sets. Never called at runtime.</summary>
        public void EditorAutoWireAdditionalPanelSets()
        {
            Transform doorHorizontal = FindChildByName(transform, "DOOR Horizontal");
            if (doorHorizontal == null)
                return;

            char[] letters = { 'B', 'C', 'D' };
            int needed = 0;
            for (int i = 0; i < letters.Length; i++)
            {
                if (FindHorizLeaf(letters[i], false) != null || FindHorizLeaf(letters[i], true) != null)
                    needed = i + 1;
            }

            if (needed == 0)
                return;

            if (additionalPanelSets == null || additionalPanelSets.Length < needed)
            {
                var resized = new DMSlidingDoorPanelSet[needed];
                if (additionalPanelSets != null)
                {
                    for (int i = 0; i < additionalPanelSets.Length && i < needed; i++)
                        resized[i] = additionalPanelSets[i];
                }

                for (int i = 0; i < needed; i++)
                {
                    if (resized[i] == null)
                        resized[i] = new DMSlidingDoorPanelSet();
                }

                additionalPanelSets = resized;
            }

            for (int i = 0; i < needed; i++)
            {
                DMSlidingDoorPanelSet set = additionalPanelSets[i];
                if (set == null)
                {
                    set = new DMSlidingDoorPanelSet();
                    additionalPanelSets[i] = set;
                }

                char letter = letters[i];
                if (set.leftPanel == null)
                    set.leftPanel = FindHorizLeaf(letter, preferDuplicateSuffix: false);
                if (set.rightPanel == null)
                    set.rightPanel = FindHorizLeaf(letter, preferDuplicateSuffix: true);
            }
        }
#endif

        private Transform FindHorizLeaf(char setLetter, bool preferDuplicateSuffix)
        {
            Transform doorHorizontal = FindChildByName(transform, "DOOR Horizontal");
            if (doorHorizontal != null)
            {
                Transform fromGroup = ScanHorizLeaves(doorHorizontal, setLetter, preferDuplicateSuffix);
                if (fromGroup != null)
                    return fromGroup;
            }

            return ScanHorizLeaves(transform, setLetter, preferDuplicateSuffix);
        }

        private static Transform FindChildByName(Transform root, string exactName)
        {
            if (root == null)
                return null;

            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t != null && t.name == exactName)
                    return t;
            }

            return null;
        }

        private static Transform ScanHorizLeaves(Transform root, char setLetter, bool preferDuplicateSuffix)
        {
            string leftName = "DoorHoriz_" + setLetter;
            string rightName = leftName + " (1)";

            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            Transform best = null;
            float bestDepth = float.MaxValue;

            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == null)
                    continue;

                if (preferDuplicateSuffix)
                {
                    if (t.name != rightName)
                        continue;
                }
                else
                {
                    if (t.name != leftName)
                        continue;
                }

                float depth = GetTransformDepth(t, root);
                if (depth < bestDepth)
                {
                    bestDepth = depth;
                    best = t;
                }
            }

            return best;
        }

        private static float GetTransformDepth(Transform t, Transform stopAt)
        {
            float depth = 0f;
            Transform walk = t;
            while (walk != null && walk != stopAt)
            {
                depth += 1f;
                walk = walk.parent;
            }

            return depth;
        }

        private void EnsureAudioSource()
        {
            _audioSource = GetComponentInChildren<AudioSource>();
            if (_audioSource != null)
                return;

            GameObject audioGo = new GameObject("DoorAudio");
            audioGo.transform.SetParent(transform, false);
            _audioSource = audioGo.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 1f;
            _audioSource.minDistance = 2f;
            _audioSource.maxDistance = 25f;
        }

        private bool ControllerHostsApproachTrigger()
        {
            return approachTrigger == null || approachTrigger == transform;
        }

        private static bool IsApproachTriggerName(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
                return false;

            return objectName.IndexOf("Door_Big_TRIGGER", System.StringComparison.OrdinalIgnoreCase) >= 0
                || objectName.IndexOf("Door_big_trigger", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void EnsureTriggerRelay()
        {
            if (approachTrigger == null)
                return;

            if (ControllerHostsApproachTrigger())
            {
                DMSlidingDoorTriggerRelay stray = GetComponent<DMSlidingDoorTriggerRelay>();
                if (stray != null)
                    DestroyRelay(stray);
                return;
            }

            DMSlidingDoorTriggerRelay relay = approachTrigger.GetComponent<DMSlidingDoorTriggerRelay>();
            if (relay == null)
                relay = approachTrigger.gameObject.AddComponent<DMSlidingDoorTriggerRelay>();

            relay.Bind(this);

            DMSlidingDoorTriggerRelay onController = GetComponent<DMSlidingDoorTriggerRelay>();
            if (onController != null && onController != relay)
                DestroyRelay(onController);
        }

        private static void DestroyRelay(DMSlidingDoorTriggerRelay relay)
        {
            if (relay == null)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(relay);
                return;
            }
#endif
            Object.Destroy(relay);
        }

        private void EnsureTriggerPhysics()
        {
            Transform host = approachTrigger != null ? approachTrigger : transform;
            Collider zone = host.GetComponent<Collider>();
            if (zone != null)
                zone.isTrigger = true;

            Rigidbody rb = host.GetComponent<Rigidbody>();
            if (rb == null)
                rb = host.gameObject.AddComponent<Rigidbody>();

            rb.isKinematic = true;
            rb.useGravity = false;
        }

        private void DisableLegacyKitTriggerDrivers()
        {
            Transform host = approachTrigger != null ? approachTrigger : transform;
            MonoBehaviour[] behaviours = host.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || behaviour == this)
                    continue;

                if (behaviour is DMSlidingDoorTriggerRelay)
                    continue;

                System.Type type = behaviour.GetType();
                if (type.Assembly == typeof(DMSlidingDoorController).Assembly)
                    continue;

                if (HasLegacyDoorTriggerFields(type))
                    behaviour.enabled = false;
            }
        }

        private static bool HasLegacyDoorTriggerFields(System.Type type)
        {
            return type.GetField("triggerCount") != null
                && type.GetField("repeatTrigger") != null
                && type.GetField("action") != null;
        }

        private float GetMoveDuration()
        {
            DMSlidingDoorProfile p = ActiveProfile;
            return p != null ? p.moveDurationSeconds : moveDurationSeconds;
        }

        private float GetCloseDelay()
        {
            DMSlidingDoorProfile p = ActiveProfile;
            return p != null ? p.closeDelaySeconds : closeDelaySeconds;
        }

        private string GetPlayerTag()
        {
            DMSlidingDoorProfile p = ActiveProfile;
            return p != null && !string.IsNullOrEmpty(p.playerTag) ? p.playerTag : playerTag;
        }

        private GameObject GetOpenVfx() =>
            openVfxPrefab != null ? openVfxPrefab : ActiveProfile != null ? ActiveProfile.openVfxPrefab : null;

        private GameObject GetCloseVfx() =>
            closeVfxPrefab != null ? closeVfxPrefab : ActiveProfile != null ? ActiveProfile.closeVfxPrefab : null;

        private float GetVfxLifetime()
        {
            if (vfxLifetimeSeconds > 0.05f)
                return vfxLifetimeSeconds;
            return ActiveProfile != null ? ActiveProfile.vfxLifetimeSeconds : 2f;
        }

        private AudioClip GetOpenClip() =>
            openClip != null ? openClip : ActiveProfile != null ? ActiveProfile.openClip : null;

        private AudioClip GetCloseClip() =>
            closeClip != null ? closeClip : ActiveProfile != null ? ActiveProfile.closeClip : null;

        private float GetSfxVolume()
        {
            if (profileOverride != null)
                return profileOverride.sfxVolume;
            if (ActiveProfile != null)
                return ActiveProfile.sfxVolume;
            return sfxVolume;
        }

#if UNITY_EDITOR
        public void EditorRebindAndRecache(bool wireAdditionalSets = false)
        {
            AutoWireCoreIfNeeded();
            if (wireAdditionalSets)
                EditorAutoWireAdditionalPanelSets();
            RebuildPanelSets();
            EnsurePanelsRuntimeMovable();
            CacheClosedPoses();
            ComputeOpenPoses();
            EnsureTriggerRelay();
            EnsureTriggerPhysics();
            DisableLegacyKitTriggerDrivers();
            DisableLegacyDoorAnimations();
        }
#endif
    }
}
