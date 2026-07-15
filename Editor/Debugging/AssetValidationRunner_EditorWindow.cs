namespace HH.Editor
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Bewildered.Editor;
    using HH.SerializedType;
    using UnityEditor;
    using UnityEngine;

    public class AssetValidationRunner_EditorWindow : EditorWindow
    {
        private const string SaveKey = "ValidateAssetTypes";
        private static Task scanBuildScenesTask;
        private SerializedObject serializedObject;
        private SerializedProperty typeProperty;
        private bool loaded = false;

        [SerializeField, TypeSelect(typeof(ScriptableObject))] private HH.SerializedType.SerializedType[] validationTargets = new HH.SerializedType.SerializedType[] { };

        [MenuItem("Tools/Validate Required References/In Assets")]
        public static void ShowEditorWindow()
        {
            GetWindow<AssetValidationRunner_EditorWindow>("Asset Validation Runner");
        }

        private void OnEnable()
        {
            serializedObject = new(this);
            typeProperty = serializedObject.FindProperty(nameof(validationTargets));
        }

        private void OnGUI()
        {
            if (!loaded)
            {
                LoadTargets();
                loaded = true;
            }

            EditorGUILayout.LabelField("Select Which Asset Types To Validate");
            EditorGUILayout.PropertyField(typeProperty);
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            Color defaultColor = GUI.color;
            if (GUILayout.Button("Load Targets"))
            {
                LoadTargets();
            }

            GUI.color = Color.cyan;
            if (GUILayout.Button("Save Targets"))
            {
                SaveTargets();
            }

            GUI.color = defaultColor;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Actions");
            GUI.color = Color.green;
            if (GUILayout.Button("Scan Assets"))
            {
                Type[] targets = validationTargets.Select(s => s.SelectedType).ToArray();
                RequiredReferencesValidator.ValidateAssets(targets);
            }

            if (GUILayout.Button("Scan Prefabs"))
            {
                RequiredReferencesValidator.ValidatePrefabs();
            }

            if (GUILayout.Button("Scan Open Scene"))
            {
                RequiredReferencesValidator.ValidateScene();
            }

            if (GUILayout.Button("Scan Build Scenes"))
            {
                if (scanBuildScenesTask != null && !scanBuildScenesTask.IsCompleted)
                {
                    Debug.LogWarning("[Reference Validation] Cannot restart build scene scan, please finish the current task first");
                    return;
                }

                EditorApplication.delayCall += async () =>
                {
                    scanBuildScenesTask = RequiredReferencesValidator.ValidateBuildScenes();
                    await scanBuildScenesTask;
                };
            }

            GUI.color = Color.red;
            GUI.enabled = ValidateReferencesToolbar.IsScanning;
            if (GUILayout.Button("Stop Build Scenes Scan"))
            {
                if (scanBuildScenesTask != null && !scanBuildScenesTask.IsCompleted)
                {
                    RequiredReferencesValidator.CancelBuildValidation();
                }
            }
            GUI.enabled = true;
            GUI.color = defaultColor;
        }


        private void SaveTargets()
        {
            SaveContainer container = new()
            {
                Types = validationTargets.Select(s => s.SelectedType.AssemblyQualifiedName).ToArray(),
            };
            string types = JsonUtility.ToJson(container);
            EditorPrefs.SetString(SaveKey, types);
        }

        private void LoadTargets()
        {
            string data = EditorPrefs.GetString(SaveKey);
            SaveContainer container;
            object load = JsonUtility.FromJson(data, typeof(SaveContainer));
            if (load != null)
            {
                container = (SaveContainer)load;
                HH.SerializedType.SerializedType[] sTypes = new HH.SerializedType.SerializedType[container.Types.Length];
                for (int i = 0; i < sTypes.Length; i++)
                {
                    Type t = Type.GetType(container.Types[i]);
                    // Debug.Log(container.Types[i] + " - " + t);
                    sTypes[i] = new HH.SerializedType.SerializedType(t);
                }

                validationTargets = sTypes;
                typeProperty.SetValue(sTypes);
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }
        }

        private struct SaveContainer
        {
            public string[] Types;
        }
    }
}
