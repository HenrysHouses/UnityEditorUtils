#nullable disable

namespace HH.Editor.Notices
{
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Events;

    public class ButtonNoticePopup : INoticePopup
    {
        private static Queue<ButtonNoticePopup> queue = new Queue<ButtonNoticePopup>();
        private bool popped = false;
        private GUIContent message;
        private bool hasIgnoreButton;
        private UnityEngine.Object targetObject;
        private Func<bool> solvedChecker;
        private UnityAction solveAction;
        private UnityEvent buttonAction;
        private string label;
        private bool wasIgnored;
        private bool wasSolved;
        private ValidationResult validationReference;

        private ButtonNoticePopup(GUIContent message, string buttonLabel, UnityAction buttonAction, Func<bool> solveCheck, ValidationResult reference, UnityEngine.Object component, bool allowIgnore)
        {
            label = buttonLabel;
            solveAction = buttonAction;
            this.buttonAction = new UnityEvent();
            this.buttonAction.AddListener(solveAction);
            this.message = message;
            solvedChecker = solveCheck;
            hasIgnoreButton = allowIgnore;
            wasIgnored = false;
            targetObject = component;
            validationReference = reference;
        }

        public bool ShouldClose { get; private set; } = false;

        public static void CreateWindow(MessageType type, GUIContent message, string buttonLabel, UnityAction buttonAction, Func<bool> solveCheck, ValidationResult reference, UnityEngine.Object component = null, bool allowIgnore = false)
        {
            ButtonNoticePopup buttonPopup = new ButtonNoticePopup(message, buttonLabel, buttonAction, solveCheck, reference, component, allowIgnore);

            if (queue.Count == 0)
            {
                EditorApplication.delayCall += () => NoticePopupWindow.ShowEditorWindow(buttonPopup, type);
            }

            queue.Enqueue(buttonPopup);
        }

        public void DrawGUI(Rect position)
        {
            if (!popped && wasSolved)
            {
                if (NoticeSolved() || wasIgnored)
                {
                    if (!popped)
                    {
                        if (queue.TryDequeue(out _))
                        {
                            popped = true;
                        }
                    }

                    message.text = "Great!\n\nThe issue was solved.";

                    buttonAction.RemoveAllListeners();
                    label = !queue.TryPeek(out ButtonNoticePopup next) ? "Close" : "Next";
                    buttonAction.AddListener(() => NextIssue(label, next));
                }
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

            if (targetObject != null)
            {
                Rect referenceRect = HEditor.PullLine(buttonsRect);
                Type referenceType = targetObject.GetType();
                GUI.enabled = false;
                EditorGUI.ObjectField(referenceRect, new GUIContent("Target", "Target object reference for the notice origin"), targetObject, referenceType, true);
                GUI.enabled = true;
            }

            (Rect leftButton, Rect rightButton) = HEditor.SplitRect(buttonsRect);

            if (hasIgnoreButton)
            {
                rightButton.x += EditorGUIUtility.standardVerticalSpacing;
                rightButton.width -= EditorGUIUtility.standardVerticalSpacing;
                leftButton.width -= EditorGUIUtility.standardVerticalSpacing;
            }

            if (GUI.Button(rightButton, label))
            {
                buttonAction?.Invoke();
            }

            if (!hasIgnoreButton)
            {
                return;
            }

            if (GUI.Button(leftButton, "Ignore"))
            {
                wasIgnored = true;
            }
        }

        public bool NoticeSolved()
        {
            if (solvedChecker == null)
            {
                return wasIgnored;
            }

            bool solve = solvedChecker.Invoke() || wasIgnored;

            if (solve && !wasSolved)
            {
                RequiredReferencesValidator.Process(validationReference);
                wasSolved = true;
            }

            return solve;
        }

        private void NextIssue(string buttonLabel, ButtonNoticePopup nextNotice)
        {
            switch (buttonLabel)
            {
                case "Next":
                    label = nextNotice.label;
                    message = nextNotice.message;
                    solvedChecker = nextNotice.solvedChecker;
                    hasIgnoreButton = nextNotice.hasIgnoreButton;
                    wasIgnored = false;
                    wasSolved = false;
                    targetObject = nextNotice.targetObject;
                    buttonAction.RemoveAllListeners();
                    buttonAction.AddListener(nextNotice.solveAction);
                    solveAction = nextNotice.solveAction;
                    validationReference = nextNotice.validationReference;
                    popped = false;
                    break;

                case "Close":
                    ShouldClose = true;
                    popped = false;
                    break;
            }
        }

        public void OnDisable()
        {
            queue.Clear();
        }
    }
}
