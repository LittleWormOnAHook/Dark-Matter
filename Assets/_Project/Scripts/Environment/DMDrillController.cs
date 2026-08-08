using Project.Core;
using Project.Interaction;
using UnityEngine;

namespace Project.Environment
{
    /// <summary>
    /// Proximity drill hatch: open on approach, hold open while near, seal when inside,
    /// close immediately when the player leaves the area. Re-opens when exiting through the doorway.
    /// Bits spin while near/inside.
    /// </summary>
    [DisallowMultipleComponent]
    public class DMDrillController : MonoBehaviour
    {
        public const string DoorOpenBool = "DoorOpen";
        public const string IsDrillingBool = "IsDrilling";

        private enum HatchPhase
        {
            Away,
            OpenForEntry,
            SealedInside,
            OpenForExit
        }

        [Header("References")]
        [SerializeField] private Animator drillAnimator;
        [SerializeField] private Collider approachTrigger;
        [SerializeField] private Collider interiorTrigger;
        [SerializeField] private Collider outerBoundaryTrigger;
        [SerializeField] private Collider doorCollider;

        [Header("Zones (local space on drill root)")]
        [Tooltip("Outside + doorway only. Must not cover deep cabin. Edit here — values push to DoorApproachTrigger on save/validate.")]
        [SerializeField] private Vector3 approachCenter = new Vector3(2.2f, 1.1f, 0f);
        [SerializeField] private Vector3 approachSize = new Vector3(1.6f, 2.6f, 1.6f);
        [Tooltip("Deep interior only — must NOT cover the doorway. Edit here — values push to DrillInteriorTrigger on save/validate.")]
        [SerializeField] private Vector3 interiorCenter = new Vector3(0.05f, 1.1f, 0f);
        [SerializeField] private Vector3 interiorSize = new Vector3(1.5f, 2.6f, 1.4f);
        [Tooltip("Whole drill + approach footprint. Edit here — values push to DrillOuterBoundary on save/validate.")]
        [SerializeField] private Vector3 outerBoundaryCenter = new Vector3(1.1f, 1.1f, 0f);
        [SerializeField] private Vector3 outerBoundarySize = new Vector3(6f, 3.5f, 3.2f);

        [Header("Presence / Latch")]
        [Tooltip("Grace after leaving a zone so CharacterController jitter does not flap the hatch.")]
        [SerializeField] private float presenceGraceSeconds = 0.25f;
        [Tooltip("Once opened from approach/exit, keep DoorOpen at least this long before sealing / timed-close while still near. Leaving the area always closes immediately (after presence grace).")]
        [SerializeField] private float openMinHoldSeconds = 5f;
        [Tooltip("Must remain in deep interior this long (after the open hold) before the hatch seals closed.")]
        [SerializeField] private float sealDwellSeconds = 0.15f;
        [SerializeField] private float playerSampleHeight = 1.0f;

        [Header("Audio — Door")]
        [SerializeField] private AudioClip doorOpenClip;
        [SerializeField] private AudioClip doorCloseClip;
        [SerializeField] private AudioSource doorAudioSource;
        [SerializeField] [Range(0f, 1f)] private float doorVolume = 0.85f;

        [Header("Audio — Drill Bits (loop while spinning)")]
        [SerializeField] private AudioClip upperDrillLoopClip;
        [SerializeField] private AudioClip lowerDrillLoopClip;
        [SerializeField] private AudioSource upperDrillAudioSource;
        [SerializeField] private AudioSource lowerDrillAudioSource;
        [SerializeField] [Range(0f, 1f)] private float upperDrillVolume = 0.55f;
        [SerializeField] [Range(0f, 1f)] private float lowerDrillVolume = 0.65f;
        [SerializeField] private float upperDrillPitch = 1.15f;
        [SerializeField] private float lowerDrillPitch = 0.9f;
        [SerializeField] private float drillAudioMinDistance = 2f;
        [SerializeField] private float drillAudioMaxDistance = 28f;

