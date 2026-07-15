#nullable disable
namespace HH.SerializableInterface
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Bewildered.Editor;
    using NUnit.Compatibility;
    using HH.Editor;
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(SerializableInterface<>), true)]
    public class SerializableInterfacePropertyDrawer : PropertyDrawer
    {
        private readonly string unsupportedFieldMessage = "Field could not be serialized or drawn:\n{0}";

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property == null)
            {
                return;
            }

            position.y += EditorGUIUtility.standardVerticalSpacing * 2;

            // Save the original indent level
            int originalIndentLevel = EditorGUI.indentLevel;
            Type targetSerializedType = null;

            StoreGenericType(ref targetSerializedType);

            SerializedProperty serializedTargetObj = property.FindPropertyRelative("obj");

            position = HEditor.DrawBackground(position);

            position.height = EditorGUIUtility.singleLineHeight;
            position.y += EditorGUIUtility.standardVerticalSpacing;

            if (serializedTargetObj == null)
            {
                Debug.Log("Something went wrong during drawing the GUI for an SerializableInterface", property.objectReferenceValue);
                return;
            }

            DrawReferenceField(position, serializedTargetObj, label, targetSerializedType);
            DrawInterfaceGetters(position, property, serializedTargetObj, targetSerializedType);

            EditorGUI.indentLevel = originalIndentLevel;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty serializedTargetObj = property.FindPropertyRelative("obj");
            float propertyCount = GetPropertyCount(property, serializedTargetObj);
            return (propertyCount * EditorGUIUtility.singleLineHeight) + (EditorGUIUtility.standardVerticalSpacing * propertyCount) + (EditorGUIUtility.standardVerticalSpacing * 5);
        }

        private GUIStyle GetUnsupportedLabelStyle()
        {
            GUIStyle style = new GUIStyle(EditorStyles.label)
            {
                clipping = TextClipping.Ellipsis,
            };
            return style;
        }

        private float GetPropertyCount(SerializedProperty property, SerializedProperty interfaceReference)
        {
            if (property == null)
            {
                return 1;
            }

            float linesDrawn = 3;

            if (!property.isExpanded)
            {
                return linesDrawn;
            }

            object referenceObject = interfaceReference.GetValue();

            if (referenceObject == null)
            {
                return linesDrawn;
            }

            Type classType = interfaceReference.GetValue().GetType();
            // Get all interfaces implemented by MyClass
            Type[] implementedInterfaces = classType.GetInterfaces();
            foreach (Type interfaces in implementedInterfaces)
            {
                // Loop over the interface properties
                foreach (PropertyInfo prop in interfaces.GetProperties())
                {
                    // Only public getters
                    if (!prop.CanRead)
                    {
                        continue;
                    }

                    if (interfaceReference.FindPropertyRelative(prop.Name) != null)
                    {
                        linesDrawn++;
                        continue;
                    }

                    object value = prop.GetValue(interfaceReference.GetValue());
                    if (typeof(UnityEngine.Object).IsCastableFrom(value.GetType()))
                    {
                        linesDrawn++;
                        continue;
                    }

                    float lineHeight = EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
                    linesDrawn += 2;
                }
            }

            return linesDrawn;
        }

        private void StoreGenericType(ref Type targetSerializedType)
        {
            if (targetSerializedType == null)
            {
                Type fieldType = fieldInfo.FieldType;
                if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    // when used in a List<>, grab the actual type from the generic argument of the List
                    fieldType = fieldInfo.FieldType.GetGenericArguments()[0];
                }
                else if (fieldType.IsArray)
                {
                    // when used in an array, grab the actual type from the element type.
                    fieldType = fieldType.GetElementType();
                }

                Type[] types = fieldType.GetGenericArguments();
                if (types != null && types.Length == 1)
                {
                    targetSerializedType = types[0];
                }
            }
        }

        private void DrawReferenceField(Rect position, SerializedProperty serializedTargetObj, GUIContent label, Type targetType)
        {
            List<Component> gameObjectComponents = new List<Component>(); // if this editor performance is an issue, find a way to cache this for

            EditorGUI.BeginChangeCheck();

            UnityEngine.Object newObj = EditorGUI.ObjectField(position, label, serializedTargetObj.objectReferenceValue, typeof(UnityEngine.Object), true);
            if (EditorGUI.EndChangeCheck())
            {
                if (newObj == null)
                {
                    serializedTargetObj.objectReferenceValue = null;
                }
                else if (targetType.IsAssignableFrom(newObj.GetType()))
                {
                    serializedTargetObj.objectReferenceValue = newObj;
                }
                else if (newObj is GameObject newlySetGameObject)
                {
                    newlySetGameObject.GetComponents(targetType, gameObjectComponents);
                    if (gameObjectComponents.Count == 1)
                    {
                        serializedTargetObj.objectReferenceValue = gameObjectComponents[0];
                    }
                    else
                    {
                        GenericMenu menu = new GenericMenu();
                        int menuContentCount = 1;
                        foreach (Component itemData in gameObjectComponents)
                        {
                            menu.AddItem(
                                new GUIContent(menuContentCount++.ToString() + " " + itemData.GetType().Name),
                                false,
                                dataReference =>
                                {
                                    serializedTargetObj.objectReferenceValue = (UnityEngine.Object)dataReference;
                                    serializedTargetObj.serializedObject.ApplyModifiedProperties();
                                },
                                itemData);
                        }

                        menu.ShowAsContext();
                    }
                }
                else
                {
                    Debug.LogWarning("Dragged object is not compatible with " + targetType.Name);
                }
            }
        }

        private void DrawInterfaceGetters(Rect position, SerializedProperty property, SerializedProperty reference, Type listType)
        {
            EditorGUI.indentLevel++;
            object referenceObject = reference.GetValue();

            Type classType = reference.GetValue().GetType();
            Type[] implementedInterfaces = classType.GetInterfaces();
            Type deepestInterface = listType;

            foreach (Type @interface in implementedInterfaces)
            {
                for (int i = 0; i < implementedInterfaces.Length; i++)
                {
                    if (deepestInterface.IsAssignableFrom(@interface))
                    {
                        deepestInterface = @interface;
                    }
                }
            }

            GUIContent label = new GUIContent();
            GUIStyle style = new GUIStyle(EditorStyles.foldout)
            {
                fontSize = EditorStyles.label.fontSize,
                clipping = TextClipping.Ellipsis,
                fixedWidth = position.width - 22,
            };

            label.text = property.isExpanded
                ? $"Interface Variables:\n{deepestInterface.Name} implemented in {referenceObject.GetType().Name}"
                : $"Interface Variables:\n{deepestInterface.Name}";

            position.y += EditorGUIUtility.singleLineHeight;
            position.height = EditorGUIUtility.singleLineHeight * 2;
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label, true, style);
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            position.height = EditorGUIUtility.singleLineHeight;

            if (!property.isExpanded)
            {
                return;
            }

            GUI.enabled = false;
            EditorGUI.indentLevel++;

            if (referenceObject == null)
            {
                EditorGUILayout.LabelField("No instance assigned.");
                EditorGUI.indentLevel--;
                return;
            }

            foreach (Type @interface in implementedInterfaces)
            {
                // Loop over the interface properties
                foreach (PropertyInfo prop in @interface.GetProperties())
                {
                    // Only public getters
                    if (!prop.CanRead)
                    {
                        continue;
                    }

                    position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                    SerializedProperty serProp = reference.FindPropertyRelative(prop.Name);
                    if (serProp != null)
                    {
                        EditorGUI.PropertyField(position, serProp);
                        continue;
                    }

                    object value = prop.GetValue(reference.GetValue());

                    if (typeof(UnityEngine.Object).IsCastableFrom(value.GetType()))
                    {
                        EditorGUI.ObjectField(position, prop.Name, (UnityEngine.Object)value, value.GetType(), false);
                        continue;
                    }

                    GUIStyle unsupportedStyle = GetUnsupportedLabelStyle();
                    string formattedMessage = string.Format(unsupportedFieldMessage, value ?? null);

                    position.height = (EditorGUIUtility.singleLineHeight * 2) + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.LabelField(position, new GUIContent(formattedMessage, formattedMessage), unsupportedStyle);
                    position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    position.height = EditorGUIUtility.singleLineHeight;
                }
            }

            EditorGUI.indentLevel--;
            GUI.enabled = true;
        }
    }
}
