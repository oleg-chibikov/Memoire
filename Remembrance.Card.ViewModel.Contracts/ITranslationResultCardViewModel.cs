using JetBrains.Annotations;
using Remembrance.Card.ViewModel.Contracts.Data;

namespace Remembrance.Card.ViewModel.Contracts
{
    public interface ITranslationResultCardViewModel
    {
        [NotNull]
        TranslationDetailsViewModel TranslationDetails { get; }

        [NotNull]
        string Word { get; }
    }
}