        private HatchPhase phase = HatchPhase.Away;
        private bool playerInApproach;
        private bool playerInInterior;
        private bool playerInOuterBoundary;
        private float approachLostAt = -1f;
        private float interiorLostAt = -1f;
        private float outerBoundaryLostAt = -1f;
        private float interiorEnteredAt = -1f;
        private float openLatchedAt = -1f;
        /// <summary>
        /// After a timed close while still standing in approach, keep the hatch sealed
        /// until the player fully clears both zones (prevents instant reopen).
        /// </summary>
        private bool reopenBlockedUntilClear;
        private bool doorOpen;
        private bool drilling;

        public bool IsDoorOpen => doorOpen;
        public bool IsDrilling => drilling;

        private void Awake()
        {
            ResolveReferences();
            EnsureTriggers();
            EnsureOuterBoundaryEnclosesInnerZones();
            ApplyZoneSettingsToColliders();
            EnsureAudioSources();
        }

        private void OnEnable()
        {
            phase = HatchPhase.Away;
            playerInApproach = false;
            playerInInterior = false;
            playerInOuterBoundary = false;
            approachLostAt = -1f;
            interiorLostAt = -1f;
            outerBoundaryLostAt = -1f;
            interiorEnteredAt = -1f;
            openLatchedAt = -1f;
            reopenBlockedUntilClear = false;
            ApplyOutputs(force: true);
        }

        private void OnDisable()
        {
            StopDrillLoops();
        }

        private void Start()
        {
            ResolveReferences();
            EnsureTriggers();
            EnsureAudioSources();
            ApplyOutputs(force: true);
        }

        private void FixedUpdate()
        {
            SamplePresence();
            UpdatePhase();
            ApplyOutputs(force: false);
        }

        private void ResolveReferences()
        {
            if (drillAnimator == null)
                drillAnimator = GetComponent<Animator>();
            if (drillAnimator == null)
                drillAnimator = GetComponentInChildren<Animator>();

            if (doorCollider == null)
            {
                Transform door = transform.Find("Door");
                if (door != null)
                    doorCollider = door.GetComponent<Collider>();
            }
        }

        private void EnsureTriggers()
        {
            approachTrigger = EnsureZone(
                "DoorApproachTrigger",
                approachTrigger,
                approachCenter,
                approachSize,
                DrillZoneKind.Approach);

            interiorTrigger = EnsureZone(
                "DrillInteriorTrigger",
                interiorTrigger,
                interiorCenter,
                interiorSize,
                DrillZoneKind.Interior);

            outerBoundaryTrigger = EnsureZone(
                "DrillOuterBoundary",
                outerBoundaryTrigger,
                outerBoundaryCenter,
                outerBoundarySize,
                DrillZoneKind.OuterBoundary);

            Transform legacyButton = transform.Find("DrillStartButton");
            if (legacyButton != null)
                legacyButton.gameObject.SetActive(false);
        }

        /// <summary>
        /// Ensures zone hosts/relays exist. Creates missing BoxColliders only (seeds center/size once).
        /// Serialized zone fields on this component are pushed to child colliders at startup and in editor OnValidate.
        /// </summary>
        private Collider EnsureZone(
            string childName,
            Collider existing,
            Vector3 center,
            Vector3 size,
            DrillZoneKind zoneKind)
        {
            Transform child = existing != null ? existing.transform : transform.Find(childName);
            GameObject host;
            if (child == null)
            {
                host = new GameObject(childName);
                host.transform.SetParent(transform, false);
                host.transform.localPosition = Vector3.zero;
                host.transform.localRotation = Quaternion.identity;
                host.transform.localScale = Vector3.one;
            }
            else
            {
                host = child.gameObject;
            }

            BoxCollider box = host.GetComponent<BoxCollider>();
            bool createdBox = box == null;
            if (createdBox)
                box = host.AddComponent<BoxCollider>();

            box.isTrigger = true;
            if (createdBox)
            {
                // First-time zone only — seed from serialized defaults.
                box.center = center;
                box.size = size;
            }

            DMDrillZoneRelay relay = host.GetComponent<DMDrillZoneRelay>();
            if (relay == null)
                relay = host.AddComponent<DMDrillZoneRelay>();
            relay.Configure(this, zoneKind);

            return box;
        }

