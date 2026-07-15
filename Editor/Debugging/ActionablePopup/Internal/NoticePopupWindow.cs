#nullable disable

namespace HH.Editor.Notices
{
    using HH.Editor;
    using UnityEditor;
    using UnityEngine;

    public class NoticePopupWindow : EditorWindow
    {
        private static readonly float Height = 150;
        private static readonly float Width = 500;
        private static NoticePopupWindow windowInstance;
        private static Rect defaultRect =
            new Rect(
                new Vector2((Screen.width / 2) - Width, (Screen.height / 2) - Height),
                new Vector2(Width, Height));

        private INoticePopup popup;
        private MessageType messageType;
        private Texture messageTexture;
        private Color messageColor;

        public static void ShowEditorWindow(INoticePopup popup, MessageType type)
        {
            string typeString = type == MessageType.None ? string.Empty : type.ToString();
            windowInstance = GetWindowWithRect<NoticePopupWindow>(defaultRect, true, $"{typeString} Notice", true);
            windowInstance.messageType = type;

            windowInstance.popup = popup;
            windowInstance.messageTexture = type switch
            {
                MessageType.None => null,
                MessageType.Info => EditorGUIUtility.IconContent("console.infoicon@2x").image,
                MessageType.Warning => EditorGUIUtility.IconContent("console.warnicon@2x").image,
                MessageType.Error => EditorGUIUtility.IconContent("console.erroricon@2x").image,
                _ => null,
            };

            windowInstance.messageColor = type switch
            {
                MessageType.None => GUI.color,
                MessageType.Info => Color.gray,
                MessageType.Warning => Color.yellow,
                MessageType.Error => Color.red,
                _ => GUI.color,
            };
        }

        public static void CloseWindowInstance()
        {
            if (windowInstance == null)
            {
                return;
            }

            windowInstance.Close();
        }

        public void OnGUI()
        {
            if (windowInstance == null || popup == null || popup.ShouldClose)
            {
                EditorApplication.delayCall += Close;
                return;
            }

            if (popup != null && popup.NoticeSolved())
            {
                windowInstance.messageTexture = EditorGUIUtility.IconContent("Installed@2x").image;
                windowInstance.messageColor = Color.green;
            }
            else
            {
                windowInstance.messageTexture = windowInstance.messageType switch
                {
                    MessageType.None => null,
                    MessageType.Info => EditorGUIUtility.IconContent("console.infoicon@2x").image,
                    MessageType.Warning => EditorGUIUtility.IconContent("console.warnicon@2x").image,
                    MessageType.Error => EditorGUIUtility.IconContent("console.erroricon@2x").image,
                    _ => null,
                };

                windowInstance.messageColor = windowInstance.messageType switch
                {
                    MessageType.None => GUI.color,
                    MessageType.Info => Color.gray,
                    MessageType.Warning => Color.yellow,
                    MessageType.Error => Color.red,
                    _ => GUI.color,
                };
            }

            Rect position = GenerateWindowRect();
            Color defaultCol = GUI.color;

            GUI.color = messageColor;
            EditorGUI.LabelField(position, string.Empty, EditorStyles.helpBox);
            GUI.color = defaultCol;

            Rect iconPosition = position;
            iconPosition.width = Height;
            iconPosition.height = Height;

            GUI.DrawTexture(iconPosition, messageTexture, ScaleMode.ScaleAndCrop);

            if (messageTexture != null)
            {
                position.x += iconPosition.width;
                position.width -= iconPosition.width;
            }

            popup.DrawGUI(HEditor.Shrink(position, 10));
        }

        private Rect GenerateWindowRect()
        {
            return new Rect(0, 0, defaultRect.width, defaultRect.height);
        }

        private void OnDisable()
        {
            popup?.OnDisable();
        }
    }
}
