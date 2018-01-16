using System;
using JetBrains.Annotations;
using PropertyChanged;

namespace Remembrance.ViewModel.Translation
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class TranslationDetailsViewModel
    {
        public TranslationDetailsViewModel([NotNull] TranslationResultViewModel translationResult)
        {
            TranslationResult = translationResult ?? throw new ArgumentNullException(nameof(translationResult));
        }

        [DoNotNotify]
        public object Id { get; set; }

        [DoNotNotify]
        public object TranslationEntryId { get; set; }

        [NotNull]
        public TranslationResultViewModel TranslationResult { get; set; }

        public override string ToString()
        {
            return $"Translation details for {TranslationEntryId}";
        }
    }
}