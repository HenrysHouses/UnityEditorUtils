namespace HH.Editor
{
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEditor.Toolbars;
    using UnityEngine;

    public static class ValidateReferencesToolbar
    {
        private const string ToolbarElementName = "Tools/Analysis";
        private static Texture2D icon;

        public static bool IsScanning { get; private set; }

        public static void ReloadOnPlay(PlayModeStateChange state)
        {
            ReloadToolbar();
        }

        public static void ReloadToolbar()
        {
            MainToolbar.Refresh("Tools/Analysis");
        }

        public static void SetScan(bool scanning)
        {
            IsScanning = scanning;
            ReloadToolbar();
        }

        [MainToolbarElement(ToolbarElementName, defaultDockPosition = MainToolbarDockPosition.Middle)]
        private static IEnumerable<MainToolbarElement> CreateAnalysisWindowsBar()
        {
            int erorrCount = RequiredReferencesValidator.Errors.Count;
            Texture2D errorIcon = (Texture2D)EditorGUIUtility.IconContent("console.erroricon").image;
            MainToolbarLabel label = new MainToolbarLabel(new MainToolbarContent(erorrCount.ToString(), errorIcon, "Required Reference Errors Found"));

            if (erorrCount > 0)
            {
                yield return label;
            }

            int warningCount = RequiredReferencesValidator.Warnings.Count;
            Texture2D warnIcon = (Texture2D)EditorGUIUtility.IconContent("console.warnicon").image;
            label = new MainToolbarLabel(new MainToolbarContent(warningCount.ToString(), warnIcon, "Required Reference Warnings Found"));

            if (warningCount > 0)
            {
                yield return label;
            }

            SetIcon();
            string tooltip = "Check scene for null references";
            MainToolbarButton button = new MainToolbarButton(
                    new MainToolbarContent(icon, tooltip), () => RequiredReferencesValidator.ValidateScene());
            yield return button;

            Texture2D scanningIcon;
            if (IsScanning)
            {
                scanningIcon = (Texture2D)EditorGUIUtility.IconContent("TestInconclusive").image;
                tooltip = "Stop Build Scenes Scan";
                button = new MainToolbarButton(
                        new MainToolbarContent(scanningIcon, tooltip), () =>
                        {
                            RequiredReferencesValidator.CancelBuildValidation();
                        });
                yield return button;
            }
            else
            {
                scanningIcon = (Texture2D)EditorGUIUtility.IconContent("d_Profiler.NetworkOperations@2x").image;
                tooltip = "Check Assets for null references";
                button = new MainToolbarButton(
                        new MainToolbarContent(scanningIcon, tooltip), () =>
                        {
                            AssetValidationRunner_EditorWindow.ShowEditorWindow();
                        });
                yield return button;
            }
        }

        private static void SetIcon()
        {
            icon = (Texture2D)EditorGUIUtility.IconContent("d_DebuggerAttached@2x").image;
            Texture2D activeIssues = (Texture2D)EditorGUIUtility.IconContent("d_DebuggerEnabled@2x").image;
            Texture2D playmode = (Texture2D)EditorGUIUtility.IconContent("d_DebuggerDisabled@2x").image;

            if (RequiredReferencesValidator.Warnings.Count > 0 || RequiredReferencesValidator.Errors.Count > 0)
            {
                icon = activeIssues;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                icon = playmode;
            }
        }
    }
}