        private void ApplyZoneSettingsToColliders()
        {
            ApplyZoneToCollider(approachTrigger, approachCenter, approachSize);
            ApplyZoneToCollider(interiorTrigger, interiorCenter, interiorSize);
            ApplyZoneToCollider(outerBoundaryTrigger, outerBoundaryCenter, outerBoundarySize);
        }

        /// <summary>
        /// Expands (never shrinks) the outer boundary so it always contains approach + interior.
        /// Prevents a stale/misplaced outer box from blocking DoorOpen / IsDrilling entirely.
        /// </summary>
        private void EnsureOuterBoundaryEnclosesInnerZones(float margin = 0.5f)
        {
            Vector3 approachHalf = approachSize * 0.5f;
            Vector3 interiorHalf = interiorSize * 0.5f;
            Vector3 innerMin = Vector3.Min(approachCenter - approachHalf, interiorCenter - interiorHalf);
            Vector3 innerMax = Vector3.Max(approachCenter + approachHalf, interiorCenter + interiorHalf);
            Vector3 requiredMin = innerMin - Vector3.one * margin;
            Vector3 requiredMax = innerMax + Vector3.one * margin;

            Vector3 outerHalf = outerBoundarySize * 0.5f;
            Vector3 outerMin = outerBoundaryCenter - outerHalf;
            Vector3 outerMax = outerBoundaryCenter + outerHalf;

            Vector3 mergedMin = Vector3.Min(outerMin, requiredMin);
            Vector3 mergedMax = Vector3.Max(outerMax, requiredMax);

            outerBoundaryCenter = (mergedMin + mergedMax) * 0.5f;
            outerBoundarySize = Vector3.Max(mergedMax - mergedMin, new Vector3(0.2f, 0.2f, 0.2f));
        }

        private static void ApplyZoneToCollider(Collider zone, Vector3 center, Vector3 size)
        {
            if (zone is not BoxCollider box)
                return;

            box.center = center;
            box.size = Vector3.Max(size, new Vector3(0.2f, 0.2f, 0.2f));
        }

        private void SamplePresence()
        {
            bool approachNow = IsPlayerOverlapping(approachTrigger);
            bool interiorNow = IsPlayerOverlapping(interiorTrigger);
            bool outerNow = IsPlayerOverlapping(outerBoundaryTrigger);
            float now = Time.time;

            if (outerNow)
            {
                playerInOuterBoundary = true;
                outerBoundaryLostAt = -1f;
            }
            else if (playerInOuterBoundary)
            {
                if (outerBoundaryLostAt < 0f)
                    outerBoundaryLostAt = now;
                else if (now - outerBoundaryLostAt >= presenceGraceSeconds)
                    playerInOuterBoundary = false;
            }

            if (approachNow)
            {
                playerInApproach = true;
                approachLostAt = -1f;
            }
            else if (playerInApproach)
            {
                if (approachLostAt < 0f)
                    approachLostAt = now;
                else if (now - approachLostAt >= presenceGraceSeconds)
                    playerInApproach = false;
            }

            if (interiorNow)
            {
                if (!playerInInterior)
                    interiorEnteredAt = now;
                playerInInterior = true;
                interiorLostAt = -1f;
            }
            else if (playerInInterior)
            {
                if (interiorLostAt < 0f)
                    interiorLostAt = now;
                else if (now - interiorLostAt >= presenceGraceSeconds)
                {
                    playerInInterior = false;
                    interiorEnteredAt = -1f;
                }
            }
            else
            {
                interiorEnteredAt = -1f;
            }

            // Inner zones must live inside the outer footprint — a mis-sized outer box must not
            // trap the phase machine in Away while the player is at the doorway or inside.
            if (playerInApproach || playerInInterior)
            {
                playerInOuterBoundary = true;
                outerBoundaryLostAt = -1f;
            }
        }

