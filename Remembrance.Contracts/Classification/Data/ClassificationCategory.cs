using System;
using System.Diagnostics;

namespace Remembrance.Contracts.Classification.Data
{
    [DebuggerDisplay("{ClassName} ({Match})")]
    public class ClassificationCategory : IEquatable<ClassificationCategory>
    {
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public string ClassName { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

        public decimal Match { get; set; }

        public bool Equals(ClassificationCategory? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(ClassName, other.ClassName, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((ClassificationCategory)obj);
        }

        public override int GetHashCode()
        {
            // ReSharper disable once NonReadonlyMemberInGetHashCode
            return StringComparer.OrdinalIgnoreCase.GetHashCode(ClassName);
        }
    }
}
