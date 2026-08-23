using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using PWCommon5;
using Gaia.Internal;
using System;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Profiling;
using UnityEditor.Experimental.GraphView;
using UnityEditor.AssetImporters;
using static UnityEditor.PlayerSettings;

namespace Gaia
{
    [CustomEditor(typeof(TerrainLoaderManager))]
    public class TerrainLoaderManagerEditor : PWEditor, IPWEditor
    {
        private TerrainLoaderManager m_terrainLoaderManager;
        #if GAIA_2023_PRO
        private TerrainLoader[] m_terrainLoaders;
        #endif
        private EditorUtils m_editorUtils;
        private GUIStyle m_terrainBoxStyle;
        private Terrain m_ingestTerrain;
        private static List<Texture2D> m_tempTextureList = new List<Texture2D>();
        private GUIStyle redStyle;
        private GUIStyle greenStyle;

        private GaiaSettings m_gaiaSettings;

        private GaiaSettings GaiaSettings
        {
            get
            {
                if (m_gaiaSettings == null)
                {
                    m_gaiaSettings = GaiaUtils.GetGaiaSettings();
                }
                return m_gaiaSettings;
            }
        }

        private GaiaSessionManager m_gaiaSessionManager;

        private GaiaSessionManager SessionManager
        {
            get
            {
                if (m_gaiaSessionManager == null)
                {
                    m_gaiaSessionManager = GaiaSessionManager.GetSessionManager();
                }
                return m_gaiaSessionManager;
            }
        }


        public void OnEnable()
        {
            m_terrainLoaderManager = (TerrainLoaderManager)target;
            m_terrainLoaderManager.m_assembliesAreReloading = false;
            m_terrainLoaderManager.AssureGridMetaData();
            m_terrainLoaderManager.SubscribeToAssemblyReloadEvents();
            
#if GAIA_2023_PRO
            m_terrainLoaders = Resources.FindObjectsOfTypeAll<TerrainLoader>();
            #endif
            //m_placeHolders = Resources.FindObjectsOfTypeAll<GaiaTerrainPlaceHolder>();
            //m_placeHolders = m_placeHolders.OrderBy(x => x.name).ToArray();
            //foreach (GaiaTerrainPlaceHolder placeHolder in m_placeHolders)
            //{
            //    placeHolder.UpdateLoadState();
            //}

            //Init editor utils
            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }
            if (m_terrainLoaderManager.TerrainSceneStorage == null)
            {
                m_terrainLoaderManager.LoadStorageData();
            }
            m_terrainLoaderManager.LookUpLoadingScreen();

            //Checks for the "Auto-Show" Terrain boxes feature - if all terrains are unloaded, Gaia can automatically draw box gizmos to give an indication where the world is at
            if (!Application.isPlaying)
            {
                if ((m_terrainLoaderManager.m_autoToggleTerrainBoxes && !m_terrainLoaderManager.m_autoToggleTerrainBoxesTriggered))
                {
                    if (m_terrainLoaderManager.m_autoToggleTerrainBoxes)
                    {
                        m_terrainLoaderManager.m_showOriginTerrainBoxes = true;
                        //we remember if we triggered the flag to show the boxes through the auto-feature, in this way we can respect it if the user switches the boxes off manually
                        m_terrainLoaderManager.m_autoToggleTerrainBoxesTriggered = true;
                    }
                }
            }
        }

        private void OnDisable()
        {
            if ((m_terrainLoaderManager.m_autoToggleTerrainBoxes))
            {
                m_terrainLoaderManager.m_showOriginTerrainBoxes = false;
                //we remember if we triggered the flag to show the boxes through the auto-feature, in this way we can respect it if the user switches the boxes off manually
                m_terrainLoaderManager.m_autoToggleTerrainBoxesTriggered = false;
            }
        }


        private void OnDestroy()
        {
            for (int i = 0; i < m_tempTextureList.Count; i++)
            {
                UnityEngine.Object.DestroyImmediate(m_tempTextureList[i]);
            }
        }


