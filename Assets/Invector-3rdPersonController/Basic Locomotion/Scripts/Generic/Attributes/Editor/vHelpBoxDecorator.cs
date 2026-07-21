using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof (vHelpBoxAttribute))]
public class vHelpBoxDecorator : DecoratorDrawer
{
    private GUIStyle _style;

    private GUIStyle Style
    {
        get
        {
            if (_style == null)
                _style = new GUIStyle(EditorStyles.helpBox);
            return _style;
        }
    }

    public override void OnGUI(Rect position)
    {
        var helpbox = attribute as vHelpBoxAttribute;
        if (helpbox == null)
            return;

        GUIContent content = new GUIContent(helpbox.text);

        switch (helpbox.messageType)
        {
            case vHelpBoxAttribute.MessageType.Info:
                content = EditorGUIUtility.IconContent("console.infoicon", helpbox.text);
                break;
            case vHelpBoxAttribute.MessageType.Warning:
                content = EditorGUIUtility.IconContent("console.warnicon", helpbox.text);
                break;
        }

        content.text = helpbox.text;
        Style.richText = true;
        GUI.Box(position, content, Style);
    }

    public override float GetHeight()
    {
        var helpBoxAttribute = attribute as vHelpBoxAttribute;
        if (helpBoxAttribute == null)
            return base.GetHeight();

        Style.richText = true;
        return Mathf.Max(
            EditorGUIUtility.singleLineHeight,
            Style.CalcHeight(new GUIContent(helpBoxAttribute.text), EditorGUIUtility.currentViewWidth) + 10);
    }
}
