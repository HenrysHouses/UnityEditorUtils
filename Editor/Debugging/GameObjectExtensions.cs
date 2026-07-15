#nullable disable

namespace HH.Debugging
{
    using UnityEngine;

    public static class GameObjectExtensions
    {
        public static string GetHierarchyPath(this GameObject obj)
        {
            string path = obj.name;
            Transform current = obj.transform.parent;

            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            string sceneName = GameObject.GetScene(obj.GetEntityId()).name;

            return $"{sceneName}/{path}";
        }
    }
}
