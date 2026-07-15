#nullable disable
namespace HH.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.Experimental.GraphView;
    using UnityEngine;

    public class ClassSearchProvider : ScriptableObject, ISearchWindowProvider
    {
        public ClassSearchProvider(Type classObjectSearch, SerializedProperty property)
        {
            ClassObjectSearch = classObjectSearch;
            SerializedProperty = property;
        }

        public Type ClassObjectSearch { get; set; }

        public SerializedProperty SerializedProperty { get; set; }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            List<SearchTreeEntry> searchList = new List<SearchTreeEntry>();

            // TODO make groups for derived classes under the search terms
            Type[] entryTitle = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract && ClassObjectSearch.IsAssignableFrom(t))
                .ToArray();

            // Type[] entryTitle = classObjectSearch.Assembly.GetTypes()
            //         .Where(t => t.IsClass && !t.IsAbstract && classObjectSearch.IsAssignableFrom(t))
            //         .ToArray();
            GUIContent uIContent = new GUIContent();
            if (ClassObjectSearch.IsInterface)
            {
                uIContent.text = "Implementing: " + ClassObjectSearch.Name;
            }
            else
            {
                uIContent.text = "Deriving: " + ClassObjectSearch.Name; // uses less space than "Classes that derive from"
            }

            searchList.Add(new SearchTreeGroupEntry(uIContent, 0));

            SearchTreeEntry nullEntry = new SearchTreeEntry(new GUIContent("Null"))
            {
                level = 1,
                userData = null,
            };

            // EditorGUIUtility.ObjectContent() to get the icon if we wanted
            searchList.Add(nullEntry);

            foreach (Type item in entryTitle)
            {
                SearchTreeEntry entry = new SearchTreeEntry(new GUIContent(item.Name))
                {
                    level = 1,
                    userData = item,
                };

                // EditorGUIUtility.ObjectContent() to get the icon if we wanted
                searchList.Add(entry);
            }

            return searchList;
        }

        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            MonoScript targetMonoScript = null;
            MonoScript[] monoScripts = MonoImporter.GetAllRuntimeMonoScripts();
            foreach (MonoScript monoScript in monoScripts)
            {
                if (monoScript.GetClass() == (Type)SearchTreeEntry.userData)
                {
                    targetMonoScript = monoScript;
                    break;
                }
            }

            SerializedProperty.serializedObject.Update();
            SerializedProperty.managedReferenceValue = !SearchTreeEntry.name.Equals("Null") ? Activator.CreateInstance(targetMonoScript.GetClass()) : null;

            SerializedProperty.serializedObject.ApplyModifiedProperties();
            return true;
        }
    }
}
