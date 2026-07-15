#nullable enable
namespace HH.SerializedType
{
    using UnityEngine;
    using System;

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class TypeSelectAttribute : PropertyAttribute
    {
        public Type BaseType { get; private set; }

        public TypeSelectAttribute(System.Type baseType)
        {
            BaseType = baseType;
        }
    }
}
