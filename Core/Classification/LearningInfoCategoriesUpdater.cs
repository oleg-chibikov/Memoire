using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Easy.MessageHub;
using Mémoire.Contracts.Classification;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.DAL.SharedBetweenMachines;
using Mémoire.Contracts.Processing.Data;
using Mémoire.Resources;
using Scar.Common.Messages;
using Scar.Services.Contracts;
using Scar.Services.Contracts.Data;
using Scar.Services.Contracts.Data.Classification;

namespace Mémoire.Core.Classification
{
    sealed class LearningInfoCategoriesUpdater : ILearningInfoCategoriesUpdater
    {
        readonly ISharedSettingsRepository _sharedSettingsRepository;
        readonly ILearningInfoRepository _learningInfoRepository;
        readonly IClassificationClient _classificationClient;
        readonly IMessageHub _messageHub;

        public LearningInfoCategoriesUpdater(
            ILearningInfoRepository learningInfoRepository,
            IClassificationClient classificationClient,
            IMessageHub messageHub,
            ISharedSettingsRepository sharedSettingsRepository)
        {
            _learningInfoRepository = learningInfoRepository ?? throw new ArgumentNullException(nameof(learningInfoRepository));
            _classificationClient = classificationClient ?? throw new ArgumentNullException(nameof(classificationClient));
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            _sharedSettingsRepository = sharedSettingsRepository ?? throw new ArgumentNullException(nameof(sharedSettingsRepository));
        }

        public async Task UpdateLearningInfoClassificationCategoriesAsync(TranslationInfo translationInfo, CancellationToken cancellationToken)
        {
            var minThreshold = _sharedSettingsRepository.ClassificationMinimalThreshold;

            // This will replace old categories if min threshold changes
            if ((translationInfo.LearningInfo.ClassificationCategories == null) || (Math.Abs(translationInfo.LearningInfo.ClassificationCategories.MinMatchThreshold - minThreshold) >= 0.01))
            {
                var classificationCategories = await GetClassificationCategoriesAsync(translationInfo, cancellationToken).ConfigureAwait(false);
                translationInfo.LearningInfo.ClassificationCategories = new LearningInfoClassificationCategories { Items = classificationCategories, MinMatchThreshold = minThreshold };

                _learningInfoRepository.Update(translationInfo.LearningInfo);
            }
        }

        async Task<IEnumerable<ClassificationCategory>> GetClassificationCategoriesAsync(TranslationInfo translationInfo, CancellationToken cancellationToken)
        {
            string? text = null;
            if (translationInfo.TranslationEntryKey.SourceLanguage == LanguageConstants.EnLanguageTwoLetters)
            {
                text = translationInfo.TranslationEntryKey.Text;
            }
            else if (translationInfo.TranslationEntryKey.TargetLanguage == LanguageConstants.EnLanguageTwoLetters)
            {
                if (translationInfo.TranslationEntry.PriorityWords?.Count > 0)
                {
                    text = translationInfo.TranslationEntry.PriorityWords.First().Text;
                }
                else
                {
                    if (translationInfo.TranslationDetails.TranslationResult.PartOfSpeechTranslations.Count > 0)
                    {
                        var firstPartOfSpeechTranslation = translationInfo.TranslationDetails.TranslationResult.PartOfSpeechTranslations.First();
                        if (firstPartOfSpeechTranslation.TranslationVariants.Count > 0)
                        {
                            text = firstPartOfSpeechTranslation.TranslationVariants.First().Text;
                        }
                    }
                }
            }

            if (text != null)
            {
                return await _classificationClient.GetCategoriesAsync(text, null, null, ex => _messageHub.Publish(Errors.CannotCategorize.ToError(ex)), cancellationToken).ConfigureAwait(false);
            }

            return Enumerable.Empty<ClassificationCategory>();
        }
    }
}
