using JetBrains.Annotations;

namespace Remembrance.Card.Management.Data
{
    internal interface IExchangeEntry
    {
        [NotNull]
        string Text { get; }
    }
}