#nullable disable
namespace HH.Attributes.Editor
{
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Draws the property field for any field marked with ExpandableAttribute.
    /// </summary>
    [CustomPropertyDrawer(typeof(ExpandableAttribute), true)]
    public class ExpandableDrawer : PropertyDrawer
    {
        /// <summary>
        /// Whether the default editor Script field should be shown.
        /// </summary>
        private static readonly bool ShowScriptField = false;

        /// <summary>
        /// The spacing on the inside of the background rect.
        /// </summary>
        private static readonly float InnerSpacing = 6.0f;

        /// <summary>
        /// The spacing on the outside of the background rect.
        /// </summary>
        private static readonly float OuterSpacing = 4.0f;

        /// <summary>
        /// The style the background uses.
        /// </summary>
        // private static readonly BackgroundStyles BackgroundStyle = BackgroundStyles.HelpBox;

        /// <summary>
        /// The color that is used to darken the background.
        /// </summary>
        // private static Color darkenColor = new(0.0f, 0.0f, 0.0f, 0.2f);

        /// <summary>
        /// The color that is used to lighten the background.
        /// </summary>
        // private static Color lightenColor = new(1.0f, 1.0f, 1.0f, 0.2f);
        // Use the following area to change the style of the expandable ScriptableObject drawers;
        private enum BackgroundStyles
        {
            None,
            HelpBox,
            Darken,
            Lighten,
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float totalHeight = 0.0f;

            totalHeight += EditorGUIUtility.singleLineHeight;

            if (property.objectReferenceValue == null)
            {
                return totalHeight;
            }

            if (!property.isExpanded)
            {
                return totalHeight;
            }

            SerializedObject targetObject = new(property.objectReferenceValue);

            if (targetObject == null)
            {
                return totalHeight;
            }

            SerializedProperty field = targetObject.GetIterator();

            _ = field.NextVisible(true);

            if (ShowScriptField)
            {
                totalHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            while (field.NextVisible(false))
            {
                totalHeight += EditorGUI.GetPropertyHeight(field, true) + EditorGUIUtility.standardVerticalSpacing;
            }

            totalHeight += InnerSpacing * 2;
            totalHeight += OuterSpacing * 2;

            return totalHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Save the original indent level
            int originalIndentLevel = EditorGUI.indentLevel;

            // Create a rect for the foldout arrow
            Rect foldoutRect = new Rect(position.x, position.y, 15f, EditorGUIUtility.singleLineHeight);

            // Draw the foldout arrow
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, GUIContent.none, true);

            // Create a rect for the object reference (this will be clickable)
            Rect objectFieldRect = new Rect(position.x + 15f, position.y, position.width - 15f, EditorGUIUtility.singleLineHeight);

            // Draw the object reference field
            EditorGUI.PropertyField(objectFieldRect, property, label, true);

            // Check if the object reference is null or if it's not expanded
            if (property.objectReferenceValue == null || !property.isExpanded)
            {
                return;
            }

            // Draw the expanded fields for the ScriptableObject
            SerializedObject targetObject = new SerializedObject(property.objectReferenceValue);
            SerializedProperty iterator = targetObject.GetIterator();

            // Move to the first visible property
            iterator.NextVisible(true);

            EditorGUI.indentLevel++;

            // Create a rect for the child properties
            Rect childPropertyRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight);

            // Iterate over the child properties and draw them
            while (iterator.NextVisible(false))
            {
                float propertyHeight = EditorGUI.GetPropertyHeight(iterator, true);
                childPropertyRect.height = propertyHeight;
                EditorGUI.PropertyField(childPropertyRect, iterator, true);
                childPropertyRect.y += propertyHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            // Apply modified properties to the target object
            targetObject.ApplyModifiedProperties();

            // Restore the original indent level
            EditorGUI.indentLevel = originalIndentLevel;
        }

        /// <summary>
        /// Draws the Background.
        /// </summary>
        /// <param name="rect">The Rect where the background is drawn.</param>
        // private void DrawBackground(Rect rect)
        // {
        //     switch (BackgroundStyle)
        //     {
        //         case BackgroundStyles.HelpBox:
        //             EditorGUI.HelpBox(rect, string.Empty, MessageType.None);
        //             break;
        //         case BackgroundStyles.Darken:
        //             EditorGUI.DrawRect(rect, darkenColor);
        //             break;
        //         case BackgroundStyles.Lighten:
        //             EditorGUI.DrawRect(rect, lightenColor);
        //             break;
        //         case BackgroundStyles.None:
        //         default:
        //             break;
        //     }
        // }
    }
}
