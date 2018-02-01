using System;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.DAL.Model;

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
        public TranslationEntryKey TranslationEntryKey { get; set; }

        [NotNull]
        public TranslationResultViewModel TranslationResult { get; set; }

        public override string ToString()
        {
            return $"Translation details for {TranslationEntryKey}";
        }
    }
}