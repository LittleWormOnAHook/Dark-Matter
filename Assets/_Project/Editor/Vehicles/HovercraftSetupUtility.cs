#if UNITY_EDITOR
using Project.EditorTools;
using Project.Vehicles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.EditorTools.Vehicles
{
    public static class HovercraftSetupUtility
    {
        private const string SourceArtPrefabPath =
            "Assets/PolygonSciFiWorlds/Prefabs/Props/Vehicles/SM_Veh_HoverCraft_01.prefab";
        private const string OutputPrefabPath = "Assets/_Project/Prefabs/Vehicles/Hovercraft_Pioneer.prefab";
        private const string ProfileAssetPath = "Assets/_Project/Data/Vehicles/HovercraftProfile_Default.asset";

        [MenuItem("Tools/Survival Pioneer/Vehicles/Repair Hovercraft References", false, 11)]
        public static void RepairHovercraftReferences()
        {
            int repaired = 0;

            HovercraftController[] controllers = Object.FindObjectsByType<HovercraftController>(FindObjectsInactive.Include);

            for (int i = 0; i < controllers.Length; i++)
            {
                if (RepairHovercraftRoot(controllers[i].gameObject))
                    repaired++;
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(OutputPrefabPath);
            try
            {
                if (RepairHovercraftRoot(prefabRoot))
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, OutputPrefabPath);
                    repaired++;
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log(repaired > 0
                ? $"Hovercraft reference repair complete ({repaired} object(s) updated)."
                : "Hovercraft references already wired.");
        }

        [MenuItem("Tools/Survival Pioneer/Vehicles/Create Hovercraft In Scene", false, 10)]
        public static void CreateHovercraftInScene()
        {
            EnsureFolder("Assets/_Project/Prefabs/Vehicles");
            EnsureFolder("Assets/_Project/Data/Vehicles");

            HovercraftProfile profile = EnsureDefaultProfile();
            GameObject hovercraft = BuildHovercraftRoot(profile);

            PrefabUtility.SaveAsPrefabAsset(hovercraft, OutputPrefabPath);
            Selection.activeGameObject = hovercraft;
            EditorGUIUtility.PingObject(hovercraft);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"Hovercraft created in scene and saved to {OutputPrefabPath}. " +
                "Board at EnterPoint with E; F1 toggles cockpit/follow camera.");
        }

        private static bool RepairHovercraftRoot(GameObject root)
        {
            if (root == null)
                return false;

            HoverPhysicsDriver physics = root.GetComponent<HoverPhysicsDriver>();
            HovercraftOccupancy occupancy = root.GetComponent<HovercraftOccupancy>();
            HovercraftCameraRig cameraRig = root.GetComponent<HovercraftCameraRig>();
            HovercraftTurretController turret = root.GetComponent<HovercraftTurretController>();
            HovercraftController controller = root.GetComponent<HovercraftController>();
            HovercraftUsable usable = root.GetComponent<HovercraftUsable>();
            HovercraftProfile profile = physics != null ? physics.Profile : null;

            if (profile == null)
                profile = AssetDatabase.LoadAssetAtPath<HovercraftProfile>(ProfileAssetPath);

            bool changed = false;

            HovercraftEngineAudio engineAudio = root.GetComponent<HovercraftEngineAudio>();
            if (engineAudio == null)
            {
                engineAudio = root.AddComponent<HovercraftEngineAudio>();
                changed = true;
            }

            HovercraftVehicleAudio vehicleAudio = root.GetComponent<HovercraftVehicleAudio>();
            if (vehicleAudio == null)
            {
                vehicleAudio = root.AddComponent<HovercraftVehicleAudio>();
                changed = true;
            }

            HovercraftThrusterVfx thrusterVfx = root.GetComponent<HovercraftThrusterVfx>();
            if (thrusterVfx == null)
            {
                thrusterVfx = root.AddComponent<HovercraftThrusterVfx>();
                changed = true;
            }

            HovercraftFuelSystem fuelSystem = root.GetComponent<HovercraftFuelSystem>();
            if (fuelSystem == null)
            {
                fuelSystem = root.AddComponent<HovercraftFuelSystem>();
                changed = true;
            }

            Project.Data.ItemData plasmaFuelItem =
                AssetDatabase.LoadAssetAtPath<Project.Data.ItemData>("Assets/_Project/Data/Items/Plasma Fuel.asset");
            changed |= WireSerializedReference(fuelSystem, "controller", controller);
            changed |= WireSerializedReference(fuelSystem, "physicsDriver", physics);
            changed |= WireSerializedReference(fuelSystem, "plasmaFuelItem", plasmaFuelItem);
            changed |= WireSerializedReference(controller, "fuelSystem", fuelSystem);
            changed |= WireSerializedReference(usable, "fuelSystem", fuelSystem);

            HovercraftHealth health = root.GetComponent<HovercraftHealth>();
            if (health == null)
            {
                health = root.AddComponent<HovercraftHealth>();
                changed = true;
            }

            changed |= WireSerializedReference(health, "controller", controller);

            changed |= WireSecondaryTurret(root, turret, profile);
            changed |= WireSerializedReference(cameraRig, "physicsDriver", physics);
            changed |= WireSerializedReference(controller, "physicsDriver", physics);
            changed |= WireSerializedReference(controller, "occupancy", occupancy);
            changed |= WireSerializedReference(controller, "cameraRig", cameraRig);
            changed |= WireSerializedReference(controller, "turret", turret);
            changed |= WireSerializedReference(controller, "engineAudio", engineAudio);
            changed |= WireSerializedReference(controller, "vehicleAudio", vehicleAudio);
            changed |= WireSerializedReference(controller, "thrusterVfx", thrusterVfx);
            changed |= WireSerializedReference(controller, "usable", usable);
            changed |= WireSerializedReference(usable, "controller", controller);
            changed |= WireSerializedReference(usable, "occupancy", occupancy);
            changed |= WireSerializedReference(engineAudio, "physicsDriver", physics);
            changed |= WireSerializedReference(engineAudio, "occupancy", occupancy);

            AudioSource engineSource = FindOrCreateChildAudioSource(root.transform, "EngineAudio", true);
            AudioSource vehicleSource = FindOrCreateChildAudioSource(root.transform, "VehicleAudio", false);
            engineAudio.Configure(profile, physics, occupancy, engineSource);
            vehicleAudio.Configure(profile, vehicleSource);

            ParticleSystem[] thrusters = FindOrCreateThrusterSystems(root.transform);
            thrusterVfx.Configure(profile, physics, occupancy, thrusters);

            if (changed)
                EditorUtility.SetDirty(root);

            return changed;
        }

        private static bool WireSecondaryTurret(GameObject root, HovercraftTurretController turret, HovercraftProfile profile)
        {
            if (turret == null)
                return false;

            Transform turretMount2 = FindChildTransform(root.transform, "TurretMount2");
            if (turretMount2 == null)
            {
                turretMount2 = CreateAnchor(root.transform, "TurretMount2", new Vector3(0f, 1.55f, 0.8f));
            }

            Transform turretYaw2 = FindChildTransform(turretMount2, "TurretYaw2")
                ?? CreateAnchor(turretMount2, "TurretYaw2", Vector3.zero);
            Transform turretPitch2 = FindChildTransform(turretYaw2, "TurretPitch2")
                ?? CreateAnchor(turretYaw2, "TurretPitch2", Vector3.zero);
            Transform muzzle2 = FindChildTransform(turretPitch2, "Muzzle2")
                ?? CreateAnchor(turretPitch2, "Muzzle2", new Vector3(0f, 0f, 0.85f));

            AudioSource fireSource2 = muzzle2.GetComponent<AudioSource>();
            if (fireSource2 == null)
                fireSource2 = CreateFireAudioSource(muzzle2);

            SerializedObject turretSo = new SerializedObject(turret);
            bool changed = AssignReference(turretSo, "profile", profile);
            changed |= AssignReference(turretSo, "turretYawPivot2", turretYaw2);
            changed |= AssignReference(turretSo, "turretPitchPivot2", turretPitch2);
            changed |= AssignReference(turretSo, "muzzle2", muzzle2);
            changed |= AssignReference(turretSo, "fireAudioSource2", fireSource2);
            if (changed)
                turretSo.ApplyModifiedPropertiesWithoutUndo();

            return changed;
        }

        private static bool WireSerializedReference(Object target, string propertyName, Object value)
        {
            if (target == null)
                return false;

            SerializedObject serializedObject = new SerializedObject(target);
            if (!AssignReference(serializedObject, propertyName, value))
                return false;

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
            return true;
        }

        private static bool AssignReference(SerializedObject serializedObject, string propertyName, Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || property.objectReferenceValue == value)
                return false;

            property.objectReferenceValue = value;
            return true;
        }

        private static Transform FindChildTransform(Transform root, string name)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == name)
                    return transforms[i];
            }

            return null;
        }

        private static AudioSource FindOrCreateChildAudioSource(Transform root, string name, bool loop)
        {
            Transform existing = root.Find(name);
            if (existing != null && existing.TryGetComponent(out AudioSource existingSource))
                return existingSource;

            GameObject audioObject = new GameObject(name);
            audioObject.transform.SetParent(root, false);
            AudioSource source = audioObject.AddComponent<AudioSource>();
            source.loop = loop;
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.minDistance = 4f;
            source.maxDistance = 45f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            return source;
        }

        private static ParticleSystem[] FindOrCreateThrusterSystems(Transform root)
        {
            Vector3[] localPositions =
            {
                new Vector3(-0.95f, 0.35f, -1.85f),
                new Vector3(0.95f, 0.35f, -1.85f),
                new Vector3(-0.95f, 0.35f, 1.85f),
                new Vector3(0.95f, 0.35f, 1.85f)
            };

            ParticleSystem[] systems = new ParticleSystem[localPositions.Length];
            for (int i = 0; i < localPositions.Length; i++)
            {
                string thrusterName = $"Thruster_{i + 1}";
                Transform anchor = FindChildTransform(root, thrusterName);
                if (anchor == null)
                {
                    anchor = CreateAnchor(root, thrusterName, localPositions[i]);
                    anchor.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                }

                ParticleSystem ps = anchor.GetComponent<ParticleSystem>();
                if (ps == null)
                {
                    ps = anchor.gameObject.AddComponent<ParticleSystem>();
                    ConfigureThrusterParticleSystem(ps);
                }

                systems[i] = ps;
            }

            return systems;
        }

        private static GameObject BuildHovercraftRoot(HovercraftProfile profile)
        {
            GameObject root = new GameObject("Hovercraft_Pioneer");
            Vector3 spawn = SceneView.lastActiveSceneView != null
                ? SceneView.lastActiveSceneView.pivot
                : Vector3.zero;
            spawn.y = SampleTerrainHeight(spawn) + profile.hoverHeight;
            root.transform.position = spawn;

            GameObject artPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourceArtPrefabPath);
            GameObject visual = null;
            if (artPrefab != null)
            {
                visual = (GameObject)PrefabUtility.InstantiatePrefab(artPrefab, root.transform);
                visual.name = "Visual";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                StripVisualColliders(visual);
            }
            else
            {
                Debug.LogWarning($"HovercraftSetupUtility: art prefab missing at {SourceArtPrefabPath}");
            }

            Rigidbody rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.mass = 850f;
            rigidbody.useGravity = false;
            rigidbody.linearDamping = profile.linearDrag;
            rigidbody.angularDamping = profile.angularDrag;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

            BoxCollider bodyCollider = root.AddComponent<BoxCollider>();
            bodyCollider.center = new Vector3(0f, 0.9f, 0f);
            bodyCollider.size = new Vector3(2.6f, 1.4f, 4.8f);

            Transform[] hoverPoints = CreateHoverRayPoints(root.transform);
            Transform enterPoint = CreateAnchor(root.transform, "EnterPoint", new Vector3(-1.6f, 0.4f, 2.2f));
            Transform exitPoint = CreateAnchor(root.transform, "ExitPoint", new Vector3(1.6f, 0.4f, 2.2f));
            Transform cockpitCamPoint = CreateAnchor(root.transform, "CockpitCamPoint", new Vector3(0f, 1.35f, 0.35f));
            cockpitCamPoint.localRotation = Quaternion.Euler(8f, 0f, 0f);
            Transform followCamPoint = CreateAnchor(root.transform, "FollowCamPoint", new Vector3(0f, 2.2f, -0.5f));
            Transform hiddenCrewHolder = CreateAnchor(root.transform, "HiddenCrewHolder", new Vector3(0f, 0.8f, 0f));
            Transform turretMount = CreateAnchor(root.transform, "TurretMount", new Vector3(0f, 1.55f, -0.8f));
            Transform turretYaw = CreateAnchor(turretMount, "TurretYaw", Vector3.zero);
            Transform turretPitch = CreateAnchor(turretYaw, "TurretPitch", Vector3.zero);
            Transform muzzle = CreateAnchor(turretPitch, "Muzzle", new Vector3(0f, 0f, 0.85f));

            Transform turretMount2 = CreateAnchor(root.transform, "TurretMount2", new Vector3(0f, 1.55f, 0.8f));
            Transform turretYaw2 = CreateAnchor(turretMount2, "TurretYaw2", Vector3.zero);
            Transform turretPitch2 = CreateAnchor(turretYaw2, "TurretPitch2", Vector3.zero);
            Transform muzzle2 = CreateAnchor(turretPitch2, "Muzzle2", new Vector3(0f, 0f, 0.85f));

            Camera vehicleCamera = CreateVehicleCamera(root.transform);
            AudioSource engineSource = CreateEngineAudioSource(root.transform);
            AudioSource vehicleOneShotSource = CreateVehicleOneShotAudioSource(root.transform);
            AudioSource fireSource = CreateFireAudioSource(muzzle);
            AudioSource fireSource2 = CreateFireAudioSource(muzzle2);
            ParticleSystem[] thrusterParticles = CreateThrusterParticles(root.transform);

            HoverPhysicsDriver physics = root.AddComponent<HoverPhysicsDriver>();
            HovercraftOccupancy occupancy = root.AddComponent<HovercraftOccupancy>();
            HovercraftCameraRig cameraRig = root.AddComponent<HovercraftCameraRig>();
            HovercraftTurretController turret = root.AddComponent<HovercraftTurretController>();
            HovercraftEngineAudio engineAudio = root.AddComponent<HovercraftEngineAudio>();
            HovercraftVehicleAudio vehicleAudio = root.AddComponent<HovercraftVehicleAudio>();
            HovercraftThrusterVfx thrusterVfx = root.AddComponent<HovercraftThrusterVfx>();
            HovercraftController controller = root.AddComponent<HovercraftController>();
            HovercraftFuelSystem fuelSystem = root.AddComponent<HovercraftFuelSystem>();
            HovercraftHealth health = root.AddComponent<HovercraftHealth>();
            HovercraftUsable usable = root.AddComponent<HovercraftUsable>();

            SerializedObject physicsSo = new SerializedObject(physics);
            physicsSo.FindProperty("profile").objectReferenceValue = profile;
            physicsSo.FindProperty("visualRoot").objectReferenceValue = visual != null ? visual.transform : null;
            physicsSo.FindProperty("hoverRayPoints").arraySize = hoverPoints.Length;
            for (int i = 0; i < hoverPoints.Length; i++)
                physicsSo.FindProperty("hoverRayPoints").GetArrayElementAtIndex(i).objectReferenceValue = hoverPoints[i];
            physicsSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject occupancySo = new SerializedObject(occupancy);
            occupancySo.FindProperty("enterPoint").objectReferenceValue = enterPoint;
            occupancySo.FindProperty("exitPoint").objectReferenceValue = exitPoint;
            occupancySo.FindProperty("hiddenCrewHolder").objectReferenceValue = hiddenCrewHolder;
            occupancySo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject cameraSo = new SerializedObject(cameraRig);
            cameraSo.FindProperty("profile").objectReferenceValue = profile;
            cameraSo.FindProperty("cockpitCamPoint").objectReferenceValue = cockpitCamPoint;
            cameraSo.FindProperty("followCamPoint").objectReferenceValue = followCamPoint;
            cameraSo.FindProperty("vehicleCamera").objectReferenceValue = vehicleCamera;
            cameraSo.FindProperty("physicsDriver").objectReferenceValue = physics;
            cameraSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject engineSo = new SerializedObject(engineAudio);
            engineSo.FindProperty("profile").objectReferenceValue = profile;
            engineSo.FindProperty("physicsDriver").objectReferenceValue = physics;
            engineSo.FindProperty("occupancy").objectReferenceValue = occupancy;
            engineSo.FindProperty("engineSource").objectReferenceValue = engineSource;
            engineSo.FindProperty("engineRunningClip").objectReferenceValue = null;
            engineSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject turretSo = new SerializedObject(turret);
            turretSo.FindProperty("profile").objectReferenceValue = profile;
            turretSo.FindProperty("turretYawPivot").objectReferenceValue = turretYaw;
            turretSo.FindProperty("turretPitchPivot").objectReferenceValue = turretPitch;
            turretSo.FindProperty("muzzle").objectReferenceValue = muzzle;
            turretSo.FindProperty("fireAudioSource").objectReferenceValue = fireSource;
            turretSo.FindProperty("turretFireClip").objectReferenceValue = null;
            turretSo.FindProperty("turretYawPivot2").objectReferenceValue = turretYaw2;
            turretSo.FindProperty("turretPitchPivot2").objectReferenceValue = turretPitch2;
            turretSo.FindProperty("muzzle2").objectReferenceValue = muzzle2;
            turretSo.FindProperty("fireAudioSource2").objectReferenceValue = fireSource2;
            turretSo.FindProperty("turretFireClip2").objectReferenceValue = null;
            turretSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject vehicleAudioSo = new SerializedObject(vehicleAudio);
            vehicleAudioSo.FindProperty("profile").objectReferenceValue = profile;
            vehicleAudioSo.FindProperty("oneShotSource").objectReferenceValue = vehicleOneShotSource;
            vehicleAudioSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject thrusterSo = new SerializedObject(thrusterVfx);
            thrusterSo.FindProperty("profile").objectReferenceValue = profile;
            thrusterSo.FindProperty("physicsDriver").objectReferenceValue = physics;
            thrusterSo.FindProperty("occupancy").objectReferenceValue = occupancy;
            SerializedProperty thrusterArray = thrusterSo.FindProperty("thrusterParticles");
            thrusterArray.arraySize = thrusterParticles.Length;
            for (int i = 0; i < thrusterParticles.Length; i++)
                thrusterArray.GetArrayElementAtIndex(i).objectReferenceValue = thrusterParticles[i];
            thrusterSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("profile").objectReferenceValue = profile;
            controllerSo.FindProperty("physicsDriver").objectReferenceValue = physics;
            controllerSo.FindProperty("occupancy").objectReferenceValue = occupancy;
            controllerSo.FindProperty("cameraRig").objectReferenceValue = cameraRig;
            controllerSo.FindProperty("turret").objectReferenceValue = turret;
            controllerSo.FindProperty("engineAudio").objectReferenceValue = engineAudio;
            controllerSo.FindProperty("vehicleAudio").objectReferenceValue = vehicleAudio;
            controllerSo.FindProperty("thrusterVfx").objectReferenceValue = thrusterVfx;
            controllerSo.FindProperty("fuelSystem").objectReferenceValue = fuelSystem;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            Project.Data.ItemData plasmaFuelItem =
                AssetDatabase.LoadAssetAtPath<Project.Data.ItemData>("Assets/_Project/Data/Items/Plasma Fuel.asset");
            SerializedObject fuelSo = new SerializedObject(fuelSystem);
            fuelSo.FindProperty("controller").objectReferenceValue = controller;
            fuelSo.FindProperty("physicsDriver").objectReferenceValue = physics;
            fuelSo.FindProperty("plasmaFuelItem").objectReferenceValue = plasmaFuelItem;
            fuelSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject usableSo = new SerializedObject(usable);
            usableSo.FindProperty("controller").objectReferenceValue = controller;
            usableSo.FindProperty("occupancy").objectReferenceValue = occupancy;
            usableSo.FindProperty("fuelSystem").objectReferenceValue = fuelSystem;
            usableSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject healthSo = new SerializedObject(health);
            healthSo.FindProperty("controller").objectReferenceValue = controller;
            healthSo.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        private static void StripVisualColliders(GameObject visualRoot)
        {
            MeshCollider[] meshColliders = visualRoot.GetComponentsInChildren<MeshCollider>(true);
            for (int i = 0; i < meshColliders.Length; i++)
                Object.DestroyImmediate(meshColliders[i]);

            Collider[] colliders = visualRoot.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                Object.DestroyImmediate(colliders[i]);
        }

        private static HovercraftProfile EnsureDefaultProfile()
        {
            HovercraftProfile existing = AssetDatabase.LoadAssetAtPath<HovercraftProfile>(ProfileAssetPath);
            if (existing != null)
                return existing;

            HovercraftProfile profile = ScriptableObject.CreateInstance<HovercraftProfile>();
            profile.weaponItem = AssetDatabase.LoadAssetAtPath<Project.Data.ItemData>(
                "Assets/_Project/Data/Items/sci_fi_pistol.asset");
            profile.ammoItem = AssetDatabase.LoadAssetAtPath<Project.Data.ItemData>(
                "Assets/_Project/Data/Items/ammo/Plasma.asset");

            AssetDatabase.CreateAsset(profile, ProfileAssetPath);
            return profile;
        }

        private static Transform[] CreateHoverRayPoints(Transform root)
        {
            Vector3[] localPositions =
            {
                new Vector3(-1.1f, 1.5f, 1.6f),
                new Vector3(1.1f, 1.5f, 1.6f),
                new Vector3(-1.1f, 1.5f, -1.6f),
                new Vector3(1.1f, 1.5f, -1.6f)
            };

            Transform[] points = new Transform[localPositions.Length];
            for (int i = 0; i < localPositions.Length; i++)
                points[i] = CreateAnchor(root, $"HoverRay_{i + 1}", localPositions[i]);

            return points;
        }

        private static Transform CreateAnchor(Transform parent, string name, Vector3 localPosition)
        {
            GameObject anchor = new GameObject(name);
            anchor.transform.SetParent(parent, false);
            anchor.transform.localPosition = localPosition;
            anchor.transform.localRotation = Quaternion.identity;
            return anchor.transform;
        }

        private static Camera CreateVehicleCamera(Transform root)
        {
            GameObject cameraObject = new GameObject("HovercraftCamera");
            cameraObject.transform.SetParent(root, false);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.tag = "MainCamera";
            camera.nearClipPlane = 0.08f;
            camera.fieldOfView = 68f;
            return camera;
        }

        private static AudioSource CreateEngineAudioSource(Transform root)
        {
            GameObject audioObject = new GameObject("EngineAudio");
            audioObject.transform.SetParent(root, false);
            AudioSource source = audioObject.AddComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.minDistance = 4f;
            source.maxDistance = 45f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            return source;
        }

        private static AudioSource CreateVehicleOneShotAudioSource(Transform root)
        {
            GameObject audioObject = new GameObject("VehicleAudio");
            audioObject.transform.SetParent(root, false);
            AudioSource source = audioObject.AddComponent<AudioSource>();
            source.loop = false;
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.minDistance = 4f;
            source.maxDistance = 45f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            return source;
        }

        private static ParticleSystem[] CreateThrusterParticles(Transform root)
        {
            Vector3[] localPositions =
            {
                new Vector3(-0.95f, 0.35f, -1.85f),
                new Vector3(0.95f, 0.35f, -1.85f),
                new Vector3(-0.95f, 0.35f, 1.85f),
                new Vector3(0.95f, 0.35f, 1.85f)
            };

            ParticleSystem[] systems = new ParticleSystem[localPositions.Length];
            for (int i = 0; i < localPositions.Length; i++)
            {
                Transform anchor = CreateAnchor(root, $"Thruster_{i + 1}", localPositions[i]);
                anchor.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                ParticleSystem ps = anchor.gameObject.AddComponent<ParticleSystem>();
                ConfigureThrusterParticleSystem(ps);
                systems[i] = ps;
            }

            return systems;
        }

        private static void ConfigureThrusterParticleSystem(ParticleSystem ps)
        {
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.startLifetime = 0.35f;
            main.startSpeed = 2.5f;
            main.startSize = 0.18f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.playOnAwake = false;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 8f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.05f;

            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        private static AudioSource CreateFireAudioSource(Transform muzzle)
        {
            AudioSource source = muzzle.gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.minDistance = 2f;
            source.maxDistance = 35f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            return source;
        }

        private static float SampleTerrainHeight(Vector3 worldPosition)
        {
            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
                return terrain.SampleHeight(worldPosition) + terrain.transform.position.y;

            if (Physics.Raycast(worldPosition + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 200f, ~0, QueryTriggerInteraction.Ignore))
                return hit.point.y;

            return worldPosition.y;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            if (!string.IsNullOrEmpty(parent))
                AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
#endif
