using JetBrains.Annotations;

namespace Remembrance.Core.CardManagement.Data
{
    internal interface IExchangeEntry
    {
        [NotNull]
        string Text { get; }
    }
}