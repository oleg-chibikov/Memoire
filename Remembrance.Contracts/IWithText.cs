using JetBrains.Annotations;

namespace Remembrance.Contracts
{
    public interface IWithText
    {
        [NotNull]
        string Text { get; }
    }
}