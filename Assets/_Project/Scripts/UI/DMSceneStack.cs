using System;
using System.Collections;
using System.Collections.Generic;
using Invector.vCharacterController;
using Project.Core;
using Project.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace Project.UI
{
    public enum DMSceneDestination
    {
        Mars = 0,
        Io = 1
    }

    /// <summary>
    /// Symmetric, repeatable Mars &lt;-&gt; IO (v1.7) switching driven from the main menu.
    ///
    /// Design (2026-11-04f):
    ///
    /// 1. BOTH SCENES STAY RESIDENT. The first IO switch loads "Dark Matter Genesis v1.7" additively
    ///    on top of Mars and it is never unloaded. This is not laziness: every UI host
    ///    (DM_UiRuntimeHost, UITK_Root, MainMenuController), PlayerProgressionManager, the inventory,
    ///    SurvivalStats and the live Kade rig are runtime objects created inside the Mars scene and
    ///    none of them are DontDestroyOnLoad. Unloading Mars would destroy the whole UI stack.
    ///
    /// 2. ONE PLAYER, ONE KADE. The authoritative rig is the Mars one: PlayerReference, PlayerLocator,
    ///    DmUiRuntimeRefs, the progression/survival components and the gameplay camera are all bound
    ///    to that instance (PlayerReference.Register is called from PlayerController.OnEnable, and
    ///    PlayerLocator.FindPlayerObject resolves "Player" tag -> PlayerController -> name). Swapping
    ///    rigs per scene would mean re-binding all of that, so instead the Mars Kade is moved between
    ///    the two authored placements and every other Kade stand-in is deactivated. There is never a
    ///    second live player.
    ///
    /// 3. EXACTLY ONE EVENTSYSTEM. Unity registers UI Toolkit panel picking with the FIRST
    ///    EventSystem that enables (UIToolkitInteroperabilityBridge.OnEnable ->
    ///    UIElementsRuntimeUtility.RegisterEventSystem) and refuses the second one - see the
    ///    "There can be only one active Event System." warning. EventSystem.current is also
    ///    "first wins" (EventSystem.m_EventSystems[0]), and only the current one runs Update()
    ///    (EventSystem.cs:478 returns early otherwise). Adding the IO scene's EventSystem on top of
    ///    Mars' therefore leaves two enabled EventSystems, and PanelEventHandler reads
    ///    UIElementsRuntimeUtility.activeEventSystem while the input module runs on
    ///    EventSystem.current - the two can disagree, which is exactly the "pointer clicks stop
    ///    reaching the UITK main menu" symptom. We keep one and disable the rest after every switch.
    ///
    /// Nothing here writes to .unity or .prefab assets. Runtime only.
    /// </summary>
    public static class DMSceneStack
    {
        public const string MarsSceneName = DMWorldScope.MarsSceneName;
        public const string IoSceneName = DMWorldScope.IoSceneName;
        public const string PersistentEventSystemName = "DM_EventSystem (persistent)";

        private const string LogTag = "[DMSceneStack]";
        private const string ChangeToHipsParentMethod = "ChangeToHipsParent";

        private static DMSceneDestination current = DMSceneDestination.Mars;
        private static bool switching;
        private static bool ioEverLoaded;

        /// <summary>Where the player was standing when he last left each world.</summary>
        private static readonly Dictionary<DMSceneDestination, Vector3> savedPositions =
            new Dictionary<DMSceneDestination, Vector3>();
        private static readonly Dictionary<DMSceneDestination, Quaternion> savedRotations =
            new Dictionary<DMSceneDestination, Quaternion>();

        /// <summary>Authored stand-in placements read out of each scene's own Kade.</summary>
        private static readonly Dictionary<DMSceneDestination, Vector3> authoredPositions =
            new Dictionary<DMSceneDestination, Vector3>();
        private static readonly Dictionary<DMSceneDestination, Quaternion> authoredRotations =
            new Dictionary<DMSceneDestination, Quaternion>();

        /// <summary>Roots we turned off for a hidden world, so we only ever restore what we changed.</summary>
        private static readonly Dictionary<DMSceneDestination, List<GameObject>> hiddenRoots =
            new Dictionary<DMSceneDestination, List<GameObject>>();

        /// <summary>
        /// Scene roots that must never be switched off. Everything here is infrastructure: audio,
        /// object pools, the game manager singletons, HDRP probe data and - most importantly - the
        /// UI hosts, which are created at runtime inside the Mars scene and are NOT DontDestroyOnLoad.
        /// </summary>
        private static readonly string[] KeepAliveRootNames =
        {
            "EventSystem",
            PersistentEventSystemName,
            "PoolManager",
            "Game Manager",
            "CameraShakeService",
            "CommunicationsBootstrap",
            "ProbeVolumePerSceneData",
            "Turret Audio Controler",
            "Gaia Runtime",
            "Gaia Tools",
            "DM_UiRuntimeHost",
            "UITK_Root",
            "UITK_Loading",
            "UITK_Hud"
        };

        /// <summary>
        /// Marker fragments that identify an enemy / NPC rather than the player. The authoritative
        /// test is the project PlayerController: every enemy prefab carries Invector's
        /// vThirdPersonController but none carries Project.Player.PlayerController, which is why the
        /// old matcher (which accepted any component whose type name was "PlayerController" OR
        /// "vThirdPersonController") teleported Kade onto "Corrupt Patrol Android".
        /// </summary>
        private static readonly string[] EnemyTypeMarkers =
        {
            "Enemy", "Ai", "Brain", "Agent", "Patrol", "Npc", "HumanoidPerformance"
        };

        // 2026-11-04g: ground validation for a restore. A switch must never drop the player into
        // empty air 100 m above the destination (that is exactly how he fell to his death in IO
        // after the saved slot had been contaminated with a Mars coordinate).
        private const float GroundProbeUp = 4f;
        private const float GroundProbeDown = 80f;
        private const float GroundSkin = 0.06f;
        /// <summary>Saved Y this far from the world's own authored Y is treated as contaminated.</summary>
        private const float WildYDelta = 25f;
        /// <summary>How long fall classification stays quiet after a switch (or until grounded once).</summary>
        private const float SwitchSettleSeconds = 0.9f;

        private static readonly RaycastHit[] PlacementHits = new RaycastHit[32];

        /// <summary>
        /// 2026-11-04g: the rig this switch is carrying. ApplyWorldVisibility() runs before the rig is
        /// re-registered, and an additive load makes the entering scene's Kade steal PlayerReference,
        /// so MustKeepAlive must consult this instead of PlayerReference or it would hide the very
        /// rig it is about to move and then hand control to an inactive object.
        /// </summary>
        private static GameObject switchRig;

        /// <summary>
        /// 2026-11-04g: the world the rig was last deliberately placed in. Used to guarantee that a
        /// "where he left X" sample was actually taken while he stood in X.
        /// </summary>
        private static bool rigPlacementKnown;
        private static DMSceneDestination rigPlacedIn;

        public static DMSceneDestination Current => current;
        public static DMSceneDestination Other =>
            current == DMSceneDestination.Io ? DMSceneDestination.Mars : DMSceneDestination.Io;
        public static bool IsSwitching => switching;
        public static bool DualWorldActive => ioEverLoaded && IsLoaded(IoSceneName) && IsLoaded(MarsSceneName);
        public static string CurrentSceneName => SceneName(current);
        public static string DestinationSceneName => SceneName(Other);

        /// <summary>Label for the single menu button that toggles between the two worlds.</summary>
        public static string SwitchButtonLabel
        {
            get
            {
                RefreshCurrentFromActiveScene();
                return current == DMSceneDestination.Io ? "Mars" : "IO (v1.7)";
            }
        }

        public static string SceneName(DMSceneDestination destination)
        {
            return destination == DMSceneDestination.Io ? IoSceneName : MarsSceneName;
        }

        public static bool IsLoaded(string sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            return scene.IsValid() && scene.isLoaded;
        }

        public static DMSceneDestination DestinationForScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return current;
            if (string.Equals(sceneName, IoSceneName, StringComparison.OrdinalIgnoreCase))
                return DMSceneDestination.Io;
            if (string.Equals(sceneName, MarsSceneName, StringComparison.OrdinalIgnoreCase))
                return DMSceneDestination.Mars;
            return current;
        }

        /// <summary>Re-derives the current world from whichever scene Unity reports as active.</summary>
        public static void RefreshCurrentFromActiveScene()
        {
            if (switching)
                return;

            Scene active = SceneManager.GetActiveScene();
            if (!active.IsValid())
                return;

            if (string.Equals(active.name, IoSceneName, StringComparison.OrdinalIgnoreCase))
                current = DMSceneDestination.Io;
            else if (string.Equals(active.name, MarsSceneName, StringComparison.OrdinalIgnoreCase))
                current = DMSceneDestination.Mars;
        }

        // ------------------------------------------------------------------ switching

        /// <summary>Menu entry point: flip to the other world.</summary>
        public static IEnumerator SwitchToOtherRoutine(Action<bool, string> completed)
        {
            RefreshCurrentFromActiveScene();
            return SwitchRoutine(current == DMSceneDestination.Io ? DMSceneDestination.Mars : DMSceneDestination.Io, completed);
        }

        /// <summary>
        /// Makes <paramref name="destination"/> the shown world. Loads it additively the first time
        /// and re-uses it afterwards, so the switch is repeatable and never reloads a loaded scene.
        /// </summary>
        public static IEnumerator SwitchRoutine(DMSceneDestination destination, Action<bool, string> completed)
        {
            if (switching)
            {
                Debug.LogWarning($"{LogTag} switch to '{SceneName(destination)}' ignored: a switch is already running.");
                completed?.Invoke(false, "A scene switch is already running.");
                yield break;
            }

            // 2026-11-04g: the world we are leaving must be measured BEFORE the additive load. A
            // destination scene's Kade runs PlayerController.OnEnable -> PlayerReference.Register
            // the moment that scene activates, so resolving the rig after the load returns the scene
            // we are loading into - which recorded a Mars coordinate as IO's "where he left" slot.
            RefreshCurrentFromActiveScene();
            DMSceneDestination previous = current;
            string targetName = SceneName(destination);

            GameObject anchoredRig = ResolvePrimaryRig();
            CapturePlayerPlacement(previous, anchoredRig);
            switchRig = anchoredRig;

            switching = true;
            string failure = null;
            Scene target = SceneManager.GetSceneByName(targetName);
            bool needsLoad = !target.IsValid() || !target.isLoaded;

            Debug.Log($"{LogTag} switch requested: '{SceneName(previous)}' -> '{targetName}' (already loaded: {!needsLoad}).");

            if (needsLoad)
            {
                if (!Application.CanStreamedLevelBeLoaded(targetName))
                {
                    failure = $"Scene '{targetName}' is not in the build scene list.";
                    Debug.LogWarning($"{LogTag} {failure}");
                }
                else
                {
                    AsyncOperation op = null;
                    try
                    {
                        // Additive: Mars owns the player, the UI hosts and all progression state.
                        op = SceneManager.LoadSceneAsync(targetName, LoadSceneMode.Additive);
                    }
                    catch (Exception ex)
                    {
                        failure = $"LoadSceneAsync('{targetName}') threw: {ex.Message}";
                        Debug.LogWarning($"{LogTag} {failure}");
                    }

                    if (op != null)
                    {
                        yield return op;
                        target = SceneManager.GetSceneByName(targetName);
                    }
                }
            }
            else
            {
                Debug.Log($"{LogTag} '{targetName}' is already loaded - reusing it, no reload.");
            }

            if (failure == null && (!target.IsValid() || !target.isLoaded))
                failure = $"Scene '{targetName}' is not loaded after the load finished.";

            if (failure != null)
            {
                switching = false;
                switchRig = null;
                completed?.Invoke(false, failure);
                yield break;
            }

            if (destination == DMSceneDestination.Io)
                ioEverLoaded = true;

            Scene activeBefore = SceneManager.GetActiveScene();
            if (!SceneManager.SetActiveScene(target))
                Debug.LogWarning($"{LogTag} SetActiveScene('{target.name}') returned false; sky profiles key off the active scene name.");

            Debug.Log($"{LogTag} active scene: '{activeBefore.name}' -> '{target.name}' (both resident).");

            ApplyWorldVisibility(destination, anchoredRig);
            ApplyPlayerOwnership(destination, anchoredRig);
            // 2026-11-04f: J / I / Cancel subscriptions must follow the rig that now owns the world
            // (each scene brings its own PlayerInput; the hidden one's action map is disabled).
            DMUiToolkitPlayerInputBridge.RebindForLivePlayer();
            EnforceSingleEventSystem();
            EnforceSingleAudioListener();
            EnforceSingleInputModule();
            ApplyWorldScope(destination);

            current = destination;
            switching = false;
            switchRig = null;

            PlayerReference.ResolveTransform();

            Debug.Log($"{LogTag} switched to '{targetName}' at {DateTime.Now:HH:mm:ss}; "
                + $"player '{Describe(PlayerReference.Transform)}'; dual-world={DualWorldActive}.");

            completed?.Invoke(true, null);
        }

        /// <summary>
        /// Hard reset used by the settings-reload and death-restart paths, which reload Mars as a
        /// single scene and therefore throw away the whole runtime UI host. Must be called before
        /// such a reload so the stack does not think IO is still resident.
        /// </summary>
        public static void ResetForSingleSceneReload()
        {
            Debug.Log($"{LogTag} resetting scene stack for a single-scene reload of '{MarsSceneName}'.");
            switching = false;
            switchRig = null;
            rigPlacementKnown = false;
            rigPlacedIn = DMSceneDestination.Mars;
            current = DMSceneDestination.Mars;
            ioEverLoaded = false;
            savedPositions.Clear();
            savedRotations.Clear();
            authoredPositions.Clear();
            authoredRotations.Clear();
            hiddenRoots.Clear();
            DMWorldScope.ResetToMars();

            // Harmless here (the reload replaces the rig anyway) and keeps the invariant "the input
            // bridge is always bound to the live player" true on every path.
            DMUiToolkitPlayerInputBridge.RebindForLivePlayer();
        }

        // --------------------------------------------------------------------- scope

        private static void ApplyWorldScope(DMSceneDestination destination)
        {
            DMWorldScope.Set(
                destination == DMSceneDestination.Io ? DMWorldScope.World.Io : DMWorldScope.World.Mars,
                DualWorldActive);
        }

        /// <summary>
        /// Re-asserts the single-EventSystem / single-AudioListener invariant and the world scope.
        /// Cheap (two FindObjectsByType) and safe to call before every menu paint.
        /// </summary>
        public static void RefreshMenuInputArbitration()
        {
            RefreshCurrentFromActiveScene();

            EventSystem[] systems = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include);
            int enabledSystems = 0;
            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] != null && systems[i].isActiveAndEnabled)
                    enabledSystems++;
            }

            if (enabledSystems != 1 || EventSystem.current == null)
            {
                Debug.Log($"{LogTag} menu arbitration: {enabledSystems} enabled EventSystem(s) found - enforcing one.");
                EnforceSingleEventSystem();
            }

            RefreshWorldScopeOnly();
        }

        private static void RefreshWorldScopeOnly()
        {
            bool dual = IsLoaded(IoSceneName) && IsLoaded(MarsSceneName);
            if (dual)
                ioEverLoaded = true;
            DMWorldScope.Set(
                current == DMSceneDestination.Io ? DMWorldScope.World.Io : DMWorldScope.World.Mars,
                dual);
        }

        // --------------------------------------------------------------------- roots

        private static void ApplyWorldVisibility(DMSceneDestination shown, GameObject carryRig)
        {
            DMSceneDestination hidden = shown == DMSceneDestination.Io ? DMSceneDestination.Mars : DMSceneDestination.Io;
            RestoreHiddenRoots(shown);
            HideRootsOf(hidden);
        }

        private static void RestoreHiddenRoots(DMSceneDestination destination)
        {
            if (!hiddenRoots.TryGetValue(destination, out List<GameObject> roots) || roots.Count == 0)
                return;

            int restored = 0;
            for (int i = 0; i < roots.Count; i++)
            {
                GameObject root = roots[i];
                if (root == null)
                    continue;
                if (!root.activeSelf)
                {
                    root.SetActive(true);
                    restored++;
                }
            }

            roots.Clear();
            Debug.Log($"{LogTag} re-activated {restored} root object(s) in '{SceneName(destination)}'.");
        }

        private static void HideRootsOf(DMSceneDestination destination)
        {
            Scene scene = SceneManager.GetSceneByName(SceneName(destination));
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            if (!hiddenRoots.TryGetValue(destination, out List<GameObject> roots))
            {
                roots = new List<GameObject>(32);
                hiddenRoots[destination] = roots;
            }

            GameObject[] all = scene.GetRootGameObjects();
            int hidden = 0;

            for (int i = 0; i < all.Length; i++)
            {
                GameObject root = all[i];
                // Never touch something that is already off: the authored scene state is the truth
                // (IO's -=SYSTEMS=-, -=ENVIRONMENT=-, -=TEST=- and Mars' TEST SCENE are deliberately
                // inactive and must stay that way when the world comes back).
                if (root == null || !root.activeSelf)
                    continue;

                string reason;
                if (MustKeepAlive(root, out reason))
                {
                    Debug.Log($"{LogTag} keep '{scene.name}' root '{root.name}' ({reason}).");
                    continue;
                }

                roots.Add(root);
                root.SetActive(false);
                hidden++;
            }

            Debug.Log($"{LogTag} hid {hidden} root(s) of the non-shown world '{scene.name}' (terrain, props, lighting, enemies).");
        }

        private static bool MustKeepAlive(GameObject root, out string reason)
        {
            for (int i = 0; i < KeepAliveRootNames.Length; i++)
            {
                if (root.name == KeepAliveRootNames[i])
                {
                    reason = "persistent infrastructure / UI host";
                    return true;
                }
            }

            // The authoritative player rig is a scene root in Mars; it must survive every switch
            // (this is the whole point of the single-Kade policy) or the whole UI stack loses him.
            // 2026-11-04g: prefer the rig this switch is carrying - PlayerReference may already have
            // been stolen by the scene we are entering.
            GameObject live = switchRig != null
                ? switchRig
                : (PlayerReference.Transform != null ? PlayerReference.Transform.gameObject : null);
            if (live != null && (root == live || live.transform.IsChildOf(root.transform)))
            {
                reason = "owns the live player rig";
                return true;
            }

            // Hard safety net: never switch off the object that currently drives pointer input.
            if (root.GetComponentInChildren<EventSystem>(true) != null)
            {
                reason = "contains an EventSystem";
                return true;
            }

            if (root.GetComponentInChildren<UnityEngine.UIElements.UIDocument>(true) != null)
            {
                reason = "contains a UIDocument";
                return true;
            }

            AudioListener[] listeners = root.GetComponentsInChildren<AudioListener>(true);
            for (int i = 0; i < listeners.Length; i++)
            {
                if (listeners[i] != null && listeners[i].enabled)
                {
                    reason = "contains an enabled AudioListener";
                    return true;
                }
            }

            reason = null;
            return false;
        }

        // -------------------------------------------------------------------- player

        private static void CapturePlayerPlacement(DMSceneDestination destination, GameObject rig)
        {
            if (rig == null || !rig.activeInHierarchy)
                return;

            // 2026-11-04g: only record a position that was MEASURED IN that world. A transform's
            // scene never changes when it is moved, so scene membership is not a usable proxy -
            // instead we track which world we last placed the rig in. This is the guard that stops
            // IO's slot from ever holding a Mars coordinate again (the 12:21:37 failure recorded
            // (-764.90, 131.85, -287.73) for IO and dropped him 110 m above IO's ground).
            if (rigPlacementKnown && rigPlacedIn != destination)
            {
                Debug.LogWarning($"{LogTag} not recording a '{SceneName(destination)}' position: the rig was "
                    + $"last placed in '{SceneName(rigPlacedIn)}'. Keeping that world's authored placement.");
                return;
            }

            savedPositions[destination] = rig.transform.position;
            savedRotations[destination] = rig.transform.rotation;
            Debug.Log($"{LogTag} recorded where he left '{SceneName(destination)}': {rig.transform.position}.");
        }

        private static void ApplyPlayerOwnership(DMSceneDestination shown, GameObject anchoredRig)
        {
            Scene shownScene = SceneManager.GetSceneByName(SceneName(shown));

            // 2026-11-04g: trust the rig anchored before the scene load. ResolvePrimaryRig() is only
            // the fallback, because PlayerReference can already point at the entering scene's Kade.
            GameObject primary = anchoredRig != null ? anchoredRig : ResolvePrimaryRig();
            if (primary == null)
            {
                Debug.LogWarning($"{LogTag} no authoritative player rig found; player not moved.");
                return;
            }

            // Read the authored placement of the world we are entering from its own stand-in, then
            // park that stand-in. The stand-in is a full Kade prefab instance; leaving it live would
            // put two Kades in the same world.
            // Only records when this world has never been seen; by now its Kade is at the authored
            // spot (it was just loaded, or it was recorded at bootstrap / on an earlier switch).
            SnapshotAuthoredPlacement(shown);

            RetireDuplicateRigs(primary, shownScene);
            Scene hiddenScene = SceneManager.GetSceneByName(SceneName(OtherOf(shown)));
            if (hiddenScene.IsValid() && hiddenScene.isLoaded)
                RetireDuplicateRigs(primary, hiddenScene);

            // 2026-11-04g: the rig may sit under a root that HideRootsOf() just turned off (its scene
            // is the one being hidden, or it lost the PlayerReference race to the entering scene's
            // Kade). Reactivate the whole chain, or we would move an invisible player and then point
            // PlayerReference at him.
            int revived = ReactivateChain(primary);
            if (revived > 0)
                Debug.Log($"{LogTag} re-activated the authoritative player '{primary.name}' ({revived} object(s) in its chain).");

            Vector3 position;
            Quaternion rotation;
            string source;

            if (savedPositions.TryGetValue(shown, out position))
            {
                rotation = savedRotations.TryGetValue(shown, out Quaternion savedRotation)
                    ? savedRotation
                    : primary.transform.rotation;
                source = $"where he left '{SceneName(shown)}'";
            }
            else if (authoredPositions.TryGetValue(shown, out position))
            {
                rotation = authoredRotations.TryGetValue(shown, out Quaternion authoredRotation)
                    ? authoredRotation
                    : primary.transform.rotation;
                source = $"authored '{SceneName(shown)}' placement";
            }
            else if (TryGetMarkerPlacement(shownScene, out Vector3 markerPos, out Quaternion markerRot, out string markerSource))
            {
                position = markerPos;
                rotation = markerRot;
                source = markerSource;
            }
            else
            {
                // 2026-11-04g: never leave him wherever he happened to be. Both scenes have a Kade
                // stand-in, so a missing placement means the bookkeeping failed - say so loudly
                // instead of silently dropping the player at the other world's coordinate.
                Debug.LogError($"{LogTag} no placement known for '{SceneName(shown)}'; player left at {primary.transform.position}. "
                    + "This should be impossible - both scenes carry a Kade stand-in.");
                PlayerReference.Register(primary.transform);
                NotifySwitchSettled(primary);
                return;
            }

            position = ValidatePlacement(shown, position, source, out rotation, out string usedSource);
            MoveRig(primary, position, rotation, usedSource);

            rigPlacementKnown = true;
            rigPlacedIn = shown;

            PlayerReference.Register(primary.transform);
            NotifySwitchSettled(primary);
            Debug.Log($"{LogTag} player owner: '{primary.name}' (scene '{primary.scene.name}') at {primary.transform.position}.");
        }

        /// <summary>
        /// 2026-11-04g: makes sure a restore actually lands on the destination world's ground.
        /// Casts down from above the stored point, ignoring the world that is not being shown, and
        /// rejects a saved Y that is wildly different from the destination's authored Y (the exact
        /// shape of the contaminated-slot bug). When the probe finds a surface the rig is put ON it;
        /// when it fails the authored placement is used instead.
        /// </summary>
        private static Vector3 ValidatePlacement(DMSceneDestination shown, Vector3 candidate, string source,
            out Quaternion rotation, out string usedSource)
        {
            usedSource = source;

            if (authoredPositions.TryGetValue(shown, out Vector3 authored))
            {
                // A stored Y this far from the world's own authored Y cannot belong to this world.
                if (Mathf.Abs(candidate.y - authored.y) > WildYDelta)
                {
                    Debug.LogWarning($"{LogTag} rejected {source} {candidate}: its Y is "
                        + $"{Mathf.Abs(candidate.y - authored.y):0.0} m from the authored '{SceneName(shown)}' Y "
                        + $"({authored.y:0.##}). Using the authored placement instead.");
                    rotation = authoredRotations.TryGetValue(shown, out Quaternion ar) ? ar : Quaternion.identity;
                    usedSource = $"authored '{SceneName(shown)}' placement (saved Y out of range)";
                    return authored;
                }
            }

            if (!TryProbeGround(shown, candidate, out float surfaceY, out float distance))
            {
                if (authoredPositions.TryGetValue(shown, out authored))
                {
                    Debug.LogWarning($"{LogTag} rejected {source} {candidate}: no ground within "
                        + $"{GroundProbeDown:0} m in '{SceneName(shown)}'. Using the authored placement instead.");
                    rotation = authoredRotations.TryGetValue(shown, out Quaternion ar2) ? ar2 : Quaternion.identity;
                    usedSource = $"authored '{SceneName(shown)}' placement (no ground under the saved spot)";
                    return authored;
                }

                Debug.LogWarning($"{LogTag} no ground under {source} {candidate} and no authored placement; keeping it.");
                rotation = savedRotations.TryGetValue(shown, out Quaternion sr) ? sr : Quaternion.identity;
                return candidate;
            }

            rotation = savedRotations.TryGetValue(shown, out Quaternion savedRot) ? savedRot : Quaternion.identity;
            float targetY = surfaceY + GroundSkin;
            usedSource = $"{source} (grounded at y={surfaceY:0.##})";
            return new Vector3(candidate.x, targetY, candidate.z);
        }

        /// <summary>
        /// Downward probe at <paramref name="candidate"/> that ignores colliders belonging to the
        /// world that is NOT being shown. Without that, Mars' terrain (still enabled and covering the
        /// IO coordinates) would validate a Mars-space position as IO ground.
        /// </summary>
        private static bool TryProbeGround(DMSceneDestination shown, Vector3 candidate, out float surfaceY, out float distance)
        {
            surfaceY = candidate.y;
            distance = float.PositiveInfinity;

            // Roots were just switched on/off for this world; make sure the physics scene agrees
            // before the probe runs.
            Physics.SyncTransforms();

            Vector3 origin = new Vector3(candidate.x, candidate.y + GroundProbeUp, candidate.z);
            float rayLength = GroundProbeUp + GroundProbeDown;
            int count = Physics.RaycastNonAlloc(origin, Vector3.down, PlacementHits, rayLength, ~0, QueryTriggerInteraction.Ignore);

            bool any = false;
            string hiddenName = SceneName(OtherOf(shown));
            GameObject rig = switchRig != null
                ? switchRig
                : (PlayerReference.Transform != null ? PlayerReference.Transform.gameObject : null);

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = PlacementHits[i];
                if (hit.collider == null)
                    continue;

                // Reject geometry the player cannot stand on (walls, steep slopes, trigger volumes).
                if (hit.normal.y < 0.5f)
                    continue;

                // 2026-11-04g: the whole point of this probe. The non-shown world is still resident
                // and its terrain is still enabled, so Mars' ground would otherwise validate an
                // IO-space point (and vice versa). Skip every collider from the hidden scene; keep
                // runtime-created / DontDestroyOnLoad geometry, which has an empty scene name.
                string hitScene = hit.collider.gameObject.scene.name;
                if (!string.IsNullOrEmpty(hitScene)
                    && string.Equals(hitScene, hiddenName, StringComparison.OrdinalIgnoreCase))
                    continue;

                // The rig itself must never validate its own capsule.
                if (rig != null)
                {
                    Transform hitTransform = hit.collider.transform;
                    if (hitTransform != null
                        && (hitTransform.gameObject == rig || hitTransform.IsChildOf(rig.transform)))
                        continue;
                }

                if (!any || hit.distance < distance)
                {
                    distance = hit.distance;
                    surfaceY = hit.point.y;
                    any = true;
                }
            }

            return any;
        }

        /// <summary>
        /// 2026-11-04g: tells the landing director that this teleport was a scene switch so fall
        /// classification cannot turn it into fall damage or the lethal-fall ragdoll.
        /// </summary>
        private static void NotifySwitchSettled(GameObject rig)
        {
            if (rig == null)
                return;

            Project.Player.DMLandingDirector landing = rig.GetComponent<Project.Player.DMLandingDirector>();
            if (landing == null)
                landing = rig.GetComponentInChildren<Project.Player.DMLandingDirector>(true);
            if (landing != null)
                landing.NotifySceneTeleport(SwitchSettleSeconds);

            Project.Player.Invector.PioneerTerrainRescue rescue =
                rig.GetComponent<Project.Player.Invector.PioneerTerrainRescue>();
            if (rescue != null)
                rescue.SuppressFor(SwitchSettleSeconds);
        }

        /// <summary>
        /// 2026-11-04g: activates <paramref name="target"/> and every ancestor so a rig parked under a
        /// hidden root still renders. Returns how many objects it had to switch on.
        /// </summary>
        private static int ReactivateChain(GameObject target)
        {
            if (target == null)
                return 0;

            int changed = 0;
            Transform node = target.transform;
            while (node != null)
            {
                if (!node.gameObject.activeSelf)
                {
                    node.gameObject.SetActive(true);
                    changed++;
                }
                node = node.parent;
            }

            return changed;
        }

        /// <summary>Deactivates every Kade rig in <paramref name="scene"/> that is not the authoritative one.</summary>
        private static void RetireDuplicateRigs(GameObject primary, Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int r = 0; r < roots.Length; r++)
            {
                if (roots[r] == null)
                    continue;

                Transform[] transforms = roots[r].GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < transforms.Length; i++)
                {
                    Transform t = transforms[i];
                    if (t == null)
                        continue;

                    GameObject go = t.gameObject;
                    if (go.scene != scene || go == primary || !go.activeSelf)
                        continue;
                    if (go.GetComponent<PlayerController>() == null)
                        continue;
                    if (IsEnemyRig(go))
                        continue;

                    // 2026-11-04h: a stand-in retired in the same frame its scene finished loading never
                    // runs Start(), so Invector's OnDisable paths read state Start() would have built
                    // (vThirdPersonMotor.animator is assigned in Init(), characterHips in LoadBodyPart()).
                    // vRagdoll.OnDisable also queues ChangeToHipsParent behind a 0.01 s Invoke that then
                    // NREs on the null hips. Disable the ragdoll component first so its OnDisable runs
                    // here, cancel the queued call, then deactivate the object - order matters, because
                    // after SetActive(false) it has already been queued.
                    vRagdoll[] ragdolls = go.GetComponentsInChildren<vRagdoll>(true);
                    for (int d = 0; d < ragdolls.Length; d++)
                    {
                        vRagdoll ragdoll = ragdolls[d];
                        if (ragdoll == null)
                            continue;

                        if (ragdoll.enabled)
                            ragdoll.enabled = false;
                        ragdoll.CancelInvoke(ChangeToHipsParentMethod);
                    }

                    go.SetActive(false);
                    Debug.Log($"{LogTag} deactivated duplicate player '{go.name}' in '{scene.name}' (single-Kade policy).");
                }
            }
        }

        private static DMSceneDestination OtherOf(DMSceneDestination destination)
        {
            return destination == DMSceneDestination.Io ? DMSceneDestination.Mars : DMSceneDestination.Io;
        }

        /// <summary>
        /// The one rig that owns progression / inventory / survival: whatever PlayerReference already
        /// points at, else the Mars scene rig, else any live non-enemy PlayerController.
        /// </summary>
        public static GameObject ResolvePrimaryRig()
        {
            GameObject registered = PlayerReference.Transform != null ? PlayerReference.Transform.gameObject : null;
            if (registered != null)
                return registered;

            Scene mars = SceneManager.GetSceneByName(MarsSceneName);
            if (mars.IsValid() && mars.isLoaded)
            {
                GameObject fromMars = FindPlayerRig(mars);
                if (fromMars != null)
                    return fromMars;
            }

            PlayerController[] controllers = UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude);
            for (int i = 0; i < controllers.Length; i++)
            {
                if (controllers[i] == null)
                    continue;
                if (IsEnemyRig(controllers[i].gameObject))
                    continue;
                return controllers[i].gameObject;
            }

            return null;
        }

        /// <summary>
        /// Finds the real player rig in a scene. Order:
        ///   1. an ACTIVE object whose name contains "Kade" and that carries Project.Player.PlayerController,
        ///   2. the same but inactive,
        ///   3. any other object carrying Project.Player.PlayerController.
        /// Anything with an enemy / NPC marker is rejected outright, so "Corrupt Patrol Android"
        /// can never be selected again.
        /// </summary>
        public static GameObject FindPlayerRig(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return null;

            GameObject kadeActive = null;
            GameObject kadeInactive = null;
            GameObject anyActive = null;
            GameObject anyInactive = null;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int r = 0; r < roots.Length; r++)
            {
                if (roots[r] == null)
                    continue;

                Transform[] transforms = roots[r].GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < transforms.Length; i++)
                {
                    Transform t = transforms[i];
                    if (t == null)
                        continue;

                    GameObject go = t.gameObject;
                    if (go.scene != scene)
                        continue;
                    if (IsEnemyRig(go))
                        continue;

                    if (go.GetComponent<PlayerController>() == null)
                        continue;

                    bool active = go.activeInHierarchy;
                    bool namedKade = go.name.IndexOf("Kade", StringComparison.OrdinalIgnoreCase) >= 0;

                    if (namedKade)
                    {
                        if (active && kadeActive == null)
                            kadeActive = go;
                        else if (!active && kadeInactive == null)
                            kadeInactive = go;
                    }

                    if (active && anyActive == null)
                        anyActive = go;
                    else if (!active && anyInactive == null)
                        anyInactive = go;
                }
            }

            if (kadeActive != null)
                return kadeActive;
            if (kadeInactive != null)
                return kadeInactive;
            return anyActive != null ? anyActive : anyInactive;
        }

        /// <summary>
        /// Position-only fallback used when a scene has no player rig of its own: the object named
        /// exactly "Player Versions" (IO v1.7 keeps the player-spawn marker there).
        /// </summary>
        public static bool TryGetMarkerPlacement(Scene scene, out Vector3 position, out Quaternion rotation, out string source)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            source = null;

            if (!scene.IsValid() || !scene.isLoaded)
                return false;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int r = 0; r < roots.Length; r++)
            {
                if (roots[r] == null)
                    continue;

                Transform[] transforms = roots[r].GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < transforms.Length; i++)
                {
                    Transform t = transforms[i];
                    if (t == null || t.gameObject.scene != scene)
                        continue;
                    if (!string.Equals(t.name, "Player Versions", StringComparison.Ordinal))
                        continue;

                    position = t.position;
                    rotation = t.rotation;
                    source = "'Player Versions' marker";
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// True when this object looks like an enemy or NPC rather than the player.
        /// The authoritative test is Project.Player.PlayerController: every enemy prefab in this
        /// project carries Invector's vThirdPersonController but none carries PlayerController.
        /// The name/type markers are a defensive second gate.
        /// </summary>
        public static bool IsEnemyRig(GameObject candidate)
        {
            if (candidate == null)
                return true;

            if (candidate.GetComponent<PlayerController>() != null)
                return false;

            if (candidate.GetComponent<Invector.vCharacterController.vThirdPersonController>() != null)
                return true;

            string name = candidate.name;
            if (name.IndexOf("Enemy", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Patrol", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Android", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("NPC", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            MonoBehaviour[] behaviours = candidate.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] == null)
                    continue;
                string typeName = behaviours[i].GetType().Name;
                for (int m = 0; m < EnemyTypeMarkers.Length; m++)
                {
                    if (typeName.IndexOf(EnemyTypeMarkers[m], StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }

            return false;
        }

        private static void MoveRig(GameObject player, Vector3 position, Quaternion rotation, string source)
        {
            if (player == null)
                return;

            CharacterController controller = player.GetComponent<CharacterController>();
            bool controllerWasEnabled = controller != null && controller.enabled;
            if (controllerWasEnabled)
                controller.enabled = false;

            Rigidbody body = player.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.position = position;
                body.rotation = rotation;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            player.transform.SetPositionAndRotation(position, rotation);
            Physics.SyncTransforms();

            if (controllerWasEnabled)
                controller.enabled = true;

            Debug.Log($"{LogTag} player moved to {source} at {position} (rot {rotation.eulerAngles}).");
        }

        // -------------------------------------------------- event system / listeners

        /// <summary>
        /// Leaves exactly one enabled EventSystem and makes it immune to further scene loads.
        /// UITK panel picking is bound to the first bridge that registers, and EventSystem.current is
        /// m_EventSystems[0]; keeping the same object for both is what makes pointer clicks work.
        /// </summary>
        public static void EnforceSingleEventSystem()
        {
            EventSystem[] all = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include);
            if (all.Length == 0)
            {
                GameObject created = new GameObject(PersistentEventSystemName);
                created.AddComponent<EventSystem>();
                created.AddComponent<InputSystemUIInputModule>();
                UnityEngine.Object.DontDestroyOnLoad(created);
                Debug.LogWarning($"{LogTag} no EventSystem found at all; created the persistent one.");
                DMUiGamepadNavigation.EnsureConfigured();
                return;
            }

            // The survivor must be the EventSystem that OWNS the UI Toolkit panel picking objects.
            // UIToolkitInteroperabilityBridge.CreatePanelGameObject parents each PanelEventHandler /
            // PanelRaycaster under its own EventSystem, and DestroyPanelGameObject refuses to clean
            // up objects it did not create - so if we keep a different one, every panel keeps its
            // raycaster under the disabled object and pointer clicks reach nobody. That is the exact
            // "main menu buttons are dead after Esc from IO" failure, so panel ownership is checked
            // FIRST, before EventSystem.current.
            EventSystem survivor = FindPanelOwner(all) ?? EventSystem.current;
            if (survivor == null || !survivor.isActiveAndEnabled)
            {
                survivor = null;
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] != null && all[i].isActiveAndEnabled)
                    {
                        survivor = all[i];
                        break;
                    }
                }
            }

            if (survivor == null)
                survivor = all[0];

            int disabled = 0;
            for (int i = 0; i < all.Length; i++)
            {
                EventSystem candidate = all[i];
                if (candidate == null || candidate == survivor)
                    continue;
                if (!candidate.gameObject.activeSelf)
                    continue;

                // Deactivate the duplicate's GameObject, not just the component: the interop bridge
                // lives on that object and would otherwise keep fighting for panel ownership.
                candidate.gameObject.SetActive(false);
                disabled++;
                Debug.Log($"{LogTag} deactivated extra EventSystem '{candidate.name}' in '{candidate.gameObject.scene.name}'.");
            }

            if (!survivor.gameObject.activeSelf)
                survivor.gameObject.SetActive(true);

            InputSystemUIInputModule module = survivor.GetComponent<InputSystemUIInputModule>();
            if (module == null)
            {
                module = survivor.gameObject.AddComponent<InputSystemUIInputModule>();
                Debug.LogWarning($"{LogTag} EventSystem survivor '{survivor.name}' had no InputSystemUIInputModule; added one.");
            }

            if (!module.enabled)
                module.enabled = true;

            if (survivor.isActiveAndEnabled)
                EventSystem.current = survivor; // re-assert m_EventSystems[0] so current == the survivor

            AdoptPersistent(survivor.gameObject);

            Debug.Log($"{LogTag} EventSystem survivor: '{survivor.name}' in '{survivor.gameObject.scene.name}'; disabled {disabled} duplicate(s).");

            // 2026-11-04h: disabling a duplicate runs its UIToolkitInteroperabilityBridge.OnDisable,
            // which unregisters that EventSystem from UIElementsRuntimeUtility. When the duplicate was
            // the one holding the registration, the utility is left with no active event system and
            // PanelEventHandler.ProcessImguiEvents NREs on `eventSystem.sendNavigationEvents` the next
            // time EventSystem.current dispatches a Cancel/Move to a UITK panel. Re-register the
            // survivor through the public (obsolete) override API so the two always agree.
            ReassertToolkitEventSystem(survivor);

            DMUiGamepadNavigation.EnsureConfigured();
        }

        /// <summary>
        /// 2026-11-04h: makes sure UIElementsRuntimeUtility still has an active event system after the
        /// duplicate EventSystems are disabled, and re-creates the panel picking objects when it does not.
        /// </summary>
        private static void ReassertToolkitEventSystem(EventSystem survivor)
        {
            if (survivor == null || !survivor.isActiveAndEnabled)
                return;

            // PanelEventHandler.eventSystem is `UIElementsRuntimeUtility.activeEventSystem as EventSystem`,
            // and ProcessImguiEvents dereferences it unconditionally. A null here means every Cancel/Move
            // aimed at a UI Toolkit panel throws, so treat "not an EventSystem" the same as "none".
            // UIElementsRuntimeUtility is internal (CS0122 if referenced), hence reflection.
            object before = ReadToolkitActiveEventSystem();
            if (before as EventSystem != null)
                return;

            // Toggling the override off and back on is what actually forces Unregister/Register:
            // assigning the same value twice early-returns on the bridge's m_OldOverride check, which
            // is why the survivor could believe it was registered while the utility had nobody.
#pragma warning disable 618 // SetUITookitEventSystemOverride is obsolete but is the only public bridge re-registration path.
            EventSystem.SetUITookitEventSystemOverride(survivor, false, true);
            EventSystem.SetUITookitEventSystemOverride(survivor, true, true);
#pragma warning restore 618

            object after = ReadToolkitActiveEventSystem();
            Debug.LogWarning($"{LogTag} UI Toolkit had no active EventSystem (was '{DescribeRef(before)}'); re-registered '{survivor.name}' (now '{DescribeRef(after)}').");
        }

        /// <summary>
        /// 2026-11-04f: the EventSystem that currently owns the UITK panel pickers, i.e. the one that
        /// has a PanelRaycaster bound to a live panel as a direct child (the bridge parents them
        /// there). Returns null when no panel has been created yet.
        /// </summary>
        private static EventSystem FindPanelOwner(EventSystem[] all)
        {
            for (int i = 0; i < all.Length; i++)
            {
                EventSystem candidate = all[i];
                if (candidate == null)
                    continue;

                UnityEngine.UIElements.PanelRaycaster[] raycasters =
                    candidate.GetComponentsInChildren<UnityEngine.UIElements.PanelRaycaster>(true);
                for (int r = 0; r < raycasters.Length; r++)
                {
                    UnityEngine.UIElements.PanelRaycaster raycaster = raycasters[r];
                    if (raycaster != null && raycaster.panel != null)
                        return candidate;
                }
            }

            return null;
        }

        /// <summary>One AudioListener. Prefers the listener under the live player camera.</summary>
        public static void EnforceSingleAudioListener()
        {
            AudioListener[] all = UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include);
            if (all.Length == 0)
                return;

            AudioListener survivor = null;
            Transform player = PlayerReference.Transform;
            if (player != null)
            {
                AudioListener[] onPlayer = player.GetComponentsInChildren<AudioListener>(true);
                for (int i = 0; i < onPlayer.Length; i++)
                {
                    if (onPlayer[i] != null && onPlayer[i].isActiveAndEnabled)
                    {
                        survivor = onPlayer[i];
                        break;
                    }
                }
            }

            if (survivor == null)
            {
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] != null && all[i].isActiveAndEnabled)
                    {
                        survivor = all[i];
                        break;
                    }
                }
            }

            if (survivor == null)
                return; // all are already disabled; leave the authored state alone

            int disabled = 0;
            for (int i = 0; i < all.Length; i++)
            {
                AudioListener candidate = all[i];
                if (candidate == null || candidate == survivor)
                    continue;
                if (!candidate.enabled)
                    continue;

                candidate.enabled = false;
                disabled++;
            }

            if (disabled > 0)
            {
                Debug.Log($"{LogTag} AudioListener survivor: '{Describe(survivor.transform)}'; disabled {disabled} duplicate(s).");
            }
        }

        /// <summary>Exactly one enabled InputSystemUIInputModule, on the EventSystem survivor.</summary>
        public static void EnforceSingleInputModule()
        {
            InputSystemUIInputModule[] all = UnityEngine.Object.FindObjectsByType<InputSystemUIInputModule>(FindObjectsInactive.Include);
            if (all.Length <= 1)
                return;

            EventSystem survivor = EventSystem.current;
            InputSystemUIInputModule keep = survivor != null ? survivor.GetComponent<InputSystemUIInputModule>() : null;

            if (keep == null)
            {
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] != null && all[i].isActiveAndEnabled)
                    {
                        keep = all[i];
                        break;
                    }
                }
            }

            if (keep == null)
                return;

            int disabled = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null || all[i] == keep)
                    continue;
                if (!all[i].enabled)
                    continue;

                all[i].enabled = false;
                disabled++;
            }

            if (!keep.enabled)
                keep.enabled = true;

            if (disabled > 0)
                Debug.Log($"{LogTag} InputSystemUIInputModule survivor: '{Describe(keep)}'; disabled {disabled} duplicate(s).");
        }

        private static void AdoptPersistent(GameObject target)
        {
            if (target == null || target.transform.parent != null)
                return;
            if (target.scene.name == "DontDestroyOnLoad")
                return;

            UnityEngine.Object.DontDestroyOnLoad(target);
            Debug.Log($"{LogTag} '{target.name}' moved to DontDestroyOnLoad so scene loads can never disable it.");
        }

        // ------------------------------------------------------------------ helpers

        private static System.Reflection.PropertyInfo toolkitActiveEventSystemProperty;
        private static bool toolkitActiveEventSystemProbed;

        /// <summary>
        /// 2026-11-04h: reads UIElementsRuntimeUtility.activeEventSystem, which is internal. Returns null
        /// when the property cannot be found so callers fall back to re-registering unconditionally.
        /// </summary>
        private static object ReadToolkitActiveEventSystem()
        {
            if (!toolkitActiveEventSystemProbed)
            {
                toolkitActiveEventSystemProbed = true;
                Type utility = Type.GetType("UnityEngine.UIElements.UIElementsRuntimeUtility, UnityEngine.UIElementsModule");
                if (utility != null)
                {
                    toolkitActiveEventSystemProperty = utility.GetProperty(
                        "activeEventSystem",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                }
            }

            return toolkitActiveEventSystemProperty != null
                ? toolkitActiveEventSystemProperty.GetValue(null)
                : null;
        }

        /// <summary>2026-11-04h: safe description of an arbitrary object reference for diagnostics.</summary>
        private static string DescribeRef(object value)
        {
            if (value == null)
                return "null";

            UnityEngine.Object obj = value as UnityEngine.Object;
            if (obj != null)
                return $"{obj.GetType().Name} '{obj.name}'";

            return value.GetType().Name;
        }

        private static string Describe(Component component)
        {
            return component == null ? "<none>" : component.gameObject.name + " (" + component.gameObject.scene.name + ")";
        }

        /// <summary>
        /// 2026-11-04g: Play can start in EITHER scene. The old code assumed Mars, so starting in IO
        /// left the first switch with no authored placement for either world. Snapshot the starting
        /// world (and resolve which world it is) before the player has had a chance to move.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapFromActiveScene()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.IsValid())
                current = DestinationForScene(active.name);

            // Both worlds may already be resident (e.g. Play started in IO with Mars loaded too).
            // Recording happens HERE, before anything moves, so a scene's own Kade position is a
            // true authored placement - including Mars, whose Kade is the live rig in that case.
            SnapshotAuthoredPlacement(DMSceneDestination.Mars);
            SnapshotAuthoredPlacement(DMSceneDestination.Io);

            // Whichever world Play started in owns the only rig present so far: its current spot is
            // that world's authored placement. Without this, starting in IO and switching to Mars
            // first would come back to IO with no authored fallback.
            if (!authoredPositions.ContainsKey(current))
            {
                GameObject startRig = ResolvePrimaryRig();
                if (startRig != null)
                {
                    authoredPositions[current] = startRig.transform.position;
                    authoredRotations[current] = startRig.transform.rotation;
                    Debug.Log($"{LogTag} recorded authored '{SceneName(current)}' placement from the live "
                        + $"rig '{startRig.name}' at {startRig.transform.position}.");
                }
            }

            rigPlacementKnown = true;
            rigPlacedIn = current;

            Debug.Log($"{LogTag} bootstrap: starting world '{active.name}' (destination {current}); "
                + $"authored known = Mars:{authoredPositions.ContainsKey(DMSceneDestination.Mars)} "
                + $"Io:{authoredPositions.ContainsKey(DMSceneDestination.Io)}.");
        }

        /// <summary>
        /// Records a scene's own Kade position the first time we see it, before anything moves it.
        /// Callers must invoke this while the rig is still at its authored spot.
        /// </summary>
        private static void SnapshotAuthoredPlacement(DMSceneDestination destination)
        {
            if (authoredPositions.ContainsKey(destination))
                return;
            if (!IsLoaded(SceneName(destination)))
                return;

            Scene scene = SceneManager.GetSceneByName(SceneName(destination));
            GameObject standIn = FindPlayerRig(scene);
            if (standIn == null)
                return;

            authoredPositions[destination] = standIn.transform.position;
            authoredRotations[destination] = standIn.transform.rotation;
            Debug.Log($"{LogTag} recorded authored '{SceneName(destination)}' placement '{standIn.name}' "
                + $"at {standIn.transform.position} (rot {standIn.transform.rotation.eulerAngles}).");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            current = DMSceneDestination.Mars;
            switching = false;
            switchRig = null;
            rigPlacementKnown = false;
            rigPlacedIn = DMSceneDestination.Mars;
            ioEverLoaded = false;
            savedPositions.Clear();
            savedRotations.Clear();
            authoredPositions.Clear();
            authoredRotations.Clear();
            hiddenRoots.Clear();
            DMWorldScope.ResetToMars();
        }
    }
}