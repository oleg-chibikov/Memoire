using System;
using System.Collections.Generic;
using Autofac;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.DAL.Model;

namespace Remembrance.ViewModel.Card
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class AssessmentViewOnlyCardViewModel : BaseAssessmentCardViewModel
    {
        public AssessmentViewOnlyCardViewModel(
            [NotNull] TranslationInfo translationInfo,
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] ISettingsRepository settingsRepository,
            [NotNull] IViewModelAdapter viewModelAdapter,
            [NotNull] IMessageHub messenger,
            [NotNull] IEqualityComparer<IWord> wordsEqualityComparer,
            [NotNull] ILog logger,
            [NotNull] ILifetimeScope lifetimeScope,
            [NotNull] IWordsProcessor wordsProcessor)
            : base(translationInfo, settingsRepository, viewModelAdapter, messenger, wordsEqualityComparer, logger, lifetimeScope)
        {
            if (settingsRepository == null)
            {
                throw new ArgumentNullException(nameof(settingsRepository));
            }

            if (viewModelAdapter == null)
            {
                throw new ArgumentNullException(nameof(viewModelAdapter));
            }

            Logger.Info($"Showing view-only card. Increasing repeat type for {TranslationInfo}...");
            TranslationInfo.TranslationEntry.IncreaseRepeatType();
            translationEntryRepository.Save(TranslationInfo.TranslationEntry);
            Messenger.Publish(TranslationInfo);
        }
    }
}