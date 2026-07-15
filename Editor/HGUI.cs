#nullable disable
namespace HH.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using NUnit.Compatibility;
    using UnityEditor;
    using UnityEngine;

    public static class HGUI
    {
        // private static readonly string UnsupportedFieldMessage = "Field could not be serialized or drawn:\n{0}";

        private static readonly Dictionary<Type, Func<Rect, string, object, object>> DrawerMap = new()
        {
            { typeof(bool), (position, label, value) => EditorGUI.Toggle(position, label, (bool)value) },
            { typeof(string), (position, label, value) => EditorGUI.TextField(position, label, (string)value) },
            { typeof(int), (position, label, value) => EditorGUI.IntField(position, label, (int)value) },
            { typeof(float), (position, label, value) => EditorGUI.FloatField(position, label, (float)value) },
            { typeof(long), (position, label, value) => EditorGUI.LongField(position, label, (long)value) },
            { typeof(double), (position, label, value) => EditorGUI.DoubleField(position, label, (double)value) },
            { typeof(Rect), (position, label, value) => EditorGUI.RectField(position, label, (Rect)value) },
            { typeof(Color), (position, label, value) => EditorGUI.ColorField(position, label, (Color)value) },
            { typeof(AnimationCurve), (position, label, value) => EditorGUI.CurveField(position, label, (AnimationCurve)value) },
            { typeof(Bounds), (position, label, value) => EditorGUI.BoundsField(position, label, (Bounds)value) },
            { typeof(BoundsInt), (position, label, value) => EditorGUI.BoundsIntField(position, label, (BoundsInt)value) },
            { typeof(Vector2), (position, label, value) => EditorGUI.Vector2Field(position, label, (Vector2)value) },
            { typeof(Vector3), (position, label, value) => EditorGUI.Vector3Field(position, label, (Vector3)value) },
            { typeof(Vector4), (position, label, value) => EditorGUI.Vector4Field(position, label, (Vector4)value) },
            { typeof(Gradient), (position, label, value) => EditorGUI.GradientField(position, label, (Gradient)value) },
            { typeof(Enum), (position, label, value) => EditorGUI.EnumFlagsField(position, label, (Enum)value) },
            { typeof(Vector2Int), (position, label, value) => EditorGUI.Vector2IntField(position, label, (Vector2Int)value) },
            { typeof(Vector3Int), (position, label, value) => EditorGUI.Vector3IntField(position, label, (Vector3Int)value) },

            // Add more types as needed
        };

        public static (bool IsExpanded, bool PrivateFields) ClassField(Rect position, GUIContent label, object data, bool isExpanded, bool includePrivateFields)
        {
            // Save the original indent level
            int originalIndentLevel = EditorGUI.indentLevel;

            if (data == null)
            {
                Debug.LogError($"Something went wrong during drawing the GUI for an interface field: {data}");
                return (false, includePrivateFields);
            }

            DrawClassFields(position, label, data, ref isExpanded, ref includePrivateFields);

            EditorGUI.indentLevel = originalIndentLevel;
            return (isExpanded, includePrivateFields);
        }

        public static bool InterfaceField(Rect position, object data, Type @interface, GUIContent label, bool isExpanded, bool privateFields)
        {
            // Save the original indent level
            int originalIndentLevel = EditorGUI.indentLevel;

            // position = HGUI.DrawBackground(position);

            position.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.LabelField(position, label);
            position.y += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;

            if (data == null)
            {
                Debug.LogError($"Something went wrong during drawing the GUI for an interface field: {data}");
                return false;
            }

            bool shouldBeExpanded = DrawInterfaceGetters(position, isExpanded, data, @interface, privateFields);

            EditorGUI.indentLevel = originalIndentLevel;
            return shouldBeExpanded;
        }

        public static float GetClassFieldHeight(Type target, bool isExpanded, bool privateFields)
        {
            float propertyCount = isExpanded ? GetClassPropertyCount(target, privateFields) + 2 : 1;
            return (propertyCount * EditorGUIUtility.singleLineHeight) + (EditorGUIUtility.standardVerticalSpacing * propertyCount) + (EditorGUIUtility.standardVerticalSpacing * 5);
        }

        public static float GetInterfaceFieldHeight(Type target, bool isExpanded, bool privateFields)
        {
            float propertyCount = isExpanded ? GetInterfacePropertyCount(target, privateFields) + 3 : 3;
            return (propertyCount * EditorGUIUtility.singleLineHeight) + (EditorGUIUtility.standardVerticalSpacing * propertyCount) + (EditorGUIUtility.standardVerticalSpacing * 5);
        }

        private static GUIStyle GetUnsupportedLabelStyle()
        {
            GUIStyle style = new GUIStyle(EditorStyles.label)
            {
                clipping = TextClipping.Ellipsis,
            };
            return style;
        }

        private static float GetClassPropertyCount(Type target, bool privateFields)
        {
            float linesDrawn = 0;

            BindingFlags flags = BindingFlags.Public |
                        BindingFlags.Instance |
                        BindingFlags.DeclaredOnly;

            if (privateFields)
            {
                flags = BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.Instance |
                        BindingFlags.DeclaredOnly;
            }

            // Loop over the interface properties
            foreach (PropertyInfo prop in target.GetProperties(flags))
            {
                // Only readable getters
                if (!prop.CanRead)
                {
                    continue;
                }

                bool isBuiltin = target.GetType().Namespace == "System" ||
                    target.GetType().Namespace.StartsWith("System") ||
                    target.GetType().Module.ScopeName == "CommonLanguageRuntimeLibrary";

                bool isUnityObject = typeof(UnityEngine.Object).IsCastableFrom(prop.PropertyType);

                if (isBuiltin && !isUnityObject && !prop.PropertyType.IsInterface)
                {
                    linesDrawn++;
                    continue;
                }

                if (!isUnityObject && !prop.PropertyType.IsInterface)
                {
                    linesDrawn += GetClassPropertyCount(prop.PropertyType, privateFields);
                    continue;
                }

                linesDrawn++;
            }

            return linesDrawn;
        }

        private static float GetInterfacePropertyCount(Type target, bool privateFields)
        {
            float linesDrawn = 0;

            BindingFlags flags = BindingFlags.Public |
                        BindingFlags.Instance |
                        BindingFlags.DeclaredOnly;

            if (privateFields)
            {
                flags = BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.Instance |
                        BindingFlags.DeclaredOnly;
            }

            // Get all interfaces implemented by MyClass
            Type[] implementedInterfaces = target.GetInterfaces();
            foreach (Type interfaces in implementedInterfaces)
            {
                // Loop over the interface properties
                foreach (PropertyInfo prop in interfaces.GetProperties(flags))
                {
                    // Only readable getters
                    if (!prop.CanRead)
                    {
                        continue;
                    }

                    bool isBuiltin = target.GetType().Namespace == "System" ||
                        target.GetType().Namespace.StartsWith("System") ||
                        target.GetType().Module.ScopeName == "CommonLanguageRuntimeLibrary";

                    bool isUnityObject = typeof(UnityEngine.Object).IsCastableFrom(prop.PropertyType);

                    if (isBuiltin && !isUnityObject && !prop.PropertyType.IsInterface)
                    {
                        linesDrawn++;
                        continue;
                    }

                    if (!isUnityObject && !prop.PropertyType.IsInterface)
                    {
                        linesDrawn += GetClassPropertyCount(prop.PropertyType, privateFields);
                        continue;
                    }

                    if (prop.PropertyType.IsInterface)
                    {
                        linesDrawn += GetInterfacePropertyCount(prop.PropertyType, privateFields);
                        continue;
                    }

                    linesDrawn++;
                }
            }

            return linesDrawn;
        }

        private static void DrawClassFields(Rect position, GUIContent label, object data, ref bool isExpanded, ref bool includePrivateFields)
        {
            EditorGUI.indentLevel++;

            Type classType = data.GetType();

            // GUIContent label = new GUIContent();
            GUIStyle style = new GUIStyle(EditorStyles.foldout)
            {
                fontSize = EditorStyles.label.fontSize,
                clipping = TextClipping.Ellipsis,
                fixedWidth = position.width - 22,
            };

            position.height = EditorGUIUtility.singleLineHeight;
            isExpanded = EditorGUI.Foldout(position, isExpanded, label, true, style);
            position = HEditor.PushLine(position);

            // return false if it shouldnt be expanded
            if (!isExpanded)
            {
                return;
            }

            GUI.enabled = false;
            EditorGUI.indentLevel++;

            if (data == null)
            {
                EditorGUILayout.LabelField("No instance assigned.");
                EditorGUI.indentLevel--;
                return;
            }

            GUI.enabled = true;
            includePrivateFields = EditorGUI.Toggle(position, "Show Private Fields", includePrivateFields);
            GUI.enabled = false;
            BindingFlags flags = BindingFlags.Public |
                        BindingFlags.Instance |
                        BindingFlags.DeclaredOnly;

            if (includePrivateFields)
            {
                flags = BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.Instance |
                        BindingFlags.DeclaredOnly;
            }

            foreach (PropertyInfo prop in classType.GetProperties(flags))
            {
                if (!prop.CanRead)
                {
                    continue;
                }

                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                // get value of the getter with reflection
                PropertyInfo getterProperty = classType.GetProperty(prop.Name);

                if (getterProperty == null)
                {
                    (Rect left, Rect right) = HEditor.SplitRect(position);
                    EditorGUI.LabelField(left, prop.Name);
                    EditorGUI.LabelField(right, "Null");
                    continue;
                }

                object drawData = getterProperty.GetValue(data);
                if (drawData != null && DrawerMap.ContainsKey(prop.PropertyType))
                {
                    DrawerMap[prop.PropertyType].Invoke(position, prop.Name, drawData);
                    continue;
                }

                object value = prop.GetValue(data);

                if (value == null)
                {
                    (Rect left, Rect right) = HEditor.SplitRect(position);
                    EditorGUI.LabelField(left, prop.Name);
                    EditorGUI.LabelField(right, "Null");
                    continue;
                }

                if (prop.PropertyType.IsInterface)
                {
                    (Rect left, Rect right) = HEditor.SplitRect(position);
                    EditorGUI.LabelField(left, prop.Name);
                    EditorGUI.LabelField(right, value.ToString());
                    continue;
                    // InterfaceField(position, drawData, prop.PropertyType, new GUIContent(prop.Name), isExpanded, includePrivateFields);
                    // float interfaceHeight = GetInterfaceFieldHeight(prop.PropertyType, isExpanded, includePrivateFields);
                    // position.y += interfaceHeight;
                }

                if (typeof(UnityEngine.Object).IsCastableFrom(value.GetType()))
                {
                    EditorGUI.ObjectField(position, prop.Name, (UnityEngine.Object)value, value.GetType(), false);
                    continue;
                }

                ClassField(position, new GUIContent(prop.Name), drawData, isExpanded, includePrivateFields);
                float classHeight = GetClassFieldHeight(prop.PropertyType, isExpanded, includePrivateFields);

                // GUIStyle unsupportedStyle = GetUnsupportedLabelStyle();
                // string formattedMessage = string.Format(UnsupportedFieldMessage, value ?? null);

                // position.height = (EditorGUIUtility.singleLineHeight * 2) + EditorGUIUtility.standardVerticalSpacing;
                // EditorGUI.LabelField(position, new GUIContent(formattedMessage, formattedMessage), unsupportedStyle);
                // position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                position.y += classHeight;
                position.height = EditorGUIUtility.singleLineHeight;
            }

            EditorGUI.indentLevel--;
            GUI.enabled = true;
            return;
        }

        private static bool DrawInterfaceGetters(Rect position, bool isExpanded, object data, Type @interface, bool privateFields)
        {
            EditorGUI.indentLevel++;

            Type classType = data.GetType();
            Type[] implementedInterfaces = classType.GetInterfaces();
            Type deepestInterface = @interface;

            // NOTE: this may be redundant as we should already know the deepest interface from definition
            foreach (Type inheritedInterface in implementedInterfaces)
            {
                for (int i = 0; i < implementedInterfaces.Length; i++)
                {
                    if (deepestInterface.IsAssignableFrom(inheritedInterface))
                    {
                        deepestInterface = inheritedInterface;
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

            label.text = isExpanded
                ? $"Interface Variables:\n{deepestInterface.Name} implemented in {data.GetType().Name}"
                : $"Interface Variables:\n{deepestInterface.Name}";

            // position.y += EditorGUIUtility.singleLineHeight;
            position.height = EditorGUIUtility.singleLineHeight * 2;
            bool newState = EditorGUI.Foldout(position, isExpanded, label, true, style);
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            position.height = EditorGUIUtility.singleLineHeight;

            // return false if it shouldnt be expanded
            if (!newState)
            {
                return false;
            }

            GUI.enabled = false;
            EditorGUI.indentLevel++;

            if (data == null)
            {
                EditorGUILayout.LabelField("No instance assigned.");
                EditorGUI.indentLevel--;
                return true;
            }

            BindingFlags flags = BindingFlags.Public |
                        BindingFlags.Instance |
                        BindingFlags.DeclaredOnly;

            if (privateFields)
            {
                flags = BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.Instance |
                        BindingFlags.DeclaredOnly;
            }

            foreach (Type inheritedInterfaces in implementedInterfaces)
            {
                // Loop over the interface properties
                foreach (PropertyInfo prop in inheritedInterfaces.GetProperties(flags))
                {
                    // Only public getters
                    if (!prop.CanRead)
                    {
                        continue;
                    }

                    position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                    // get value of the getter with reflection
                    PropertyInfo getterProperty = @interface.GetProperty(prop.Name);

                    if (getterProperty == null)
                    {
                        (Rect left, Rect right) = HEditor.SplitRect(position);
                        EditorGUI.LabelField(left, prop.Name);
                        EditorGUI.LabelField(right, "Null");
                        continue;
                    }

                    object drawData = getterProperty.GetValue(data);
                    if (drawData != null && DrawerMap.ContainsKey(prop.PropertyType))
                    {
                        DrawerMap[prop.PropertyType].Invoke(position, prop.Name, drawData);
                        continue;
                    }

                    object value = prop.GetValue(data);

                    if (value == null)
                    {
                        (Rect left, Rect right) = HEditor.SplitRect(position);
                        EditorGUI.LabelField(left, prop.Name);
                        EditorGUI.LabelField(right, "Null");
                        continue;
                    }

                    if (typeof(UnityEngine.Object).IsCastableFrom(value.GetType()))
                    {
                        EditorGUI.ObjectField(position, prop.Name, (UnityEngine.Object)value, value.GetType(), false);
                        continue;
                    }

                    if (prop.PropertyType.IsInterface)
                    {
                        InterfaceField(position, drawData, prop.PropertyType, new GUIContent(prop.Name), isExpanded, privateFields);
                        float interfaceHeight = GetInterfaceFieldHeight(prop.PropertyType, isExpanded, privateFields);
                        position.y += interfaceHeight;
                    }

                    ClassField(position, new GUIContent(prop.Name), drawData, isExpanded, privateFields);
                    float classHeight = GetClassFieldHeight(prop.PropertyType, isExpanded, privateFields);

                    // GUIStyle unsupportedStyle = GetUnsupportedLabelStyle();
                    // string formattedMessage = string.Format(UnsupportedFieldMessage, value ?? null);

                    // position.height = (EditorGUIUtility.singleLineHeight * 2) + EditorGUIUtility.standardVerticalSpacing;
                    // EditorGUI.LabelField(position, new GUIContent(formattedMessage, formattedMessage), unsupportedStyle);
                    position.y += classHeight;
                    position.height = EditorGUIUtility.singleLineHeight;
                }
            }

            EditorGUI.indentLevel--;
            GUI.enabled = true;
            return true;
        }
    }
}
