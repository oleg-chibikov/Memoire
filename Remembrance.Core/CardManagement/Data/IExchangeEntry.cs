using JetBrains.Annotations;

namespace Remembrance.Card.Management.CardManagement.Data
{
    internal interface IExchangeEntry
    {
        [NotNull]
        string Text { get; }
    }
}