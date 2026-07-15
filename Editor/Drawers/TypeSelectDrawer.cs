namespace HH.SerializedType.Editor
{
    using System;
    using Bewildered.Editor;
    // using HH.SerializedType;
    using HH.Editor;
    using UnityEditor;
    using UnityEditor.Experimental.GraphView;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(TypeSelectAttribute))]
    public class TypeSelectDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            TypeSelectAttribute validClassType = (TypeSelectAttribute)attribute;
            Type baseType = validClassType.BaseType;

            // Ensure property is a list
            if (property.isArray)
            {
                EditorGUI.BeginProperty(position, label, property);
                for (int i = 0; i < property.arraySize; i++)
                {
                    property.serializedObject.ApplyModifiedProperties();
                    SerializedProperty elementProperty = property.GetArrayElementAtIndex(i);
                    DrawElement(position, elementProperty, baseType, i);
                    position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                }

                EditorGUI.EndProperty();
            }
            else
            {
                DrawElement(position, property, baseType, -1);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.isArray)
            {
                float height = 0;
                for (int i = 0; i < property.arraySize; i++)
                {
                    SerializedProperty elementProperty = property.GetArrayElementAtIndex(i);
                    HH.SerializedType.SerializedType myType = elementProperty.GetValue<HH.SerializedType.SerializedType>();
                    height += (myType == null || myType.SelectedType == null || myType.SelectedType.Name.Equals(string.Empty))
                        ? (EditorGUIUtility.singleLineHeight * 3) + EditorGUIUtility.standardVerticalSpacing
                        : EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                }

                return height;
            }

            return EditorGUIUtility.singleLineHeight;
        }

        private void DrawElement(Rect position, SerializedProperty elementProperty, Type baseType, int index)
        {
            elementProperty.serializedObject.ApplyModifiedProperties();

            Rect textRect = new Rect(position.x, position.y, position.width - 20, EditorGUIUtility.singleLineHeight);
            Rect searchRect = new Rect(position.x + textRect.width, position.y, 20, EditorGUIUtility.singleLineHeight);

            if (baseType == typeof(object))
            {
                GUI.enabled = false;
            }

            HH.SerializedType.SerializedType myType;

            GUIContent icon = EditorGUIUtility.IconContent("Button Icon");
            if (GUI.Button(searchRect, icon))
            {
                ClassTypeSearchProvider provider = ScriptableObject.CreateInstance<ClassTypeSearchProvider>();
                provider.ClassObjectSearch = baseType;
                SerializedProperty serializedTarget = elementProperty.FindPropertyRelative(nameof(myType.QualifiedName));
                provider.SerializedProperty = serializedTarget;
                SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(Event.current.mousePosition)), provider);
            }

            myType = elementProperty.GetValue<HH.SerializedType.SerializedType>();

            myType.SelectedType = myType.QualifiedName != string.Empty ? Type.GetType(myType.QualifiedName) : baseType;

            string display = myType.SelectedType != null ? myType.SelectedType.ToString() : "None";

            GUI.enabled = false;
            EditorGUI.TextField(textRect, GUIContent.none, display);
            GUI.enabled = true;

            if (myType == null || myType.SelectedType == null || myType.SelectedType.Name.Equals(string.Empty))
            {
                // Shift the help box down without increasing the property height
                string message = index == -1 ? $"Element {index} is invalid. It should derive from: {baseType.Name} but was {myType.SelectedType}"
                    : $"Selected Type is invalid. It should derive from: {baseType.Name} but was {myType.SelectedType}";
                EditorGUI.indentLevel++;
                Rect helpBoxRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight * 2);
                EditorGUI.HelpBox(helpBoxRect, message, MessageType.Error);
                EditorGUI.indentLevel--;
                Debug.LogError(message, elementProperty.serializedObject.targetObject);
            }
        }
    }
}
