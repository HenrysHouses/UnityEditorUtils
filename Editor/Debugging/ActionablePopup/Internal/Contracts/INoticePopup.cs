#nullable disable

namespace HH.Editor.Notices
{
    using UnityEngine;

    public interface INoticePopup
    {
        bool ShouldClose { get; }

        void DrawGUI(Rect position);

        bool NoticeSolved();

        void OnDisable();
    }
}
