namespace HH.Editor
{
    using HH.Attributes;
    using System;
    using System.Linq;
    using HH.Editor.Notices;
    using HH.Attributes;
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(RequireReferenceAttribute))]
    public class RequireReferenceData
    {
        public UnityEngine.Object SerializingObject { get; set; }

        public Func<object> ValueGetter { get; set; }

        public string Message { get; set; } = "This field cannot be null";

        public void NoticePopup(ValidationResult validationReference)
        {
            GUIContent content = new(Message);
            ButtonNoticePopup.CreateWindow(
                    MessageType.Error,
                    content,
                    "Find",
                    () => { Selection.activeObject = SerializingObject; },
                    CheckReference(ValueGetter),
                    validationReference,
                    null,
                    false);
        }

        private Func<bool> CheckReference(Func<object> fieldRefenrece)
        {
            return () =>
            {
                object value = fieldRefenrece.Invoke();

                if (value == null)
                {
                    return false;
                }

                if (value is UnityEngine.Object unityObj)
                {
                    if (!unityObj)
                    {
                        return false;
                    }

                    if (unityObj is GameObject go && go.GetComponents<Component>().Any(c => c == null))
                    {
                        return false;
                    }

                    if (PrefabUtility.IsPrefabAssetMissing(unityObj))
                    {
                        return false;
                    }
                }

                return true;
            };
        }
    }
}
