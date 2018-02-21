using System.Collections.Generic;
using JetBrains.Annotations;

namespace Remembrance.Contracts.DAL.Model
{
    public sealed class PrepositionsCollection
    {
        [CanBeNull]
        public ICollection<string> Texts { get; set; }

        public override string ToString()
        {
            return Texts != null
                ? string.Join("/", Texts)
                : "";
        }
    }
}