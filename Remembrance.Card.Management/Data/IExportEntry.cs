using JetBrains.Annotations;

namespace Remembrance.Card.Management.Data
{
    internal interface IExportEntry
    {
        [NotNull]
        string Text { get; }
    }
}