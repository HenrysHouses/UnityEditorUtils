#nullable disable

namespace HH.Attributes
{
    using System;
    using UnityEngine;

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class ValidClassTypeAttribute : PropertyAttribute
    {
        public ValidClassTypeAttribute(Type baseType)
        {
            BaseType = baseType;
        }

        public Type BaseType { get; private set; }
    }
}