        private bool IsPlayerOverlapping(Collider zone)
        {
            if (zone == null || !zone.enabled)
                return false;

            GameObject player = null;
            if (!PlayerInteractionUtility.TryGetPlayerPosition(out Vector3 feet))
            {
                player = PlayerLocator.FindPlayerObject();
                if (player == null)
                    return false;
                feet = player.transform.position;
            }
            else
            {
                player = PlayerLocator.FindPlayerObject();
            }

            // Prefer oriented box tests — world AABB from a rotated BoxCollider is too fat
            // and can keep inArea true after the player thinks they have left.
            float height = Mathf.Max(0.2f, playerSampleHeight);
            if (PointInZone(zone, feet))
                return true;
            if (PointInZone(zone, feet + Vector3.up * (height * 0.25f)))
                return true;
            if (PointInZone(zone, feet + Vector3.up * (height * 0.5f)))
                return true;
            if (PointInZone(zone, feet + Vector3.up * (height * 0.75f)))
                return true;
            if (PointInZone(zone, feet + Vector3.up * height))
                return true;

            // CharacterController / capsule sampling (Invector may not expose CC on root).
            if (player != null)
            {
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc == null)
                    cc = player.GetComponentInChildren<CharacterController>();
                if (cc != null)
                {
                    Vector3 worldCenter = player.transform.TransformPoint(cc.center);
                    if (PointInZone(zone, worldCenter))
                        return true;
                    float r = cc.radius * 0.85f;
                    Vector3 right = player.transform.right * r;
                    Vector3 forward = player.transform.forward * r;
                    if (PointInZone(zone, worldCenter + right) || PointInZone(zone, worldCenter - right))
                        return true;
                    if (PointInZone(zone, worldCenter + forward) || PointInZone(zone, worldCenter - forward))
                        return true;
                }
            }

            return false;
        }

        private static bool PointInZone(Collider zone, Vector3 worldPoint)
        {
            if (zone is BoxCollider box)
            {
                Vector3 local = box.transform.InverseTransformPoint(worldPoint) - box.center;
                Vector3 half = box.size * 0.5f;
                // Tiny epsilon so ground feet near the box floor still count.
                const float eps = 0.05f;
                return Mathf.Abs(local.x) <= half.x + eps
                    && Mathf.Abs(local.y) <= half.y + eps
                    && Mathf.Abs(local.z) <= half.z + eps;
            }

            // Fallback for non-box colliders.
            return zone.bounds.Contains(worldPoint);
        }

        private bool InteriorSealReady
        {
            get
            {
                if (!playerInInterior || interiorEnteredAt < 0f)
                    return false;
                return Time.time - interiorEnteredAt >= sealDwellSeconds;
            }
        }

        private bool OpenHoldElapsed
        {
            get
            {
                if (openLatchedAt < 0f)
                    return true;
                return Time.time - openLatchedAt >= openMinHoldSeconds;
            }
        }

