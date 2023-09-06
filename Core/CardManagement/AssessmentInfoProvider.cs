using System;
using System.Collections.Generic;
using System.Linq;
using Mémoire.Contracts.CardManagement;
using Mémoire.Contracts.CardManagement.Data;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.Processing.Data;
using Mémoire.Resources;
using Microsoft.Extensions.Logging;
using Scar.Common.Exceptions;
using Scar.Services.Contracts.Data.Translation;

namespace Mémoire.Core.CardManagement
{
    public sealed class AssessmentInfoProvider : IAssessmentInfoProvider
    {
        static readonly Random Random = new ();
        readonly ILogger _logger;

        public AssessmentInfoProvider(ILogger<AssessmentInfoProvider> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            logger.LogTrace("Initializing {Type}...", GetType().Name);
            logger.LogDebug("Initialized {Type}", GetType().Name);
        }

        public AssessmentInfo ProvideAssessmentInfo(TranslationInfo translationInfo)
        {
            _ = translationInfo ?? throw new ArgumentNullException(nameof(translationInfo));

            var repeatType = translationInfo.LearningInfo.RepeatType;
            var randomTranslation = repeatType >= RepeatType.Proficiency;
            var isReverse = IsReverse(repeatType);
            var translationResult = translationInfo.TranslationDetails.TranslationResult;
            var filteredPriorityPartOfSpeechTranslations = GetPartOfSpeechTranslationsWithRespectToPriority(translationResult, translationInfo.TranslationEntry, out var hasPriorityItems);
            var partOfSpeechGroup = SelectSinglePartOfSpeechGroup(randomTranslation, filteredPriorityPartOfSpeechTranslations);
            var acceptedWordGroups = GetAcceptedWordGroups(partOfSpeechGroup, translationInfo.TranslationEntry, hasPriorityItems);
            return isReverse ? GetReverseAssessmentInfo(randomTranslation, acceptedWordGroups) : GetStraightAssessmentInfo(acceptedWordGroups);
        }

        static bool HasPriorityItems(TranslationVariant translationVariant, TranslationEntry translationEntry)
        {
            return IsPriority(translationVariant, translationEntry) || (translationVariant.Synonyms?.Any(synonym => IsPriority(synonym, translationEntry)) == true);
        }

        static bool HasPriorityItems(PartOfSpeechTranslation partOfSpeechTranslation, TranslationEntry translationEntry)
        {
            return partOfSpeechTranslation.TranslationVariants.Any(translationVariant => HasPriorityItems(translationVariant, translationEntry));
        }

        static bool IsPriority(BaseWord word, TranslationEntry translationEntry)
        {
            return translationEntry.PriorityWords?.Contains(word) == true;
        }

        static bool IsReverse(RepeatType repeatType)
        {
            var isReverse = false;
            if (repeatType >= RepeatType.Advanced)
            {
                isReverse = Random.Next(2) == 1;
            }

            return isReverse;
        }

        IReadOnlyCollection<GroupingInfo> GetAcceptedWordGroups(
            IGrouping<PartOfSpeech, PartOfSpeechTranslation> partOfSpeechGroup,
            TranslationEntry translationEntry,
            bool translationEntryHasPriorityItems)
        {
            _logger.LogTrace("Getting accepted words groups for {PartOfSpeechGroupKey}...", partOfSpeechGroup.Key);
            var acceptedWordGroups = partOfSpeechGroup.SelectMany(
                    partOfSpeechTranslation => partOfSpeechTranslation.TranslationVariants.Select(
                            translationVariant =>
                            {
                                var hasPriorityItems = translationEntryHasPriorityItems && HasPriorityItems(translationVariant, translationEntry);
                                return (TranslationVariant: translationVariant, HasPriorityItems: hasPriorityItems);
                            })
                        .Where(translationVariantWithPriorityInfo => !translationEntryHasPriorityItems || translationVariantWithPriorityInfo.HasPriorityItems)
                        .Select(
                            translationVariantWithPriorityInfo => new GroupingInfo(
                                partOfSpeechTranslation,
                                GetPossibleTranslations(translationVariantWithPriorityInfo, translationEntry),
                                translationVariantWithPriorityInfo.TranslationVariant.Meanings ?? Enumerable.Empty<Word>(),
                                translationVariantWithPriorityInfo.TranslationVariant.Synonyms ?? Enumerable.Empty<Word>())))
                .ToArray();

            if (!(acceptedWordGroups.Length > 0))
            {
                throw new LocalizableException(Errors.NoAssessmentTranslations, "No translations found");
            }

            _logger.LogDebug("There are {AcceptedWordGroupCount} accepted words groups", acceptedWordGroups.Length);

            return acceptedWordGroups;
        }

        IEnumerable<PartOfSpeechTranslation> GetPartOfSpeechTranslationsWithRespectToPriority(TranslationResult translationResult, TranslationEntry translationEntry, out bool hasPriorityItems)
        {
            _logger.LogTrace("Getting translations with respect to priority...");
            var priorityPartOfSpeechTranslations = translationResult.PartOfSpeechTranslations.ToList();
            priorityPartOfSpeechTranslations.RemoveAll(partOfSpeechTranslation => !HasPriorityItems(partOfSpeechTranslation, translationEntry));
            hasPriorityItems = priorityPartOfSpeechTranslations.Count > 0;
            if (hasPriorityItems)
            {
                _logger.LogDebug("There are {PriorityPartOfSpeechTranslationCount} priority translations", priorityPartOfSpeechTranslations.Count);
                return priorityPartOfSpeechTranslations;
            }

            _logger.LogDebug("There are no priority translations");
            return translationResult.PartOfSpeechTranslations;
        }

