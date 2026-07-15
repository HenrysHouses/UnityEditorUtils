#nullable disable
namespace HH.Attributes
{
    using UnityEngine;

    public class ReferenceInstancerOverrideAttribute : PropertyAttribute
    {
        public ReferenceInstancerOverrideAttribute(System.Type restrictedType)
        {
            TypeOverride = restrictedType;
        }

        public System.Type TypeOverride { get; set; } = null;
    }
}