        private void UpdatePhase()
        {
            // Outer boundary is authoritative for leaving the drill footprint.
            if (!playerInOuterBoundary)
            {
                phase = HatchPhase.Away;
                openLatchedAt = -1f;
                reopenBlockedUntilClear = false;
                return;
            }

            bool inInnerZones = playerInApproach || playerInInterior;

            switch (phase)
            {
                case HatchPhase.Away:
                    // Timed-close suppress: stay sealed until the player fully clears inner zones.
                    if (reopenBlockedUntilClear)
                    {
                        if (!inInnerZones)
                            reopenBlockedUntilClear = false;
                        else
                            break;
                    }

                    // Always open on approach so the timed hold can run; never skip straight to sealed.
                    if (playerInApproach || playerInInterior)
                    {
                        phase = HatchPhase.OpenForEntry;
                        openLatchedAt = Time.time;
                    }
                    break;

                case HatchPhase.OpenForEntry:
                    // Player left approach + interior: always close. Hold must not keep the hatch open.
                    if (!inInnerZones)
                    {
                        phase = HatchPhase.Away;
                        openLatchedAt = -1f;
                        reopenBlockedUntilClear = false;
                        break;
                    }

                    // Timed hold only while still near — prevents premature seal on brief flicker.
                    if (!OpenHoldElapsed)
                        break;

                    if (InteriorSealReady)
                    {
                        phase = HatchPhase.SealedInside;
                        openLatchedAt = -1f;
                    }
                    else if (!playerInInterior)
                    {
                        // Still only near the doorway after the hold — close and suppress reopen
                        // until they fully leave inner zones (otherwise Away immediately re-opens).
                        phase = HatchPhase.Away;
                        openLatchedAt = -1f;
                        reopenBlockedUntilClear = true;
                    }
                    break;

                case HatchPhase.SealedInside:
                    // Opening starts once the player leaves deep interior into the approach/doorway.
                    if (!playerInInterior && playerInApproach)
                    {
                        phase = HatchPhase.OpenForExit;
                        openLatchedAt = Time.time;
                    }
                    else if (!inInnerZones)
                    {
                        phase = HatchPhase.Away;
                        openLatchedAt = -1f;
                        reopenBlockedUntilClear = false;
                    }
                    break;

                case HatchPhase.OpenForExit:
                    // Player left approach + interior: always close. Hold must not keep the hatch open.
                    if (!inInnerZones)
                    {
                        phase = HatchPhase.Away;
                        openLatchedAt = -1f;
                        reopenBlockedUntilClear = false;
                        break;
                    }

                    if (!OpenHoldElapsed)
                        break;

                    if (InteriorSealReady)
                    {
                        phase = HatchPhase.SealedInside;
                        openLatchedAt = -1f;
                    }
                    else if (!playerInInterior)
                    {
                        phase = HatchPhase.Away;
                        openLatchedAt = -1f;
                        reopenBlockedUntilClear = true;
                    }
                    break;
            }
        }

        private void ApplyOutputs(bool force)
        {
            bool wantDoorOpen = phase == HatchPhase.OpenForEntry || phase == HatchPhase.OpenForExit;
            bool wantDrilling = phase != HatchPhase.Away;

            if (force || wantDoorOpen != doorOpen)
            {
                bool previousDoorOpen = doorOpen;
                doorOpen = wantDoorOpen;
                if (drillAnimator != null)
                {
                    drillAnimator.SetBool(DoorOpenBool, doorOpen);
                    // Belt-and-suspenders: if bool is false but we are still holding open,
                    // kick the close state so a missed transition cannot leave the hatch ajar.
                    if (!doorOpen && !force)
                        EnsureDoorClosingState();
                }

                if (doorCollider != null)
                    doorCollider.enabled = !doorOpen;

                // One-shots only on real transitions (not OnEnable/Start sync).
                if (!force && previousDoorOpen != doorOpen)
                    PlayDoorSfx(doorOpen);
            }

            if (force || wantDrilling != drilling)
            {
                drilling = wantDrilling;
                if (drillAnimator != null)
                    drillAnimator.SetBool(IsDrillingBool, drilling);

                if (drilling)
                    StartDrillLoops();
                else
                    StopDrillLoops();
            }
        }

        private void EnsureDoorClosingState()
        {
            if (drillAnimator == null)
                return;

            // Layer 0 is Door.
            AnimatorStateInfo info = drillAnimator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName("DoorClosing") || info.IsName("DoorClosed"))
                return;

