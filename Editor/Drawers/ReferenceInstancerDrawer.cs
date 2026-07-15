#nullable disable
namespace HH.Attributes.Editor
{
    using System;
    using System.Collections;
    using System.Reflection;
    using Bewildered.Editor;
    using HH.Editor;
    using HH.EditorExtensions;
    using UnityEditor;
    using UnityEditor.Experimental.GraphView;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(ReferenceInstancerAttribute))]
    public class ReferenceInstancerDrawer : PropertyDrawer
    {
        private readonly Color blue = new Color(0.129f, 0.419f, 0.729f);
        private readonly Color red = new Color(0.729f, 0.129f, 0.129f);
        private readonly Color green = new Color(0.129f, 0.729f, 0.239f);
        private bool hasChanged = true;
        private Type t = null;
        private MonoScript monoScript = null;

        private void OnEnable()
        {
            Debug.Log("is this working?");
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                Debug.LogError($"{property.displayName} must be a SerializedReference for ReferenceInstancer to function", property.serializedObject.targetObject);
                return;
            }

            Rect childrenRect = position;
            position.xMax -= 80 + 40;
            Rect fieldRect = new Rect(position.xMin, position.y, position.width, EditorGUIUtility.singleLineHeight);

            EditorGUI.BeginChangeCheck();
            if (property.managedReferenceValue != null)
            {
                if (hasChanged)
                {
                    t = property.managedReferenceValue.GetType();
                    monoScript = MonoScriptUtility.FindMonoScriptFromType(t);
                }
            }

            // Draw the script the reference originates from
            // NOTE: if this is complaining with errors then add null checks for Type t, and MonoScript to exit early
            GUI.enabled = false;
            monoScript = (MonoScript)EditorGUI.ObjectField(fieldRect, label, monoScript, typeof(MonoScript), false);
            GUI.enabled = true;
            hasChanged = EditorGUI.EndChangeCheck();

            // Enable edit attributes of the class / expand the property
            position.x = position.xMax;
            position.width = 40;
            Rect expandRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            if (property.isExpanded)
            {
                GUI.color = property.managedReferenceValue != null ? blue : red;
            }

            if (GUI.Button(expandRect, new GUIContent("Edit")))
            {
                property.isExpanded = !property.isExpanded;
            }

            GUI.color = Color.white;

            position.x += position.width;
            position.width = 80;
            Rect findRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            GUI.color = property.managedReferenceValue != null ? green : red;

            string parentPropertyName = property.propertyPath.Split('.')[0];
            SerializedProperty parentProperty = property.serializedObject.FindProperty(parentPropertyName);
            FieldInfo overrideField = parentProperty.GetFieldInfo();
            Type searchObjectType = GetRestrictionType(overrideField);


            if (GUI.Button(findRect, new GUIContent("Instantiate")))
            {
                ClassSearchProvider provider = ScriptableObject.CreateInstance<ClassSearchProvider>();
                provider.ClassObjectSearch = searchObjectType;
                provider.SerializedProperty = property;
                SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(Event.current.mousePosition)), provider);
                property.serializedObject.ApplyModifiedProperties();
            }

            GUI.color = Color.white;

            // If the property is expanded, draw its children
            if (property.managedReferenceValue != null)
            {
                EditorGUI.indentLevel++;
                if (property.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    childrenRect.y += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
                    childrenRect.height = EditorGUI.GetPropertyHeight(property);
                    EditorGUI.PropertyField(childrenRect, property, true);
                    EditorGUI.indentLevel--;
                }

                EditorGUI.indentLevel--;

                property.serializedObject.ApplyModifiedProperties();
            }
        }

        private Type GetRestrictionType(FieldInfo attributedOverrideField)
        {
            ReferenceInstancerAttribute instancerAttr = (ReferenceInstancerAttribute)attribute;

            Type attributeOverrideType = typeof(ReferenceInstancerOverrideAttribute);
            Attribute customAttr = Attribute.GetCustomAttribute(attributedOverrideField, attributeOverrideType);
            ReferenceInstancerOverrideAttribute overrideAttr = customAttr as ReferenceInstancerOverrideAttribute;

            Type inferred = GetInferredType();

            if (overrideAttr != null && inferred.IsAssignableFrom(overrideAttr.TypeOverride))
            {
                return overrideAttr.TypeOverride;
            }

            if (overrideAttr != null)
            {
                // Debug.LogWarning($"ReferenceInstancer: an invalid override type was detected in field \"{attributedOverrideField}\"");
            }

            if (instancerAttr.TypeRestriction != null && inferred.IsAssignableFrom(instancerAttr.TypeRestriction))
            {
                return instancerAttr.TypeRestriction;
            }

            if (instancerAttr.TypeRestriction != null)
            {
                Debug.LogWarning($"ReferenceInstancer: an invalid restriction type was detected in field \"{attributedOverrideField}\"");
            }

            return inferred;
        }

        private Type GetInferredType()
        {
            if (fieldInfo.FieldType.IsArray)
            {
                return fieldInfo.FieldType.GetElementType();
            }

            if (fieldInfo.FieldType.IsGenericType && typeof(IList).IsAssignableFrom(fieldInfo.FieldType))
            {
                return fieldInfo.FieldType.GetGenericArguments()[0];
            }

            return fieldInfo.FieldType;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;

            if (property.managedReferenceValue != null && property.isExpanded)
            {
                height += EditorGUIUtility.standardVerticalSpacing;
                height += EditorGUI.GetPropertyHeight(property, label, true);
            }

            return height;
        }
    }
}
