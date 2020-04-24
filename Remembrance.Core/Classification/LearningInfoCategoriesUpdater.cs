using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Remembrance.Contracts;
using Remembrance.Contracts.Classification;
using Remembrance.Contracts.Classification.Data;
using Remembrance.Contracts.DAL.SharedBetweenMachines;
using Remembrance.Contracts.Processing.Data;

namespace Remembrance.Core.Classification
{
    sealed class LearningInfoCategoriesUpdater : ILearningInfoCategoriesUpdater
    {
        const decimal MinMatchThreshold = 0.4M;
        readonly ILearningInfoRepository _learningInfoRepository;
        readonly IClassificationClient _classificationClient;

        public LearningInfoCategoriesUpdater(ILearningInfoRepository learningInfoRepository, IClassificationClient classificationClient)
        {
            _learningInfoRepository = learningInfoRepository ?? throw new ArgumentNullException(nameof(learningInfoRepository));
            _classificationClient = classificationClient ?? throw new ArgumentNullException(nameof(classificationClient));
        }

        public async Task<IReadOnlyCollection<ClassificationCategory>> UpdateLearningInfoClassificationCategoriesAsync(TranslationInfo translationInfo, CancellationToken cancellationToken)
        {
            // This will replace old categories if min threshold changes
            if (translationInfo.LearningInfo.ClassificationCategories == null ||
                translationInfo.LearningInfo.ClassificationCategories.Count == 0 ||
                translationInfo.LearningInfo.ClassificationCategories.Any(x => x.Match < MinMatchThreshold))
            {
                var classificationCategories = await GetClassificationCategoriesAsync(translationInfo, cancellationToken);
                translationInfo.LearningInfo.ClassificationCategories = classificationCategories.Where(x => x.Match >= MinMatchThreshold).ToArray();
                _learningInfoRepository.Update(translationInfo.LearningInfo);
            }

            return translationInfo.LearningInfo.ClassificationCategories;
        }

        async Task<IEnumerable<ClassificationCategory>?> GetClassificationCategoriesAsync(TranslationInfo translationInfo, CancellationToken cancellationToken)
        {
            IEnumerable<ClassificationCategory>? classificationCategories = null;
            if (translationInfo.TranslationEntryKey.SourceLanguage == Constants.EnLanguageTwoLetters)
            {
                classificationCategories = await _classificationClient.GetCategoriesAsync(translationInfo.TranslationEntryKey.Text, cancellationToken);
            }
            else if (translationInfo.TranslationEntryKey.TargetLanguage == Constants.EnLanguageTwoLetters && translationInfo.TranslationDetails.TranslationResult.PartOfSpeechTranslations.Count > 0)
            {
                var firstPartOfSpeechTranslation = translationInfo.TranslationDetails.TranslationResult.PartOfSpeechTranslations.First();
                if (firstPartOfSpeechTranslation.TranslationVariants.Count > 0)
                {
                    classificationCategories = await _classificationClient.GetCategoriesAsync(firstPartOfSpeechTranslation.TranslationVariants.First().Text, cancellationToken);
                }
            }

            return classificationCategories;
        }
    }
}