            if (info.IsName("DoorOpenIdle") || info.IsName("DoorOpening"))
                drillAnimator.CrossFade("DoorClosing", 0.05f, 0, 0f);
        }

        private void EnsureAudioSources()
        {
            Transform doorHost = FindDeepChild("Door");
            Transform upperHost = FindDeepChild("Upper Drill Bits");
            Transform lowerHost = FindDeepChild("Lower Drill Bits");

            doorAudioSource = EnsureLoopSource(
                doorAudioSource,
                doorHost != null ? doorHost : transform,
                "DoorAudio",
                loop: false,
                pitch: 1f);

            upperDrillAudioSource = EnsureLoopSource(
                upperDrillAudioSource,
                upperHost != null ? upperHost : transform,
                "UpperDrillAudio",
                loop: true,
                pitch: upperDrillPitch);

            lowerDrillAudioSource = EnsureLoopSource(
                lowerDrillAudioSource,
                lowerHost != null ? lowerHost : transform,
                "LowerDrillAudio",
                loop: true,
                pitch: lowerDrillPitch);
        }

        private AudioSource EnsureLoopSource(
            AudioSource existing,
            Transform parent,
            string childName,
            bool loop,
            float pitch)
        {
            if (existing != null)
            {
                ConfigureDrillSource(existing, loop, pitch);
                return existing;
            }

            Transform child = parent.Find(childName);
            GameObject host;
            if (child == null)
            {
                host = new GameObject(childName);
                host.transform.SetParent(parent, false);
                host.transform.localPosition = Vector3.zero;
                host.transform.localRotation = Quaternion.identity;
                host.transform.localScale = Vector3.one;
            }
            else
            {
                host = child.gameObject;
            }

            AudioSource source = host.GetComponent<AudioSource>();
            if (source == null)
                source = host.AddComponent<AudioSource>();

            ConfigureDrillSource(source, loop, pitch);
            return source;
        }

        private void ConfigureDrillSource(AudioSource source, bool loop, float pitch)
        {
            if (source == null)
                return;

            source.playOnAwake = false;
            source.loop = loop;
            source.pitch = pitch;
            source.spatialBlend = 1f;
            GameplayAudioUtility.ConfigureWorldSpatialSource(
                source,
                drillAudioMinDistance,
                drillAudioMaxDistance);
        }

        private Transform FindDeepChild(string childName)
        {
            Transform[] all = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name == childName)
                    return all[i];
            }

            return null;
        }

        private void PlayDoorSfx(bool opening)
        {
            AudioClip clip = opening ? doorOpenClip : doorCloseClip;
            if (clip == null)
                return;

            EnsureAudioSources();
            if (doorAudioSource == null || !GameplayAudioUtility.CanPlaySpatialSource(doorAudioSource))
                return;

            doorAudioSource.pitch = 1f;
            doorAudioSource.PlayOneShot(clip, GameSettings.SfxVolume * doorVolume);
        }

        private void StartDrillLoops()
        {
            EnsureAudioSources();
            PlayOrRefreshLoop(upperDrillAudioSource, upperDrillLoopClip, upperDrillVolume, upperDrillPitch);
            PlayOrRefreshLoop(lowerDrillAudioSource, lowerDrillLoopClip, lowerDrillVolume, lowerDrillPitch);
        }

        private void StopDrillLoops()
        {
            if (upperDrillAudioSource != null && upperDrillAudioSource.isPlaying)
                upperDrillAudioSource.Stop();
            if (lowerDrillAudioSource != null && lowerDrillAudioSource.isPlaying)
                lowerDrillAudioSource.Stop();
        }

        private void PlayOrRefreshLoop(AudioSource source, AudioClip clip, float volume, float pitch)
        {
            if (source == null || clip == null || !GameplayAudioUtility.CanPlaySpatialSource(source))
                return;

            source.loop = true;
            source.pitch = pitch;
            source.volume = GameSettings.SfxVolume * volume;
            if (source.clip != clip || !source.isPlaying)
            {
                source.clip = clip;
                source.time = 0f;
                source.Play();
            }
            else
            {
                source.volume = GameSettings.SfxVolume * volume;
            }
        }

        // Relays are optional now (presence is sampled), but kept for gizmos / debugging hooks.
        internal void NotifyPlayerEnter(DrillZoneKind zoneKind) { }
        internal void NotifyPlayerExit(DrillZoneKind zoneKind) { }

