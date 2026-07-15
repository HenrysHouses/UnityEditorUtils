#nullable disable

namespace HH.Editor.Notices
{
    using System;
    using System.Collections.Generic;
    using HH.Debugging;
    using HH.EditorExtensions;
    using UnityEditor;
    using UnityEngine;

    public class ExceptionNoticePopup : INoticePopup
    {
        private static Queue<ExceptionNoticePopup> queue = new Queue<ExceptionNoticePopup>();
        private bool popped = false;
        private GUIContent message;
        private object targetObject;
        private string label;
        private bool wasIgnored;

        private ExceptionNoticePopup(GUIContent message, object component)
        {
            label = "Close";
            wasIgnored = false;
            this.message = message;
            targetObject = component;
        }

        public bool ShouldClose { get; private set; } = false;

        public static void CreateWindow(object triggerObject, GUIContent message)
        {
            string path = string.Empty;
            if (triggerObject is Component objectComponent)
            {
                string hierarchyPath = GameObjectExtensions.GetHierarchyPath(objectComponent.gameObject);
                path = $"{objectComponent.gameObject.name} at {hierarchyPath}";
            }

            if (triggerObject is GameObject gameObject)
            {
                string hierarchyPath = GameObjectExtensions.GetHierarchyPath(gameObject);
                path = $"{gameObject.name} at {hierarchyPath}";
            }

            if (triggerObject is ScriptableObject scriptableObject)
            {
                path = AssetDatabase.GetAssetPath(scriptableObject);
            }

            // string executionPath = StackTraceUtility.ExtractStackTrace();

            message.text = $"{message.text}\n\nOriginated from: {triggerObject.GetType().FullName}\n";

            ExceptionNoticePopup errorPopup;
            if (triggerObject is UnityEngine.Object @object)
            {
                Debug.LogError(message.text, @object);
                errorPopup = new ExceptionNoticePopup(message, triggerObject);
            }
            else
            {
                MonoScript script = MonoScriptUtility.FindMonoScriptFromType(triggerObject.GetType());
                message.text = $"{message.text}{AssetDatabase.GetAssetPath(script)}";
                Debug.LogError(message.text, script);
                errorPopup = new ExceptionNoticePopup(message, script);
            }

            if (queue.Count == 0)
            {
                EditorApplication.delayCall += () => NoticePopupWindow.ShowEditorWindow(errorPopup, MessageType.Error);
            }

            queue.Enqueue(errorPopup);
        }

        public void DrawGUI(Rect position)
        {
            if (!popped && wasIgnored)
            {
                if (queue.TryDequeue(out _))
                {
                    popped = true;
                }

                label = !queue.TryPeek(out ExceptionNoticePopup next) ? "Close" : "Next";
                NextIssue(label, next);
            }

            Rect messageRect = position;
            messageRect.y += EditorGUIUtility.standardVerticalSpacing;

            GUIStyle messageStyle = EditorStyles.label;
            messageStyle.wordWrap = true;
            messageRect.height = messageStyle.CalcHeight(message, messageRect.width);

            EditorGUI.LabelField(messageRect, message, messageStyle);

            Rect buttonsRect = messageRect;
            buttonsRect.height = EditorGUIUtility.singleLineHeight;
            buttonsRect.y = HEditor.FindBottomLinePosition(position);

            if (targetObject is not null and UnityEngine.Object serializableObject)
            {
                Rect referenceRect = HEditor.PullLine(buttonsRect);
                Type referenceType = targetObject.GetType();
                GUI.enabled = false;
                EditorGUI.ObjectField(referenceRect, new GUIContent("Target", "Target object reference for the notice origin"), serializableObject, referenceType, true);
                GUI.enabled = true;
            }

            if (GUI.Button(buttonsRect, label))
            {
                wasIgnored = true;
            }
        }

        public bool NoticeSolved()
        {
            return wasIgnored;
        }

        public void OnDisable()
        {
            queue.Clear();
        }

        private void NextIssue(string buttonLabel, ExceptionNoticePopup nextNotice)
        {
            switch (buttonLabel)
            {
                case "Next":
                    label = nextNotice.label;
                    message = nextNotice.message;
                    wasIgnored = false;
                    targetObject = nextNotice.targetObject;
                    popped = false;
                    break;

                case "Close":
                    ShouldClose = true;
                    popped = false;
                    break;
            }
        }
    }
}
