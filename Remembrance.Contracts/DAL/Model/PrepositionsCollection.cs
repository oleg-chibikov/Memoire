using JetBrains.Annotations;

namespace Remembrance.Contracts.DAL.Model
{
    public sealed class PrepositionsCollection
    {
        [CanBeNull]
        public string[] Texts
        {
            get;

            [UsedImplicitly]
            set;
        }

        public override string ToString()
        {
            return Texts != null
                ? string.Join("/", Texts)
                : "";
        }
    }
}