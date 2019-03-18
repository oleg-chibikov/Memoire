using System;
using JetBrains.Annotations;
using Scar.Common;

namespace Remembrance.Contracts.ProcessMonitoring.Data
{
    public sealed class ProcessInfo
    {
        public ProcessInfo([NotNull] string name, [CanBeNull] string? filePath = null)
        {
            Name = name.Capitalize() ?? throw new ArgumentNullException(nameof(name));
            FilePath = filePath;
        }

        [CanBeNull]
        public string? FilePath { get; }

        [NotNull]
        public string Name { get; }

        public override bool Equals(object obj)
        {
            return obj is ProcessInfo processInfo && Equals(processInfo);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }

        private bool Equals([NotNull] ProcessInfo other)
        {
            return string.Equals(Name, other.Name, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}