#if UNITY_EDITOR
        private void OnValidate()
        {
            approachSize = Vector3.Max(approachSize, new Vector3(0.2f, 0.2f, 0.2f));
            interiorSize = Vector3.Max(interiorSize, new Vector3(0.2f, 0.2f, 0.2f));
            outerBoundarySize = Vector3.Max(outerBoundarySize, new Vector3(0.2f, 0.2f, 0.2f));
            presenceGraceSeconds = Mathf.Max(0f, presenceGraceSeconds);
            openMinHoldSeconds = Mathf.Max(0f, openMinHoldSeconds);
            sealDwellSeconds = Mathf.Max(0f, sealDwellSeconds);
            drillAudioMinDistance = Mathf.Max(0.1f, drillAudioMinDistance);
            drillAudioMaxDistance = Mathf.Max(drillAudioMinDistance + 0.1f, drillAudioMaxDistance);

            if (Application.isPlaying)
                return;

            ResolveTriggerRefsForEditor();
            EnsureOuterBoundaryEnclosesInnerZones();
            ApplyZoneSettingsToColliders();
        }

        private void ResolveTriggerRefsForEditor()
        {
            if (approachTrigger == null)
            {
                Transform approach = transform.Find("DoorApproachTrigger");
                if (approach != null)
                    approachTrigger = approach.GetComponent<Collider>();
            }

            if (interiorTrigger == null)
            {
                Transform interior = transform.Find("DrillInteriorTrigger");
                if (interior != null)
                    interiorTrigger = interior.GetComponent<Collider>();
            }

            if (outerBoundaryTrigger == null)
            {
                Transform outer = transform.Find("DrillOuterBoundary");
                if (outer != null)
                    outerBoundaryTrigger = outer.GetComponent<Collider>();
            }
        }

        [ContextMenu("Drill Zones/Push Settings To Colliders")]
        private void ContextPushZoneSettingsToColliders()
        {
            EnsureTriggers();
            ApplyZoneSettingsToColliders();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [ContextMenu("Drill Zones/Pull Settings From Colliders")]
        private void ContextPullZoneSettingsFromColliders()
        {
            PullZoneSettingsFromColliders();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private void PullZoneSettingsFromColliders()
        {
            if (approachTrigger is BoxCollider approachBox)
            {
                approachCenter = approachBox.center;
                approachSize = approachBox.size;
            }

            if (interiorTrigger is BoxCollider interiorBox)
            {
                interiorCenter = interiorBox.center;
                interiorSize = interiorBox.size;
            }

            if (outerBoundaryTrigger is BoxCollider outerBox)
            {
                outerBoundaryCenter = outerBox.center;
                outerBoundarySize = outerBox.size;
            }
        }

        private void OnDrawGizmosSelected()
        {
            DrawZoneGizmo(outerBoundaryCenter, outerBoundarySize, new Color(0.12f, 0.45f, 0.72f, 0.12f));
            DrawZoneGizmo(approachCenter, approachSize, new Color(0.75f, 0.18f, 0.48f, 0.2f));
            DrawZoneGizmo(interiorCenter, interiorSize, new Color(0.83f, 0.63f, 0.09f, 0.2f));
        }

        private void DrawZoneGizmo(Vector3 center, Vector3 size, Color color)
        {
            Gizmos.color = color;
            Matrix4x4 matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = matrix;
            Gizmos.DrawCube(center, size);
            Gizmos.DrawWireCube(center, size);
            Gizmos.matrix = Matrix4x4.identity;
        }
#endif
    }
}
