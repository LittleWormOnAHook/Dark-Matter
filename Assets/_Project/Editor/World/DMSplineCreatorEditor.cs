#if UNITY_EDITOR
using MalbersAnimations.PathCreation;
using Project.World;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    [CustomEditor(typeof(DMSplineCreator))]
    public class DMSplineCreatorEditor : Editor
    {
        private enum SceneEditMode
        {
            BezierPath = 0,
            AnchorObjects = 1
        }

        private DMSplineCreator creator;
        private PathCreator pathCreator;
        private Editor pathCreatorEditor;
        private SceneEditMode sceneMode = SceneEditMode.AnchorObjects;
        private int selectedAnchor = -1;

        private SerializedProperty modeProp;
        private SerializedProperty anchorPrefabProp;
        private SerializedProperty anchorPointCountProp;
        private SerializedProperty anchorSpacingProp;
        private SerializedProperty prefabUpAxisProp;
        private SerializedProperty prefabForwardAxisProp;
        private SerializedProperty alignSurfaceProp;
        private SerializedProperty alignTangentProp;
        private SerializedProperty lineTwistProp;
        private SerializedProperty lineRoutingProp;
        private SerializedProperty wireConnectEndProp;
        private SerializedProperty lineAttachOffsetProp;
        private SerializedProperty segmentsPerSpanProp;
        private SerializedProperty lockAnchorsProp;
        private SerializedProperty enableWiggleProp;
        private SerializedProperty wiggleEditProp;
        private SerializedProperty wiggleAmpDirectionalProp;
        private SerializedProperty autoWireAttachProp;
        private SerializedProperty wiggleFreqProp;
        private SerializedProperty wiggleSpeedProp;
        private SerializedProperty lineMaterialProp;
        private SerializedProperty linePreviewColorProp;
        private SerializedProperty lineWidthProp;
        private SerializedProperty sagProp;
        private SerializedProperty autoRebuildProp;
        private SerializedProperty anchorSettingsProp;
        private SerializedProperty showEditPreviewProp;
        private SerializedProperty editPivotRadiusProp;
        private SerializedProperty editHookRadiusProp;

        private void OnEnable()
        {
            creator = (DMSplineCreator)target;
            creator.EnsurePathInitialized();
            pathCreator = creator.PathCreator;
            if (pathCreator != null)
                pathCreatorEditor = CreateEditor(pathCreator);

            modeProp = serializedObject.FindProperty("mode");
            anchorPrefabProp = serializedObject.FindProperty("anchorPrefab");
            anchorPointCountProp = serializedObject.FindProperty("anchorPointCount");
            anchorSpacingProp = serializedObject.FindProperty("anchorSpacing");
            prefabUpAxisProp = serializedObject.FindProperty("prefabUpAxis");
            prefabForwardAxisProp = serializedObject.FindProperty("prefabForwardAxis");
            alignSurfaceProp = serializedObject.FindProperty("alignPrefabToSurfaceNormal");
            alignTangentProp = serializedObject.FindProperty("alignPrefabFacingPathTangent");
            lineTwistProp = serializedObject.FindProperty("lineCrossSectionTwistDegrees");
            lineRoutingProp = serializedObject.FindProperty("lineRouting");
            wireConnectEndProp = serializedObject.FindProperty("wireConnectEndToStart");
            lineAttachOffsetProp = serializedObject.FindProperty("lineAttachOffsetAlongUp");
            segmentsPerSpanProp = serializedObject.FindProperty("segmentsPerAnchorSpan");
            lockAnchorsProp = serializedObject.FindProperty("lockObjectsToPathAnchors");
            enableWiggleProp = serializedObject.FindProperty("enableRopeWiggle");
            wiggleEditProp = serializedObject.FindProperty("ropeWiggleInEditMode");
            wiggleAmpDirectionalProp = serializedObject.FindProperty("ropeWiggleAmplitudeDirectional");
            autoWireAttachProp = serializedObject.FindProperty("autoWireAttachFromRendererTop");
            wiggleFreqProp = serializedObject.FindProperty("ropeWiggleFrequency");
            wiggleSpeedProp = serializedObject.FindProperty("ropeWiggleTravelSpeed");
            lineMaterialProp = serializedObject.FindProperty("lineMaterial");
            linePreviewColorProp = serializedObject.FindProperty("linePreviewColor");
            lineWidthProp = serializedObject.FindProperty("lineWidth");
            sagProp = serializedObject.FindProperty("sagMeters");
            autoRebuildProp = serializedObject.FindProperty("autoRebuildLineMesh");
            anchorSettingsProp = serializedObject.FindProperty("anchorSettings");
            showEditPreviewProp = serializedObject.FindProperty("showEditModeAnchorPreview");
            editPivotRadiusProp = serializedObject.FindProperty("editPreviewPivotSphereRadius");
            editHookRadiusProp = serializedObject.FindProperty("editPreviewWireHookSphereRadius");
        }

        private void OnDisable()
        {
            if (pathCreatorEditor != null)
                DestroyImmediate(pathCreatorEditor);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "1) Drop anchor prefab → set count → Apply.\n" +
                "2) Move stakes (Anchor objects mode) or Bézier handles — wire follows pinned attach points.\n" +
                "3) Put a child named WireAttach (or Spline Wire Attach Point) on the prefab for exact hookups.\n" +
                "Width / sag / wiggle never move pinned attach points.",
                MessageType.Info);

            EditorGUILayout.PropertyField(modeProp);
            DrawSetupSection();
            DrawLineSection();
            DrawWiggleSection();

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Path Creator (Bézier)", EditorStyles.boldLabel);
            if (pathCreatorEditor != null)
            {
                pathCreatorEditor.OnInspectorGUI();
                Tools.hidden = false;
            }

            DrawAnchorList();
            DrawActionButtons();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSetupSection()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Anchors", EditorStyles.boldLabel);
            DrawPrefabDrop();

            EditorGUILayout.PropertyField(prefabUpAxisProp, new GUIContent("Prefab up axis"));
            EditorGUILayout.PropertyField(prefabForwardAxisProp, new GUIContent("Prefab forward axis"));
            EditorGUILayout.PropertyField(alignSurfaceProp, new GUIContent("Align to ground normal"));
            EditorGUILayout.PropertyField(alignTangentProp, new GUIContent("Face path tangent"));
            EditorGUILayout.PropertyField(lockAnchorsProp, new GUIContent("Lock stakes ↔ path"));
            EditorGUILayout.PropertyField(autoRebuildProp, new GUIContent("Live rebuild wire"));
        }

        private void DrawLineSection()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Wire mesh (appearance only)", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(lineMaterialProp);
            EditorGUILayout.PropertyField(lineWidthProp);
            EditorGUILayout.PropertyField(sagProp);
            EditorGUILayout.PropertyField(segmentsPerSpanProp, new GUIContent("Segments per span"));
            EditorGUILayout.PropertyField(lineTwistProp, new GUIContent("Cross-section twist"));
            EditorGUILayout.PropertyField(linePreviewColorProp);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                RebuildWireAppearanceOnly();
            }

            EditorGUILayout.PropertyField(lineRoutingProp);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(wireConnectEndProp, new GUIContent("Connect last → first (closed loop)"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                Undo.RecordObject(creator, "Wire Loop");
                creator.SetWireConnectEndToStart(wireConnectEndProp.boolValue);
                EditorUtility.SetDirty(creator);
            }

            if (!wireConnectEndProp.boolValue)
            {
                EditorGUILayout.HelpBox(
                    "Open chain: wire runs only between consecutive anchors (5 anchors = 4 spans).",
                    MessageType.None);
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(lineAttachOffsetProp, new GUIContent("Wire attach height (along prefab up)"));
            if (GUILayout.Button("Apply Height", GUILayout.Width(96f), GUILayout.Height(18f)))
            {
                Undo.RecordObject(creator, "Apply Wire Attach Height");
                serializedObject.ApplyModifiedProperties();
                creator.ApplyDefaultAttachHeightToAllAnchors();
                EditorUtility.SetDirty(creator);
                SceneView.RepaintAll();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox(
                "Apply Height moves every wire hook up/down along Prefab Up Axis and rebuilds the cable. " +
                "Does not move stake objects — only the hook point the mesh uses.",
                MessageType.None);

            EditorGUILayout.PropertyField(autoWireAttachProp, new GUIContent("Use mesh top on first setup"));

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Scene preview (Edit Mode only)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(showEditPreviewProp, new GUIContent("Show anchor spheres"));
            EditorGUILayout.PropertyField(editPivotRadiusProp, new GUIContent("Pivot sphere size"));
            EditorGUILayout.PropertyField(editHookRadiusProp, new GUIContent("Wire hook sphere size"));
        }

        private void DrawWiggleSection()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Rope wiggle", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(enableWiggleProp);
            EditorGUILayout.PropertyField(wiggleEditProp, new GUIContent("Preview in Edit Mode"));
            EditorGUILayout.PropertyField(wiggleAmpDirectionalProp, new GUIContent("Amplitude (X=side Y=along Z=up)"));
            EditorGUILayout.PropertyField(wiggleFreqProp, new GUIContent("Frequency"));
            EditorGUILayout.PropertyField(wiggleSpeedProp, new GUIContent("Travel speed"));
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply Count + Prefab", GUILayout.Height(28f)))
            {
                Undo.RecordObject(creator, "Apply Anchors");
                serializedObject.ApplyModifiedProperties();
                creator.SetDesiredAnchorCount(anchorPointCountProp.intValue);
                creator.ApplyAnchorCountAndPrefab(true);
                EditorUtility.SetDirty(creator);
            }

            if (GUILayout.Button("Snap To Ground", GUILayout.Height(28f)))
            {
                Undo.RecordObject(creator, "Snap Anchors");
                creator.SnapAllAnchorsToSurface();
                creator.RebuildLineMeshAppearance();
                EditorUtility.SetDirty(creator);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Calibrate Wire Hooks (pin)", GUILayout.Height(24f)))
            {
                Undo.RecordObject(creator, "Calibrate Wire Attach");
                creator.CalibrateAllWireAttachOffsetsFromRenderers();
                creator.RebuildLineMeshAppearance();
                EditorUtility.SetDirty(creator);
            }

            if (GUILayout.Button("Rebuild Wire", GUILayout.Height(24f)))
            {
                Undo.RecordObject(creator, "Rebuild Wire");
                creator.RebuildLineMeshAppearance();
                EditorUtility.SetDirty(creator);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void RebuildWireAppearanceOnly()
        {
            if (creator.Mode != DMSplineCreator.CreatorMode.ElectricalLine)
                return;

            creator.RebuildLineMeshAppearance();
            EditorUtility.SetDirty(creator);
        }

        private void DrawPrefabDrop()
        {
            Rect drop = GUILayoutUtility.GetRect(0f, 48f, GUILayout.ExpandWidth(true));
            GUI.Box(drop, GUIContent.none, EditorStyles.helpBox);

            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!drop.Contains(evt.mousePosition))
                        break;
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        foreach (Object obj in DragAndDrop.objectReferences)
                        {
                            if (obj is GameObject go)
                            {
                                Undo.RecordObject(creator, "Assign Anchor Prefab");
                                creator.SetAnchorPrefab(go);
                                creator.ApplyAnchorCountAndPrefab(true);
                                EditorUtility.SetDirty(creator);
                                break;
                            }
                        }
                    }

                    evt.Use();
                    break;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(anchorPrefabProp, GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(anchorPointCountProp, new GUIContent("Count"));
            EditorGUILayout.PropertyField(anchorSpacingProp, new GUIContent("Spacing"));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAnchorList()
        {
            int count = creator.PlacedAnchorCount;
            if (count <= 0)
                return;

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField($"Placed anchors ({count})", EditorStyles.boldLabel);
            EnsureAnchorSettingsArraySize(count);

            for (int i = 0; i < count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button($"{i}", GUILayout.Width(28f)))
                {
                    Transform anchor = creator.GetAnchorTransform(i);
                    if (anchor != null)
                        Selection.activeTransform = anchor;
                }

                SerializedProperty entry = anchorSettingsProp.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("gizmoColor"), GUIContent.none, GUILayout.Width(64f));
                EditorGUILayout.LabelField(
                    entry.FindPropertyRelative("wireAttachPinned").boolValue ? "Pinned" : "Unpinned",
                    EditorStyles.miniLabel,
                    GUILayout.Width(52f));
                EditorGUILayout.EndHorizontal();
            }
        }

        private void EnsureAnchorSettingsArraySize(int count)
        {
            while (anchorSettingsProp.arraySize < count)
                anchorSettingsProp.InsertArrayElementAtIndex(anchorSettingsProp.arraySize);
            while (anchorSettingsProp.arraySize > count)
                anchorSettingsProp.DeleteArrayElementAtIndex(anchorSettingsProp.arraySize - 1);
        }

        private void OnSceneGUI()
        {
            if (creator == null || pathCreator == null)
                return;

            Handles.BeginGUI();
            sceneMode = (SceneEditMode)GUILayout.Toolbar((int)sceneMode, new[] { "Anchor objects", "Bézier path" }, GUILayout.Width(260f));
            if (sceneMode == SceneEditMode.BezierPath)
                GUILayout.Label("Shift-click path to add points (Path Creator).", EditorStyles.miniLabel);
            else
                GUILayout.Label("Move stakes; yellow handle = wire hook (pinned).", EditorStyles.miniLabel);
            Handles.EndGUI();

            DrawWireAttachHandles();

            if (sceneMode == SceneEditMode.BezierPath)
                return;

            DrawAnchorObjectHandles();
        }

        private void DrawWireAttachHandles()
        {
            if (!creator.ShowEditModeAnchorPreview)
                return;

            int count = creator.PlacedAnchorCount;
            for (int i = 0; i < count; i++)
            {
                Transform anchor = creator.GetAnchorTransform(i);
                if (anchor == null)
                    continue;

                Vector3 attach = creator.GetLineAttachWorldPosition(i);

                if (creator.HasPrefabWireAttachPoint(i))
                    continue;

                EditorGUI.BeginChangeCheck();
                Vector3 movedAttach = Handles.FreeMoveHandle(
                    attach,
                    creator.EditPreviewWireHookSphereRadius * 2f,
                    Vector3.zero,
                    Handles.SphereHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(creator, "Move Wire Hook");
                    creator.SetPinnedWireAttachWorldPosition(i, movedAttach);
                    creator.SyncAnchorPathBinding();
                    creator.RebuildLineMeshAppearance();
                    EditorUtility.SetDirty(creator);
                }
            }
        }

        private void DrawAnchorObjectHandles()
        {
            int count = creator.PlacedAnchorCount;
            for (int i = 0; i < count; i++)
            {
                Transform anchor = creator.GetAnchorTransform(i);
                if (anchor == null)
                    continue;

                if (Handles.Button(anchor.position, Quaternion.identity, 0.35f, 0.45f, Handles.SphereHandleCap))
                {
                    selectedAnchor = i;
                    Selection.activeTransform = anchor;
                }

                if (selectedAnchor == i || Selection.activeTransform == anchor)
                {
                    EditorGUI.BeginChangeCheck();
                    Vector3 pos = anchor.position;
                    Quaternion rot = anchor.rotation;
                    Vector3 scale = anchor.localScale;

                    pos = Handles.PositionHandle(pos, rot);
                    rot = Handles.RotationHandle(rot, pos);
                    scale = Handles.ScaleHandle(scale, pos, rot, HandleUtility.GetHandleSize(pos));

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(anchor, "Transform Spline Anchor");
                        anchor.SetPositionAndRotation(pos, rot);
                        anchor.localScale = scale;
                        creator.SyncAnchorPathBinding();
                        creator.TryRebuildIfAttachPointsMoved();
                    }
                }
            }
        }
    }
}
#endif
