#nullable disable
namespace HH.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1649 // SA1649FileNameMustMatchTypeName
    public static class HEditor
#pragma warning restore SA1649 // SA1649FileNameMustMatchTypeName
    {
        public static readonly Color BaseOutlineColor = new Color(0.175f, 0.175f, 0.175f, 1f);
        public static readonly Color BaseBackgroundColor = new Color(0.25f, 0.25f, 0.25f, 1f);

        public static Rect DrawBackground(Rect position, Color? background, Color? outline, float outlineSize = 2, bool drawBaseOutline = true)
        {
            Rect OutlineRect = position;
            Rect backgroundRect = Shrink(position, outlineSize);

            return DrawBackgroundContainerRect(backgroundRect, background, OutlineRect, outline, drawBaseOutline);
        }

        // TODO: add options for having no border on certain sides

        public static Rect DrawBackground(Rect position, float outlineSize = 2, bool drawBaseOutline = true)
        {
            return DrawBackground(position, null, null, outlineSize, drawBaseOutline);
        }

        private static Rect DrawBackgroundContainerRect(Rect backgroundRect, Color? backgroundColor, Rect OutlineRect, Color? outlineColor, bool drawBaseOutline = true)
        {
            float basicOutlineWidth = 1f;

            if (drawBaseOutline)
            {
                EditorGUI.DrawRect(OutlineRect, Color.black);

                OutlineRect.x += basicOutlineWidth;
                OutlineRect.y += basicOutlineWidth;
                OutlineRect.width -= basicOutlineWidth * 2;
                OutlineRect.height -= basicOutlineWidth * 2;

                backgroundRect.x += basicOutlineWidth;
                backgroundRect.y += basicOutlineWidth;
                backgroundRect.width -= basicOutlineWidth * 2;
                backgroundRect.height -= basicOutlineWidth * 2;


                // HELP: dont know why the pixels are sometimes off, but this version works perfectly on a different size, but breaks others.
                // OutlineRect.x += basicOutlineWidth / 2;
                // OutlineRect.y += basicOutlineWidth;
                // OutlineRect.width -= basicOutlineWidth * 2;
                // OutlineRect.height -= basicOutlineWidth * 2;
                //
                // backgroundRect.x += basicOutlineWidth;
                // backgroundRect.y += basicOutlineWidth;
                // backgroundRect.width -= basicOutlineWidth * 3;
                // backgroundRect.height -= basicOutlineWidth * 2;
            }

            Color targetOutlineColor = outlineColor ?? BaseOutlineColor;
            Color targetBackgroundColor = backgroundColor ?? BaseBackgroundColor;

            EditorGUI.DrawRect(OutlineRect, targetOutlineColor);
            EditorGUI.DrawRect(backgroundRect, targetBackgroundColor);
            return Shrink(backgroundRect, EditorGUIUtility.standardVerticalSpacing);
        }

        public static Rect DrawBackgroundRect(Rect backgroundRect, Color? backgroundColor, Rect OutlineRect, Color? outlineColor, bool drawBaseOutline = true)
        {
            if (drawBaseOutline)
            {
                EditorGUI.DrawRect(OutlineRect, Color.black);

                // offsets the background rects for the base outline
                (OutlineRect, backgroundRect) = GetOutlinedBackgroundRect(OutlineRect, backgroundRect);
            }

            Color targetOutlineColor = outlineColor ?? BaseOutlineColor;
            Color targetBackgroundColor = backgroundColor ?? BaseBackgroundColor;

            EditorGUI.DrawRect(OutlineRect, targetOutlineColor);
            EditorGUI.DrawRect(backgroundRect, targetBackgroundColor);
            return backgroundRect;
        }

        public static (Rect OutlineRect, Rect BackgroundRect) GetOutlinedBackgroundRect(Rect OutlineRect, Rect backgroundRect)
        {
            float basicOutlineWidth = 1f;

            OutlineRect.x += basicOutlineWidth;
            OutlineRect.y += basicOutlineWidth;
            OutlineRect.width -= basicOutlineWidth * 2;
            OutlineRect.height -= basicOutlineWidth * 2;

            backgroundRect.x += basicOutlineWidth;
            backgroundRect.y += basicOutlineWidth;
            backgroundRect.width -= basicOutlineWidth * 2;
            backgroundRect.height -= basicOutlineWidth * 2;

            return (OutlineRect, backgroundRect);
        }

        public static Rect Shrink(Rect position, float margin = 2)
        {
            position.y += margin;
            position.x += margin;
            position.width -= margin * 2;
            position.height -= margin * 2;
            return position;
        }

        public static Rect PushLine(Rect position)
        {
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            return position;
        }

        public static float GetPushLineHeight()
        {
            return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        public static Rect PullLine(Rect position)
        {
            position.y -= EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            return position;
        }

        public static float FindBottomLinePosition(Rect position)
        {
            return position.height - EditorGUIUtility.singleLineHeight;
        }

        public static Rect[] SplitRect(Rect position, int desiredElements)
        {
            float elementWidths = position.width /= desiredElements;
            Rect[] elements = new Rect[desiredElements];

            Rect lastSplit = position;
            lastSplit.width = elementWidths;
            for (int i = 0; i < desiredElements; i++)
            {
                elements[i] = lastSplit;
                elements[i].x += elementWidths * (i % desiredElements);
            }

            return elements;
        }

        public static (Rect Left, Rect Right) SplitRect(Rect position)
        {
            position.width /= 2;
            Rect leftSplit = position;

            position.x += position.width;
            Rect rightSplit = position;

            return (leftSplit, rightSplit);
        }

        public static float CalculateHeightWithSpacing(int drawnLines)
        {
            return EditorGUIUtility.standardVerticalSpacing + (EditorGUIUtility.singleLineHeight * drawnLines) + (EditorGUIUtility.standardVerticalSpacing * drawnLines);
        }

        public static Rect SetStandardHeight(Rect target, int lineHeight)
        {
            target.height = CalculateHeightWithSpacing(lineHeight);
            return target;
        }

        public static float GetStandardHeight(int lineHeight)
        {
            return CalculateHeightWithSpacing(lineHeight);
        }

        public static void DrawErrorBox(Rect position, string message)
        {
            Debug.LogError(message);

            bool guiState = GUI.enabled;
            GUI.enabled = true;
            Color @default = GUI.color;
            GUI.color = Color.red;
            EditorGUI.HelpBox(position, message, MessageType.Error);
            GUI.color = @default;
            GUI.enabled = guiState;
        }

        public static void DrawErrorBox(string message)
        {
            Debug.LogError(message);

            bool guiState = GUI.enabled;
            Color @default = GUI.color;
            GUI.color = Color.red;
            EditorGUILayout.HelpBox(message, MessageType.Error, false);
            GUI.color = @default;
            GUI.enabled = guiState;
        }

        public static void DrawErrorBox(string message, Component component)
        {
            Debug.LogError(message, component);

            bool guiState = GUI.enabled;
            Color @default = GUI.color;
            GUI.color = Color.red;
            EditorGUILayout.HelpBox(message, MessageType.Error, false);
            GUI.color = @default;
            GUI.enabled = guiState;
        }

        public static void DrawErrorBox(Rect position, SerializedProperty property, string message)
        {
            if (message == string.Empty)
            {
                message = "is invalid";
                Debug.LogError($"{property.displayName} {message}");
            }
            else
            {
                Debug.LogError(message);
            }

            EditorGUI.LabelField(position, property.displayName);
            Color @default = GUI.color;
            GUI.color = Color.red;
            position.xMin += 123;
            EditorGUI.HelpBox(position, message, MessageType.Error);
            GUI.color = @default;
        }

        public static string CleanBackingFieldName(string propertyName)
        {
            return propertyName.Trim('<').Replace(">k__BackingField", string.Empty);
        }

        public static SerializedObject CreateSerializedObject(object obj, FieldInfo field)
        {
            UnityEngine.Object value = (UnityEngine.Object)field.GetValue(obj);

            return new SerializedObject(value);
        }

        public static FieldInfo GetField(object obj, string propertyName)
        {
            return obj.GetType().GetField(propertyName, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public static FieldInfo GetBackingField(object obj, string propertyName)
        {
            return obj.GetType().GetField(GetBackingFieldName(propertyName), BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public static string GetBackingFieldName(string propertyName)
        {
            return string.Format("<{0}>k__BackingField", propertyName);
        }

        // <summary> </summary>
        // <param name="ui">GUI that describes the table's colors, outlines, cells, and position</param>
        public static void DrawTable(TableGUI ui, int cellFoldingControl)
        {
            if (ui.Rect.Rows.Length == 0)
            {
                return;
            }

            // Table Container
            DrawBackground(ui.Rect.OutlineRect, ui.GUI.Background, ui.GUI.Outline);

            // Table Cells
            for (int i = ui.Rect.Rows.Length - 1; i >= 0; i--)
            {
                bool rowExpandedState = ui.IsExpanded[i];
                for (int j = ui.Rect.Rows[i].Column.Length - 1; j >= 0; j--)
                {
                    // cell is the rect thats slightly bigger that creates the outline
                    Rect cell = ui.Rect.Rows[i].Column[j];

                    Color cellBackground = ui.CellGUI[i, j].Background;
                    Color cellOutline = ui.CellGUI[i, j].Outline;
                    ui.Rect.Rows[i].Foreground[j] = DrawBackground(cell, cellBackground, cellOutline);

                    ui.Rect.Rows[i].Foreground[j].x -= 15;
                    ui.Rect.Rows[i].Foreground[j].width += 15;
                    // Hande table GUI
                    (bool isExpanded, object returnArgs) = ui.CellGUI[i, j].GUIDrawCall.Invoke(ui.Rect.Rows[i].Foreground[j], ui.IsExpanded[i], ui.DrawCallCache[i, j]);

                    ui.CacheDrawCallValue(i, j, returnArgs);

                    if (cellFoldingControl == j)
                    {
                        if (isExpanded != rowExpandedState)
                        {
                            ui.IsExpanded[i] = isExpanded; // current expanded value
                        }
                    }
                }
            }
        }

        public struct TableRow
        {
            public Rect[] Column;
            public Rect[] Foreground;

            public TableRow(int columns, Rect size)
            {
                Column = SplitRect(size, columns);
                Foreground = new Rect[Column.Length];

                // making the stand and begin pixels overlap
                for (int i = 0; i < Column.Length - 1; i++)
                {
                    Column[i].width += 1;
                    // NOTE the foreground is not populated here
                    // the foreground rect is created when the content of the cell is being drawn
                }
            }

            public readonly float GetHeight()
            {
                return Column.Length > 0 ? Column[0].height : 0;
            }
        }

        public struct TableRect
        {
            public Rect Position;
            public Rect OutlineRect;
            public Rect TableSize;
            public TableRow[] Rows;

            public TableRect(int rows, int columns, Rect size, float outlineSize)
            {
                Position = size;
                TableSize = Shrink(Position, outlineSize);
                Rows = new TableRow[rows];

                Rect tableDefaultElement = TableSize;
                tableDefaultElement.height = CalculateHeightWithSpacing(1);

                for (int i = 0; i < rows; i++)
                {
                    // setting the end and begin pixels overlap on the same pixel
                    if (i > 0)
                    {
                        tableDefaultElement.y -= 1;
                    }

                    Rows[i] = new TableRow(columns, tableDefaultElement);
                    tableDefaultElement.y += tableDefaultElement.height;
                }

                // remove the added height of the last iteration as we are not drawing another cell
                size.height = CalculateHeightWithSpacing(Rows.Length) + Rows.Length + 2 + outlineSize;
                OutlineRect = size;
            }

            public TableRect(int rows, int columns, Rect size, float outlineSize, Dictionary<int, float> rowHeights)
            {
                Position = size;
                TableSize = Shrink(Position, outlineSize);
                TableSize.height = CalculateHeightWithSpacing(1);
                Rows = new TableRow[rows];

                Rect tableDefaultElement = TableSize;

                float totalCellHeight = 0;
                for (int i = 0; i < rows; i++)
                {
                    // setting the end and begin pixels overlap on the same pixel
                    if (i > 0)
                    {
                        tableDefaultElement.y -= 1;
                    }

                    float variedHeight = 0;
                    if (rowHeights.ContainsKey(i))
                    {
                        variedHeight = rowHeights[i];
                    }

                    tableDefaultElement.height = variedHeight != 0 ? variedHeight : CalculateHeightWithSpacing(1);

                    totalCellHeight += tableDefaultElement.height;
                    Rows[i] = new TableRow(columns, tableDefaultElement);
                    tableDefaultElement.y += tableDefaultElement.height;
                }

                // remove the added height of the last iteration as we are not drawing another cell
                // size.height = tableDefaultElement.y - tableDefaultElement.height;
                size.height = totalCellHeight - 1;
                // size.height = tableDefaultElement.y + tableDefaultElement.height;
                // size.height = CalculateHeightWithSpacing(Rows.Length) + Rows.Length + 2 + outlineSize;
                OutlineRect = size;
            }

            public readonly bool ElementsFitsInTable => Height < Position.height;

            public readonly int Columns => Rows[0].Column.Length;

            public readonly float Height
            {
                get
                {
                    float height = 0;
                    foreach (TableRow element in Rows)
                    {
                        height += element.GetHeight();
                    }

                    return height;
                }
            }
        }

        public class CellGUI : BackgroundGUI
        {
            public CellGUI() : base()
            {
                GUIDrawCall = null;
            }

            public CellGUI(Color background, Color outline) : base(background, outline)
            {
                GUIDrawCall = null;
            }

            public CellGUI(Func<Rect, bool, object, (bool IsExpanded, object ReturnArgs)> guiDrawCall, Color background, Color outline) : base(background, outline)
            {
                GUIDrawCall = guiDrawCall;
            }

            public CellGUI(Func<Rect, bool, object, (bool IsExpanded, object ReturnArgs)> guiDrawCall) : base()
            {
                GUIDrawCall = guiDrawCall;
            }

            public Func<Rect, bool, object, (bool IsExpanded, object ReturnArgs)> GUIDrawCall { get; set; }
        }

        public class BackgroundGUI
        {
            public BackgroundGUI()
            {
                Background = BaseBackgroundColor;
                Outline = BaseOutlineColor;
            }

            public BackgroundGUI(Color background, Color outline)
            {
                Background = background;
                Outline = outline;
            }

            public Color Background { get; private set; }

            public Color Outline { get; private set; }
        }

        public class TableGUI
        {
            public TableGUI(TableRect rect)
            {
                if (rect.Rows != null)
                {
                    Rect = rect;
                    CellGUI = new CellGUI[rect.Rows.Length, rect.Columns];

                    for (int i = 0; i < rect.Rows.Length; i++)
                    {
                        for (int j = 0; j < rect.Columns; j++)
                        {
                            CellGUI[i, j] = new CellGUI();
                        }
                    }
                }

                int n = rect.Rows.Length;
                IsExpanded = new bool[n];
                DrawCallCache = new object[rect.Rows.Length, Rect.Columns];
            }

            public TableGUI(TableRect rect, BackgroundGUI table)
            {
                TableGUI defaultTable = new TableGUI(rect);
                Rect = rect;
                GUI = table;
                CellGUI = defaultTable.CellGUI;
                int n = rect.Rows.Length;
                IsExpanded = new bool[n];
                DrawCallCache = new object[rect.Rows.Length, Rect.Columns];
            }

            public TableGUI(TableRect rect, BackgroundGUI table, CellGUI[,] cells)
            {
                Rect = rect;
                CellGUI = cells;
                GUI = table;
                int n = rect.Rows.Length;
                IsExpanded = new bool[n];
                DrawCallCache = new object[rect.Rows.Length, Rect.Columns];
            }

            public TableRect Rect { get; private set; }

            public CellGUI[,] CellGUI { get; private set; }

            public BackgroundGUI GUI { get; private set; }

            // <summary> dimension 0: expanded state, dimension 1: Include private fields.</summary>
            public bool[] IsExpanded { get; private set; }

            public object[,] DrawCallCache { get; private set; }

            public void Update(TableGUI gui)
            {
                Rect = gui.Rect;
                CellGUI = gui.CellGUI;
                GUI = gui.GUI;
            }

            public void CacheDrawCallValue(int x, int y, object value)
            {
                DrawCallCache[x, y] = value;
            }
        }
    }
}
