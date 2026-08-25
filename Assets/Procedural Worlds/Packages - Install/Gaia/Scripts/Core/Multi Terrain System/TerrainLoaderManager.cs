using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Search;
using UnityEditor.SearchService;


#endif
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using static UnityEngine.EventSystems.EventTrigger;

namespace Gaia
{

    public enum CenterSceneViewLoadingOn { SceneViewCamera, WorldOrigin }
    public enum CacheSizePreset { _Off, _1GB, _2GB, _3GB, _4GB, _5GB, _6GB, _7GB, _8GB, Custom }
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public class TerrainLoaderManager : MonoBehaviour
    {
        /// <summary>
        /// Loading Bounds around the world origin - controlled directly in the scene view and displays the part of the world the user wants to edit.
        /// Use Get/SetLoadingRange and Get/SetOrigin to access
        /// </summary>
        [SerializeField]
        private BoundsDouble m_sceneViewOriginLoadingBounds = new BoundsDouble(Vector3Double.zero, new Vector3Double(500f, 500f, 500f));
        [SerializeField]
        private BoundsDouble m_sceneViewImpostorLoadingBounds = new BoundsDouble(Vector3Double.zero, new Vector3Double(500f, 500f, 500f));
        [SerializeField]
        private BoundsDouble m_sceneViewCameraLoadingBounds = new BoundsDouble(Vector3Double.zero, new Vector3Double(500f, 500f, 500f));
        [SerializeField]
        private CenterSceneViewLoadingOn m_centerSceneViewLoadingOn = CenterSceneViewLoadingOn.WorldOrigin;
        [SerializeField]
        private DateTime m_assembliesReloadTimeStamp;
        [SerializeField]
        private string m_lastUsedGUID;
        private bool m_progressTrackingRunning;
        private long m_lastLoadingProgressTimeStamp;
        private float m_lastTrackedProgressValue;
        private Plane[] m_planes = new Plane[6];
        private Dictionary<Vector2Int, List<TerrainScene>> m_regularLoadRangesCache = new Dictionary<Vector2Int, List<TerrainScene>>();
        private Dictionary<Vector2Int, List<TerrainScene>> m_impostorLoadRangesCache = new Dictionary<Vector2Int, List<TerrainScene>>();
        [SerializeField]
        private List<int> m_manualLoadedRegularIndices = new List<int>();
        [SerializeField]
        private List<int> m_manualLoadedImpostorIndices = new List<int>();

        public float m_sceneViewLoadDelay = 0f;

#if GAIA_2023_PRO
        public List<FloatingPointFixMember> m_allFloatingPointFixMembers = new List<FloatingPointFixMember>();
        public List<ParticleSystem> m_allWorldSpaceParticleSystems = new List<ParticleSystem>();
        public GaiaLoadingScreen m_loadingScreen;
#endif
        public bool m_assumeGridLayout = true;
        public bool m_unloadUnusedAssetsRuntime = true;
        public bool m_unloadUnusedAssetsEditor = true;
        public bool m_autoTerrainStitching = true;
        public bool m_assembliesAreReloading = false;
        public int m_originTargetTileX;
        public int m_originTargetTileZ;
        public bool m_showOriginLoadingBounds;
        public bool m_showOriginTerrainBoxes;
        public CacheSizePreset m_cacheMemoryThresholdPreset = CacheSizePreset._4GB;
        public long m_cacheMemoryThreshold = 4294967296;
        public long m_cacheKeepAliveTime = 300000;
        public bool m_cacheInRuntime = true;
        public bool m_cacheInEditor = true;
        public int m_terrainLoadingTresholdMS = 100;
        public long m_lastTerrainLoadedTimeStamp = 0;
        public long m_trackLoadingProgressTimeOut = 10000;
        private bool m_showLocalTerrain = true;
        public Dictionary<Vector3, string> m_freeTerrainSlotsButtonPositions = new Dictionary<Vector3, string>();
        public bool m_autoSelectTLMOnGaiaPanelDrop = true;
        public bool m_autoToggleTerrainBoxes = true;
        public bool m_autoToggleTerrainBoxesTriggered = false;
        public bool m_showOriginTerrainBoxesLoaded = true;
        public bool m_showOriginTerrainBoxesUnLoaded = true;
        public bool m_showGoToTerrainButton = true;
        //public bool m_autoToggleGoToTerrainButton = true;
        public bool m_showSceneViewLoadingButttons = true;
        public bool m_collapseDistantButtons = true;
        public float m_collapseButtonsDistance = 2800f;
        public bool m_showSceneViewImpostorButttons = true;
        public bool m_showSceneViewEditButttons = false;
        public bool m_confirmOnSceneViewDelete = true;
        public bool m_copyPrototypesForNewTerrainTiles = true;
        //public bool m_showManualRefreshButton = false;
        public bool m_showSelectLoaderManagerButton = true;
        public bool m_showLockSceneViewLoadingButton = true;

        private GaiaSessionManager m_sessionManager;

        private GaiaSessionManager SessionManager
        {
            get
            {
                if (m_sessionManager == null)
                {
                    m_sessionManager = GaiaSessionManager.GetSessionManager(false);
                }
                return m_sessionManager;
            }
        }

        private List<TerrainSceneActionQueueEntry> m_terrainSceneActionQueue = new List<TerrainSceneActionQueueEntry>();

        private static TerrainLoaderManager instance = null;

        public static TerrainLoaderManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = GaiaUtils.GetTerrainLoaderManagerObject().GetComponent<TerrainLoaderManager>();
                }

                return instance;
            }
        }
        [SerializeField]
        private TerrainSceneStorage m_terrainSceneStorage;
        public TerrainSceneStorage TerrainSceneStorage
        {
            get
            {
                if (m_terrainSceneStorage == null)
                {
                    LoadStorageData();
                }
                return m_terrainSceneStorage;
            }
            set
            {
                if (m_terrainSceneStorage != value)
                {
                    //Remove all terrains from old storage file
                    UnloadAll(true);
                    m_terrainSceneStorage = value;
#if UNITY_EDITOR
                    m_lastUsedGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m_terrainSceneStorage));
#endif
                    //load back in the terrains from the new file
                    RefreshSceneViewLoadingRange();
                }
            }
        }

#if UNITY_EDITOR
        static TerrainLoaderManager()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged; 
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (GaiaUtils.GetTerrainLoaderManagerObject(false) == null)
            {
                //Make sure that we are not subscribed to the play mode change event anymore
                //if there is no actual terrain loader manager in the scene anymore
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                return;
            }

            //Remember all internal references that the Session Manager currently owns when exiting edit mode. We can then restore those later when exiting play mode.
            //This makes the state of loaded terrains more consistent between entering and exiting play mode.
            if (change == PlayModeStateChange.EnteredEditMode)
            {
                TerrainLoaderManager.Instance.RestoreManuallyLoadedTerrains();
            }
        }
#endif

        public static TerrainLoaderManager GetInstanceWithoutCreation()
        {
            if (instance == null)
            {
                GameObject go = GaiaUtils.GetTerrainLoaderManagerObject(false);
                if (go != null)
                {
                    instance = go.GetComponent<TerrainLoaderManager>();
                }
            }

            return instance;
        }

        public CenterSceneViewLoadingOn CenterSceneViewLoadingOn
        {
            get
            {
                return m_centerSceneViewLoadingOn;
            }
            set
            {
                if (m_centerSceneViewLoadingOn != value)
                {
                    m_centerSceneViewLoadingOn = value;
                    //if both loading ranges are set to 0, we return to the default loading ranges
                    if (m_centerSceneViewLoadingOn == CenterSceneViewLoadingOn.SceneViewCamera)
                    {
                        m_showOriginLoadingBounds = false;
                    }

                    if (GetLoadingRange() == 0 && GetImpostorLoadingRange() == 0)
                    {
                        Double regularRange = TerrainLoaderManager.GetDefaultLoadingRangeForTilesize(TerrainSceneStorage.m_terrainTilesSize);
                        SetLoadingRange(regularRange, regularRange * 3f);
                    }
                    else
                    {
                        SetLoadingRange(m_sceneViewCameraLoadingBounds.extents.x, m_sceneViewImpostorLoadingBounds.extents.x);
                    }
                }
            }
        }

        public void LockSceneViewLoading()
        {
            m_centerSceneViewLoadingOn = CenterSceneViewLoadingOn.WorldOrigin;
            RemoveAllReferencesOfGameObject(gameObject);
            SetLoadingRange(0f, 0f);
        }



        public void ResetStorage()
        {
            m_terrainSceneStorage = null;
        }

        public void LoadStorageData()
        {
#if UNITY_EDITOR
            GaiaSessionManager gsm = GaiaSessionManager.GetSessionManager();

            //Try to get the terrain scene storage file from the last used GUID first
            if (!String.IsNullOrEmpty(m_lastUsedGUID))
            {
                m_terrainSceneStorage = (TerrainSceneStorage)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(m_lastUsedGUID), typeof(TerrainSceneStorage));

            }

            //No guid / storage object? Then we need to create one in the current session directory
            if (m_terrainSceneStorage == null)
            {

                if (gsm != null && gsm.m_session != null)
                {
                    string path = GaiaDirectories.GetScenePath(gsm.m_session) + "/TerrainScenes.asset";
                    if (File.Exists(path))
                    {
                        m_terrainSceneStorage = (TerrainSceneStorage)AssetDatabase.LoadAssetAtPath(path, typeof(TerrainSceneStorage));
                    }
                    else
                    {
                        m_terrainSceneStorage = ScriptableObject.CreateInstance<TerrainSceneStorage>();
                        if (TerrainHelper.GetWorldMapTerrain() != null)
                        {
                            m_terrainSceneStorage.m_hasWorldMap = true;
                        }
                        AssetDatabase.CreateAsset(m_terrainSceneStorage, path);
                        AssetDatabase.ImportAsset(path);
                    }
                }
                else
                {
                    m_terrainSceneStorage = ScriptableObject.CreateInstance<TerrainSceneStorage>();
                }
            }

            //Do not load any old storage data in the midst of world creation - this causes issues in the world creation process!
            if (gsm.m_worldCreationRunning)
            {
                return;
            }

            //Check if there are scene files existing already and if they are in the storage data - if not, we should pick them up accordingly
            string directory = GaiaDirectories.GetTerrainScenePathForStorageFile(m_terrainSceneStorage);
            var dirInfo = new DirectoryInfo(directory);

            if (dirInfo != null)
            {
                FileInfo[] allFiles = dirInfo.GetFiles();
                foreach (FileInfo fileInfo in allFiles)
                {
                    if (fileInfo.Extension == ".unity")
                    {
                        string path = GaiaDirectories.GetPathStartingAtAssetsFolder(fileInfo.FullName);

                        if (!m_terrainSceneStorage.m_terrainScenes.Exists(x => x.GetTerrainName() == x.GetTerrainName(path)))
                        {
                            int xCoord = -99;
                            int zCoord = -99;
                            if (TerrainScene.GetCoordsFromFilename(fileInfo.Name, ref xCoord, ref zCoord))
                            {

                                //double centerX = (xCoord - (m_terrainSceneStorage.m_terrainTilesX / 2f)) * m_terrainSceneStorage.m_terrainTilesSize + (m_terrainSceneStorage.m_terrainTilesSize /2f);
                                //double centerZ = (zCoord - (m_terrainSceneStorage.m_terrainTilesZ / 2f)) * m_terrainSceneStorage.m_terrainTilesSize + (m_terrainSceneStorage.m_terrainTilesSize / 2f);
                                Vector2 offset = new Vector2(-m_terrainSceneStorage.m_terrainTilesSize * m_terrainSceneStorage.m_terrainTilesX * 0.5f, -m_terrainSceneStorage.m_terrainTilesSize * m_terrainSceneStorage.m_terrainTilesZ * 0.5f);
                                Vector3Double position = new Vector3(m_terrainSceneStorage.m_terrainTilesSize * xCoord + offset.x, 0, m_terrainSceneStorage.m_terrainTilesSize * zCoord + offset.y);
                                Vector3Double center = new Vector3Double(position + new Vector3Double(m_terrainSceneStorage.m_terrainTilesSize / 2f, 0f, m_terrainSceneStorage.m_terrainTilesSize / 2f));
                                BoundsDouble bounds = new BoundsDouble(center, new Vector3Double(m_terrainSceneStorage.m_terrainTilesSize, m_terrainSceneStorage.m_terrainTilesSize * 4, m_terrainSceneStorage.m_terrainTilesSize));
                                //Use forward slashes in the path - The Unity scene management classes expect it that way
                                path = path.Replace("\\", "/");
                                TerrainScene terrainScene = new TerrainScene()
                                {
                                    m_scenePath = path,
                                    m_pos = position,
                                    m_bounds = bounds,
                                    m_useFloatingPointFix = m_terrainSceneStorage.m_useFloatingPointFix
                                };

                                if (File.Exists(path.Replace("Terrain", GaiaConstants.ImpostorTerrainName)))
                                {
                                    terrainScene.m_impostorScenePath = path.Replace("Terrain", GaiaConstants.ImpostorTerrainName);
                                }

                                if (File.Exists(path.Replace("Terrain", "Collider")))
                                {
                                    terrainScene.m_colliderScenePath = path.Replace("Terrain", "Collider");
                                }

                                if (File.Exists(path.Replace("Terrain", "Backup")))
                                {
                                    terrainScene.m_backupScenePath = path.Replace("Terrain", "Backup");
                                }

                                m_terrainSceneStorage.m_terrainScenes.Add(terrainScene);
                            }
                        }
                    }
                }

                //Check if there is any terrain scene does not have its x/z coordinates initialized yet - if we are running a grid layout, we 
                //want to populate this now and store it
                if (m_assumeGridLayout)
                {
                    PopulateXZCoords();
                }

                m_lastUsedGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m_terrainSceneStorage));


                RefreshTerrainsWithCurrentData();

                RefreshSceneViewLoadingRange();

                ////Go over the currently open scene and close the ones that do not seem to have a reference on them
                //for (int i = EditorSceneManager.loadedSceneCount-1; i >= 0; i--)
                //{
                //    Scene scene = EditorSceneManager.GetSceneAt(i);
                //    if (EditorSceneManager.GetActiveScene().Equals(scene))
                //    {
                //        continue;
                //    }
                //      TerrainScene terrainScene = m_terrainSceneStorage.m_terrainScenes.Find(x => x.m_scenePath == scene.path || x.m_impostorScenePath == scene.path || x.m_colliderScenePath == scene.path);
                //      if (terrainScene != null)
                //      {
                //            terrainScene.UpdateWithCurrentData();
                //      }
                //      else
                //      {
                //            EditorSceneManager.UnloadSceneAsync(scene);
                //      }
                //}
            }
