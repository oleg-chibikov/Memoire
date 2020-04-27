using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Remembrance.Contracts;
using Remembrance.Contracts.Classification;
using Remembrance.Contracts.Classification.Data;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.SharedBetweenMachines;
using Remembrance.Contracts.Processing.Data;

namespace Remembrance.Core.Classification
{
    sealed class LearningInfoCategoriesUpdater : ILearningInfoCategoriesUpdater
    {
        const decimal MinMatchThreshold = 0.15M;
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
                translationInfo.LearningInfo.ClassificationCategories.MinMatchThreshold != MinMatchThreshold)
            {
                var classificationCategories = await GetClassificationCategoriesAsync(translationInfo, cancellationToken).ConfigureAwait(false);
                translationInfo.LearningInfo.ClassificationCategories = new LearningInfoClassificationCategories { Items = classificationCategories, MinMatchThreshold = MinMatchThreshold };

                _learningInfoRepository.Update(translationInfo.LearningInfo);
                return classificationCategories;
            }

            return translationInfo.LearningInfo.ClassificationCategories?.Items ?? Array.Empty<ClassificationCategory>();
        }

        async Task<IReadOnlyCollection<ClassificationCategory>> GetClassificationCategoriesAsync(TranslationInfo translationInfo, CancellationToken cancellationToken)
        {
            string? text = null;
            if (translationInfo.TranslationEntryKey.SourceLanguage == Constants.EnLanguageTwoLetters)
            {
                text = translationInfo.TranslationEntryKey.Text;
            }
            else if (translationInfo.TranslationEntryKey.TargetLanguage == Constants.EnLanguageTwoLetters)
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
                var classificationCategories = await _classificationClient.GetCategoriesAsync(text, null, cancellationToken).ConfigureAwait(false);
                var limitedCategories = classificationCategories.Where(x => x.Match >= MinMatchThreshold);
                var tasks = limitedCategories.Select(category => GetInnerCategoriesAsync(category, text, cancellationToken));
                var results = await Task.WhenAll(tasks).ConfigureAwait(false);
                return results.SelectMany(x => x).OrderByDescending(x => x.Match).ToArray();
            }

            return Array.Empty<ClassificationCategory>();
        }

        async Task<IReadOnlyCollection<ClassificationCategory>> GetInnerCategoriesAsync(ClassificationCategory category, string text, CancellationToken cancellationToken)
        {
            var currentCategories = new List<ClassificationCategory> { category };
            var innerClassifier = category.ClassName.TrimEnd('s') + " Topics";
            var innerCategories = await _classificationClient.GetCategoriesAsync(text, innerClassifier, cancellationToken).ConfigureAwait(false);
            var limitedInnerCategories = innerCategories.Where(x => x.Match >= MinMatchThreshold);
            foreach (var innerCategory in limitedInnerCategories)
            {
                currentCategories.Add(innerCategory);
            }

            return currentCategories;
        }
    }
}
