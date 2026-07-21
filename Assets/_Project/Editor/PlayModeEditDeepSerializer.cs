using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Deep SerializedProperty export/import used by Play Mode Saver absolute capture.
    /// </summary>
    internal static class PlayModeEditDeepSerializer
    {
        internal static PropertySnapshot[] CaptureAllProperties(
            SerializedObject serializedObject,
            Component sourceComponent = null)
        {
            List<PropertySnapshot> properties = new List<PropertySnapshot>();
            SerializedProperty iterator = serializedObject.GetIterator();

            while (iterator.Next(true))
            {
                if (iterator.propertyPath == "m_Script")
                    continue;

                if (sourceComponent != null && ShouldSkipPlayModeProperty(sourceComponent, iterator))
                    continue;

                if (!TryExportProperty(iterator, out string value))
                    continue;

                properties.Add(new PropertySnapshot
                {
                    propertyPath = iterator.propertyPath,
                    value = value
                });
            }

            return properties.Count > 0 ? properties.ToArray() : Array.Empty<PropertySnapshot>();
        }

        internal static bool ApplyAllProperties(
            SerializedObject serializedObject,
            PropertySnapshot[] properties,
            out bool changed)
        {
            changed = false;
            if (properties == null)
                return false;

            for (int i = 0; i < properties.Length; i++)
            {
                PropertySnapshot propertySnapshot = properties[i];
                if (propertySnapshot == null || string.IsNullOrEmpty(propertySnapshot.propertyPath))
                    continue;

                SerializedProperty property = serializedObject.FindProperty(propertySnapshot.propertyPath);
                if (property == null)
                    continue;

                if (TryImportProperty(property, propertySnapshot.value))
                    changed = true;
            }

            return changed;
        }

        private static bool ShouldSkipPlayModeProperty(Component component, SerializedProperty property)
        {
            if (component is not Animator || property.propertyPath != "m_Controller")
                return false;

            UnityEngine.Object reference = property.objectReferenceValue;
            if (reference == null)
                return true;

            return string.IsNullOrEmpty(AssetDatabase.GetAssetPath(reference));
        }

        internal static bool TryExportProperty(SerializedProperty property, out string value)
        {
            value = null;
            if (property == null)
                return false;

            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    value = property.intValue.ToString(CultureInfo.InvariantCulture);
                    return true;
                case SerializedPropertyType.Boolean:
                    value = property.boolValue ? "1" : "0";
                    return true;
                case SerializedPropertyType.Float:
                    value = property.floatValue.ToString(CultureInfo.InvariantCulture);
                    return true;
                case SerializedPropertyType.String:
                    value = property.stringValue ?? string.Empty;
                    return true;
                case SerializedPropertyType.Color:
                    Color color = property.colorValue;
                    value = $"{color.r},{color.g},{color.b},{color.a}";
                    return true;
                case SerializedPropertyType.Vector2:
                    Vector2 v2 = property.vector2Value;
                    value = $"{v2.x},{v2.y}";
                    return true;
                case SerializedPropertyType.Vector3:
                    Vector3 v3 = property.vector3Value;
                    value = $"{v3.x},{v3.y},{v3.z}";
                    return true;
                case SerializedPropertyType.Vector4:
                    Vector4 v4 = property.vector4Value;
                    value = $"{v4.x},{v4.y},{v4.z},{v4.w}";
                    return true;
                case SerializedPropertyType.Quaternion:
                    Quaternion q = property.quaternionValue;
                    value = $"{q.x},{q.y},{q.z},{q.w}";
                    return true;
                case SerializedPropertyType.Enum:
                    value = property.enumValueIndex.ToString(CultureInfo.InvariantCulture);
                    return true;
                case SerializedPropertyType.Vector2Int:
                    Vector2Int v2i = property.vector2IntValue;
                    value = $"{v2i.x},{v2i.y}";
                    return true;
                case SerializedPropertyType.Vector3Int:
                    Vector3Int v3i = property.vector3IntValue;
                    value = $"{v3i.x},{v3i.y},{v3i.z}";
                    return true;
                case SerializedPropertyType.Rect:
                    Rect rect = property.rectValue;
                    value = $"{rect.x},{rect.y},{rect.width},{rect.height}";
                    return true;
                case SerializedPropertyType.ObjectReference:
                {
                    UnityEngine.Object reference = property.objectReferenceValue;
                    if (reference == null)
                    {
                        value = string.Empty;
                        return true;
                    }

                    string assetPath = AssetDatabase.GetAssetPath(reference);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        value = "asset:" + assetPath + "|" + reference.GetType().AssemblyQualifiedName;
                        return true;
                    }

                    return false;
                }
                case SerializedPropertyType.Bounds:
                    Bounds bounds = property.boundsValue;
                    value = $"{bounds.center.x},{bounds.center.y},{bounds.center.z}|{bounds.size.x},{bounds.size.y},{bounds.size.z}";
                    return true;
                case SerializedPropertyType.AnimationCurve:
                    return TryExportAnimationCurve(property, out value);
                case SerializedPropertyType.Generic:
                    return TryExportGenericProperty(property, out value);
                default:
                    return false;
            }
        }

        internal static bool TryImportProperty(SerializedProperty property, string value)
        {
            if (property == null || value == null)
                return false;

            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
                        return false;
                    if (property.intValue == intValue)
                        return false;
                    property.intValue = intValue;
                    return true;
                case SerializedPropertyType.Boolean:
                    bool boolValue = value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
                    if (property.boolValue == boolValue)
                        return false;
                    property.boolValue = boolValue;
                    return true;
                case SerializedPropertyType.Float:
                    if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
                        return false;
                    if (Mathf.Approximately(property.floatValue, floatValue))
                        return false;
                    property.floatValue = floatValue;
                    return true;
                case SerializedPropertyType.String:
                    if (property.stringValue == value)
                        return false;
                    property.stringValue = value;
                    return true;
                case SerializedPropertyType.Color:
                    if (!TryParseFloatCsv(value, 4, out float[] colorParts))
                        return false;
                    Color color = new Color(colorParts[0], colorParts[1], colorParts[2], colorParts[3]);
                    if (property.colorValue == color)
                        return false;
                    property.colorValue = color;
                    return true;
                case SerializedPropertyType.Vector2:
                    if (!TryParseFloatCsv(value, 2, out float[] v2Parts))
                        return false;
                    Vector2 vector2 = new Vector2(v2Parts[0], v2Parts[1]);
                    if (property.vector2Value == vector2)
                        return false;
                    property.vector2Value = vector2;
                    return true;
                case SerializedPropertyType.Vector3:
                    if (!TryParseFloatCsv(value, 3, out float[] v3Parts))
                        return false;
                    Vector3 vector3 = new Vector3(v3Parts[0], v3Parts[1], v3Parts[2]);
                    if (property.vector3Value == vector3)
                        return false;
                    property.vector3Value = vector3;
                    return true;
                case SerializedPropertyType.Vector4:
                    if (!TryParseFloatCsv(value, 4, out float[] v4Parts))
                        return false;
                    Vector4 vector4 = new Vector4(v4Parts[0], v4Parts[1], v4Parts[2], v4Parts[3]);
                    if (property.vector4Value == vector4)
                        return false;
                    property.vector4Value = vector4;
                    return true;
                case SerializedPropertyType.Quaternion:
                    if (!TryParseFloatCsv(value, 4, out float[] qParts))
                        return false;
                    Quaternion quaternion = new Quaternion(qParts[0], qParts[1], qParts[2], qParts[3]);
                    if (property.quaternionValue == quaternion)
                        return false;
                    property.quaternionValue = quaternion;
                    return true;
                case SerializedPropertyType.Enum:
                    if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int enumIndex))
                        return false;
                    if (property.enumValueIndex == enumIndex)
                        return false;
                    property.enumValueIndex = enumIndex;
                    return true;
                case SerializedPropertyType.Vector2Int:
                    if (!TryParseIntCsv(value, 2, out int[] v2iParts))
                        return false;
                    Vector2Int vector2Int = new Vector2Int(v2iParts[0], v2iParts[1]);
                    if (property.vector2IntValue == vector2Int)
                        return false;
                    property.vector2IntValue = vector2Int;
                    return true;
                case SerializedPropertyType.Vector3Int:
                    if (!TryParseIntCsv(value, 3, out int[] v3iParts))
                        return false;
                    Vector3Int vector3Int = new Vector3Int(v3iParts[0], v3iParts[1], v3iParts[2]);
                    if (property.vector3IntValue == vector3Int)
                        return false;
                    property.vector3IntValue = vector3Int;
                    return true;
                case SerializedPropertyType.Rect:
                    if (!TryParseFloatCsv(value, 4, out float[] rectParts))
                        return false;
                    Rect rect = new Rect(rectParts[0], rectParts[1], rectParts[2], rectParts[3]);
                    if (property.rectValue == rect)
                        return false;
                    property.rectValue = rect;
                    return true;
                case SerializedPropertyType.ObjectReference:
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        if (property.objectReferenceValue == null)
                            return false;
                        property.objectReferenceValue = null;
                        return true;
                    }

                    if (!value.StartsWith("asset:", StringComparison.Ordinal))
                        return false;

                    string payload = value.Substring("asset:".Length);
                    int typeSeparator = payload.LastIndexOf('|');
                    if (typeSeparator <= 0)
                        return false;

                    string assetPath = payload.Substring(0, typeSeparator);
                    string typeName = payload.Substring(typeSeparator + 1);
                    Type referenceType = Type.GetType(typeName) ?? typeof(UnityEngine.Object);
                    UnityEngine.Object loaded = AssetDatabase.LoadAssetAtPath(assetPath, referenceType);
                    if (loaded == null || property.objectReferenceValue == loaded)
                        return false;

                    property.objectReferenceValue = loaded;
                    return true;
                }
                case SerializedPropertyType.Bounds:
                {
                    string[] boundsGroups = value.Split('|');
                    if (boundsGroups.Length != 2
                        || !TryParseFloatCsv(boundsGroups[0], 3, out float[] centerParts)
                        || !TryParseFloatCsv(boundsGroups[1], 3, out float[] sizeParts))
                        return false;

                    Bounds bounds = new Bounds(
                        new Vector3(centerParts[0], centerParts[1], centerParts[2]),
                        new Vector3(sizeParts[0], sizeParts[1], sizeParts[2]));
                    if (property.boundsValue.center == bounds.center && property.boundsValue.size == bounds.size)
                        return false;
                    property.boundsValue = bounds;
                    return true;
                }
                case SerializedPropertyType.AnimationCurve:
                    return TryImportAnimationCurve(property, value);
                case SerializedPropertyType.Generic:
                    return TryImportGenericProperty(property, value);
                default:
                    return false;
            }
        }

        private static bool TryExportGenericProperty(SerializedProperty property, out string value)
        {
            value = null;
            if (property.isArray)
            {
                StringBuilder builder = new StringBuilder();
                builder.Append("array:").Append(property.arraySize);
                for (int i = 0; i < property.arraySize; i++)
                {
                    SerializedProperty element = property.GetArrayElementAtIndex(i);
                    if (!TryExportProperty(element, out string elementValue))
                        continue;

                    builder.Append('|').Append(i).Append('=').Append(EscapeSnapshotValue(elementValue));
                }

                value = builder.ToString();
                return true;
            }

            if (!property.hasVisibleChildren)
                return false;

            StringBuilder structBuilder = new StringBuilder("struct:");
            SerializedProperty copy = property.Copy();
            SerializedProperty end = copy.GetEndProperty();
            bool enterChildren = true;
            bool wroteAny = false;

            while (copy.Next(enterChildren) && !SerializedProperty.EqualContents(copy, end))
            {
                enterChildren = false;
                if (!TryExportProperty(copy, out string childValue))
                    continue;

                structBuilder.Append('|').Append(copy.propertyPath).Append('=').Append(EscapeSnapshotValue(childValue));
                wroteAny = true;
            }

            if (!wroteAny)
                return false;

            value = structBuilder.ToString();
            return true;
        }

        private static bool TryImportGenericProperty(SerializedProperty property, string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            if (value.StartsWith("array:", StringComparison.Ordinal))
            {
                string payload = value.Substring("array:".Length);
                string[] segments = payload.Split('|');
                if (segments.Length == 0 || !int.TryParse(segments[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int arraySize))
                    return false;

                bool changed = property.arraySize != arraySize;
                property.arraySize = arraySize;

                for (int i = 1; i < segments.Length; i++)
                {
                    int equalsIndex = segments[i].IndexOf('=');
                    if (equalsIndex <= 0)
                        continue;

                    if (!int.TryParse(segments[i].Substring(0, equalsIndex), NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
                        continue;

                    if (index < 0 || index >= property.arraySize)
                        continue;

                    string elementValue = UnescapeSnapshotValue(segments[i].Substring(equalsIndex + 1));
                    if (TryImportProperty(property.GetArrayElementAtIndex(index), elementValue))
                        changed = true;
                }

                return changed;
            }

            if (!value.StartsWith("struct:", StringComparison.Ordinal))
                return false;

            string structPayload = value.Substring("struct:".Length);
            string[] structSegments = structPayload.Split('|');
            bool structChanged = false;

            for (int i = 0; i < structSegments.Length; i++)
            {
                int equalsIndex = structSegments[i].IndexOf('=');
                if (equalsIndex <= 0)
                    continue;

                string childPath = structSegments[i].Substring(0, equalsIndex);
                string childValue = UnescapeSnapshotValue(structSegments[i].Substring(equalsIndex + 1));
                SerializedProperty childProperty = property.FindPropertyRelative(childPath);
                if (childProperty == null)
                    childProperty = property.serializedObject.FindProperty(childPath);

                if (childProperty != null && TryImportProperty(childProperty, childValue))
                    structChanged = true;
            }

            return structChanged;
        }

        private static bool TryExportAnimationCurve(SerializedProperty property, out string value)
        {
            value = null;
            AnimationCurve curve = property.animationCurveValue;
            if (curve == null)
            {
                value = "curve:null";
                return true;
            }

            StringBuilder builder = new StringBuilder();
            builder.Append("curve:").Append((int)curve.preWrapMode).Append(',').Append((int)curve.postWrapMode).Append(',').Append(curve.length);
            for (int i = 0; i < curve.length; i++)
            {
                Keyframe key = curve.keys[i];
                builder.Append('|').Append(key.time.ToString(CultureInfo.InvariantCulture))
                    .Append(',').Append(key.value.ToString(CultureInfo.InvariantCulture))
                    .Append(',').Append(key.inTangent.ToString(CultureInfo.InvariantCulture))
                    .Append(',').Append(key.outTangent.ToString(CultureInfo.InvariantCulture))
                    .Append(',').Append(key.inWeight.ToString(CultureInfo.InvariantCulture))
                    .Append(',').Append(key.outWeight.ToString(CultureInfo.InvariantCulture))
                    .Append(',').Append((int)key.weightedMode);
            }

            value = builder.ToString();
            return true;
        }

        private static bool TryImportAnimationCurve(SerializedProperty property, string value)
        {
            if (string.IsNullOrEmpty(value) || !value.StartsWith("curve:", StringComparison.Ordinal))
                return false;

            string payload = value.Substring("curve:".Length);
            if (payload == "null")
            {
                if (property.animationCurveValue == null)
                    return false;
                property.animationCurveValue = null;
                return true;
            }

            string[] segments = payload.Split('|');
            if (segments.Length == 0)
                return false;

            string[] header = segments[0].Split(',');
            if (header.Length < 3
                || !int.TryParse(header[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int preWrapMode)
                || !int.TryParse(header[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int postWrapMode)
                || !int.TryParse(header[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int keyCount))
                return false;

            Keyframe[] keys = new Keyframe[keyCount];
            for (int i = 0; i < keyCount; i++)
            {
                if (i + 1 >= segments.Length)
                    return false;

                string[] keyParts = segments[i + 1].Split(',');
                if (keyParts.Length < 7)
                    return false;

                keys[i] = new Keyframe(
                    float.Parse(keyParts[0], CultureInfo.InvariantCulture),
                    float.Parse(keyParts[1], CultureInfo.InvariantCulture),
                    float.Parse(keyParts[2], CultureInfo.InvariantCulture),
                    float.Parse(keyParts[3], CultureInfo.InvariantCulture),
                    float.Parse(keyParts[4], CultureInfo.InvariantCulture),
                    float.Parse(keyParts[5], CultureInfo.InvariantCulture))
                {
                    weightedMode = int.TryParse(keyParts[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out int mode)
                        ? (WeightedMode)mode
                        : WeightedMode.None
                };
            }

            AnimationCurve curve = new AnimationCurve(keys)
            {
                preWrapMode = (WrapMode)preWrapMode,
                postWrapMode = (WrapMode)postWrapMode
            };

            property.animationCurveValue = curve;
            return true;
        }

        private static bool TryParseFloatCsv(string value, int expectedCount, out float[] parts)
        {
            parts = null;
            if (string.IsNullOrEmpty(value))
                return false;

            string[] tokens = value.Split(',');
            if (tokens.Length != expectedCount)
                return false;

            parts = new float[expectedCount];
            for (int i = 0; i < expectedCount; i++)
            {
                if (!float.TryParse(tokens[i], NumberStyles.Float, CultureInfo.InvariantCulture, out parts[i]))
                    return false;
            }

            return true;
        }

        private static bool TryParseIntCsv(string value, int expectedCount, out int[] parts)
        {
            parts = null;
            if (string.IsNullOrEmpty(value))
                return false;

            string[] tokens = value.Split(',');
            if (tokens.Length != expectedCount)
                return false;

            parts = new int[expectedCount];
            for (int i = 0; i < expectedCount; i++)
            {
                if (!int.TryParse(tokens[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out parts[i]))
                    return false;
            }

            return true;
        }

        private const int SnapshotEscapeMaxLength = 4096;

        private static string EscapeSnapshotValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            if (value.Length > SnapshotEscapeMaxLength)
                return "b64:" + Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

            StringBuilder builder = new StringBuilder(value.Length + 8);
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                switch (character)
                {
                    case '%':
                        builder.Append("%25");
                        break;
                    case '|':
                        builder.Append("%7C");
                        break;
                    case '=':
                        builder.Append("%3D");
                        break;
                    default:
                        builder.Append(character);
                        break;
                }
            }

            return builder.ToString();
        }

        private static string UnescapeSnapshotValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            if (value.StartsWith("b64:", StringComparison.Ordinal))
                return Encoding.UTF8.GetString(Convert.FromBase64String(value.Substring(4)));

            StringBuilder builder = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (character != '%' || i + 2 >= value.Length)
                {
                    builder.Append(character);
                    continue;
                }

                if (!int.TryParse(
                        value.Substring(i + 1, 2),
                        NumberStyles.HexNumber,
                        CultureInfo.InvariantCulture,
                        out int codePoint))
                {
                    builder.Append(character);
                    continue;
                }

                builder.Append((char)codePoint);
                i += 2;
            }

            return builder.ToString();
        }

        [Serializable]
        internal class PropertySnapshot
        {
            public string propertyPath;
            public string value;
        }
    }
}