        private void OnSceneGUI()
        {
            Vector3 originShift = m_terrainLoaderManager.GetOrigin();

            if (m_terrainLoaderManager.m_showSceneViewEditButttons)
            {
                float scaledScreenWidth = (Camera.current.pixelRect.size.x / EditorGUIUtility.pixelsPerPoint);
                float scaledScreenHeight = (Camera.current.pixelRect.size.y / EditorGUIUtility.pixelsPerPoint);
                float addButtonWidth = 35;
                float addButtonHeight = 20;

                Color originalGUIColor = GUI.backgroundColor;

                Vector2 addButtonSize = new Vector2(addButtonWidth, addButtonHeight);

                //Iterate through all the knonw terrain scenes and find free "terrain edge pieces" on which one could expand the world to 
                //We only want to perform this once when the list is empty, then cache the results, to force a refresh we can empty the list
                if (m_terrainLoaderManager.m_freeTerrainSlotsButtonPositions.Count == 0)
                {
                    foreach (TerrainScene ts in m_terrainLoaderManager.TerrainSceneStorage.m_terrainScenes)
                    {
                        //check for a free space in all cardinal directions
                        //north / top neighbor
                        double northX = ts.m_bounds.center.x + originShift.x;
                        double northZ = ts.m_bounds.center.z + ts.m_bounds.size.z - originShift.z;
                        AddTerrainSlotButtonIfFree(northX, northZ, ts.GetTerrainName());
                        //south / bottom neighbor
                        double southX = ts.m_bounds.center.x + originShift.x;
                        double southZ = ts.m_bounds.center.z - ts.m_bounds.size.z - originShift.z;
                        AddTerrainSlotButtonIfFree(southX, southZ, ts.GetTerrainName());
                        //west / left neighbor
                        double westX = ts.m_bounds.center.x - ts.m_bounds.size.x - originShift.x;
                        double westZ = ts.m_bounds.center.z + originShift.z;
                        AddTerrainSlotButtonIfFree(westX, westZ, ts.GetTerrainName());
                        //west / left neighbor
                        double eastX = ts.m_bounds.center.x + ts.m_bounds.size.x - originShift.x;
                        double eastZ = ts.m_bounds.center.z + originShift.z;
                        AddTerrainSlotButtonIfFree(eastX, eastZ, ts.GetTerrainName());
                    }
                }

                Camera sceneCamera = SceneView.lastActiveSceneView.camera;

                Handles.BeginGUI();
                foreach (Vector3 pos in m_terrainLoaderManager.m_freeTerrainSlotsButtonPositions.Keys)
                {
                    //We need the distance to the camera for a few GUI elements / features
                    float distanceToCamera = Vector3.Distance(sceneCamera.transform.position, pos);
                    //Do we collapse the buttons into a single "Go To" button?
                    bool collapseButtons = m_terrainLoaderManager.m_collapseDistantButtons && (distanceToCamera >= m_terrainLoaderManager.m_collapseButtonsDistance);

                    //Before we even do anything - is the center of the button even in the scene view? If not, skip all calculations etc.
                    //since there should be nothing to display in the end
                    if (!GaiaUtils.IsOnScreen(pos, GaiaSettings.m_maxGizmoDistance) || collapseButtons)
                    {
                        continue;
                    }

                    Vector3 sceneViewVector = sceneCamera.WorldToScreenPoint(pos);
                    if (sceneViewVector.z < 0)
                        sceneViewVector *= -1;

                    sceneViewVector = OffsetDPIScalingForScreenVector(sceneViewVector);

                    Vector2 addButtonPos = new Vector2(sceneViewVector.x - addButtonWidth / 2f, scaledScreenHeight - sceneViewVector.y - addButtonHeight / 2f);

                    GUI.backgroundColor = Color.green;
                    if (GUI.Button(new Rect(addButtonPos, addButtonSize), "Add"))
                    {
                        SingleTileCreationSettings singleTileCreationSettings = ScriptableObject.CreateInstance<SingleTileCreationSettings>();

                        //We want to create a terrain with the same dimensions / properties than its neighbor
                        TerrainData sourceTerrainData = null;

                        //We can try if the source neighbor terrain is loaded in the scene
                        string neighborName = "";
                        m_terrainLoaderManager.m_freeTerrainSlotsButtonPositions.TryGetValue(pos, out neighborName);
                        if (!string.IsNullOrEmpty(neighborName))
                        {
                            if (Terrain.activeTerrains.Count(x => x.name == neighborName) > 0)
                            {
                                Terrain neighborTerrain = Terrain.activeTerrains.First(x => x.name == neighborName);
                                if (neighborTerrain != null)
                                {
                                    //we found the neighbor terrain loaded in the scene, we can use this as source 
                                    sourceTerrainData = neighborTerrain.terrainData;
                                }
                            }
                        }
                        //No source terrain data found yet? We can try to load it up according to the terrain name then
                        if (sourceTerrainData == null)
                        {
                            string terrainDataPath = GaiaDirectories.GetTerrainDataScenePath() + "/" + neighborName + ".asset";
                            sourceTerrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(terrainDataPath);
                        }

                        //if the source terrain data still is not found, we display a message in the console 
                        //TODO: check if we can load in the "missing" terrain scene instead and grab the info from there.
                        if (sourceTerrainData == null)
                        {
                            Debug.LogError("Gaia was not able to gather the terrain data from the neighboring terrains. This might be caused by terrains having an unexpected name / storage location for their terrain data object. You can try to load in the surrounding, already existing terrain tiles before clicking the 'Add' button.");
                            Handles.EndGUI();
                            return;
                        }
                        else
                        {
                            //we do have a source terrain to set up the new terrain with, we can create the terrain tile through the session manager now
                            singleTileCreationSettings.m_createInTerrainScene = true;

                            singleTileCreationSettings.m_terrainSize = (int)sourceTerrainData.size.x;
                            //keep the new terrain centered at the position where the button is in the scene view!
                            singleTileCreationSettings.m_position = new Vector3(pos.x - sourceTerrainData.size.x / 2f, pos.y, pos.z - sourceTerrainData.size.z / 2f);

                            //Figure out a new name by looking at the world layout in the terrain scene storage and calculate the X / Z tile order numbers

                            int numberXTiles = m_terrainLoaderManager.TerrainSceneStorage.m_terrainTilesX;
                            int numberZTiles = m_terrainLoaderManager.TerrainSceneStorage.m_terrainTilesZ;
                            int tileSize = m_terrainLoaderManager.TerrainSceneStorage.m_terrainTilesSize;

                            //need to take into account that the world is centered around 0,0 - so x=0 z=0 is in the negative!
                            int tx = Mathf.FloorToInt((pos.x - (float)m_terrainLoaderManager.TerrainSceneStorage.m_pos00X) / singleTileCreationSettings.m_terrainSize);
                            int tz = Mathf.FloorToInt((pos.z - (float)m_terrainLoaderManager.TerrainSceneStorage.m_pos00Z) / singleTileCreationSettings.m_terrainSize);

                            singleTileCreationSettings.m_terrainName = string.Format("Terrain_{0}_{1}-{2}", tx, tz, String.Format("{0:yyyyMMdd - HHmmss}", DateTime.Now));
                            singleTileCreationSettings.m_terrainHeight = sourceTerrainData.size.y;

                            //Initialize the prototypes with null, if the user wants them to be copied we use the ones from the source terrain.
                            singleTileCreationSettings.m_terrainLayers = null;
                            singleTileCreationSettings.m_terrainTrees = null;
                            singleTileCreationSettings.m_terrainDetails = null;

                            if (m_terrainLoaderManager.m_copyPrototypesForNewTerrainTiles)
                            {
                                singleTileCreationSettings.m_terrainLayers = sourceTerrainData.terrainLayers;
                                singleTileCreationSettings.m_terrainTrees = sourceTerrainData.treePrototypes;
                                singleTileCreationSettings.m_terrainDetails = sourceTerrainData.detailPrototypes;
                            }

                            GaiaSessionManager.CreateSingleTerrainTile(singleTileCreationSettings, true);

                            //Load up the newest terrain scene (= the one we just created)
                            TerrainLoaderManager.TerrainScenes.Last().AddRegularReference(SessionManager.gameObject);

                            //force a refresh for the "Add" buttons in the scene view
                            m_terrainLoaderManager.m_freeTerrainSlotsButtonPositions.Clear();
                            Handles.EndGUI();
                            return;
                        }


                    }
                    GUI.backgroundColor = originalGUIColor;
                }
                Handles.EndGUI();

            }

            if (m_terrainLoaderManager.m_showSceneViewLoadingButttons || m_terrainLoaderManager.m_showSceneViewEditButttons || m_terrainLoaderManager.m_showGoToTerrainButton)
            {
                float scaledScreenWidth = (Camera.current.pixelRect.size.x / EditorGUIUtility.pixelsPerPoint);
                float scaledScreenHeight = (Camera.current.pixelRect.size.y / EditorGUIUtility.pixelsPerPoint);
                float buttonWidth = 65;
                float buttonHeight = 20;
                Vector2 buttonSize = new Vector2(buttonWidth, buttonHeight);


                Color originalGUIColor = GUI.backgroundColor;
                bool originalGUIState = GUI.enabled;

                Camera sceneCamera = SceneView.lastActiveSceneView.camera;

                //Count the max number of buttons displayed - this will determine the positioning of the buttons in screen space
                int maxButtonCount = 0;
                if (m_terrainLoaderManager.m_showSceneViewLoadingButttons)
                {
                    maxButtonCount++;
                }
                if (m_terrainLoaderManager.m_showSceneViewEditButttons)
                {
                    maxButtonCount++;
                }
                if (m_terrainLoaderManager.m_showGoToTerrainButton)
                {
                    maxButtonCount++;
                }
                


                if (sceneCamera != null) 
                {
                    Handles.BeginGUI();
                    int removeIndex = -99;
                    int removeImpostorIndex = -99;

                    for (int i = 0; i < m_terrainLoaderManager.TerrainSceneStorage.m_terrainScenes.Count; i++)
                    {
                        TerrainScene ts = m_terrainLoaderManager.TerrainSceneStorage.m_terrainScenes[i];


                        //Before we even do anything - is the center of this terrain even in the scene view? If not, skip all calculations etc.
                        //since there should be nothing to display in the end
                        if (!GaiaUtils.IsOnScreen(ts.m_bounds.center, GaiaSettings.m_maxGizmoDistance))
                        {
                            continue;
                        }

                        int currentButtonIndex = 0;

                        //Get the center of the terrain in screen space - this is what the buttons will be aligned to
                        Vector3 centerVector = Vector3.zero;
                        centerVector = sceneCamera.WorldToScreenPoint(ts.m_bounds.center - (Vector3Double)originShift) ;
                        centerVector = OffsetDPIScalingForScreenVector(centerVector);

                        //We need the distance to the camera for a few GUI elements / features
                        float distanceToCamera = Vector3.Distance(sceneCamera.transform.position, ts.m_bounds.center);

                        //Do we collapse the buttons into a single "Go To" button?
                        bool collapseButtons = m_terrainLoaderManager.m_collapseDistantButtons && (distanceToCamera >= m_terrainLoaderManager.m_collapseButtonsDistance);

                        if (m_terrainLoaderManager.m_showSceneViewEditButttons && !collapseButtons)
                        {
                            GUI.backgroundColor = Color.red;
                            if (DrawTerrainButton(ref currentButtonIndex, maxButtonCount, scaledScreenHeight, centerVector, buttonSize, "Delete"))
                            {
                                //only remember the index of the terrain scene to remove so we do not remove it while still iterating over the terrain scene storage.
                                if (!m_terrainLoaderManager.m_confirmOnSceneViewDelete || EditorUtility.DisplayDialog("Confirm Deletion", $"Do you really want to delete the terrain tile {ts.GetTerrainName()}? This will permanently DELETE this tile and remove it from the loading setup.", "Delete Tile", "Cancel"))
                                {
                                    removeIndex = i;
                                    if (!string.IsNullOrEmpty(ts.m_impostorScenePath))
                                    {
                                        removeImpostorIndex = i;
                                    }
                                }
                            }
                            GUI.backgroundColor = originalGUIColor;
                        }


                        if (m_terrainLoaderManager.m_showGoToTerrainButton || collapseButtons)
                        {
                            //further optimization: At twice the collapse distance we only display "Go To" buttons for half of the buttons
                            if (!collapseButtons || distanceToCamera <= m_terrainLoaderManager.m_collapseButtonsDistance * 2 || ((ts.m_bounds.center.x / ts.m_bounds.size.x) % 2==0) && ((ts.m_bounds.center.z / ts.m_bounds.size.z) % 2 == 0))
                            {
                                bool goTobuttonPressed = false;

                                if (collapseButtons)
                                {
                                    //If the buttons are collapsed, we layout the single button ourselves at the center of the tile
                                    Vector2 goToButtonPos = new Vector2(centerVector.x - buttonWidth / 2f, scaledScreenHeight - centerVector.y - buttonHeight / 2f);

                                    if (GUI.Button(new Rect(goToButtonPos, buttonSize), "Go To"))
                                    {
                                        goTobuttonPressed = true;
                                    }
                                }
                                else
                                {
                                    //If the go-to button is a regular button, we can draw it with our function so it has the right y-offset
                                    if (DrawTerrainButton(ref currentButtonIndex, maxButtonCount, scaledScreenHeight, centerVector, buttonSize, "Go To"))
                                    {
                                        goTobuttonPressed = true;
                                    }
                                }

                                if (goTobuttonPressed) 
                                { 
                                    Camera target = SceneView.lastActiveSceneView.camera;
                                    Transform temp = target.transform;
                                    Vector3 originalPosition = temp.position;
                                    Vector3 targetPos = Vector3.MoveTowards(ts.m_bounds.center, originalPosition, (float)ts.m_bounds.size.x / 4f);


                                    float min = 0, max = 0;

                                    //We want to position the camera close to the height of the terrain at that spot, but not below!
                                    //See if we can get the terrain data either from the scene, or from loading it up
                                    Terrain terrain = Terrain.activeTerrains.FirstOrDefault(x => x.name == ts.GetTerrainName());
                                    TerrainData terrainData = null;
                                    if (terrain != null)
                                    {
                                        terrainData = terrain.terrainData;
                                    }
                                    else
                                    {
                                        string terrainDataPath = GaiaDirectories.GetTerrainDataScenePath() + "/" + ts.GetTerrainName() + ".asset";
                                        terrainData = (TerrainData)AssetDatabase.LoadAssetAtPath(terrainDataPath, typeof(TerrainData));
                                    }
                                    //We got terrain data, we can sample the target height from it.
                                    if (terrainData != null)
                                    {
                                        //sample from the terrain, and add a bit of extra height to it, otherwise we will be glued to the ground
                                        max = terrainData.GetInterpolatedHeight(Mathf.InverseLerp((float)ts.m_bounds.min.x, (float)ts.m_bounds.max.x,targetPos.x), Mathf.InverseLerp((float)ts.m_bounds.min.z, (float)ts.m_bounds.max.z, targetPos.z)) + 20f;
                                    }
                                    else
                                    {
                                        //no terrain data? Concerning, but let's take the session manager world max height instead. This is usually high enough.
                                        SessionManager.GetWorldMinMax(ref min, ref max);
                                    }

                                    temp.position = (Vector3)ts.m_bounds.center + new Vector3(0f, max, 0f);
                                    temp.position = Vector3.MoveTowards(temp.position, originalPosition, (float)ts.m_bounds.size.x / 4f);
                                    temp.LookAt((Vector3)ts.m_bounds.center, Vector3.up);
                                    SceneView.lastActiveSceneView.AlignViewToObject(temp);
                                }
                            }
                        }

                        if (m_terrainLoaderManager.m_showSceneViewLoadingButttons && !collapseButtons)
                        {
                            //Switch the state of the load button depending on the load state of the terrain tile
                            if (ts.m_regularLoadState != LoadState.Loaded && ts.m_impostorLoadState != LoadState.Loaded)
                            {
                                if (DrawTerrainButton(ref currentButtonIndex, maxButtonCount, scaledScreenHeight, centerVector, buttonSize, "Terrain"))
                                {
                                    //We manage tiles loaded in via this button under the session manager
                                    //this avoids conflicts with the scene view camera / world origin loading, which tracks references under the terrain loader manager
                                    
                                    m_terrainLoaderManager.LockSceneViewLoading();
                                    ts.RemoveAllReferences(true);
                                    ts.RemoveAllImpostorReferences(true);
                                    ts.AddRegularReference(SessionManager.gameObject);
                                    m_terrainLoaderManager.AddManualLoadedRegularIndex(i);
                                }
                            }
                            else if (ts.m_regularLoadState == LoadState.Loaded && !string.IsNullOrEmpty(ts.m_impostorScenePath))
                            {
                                if (DrawTerrainButton(ref currentButtonIndex, maxButtonCount, scaledScreenHeight, centerVector, buttonSize, "Impostor"))
                                {
                                    //We manage impostors loaded in via this button under the session manager
                                    //this avoids conflicts with the scene view camera / world origin loading, which tracks references under the terrain loader manager
                                    ts.RemoveAllReferences(true);
                                    ts.RemoveAllImpostorReferences(true);
                                    ts.AddImpostorReference(SessionManager.gameObject);
                                    m_terrainLoaderManager.AddManualLoadedImpostorIndex(i);
                                }
                            }
                            else if (ts.m_impostorLoadState == LoadState.Loaded || ts.m_regularLoadState == LoadState.Loaded)
                            {
                                if (DrawTerrainButton(ref currentButtonIndex, maxButtonCount, scaledScreenHeight, centerVector, buttonSize, "Unload"))
                                {
                                    ts.RemoveAllReferences(true);
                                    ts.RemoveAllImpostorReferences(true);
                                    m_terrainLoaderManager.RemoveManualIndices(i);
                                }
                            }


                            
                        }
                    }
                    if (removeImpostorIndex >= 0)
                    {
                        m_terrainLoaderManager.TerrainSceneStorage.m_terrainScenes[removeImpostorIndex].RemoveAllImpostorReferences(true);
                        AssetDatabase.DeleteAsset(m_terrainLoaderManager.TerrainSceneStorage.m_terrainScenes[removeImpostorIndex].m_impostorScenePath);
                        m_terrainLoaderManager.TerrainSceneStorage.m_terrainScenes[removeImpostorIndex].m_impostorScenePath = "";
                        m_terrainLoaderManager.SaveStorageData();
                    }

                    if (removeIndex >= 0)
                    {
                        m_terrainLoaderManager.TerrainSceneStorage.m_terrainScenes[removeIndex].RemoveAllReferences(true);
                        AssetDatabase.DeleteAsset(m_terrainLoaderManager.TerrainSceneStorage.m_terrainScenes[removeIndex].m_scenePath);
                        m_terrainLoaderManager.TerrainSceneStorage.m_terrainScenes.RemoveAt(removeIndex);
                        m_terrainLoaderManager.SaveStorageData();

                        //Force refresh for the "add" buttons
                        m_terrainLoaderManager.m_freeTerrainSlotsButtonPositions.Clear();
                    }


                    Handles.EndGUI();
                }
            }

           

        }

