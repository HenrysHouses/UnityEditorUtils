#nullable enable
namespace HH.SerializedType
{
    using System;
    using UnityEngine;

    [Serializable]
    public class SerializedType
    {
        /// <summary>
        /// This value is equal to Type.AssemblyQualifiedName
        /// Do not use this variable unless you know what you are doing. Use SelectedType instead.
        /// </summary>
        [field: SerializeField, HideInInspector] public string QualifiedName;
        private readonly Type defaultType;

        public SerializedType()
        {
            QualifiedName = string.Empty;

            if (defaultType != null)
            {
                SelectedType = defaultType;
                // defaultType = defaultType;
            }
            else
            {
                SelectedType = typeof(UnityEngine.Object);
                defaultType = typeof(UnityEngine.Object);
            }
        }

        public SerializedType(Type defaultType)
        {
            QualifiedName = string.Empty;
            SelectedType = defaultType;
            this.defaultType = defaultType;
        }

        public Type SelectedType
        {
            get => (QualifiedName is not "Null" and not "None") ? Type.GetType(QualifiedName) : defaultType;
            set => QualifiedName = value != null ? value.AssemblyQualifiedName : defaultType.Name;
        }

        public static bool operator ==(SerializedType left, SerializedType right)
        {
            if (left is not null)
            {
                return left.Equals(right);
            }

            return right is null;
        }

        public static bool operator !=(SerializedType left, SerializedType right)
        {
            return !(left == right);
        }

        // Equality based on SelectedType
        public override bool Equals(object obj)
        {
            return obj is SerializedType other && Equals(other);
        }

        public bool Equals(SerializedType other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Equals(SelectedType, other.SelectedType);
        }

        public override int GetHashCode()
        {
            return SelectedType?.GetHashCode() ?? 0;
        }
    }
}