#endif
        }

        public void PopulateXZCoords()
        {
            bool madeChanges = false;

            List<TerrainScene> tempScenes = m_terrainSceneStorage.m_terrainScenes.Where(x => x.m_xCoord == int.MinValue || x.m_zCoord == int.MinValue).ToList();
            if (tempScenes.Count > 0)
            {
                //find the first terrain that we can find and figure out where the terrain with the terrain coordinates X=0 Z=0 would be positioned in world space
                TerrainScene firstScene = m_terrainSceneStorage.m_terrainScenes.First();
                string name = firstScene.GetTerrainName();

                if (m_terrainSceneStorage.m_pos00X == double.MinValue || m_terrainSceneStorage.m_pos00Z == double.MinValue)
                {
                    int firstSceneX = int.MinValue;
                    int firstSceneZ = int.MinValue;
                    Int32.TryParse(name.Substring(name.IndexOf('_'), (name.LastIndexOf('_') - name.IndexOf('_'))), out firstSceneX);
                    Int32.TryParse(name.Substring(name.LastIndexOf('_'), (name.IndexOf('-') - name.LastIndexOf('_') + 1)), out firstSceneZ);

                    if (firstSceneX == int.MinValue || firstSceneZ == int.MinValue)
                    {
                        Debug.LogError($"Could not correctly determine grid layout for the terrain scenes based on the first found terrain {firstScene.m_scenePath}");
                    }

                    m_terrainSceneStorage.m_pos00X = firstScene.m_pos.x - (firstSceneX * m_terrainSceneStorage.m_terrainTilesSize);
                    m_terrainSceneStorage.m_pos00Z = firstScene.m_pos.z - (firstSceneZ * m_terrainSceneStorage.m_terrainTilesSize);
                }

                for (int i = 0; i < tempScenes.Count; i++)
                {
                    TerrainScene terrainScene = tempScenes[i];
                    terrainScene.m_xCoord = Mathf.FloorToInt((float)(terrainScene.m_pos.x - m_terrainSceneStorage.m_pos00X) / m_terrainSceneStorage.m_terrainTilesSize);
                    terrainScene.m_zCoord = Mathf.FloorToInt((float)(terrainScene.m_pos.z - m_terrainSceneStorage.m_pos00Z) / m_terrainSceneStorage.m_terrainTilesSize);
                    madeChanges = true;
                }

                //make sure the amount of x-z tiles is right
                double minX = m_terrainSceneStorage.m_terrainScenes.Min(t => t.m_pos.x);
                double maxX = m_terrainSceneStorage.m_terrainScenes.Max(t => t.m_pos.x);
                double minZ = m_terrainSceneStorage.m_terrainScenes.Min(t => t.m_pos.z);
                double maxZ = m_terrainSceneStorage.m_terrainScenes.Max(t => t.m_pos.z);

                //the addition of the terrain size is needed because otherwise we are calculating with the lower left corner of the max terrains,
                //but we need the upper right corner to correctly calculate the number of the terrains
                int tilesX = Mathf.FloorToInt(((float)maxX + m_terrainSceneStorage.m_terrainTilesSize - (float)minX) / m_terrainSceneStorage.m_terrainTilesSize);
                int tilesZ = Mathf.FloorToInt(((float)maxZ + m_terrainSceneStorage.m_terrainTilesSize - (float)minZ) / m_terrainSceneStorage.m_terrainTilesSize);

                if (tilesX != m_terrainSceneStorage.m_terrainTilesX || tilesZ != m_terrainSceneStorage.m_terrainTilesZ)
                {
                    m_terrainSceneStorage.m_terrainTilesX = tilesX;
                    m_terrainSceneStorage.m_terrainTilesZ = tilesZ;
                    madeChanges = true;
                }
            }
            if (madeChanges)
            {
#if UNITY_EDITOR
                EditorUtility.SetDirty(m_terrainSceneStorage);
                AssetDatabase.SaveAssets();
#endif
            }
        }

        private Terrain m_worldMapTerrain;
        public Terrain WorldMapTerrain
        {
            get
            {
                if (m_worldMapTerrain == null)
                {
                    m_worldMapTerrain = TerrainHelper.GetWorldMapTerrain();
                }
                return m_worldMapTerrain;
            }
        }

        private GameObject m_terrainGO;
        public GameObject TerrainGO
        {
            get
            {
                if (m_terrainGO == null)
                {
                    m_terrainGO = GaiaUtils.GetTerrainObject();
                }
                return m_terrainGO;
            }
        }


        public static List<TerrainScene> TerrainScenes
        {
            get
            {
                return Instance.TerrainSceneStorage.m_terrainScenes;
            }
        }

        public static bool TerrainSceneStorageCreated
        {
            get
            {
                TerrainLoaderManager tlm = GetInstanceWithoutCreation();
                if (tlm != null)
                {
                    return tlm.m_terrainSceneStorage != null;
                }
                else
                {
                    return false;
                }
            }
        }


        private bool m_showWorldMapTerrain;
        public bool ShowWorldMapTerrain
        {
            get
            {
                return m_showWorldMapTerrain;
            }
            private set
            {
                m_showWorldMapTerrain = value;
                if (WorldMapTerrain != null)
                {
                    if (m_showWorldMapTerrain)
                    {
                        WorldMapTerrain.gameObject.SetActive(true);
                    }
                    else
                    {
                        WorldMapTerrain.gameObject.SetActive(false);
                    }
                }
            }
        }


        /// <summary>
        /// Used to determine if the terrain loader manager is ready to start loading terrains during runtime
        /// </summary>
        private bool m_runtimeInitialized;
        private GameObject m_runtimeFollowTarget;
        private GameObject m_runtimeCameraTarget;
        private bool m_hidBakedGaiaTerrains;
        [SerializeField] private double m_lastNonZeroLoadingRange = 2;

        public bool RuntimeIsInitialized
        {
            get { return m_runtimeInitialized; }
        }

        public bool ShowLocalTerrain
        {
            get
            {
                return m_showLocalTerrain;
            }
            private set
            {
                if (value != m_showLocalTerrain)
                {
                    m_showLocalTerrain = value;
                    if (GaiaUtils.HasDynamicLoadedTerrains())
                    {
                        if (!m_showLocalTerrain)
                        {
                            TerrainLoaderManager.Instance.UnloadAll();
                        }
                    }
                    else
                    {

                        foreach (Transform child in TerrainGO.transform)
                        {
                            Terrain t = child.GetComponent<Terrain>();
                            if (t != null)
                            {
                                t.drawHeightmap = m_showLocalTerrain;
                                t.drawTreesAndFoliage = m_showLocalTerrain;
                                //Activate / deactivate all Childs below the terrain
                                foreach (Transform subTrans in t.transform)
                                {
                                    subTrans.gameObject.SetActive(m_showLocalTerrain);
                                }
                            }
                        }
                    }
                }
            }
        }

        public static bool ColliderOnlyLoadingActive
        {
            get
            {
                return Instance.TerrainSceneStorage.m_colliderOnlyLoading;
            }
        }
#if GAIA_2023_PRO
        /// <summary>
        /// Triggered when load progress tracking starts in the Terrain Loader Manager.
        /// </summary>
        public delegate void LoadProgressStartedCallback();
        public event LoadProgressStartedCallback OnLoadProgressStarted;

        /// <summary>
        /// Triggered when there is an update on the load progress during load progress tracking.
        /// </summary>
        /// <param name="progress">The load progress expressed as scalar value (0 to 1)</param>
        public delegate void LoadProgressUpdatedCallback(float progress);
        public event LoadProgressUpdatedCallback OnLoadProgressUpdated;

        /// <summary>
        /// Triggered when load progress tracking ends in the Terrain Loader Manager.
        /// </summary>
        public delegate void LoadProgressEndedCallback();
        public event LoadProgressEndedCallback OnLoadProgressEnded;

        /// <summary>
        /// Triggered when load progress tracking times out in the Terrain Loader Manager.
        /// </summary>
        /// <param name="missingTerrainScenes">List of terrain scenes which are referenced, but not loaded yet at the point of the timeout.</param>
        public delegate void LoadProgressTimeOutCallback(List<TerrainScene> missingTerrainScenes);
        public event LoadProgressTimeOutCallback OnLoadProgressTimeOut;
#endif

        public void Start()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                if (instance != this)
                {
                    Destroy(this);
                }
            }

            //Nothing to do if no terrain loading
            if (!GaiaUtils.HasDynamicLoadedTerrains())
            {
                return;
            }

            LookUpLoadingScreen();

            //Process the runtime deactivation if the "Collider Only" mode is active
            if (TerrainSceneStorage.m_colliderOnlyLoading)
            {
                DeactivateIfRequested(m_terrainSceneStorage.m_deactivateRuntimePlayer, GaiaConstants.gaiaPlayerObject);
                DeactivateIfRequested(m_terrainSceneStorage.m_deactivateRuntimeLighting, GaiaConstants.gaiaLightingObject);
                DeactivateIfRequested(m_terrainSceneStorage.m_deactivateRuntimeAudio, GaiaConstants.gaiaAudioObject);
                DeactivateIfRequested(m_terrainSceneStorage.m_deactivateRuntimeWeather, GaiaConstants.gaiaWeatherObject);
                DeactivateIfRequested(m_terrainSceneStorage.m_deactivateRuntimeWater, GaiaConstants.gaiaWaterObject);
                DeactivateIfRequested(m_terrainSceneStorage.m_deactivateRuntimeScreenShotter, GaiaConstants.gaiaScreenshotter);
            }

            UnloadAll();
            m_runtimeInitialized = true;