        IReadOnlyCollection<Word> GetPossibleTranslations((TranslationVariant TranslationVariant, bool HasPriorityItems) translationVariantWithPriorityInfo, TranslationEntry translationEntry)
        {
            var (translationVariant, hasPriorityItems) = translationVariantWithPriorityInfo;
            _logger.LogTrace("Getting accepted words groups for {TranslationVariant}...", translationVariant);
            IEnumerable<Word> result = new Word[]
            {
                translationVariant
            };
            if (translationVariant.Synonyms != null)
            {
                result = result.Concat(translationVariant.Synonyms.Select(synonym => synonym));
            }

            if (hasPriorityItems)
            {
                result = result.OrderByDescending(word => IsPriority(word, translationEntry));
            }

            return result.ToArray();
        }

        IGrouping<PartOfSpeech, PartOfSpeechTranslation> GetRandomPartOfSpeechGroup(IReadOnlyCollection<IGrouping<PartOfSpeech, PartOfSpeechTranslation>> partOfSpeechGroups)
        {
            _logger.LogTrace("Getting random part of speech group...");
            var randomPartOfSpeechGroupIndex = Random.Next(partOfSpeechGroups.Count);
            var result = partOfSpeechGroups.ElementAt(randomPartOfSpeechGroupIndex);
            return result;
        }

        AssessmentInfo GetReverseAssessmentInfo(bool needRandom, IReadOnlyCollection<GroupingInfo> acceptedWordGroups)
        {
            _logger.LogTrace("Getting reverse assessment info...");
            return needRandom ? GetReverseAssessmentInfoFromRandomTranslation(acceptedWordGroups) : GetReverseAssessmentInfoFromFirstTranslation(acceptedWordGroups);
        }

        AssessmentInfo GetReverseAssessmentInfoFromFirstTranslation(IEnumerable<GroupingInfo> acceptedWordGroups)
        {
            _logger.LogTrace("Getting info from first translation...");
            var acceptedWordGroup = acceptedWordGroups.First();
            var translation = acceptedWordGroup.Words.First();
            var correct = acceptedWordGroup.PartOfSpeechTranslation;
            return new AssessmentInfo(new HashSet<Word> { correct }, translation, correct, true, acceptedWordGroup.Synonyms);
        }

        AssessmentInfo GetReverseAssessmentInfoFromRandomTranslation(IReadOnlyCollection<GroupingInfo> acceptedWordGroups)
        {
            _logger.LogTrace("Getting info from random translation...");
            var randomAcceptedWordGroupIndex = Random.Next(acceptedWordGroups.Count);
            var randomAcceptedWordGroup = acceptedWordGroups.ElementAt(randomAcceptedWordGroupIndex);
            var randomTranslationIndex = Random.Next(randomAcceptedWordGroup.Words.Count);
            var randomTranslation = randomAcceptedWordGroup.Words.ElementAt(randomTranslationIndex);
            var correct = randomAcceptedWordGroup.PartOfSpeechTranslation;
            return new AssessmentInfo(new HashSet<Word> { correct }, randomTranslation, correct, true, randomAcceptedWordGroup.Synonyms);
        }

        AssessmentInfo GetStraightAssessmentInfo(IReadOnlyCollection<GroupingInfo> acceptedWordGroups)
        {
            _logger.LogTrace("Getting straight assessment info...");
            var acceptedAnswers = new HashSet<Word>(acceptedWordGroups.SelectMany(x => x.Words));
            var acceptedWordGroup = acceptedWordGroups.First();
            var word = acceptedWordGroup.PartOfSpeechTranslation;
            var correct = acceptedAnswers.First();
            return new AssessmentInfo(acceptedAnswers, word, correct, false, acceptedWordGroup.Meanings);
        }

        IGrouping<PartOfSpeech, PartOfSpeechTranslation> SelectSinglePartOfSpeechGroup(bool randomPossible, IEnumerable<PartOfSpeechTranslation> partOfSpeechTranslations)
        {
            _logger.LogTrace("Selecting single part of speech group...");
            var partOfSpeechGroups = partOfSpeechTranslations.GroupBy(x => x.PartOfSpeech).ToArray();
            var partOfSpeechGroup = randomPossible ? GetRandomPartOfSpeechGroup(partOfSpeechGroups) : partOfSpeechGroups[0];
            if (partOfSpeechGroup == null)
            {
                throw new LocalizableException(Errors.NoAssessmentTranslations, "No translations found");
            }

            return partOfSpeechGroup;
        }

        sealed class GroupingInfo
        {
            public GroupingInfo(PartOfSpeechTranslation partOfSpeechTranslation, IReadOnlyCollection<Word> words, IEnumerable<Word> meanings, IEnumerable<Word> synonyms)
            {
                PartOfSpeechTranslation = partOfSpeechTranslation ?? throw new ArgumentNullException(nameof(partOfSpeechTranslation));
                Words = words ?? throw new ArgumentNullException(nameof(words));
                Meanings = meanings ?? throw new ArgumentNullException(nameof(meanings));
                Synonyms = synonyms ?? throw new ArgumentNullException(nameof(synonyms));
            }

            public IEnumerable<Word> Meanings { get; }

            public IEnumerable<Word> Synonyms { get; }

            public PartOfSpeechTranslation PartOfSpeechTranslation { get; }

            public IReadOnlyCollection<Word> Words { get; }
        }
    }
}
