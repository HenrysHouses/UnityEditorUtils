#nullable disable
namespace HH.EditorExtensions
{
    using System;
    using UnityEditor;

    public static class MonoScriptUtility
    {
        public static MonoScript FindMonoScriptFromType(Type type)
        {
            MonoScript[] monoScripts = MonoImporter.GetAllRuntimeMonoScripts();
            foreach (MonoScript monoScript in monoScripts)
            {
                if (monoScript.GetClass() == type)
                {
                    return monoScript;
                }
            }

            return null;
        }
    }
}
