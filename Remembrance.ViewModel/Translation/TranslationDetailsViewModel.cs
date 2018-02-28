using System;
using Autofac;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Processing.Data;
using Remembrance.Contracts.Translate.Data.WordsTranslator;

namespace Remembrance.ViewModel.Translation
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class TranslationDetailsViewModel
    {
        private readonly TranslationEntryKey _translationEntryKey;

        public TranslationDetailsViewModel([NotNull] ILifetimeScope lifetimeScope, [NotNull] TranslationInfo translationInfo)
        {
            if (lifetimeScope == null)
            {
                throw new ArgumentNullException(nameof(lifetimeScope));
            }

            if (translationInfo == null)
            {
                throw new ArgumentNullException(nameof(translationInfo));
            }

            _translationEntryKey = translationInfo.TranslationEntryKey;
            TranslationResult = lifetimeScope.Resolve<TranslationResultViewModel>(
                new TypedParameter(typeof(TranslationResult), translationInfo.TranslationDetails.TranslationResult),
                new TypedParameter(typeof(TranslationEntry), translationInfo.TranslationEntry));
        }

        [NotNull]
        public TranslationResultViewModel TranslationResult { get; }

        public override string ToString()
        {
            return $"Translation details for {_translationEntryKey}";
        }
    }
}