#if GAIA_2023_PRO
            if (m_loadingScreen != null)
            {
                m_loadingScreen.gameObject.SetActive(true);
            }

            StartTrackingProgress();
#endif
        }

        private void RestoreManuallyLoadedTerrains()
        {
            foreach (int i in m_manualLoadedRegularIndices)
            {
                TerrainSceneStorage.m_terrainScenes[i].AddRegularReference(SessionManager.gameObject);
            }
            foreach (int i in m_manualLoadedImpostorIndices)
            {
                TerrainSceneStorage.m_terrainScenes[i].AddImpostorReference(SessionManager.gameObject);
            }

            SetLoadingRange(GetLoadingRange(), GetImpostorLoadingRange());


            //Remove terrains that might linger around because the unity editor tries to load in the same scenes that were active before playmode
            //if the user loads terrains in based on the scene view camera and has since moved the scene view around, this might lead to the
            //Unity editor loading in scenes that we actually do not want to load after play mode.
            //foreach (TerrainScene ts in TerrainSceneStorage.m_terrainScenes.Where(x=>x.RegularReferences.Count==0))
            //{
            //    ts.RemoveAllReferences();
            //}

            //foreach (TerrainScene ts in TerrainSceneStorage.m_terrainScenes.Where(x => x.ImpostorReferences.Count == 0))
            //{
            //    ts.RemoveAllImpostorReferences();
            //}


            //List<string> paths = new List<string>();
            //for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            //{
            //    UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetSceneAt(i);
            //    paths.Add(scene.path);
            //}

            //for (int i = 0; i < m_terrainSceneStorage.m_terrainScenes.Count; i++)
            //{
            //    TerrainScene terrainScene = m_terrainSceneStorage.m_terrainScenes[i];

            //    if (paths.Contains(terrainScene.m_scenePath) && terrainScene.RegularReferences.Count==0)
            //    {
            //        foreach (GameObject go in EditorSceneManager.GetSceneByPath(terrainScene.m_scenePath).GetRootGameObjects())
            //        { 
            //            //check if the scene is cached, only if the terrain is active we treat it as loaded
            //            Terrain terrain = go.GetComponent<Terrain>();
            //            if (terrain != null && go.activeInHierarchy)
            //            {
            //                terrainScene.AddRegularReference(SessionManager.gameObject);
            //            }
            //        }
            //    }
            //    if (paths.Contains(terrainScene.m_impostorScenePath) && terrainScene.ImpostorReferences.Count == 0)
            //    {
            //        foreach (GameObject go in EditorSceneManager.GetSceneByPath(terrainScene.m_scenePath).GetRootGameObjects())
            //        {
            //            //if we find any active object in an impostor scene, we assume the impostor is active and therefore the scene is not cached
            //            if (go.activeInHierarchy)
            //            {
            //                terrainScene.AddImpostorReference(SessionManager.gameObject);
            //                break;
            //            }
            //        }
            //    }
            // }


            //for (int i = 0; i < m_sessionManagerRegularIndices.Count(); i++)
            //{
            //    TerrainSceneStorage.m_terrainScenes[i].AddRegularReference(SessionManager.gameObject);
            //}
            //for (int i = 0; i < m_sessionManagerImpostorIndices.Count(); i++)
            //{
            //    TerrainSceneStorage.m_terrainScenes[i].AddImpostorReference(SessionManager.gameObject);
            //}
        }

        public void CheckAssemblyReloadThreshold()
        {
            if ((DateTime.Now - m_assembliesReloadTimeStamp).TotalMilliseconds > 10000)
            {
                m_assembliesAreReloading = false;
            }
        }

        private void Update()
        {
#if GAIA_2023_PRO
            if (m_progressTrackingRunning)
            {
                float progress = 0f;
                int loadedScenesCount = TerrainSceneStorage.m_terrainScenes.FindAll(x => x.RegularReferences.Count > 0 && x.m_regularLoadState == LoadState.Loaded).Count;
                int referencedScenesCount = TerrainSceneStorage.m_terrainScenes.FindAll(x => x.RegularReferences.Count > 0).Count;
                int loadedScenesImpostorCount = TerrainSceneStorage.m_terrainScenes.FindAll(x => x.RegularReferences.Count == 0 && x.ImpostorReferences.Count > 0 && x.m_impostorLoadState == LoadState.Loaded).Count;
                int referencedImpostorScenesCount = TerrainSceneStorage.m_terrainScenes.FindAll(x => x.RegularReferences.Count == 0 && x.ImpostorReferences.Count > 0).Count;
                float loadedScenesTotal = loadedScenesCount + loadedScenesImpostorCount;
                float referencedScenesTotal = referencedScenesCount + referencedImpostorScenesCount;
                if (referencedScenesTotal > 0)
                {
                    progress = loadedScenesTotal / referencedScenesTotal;
                }
                else
                {
                    m_progressTrackingRunning = false;
                    if (OnLoadProgressEnded != null)
                    {
                        OnLoadProgressEnded();
                    }
                }

                if (OnLoadProgressUpdated != null)
                {
                    OnLoadProgressUpdated(progress);
                }

                if (progress >= 1f)
                {
                    m_progressTrackingRunning = false;
                    if (OnLoadProgressEnded != null)
                    {
                        OnLoadProgressEnded();
                    }
                }
                else
                {
                    long currentTimeStamp = GaiaUtils.GetUnixTimestamp();

                    //Check if we made progress since the last update
                    if (progress != m_lastTrackedProgressValue)
                    {
                        //we made loading progress, update the progress and timestamp
                        m_lastTrackedProgressValue = progress;
                        m_lastLoadingProgressTimeStamp = currentTimeStamp;
                    }
                    else
                    {
                        //no load progress anymore? Time out the loading tracking eventually
                        if (m_lastLoadingProgressTimeStamp + m_trackLoadingProgressTimeOut < currentTimeStamp)
                        {
                            m_progressTrackingRunning = false;

                            if (OnLoadProgressTimeOut != null)
                            {
                                List<TerrainScene> missingScenes = m_terrainSceneStorage.m_terrainScenes.FindAll(x => (x.RegularReferences.Count > 0 && x.m_regularLoadState != LoadState.Loaded) || (x.ImpostorReferences.Count > 0 && x.m_impostorLoadState != LoadState.Loaded));
                                OnLoadProgressTimeOut(missingScenes);
                            }

                            if (OnLoadProgressEnded != null)
                            {
                                OnLoadProgressEnded();
                            }
                        }
                    }
                }
            }
#endif
            if (m_terrainSceneActionQueue.Count > 0)
            {
                long currentTimeStamp = GaiaUtils.GetUnixTimestamp();
                //if this happens, a new load / unload request has already been started and we must stop processing the queue for now.
                if (m_lastTerrainLoadedTimeStamp + m_terrainLoadingTresholdMS < currentTimeStamp)
                {
                    //string message = "";
                    //for (int i = 0; i < m_terrainSceneActionQueue.Count; i++)
                    //{
                    //    message += $"Queue entry {i}: {m_terrainSceneActionQueue[i].m_terrainScene.GetTerrainName()} , distance {m_terrainSceneActionQueue[i].m_distance}, in Frustum: {m_terrainSceneActionQueue[i].m_inFrustum}\r\n";
                    //}
                    //Debug.Log(message);

                    for (int i = 0; i < m_terrainSceneActionQueue.Count; i++)
                    {
                        if (m_lastTerrainLoadedTimeStamp > currentTimeStamp)
                        {
                            break;
                        }
                        ProcessActionQueueEntry(m_terrainSceneActionQueue[i]);
                        m_terrainSceneActionQueue[i] = null;
                    }
                    //remove processed entries
                    m_terrainSceneActionQueue.RemoveAll(x => x == null);
                }
            }

            RefreshRuntimePlayerLoading();
        }

        /// <summary>
        /// Play-mode only: stream tiles around Player_v7 using TLM loading range (meters).
        /// </summary>
        private void RefreshRuntimePlayerLoading()
        {
            if (!Application.isPlaying || !m_runtimeInitialized)
            {
                return;
            }

            if (!GaiaUtils.HasDynamicLoadedTerrains())
            {
                return;
            }

            if (!m_hidBakedGaiaTerrains)
            {
                m_hidBakedGaiaTerrains = true;
                GameObject baked = GameObject.Find("Gaia Terrains");
                if (baked != null)
                {
                    baked.SetActive(false);
                }
            }

            double range = GetLoadingRange();
            if (range <= 0)
            {
                range = m_lastNonZeroLoadingRange > 0 ? m_lastNonZeroLoadingRange : 2;
            }

            if (GetImpostorLoadingRange() < 1000)
            {
                m_sceneViewImpostorLoadingBounds.extents = new Vector3Double(3500, 3500, 3500);
            }

            m_cacheInRuntime = true;
            m_unloadUnusedAssetsRuntime = false;

            GameObject player = FindRuntimePlayer();
            GameObject cameraGo = FindRuntimeCamera();
            GameObject origin = player != null ? player : cameraGo;

            RequestRuntimeTilesAround(origin, range);

            if (m_runtimeFollowTarget != null && m_runtimeFollowTarget != origin)
            {
                RemoveAllReferencesOfGameObject(m_runtimeFollowTarget);
            }
            if (m_runtimeCameraTarget != null && m_runtimeCameraTarget != origin)
            {
                RemoveAllReferencesOfGameObject(m_runtimeCameraTarget);
            }

            m_runtimeFollowTarget = origin;
            m_runtimeCameraTarget = origin;
        }

        private static GameObject FindRuntimePlayer()
        {
            GameObject player = GameObject.Find("Player_v7");
            if (player != null && player.activeInHierarchy)
            {
                return player;
            }
            return null;
        }

        private static GameObject FindRuntimeCamera()
        {
            if (Camera.main != null && Camera.main.gameObject.activeInHierarchy)
            {
                return Camera.main.gameObject;
            }

            GameObject named = GameObject.Find("vThirdPersonCamera");
            if (named != null && named.activeInHierarchy)
            {
                return named;
            }

            Camera[] cameras = Camera.allCameras;
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera cam = cameras[i];
                if (cam != null && cam.enabled && cam.gameObject.activeInHierarchy)
                {
                    return cam.gameObject;
                }
            }
            return null;
        }

        private void RequestRuntimeTilesAround(GameObject requester, double range)
        {
            if (requester == null || !requester.activeInHierarchy || m_terrainSceneStorage == null)
            {
                return;
            }

            Vector3 pos = requester.transform.position;

            // Four nearest regular tiles around the player. Impostors cover the rest.
            double tile = m_terrainSceneStorage.m_terrainTilesSize > 0 ? m_terrainSceneStorage.m_terrainTilesSize : 2048;
            int tx = GetTerrainX(pos.x);
            int tz = GetTerrainZ(pos.z);
            double cx = m_terrainSceneStorage.m_pos00X + tx * tile + tile * 0.5;
            double cz = m_terrainSceneStorage.m_pos00Z + tz * tile + tile * 0.5;
            BoundsDouble regular = new BoundsDouble(
                new Vector3Double(cx, pos.y, cz),
                new Vector3Double(tile * 0.75, tile, tile * 0.75));

            double impostorRange = GetImpostorLoadingRange();
            if (impostorRange < 1000)
            {
                impostorRange = 3500;
            }
            BoundsDouble impostor = new BoundsDouble(
                new Vector3Double(pos.x, pos.y, pos.z),
                new Vector3Double(impostorRange * 2.0, impostorRange * 2.0, impostorRange * 2.0));
            Camera cam = null;
            if (Camera.main != null && Camera.main.isActiveAndEnabled)
            {
                cam = Camera.main;
            }
            UpdateTerrainLoadState(regular, impostor, requester, 0, 0, 0, 0, cam);
            AddNearestRegularTiles(requester, pos, tx, tz, cam, 4);
        }

        private void AddNearestRegularTiles(GameObject requester, Vector3 pos, int tx, int tz, Camera cam, int maxRegular)
        {
            if (requester == null || m_terrainSceneStorage == null || m_terrainSceneStorage.m_terrainScenes == null)
            {
                return;
            }

            List<TerrainScene> extras = new List<TerrainScene>();
            for (int i = 0; i < m_terrainSceneStorage.m_terrainScenes.Count; i++)
            {
                TerrainScene ts = m_terrainSceneStorage.m_terrainScenes[i];
                if (ts == null || (ts.m_xCoord == tx && ts.m_zCoord == tz))
                {
                    continue;
                }
                extras.Add(ts);
            }

            extras.Sort((a, b) =>
            {
                float dax = pos.x - (float)a.m_bounds.center.x;
                float daz = pos.z - (float)a.m_bounds.center.z;
                float dbx = pos.x - (float)b.m_bounds.center.x;
                float dbz = pos.z - (float)b.m_bounds.center.z;
                return (dax * dax + daz * daz).CompareTo(dbx * dbx + dbz * dbz);
            });

            int extraSlots = maxRegular - 1;
            HashSet<TerrainScene> kept = new HashSet<TerrainScene>();
            for (int i = 0; i < extras.Count && i < extraSlots; i++)
            {
                TerrainScene ts = extras[i];
                kept.Add(ts);
                float dx = pos.x - (float)ts.m_bounds.center.x;
                float dz = pos.z - (float)ts.m_bounds.center.z;
                AddToTerrainSceneActionQueue(ts, ReferenceChange.AddRegularReference, requester, Mathf.Sqrt(dx * dx + dz * dz), false, cam);
            }

            PrefetchLookAheadTile(requester, pos, extras, kept, cam);
        }

        private void PrefetchLookAheadTile(GameObject requester, Vector3 pos, List<TerrainScene> extras, HashSet<TerrainScene> kept, Camera cam)
        {
            Vector3 dir = Vector3.zero;
            if (cam != null)
            {
                dir = cam.transform.forward;
            }
            else if (requester != null)
            {
                dir = requester.transform.forward;
            }
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f)
            {
                return;
            }
            dir.Normalize();

            TerrainScene best = null;
            float bestDot = 0.35f;
            for (int i = 0; i < extras.Count; i++)
            {
                TerrainScene ts = extras[i];
                if (ts == null || kept.Contains(ts))
                {
                    continue;
                }

                Vector3 to = new Vector3((float)ts.m_bounds.center.x - pos.x, 0f, (float)ts.m_bounds.center.z - pos.z);
                if (to.sqrMagnitude < 1f)
                {
                    continue;
                }
                float d = Vector3.Dot(dir, to.normalized);
                if (d > bestDot)
                {
                    bestDot = d;
                    best = ts;
                }
            }

            if (best == null)
            {
                return;
            }

            float dx = pos.x - (float)best.m_bounds.center.x;
            float dz = pos.z - (float)best.m_bounds.center.z;
            AddToTerrainSceneActionQueue(best, ReferenceChange.AddRegularReference, requester, Mathf.Sqrt(dx * dx + dz * dz), false, cam);
        }

        public void BeginStaggeredTerrainWake(TerrainScene terrainScene, Terrain terrain)
        {
            if (!isActiveAndEnabled)
            {
                if (terrainScene != null)
                {
                    terrainScene.FinishStaggeredWake(terrain);
                }
                return;
            }
            StartCoroutine(StaggeredTerrainWake(terrainScene, terrain));
        }

        private IEnumerator StaggeredTerrainWake(TerrainScene terrainScene, Terrain terrain)
        {
            yield return null;
            if (terrain != null)
            {
                terrain.drawTreesAndFoliage = true;
            }
            yield return null;
            if (terrainScene != null)
            {
                terrainScene.FinishStaggeredWake(terrain);
            }
        }

        /// <summary>
        /// Clears the current scene action queue. Can be used to cancel all queued up loading / unloading operations.
        /// </summary>
        /// <param name="forceLoaderRefresh">Forces a refresh on all terrain loaders in the scene </param>
        public void ClearTerrainSceneActionQueue(bool forceLoaderRefresh)
        {
#if GAIA_2023_PRO
            m_terrainSceneActionQueue.Clear();
            m_lastTerrainLoadedTimeStamp = long.MinValue;

            if (forceLoaderRefresh)
            {
                TerrainLoader[] allLoaders = GaiaUtils.FindObjectsByType<TerrainLoader>(FindObjectsSortMode.None);
                foreach (TerrainLoader loader in allLoaders)
                {
                    loader.Refresh();
                }
            }
#endif
        }

        public void StartTrackingProgress()
        {
#if GAIA_2023_PRO
            //Update all runtime loaders to make sure the current references are set when we start tracking
            var allLoaders = Resources.FindObjectsOfTypeAll<TerrainLoader>();
            foreach (TerrainLoader terrainLoader in allLoaders.Where(x => x.LoadMode == LoadMode.RuntimeAlways))
            {
                terrainLoader.UpdateTerrains();
            }

            m_progressTrackingRunning = true;
            m_lastLoadingProgressTimeStamp = GaiaUtils.GetUnixTimestamp();

            if (OnLoadProgressStarted != null)
            {
                OnLoadProgressStarted();
            }
#endif
        }

        public void LookUpLoadingScreen()
        {
#if GAIA_2023_PRO
            if (m_loadingScreen == null && GaiaUtils.HasDynamicLoadedTerrains())
            {
                var loadingScreens = Resources.FindObjectsOfTypeAll<GaiaLoadingScreen>();
                if (loadingScreens.Length > 0)
                {
                    m_loadingScreen = loadingScreens[0];
                }
            }
#endif
        }

        /// <summary>
        /// Deactivates the Game Object with the given name if requested & if it exists
        /// </summary>
        /// <param name="requested">If the deactivation is requested</param>
        /// <param name="gameObjectName">The name of the Game Object that will be deactivated</param>
        private void DeactivateIfRequested(bool requested, string gameObjectName)
        {
            if (requested)
            {
                GameObject gameObject = GameObject.Find(gameObjectName);
                if (gameObject != null)
                {
                    gameObject.SetActive(false);
                }
            }
        }

        public void Reset()
        {
#if GAIA_2023_PRO
            m_allFloatingPointFixMembers.Clear();
            m_allWorldSpaceParticleSystems.Clear();
#endif
        }

        void OnApplicationQuit()
        {
#if UNITY_EDITOR
            //Unload everything after runtime stopped 
            UnloadAll();
#endif

        }

        private void OnEnable()
        {

            if (instance == null)
            {
                instance = this;
            }
            else
            {
                if (instance != this)
                {
                    Destroy(this);
                }
            }
            GaiaSettings gaiaSettings = GaiaUtils.GetGaiaSettings();

            m_assembliesAreReloading = false;

            SubscribeToAssemblyReloadEvents();
        }

        

        public void Awake()
        {
            AssureGridMetaData();
            UpdateTerrainLoadState();
        }

        public void AssureGridMetaData()
        {
            //Check if there is any terrain scene does not have its grid layout metadata yet - if we are running a grid layout, we 
            //want to populate this now and store it
            if (m_assumeGridLayout)
            {
                if (m_terrainSceneStorage == null)
                    LoadStorageData();
                if (m_terrainSceneStorage == null || m_terrainSceneStorage.m_terrainScenes == null)
                    return;
                List<TerrainScene> tempScenes = m_terrainSceneStorage.m_terrainScenes.Where(x => x != null && (x.m_xCoord == int.MinValue || x.m_zCoord == int.MinValue)).ToList();
                if (tempScenes.Count > 0 || TerrainSceneStorage.m_pos00X == double.MinValue || TerrainSceneStorage.m_pos00Z == double.MinValue)
                {
                    LoadStorageData();
                }
            }
        }

        public void SubscribeToAssemblyReloadEvents()
        {
#if UNITY_EDITOR
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
#endif
        }

        void OnDisable()
        {
            m_assembliesAreReloading = false;
#if UNITY_EDITOR
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
#endif
        }

        public void OnBeforeAssemblyReload()
        {
            m_assembliesAreReloading = true;
            m_assembliesReloadTimeStamp = DateTime.Now;
        }

        public void OnAfterAssemblyReload()
        {
            m_assembliesAreReloading = false;
        }

        public void ResetTimestamps()
        {
            foreach (TerrainScene ts in m_terrainSceneStorage.m_terrainScenes)
            {
                ts.m_impostorCachedTimestamp = 0;
                ts.m_regularCachedTimestamp = 0;
                ts.m_nextUpdateTimestamp = 0;
            }
        }

        public Vector3Double GetOrigin()
        {
            return new Vector3Double(m_sceneViewOriginLoadingBounds.center);
        }

        public void SetOrigin(Vector3Double newOrigin)
        {
#if GAIA_2023_PRO
            if (newOrigin != m_sceneViewOriginLoadingBounds.center)
            {
                //a origin shift has occured, 
                Vector3Double shiftDifference = newOrigin - m_sceneViewOriginLoadingBounds.center;

                //Don't shift on y-axis this will only lead to problems with sea level, height based rules, etc.
                //and should not be required under normal circumstances.
                shiftDifference.y = 0;

                //shift all tools such as stampers and spawners
                //Stamper[] allStampers = Resources.FindObjectsOfTypeAll<Stamper>();
                //foreach (Stamper stamper in allStampers)
                //{
                //    stamper.transform.position = (Vector3)((Vector3Double)stamper.transform.position + m_originLoadingBounds.center - shiftDifference);
                //}

                //if not in playmode, shift the player, if exists, very confusing otherwise
                if (!Application.isPlaying)
                {
                    GameObject playerObj = GameObject.Find(GaiaConstants.playerFlyCamName);

                    if (playerObj == null)
                    {
                        playerObj = GameObject.Find(GaiaConstants.playerFirstPersonName);
                    }

                    if (playerObj == null)
                    {
                        playerObj = GameObject.Find(GaiaConstants.playerThirdPersonName);
                    }

                    if (playerObj != null)
                    {
                        playerObj.transform.position = (Vector3)((Vector3Double)playerObj.transform.position - shiftDifference);
                    }

                    //Move spawners also only when not in playmode
                    Spawner[] allSpawners = Resources.FindObjectsOfTypeAll<Spawner>();
                    foreach (Spawner spawner in allSpawners)
                    {
                        spawner.transform.position = (Vector3)((Vector3Double)spawner.transform.position - shiftDifference);
                    }

                    //Move all poly masks when not in play mode
                    PolyMask[] allPolyMasks = Resources.FindObjectsOfTypeAll<PolyMask>();
                    foreach (PolyMask polyMask in allPolyMasks)
                    {
                        polyMask.transform.position = (Vector3)((Vector3Double)polyMask.transform.position - shiftDifference);
                        polyMask.ProcessTransformUpdate();
                    }


                    //When the application is not playing we can look for all floating point fix members, if it is playing we should
                    //rely on the list of members being filled correctly at the start of the scene
                    m_allFloatingPointFixMembers = Resources.FindObjectsOfTypeAll<FloatingPointFixMember>().ToList();
                }
                m_allFloatingPointFixMembers.RemoveAll(x => x == null);
                foreach (FloatingPointFixMember member in m_allFloatingPointFixMembers)
                {
                    member.transform.position = (Vector3)((Vector3Double)member.transform.position - shiftDifference);
                }


                //shift world space particles accordingly - only worth dealing with during playmode 
                if (Application.isPlaying)
                {
                    m_allWorldSpaceParticleSystems.RemoveAll(x => x == null);
                    foreach (ParticleSystem ps in m_allWorldSpaceParticleSystems)
                    {
                        bool wasPaused = ps.isPaused;
                        bool wasPlaying = ps.isPlaying;
                        ParticleSystem.Particle[] currentParticles = null;

                        if (!wasPaused)
                            ps.Pause();

                        if (currentParticles == null || currentParticles.Length < ps.main.maxParticles)
                        {
                            currentParticles = new ParticleSystem.Particle[ps.main.maxParticles];
                        }

                        int num = ps.GetParticles(currentParticles);

                        for (int i = 0; i < num; i++)
                        {
                            currentParticles[i].position -= (Vector3)shiftDifference;
                        }

                        ps.SetParticles(currentParticles, num);

                        if (wasPlaying)
                            ps.Play();
                    }
                }

                m_sceneViewOriginLoadingBounds.center = newOrigin;
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif

                if (m_terrainSceneStorage != null && m_terrainSceneStorage.m_terrainScenes != null)
                {
                    foreach (TerrainScene terrainScene in m_terrainSceneStorage.m_terrainScenes.Where(x => x.m_regularLoadState == LoadState.Loaded || x.m_regularLoadState == LoadState.Cached || x.m_impostorLoadState == LoadState.Loaded || x.m_impostorLoadState == LoadState.Cached))
                    {
                        terrainScene.ShiftLoadedTerrain();
                    }
                }
                
            }
#endif
        }

        public void SetOriginByTargetTile(int tileX = -99, int tileZ = -99)
        {
            if (tileX == -99)
            {
                tileX = m_originTargetTileX;
            }

            if (tileZ == -99)
            {
                tileZ = m_originTargetTileZ;
            }

            if (GaiaUtils.HasDynamicLoadedTerrains())
            {
                //Get the terrain tile by X / Z tile in the scene path
                TerrainScene targetScene = TerrainLoaderManager.TerrainScenes.Find(x => x.m_scenePath.Contains("Terrain_" + tileX.ToString() + "_" + tileZ.ToString()));
                if (targetScene != null)
                {
                    SetOrigin(new Vector3Double(targetScene.m_pos.x + (m_terrainSceneStorage.m_terrainTilesSize / 2f), 0f, targetScene.m_pos.z + (m_terrainSceneStorage.m_terrainTilesSize / 2f)));
                    string terrainName = targetScene.GetTerrainName();
                    GameObject go = GameObject.Find(terrainName);
                    if (go != null)
                    {
#if UNITY_EDITOR
                        Selection.activeObject = go;
#endif
                    }
                }
                else
                {
                    Debug.LogWarning("Could not find a terrain with the tile coordinates " + tileX.ToString() + "-" + tileZ.ToString() + " in the available terrains. Please check if these coordinates are within the available bounds.");
                }
            }
            else
            {
                Terrain t = Terrain.activeTerrains.Where(x => x.name.Contains("Terrain_" + tileX.ToString() + "_" + tileZ.ToString())).First();
                if (t != null)
                {
                    SetOrigin(new Vector3Double(t.transform.position.x + (t.terrainData.size.x / 2f), 0f, t.transform.position.z + (t.terrainData.size.z / 2f)));
#if UNITY_EDITOR
                    Selection.activeObject = t.gameObject;
#endif
                }
                else
                {
                    Debug.LogWarning("Could not find a terrain with the tile coordinates " + tileX.ToString() + "-" + tileZ.ToString() + " in the scene.");
                }
            }


        }
        /// <summary>
        /// Updates the caching mechanism with the new settings - effectively this does only unload cached terrains which are not allowed to be cached anymore due to the new settings.
        /// </summary>
        public void UpdateCaching()
        {
            long currentTimeStamp = GaiaUtils.GetUnixTimestamp();
            foreach (TerrainScene terrainScene in m_terrainSceneStorage.m_terrainScenes)
            {
                if (terrainScene.m_impostorLoadState == LoadState.Cached && (!CachingAllowed() || terrainScene.m_impostorCachedTimestamp + m_cacheKeepAliveTime < currentTimeStamp))
                {
                    terrainScene.RemoveAllImpostorReferences(true);
                }
                if (terrainScene.m_regularLoadState == LoadState.Cached && (!CachingAllowed() || terrainScene.m_regularCachedTimestamp + m_cacheKeepAliveTime < currentTimeStamp))
                {
                    terrainScene.RemoveAllReferences(true);
                }
            }
        }

        /// <summary>
        /// Creates a small loading bounds at a vector3 position in world space to load the terrains located there.
        /// </summary>
        /// <param name="position">Position to load the terrains at.</param>
        /// <param name="range">The range to load around that point.</param>
        /// <param name="referenceGO">The game object referencing that point in the world.</param>
        public void LoadTerrainByPosition(Vector3 position, float range = 1, GameObject referenceGO = null)
        {
            BoundsDouble loadingBounds = new BoundsDouble(position, new Vector3Double(range, range, range));
            if (referenceGO == null)
            {
                referenceGO = this.gameObject;
            }
            UpdateTerrainLoadState(loadingBounds, loadingBounds, referenceGO);
        }

        /// <summary>
        /// /// Load and unload the terrain scenes stored in the current session for a certain object - based on a frustum check on the passed in camera. Any terrain / impostor will receive a reference and will be loaded in as result
        /// </summary>
        public void UpdateTerrainLoadStateFrustum(Camera frustumCam, GameObject requestingObject = null, float minDistance = 0, float maxDistance = 0, float minThresholdMS = 0, float maxThresholdMS = 0)
        { 
        
        }


        /// <summary>
        /// Converts world space X to the terrain X coordinate
        /// </summary>
        /// <param name="worldSpaceX"></param>
        /// <returns></returns>
        public int GetTerrainX(double worldSpaceX)
        {
            if (m_assumeGridLayout)
            {
                return Mathd.FloorToInt((worldSpaceX - m_terrainSceneStorage.m_pos00X) / (double)m_terrainSceneStorage.m_terrainTilesSize);
            }
            else
            {
                Debug.LogError("Trying to calculate a terrain x coordinate while not in a grid layout - this is not possible");
                return int.MinValue;
            }
        }

        /// <summary>
        /// Converts world space Z to the terrain Z coordinate
        /// </summary>
        /// <param name="worldSpaceZ"></param>
        /// <returns></returns>
        public int GetTerrainZ(double worldSpaceZ)
        {
            if (m_assumeGridLayout)
            {
                return Mathd.FloorToInt((worldSpaceZ - m_terrainSceneStorage.m_pos00Z) / (double)m_terrainSceneStorage.m_terrainTilesSize);
            }
            else
            {
                Debug.LogError("Trying to calculate a terrain z coordinate while not in a grid layout - this is not possible");
                return int.MinValue;
            }
        }

        /// <summary>
        /// Gets the Terrain Bounds for world space x/z coordinates when operating in a grid layout. Will not check if that terrain exists or not, if the terrain does not exists this will
        /// still return a bounds object with the values as if there was a terrain at that location.
        /// </summary>
        /// <param name="worldSpaceX"></param>
        /// <param name="worldSpaceZ"></param>
        /// <returns></returns>
        public BoundsDouble GetTerrainBounds(double worldSpaceX, double worldSpaceZ)
        {
            if (!m_assumeGridLayout)
            {
                Debug.LogError("Trying to calculate terrain bounds while not in a grid layout - this is not possible");
                return null;
            }

            int terrainX = GetTerrainX(worldSpaceX);
            int terrainZ = GetTerrainX(worldSpaceZ);

            float halfWidth = m_terrainSceneStorage.m_terrainTilesSize * 0.5f;

            double centerX = m_terrainSceneStorage.m_pos00X + terrainX * m_terrainSceneStorage.m_terrainTilesSize + halfWidth;
            double centerZ = m_terrainSceneStorage.m_pos00Z + terrainZ * m_terrainSceneStorage.m_terrainTilesSize + halfWidth;

            return new BoundsDouble(new Vector3Double(centerX, 0, centerZ), new Vector3Double(m_terrainSceneStorage.m_terrainTilesSize, m_terrainSceneStorage.m_terrainTilesSize, m_terrainSceneStorage.m_terrainTilesSize));
        }

        /// <summary>
        /// Caluclates an "next update" bounds object for a given loading bounds object when operating in a grid layout. This will figure out how far the loading bounds
        /// could move in world space before a new load / unload event would happen, and return a new bounds object based on that. If the center of the original loading bounds moves outside of the "update bounds",
        /// one would need to evaluate loading again then.
        /// </summary>
        /// <param name="loadingBounds"></param>
        /// <returns></returns>
        public BoundsDouble GetNextUpdateBounds(BoundsDouble loadingBounds)
        {
            if (!m_assumeGridLayout)
            {
                Debug.LogError("Trying to calculate next update bounds while not in a grid layout - this is not possible");
                return null;
            }

            BoundsDouble currentbounds = TerrainLoaderManager.Instance.GetTerrainBounds(loadingBounds.center.x, loadingBounds.center.z);
            BoundsDouble negativeXbounds = TerrainLoaderManager.Instance.GetTerrainBounds(loadingBounds.min.x, loadingBounds.center.z);
            BoundsDouble positiveXbounds = TerrainLoaderManager.Instance.GetTerrainBounds(loadingBounds.max.x, loadingBounds.center.z);
            BoundsDouble negativeZbounds = TerrainLoaderManager.Instance.GetTerrainBounds(loadingBounds.center.x, loadingBounds.min.z);
            BoundsDouble positiveZbounds = TerrainLoaderManager.Instance.GetTerrainBounds(loadingBounds.center.x, loadingBounds.max.z);

            //calculate the distance we can travel into negative x direction until a load / unload happens
            double negativeXdistance = Mathd.Min(loadingBounds.min.x - negativeXbounds.min.x, loadingBounds.max.x - positiveXbounds.min.x);

            //calculate the distance we can travel into positive x direction until a load / unload happens
            double positiveXDistance = Mathd.Min(positiveXbounds.max.x - loadingBounds.max.x, negativeXbounds.max.x - loadingBounds.min.x);

            //calculate the distance we can travel into negative z direction until a load / unload happens
            double negativeZdistance = Mathd.Min(loadingBounds.min.z - negativeZbounds.min.z, loadingBounds.max.z - positiveZbounds.min.z);

            //calculate the distance we can travel into positive z direction until a load / unload happens
            double positiveZDistance = Mathd.Min(positiveZbounds.max.z - loadingBounds.max.z, negativeZbounds.max.z - loadingBounds.min.z);

            double updateCenterX = loadingBounds.center.x - negativeXdistance * 0.5f + positiveXDistance * 0.5f;
            double updateCenterZ = loadingBounds.center.z - negativeZdistance * 0.5f + positiveZDistance * 0.5f;

            return new BoundsDouble(new Vector3Double(updateCenterX, 0, updateCenterZ), new Vector3Double(negativeXdistance + positiveXDistance, double.MaxValue, negativeZdistance + positiveZDistance));
        }


        /// <summary>
        /// Load and unload the terrain scenes stored in the current session for a certain object - based on a bounds check around the object, any terrains/impostors within the bounds will receive a reference and will be loaded in as result.
        /// </summary>
        public void UpdateTerrainLoadState(BoundsDouble loadingBoundsRegular = null, BoundsDouble loadingBoundsImpostor = null, GameObject requestingObject = null, float minDistance = 0, float maxDistance = 0, float minThresholdMS = 0, float maxThresholdMS = 0, Camera frustumCamera = null)
        {
            //Do not accept changes to load state during runtime when there was no runtime init yet
            if (Application.isPlaying && !m_runtimeInitialized)
            {
                return;
            }

            //Do not perform any updates when terrain loading is disabled per default
#if GAIA_2023_PRO
            if (TerrainLoaderManager.Instance.m_terrainSceneStorage == null || !TerrainLoaderManager.Instance.m_terrainSceneStorage.m_terrainLoadingEnabled)
            {
                return;
            }
#endif
            CheckAssemblyReloadThreshold();
            //Do not accept any changes to load state while assemblies are being reloaded, this leads to errors in the editor.
            if (m_assembliesAreReloading)
            {
                return;
            }

            if (requestingObject == null)
            {
                requestingObject = gameObject;
            }

            long currentTimeStamp = GaiaUtils.GetUnixTimestamp();

            List<TerrainScene> regularTerrainScenesToLoad = null;
            List<TerrainScene> impostorTerrainScenesToLoad = null;

            //if we do not have any impostors whatsoever we can cut out all calculations for that
            if (!GaiaUtils.HasImpostorTerrains())
            {
                loadingBoundsImpostor = null;
            }

            //no bounds, or just a bounds of size 0? nothing to do, we can just create an empty list
            if (loadingBoundsRegular == null || loadingBoundsRegular.extents.magnitude == 0)
            {
                regularTerrainScenesToLoad = new List<TerrainScene>();
            }
            if (loadingBoundsImpostor == null || loadingBoundsImpostor.extents.magnitude == 0)
            {
                impostorTerrainScenesToLoad = new List<TerrainScene>();
            }


            //if we can assume a that terrains are in a grid layout, we can simplify the checks for evaluating which scenes need to be loaded. We can take the coordinates that
            //would be reasonably in reach by the loading bounds, this is faster than actually checking the bounds against each terrain scene
            if (m_assumeGridLayout)
            {
                if (loadingBoundsRegular != null || loadingBoundsImpostor != null)
                {
                    double centerX = loadingBoundsRegular != null ? loadingBoundsRegular.center.x : loadingBoundsImpostor.center.x;
                    double centerZ = loadingBoundsRegular != null ? loadingBoundsRegular.center.z : loadingBoundsImpostor.center.z;

                    int currentTileX = GetTerrainX(centerX);
                    int currentTileZ = GetTerrainZ(centerZ); 

                    if (regularTerrainScenesToLoad == null && loadingBoundsRegular != null && loadingBoundsRegular.extents.magnitude > 0)
                    {
                        regularTerrainScenesToLoad = new List<TerrainScene>();

                        //get the center coordinates for the tile we are in
                        double currentTileCenterPosX = m_terrainSceneStorage.m_pos00X + currentTileX * m_terrainSceneStorage.m_terrainTilesSize + ((double)m_terrainSceneStorage.m_terrainTilesSize / 2d);
                        double currentTileCenterPosZ = m_terrainSceneStorage.m_pos00Z + currentTileZ * m_terrainSceneStorage.m_terrainTilesSize + ((double)m_terrainSceneStorage.m_terrainTilesSize / 2d);

                        //get the loading range in all 4 directions in tile ranges. We need to take the position of the loading bounds within the current tile into account
                        int negativeXRange = Mathd.RoundToInt((loadingBoundsRegular.extents.x + (currentTileCenterPosX - loadingBoundsRegular.center.x)) / (double)m_terrainSceneStorage.m_terrainTilesSize);
                        int positiveXRange = Mathd.RoundToInt((loadingBoundsRegular.extents.x + (loadingBoundsRegular.center.x - currentTileCenterPosX)) / (double)m_terrainSceneStorage.m_terrainTilesSize);
                        int negativeZRange = Mathd.RoundToInt((loadingBoundsRegular.extents.z + (currentTileCenterPosZ - loadingBoundsRegular.center.z)) / (double)m_terrainSceneStorage.m_terrainTilesSize);
                        int positiveZRange = Mathd.RoundToInt((loadingBoundsRegular.extents.z + (loadingBoundsRegular.center.z - currentTileCenterPosZ)) / (double)m_terrainSceneStorage.m_terrainTilesSize);

                        //calculate the minimum and maximum tile coordinates
                        int minXcoord = currentTileX - negativeXRange;
                        int maxXcoord = currentTileX + positiveXRange;
                        int minZcoord = currentTileZ - negativeZRange;
                        int maxZcoord = currentTileZ + positiveZRange;

                        //get the terrain scenes to be loaded based on the tile coordinates
                        regularTerrainScenesToLoad = m_terrainSceneStorage.m_terrainScenes.Where(t => t.m_xCoord >= minXcoord && t.m_xCoord <= maxXcoord && t.m_zCoord >= minZcoord && t.m_zCoord <= maxZcoord).ToList();
                    }
                    //no cached list? need to assemble it and cache it
                    if (impostorTerrainScenesToLoad == null && loadingBoundsImpostor != null && loadingBoundsImpostor.extents.magnitude > 0)
                    {
                        impostorTerrainScenesToLoad = new List<TerrainScene>();

                        //get the center coordinates for the tile we are in
                        double currentTileCenterPosX = m_terrainSceneStorage.m_pos00X + currentTileX * m_terrainSceneStorage.m_terrainTilesSize + ((double)m_terrainSceneStorage.m_terrainTilesSize / 2d);
                        double currentTileCenterPosZ = m_terrainSceneStorage.m_pos00Z + currentTileZ * m_terrainSceneStorage.m_terrainTilesSize + ((double)m_terrainSceneStorage.m_terrainTilesSize / 2d);

                        //get the loading range in all 4 directions in tile ranges. We need to take the position of the loading bounds within the current tile into account
                        int negativeXRange = Mathd.RoundToInt((loadingBoundsImpostor.extents.x + (currentTileCenterPosX - loadingBoundsRegular.center.x)) / (double)m_terrainSceneStorage.m_terrainTilesSize);
                        int positiveXRange = Mathd.RoundToInt((loadingBoundsImpostor.extents.x + (loadingBoundsImpostor.center.x - currentTileCenterPosX)) / (double)m_terrainSceneStorage.m_terrainTilesSize);
                        int negativeZRange = Mathd.RoundToInt((loadingBoundsImpostor.extents.z + (currentTileCenterPosZ - loadingBoundsRegular.center.z)) / (double)m_terrainSceneStorage.m_terrainTilesSize);
                        int positiveZRange = Mathd.RoundToInt((loadingBoundsImpostor.extents.z + (loadingBoundsImpostor.center.z - currentTileCenterPosZ)) / (double)m_terrainSceneStorage.m_terrainTilesSize);

                        //calculate the minimum and maximum tile coordinates
                        int minXcoord = currentTileX - negativeXRange;
                        int maxXcoord = currentTileX + positiveXRange;
                        int minZcoord = currentTileZ - negativeZRange;
                        int maxZcoord = currentTileZ + positiveZRange;

                        //get the terrain scenes to be loaded based on the tile coordinates
                        impostorTerrainScenesToLoad = m_terrainSceneStorage.m_terrainScenes.Where(t => t.m_xCoord >= minXcoord && t.m_xCoord <= maxXcoord && t.m_zCoord >= minZcoord && t.m_zCoord <= maxZcoord).ToList();
                    }
                }
            }
            else
            {
                impostorTerrainScenesToLoad = new List<TerrainScene>();
                regularTerrainScenesToLoad = new List<TerrainScene>();

                foreach (TerrainScene terrainScene in TerrainLoaderManager.TerrainScenes)
                {
                    if (terrainScene.m_nextUpdateTimestamp > currentTimeStamp)
                    {
                        continue;
                    }
                    float distance = Vector3.Distance(terrainScene.m_bounds.center - GetOrigin(), requestingObject.transform.position);
                    //only evaluate load state if local terrain is supposed to be displayed
                    if (m_showLocalTerrain)
                    {
                        if (loadingBoundsImpostor != null && loadingBoundsImpostor.extents.magnitude > 0 && loadingBoundsImpostor.extents.magnitude > loadingBoundsRegular.extents.magnitude)
                        {
                            if (terrainScene.m_bounds.Intersects(loadingBoundsImpostor) && !TerrainSceneStorage.m_colliderOnlyLoading && !terrainScene.HasImpostorReference(requestingObject))
                            {
                                impostorTerrainScenesToLoad.Add(terrainScene);
                            }
                        }
                        if (loadingBoundsRegular != null && loadingBoundsRegular.extents.magnitude > 0)
                        {
                            if (terrainScene.m_bounds.Intersects(loadingBoundsRegular) && !terrainScene.HasImpostorReference(requestingObject))
                            {
                                regularTerrainScenesToLoad.Add(terrainScene);
                            }
                        }
                    }
                }
            }

            //Remove the reference from all impostor terrains that should not be loaded anymore
            foreach (TerrainScene terrainScene in m_terrainSceneStorage.m_terrainScenes.Where(x => (x.HasImpostorReference(requestingObject) || x.m_impostorLoadState == LoadState.Cached) && !impostorTerrainScenesToLoad.Contains(x)))
            {
                int manualImpostorIndex = m_terrainSceneStorage.m_terrainScenes.IndexOf(terrainScene);
                if (manualImpostorIndex >= 0 && m_manualLoadedImpostorIndices.Contains(manualImpostorIndex) && requestingObject == gameObject)
                {
                    continue;
                }
                float distance;
                if (frustumCamera == null)
                {
                    distance = Vector3.Distance(terrainScene.m_bounds.center - GetOrigin(), requestingObject.transform.position);
                }
                else
                {
                    distance = Vector3.Distance(terrainScene.m_bounds.center - GetOrigin(), frustumCamera.transform.position);
                }
                AddToTerrainSceneActionQueue(terrainScene, ReferenceChange.RemoveImpostorReference, requestingObject, distance, false, frustumCamera);
            }

            //Add a reference to all the impostor terrains that need to be loaded
            if (impostorTerrainScenesToLoad != null)
            {
                for (int i = 0; i < impostorTerrainScenesToLoad.Count; i++)
                {
                    TerrainScene terrainScene = impostorTerrainScenesToLoad[i];
                    float distance;
                    if (frustumCamera == null)
                    {
                        distance = Vector3.Distance(terrainScene.m_bounds.center - GetOrigin(), requestingObject.transform.position);
                    }
                    else
                    {
                        distance = Vector3.Distance(terrainScene.m_bounds.center - GetOrigin(), frustumCamera.transform.position);
                    }
                    AddToTerrainSceneActionQueue(terrainScene, ReferenceChange.AddImpostorReference, requestingObject, distance, false, frustumCamera);
                }
            }



            //Remove the reference from all regular terrains that should not be loaded anymore
            foreach (TerrainScene terrainScene in m_terrainSceneStorage.m_terrainScenes.Where(x => (x.HasRegularReference(requestingObject) || x.m_regularLoadState == LoadState.Cached) && !regularTerrainScenesToLoad.Contains(x)))
            {
                int manualRegularIndex = m_terrainSceneStorage.m_terrainScenes.IndexOf(terrainScene);
                if (manualRegularIndex >= 0 && m_manualLoadedRegularIndices.Contains(manualRegularIndex) && requestingObject == gameObject)
                {
                    continue;
                }
                float distance;
                if (frustumCamera == null)
                {
                    distance = Vector3.Distance(terrainScene.m_bounds.center - GetOrigin(), requestingObject.transform.position);
                }
                else
                {
                    distance = Vector3.Distance(terrainScene.m_bounds.center - GetOrigin(), frustumCamera.transform.position);
                }
                AddToTerrainSceneActionQueue(terrainScene, ReferenceChange.RemoveRegularReference, requestingObject, distance, false, frustumCamera);
            }
            if (regularTerrainScenesToLoad != null)
            {
                //Add a reference to all the regular terrains that need to be loaded
                for (int i = 0; i < regularTerrainScenesToLoad.Count; i++)
                {
                    TerrainScene terrainScene = regularTerrainScenesToLoad[i];
                    float distance;
                    if (frustumCamera == null)
                    {
                        distance = Vector3.Distance(terrainScene.m_bounds.center - GetOrigin(), requestingObject.transform.position);
                    }
                    else
                    {
                        distance = Vector3.Distance(terrainScene.m_bounds.center - GetOrigin(), frustumCamera.transform.position);
                    }
                    AddToTerrainSceneActionQueue(terrainScene, ReferenceChange.AddRegularReference, requestingObject, distance, false, frustumCamera);
                }
            }

            //If we are not in play mode, we can process the entire queue right away. During runtime the queue will be processed in the update() function, with delays between load operations
#if UNITY_EDITOR
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                for (int i = 0; i < m_terrainSceneActionQueue.Count; i++)
                {
                    ProcessActionQueueEntry(m_terrainSceneActionQueue[i]);
                }
                m_terrainSceneActionQueue.Clear();
            }
