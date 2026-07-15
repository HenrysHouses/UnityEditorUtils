namespace HH.Editor.SerializedType
{
    using System;
    // using HH.Editor;
    using HH.SerializedType;
    using UnityEditor;
    using UnityEditor.Experimental.GraphView;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(HH.SerializedType.SerializedType))]
    public class SerializedTypeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty typeNameProperty = property.FindPropertyRelative("QualifiedName");

            // Check if this SerializedType has a TypeSelectAttribute
            TypeSelectAttribute typeSelectAttr = attribute as TypeSelectAttribute;
            Type baseType = typeSelectAttr?.BaseType ?? typeof(object);

            // Layout rects
            Rect labelRect;
            Rect textRect;
            Rect buttonRect;

            if (label.text != string.Empty)
            {
                labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
                textRect = new Rect(labelRect.xMax, position.y, position.width - labelRect.width - 20, EditorGUIUtility.singleLineHeight);
                buttonRect = new Rect(position.xMax - 20, position.y, 20, EditorGUIUtility.singleLineHeight);

                EditorGUI.LabelField(labelRect, label);
            }
            else
            {
                textRect = new Rect(position.x, position.y, position.width - 20, EditorGUIUtility.singleLineHeight);
                buttonRect = new Rect(position.xMax - 20, position.y, 20, EditorGUIUtility.singleLineHeight);
            }

            Type currentType = !string.IsNullOrEmpty(typeNameProperty.stringValue) ?
                Type.GetType(typeNameProperty.stringValue) : null;
            string displayText = currentType != null ? currentType.Name : "None";

            GUI.enabled = false;
            EditorGUI.TextField(textRect, displayText);
            GUI.enabled = true;

            if (baseType == typeof(object))
            {
                GUI.enabled = false;
            }

            // Use your existing search provider
            GUIContent icon = EditorGUIUtility.IconContent("Button Icon");
            if (GUI.Button(buttonRect, icon))
            {
                ClassTypeSearchProvider provider = ScriptableObject.CreateInstance<ClassTypeSearchProvider>();
                provider.ClassObjectSearch = baseType;
                provider.SerializedProperty = typeNameProperty;
                provider.AllowNull = false;
                SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(Event.current.mousePosition)), provider);
            }

            EditorGUI.EndProperty();
            GUI.enabled = true;
        }
    }
}
