namespace HH.Editor
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Bewildered.Editor;
    using HH.Attributes;
    using HH.Editor.Notices;
    using UnityEditor;
    using UnityEditor.Callbacks;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    [InitializeOnLoad]
    public static class RequiredReferencesValidator
    {
        private static CancellationTokenSource scanTokenSource = new();
        private static bool isExecutingTask = false;

        static RequiredReferencesValidator()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += ValidateReferencesToolbar.ReloadOnPlay;
            EditorApplication.playModeStateChanged += ValidateReferencesToolbar.ReloadOnPlay;
            EditorSceneManager.sceneSaved += OnSceneSaved;
            EditorSceneManager.sceneOpened += OnSceneLoaded;
        }

        public static List<ValidationResult> Warnings { get; private set; } = new List<ValidationResult>();

        public static List<ValidationResult> Errors { get; private set; } = new List<ValidationResult>();

        public static void Process(ValidationResult result)
        {
            if (Warnings.Contains(result))
            {
                Warnings.Remove(result);
            }

            if (Errors.Contains(result))
            {
                Errors.Remove(result);
            }

            ValidateReferencesToolbar.ReloadToolbar();
        }

        [DidReloadScripts, MenuItem("Tools/Validate Required References/Open Scene")]
        public static void ValidateScene()
        {
            Errors.Clear();
            Warnings.Clear();
            GameObject[] allGameObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int totalFieldsChecked = 0;
            int fieldsWithAttribute = 0;
            int invalidReferences = 0;

            foreach (GameObject go in allGameObjects)
            {
                MonoBehaviour[] components = go.GetComponents<MonoBehaviour>();

                foreach (MonoBehaviour component in components)
                {
                    if (component == null)
                    {
                        continue;
                    }

                    SerializedObject serializedObject = new SerializedObject(component);

                    ValidateComponent(serializedObject, component, component.GetType(), go.name, ref totalFieldsChecked, ref fieldsWithAttribute, ref invalidReferences, new HashSet<object>());
                }
            }

            ValidateReferencesToolbar.ReloadToolbar();
        }

        public static void ValidateAssets(Type[] assetTypes)
        {
            List<(UnityEngine.Object, Type)> search = new List<(UnityEngine.Object, Type)>();
            foreach (Type t in assetTypes)
            {
                string[] guids = AssetDatabase.FindAssets("t:" + t.Name);
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(path, t);
                    search.Add((obj, t));
                }
            }

            int fieldsChecked = 0;
            int fieldsRequireReference = 0;
            int invalidFields = 0;
            HashSet<object> visited = new HashSet<object>();
            foreach ((UnityEngine.Object obj, Type t) in search)
            {
                ValidateObject(obj, t, obj.name, ref fieldsChecked, ref fieldsRequireReference, ref invalidFields, visited);
            }

            ValidateReferencesToolbar.ReloadToolbar();
        }

        public static void ValidatePrefabs()
        {
            List<UnityEngine.Object> search = new List<UnityEngine.Object>();
            string[] guids = AssetDatabase.FindAssets("t:prefab");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject));
                search.Add(obj);
            }

            int fieldsChecked = 0;
            int fieldsRequireReference = 0;
            int invalidFields = 0;
            HashSet<object> visited = new HashSet<object>();
            foreach (UnityEngine.Object obj in search)
            {
                ValidateGameObjectDeep((GameObject)obj, ref fieldsChecked, ref fieldsRequireReference, ref invalidFields, visited);
            }

            ValidateReferencesToolbar.ReloadToolbar();
        }

        public static void CancelBuildValidation()
        {
            Debug.Log("Cancel requested");

            scanTokenSource.Cancel();
        }

        public static async Task ValidateBuildScenes()
        {
            if (isExecutingTask)
            {
                return;
            }

            isExecutingTask = true;

            Scene start = EditorSceneManager.GetActiveScene();
            string startingScene = start.path;
            ValidateReferencesToolbar.SetScan(isExecutingTask);

            for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
            {
                if (scanTokenSource.IsCancellationRequested)
                {
                    break;
                }

                string load = EditorBuildSettings.scenes[i].path;
                // EditorSceneManager.LoadScene(load.name);
                EditorSceneManager.OpenScene(load);
                // await LoadScene(load.name);
                await SceneIsValid();
                Scene save = EditorSceneManager.GetActiveScene();
                EditorSceneManager.SaveScene(save, save.path);
                NoticePopupWindow.CloseWindowInstance();
            }

            EditorSceneManager.OpenScene(startingScene);

            if (scanTokenSource.IsCancellationRequested)
            {
                scanTokenSource.Dispose();
                scanTokenSource = new();
            }

            isExecutingTask = false;
            ValidateReferencesToolbar.SetScan(isExecutingTask);
            Debug.Log("Build Scene Validation Complete");
        }

        private static async Task SceneIsValid()
        {
            while (Errors.Count > 0)
            {
                Debug.Log("waiting");
                if (scanTokenSource.IsCancellationRequested)
                {
                    break;
                }

                await Task.Delay(50);
            }
        }

        private static void OnSceneSaved(Scene scene)
        {
            ValidateScene();
        }

        private static void OnSceneLoaded(Scene scene, OpenSceneMode mode)
        {
            ValidateScene();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                ValidateScene();
            }
        }

        private static void ValidateGameObjectDeep(
                GameObject gameObject,
                ref int totalFieldsChecked, ref int fieldsWithAttribute, ref int invalidReferences,
                HashSet<object> visitedObjects)
        {
            ValidateGameObject(gameObject, ref fieldsWithAttribute, ref invalidReferences, ref totalFieldsChecked, visitedObjects);
            ValidateGameObjectChildren(gameObject, ref fieldsWithAttribute, ref invalidReferences, ref totalFieldsChecked, visitedObjects);
        }

        private static void ValidateGameObjectChildren(
                GameObject gameObject,
                ref int totalFieldsChecked, ref int fieldsWithAttribute, ref int invalidReferences,
                HashSet<object> visitedObjects)
        {
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                GameObject childObject = gameObject.transform.GetChild(i).gameObject;

                ValidateGameObject(childObject, ref fieldsWithAttribute, ref invalidReferences, ref totalFieldsChecked, visitedObjects);
                ValidateGameObjectChildren(childObject, ref fieldsWithAttribute, ref invalidReferences, ref totalFieldsChecked, visitedObjects);
            }
        }

        private static void ValidateGameObject(
                GameObject gameObject,
                ref int totalFieldsChecked, ref int fieldsWithAttribute, ref int invalidReferences,
                HashSet<object> visitedObjects)
        {
            Component[] components = gameObject.GetComponents<Component>();

            Type currentType;
            SerializedObject serializedObject;
            foreach (Component comp in components)
            {
                if (comp == null)
                {
                    continue;
                }

                currentType = comp.GetType();
                serializedObject = new SerializedObject(comp);
                ValidateComponent(serializedObject, comp, currentType, comp.name, ref totalFieldsChecked, ref fieldsWithAttribute, ref invalidReferences, visitedObjects);
            }
        }

        private static void ValidateObject(
                UnityEngine.Object @object, Type objectType, string objectName,
                ref int totalFieldsChecked, ref int fieldsWithAttribute, ref int invalidReferences,
                HashSet<object> visitedObjects)
        {
            Type currentType = objectType != null ? @object.GetType() : objectType;
            SerializedObject serializedObject = new SerializedObject(@object);

            while (currentType != null && currentType != typeof(MonoBehaviour))
            {
                FieldInfo[] fieldInfos = currentType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                foreach (FieldInfo fieldInfo in fieldInfos)
                {
                    if (!fieldInfo.IsPublic &&
                            fieldInfo.GetCustomAttribute<SerializeField>() == null &&
                            fieldInfo.GetCustomAttribute<SerializeReference>() == null)
                    {
                        continue;
                    }

                    RequireReferenceAttribute requireReferenceAttr = fieldInfo.GetCustomAttribute<RequireReferenceAttribute>();
                    if (requireReferenceAttr == null)
                    {
                        continue;
                    }

                    RequireReferenceData referenceData = new();
                    SerializedProperty serializedProperty = serializedObject.FindProperty(fieldInfo.Name);
                    if (serializedProperty == null)
                    {
                        Debug.LogWarning($"{objectName} -> {currentType.Name}.{fieldInfo.Name} | Serialized property not found");
                        continue;
                    }

                    totalFieldsChecked++;
                    fieldsWithAttribute++;

                    ValidationResult validationResult = ValidateSerializedProperty(serializedProperty, fieldInfo, referenceData, objectName, ref invalidReferences, visitedObjects);

                    switch (validationResult.Status)
                    {
                        default:
                        case ValidationStatus.None:
                            break;

                        case ValidationStatus.Warning:
                            Warnings.Add(validationResult);
                            Debug.LogWarning($"{objectName} -> {currentType.Name}.{fieldInfo.Name} | {validationResult.Message}", @object);
                            break;
                        case ValidationStatus.Error:
                            Errors.Add(validationResult);
                            Debug.LogError($"{objectName} -> {currentType.Name}.{fieldInfo.Name} | {validationResult.Message}", @object);
                            break;
                    }
                }

                currentType = currentType.BaseType;
                ValidateReferencesToolbar.ReloadToolbar();
            }
        }

        private static void ValidateComponent(
                SerializedObject serializedObject, Component component, Type componentType, string gameObjectName,
                ref int totalFieldsChecked, ref int fieldsWithAttribute, ref int invalidReferences,
                HashSet<object> visitedObjects)
        {
            Type currentType = componentType != null ? component.GetType() : componentType;

            while (currentType != null && currentType != typeof(MonoBehaviour))
            {
                FieldInfo[] fieldInfos = currentType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                foreach (FieldInfo fieldInfo in fieldInfos)
                {
                    if (!fieldInfo.IsPublic &&
                            fieldInfo.GetCustomAttribute<SerializeField>() == null &&
                            fieldInfo.GetCustomAttribute<SerializeReference>() == null)
                    {
                        continue;
                    }

                    RequireReferenceAttribute requireReferenceAttr = fieldInfo.GetCustomAttribute<RequireReferenceAttribute>();
                    if (requireReferenceAttr == null)
                    {
                        continue;
                    }

                    SerializedProperty serializedProperty = serializedObject.FindProperty(fieldInfo.Name);
                    if (serializedProperty == null)
                    {
                        Debug.LogWarning($"{gameObjectName} -> {currentType.Name}HH.SerializableInterface.{fieldInfo.Name} | Serialized property not found");
                        continue;
                    }

                    totalFieldsChecked++;
                    fieldsWithAttribute++;

                    RequireReferenceData referenceData = new();
                    ValidationResult validationResult = ValidateSerializedProperty(serializedProperty, fieldInfo, referenceData, gameObjectName, ref invalidReferences, visitedObjects);

                    switch (validationResult.Status)
                    {
                        default:
                        case ValidationStatus.None:
                            break;

                        case ValidationStatus.Warning:
                            Warnings.Add(validationResult);
                            Debug.LogWarning($"{gameObjectName} -> {currentType.Name}.{fieldInfo.Name} | {validationResult.Message}", component);
                            break;
                        case ValidationStatus.Error:
                            Errors.Add(validationResult);
                            Debug.LogError($"{gameObjectName} -> {currentType.Name}.{fieldInfo.Name} | {validationResult.Message}", component);
                            break;
                    }
                }

                currentType = currentType.BaseType;
            }
        }

        private static ValidationResult ValidateSerializedProperty(SerializedProperty property, FieldInfo fieldInfo, RequireReferenceData referenceData,
                string gameObjectName, ref int invalidReferences, HashSet<object> visitedObjects)
        {
            referenceData.ValueGetter = () => { return property.GetValue(); };

            if (property.serializedObject.targetObject is not UnityEngine.Object @object)
            {
                throw new InvalidCastException("Attempted to validate a non serializable object");
            }

            referenceData.SerializingObject = @object;

            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                return ValidateObjectReference(property, referenceData, gameObjectName, fieldInfo, ref invalidReferences);
            }
            else if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                return ValidateManagedReference(property, referenceData, gameObjectName, fieldInfo, ref invalidReferences, visitedObjects);
            }
            else
            {
                return new ValidationResult(ValidationStatus.Warning, $"Not a reference field (type: {property.propertyType})", referenceData.NoticePopup);
            }
        }

        private static ValidationResult ValidateObjectReference(SerializedProperty property, RequireReferenceData referenceData,
                string gameObjectName, FieldInfo field, ref int invalidReferences)
        {
            UnityEngine.Object referencedObject = property.objectReferenceValue;
            Component componentSource = (Component)property.serializedObject.targetObject;
            referenceData.ValueGetter = () => { return field.GetValue(componentSource); };

            if (referencedObject == null || (referencedObject is UnityEngine.Object unityObj && !unityObj))
            {
                invalidReferences++;
                string message = referenceData.Message;
                // Debug.Log($"[6] {attribute.SerializingComponent} -> {field.GetValue(componentSource)} unknown");
                referenceData.Message = "Null reference detected\n" + referenceData.Message + $":\n\n{gameObjectName}.{componentSource.GetType().Name}.{field.Name} = null";

                ValidationResult validation = new ValidationResult(ValidationStatus.Error, $"NULL - {message}", referenceData.NoticePopup);
                referenceData.NoticePopup(validation);
                return validation;
            }

            if (!referencedObject)
            {
                invalidReferences++;
                string message = referenceData.Message;
                // Debug.Log($"[5] {attribute.SerializingComponent} -> {attribute.ValueGetter} unknown");
                referenceData.Message = "Invalid reference detected (destroyed object)\n" + referenceData.Message + $":\n\n{gameObjectName}.{componentSource.GetType().Name}.{field.Name} = null";

                ValidationResult validation = new ValidationResult(ValidationStatus.Error, $"Invalid reference (destroyed object) - {message}", referenceData.NoticePopup);
                referenceData.NoticePopup(validation);
                return validation;
            }

            if (EditorSceneManager.preventCrossSceneReferences && IsCrossSceneReference(referencedObject, property.serializedObject.targetObject))
            {
                return new ValidationResult(ValidationStatus.Warning, $"Cross-scene reference detected - {referenceData.Message}", referenceData.NoticePopup);
            }

            if (PrefabUtility.IsPartOfPrefabAsset(referencedObject) && !referencedObject)
            {
                invalidReferences++;
                string message = referenceData.Message;
                // Debug.Log($"[4] {attribute.SerializingComponent} -> {attribute.ValueGetter} {(attribute.ValueGetter != null ? referencedObject.name : "unknown")}");
                referenceData.Message = "Missing prefab reference detected\n" + referenceData.Message + $":\n\n{gameObjectName}.{componentSource.GetType().Name}.{field.Name} = null";
                ValidationResult validation = new ValidationResult(ValidationStatus.Error, $"Missing prefab reference - {message}", referenceData.NoticePopup);
                referenceData.NoticePopup(validation);
                return validation;
            }

            bool isSceneObject = !EditorUtility.IsPersistent(referencedObject);
            bool isPrefab = PrefabUtility.IsPartOfPrefabAsset(referencedObject);
            bool isPrefabInstance = PrefabUtility.IsPartOfPrefabInstance(referencedObject);

            string objectInfo = $"Type: {referencedObject.GetType().Name}";
            if (isSceneObject)
            {
                objectInfo += " (Scene Object)";
            }
            else if (isPrefab)
            {
                objectInfo += " (Prefab Asset)";
            }
            else if (isPrefabInstance)
            {
                objectInfo += " (Prefab Instance)";
            }

            return new ValidationResult(ValidationStatus.None, $"Valid reference - {objectInfo}", referenceData.NoticePopup);
        }

        private static ValidationResult ValidateManagedReference(SerializedProperty property, RequireReferenceData referenceData,
                string gameObjectName, FieldInfo field, ref int invalidReferences, HashSet<object> visitedObjects)
        {
            object managedReferenceValue = property.managedReferenceValue;

            if (managedReferenceValue == null)
            {
                string message = referenceData.Message;
                UnityEngine.Object objectSource = property.serializedObject.targetObject;
                // Debug.Log($"[3] {attribute.SerializingComponent} -> {attribute.ValueGetter} unknown");
                referenceData.Message = "Null reference detected\n" + referenceData.Message + $":\n\n{gameObjectName}.{objectSource.GetType().Name}.{field.Name} = null";

                ValidationResult validation = new ValidationResult(ValidationStatus.Error, $"NULL - {message}", referenceData.NoticePopup);
                referenceData.NoticePopup(validation);
                return validation;
            }

            if (visitedObjects.Contains(managedReferenceValue))
            {
                return new ValidationResult(ValidationStatus.Warning, $"Cyclic reference detected - {referenceData.Message}", referenceData.NoticePopup);
            }

            visitedObjects.Add(managedReferenceValue);

            Type actualType = managedReferenceValue.GetType();
            string objectInfo = $"Type: {actualType.Name}";

            // Check if it's a Unity object that might be destroyed
            if (managedReferenceValue is UnityEngine.Object unityObj)
            {
                if (!unityObj)
                {
                    string message = referenceData.Message;
                    // Debug.Log($"[2] {attribute.SerializingComponent} -> {attribute.ValueGetter} {(attribute.ValueGetter != null ? unityObj.name : "unknown")}");
                    Component componentSource = (Component)property.serializedObject.targetObject;
                    referenceData.Message = "Invalid Unity reference detected (destroyed object)\n" + referenceData.Message + $":\n\n{gameObjectName}.{componentSource.GetType().Name}.{field.Name} = null";
                    ValidationResult validation = new ValidationResult(ValidationStatus.Error, $"Invalid Unity reference (destroyed object) - {message}", referenceData.NoticePopup);
                    referenceData.NoticePopup(validation);
                    return validation;
                }
                else
                {
                    SerializedObject managedReferenceSerializedObject = new SerializedObject(managedReferenceValue as UnityEngine.Object);

                    // Get the type and validate its fields
                    int nestedInvalidCount = 0;
                    int fieldsChecked = 0;
                    int fieldsWithAttribute = 0;
                    ValidateComponent(managedReferenceSerializedObject, (Component)property.serializedObject.targetObject, actualType, $"{gameObjectName}.{field.Name}",
                            ref fieldsChecked, ref fieldsWithAttribute, ref nestedInvalidCount, visitedObjects);

                    if (nestedInvalidCount > 0)
                    {
                        invalidReferences += nestedInvalidCount;
                        Component componentSource = (Component)property.serializedObject.targetObject;
                        referenceData.Message = $"Invalid nested reference detected {nestedInvalidCount}\n{objectInfo}\n\n{gameObjectName}.{componentSource.GetType().Name}.{field.Name} = null";
                        ValidationResult validation = new ValidationResult(ValidationStatus.Error, $"Valid managed reference but has {nestedInvalidCount} invalid nested fields - {objectInfo}\n\n{gameObjectName}.{componentSource.GetType().Name}.{field.Name}", referenceData.NoticePopup);
                        referenceData.NoticePopup(validation);
                        return validation;
                    }
                }
            }
            else
            {
                List<FieldInfo> nestedFields = GetFieldsWithRequireReferenceAttribute(actualType);
                foreach (FieldInfo nestedField in nestedFields)
                {
                    object nestedValue = nestedField.GetValue(managedReferenceValue);
                    RequireReferenceData nestedReferenceData = new()
                    {
                        // RequireReferenceAttribute nestedAttribute = nestedField.GetCustomAttribute<RequireReferenceAttribute>();
                        ValueGetter = () => { return nestedField.GetValue(managedReferenceValue); },
                        SerializingObject = property.serializedObject.targetObject,
                    };

                    if (nestedValue == null || (nestedValue is UnityEngine.Object nestedUnityObj && !nestedUnityObj))
                    {
                        string logtest = nestedValue != null ? nestedValue.ToString() : "null";
                        invalidReferences++;
                        string message = nestedReferenceData.Message;
                        nestedReferenceData.Message = $"Invalid class reference detected from:\n{gameObjectName}.{field.Name}\n-> \n{actualType.Name}.{nestedField.Name}\n\n {nestedReferenceData.Message}";
                        ValidationResult validation = new ValidationResult(ValidationStatus.None, $"Valid managed reference (non-Unity object) - {objectInfo}", referenceData.NoticePopup);
                        nestedReferenceData.NoticePopup(validation);
                        Debug.LogError($"{gameObjectName}.{field.Name} -> {actualType.Name}.{nestedField.Name} | NULL - {message}", nestedReferenceData.SerializingObject);
                        return validation;
                    }
                }

                if (nestedFields.Count > 0)
                {
                    return new ValidationResult(ValidationStatus.None, $"Valid managed reference (non-Unity object) - {objectInfo}", referenceData.NoticePopup);
                }
            }

            visitedObjects.Remove(managedReferenceValue);

            return new ValidationResult(ValidationStatus.None, $"Valid managed reference - {objectInfo}", referenceData.NoticePopup);
        }

        private static List<FieldInfo> GetFieldsWithRequireReferenceAttribute(Type type)
        {
            List<FieldInfo> fields = new List<FieldInfo>();
            Type currentType = type;

            while (currentType != null)
            {
                FieldInfo[] fieldInfos = currentType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                foreach (FieldInfo field in fieldInfos)
                {
                    if (!field.IsPublic &&
                            field.GetCustomAttribute<SerializeField>() == null &&
                            field.GetCustomAttribute<SerializeReference>() == null)
                    {
                        continue;
                    }

                    if (field.GetCustomAttribute<RequireReferenceAttribute>() != null)
                    {
                        fields.Add(field);
                    }
                }

                currentType = currentType.BaseType;
            }

            return fields;
        }

        private static bool IsCrossSceneReference(UnityEngine.Object obj, UnityEngine.Object targetObject)
        {
            if (obj == null || targetObject == null)
            {
                return false;
            }

            GameObject objGo = GetGameObjectFromObject(obj);
            GameObject targetGo = GetGameObjectFromObject(targetObject);

            if (objGo == null || targetGo == null)
            {
                return false;
            }

            return objGo.scene != targetGo.scene && objGo.scene.IsValid() && targetGo.scene.IsValid();
        }

        private static GameObject GetGameObjectFromObject(UnityEngine.Object obj)
        {
            if (obj is GameObject go)
            {
                return go;
            }

            if (obj is Component component)
            {
                return component.gameObject;
            }

            return null;
        }
    }

    public enum ValidationStatus
    {
        None,
        Warning,
        Error,
    }

    public class ValidationResult
    {
        public ValidationResult(ValidationStatus status, string message, Action<ValidationResult> triggerPopup)
        {
            Status = status;
            Message = message;
            Popup = triggerPopup;
        }

        public ValidationStatus Status { get; }

        public string Message { get; }

        public Action<ValidationResult> Popup { get; }
    }
}