#endif

            //Debug helper for reviewing the current queue
            //string message = "Current Queue Entries; \r\n";
            //for (int i = 0; i < m_terrainSceneActionQueue.Count; i++)
            //{
            //    message += $"{m_terrainSceneActionQueue[i].m_terrainScene.GetTerrainName()} - {m_terrainSceneActionQueue[i].m_referenceChange.ToString()} - {m_terrainSceneActionQueue[i].m_inFrustum} - {m_terrainSceneActionQueue[i].m_distance} \r\n";
            //}
            //Debug.Log(message);


        }

        private void ProcessActionQueueEntry(TerrainSceneActionQueueEntry terrainSceneActionQueueEntry)
        {
            switch (terrainSceneActionQueueEntry.m_referenceChange)
            {
                case ReferenceChange.AddImpostorReference:
                    terrainSceneActionQueueEntry.m_terrainScene.AddImpostorReference(terrainSceneActionQueueEntry.referencingObject);
                    break;
                case ReferenceChange.AddRegularReference:
                    terrainSceneActionQueueEntry.m_terrainScene.AddRegularReference(terrainSceneActionQueueEntry.referencingObject);
                    break;
                case ReferenceChange.RemoveImpostorReference:
                    terrainSceneActionQueueEntry.m_terrainScene.RemoveImpostorReference(terrainSceneActionQueueEntry.referencingObject, m_cacheMemoryThreshold, terrainSceneActionQueueEntry.m_forced);
                    break;
                case ReferenceChange.RemoveRegularReference:
                    terrainSceneActionQueueEntry.m_terrainScene.RemoveRegularReference(terrainSceneActionQueueEntry.referencingObject, m_cacheMemoryThreshold, terrainSceneActionQueueEntry.m_forced);
                    break;
            }
        }

        private void AddToTerrainSceneActionQueue(TerrainScene terrainScene, ReferenceChange referenceChange, GameObject referencingObject, float distance, bool forced = false, Camera frustumCamera = null) 
        {
            //Do not allow duplicate reference changes in the queue
            if (m_terrainSceneActionQueue.Exists(x => x.m_terrainScene == terrainScene && x.referencingObject == referencingObject && x.m_referenceChange == referenceChange))
            {
                return;
            }

            //If an incoming reference change would cancel an existing entry out, remove both
            ReferenceChange oppositeAction = ReferenceChange.RemoveRegularReference;
            switch (referenceChange)
            {
                case ReferenceChange.AddImpostorReference:
                    oppositeAction = ReferenceChange.RemoveImpostorReference;
                    break;
                case ReferenceChange.AddRegularReference:
                    oppositeAction = ReferenceChange.RemoveRegularReference;
                    break;
                case ReferenceChange.RemoveImpostorReference:
                    oppositeAction = ReferenceChange.AddImpostorReference;
                    break;
                case ReferenceChange.RemoveRegularReference:
                    oppositeAction = ReferenceChange.AddRegularReference;
                    break;
            }
            int index = m_terrainSceneActionQueue.FindIndex(x => x.m_terrainScene == terrainScene && x.referencingObject == referencingObject && x.m_referenceChange == oppositeAction);
            if (index != -1)
            {
                m_terrainSceneActionQueue.RemoveAt(index);
            }


            //Figure out the index / priority at which this action needs to be in the queue

            //If a frustum Camera is passed in, check if the tile in question is within the camera frustum
            //we want to perform actions in the frustum first 
            bool inFrustum = false;
            if (frustumCamera != null)
            {
                GeometryUtility.CalculateFrustumPlanes(frustumCamera, m_planes);
                Bounds boundsWithOffset = new Bounds(terrainScene.m_bounds.center + GetOrigin(), terrainScene.m_bounds.size);
                if (GeometryUtility.TestPlanesAABB(m_planes, boundsWithOffset))
                {
                    inFrustum = true;
                    //Debug.DrawLine(boundsWithOffset.min,boundsWithOffset.max, UnityEngine.Color.red, 5f);
                }
            }

            index = m_terrainSceneActionQueue.FindIndex(x => x.m_distance > distance && x.m_inFrustum == inFrustum);

            if (index != -1)
            {
                //We found an entry with a larger distance and same frustum state -> insert it in before that
                m_terrainSceneActionQueue.Insert(index, new TerrainSceneActionQueueEntry { m_terrainScene = terrainScene, m_referenceChange = referenceChange, referencingObject = referencingObject, m_forced = forced, m_distance = distance, m_inFrustum = inFrustum });
            }
            else
            {
                //no larger distance / frustum state found
                if (inFrustum)
                {
                    //do other "in frustum" entries exist? If yes, we would need to place them after those as the largest new entry
                    index = m_terrainSceneActionQueue.FindLastIndex(x => x.m_inFrustum == inFrustum);
                    if (index != -1)
                    {
                        //We found an entry with the same frustum state => needs to go behind that (since we have the largest distance in this case)
                        m_terrainSceneActionQueue.Insert(index + 1, new TerrainSceneActionQueueEntry { m_terrainScene = terrainScene, m_referenceChange = referenceChange, referencingObject = referencingObject, m_forced = forced, m_distance = distance, m_inFrustum = inFrustum });
                    }
                    else
                    {
                        //no other entries with the same frustum state exist, is this in frustum? 
                        if (inFrustum)
                        {
                            //then it needs to go to the start of the queue, since it would be the first entry in the queue that is within frustum
                            m_terrainSceneActionQueue.Insert(0, new TerrainSceneActionQueueEntry { m_terrainScene = terrainScene, m_referenceChange = referenceChange, referencingObject = referencingObject, m_forced = forced, m_distance = distance, m_inFrustum = inFrustum });
                        }
                        else
                        {
                            //then it needs to go to the end of the queue, since it would be the first entry that is not within the frustum
                            m_terrainSceneActionQueue.Add(new TerrainSceneActionQueueEntry { m_terrainScene = terrainScene, m_referenceChange = referenceChange, referencingObject = referencingObject, m_forced = forced, m_distance = distance, m_inFrustum = inFrustum });
                        }
                    }

                }
                else
                {
                    //this needs to go to the end of the queue
                    m_terrainSceneActionQueue.Add(new TerrainSceneActionQueueEntry { m_terrainScene = terrainScene, m_referenceChange = referenceChange, referencingObject = referencingObject, m_forced = forced, m_distance = distance, m_inFrustum = inFrustum });
                }
            }

        }

        /// <summary>
        /// Tries to get the neighboring terrain scene for a scene in the given direction
        /// </summary>
        /// <param name="terrainScene">The terrain scene we are looking up the neighbors for</param>
        /// <param name="direction">The cardinal direction to look in</param>
        /// <returns>The neighboring terrain scene if found, returns null otherwise</returns>
        public TerrainScene TryGetNeighbor(TerrainScene terrainScene, StitchDirection direction)
        {
            if (terrainScene == null)
            {
                //Can't find a neighbor without an input scene
                return null;
            }

            int coordX = terrainScene.m_xCoord;
            int coordZ = terrainScene.m_zCoord;
            
            TerrainScene returnScene = null;
            switch (direction)
            {
                case StitchDirection.North:
                    coordZ += 1;
                    break;
                case StitchDirection.South:
                    coordZ -= 1;
                    break;
                case StitchDirection.West:
                    coordX -= 1;
                    break;
                case StitchDirection.East:
                    coordX += 1;
                    break;
            }

            returnScene = TerrainSceneStorage.m_terrainScenes.Find(x => x.m_xCoord == coordX && x.m_zCoord == coordZ);
            return returnScene;
        }

        public bool CachingAllowed()
        {
            return (Application.isPlaying && m_cacheInRuntime) || (!Application.isPlaying && m_cacheInEditor);
        }

        public double GetLoadingRange()
        {
            return m_sceneViewOriginLoadingBounds.extents.x;
        }

        public double GetImpostorLoadingRange()
        {
            return m_sceneViewImpostorLoadingBounds.extents.x;
        }

        public Vector3Double GetLoadingSize()
        {
            return new Vector3Double(m_sceneViewOriginLoadingBounds.size);
        }

        public Vector3Double GetImpostorLoadingSize()
        {
            return new Vector3Double(m_sceneViewImpostorLoadingBounds.size);
        }


        public Vector3Double GetLoadingCenter()
        {
            if (m_centerSceneViewLoadingOn == CenterSceneViewLoadingOn.SceneViewCamera)
            {
                return m_sceneViewCameraLoadingBounds.center;
            }
            else
            {
                return m_sceneViewOriginLoadingBounds.center;
            }
        }

        public void SwitchToWorldMap()
        {
            ShowWorldMapTerrain = true;
            ShowLocalTerrain = false;
            TerrainLoaderManager.Instance.SetOrigin(Vector3.zero);
            if (!Application.isPlaying)
            {
                UpdateTerrainLoadState();
            }
#if UNITY_EDITOR
            Selection.activeGameObject = GaiaUtils.GetOrCreateWorldDesigner();
#endif

        }

        public void SwitchToLocalMap(bool useInternalLoadingBounds = false)
        {
            ShowLocalTerrain = true;
            ShowWorldMapTerrain = false;
            if (!Application.isPlaying)
            {
                if (useInternalLoadingBounds)
                {
                    UpdateTerrainLoadState(m_sceneViewOriginLoadingBounds);
                }
                else
                {
                    UpdateTerrainLoadState();
                }
            }
        }


        public void SetLoadingRange(Double regularLoadingRange, Double impostorLoadingRange, bool updateLoading = true, bool forceUpdate = false)
        {
#if UNITY_EDITOR
            if (regularLoadingRange > 0 || impostorLoadingRange > 0)
            {
                Vector3Double targetLoadingExtentsRegular = new Vector3Double(regularLoadingRange, regularLoadingRange, regularLoadingRange);
                Vector3Double targetLoadingExtentsImpostor = new Vector3Double(impostorLoadingRange, impostorLoadingRange, impostorLoadingRange);

                BoundsDouble loadingBounds = m_sceneViewOriginLoadingBounds;
                if (m_centerSceneViewLoadingOn == CenterSceneViewLoadingOn.SceneViewCamera)
                {

                    var allCameras = SceneView.GetAllSceneCameras();
                    if (allCameras.Length > 0)
                    {
                        if (!forceUpdate && m_sceneViewCameraLoadingBounds.center.Equals((Vector3Double)allCameras[0].transform.position) && m_sceneViewCameraLoadingBounds.extents.Equals(targetLoadingExtentsRegular) && m_sceneViewImpostorLoadingBounds.extents.Equals(targetLoadingExtentsImpostor))
                        {
                            //this exact setup is what is already currently loaded, skip the rest of processing
                            return;
                        }

                        m_sceneViewCameraLoadingBounds.center = (Vector3Double)allCameras[0].transform.position;
                    }
                    loadingBounds = m_sceneViewCameraLoadingBounds;
                }
                loadingBounds.extents = targetLoadingExtentsRegular;
                m_sceneViewOriginLoadingBounds.extents = loadingBounds.extents;
                m_sceneViewImpostorLoadingBounds.center = loadingBounds.center;
                m_sceneViewImpostorLoadingBounds.extents = targetLoadingExtentsImpostor;

                EditorUtility.SetDirty(this);
                if (regularLoadingRange > 0)
                {
                    m_lastNonZeroLoadingRange = regularLoadingRange;
                }
                if (updateLoading)
                {
                    UpdateTerrainLoadState(loadingBounds, m_sceneViewImpostorLoadingBounds, gameObject);
                }
            }
            else
            {
                m_sceneViewOriginLoadingBounds.extents = Vector3.zero;
                m_sceneViewImpostorLoadingBounds.extents = Vector3.zero;
                foreach (TerrainScene ts in m_terrainSceneStorage.m_terrainScenes)
                {
                    ts.RemoveRegularReference(gameObject);
                    ts.RemoveImpostorReference(gameObject);
                }
            }
#endif
        }

        public void RefreshSceneViewLoadingRange()
        {
            // Scene-view preview is editor-only. Play mode follows the player.
            if (Application.isPlaying)
            {
                return;
            }

#if UNITY_EDITOR
            if (EditorKeepFourTerrainsAndImpostors())
            {
                return;
            }
#endif

            if (m_centerSceneViewLoadingOn == CenterSceneViewLoadingOn.WorldOrigin)
            {
                UpdateTerrainLoadState(m_sceneViewOriginLoadingBounds, m_sceneViewImpostorLoadingBounds);
            }
            else
            {
                UpdateTerrainLoadState(m_sceneViewCameraLoadingBounds, m_sceneViewImpostorLoadingBounds);
            }
        }

        /// <summary>
        /// Editor: keep the 4 nearest regular tiles around Player_v7 and leave impostors loaded.
        /// Play/build still streams from the live player position (including after a save load).
        /// </summary>
        public bool EditorKeepFourTerrainsAndImpostors()
        {
            if (Application.isPlaying || m_terrainSceneStorage == null || m_terrainSceneStorage.m_terrainScenes == null)
            {
                return false;
            }

            GameObject player = GameObject.Find("Player_v7");
            if (player == null)
            {
                return false;
            }

            Vector3 pos = player.transform.position;
            List<TerrainScene> ordered = m_terrainSceneStorage.m_terrainScenes
                .Where(ts => ts != null)
                .OrderBy(ts =>
                {
                    float dx = pos.x - (float)ts.m_bounds.center.x;
                    float dz = pos.z - (float)ts.m_bounds.center.z;
                    return dx * dx + dz * dz;
                })
                .ToList();

            HashSet<TerrainScene> keepRegular = new HashSet<TerrainScene>();
            for (int i = 0; i < ordered.Count && keepRegular.Count < 4; i++)
            {
                keepRegular.Add(ordered[i]);
            }

            for (int i = 0; i < m_terrainSceneStorage.m_terrainScenes.Count; i++)
            {
                TerrainScene ts = m_terrainSceneStorage.m_terrainScenes[i];
                if (ts == null)
                {
                    continue;
                }

                if (keepRegular.Contains(ts))
                {
                    ts.AddRegularReference(player);
                }
                else
                {
                    ts.RemoveAllRegularReferences(true);
                }

                if (!string.IsNullOrEmpty(ts.m_impostorScenePath))
                {
                    ts.AddImpostorReference(player);
                }
            }

            return true;
        }

        public void RefreshTerrainsWithCurrentData()
        {
            foreach (TerrainScene ts in TerrainSceneStorage.m_terrainScenes)
            {
                ts.UpdateWithCurrentData();
            }
        }

        /// <summary>
        /// Removes all references to terrains or impostors originating by the given game object - this will allow the affected terrains to unload (assuming the GameObject was the only reference)
        /// </summary>
        /// <param name="go">The GameObject whose references will be removed</param>
        public void RemoveAllReferencesOfGameObject(GameObject go)
        {
            if (m_terrainSceneStorage != null)
            {
                foreach (TerrainScene terrainScene in m_terrainSceneStorage.m_terrainScenes.Where(x => x.RegularReferences.Contains(go) || x.ImpostorReferences.Contains(go)))
                {
                    terrainScene.RemoveRegularReference(go);
                    terrainScene.RemoveImpostorReference(go);
                    terrainScene.m_nextUpdateTimestamp = 0;
                }
            }

        }

        public void UnloadAll(bool forceUnload = false)
        {
            if (m_terrainSceneStorage != null)
            {
                foreach (TerrainScene terrainScene in m_terrainSceneStorage.m_terrainScenes)
                {
                    terrainScene.RemoveAllReferences(forceUnload);
                    terrainScene.m_nextUpdateTimestamp = 0;
                }
            }
        }

        public void UnloadAllImpostors(bool forceUnload = false)
        {
            if (m_terrainSceneStorage != null)
            {
                foreach (TerrainScene terrainScene in m_terrainSceneStorage.m_terrainScenes)
                {
                    terrainScene.RemoveAllImpostorReferences(forceUnload);
                    terrainScene.m_nextUpdateTimestamp = 0;
                }
            }
        }

        public void EmptyCache()
        {
            if (m_terrainSceneStorage != null)
            {
                foreach (TerrainScene terrainScene in m_terrainSceneStorage.m_terrainScenes.Where(x => x.RegularReferences.Count() <= 0))
                {
                    terrainScene.RemoveAllReferences(true);
                }
            }
        }


        public void DirtyStorageData()
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(m_terrainSceneStorage);
#endif
        }

        public void SaveStorageData()
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(m_terrainSceneStorage);
            AssetDatabase.SaveAssets();
            LoadStorageData();