        private bool DrawTerrainButton(ref int currentButtonIndex, int maxButtonCount, float scaledScreenHeight, Vector3 centerVector, Vector2 buttonSize, string text)
        {
            //Buttons are centered around the centerVector - depending on the max amount of buttons and the current index we can calculate the correct y-offset for the current button
            float buttonSpacing = 25;
            float yOffset = maxButtonCount * buttonSpacing / 2f - (maxButtonCount - currentButtonIndex) * buttonSpacing;
            Vector2 buttonPos = new Vector2(centerVector.x - buttonSize.x / 2f, scaledScreenHeight - centerVector.y  - yOffset - buttonSize.y);
            currentButtonIndex++;
            if (GUI.Button(new Rect(buttonPos, buttonSize), text))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Offsets / converts DPI scaling for a screen point so that buttons etc. are drawn in the right spot across different DPI scaling settings within the OS / Unity Editor
        /// </summary>
        /// <param name="inputVector"></param>
        /// <returns></returns>
        private Vector3 OffsetDPIScalingForScreenVector(Vector3 inputVector)
        {
            return new Vector3(inputVector.x / EditorGUIUtility.pixelsPerPoint, inputVector.y / EditorGUIUtility.pixelsPerPoint, inputVector.z / EditorGUIUtility.pixelsPerPoint); ;
        }

        private Vector3Double OnTerrainOffset(Vector3 flatForwardVector, float terrainSize, float terrainOffsetAmount)
        {
            return new Vector3Double(flatForwardVector.x * terrainSize * terrainOffsetAmount, 0, flatForwardVector.z * terrainSize * terrainOffsetAmount);
        }

        private void AddTerrainSlotButtonIfFree(double xPos, double zPos, string terrainName)
        {
            if (!m_terrainLoaderManager.TerrainSceneStorage.m_terrainScenes.Exists(b => b.m_bounds.center.x == xPos && b.m_bounds.center.z == zPos))
            {

                Vector3 newPos = new Vector3((float)xPos, 0f, (float)zPos);
                if (!m_terrainLoaderManager.m_freeTerrainSlotsButtonPositions.ContainsKey(newPos))
                {
                    m_terrainLoaderManager.m_freeTerrainSlotsButtonPositions.Add(newPos, terrainName);
                }
            }
        }

        public override void OnInspectorGUI()
        {
            if (redStyle == null || redStyle.normal.background == null || greenStyle == null || greenStyle.normal.background == null)
            {
                redStyle = new GUIStyle();
                redStyle.normal.background = GaiaUtils.GetBGTexture(Color.red, m_tempTextureList);

                greenStyle = new GUIStyle();
                greenStyle.normal.background = GaiaUtils.GetBGTexture(Color.green, m_tempTextureList);
            }


            //Init editor utils
            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }
            m_editorUtils.Initialize(); // Do not remove this!
            
            if (GaiaUtils.HasDynamicLoadedTerrains())
            {
                m_editorUtils.Panel("GeneralSettings", DrawGeneralSettings, true);
                m_editorUtils.Panel("SceneViewSettings", DrawSceneViewSettings, true);
                m_editorUtils.Panel("LoaderPanel", DrawLoaders, false);
                m_editorUtils.Panel("PlaceholderPanel", DrawTerrains, false);
            }
            else
            {
                EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("NoTerrainLoadingMessage"),MessageType.Info);
                m_editorUtils.Panel("GeneralSettings", DrawReducedGeneralSettings, true);
            }
            m_editorUtils.Panel("FloatingPointFix", DrawFloatingPointFix, false);
        }

        private void DrawSceneViewSettings(bool helpEnabled)
        {

            bool originalGUIState = GUI.enabled;

            m_terrainLoaderManager.CenterSceneViewLoadingOn = (CenterSceneViewLoadingOn)m_editorUtils.EnumPopup("CenterSceneViewLoadingOn", m_terrainLoaderManager.CenterSceneViewLoadingOn, helpEnabled);
            if (m_terrainLoaderManager.CenterSceneViewLoadingOn != CenterSceneViewLoadingOn.SceneViewCamera)
            {
                GUI.enabled = false;
            }
            m_terrainLoaderManager.m_sceneViewLoadDelay = m_editorUtils.FloatField("SceneViewLoadDelay", m_terrainLoaderManager.m_sceneViewLoadDelay / 1000f, helpEnabled) * 1000f;
            //m_terrainLoaderManager.m_showManualRefreshButton = m_editorUtils.Toggle("SceneViewShowManualRefreshButton", m_terrainLoaderManager.m_showManualRefreshButton, helpEnabled);
            m_terrainLoaderManager.m_showSelectLoaderManagerButton = m_editorUtils.Toggle("ShowSelectLoaderManagerButton", m_terrainLoaderManager.m_showSelectLoaderManagerButton, helpEnabled);
            m_terrainLoaderManager.m_showLockSceneViewLoadingButton = m_editorUtils.Toggle("ShowLockSceneViewButton", m_terrainLoaderManager.m_showLockSceneViewLoadingButton, helpEnabled);
            GUI.enabled = originalGUIState;
            double loadingRange = m_editorUtils.DoubleField("SceneViewLoadingRange", m_terrainLoaderManager.GetLoadingRange(), helpEnabled);
            double impostorLoadingRange = m_terrainLoaderManager.GetImpostorLoadingRange();
            if (GaiaUtils.HasImpostorTerrains())
            {
                impostorLoadingRange = m_editorUtils.DoubleField("SceneViewImpostorLoadingRange", m_terrainLoaderManager.GetImpostorLoadingRange(), helpEnabled);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                m_editorUtils.Label("SceneViewImpostorLoadingRange");
                //offer button to create impostors setup
                if (m_editorUtils.Button("OpenTerrainMeshExporterForImpostors", GUILayout.Width(EditorGUIUtility.currentViewWidth - EditorGUIUtility.labelWidth - 38)))
                {
                    TerrainConverterEditorWindow.OpenWithPreset("Create Impostors");
                }
                EditorGUILayout.EndHorizontal();

            }

            if (loadingRange != m_terrainLoaderManager.GetLoadingRange() || impostorLoadingRange != m_terrainLoaderManager.GetImpostorLoadingRange())
            {
                m_terrainLoaderManager.SetLoadingRange(loadingRange, impostorLoadingRange);
            }
            m_terrainLoaderManager.m_autoSelectTLMOnGaiaPanelDrop = m_editorUtils.Toggle("AutoShowTerrainManager", m_terrainLoaderManager.m_autoSelectTLMOnGaiaPanelDrop, helpEnabled);
            EditorGUI.BeginChangeCheck();
            m_terrainLoaderManager.m_autoToggleTerrainBoxes = m_editorUtils.Toggle("AutoShowTerrainBoxes", m_terrainLoaderManager.m_autoToggleTerrainBoxes, helpEnabled);
            EditorGUI.BeginChangeCheck();
            m_terrainLoaderManager.m_showOriginTerrainBoxes = m_editorUtils.Toggle("ShowTerrainBoxes", m_terrainLoaderManager.m_showOriginTerrainBoxes, helpEnabled);
            EditorGUI.indentLevel++;
            if (!m_terrainLoaderManager.m_showOriginTerrainBoxes)
            {
                GUI.enabled = false;
            }
            m_terrainLoaderManager.m_showOriginTerrainBoxesLoaded = m_editorUtils.Toggle("ShowTerrainBoxesLoaded", m_terrainLoaderManager.m_showOriginTerrainBoxesLoaded, helpEnabled);
            m_terrainLoaderManager.m_showOriginTerrainBoxesUnLoaded = m_editorUtils.Toggle("ShowTerrainBoxesUnloaded", m_terrainLoaderManager.m_showOriginTerrainBoxesUnLoaded, helpEnabled);
            GUI.enabled = originalGUIState;
            EditorGUI.indentLevel--;
            GUILayout.Space(10);
            m_editorUtils.Heading("SceneViewEditing");
            m_terrainLoaderManager.m_showSceneViewLoadingButttons = m_editorUtils.Toggle("ShowSceneViewLoadingButtons", m_terrainLoaderManager.m_showSceneViewLoadingButttons, helpEnabled);
            m_terrainLoaderManager.m_collapseDistantButtons = m_editorUtils.Toggle("CollapseDistantButtons", m_terrainLoaderManager.m_collapseDistantButtons, helpEnabled);
            EditorGUI.indentLevel++;
            if (!m_terrainLoaderManager.m_collapseDistantButtons)
            {
                GUI.enabled = false;
            }
            m_terrainLoaderManager.m_collapseButtonsDistance = m_editorUtils.FloatField("CollapseButtonsDistance", m_terrainLoaderManager.m_collapseButtonsDistance, helpEnabled);
            GUI.enabled = originalGUIState;
            EditorGUI.indentLevel--;
            //m_terrainLoaderManager.m_autoToggleGoToTerrainButton = m_editorUtils.Toggle("AutoToggleGoToTerrainButton", m_terrainLoaderManager.m_autoToggleGoToTerrainButton);
            m_terrainLoaderManager.m_showGoToTerrainButton = m_editorUtils.Toggle("ShowGoToTerrainButton", m_terrainLoaderManager.m_showGoToTerrainButton, helpEnabled);
            m_terrainLoaderManager.m_showSceneViewEditButttons = m_editorUtils.Toggle("ShowSceneViewExtensionButtons", m_terrainLoaderManager.m_showSceneViewEditButttons, helpEnabled);
            if (!m_terrainLoaderManager.m_showSceneViewEditButttons)
            {
                GUI.enabled = false;
            }
            EditorGUI.indentLevel++;
            m_terrainLoaderManager.m_copyPrototypesForNewTerrainTiles = m_editorUtils.Toggle("CopyPrototypesForNewTerrainTiles", m_terrainLoaderManager.m_copyPrototypesForNewTerrainTiles, helpEnabled);
            m_terrainLoaderManager.m_confirmOnSceneViewDelete = m_editorUtils.Toggle("ConfirmSceneViewDelete", m_terrainLoaderManager.m_confirmOnSceneViewDelete, helpEnabled);
            EditorGUI.indentLevel--;
            GUI.enabled = originalGUIState;



            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }
        }

        private void DrawReducedGeneralSettings(bool helpEnabled)
        {
            m_terrainLoaderManager.m_autoTerrainStitching = m_editorUtils.Toggle("AutoTerrainStitching", m_terrainLoaderManager.m_autoTerrainStitching, helpEnabled);
        }


        private void DrawFloatingPointFix(bool helpEnabled)
        {
            EditorGUI.BeginChangeCheck();
            m_terrainLoaderManager.TerrainSceneStorage.m_useFloatingPointFix = m_editorUtils.Toggle("FloatingPointFixEnabled", m_terrainLoaderManager.TerrainSceneStorage.m_useFloatingPointFix, helpEnabled);
            if (EditorGUI.EndChangeCheck())
            {
                if (m_terrainLoaderManager.TerrainSceneStorage.m_useFloatingPointFix)
                {
                    if (EditorUtility.DisplayDialog("Add Floating Point Fix?", "This will activate a fix for floating point imprecisions in your scene. This will add aditional components to your player, and other objects in your scene. Continue?", "OK", "Cancel"))
                    {
                       TerrainLoaderManager.FloatingPointFix_Add();
                    }
                    else
                    {
                        m_terrainLoaderManager.TerrainSceneStorage.m_useFloatingPointFix = false;
                    }
                }
                else
                {
                    if (EditorUtility.DisplayDialog("Remove Floating Point Fix?", "This will remove all floating point fix components in the scene. Continue?", "OK", "Cancel"))
                    {
                        TerrainLoaderManager.FloatingPointFix_Remove();
                    }
                    else
                    {
                        m_terrainLoaderManager.TerrainSceneStorage.m_useFloatingPointFix = true;
                    }
                }
            }

        }

        private void DrawGeneralSettings(bool helpEnabled)
        {

            bool originalGUIState = GUI.enabled;

            m_terrainLoaderManager.TerrainSceneStorage = (TerrainSceneStorage)m_editorUtils.ObjectField("TerrainSceneStorage", m_terrainLoaderManager.TerrainSceneStorage, typeof(TerrainSceneStorage), false, helpEnabled);

            bool oldLoadingEnabled = m_terrainLoaderManager.TerrainSceneStorage.m_terrainLoadingEnabled;
            m_terrainLoaderManager.TerrainSceneStorage.m_terrainLoadingEnabled = m_editorUtils.Toggle("TerrainLoadingEnabled", m_terrainLoaderManager.TerrainSceneStorage.m_terrainLoadingEnabled, helpEnabled);
            if (!oldLoadingEnabled && m_terrainLoaderManager.TerrainSceneStorage.m_terrainLoadingEnabled)
            {
                //User re-enabled the loaders
                m_terrainLoaderManager.UpdateTerrainLoadState();
            }
            if (oldLoadingEnabled != m_terrainLoaderManager.TerrainSceneStorage.m_terrainLoadingEnabled)
            {
                //Value was changed, dirty the object to make sure the value is being saved
                EditorUtility.SetDirty(m_terrainLoaderManager.TerrainSceneStorage);
            }
            m_terrainLoaderManager.m_assumeGridLayout = m_editorUtils.Toggle("AssumeGridLayout", m_terrainLoaderManager.m_assumeGridLayout, helpEnabled);


#if !PW_ADDRESSABLES
            GUI.enabled = false;
#endif
            m_terrainLoaderManager.TerrainSceneStorage.m_useAddressables = m_editorUtils.Toggle("UseAddressables", m_terrainLoaderManager.TerrainSceneStorage.m_useAddressables, helpEnabled);
            if (m_terrainLoaderManager.TerrainSceneStorage.m_useAddressables)
            {
                EditorGUI.indentLevel++;
                m_terrainLoaderManager.TerrainSceneStorage.m_preloadAddressablesWithImpostors = m_editorUtils.Toggle("PreloadAddressablesWithImpostors", m_terrainLoaderManager.TerrainSceneStorage.m_preloadAddressablesWithImpostors, helpEnabled);
                EditorGUI.indentLevel--;
            }
            GUI.enabled = originalGUIState;

            m_terrainLoaderManager.m_terrainLoadingTresholdMS = m_editorUtils.IntField("LoadingTimeTreshold", m_terrainLoaderManager.m_terrainLoadingTresholdMS, helpEnabled);
            m_terrainLoaderManager.m_trackLoadingProgressTimeOut = m_editorUtils.LongField("LoadingProgressTimeout", m_terrainLoaderManager.m_trackLoadingProgressTimeOut, helpEnabled);
            Application.backgroundLoadingPriority = (ThreadPriority)m_editorUtils.EnumPopup("ApplicationLoadingPriority", Application.backgroundLoadingPriority, helpEnabled);
            GUILayout.Space(10);
            m_editorUtils.Heading("CachingSettings");
            m_editorUtils.InlineHelp("CachingSettings", helpEnabled);
            EditorGUI.BeginChangeCheck();
            m_terrainLoaderManager.m_cacheInRuntime = m_editorUtils.Toggle("CacheInRuntime", m_terrainLoaderManager.m_cacheInRuntime, helpEnabled);
            m_terrainLoaderManager.m_cacheInEditor = m_editorUtils.Toggle("CacheInEditor", m_terrainLoaderManager.m_cacheInEditor, helpEnabled);
            m_terrainLoaderManager.m_unloadUnusedAssetsRuntime = m_editorUtils.Toggle("UnloadAssetsRuntime", m_terrainLoaderManager.m_unloadUnusedAssetsRuntime, helpEnabled);
            m_terrainLoaderManager.m_unloadUnusedAssetsEditor = m_editorUtils.Toggle("UnloadAssetsEditor", m_terrainLoaderManager.m_unloadUnusedAssetsEditor, helpEnabled);
            string allocatedMegabytes = (Math.Round(Profiler.GetTotalAllocatedMemoryLong() / Math.Pow(1024, 2))).ToString();
            string availableMegabytes = SystemInfo.systemMemorySize.ToString();
            allocatedMegabytes = allocatedMegabytes.PadLeft(allocatedMegabytes.Length + (availableMegabytes.Length - allocatedMegabytes.Length) * 3 , ' ');

            GUIStyle style = new GUIStyle(GUI.skin.label);

            if (m_terrainLoaderManager.m_cacheMemoryThreshold < Profiler.GetTotalAllocatedMemoryLong())
            {
                style = redStyle;
            }
            else
            {
                style = greenStyle;
            }
            m_editorUtils.LabelField("MemoryAvailable", new GUIContent(availableMegabytes), helpEnabled);
            m_editorUtils.LabelField("MemoryAllocated", new GUIContent(allocatedMegabytes), style, helpEnabled);

            //GUI.color = originalGUIColor;

            if (m_terrainLoaderManager.m_cacheInRuntime || m_terrainLoaderManager.m_cacheInEditor)
            {
                m_terrainLoaderManager.m_cacheMemoryThresholdPreset = (CacheSizePreset)m_editorUtils.EnumPopup("CacheMemoryThreshold", m_terrainLoaderManager.m_cacheMemoryThresholdPreset, helpEnabled);
                if (m_terrainLoaderManager.m_cacheMemoryThresholdPreset == CacheSizePreset.Custom)
                {
                    EditorGUI.indentLevel++;
                    m_terrainLoaderManager.m_cacheMemoryThreshold = m_editorUtils.LongField("ThresholdInBytes", m_terrainLoaderManager.m_cacheMemoryThreshold, helpEnabled);
                    EditorGUI.indentLevel--;
                }
                int cacheKeepAliveSeconds = (int)m_terrainLoaderManager.m_cacheKeepAliveTime / 1000;
                cacheKeepAliveSeconds = m_editorUtils.IntField("CacheKeepAliveSeconds", cacheKeepAliveSeconds, helpEnabled);
                m_terrainLoaderManager.m_cacheKeepAliveTime = cacheKeepAliveSeconds * 1000;
            }
            if (EditorGUI.EndChangeCheck())
            {
                if (m_terrainLoaderManager.m_cacheMemoryThresholdPreset != CacheSizePreset.Custom)
                {
                    m_terrainLoaderManager.m_cacheMemoryThreshold = (long)((int)m_terrainLoaderManager.m_cacheMemoryThresholdPreset * Math.Pow(1024, 3));
                }
                m_terrainLoaderManager.UpdateCaching();
            }



            
            GUILayout.Space(10);

            if (!GaiaUtils.HasColliderTerrains())
            {
                GUI.enabled = false;
            }

            m_editorUtils.Heading("ColliderOnlyLoadingHeader");
            //This flag is special in so far as that when the user switches it, we must first perform a scene unload while the old value is still active
            //then change the flag in the terrain scene storage and then do a refresh with the new setting applied.
            bool colliderLoadingEnabled = m_terrainLoaderManager.TerrainSceneStorage.m_colliderOnlyLoading;
            colliderLoadingEnabled = m_editorUtils.Toggle("ColliderOnlyLoadingEnabled", colliderLoadingEnabled, helpEnabled);
            if (colliderLoadingEnabled != m_terrainLoaderManager.TerrainSceneStorage.m_colliderOnlyLoading)
            {
                //User changed the flag, do an unload with the old setting
                m_terrainLoaderManager.UnloadAll(true);
                //then change the actual flag in storage
                m_terrainLoaderManager.TerrainSceneStorage.m_colliderOnlyLoading = colliderLoadingEnabled;
                //now do a refresh under the new setting
                m_terrainLoaderManager.RefreshSceneViewLoadingRange();

                //Add the required scenes to build settings
                if (colliderLoadingEnabled)
                {
                    GaiaSessionManager.AddOnlyColliderScenesToBuildSettings(TerrainLoaderManager.TerrainScenes);
                }
                else
                {
                    GaiaSessionManager.AddTerrainScenesToBuildSettings(TerrainLoaderManager.TerrainScenes);
                }
            }

            GUI.enabled = originalGUIState;

            if (colliderLoadingEnabled)
            {
                EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("ColliderOnlyLoadingInfo"), MessageType.Info);
                m_editorUtils.Heading("DeactivateRuntimeHeader");
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                m_terrainLoaderManager.TerrainSceneStorage.m_deactivateRuntimePlayer = m_editorUtils.Toggle("DeactivateRuntimePlayer", m_terrainLoaderManager.TerrainSceneStorage.m_deactivateRuntimePlayer, helpEnabled);
                m_terrainLoaderManager.TerrainSceneStorage.m_deactivateRuntimeLighting = m_editorUtils.Toggle("DeactivateRuntimeLighting", m_terrainLoaderManager.TerrainSceneStorage.m_deactivateRuntimeLighting, helpEnabled);
                m_terrainLoaderManager.TerrainSceneStorage.m_deactivateRuntimeAudio = m_editorUtils.Toggle("DeactivateRuntimeAudio", m_terrainLoaderManager.TerrainSceneStorage.m_deactivateRuntimeAudio, helpEnabled);
                m_terrainLoaderManager.TerrainSceneStorage.m_deactivateRuntimeWeather = m_editorUtils.Toggle("DeactivateRuntimeWeather", m_terrainLoaderManager.TerrainSceneStorage.m_deactivateRuntimeWeather, helpEnabled);
                m_terrainLoaderManager.TerrainSceneStorage.m_deactivateRuntimeWater = m_editorUtils.Toggle("DeactivateRuntimeWater", m_terrainLoaderManager.TerrainSceneStorage.m_deactivateRuntimeWater, helpEnabled);
                m_terrainLoaderManager.TerrainSceneStorage.m_deactivateRuntimeScreenShotter = m_editorUtils.Toggle("DeactivateRuntimeScreenShotter", m_terrainLoaderManager.TerrainSceneStorage.m_deactivateRuntimeScreenShotter, helpEnabled);
                if (EditorGUI.EndChangeCheck())
                {
                    m_terrainLoaderManager.SaveStorageData();
                }
                EditorGUI.indentLevel--;
            }
            else
            {
                //offer button to create collider setup
                if (m_editorUtils.Button("OpenTerrainMeshExporterForColliders"))
                {
                    ExportTerrain exportTerrainWindow = EditorWindow.GetWindow<ExportTerrain>();
                    exportTerrainWindow.FindAndSetPreset("Collider");
                    exportTerrainWindow.m_settings.m_customSettingsFoldedOut = false;
                }
            }
        }

        private void DrawTerrains(bool helpEnabled)
        {
            bool originalGUIState = GUI.enabled;

#if GAIA_2023_PRO
            EditorGUILayout.BeginHorizontal();
            m_editorUtils.Label("IngestTerrain", GUILayout.Width(100));
            m_ingestTerrain = (Terrain)EditorGUILayout.ObjectField(m_ingestTerrain, typeof(Terrain), true);
            if (m_editorUtils.Button("Ingest"))
            {
                ////Try to find the X and Z coordinate for the scene name
                //double minX = m_terrainLoaderManager.TerrainSceneStorage.m_terrainScenes.Select(x => x.m_pos.x).Min();
                //double maxX = m_terrainLoaderManager.TerrainSceneStorage.m_terrainScenes.Select(x => x.m_pos.x).Max();
                //double minZ = m_terrainLoaderManager.TerrainSceneStorage.m_terrainScenes.Select(x => x.m_pos.z).Min();
                //double maxZ = m_terrainLoaderManager.TerrainSceneStorage.m_terrainScenes.Select(x => x.m_pos.z).Max();
                GaiaSessionManager sessionManager = GaiaSessionManager.GetSessionManager();
                WorldCreationSettings worldCreationSettings = new WorldCreationSettings()
                {
                    m_autoUnloadScenes = false,
                    m_addLoadingScreen = false,
                    m_applyFloatingPointFix = TerrainLoaderManager.Instance.TerrainSceneStorage.m_useFloatingPointFix,
                    m_isWorldMap = false
                };
                TerrainScene newScene = TerrainSceneCreator.CreateTerrainScene(m_ingestTerrain.gameObject.scene, TerrainLoaderManager.Instance.TerrainSceneStorage, sessionManager.m_session, m_ingestTerrain.gameObject, worldCreationSettings);
                TerrainLoaderManager.Instance.TerrainSceneStorage.m_terrainScenes.Add(newScene);
                TerrainLoaderManager.Instance.SaveStorageData();
                GaiaSessionManager.AddTerrainScenesToBuildSettings(new List<TerrainScene>() { newScene});
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(EditorGUIUtility.singleLineHeight);
#endif

            EditorGUILayout.BeginHorizontal();
            if (m_editorUtils.Button("AddToBuildSettings"))
            {
                if (TerrainLoaderManager.ColliderOnlyLoadingActive)
                {
                    if (EditorUtility.DisplayDialog(m_editorUtils.GetTextValue("AddColliderScenesToBuildSettingsPopupTitle"), m_editorUtils.GetTextValue("AddColliderScenesToBuildSettingsPopupText"), m_editorUtils.GetTextValue("Continue"), m_editorUtils.GetTextValue("Cancel")))
                    {
#if GAIA_2023_PRO
                        GaiaSessionManager.AddOnlyColliderScenesToBuildSettings(m_terrainLoaderManager.TerrainSceneStorage.m_terrainScenes);
#endif
                        EditorGUIUtility.ExitGUI();
                    }
                }
                else
                {
                    if (EditorUtility.DisplayDialog(m_editorUtils.GetTextValue("AddToBuildSettingsPopupTitle"), m_editorUtils.GetTextValue("AddToBuildSettingsPopupText"), m_editorUtils.GetTextValue("Continue"), m_editorUtils.GetTextValue("Cancel")))
                    {
#if GAIA_2023_PRO
                        GaiaSessionManager.AddTerrainScenesToBuildSettings(m_terrainLoaderManager.TerrainSceneStorage.m_terrainScenes);
#endif
                        EditorGUIUtility.ExitGUI();
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (m_editorUtils.Button("AddOnlyLoadedToBuildSettings"))
            {
                if (TerrainLoaderManager.ColliderOnlyLoadingActive)
                {
                    if (EditorUtility.DisplayDialog(m_editorUtils.GetTextValue("AddColliderScenesToBuildSettingsPopupTitle"), m_editorUtils.GetTextValue("AddOnlyLoadedColliderScenesToBuildSettingsPopupText"), m_editorUtils.GetTextValue("Continue"), m_editorUtils.GetTextValue("Cancel")))
                    {
#if GAIA_2023_PRO
                        GaiaSessionManager.AddOnlyColliderScenesToBuildSettings(m_terrainLoaderManager.TerrainSceneStorage.m_terrainScenes.Where(x=>x.m_regularLoadState == LoadState.Loaded).ToList());
#endif
                        EditorGUIUtility.ExitGUI();
                    }
                }
                else
                {
                    if (EditorUtility.DisplayDialog(m_editorUtils.GetTextValue("AddToBuildSettingsPopupTitle"), m_editorUtils.GetTextValue("AddOnlyLoadedToBuildSettingsPopupText"), m_editorUtils.GetTextValue("Continue"), m_editorUtils.GetTextValue("Cancel")))
                    {
#if GAIA_2023_PRO
                        GaiaSessionManager.AddTerrainScenesToBuildSettings(m_terrainLoaderManager.TerrainSceneStorage.m_terrainScenes.Where(x => x.m_regularLoadState == LoadState.Loaded).ToList());
#endif
                        EditorGUIUtility.ExitGUI();
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (m_editorUtils.Button("UnloadAll"))
            {
                m_terrainLoaderManager.RemoveAllManualIndices();
                m_terrainLoaderManager.UnloadAll(true);
            }
            if (m_editorUtils.Button("LoadAll"))
            {
                if (EditorUtility.DisplayDialog(m_editorUtils.GetTextValue("LoadAllPopupTitle"), m_editorUtils.GetTextValue("LoadAllPopupText"), m_editorUtils.GetTextValue("Continue"), m_editorUtils.GetTextValue("Cancel")))
                {
                    for (int i = 0; i < m_terrainLoaderManager.TerrainSceneStorage.m_terrainScenes.Count; i++)
                    {
                        TerrainScene terrainScene = m_terrainLoaderManager.TerrainSceneStorage.m_terrainScenes[i];
                        terrainScene.AddRegularReference(m_terrainLoaderManager.gameObject);
                        m_terrainLoaderManager.AddManualLoadedRegularIndex(i);
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            if (!GaiaUtils.HasImpostorTerrains())
            {
                GUI.enabled = false;            
            }
            EditorGUILayout.BeginHorizontal();
            if (m_editorUtils.Button("UnloadAllImpostors"))
            {
                m_terrainLoaderManager.SetLoadingRange(m_terrainLoaderManager.GetLoadingRange(), 0);
                m_terrainLoaderManager.UnloadAllImpostors(true);
                m_terrainLoaderManager.RemoveAllManualImpostorIndices();
            }
            if (m_editorUtils.Button("LoadAllImpostors"))
            {
                if (EditorUtility.DisplayDialog(m_editorUtils.GetTextValue("LoadAllPopupTitle"), m_editorUtils.GetTextValue("LoadAllImpostorsPopupText"), m_editorUtils.GetTextValue("Continue"), m_editorUtils.GetTextValue("Cancel")))
                {
                    for (int i = 0; i < m_terrainLoaderManager.TerrainSceneStorage.m_terrainScenes.Count; i++)
                    {
                        TerrainScene terrainScene = m_terrainLoaderManager.TerrainSceneStorage.m_terrainScenes[i];
                        terrainScene.AddImpostorReference(m_terrainLoaderManager.gameObject);
                        m_terrainLoaderManager.AddManualLoadedImpostorIndex(i);
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (m_editorUtils.Button("DeleteAllImpostors"))
            {
                if (EditorUtility.DisplayDialog(m_editorUtils.GetTextValue("DeleteAllImpPopupTitle"), m_editorUtils.GetTextValue("DeleteAllImpPopupText"), m_editorUtils.GetTextValue("Continue"), m_editorUtils.GetTextValue("Cancel")))
                {
                    for (int i = 0; i < m_terrainLoaderManager.TerrainSceneStorage.m_terrainScenes.Count; i++)
                    {
                        m_terrainLoaderManager.TerrainSceneStorage.m_terrainScenes[i].RemoveAllImpostorReferences(true);
                        AssetDatabase.DeleteAsset(m_terrainLoaderManager.TerrainSceneStorage.m_terrainScenes[i].m_impostorScenePath);
                        m_terrainLoaderManager.TerrainSceneStorage.m_terrainScenes[i].m_impostorScenePath = "";
                    }
                    m_terrainLoaderManager.RemoveAllManualImpostorIndices();
                    m_terrainLoaderManager.SaveStorageData();
                }
            }

            EditorGUILayout.EndHorizontal();

            GUI.enabled = originalGUIState;

            GUILayout.Space(EditorGUIUtility.singleLineHeight);

            float buttonWidth1 = 110;
            float buttonWidth2 = 60;

            if (m_terrainBoxStyle == null || m_terrainBoxStyle.normal.background == null)
            {
                m_terrainBoxStyle = new GUIStyle(EditorStyles.helpBox);
                m_terrainBoxStyle.margin = new RectOffset(0, 0, 0, 0);
                m_terrainBoxStyle.padding = new RectOffset(3, 3, 3, 3);
            }

            int removeIndex = -99;
            int removeImpostorIndex = -99;
            int currentIndex = 0;

            for (int i = 0; i < m_terrainLoaderManager.TerrainSceneStorage.m_terrainScenes.Count; i++)
            {
                TerrainScene terrainScene = m_terrainLoaderManager.TerrainSceneStorage.m_terrainScenes[i];
                EditorGUILayout.BeginVertical(m_terrainBoxStyle);
                {
                    EditorGUILayout.LabelField(terrainScene.GetTerrainName());
                    EditorGUILayout.BeginHorizontal();
                    bool isLoaded = terrainScene.m_regularLoadState == LoadState.Loaded && terrainScene.TerrainObj != null && terrainScene.TerrainObj.activeInHierarchy;
                    bool isImpostorLoaded = terrainScene.m_impostorLoadState == LoadState.Loaded || terrainScene.m_impostorLoadState == LoadState.Cached;

                    bool currentGUIState = GUI.enabled;
                    GUI.enabled = isLoaded;
                    if (m_editorUtils.Button("SelectPlaceholder", GUILayout.Width(buttonWidth1)))
                    {
                        Selection.activeGameObject = GameObject.Find(terrainScene.GetTerrainName());
                        EditorGUIUtility.PingObject(Selection.activeObject);
                    }
                    GUI.enabled = currentGUIState;
                    if (isLoaded)
                    {
                        if (m_editorUtils.Button("UnloadPlaceholder", GUILayout.Width(buttonWidth2)))
                        {
                            if (ResetToWorldOriginLoading())
                            {
                                terrainScene.RemoveAllReferences(true);
                                m_terrainLoaderManager.RemoveManualLoadedRegularIndex(i);
                            }
                        }
                    }
                    else
                    {
                        if (m_editorUtils.Button("LoadPlaceholder", GUILayout.Width(buttonWidth2)))
                        {
                            if (ResetToWorldOriginLoading())
                            {
                                terrainScene.AddRegularReference(m_terrainLoaderManager.gameObject);
                                m_terrainLoaderManager.AddManualLoadedRegularIndex(i);
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(terrainScene.m_impostorScenePath))
                    {
                        GUI.enabled = false;
                    }

                    if (isImpostorLoaded)
                    {
                        if (m_editorUtils.Button("UnLoadImpostor", GUILayout.Width(buttonWidth1)))
                        {
                            if (ResetToWorldOriginLoading())
                            {
                                terrainScene.RemoveAllImpostorReferences(true);
                                m_terrainLoaderManager.RemoveManualLoadedImpostorIndex(i);
                            }
                        }
                    }
                    else
                    {
                        if (m_editorUtils.Button("LoadImpostor", GUILayout.Width(buttonWidth1)))
                        {
                            if (ResetToWorldOriginLoading())
                            {
                                terrainScene.AddImpostorReference(m_terrainLoaderManager.gameObject);
                                m_terrainLoaderManager.AddManualLoadedImpostorIndex(i);
                            }
                        }
                    }

                    GUI.enabled = originalGUIState;

                    if (m_editorUtils.Button("RemoveScene"))
                    {
                        if (EditorUtility.DisplayDialog(m_editorUtils.GetTextValue("RemoveSceneTitle"), m_editorUtils.GetTextValue("RemoveSceneText"), m_editorUtils.GetTextValue("Continue"), m_editorUtils.GetTextValue("Cancel")))
                        {
                            removeIndex = currentIndex;
                        }
                    }

                    if (string.IsNullOrEmpty(terrainScene.m_impostorScenePath))
                    {
                        GUI.enabled = false;
                    }

                        if (m_editorUtils.Button("RemoveImpostor"))
                    {
                        if (EditorUtility.DisplayDialog(m_editorUtils.GetTextValue("RemoveImpostorTitle"), m_editorUtils.GetTextValue("RemoveImpostorText"), m_editorUtils.GetTextValue("Continue"), m_editorUtils.GetTextValue("Cancel")))
                        {
                            removeImpostorIndex = currentIndex;
                        }
                    }

                    GUI.enabled = originalGUIState;

                    EditorGUILayout.EndHorizontal();
                    if (terrainScene.RegularReferences.Count > 0)
                    {
                        EditorGUI.indentLevel++;
                        terrainScene.m_isFoldedOut = m_editorUtils.Foldout(terrainScene.m_isFoldedOut, "ShowTerrainReferences");
                        if (terrainScene.m_isFoldedOut)
                        {

                            foreach (GameObject go in terrainScene.RegularReferences)
                            {
                                EditorGUILayout.BeginHorizontal();
                                GUILayout.Space(20);
                                m_editorUtils.Label(new GUIContent(go.name, m_editorUtils.GetTextValue("TerrainReferenceToolTip")));
                                if (m_editorUtils.Button("TerrainReferenceSelect", GUILayout.Width(buttonWidth1)))
                                {
                                    Selection.activeObject = go;
                                    SceneView.lastActiveSceneView.FrameSelected();
                                }
                                if (m_editorUtils.Button("TerrainReferenceRemove", GUILayout.Width(buttonWidth2)))
                                {
                                    terrainScene.RemoveRegularReference(go);
                                }
                                GUILayout.Space(100);
                                EditorGUILayout.EndHorizontal();
                            }

                        }
                        EditorGUI.indentLevel--;
                    }
                    if (terrainScene.ImpostorReferences.Count > 0)
                    {
                        EditorGUI.indentLevel++;
                        terrainScene.m_isFoldedOut = m_editorUtils.Foldout(terrainScene.m_isFoldedOut, "ShowImpostorReferences");
                        if (terrainScene.m_isFoldedOut)
                        {

                            foreach (GameObject go in terrainScene.ImpostorReferences)
                            {
                                EditorGUILayout.BeginHorizontal();
                                GUILayout.Space(20);
                                m_editorUtils.Label(new GUIContent(go.name, m_editorUtils.GetTextValue("TerrainReferenceToolTip")));
                                if (m_editorUtils.Button("TerrainReferenceSelect", GUILayout.Width(buttonWidth1)))
                                {
                                    Selection.activeObject = go;
                                    SceneView.lastActiveSceneView.FrameSelected();
                                }
                                if (m_editorUtils.Button("TerrainReferenceRemove", GUILayout.Width(buttonWidth2)))
                                {
                                    terrainScene.RemoveImpostorReference(go);
                                }
                                GUILayout.Space(100);
                                EditorGUILayout.EndHorizontal();
                            }

                        }
                        EditorGUI.indentLevel--;
                    }
                }
                GUILayout.EndVertical();
                GUILayout.Space(5f);
                currentIndex++;
            }

            if (removeIndex != -99)
            {
                m_terrainLoaderManager.TerrainSceneStorage.m_terrainScenes[removeIndex].RemoveAllReferences(true);
                AssetDatabase.DeleteAsset(m_terrainLoaderManager.TerrainSceneStorage.m_terrainScenes[removeIndex].m_scenePath);
                m_terrainLoaderManager.TerrainSceneStorage.m_terrainScenes.RemoveAt(removeIndex);
                m_terrainLoaderManager.SaveStorageData();
            }

            if (removeImpostorIndex != -99)
            {
                m_terrainLoaderManager.TerrainSceneStorage.m_terrainScenes[removeImpostorIndex].RemoveAllImpostorReferences(true);
                AssetDatabase.DeleteAsset(m_terrainLoaderManager.TerrainSceneStorage.m_terrainScenes[removeImpostorIndex].m_impostorScenePath);
                m_terrainLoaderManager.TerrainSceneStorage.m_terrainScenes[removeImpostorIndex].m_impostorScenePath = "";
                m_terrainLoaderManager.SaveStorageData();
            }



        }

        /// <summary>
        /// Checks if the scene view loading is currently set to center on world origin, if not, prompts the user to switch.
        /// Used when loading in and out single terrains which would clash with the scene view camera loading mode.
        /// </summary>
        /// <returns>True if the scene was already on World Origin mode, or if the switch was performed.</returns>
        private bool ResetToWorldOriginLoading()
        {
            if (m_terrainLoaderManager.CenterSceneViewLoadingOn != CenterSceneViewLoadingOn.WorldOrigin)
            {
                if (EditorUtility.DisplayDialog(m_editorUtils.GetTextValue("ManualLoadPopUpTitle"), m_editorUtils.GetTextValue("ManualLoadPopUpText"), m_editorUtils.GetTextValue("Continue"), m_editorUtils.GetTextValue("Cancel")))
                {
                    m_terrainLoaderManager.LockSceneViewLoading();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        private void DrawLoaders(bool helpEnabled)
        {
            bool originalGUIState = GUI.enabled;
            EditorGUIUtility.labelWidth = 20;
#if GAIA_2023_PRO
            foreach (TerrainLoader terrainLoader in m_terrainLoaders)
            {
                if (terrainLoader != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(terrainLoader.name);
                    terrainLoader.LoadMode = (LoadMode)EditorGUILayout.EnumPopup(terrainLoader.LoadMode);
                    if (m_editorUtils.Button("SelectLoader", GUILayout.MaxWidth(100)))
                    {
                        Selection.activeGameObject = terrainLoader.gameObject;
                        EditorGUIUtility.PingObject(Selection.activeObject);

                        //Try to find out which kind of Gaia Tool that is, and open / highlight the terrain loading settings where appropiate
                        Stamper stamper = terrainLoader.gameObject.GetComponent<Stamper>();
                        if (stamper != null)
                        {
                            stamper.HighlightLoadingSettings();
                        }

                        BiomeController biomeController = terrainLoader.gameObject.GetComponent<BiomeController>();
                        if (biomeController != null)
                        {
                            biomeController.HighlightLoadingSettings();
                        }

                        Spawner spawner = terrainLoader.gameObject.GetComponent<Spawner>();
                        if (spawner != null)
                        {
                            spawner.HighlightLoadingSettings();
                        }

                        MaskMapExport maskMapExport = terrainLoader.gameObject.GetComponent<MaskMapExport>();
                        if (maskMapExport != null)
                        {
                            maskMapExport.HighlightLoadingSettings();
                        }

                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
#endif
            EditorGUIUtility.labelWidth = 0;
        }
    }
}
