using System.Linq;
using UnityEditor;
using UnityEngine;

public class BlinkSkinnedMeshTransfer : EditorWindow
{
    private ScriptableObject scriptableObj;
    private SerializedObject serialObj;

    public SkinnedMeshRenderer[] skinnedMeshRenderersList;
    public Transform newArmature;
    public Transform newParent;

    private Vector2 viewScrollPosition;

    [MenuItem("BLINK/Skinned Mesh Transfer")]
    private static void OpenWindow()
    {
        var window = (BlinkSkinnedMeshTransfer)GetWindow(typeof(BlinkSkinnedMeshTransfer), false, "Skinned Mesh Transfer");
        window.minSize = new Vector2(400, 500);
        GUI.contentColor = Color.white;
        window.Show();
    }

    private void OnEnable()
    {
        EnsureSerializedObject();
    }

    private void EnsureSerializedObject()
    {
        if (scriptableObj == null)
            scriptableObj = this;
        if (serialObj == null || serialObj.targetObject == null)
            serialObj = new SerializedObject(scriptableObj);
    }

    private void OnGUI()
    {
        EnsureSerializedObject();
        if (serialObj == null)
        {
            EditorGUILayout.HelpBox("SerializedObject failed to initialize. Close and reopen this window.", MessageType.Error);
            return;
        }

        DrawMain();
    }

    private void DrawMain()
    {
        // Keep layout Begin/End balanced even if a dialog or exception fires mid-draw.
        viewScrollPosition = EditorGUILayout.BeginScrollView(viewScrollPosition, false, false);
        try
        {
            serialObj.Update();
            var serialProp = serialObj.FindProperty("skinnedMeshRenderersList");
            if (serialProp != null)
                EditorGUILayout.PropertyField(serialProp, true);

            GUILayout.Space(7);
            newArmature = (Transform)EditorGUILayout.ObjectField("New Armature (Hips)", newArmature, typeof(Transform), true);
            GUILayout.Space(7);
            newParent = (Transform)EditorGUILayout.ObjectField("New Parent", newParent, typeof(Transform), true);
            GUILayout.Space(15);

            if (GUILayout.Button("TRANSFER", GUILayout.MinWidth(150), GUILayout.MinHeight(30), GUILayout.ExpandWidth(true)))
            {
                serialObj.ApplyModifiedProperties();
                // Defer out of OnGUI so DisplayDialog cannot break GUILayout Begin/End pairing.
                EditorApplication.delayCall += TransferSkinnedMeshes;
            }

            serialObj.ApplyModifiedProperties();
            GUILayout.Space(20);
        }
        finally
        {
            EditorGUILayout.EndScrollView();
        }
    }

    private void TransferSkinnedMeshes()
    {
        if (skinnedMeshRenderersList == null || skinnedMeshRenderersList.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Skinned Mesh Transfer",
                "Skinned Mesh Renderers list is empty.\n\n" +
                "1. Set Size to 1+.\n" +
                "2. Drag the Sulfur Hound SkinnedMeshRenderer(s) into the list.\n" +
                "3. New Armature = destination Hips/Pelvis (e.g. Wolf Pelvis).\n" +
                "4. New Parent = object to parent the mesh under.\n\n" +
                "Note: this tool only remaps bones by matching names. " +
                "Wolf Lite and Sulfur Hound use different bone names, so this will not correctly retarget Malbers animations onto Sulfur.",
                "OK");
            return;
        }

        if (newArmature == null || newParent == null)
        {
            EditorUtility.DisplayDialog(
                "Skinned Mesh Transfer",
                "Assign both New Armature (Hips/Pelvis) and New Parent before transferring.",
                "OK");
            return;
        }

        foreach (var t in skinnedMeshRenderersList)
        {
            if (t == null)
            {
                Debug.LogWarning("[BlinkSkinnedMeshTransfer] Skipping null SkinnedMeshRenderer entry.");
                continue;
            }

            if (t.rootBone == null || t.bones == null)
            {
                Debug.LogWarning($"[BlinkSkinnedMeshTransfer] '{t.name}' has no rootBone/bones — skip.");
                continue;
            }

            string cachedRootBoneName = t.rootBone.name;
            var newBones = new Transform[t.bones.Length];
            int matched = 0;
            Transform[] armatureBones = newArmature.GetComponentsInChildren<Transform>(true);
            for (var x = 0; x < t.bones.Length; x++)
            {
                if (t.bones[x] == null)
                    continue;

                string boneName = t.bones[x].name;
                for (int i = 0; i < armatureBones.Length; i++)
                {
                    if (armatureBones[i] != null && armatureBones[i].name == boneName)
                    {
                        newBones[x] = armatureBones[i];
                        matched++;
                        break;
                    }
                }
            }

            if (matched == 0)
            {
                EditorUtility.DisplayDialog(
                    "Skinned Mesh Transfer",
                    $"No bones matched by name for '{t.name}'.\n\n" +
                    "Source mesh bones and New Armature bones must share the same names. " +
                    "Meshy Sulfur and Malbers Wolf Lite do not — use Sulfur_Hound_V2 (own mesh + Meshy walk) " +
                    "or a DCC Skin Wrap / Humanoid retarget instead.",
                    "OK");
                return;
            }

            Transform matchingRootBone = GetRootBoneByName(newArmature, cachedRootBoneName);
            t.rootBone = matchingRootBone != null ? matchingRootBone : newArmature;
            t.bones = newBones;
            Transform meshXf = t.transform;
            meshXf.SetParent(newParent);
            meshXf.localPosition = Vector3.zero;

            Debug.Log($"[BlinkSkinnedMeshTransfer] Remapped '{t.name}': {matched}/{t.bones.Length} bones matched by name.");
        }
    }

    static Transform GetRootBoneByName(Transform parentTransform, string name)
    {
        if (parentTransform == null || string.IsNullOrEmpty(name))
            return null;

        return parentTransform.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(transformChild => transformChild != null && transformChild.name == name);
    }
}
