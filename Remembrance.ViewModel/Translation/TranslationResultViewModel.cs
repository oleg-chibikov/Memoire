using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Translate.Data.WordsTranslator;

namespace Remembrance.ViewModel.Translation
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class TranslationResultViewModel
    {
        public TranslationResultViewModel([NotNull] TranslationResult translationResult, [NotNull] TranslationEntry translationEntry, [NotNull] ILifetimeScope lifetimeScope)
        {
            if (lifetimeScope == null)
            {
                throw new ArgumentNullException(nameof(lifetimeScope));
            }

            if (translationResult == null)
            {
                throw new ArgumentNullException(nameof(translationResult));
            }

            PartOfSpeechTranslations = translationResult.PartOfSpeechTranslations.Select(
                    partOfSpeechTranslation => lifetimeScope.Resolve<PartOfSpeechTranslationViewModel>(
                        new TypedParameter(typeof(PartOfSpeechTranslation), partOfSpeechTranslation),
                        new TypedParameter(typeof(TranslationEntry), translationEntry)))
                .ToArray();
        }

        [NotNull]
        public ICollection<PartOfSpeechTranslationViewModel> PartOfSpeechTranslations { get; set; }
    }
}