#nullable disable
#if UNITY_EDITOR
namespace HH.Attributes.Editor
{
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(ValidClassTypeAttribute))]
    public class ValidClassTypeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ValidClassTypeAttribute validClassType = (ValidClassTypeAttribute)attribute;
            System.Type baseType = validClassType.BaseType;

            EditorGUI.BeginProperty(position, label, property);

            // Draw the MonoScript field
            Rect fieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.objectReferenceValue = EditorGUI.ObjectField(fieldRect, label, property.objectReferenceValue, typeof(MonoScript), false);

            // Validate the selected MonoScript
            MonoScript monoScript = property.objectReferenceValue as MonoScript;
            if (monoScript != null)
            {
                System.Type scriptClass = monoScript.GetClass();
                if (scriptClass != null)
                {
                    if (!baseType.IsAssignableFrom(scriptClass))
                    {
                        // Shift the help box down without increasing the property height
                        EditorGUI.indentLevel++;
                        Rect helpBoxRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight * 2);
                        EditorGUI.HelpBox(helpBoxRect, $"The selected class does not inherit from {baseType.Name}.", MessageType.Error);
                        EditorGUI.indentLevel--;
                        Debug.LogError("The selected class does not inherit from " + baseType.Name, property.serializedObject.targetObject);
                    }
                }
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            MonoScript monoScript = property.objectReferenceValue as MonoScript;
            if (monoScript != null && monoScript.GetClass() != null)
            {
                System.Type scriptClass = monoScript.GetClass();
                if (!((ValidClassTypeAttribute)attribute).BaseType.IsAssignableFrom(scriptClass))
                {
                    return (EditorGUIUtility.singleLineHeight * 3) + EditorGUIUtility.standardVerticalSpacing;
                }
            }

            return EditorGUIUtility.singleLineHeight;
        }
    }
}
#endif