#endif
        }

        /// <summary>
        /// //Update impostor state in the addressable / build scriptable object, if the scene of the terrain loader manager is found in the current configuration.
        /// </summary>
        public void UpdateImpostorStateInBuildSettings()
        {
#if UNITY_EDITOR
            BuildConfig config = GaiaUtils.GetOrCreateBuildConfig();
            SceneBuildEntry sceneBuildEntry = config.m_sceneBuildEntries.Find(x => x.m_masterScene.name == this.gameObject.scene.name);
            if (sceneBuildEntry != null)
            {
                if (TerrainSceneStorage.m_terrainScenes.Count > 0)
                {
                    if (TerrainSceneStorage.m_terrainScenes.Exists(x => !string.IsNullOrEmpty(x.m_impostorScenePath)))
                    {
                        sceneBuildEntry.m_impostorState = ImpostorState.ImpostorsCreated;
                    }
                    else
                    {
                        sceneBuildEntry.m_impostorState = ImpostorState.ImpostorsNotCreated;
                    }
                }
                else
                {
                    sceneBuildEntry.m_impostorState = ImpostorState.NoTerrainLoading;
                }
            }
            EditorUtility.SetDirty(config);
#endif
        }

        /// <summary>
        /// Gets a terrain scene from the terrain scene storage by the x-z tile index
        /// </summary>
        /// <param name="x"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public TerrainScene GetTerrainSceneByIndices(int x, int z)
        {
            return m_terrainSceneStorage.m_terrainScenes.Find(k => k.m_scenePath.Contains($"_{x}_{z}-"));
        }

        public TerrainScene GetTerrainSceneAtPosition(Vector3Double center)
        {
            return m_terrainSceneStorage.m_terrainScenes.Find(x => x.m_bounds.Contains(center));
        }

        /// <summary>
        /// Returns the default loading range according to the tile size of a single terrain tile of the scene.
        /// </summary>
        /// <param name="tileSize">The tile size to get the loading range for.</param>
        /// <returns>The default loading range.</returns>
        public static double GetDefaultLoadingRangeForTilesize(int tileSize)
        {
            return Mathf.Round((tileSize * 1.9f) / 2f);
        }

        public static void FloatingPointFix_Add()
        {
#if GAIA_2023_PRO
            //Add main fix to player

            GameObject playerObj = GameObject.Find(GaiaConstants.playerFlyCamName);

            if (playerObj == null)
            {
                playerObj = GameObject.Find(GaiaConstants.playerFirstPersonName);
            }

            if (playerObj == null)
            {
                playerObj = GameObject.Find(GaiaConstants.playerThirdPersonName);
            }

            if (playerObj == null)
            {
                playerObj = GameObject.Find(GaiaConstants.m_carPlayerPrefabName);
            }

            if (playerObj == null)
            {
                playerObj = GameObject.Find(GaiaConstants.playerXRName);
            }

            if (playerObj != null)
            {
                FloatingPointFix fix = playerObj.GetComponent<FloatingPointFix>();
                if (fix == null)
                {
                    fix = playerObj.AddComponent<FloatingPointFix>();
                }

                fix.threshold = GaiaUtils.GetGaiaSettings().m_FPFDefaultThreshold;
            }

            //Check if we are in a placeholder / terrain loading setup - if yes switch on all placeholders for the fix
            if (GaiaUtils.HasDynamicLoadedTerrains())
            {
                foreach (TerrainScene ts in TerrainLoaderManager.TerrainScenes)
                {
                    ts.m_useFloatingPointFix = true;
                }

                if (GaiaUtils.DisplayDialogNoEditor("Adjust unloaded Terrains?", "You are using dynamic terrain loading with terrain placeholders. Do you want to load all terrains after another to apply the fix to them and make all objects non-static?", "OK", "Cancel"))
                {
                    GaiaUtils.CallFunctionOnDynamicLoadedTerrains(AddFloatingPointFixToTerrain, true);
                }
            }
            else
            {
                //regular terrain setup - add the membership to all terrains in the scene instead.
                foreach (Terrain terrain in Terrain.activeTerrains)
                {
                    AddFloatingPointFixToTerrain(terrain);
                }
            }
#endif
        }

        public static void AddFloatingPointFixToTerrain(Terrain terrain)
        {
#if GAIA_2023_PRO
            FloatingPointFixMember ffMember = terrain.gameObject.GetComponent<FloatingPointFixMember>();
            if (ffMember == null)
            {
                ffMember = terrain.gameObject.AddComponent<FloatingPointFixMember>();
            }
            SetAllChildsNonStatic(terrain.transform);
#endif
        }

        private static void RemoveFloatingPointFixToTerrain(Terrain terrain)
        {
#if GAIA_2023_PRO
            FloatingPointFixMember ffMember = terrain.gameObject.GetComponent<FloatingPointFixMember>();
            if (ffMember != null)
            {
                Component.DestroyImmediate(ffMember);
            }
#endif
        }


        public static void SetAllChildsNonStatic(Transform transform)
        {
            transform.gameObject.isStatic = false;
            foreach (Transform t in transform)
            {
                SetAllChildsNonStatic(t);
            }
        }

        /// <summary>
        /// Removes floating point fix system from the scene
        /// </summary>
        public static void FloatingPointFix_Remove()
        {
#if GAIA_2023_PRO
            //Remove main fix to player

            GameObject playerObj = GameObject.Find(GaiaConstants.playerFlyCamName);

            if (playerObj == null)
            {
                playerObj = GameObject.Find(GaiaConstants.playerFirstPersonName);
            }

            if (playerObj == null)
            {
                playerObj = GameObject.Find(GaiaConstants.playerThirdPersonName);
            }

            if (playerObj == null)
            {
                playerObj = GameObject.Find(GaiaConstants.m_carPlayerPrefabName);
            }

            if (playerObj == null)
            {
                playerObj = GameObject.Find(GaiaConstants.playerXRName);
            }


            if (playerObj != null)
            {
                FloatingPointFix fix = playerObj.GetComponent<FloatingPointFix>();
                if (fix != null)
                {
                    Component.DestroyImmediate(fix);
                }
            }

            //Remove membership from water

            GameObject waterGO = GameObject.Find(GaiaConstants.waterSurfaceObject);
            if (waterGO != null)
            {
                FloatingPointFixMember ffMember = waterGO.transform.parent.gameObject.GetComponent<FloatingPointFixMember>();
                if (ffMember != null)
                {
                    Component.DestroyImmediate(ffMember);
                }
            }

            //Check if we are in a terrain loading setup - if yes switch off all placeholders for the fix
            if (GaiaUtils.HasDynamicLoadedTerrains())
            {
                foreach (TerrainScene ts in TerrainLoaderManager.TerrainScenes)
                {
                    ts.m_useFloatingPointFix = false;
                }

                if (GaiaUtils.DisplayDialogNoEditor("Adjust unloaded Terrains?", "You are using dynamic terrain loading with terrain placeholders. Do you want to load all terrains one after another to remove the fix from those as well?", "OK", "Cancel"))
                {
                    GaiaUtils.CallFunctionOnDynamicLoadedTerrains(RemoveFloatingPointFixToTerrain, true);
                }
            }
            else
            {
                //regular terrain setup - add the membership to all terrains in the scene instead.
                foreach (Terrain terrain in Terrain.activeTerrains)
                {
                    RemoveFloatingPointFixToTerrain(terrain);
                }
            }
#endif
        }

        public void AddManualLoadedRegularIndex(int i)
        {
            m_manualLoadedImpostorIndices.Remove(i);
            if (!m_manualLoadedRegularIndices.Contains(i))
            {
                m_manualLoadedRegularIndices.Add(i);
            }
        }

        public void AddManualLoadedImpostorIndex(int i)
        {
            m_manualLoadedRegularIndices.Remove(i);
            if (!m_manualLoadedImpostorIndices.Contains(i))
            {
                m_manualLoadedImpostorIndices.Add(i);
            }
        }

        public void RemoveManualIndices(int i)
        {
            m_manualLoadedImpostorIndices.Remove(i);
            m_manualLoadedRegularIndices.Remove(i);
        }

        public void RemoveAllManualIndices()
        {
            m_manualLoadedImpostorIndices.Clear();
            m_manualLoadedRegularIndices.Clear();
        }
        public void RemoveAllManualImpostorIndices()
        {
            m_manualLoadedImpostorIndices.Clear();
        }
        public void RemoveAllManualRegularIndices()
        {
            m_manualLoadedRegularIndices.Clear();
        }

        public void RemoveManualLoadedRegularIndex(int i)
        {
            m_manualLoadedRegularIndices.Remove(i);
        }

        public void RemoveManualLoadedImpostorIndex(int i)
        {
            m_manualLoadedImpostorIndices.Remove(i);
        }

    }
}
