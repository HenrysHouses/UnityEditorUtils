#nullable disable
namespace HH.Attributes
{
    using UnityEngine;

    public class ReferenceInstancerAttribute : PropertyAttribute
    {
        public ReferenceInstancerAttribute()
        {
        }

        public ReferenceInstancerAttribute(System.Type restrictedType)
        {
            TypeRestriction = restrictedType;
        }

        public System.Type TypeRestriction { get; set; } = null;
    }
}
