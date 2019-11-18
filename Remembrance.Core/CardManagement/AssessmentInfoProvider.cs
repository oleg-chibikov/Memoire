using System;
using System.Collections.Generic;
using System.Linq;
using Common.Logging;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Processing.Data;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Remembrance.Resources;
using Scar.Common.Exceptions;

namespace Remembrance.Core.CardManagement
{
    internal sealed class AssessmentInfoProvider : IAssessmentInfoProvider
    {
        private static readonly Random Random = new Random();

        private readonly ILog _logger;

        public AssessmentInfoProvider(ILog logger)
        {
            _logger = logger;
        }

        public AssessmentInfo ProvideAssessmentInfo(TranslationInfo translationInfo)
        {
            var repeatType = translationInfo.LearningInfo.RepeatType;
            var randomTranslation = repeatType >= RepeatType.Proficiency;
            var isReverse = IsReverse(repeatType);
            var translationResult = translationInfo.TranslationDetails.TranslationResult;
            var filteredPriorityPartOfSpeechTranslations =
                GetPartOfSpeechTranslationsWithRespectToPriority(translationResult, translationInfo.TranslationEntry, out var hasPriorityItems);
            var partOfSpeechGroup = SelectSinglePartOfSpeechGroup(randomTranslation, filteredPriorityPartOfSpeechTranslations);
            var acceptedWordGroups = GetAcceptedWordGroups(partOfSpeechGroup, translationInfo.TranslationEntry, hasPriorityItems);
            return isReverse ? GetReverseAssessmentInfo(randomTranslation, acceptedWordGroups) : GetStraightAssessmentInfo(acceptedWordGroups);
        }

        private static bool HasPriorityItems(TranslationVariant translationVariant, TranslationEntry translationEntry)
        {
            return IsPriority(translationVariant, translationEntry) || translationVariant.Synonyms?.Any(synonym => IsPriority(synonym, translationEntry)) == true;
        }

        private static bool HasPriorityItems(PartOfSpeechTranslation partOfSpeechTranslation, TranslationEntry translationEntry)
        {
            return partOfSpeechTranslation.TranslationVariants.Any(translationVariant => HasPriorityItems(translationVariant, translationEntry));
        }

        private static bool IsPriority(BaseWord word, TranslationEntry translationEntry)
        {
            return translationEntry.PriorityWords?.Contains(word) == true;
        }

        private static bool IsReverse(RepeatType repeatType)
        {
            var isReverse = false;
            if (repeatType >= RepeatType.Advanced)
            {
                isReverse = Random.Next(2) == 1;
            }

            return isReverse;
        }

        private IReadOnlyCollection<GroupingInfo> GetAcceptedWordGroups(
            IGrouping<PartOfSpeech, PartOfSpeechTranslation> partOfSpeechGroup,
            TranslationEntry translationEntry,
            bool translationEntryHasPriorityItems)
        {
            _logger.TraceFormat("Getting accepted words groups for {0}...", partOfSpeechGroup.Key);
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

            if (!acceptedWordGroups.Any())
            {
                throw new LocalizableException(Errors.NoAssessmentTranslations, "No translations found");
            }

            _logger.DebugFormat("There are {0} accepted words groups", acceptedWordGroups.Length);

            return acceptedWordGroups;
        }

        private IEnumerable<PartOfSpeechTranslation> GetPartOfSpeechTranslationsWithRespectToPriority(
            TranslationResult translationResult,
            TranslationEntry translationEntry,
            out bool hasPriorityItems)
        {
            _logger.Trace("Getting translations with respect to priority...");
            var priorityPartOfSpeechTranslations = translationResult.PartOfSpeechTranslations.ToList();
            priorityPartOfSpeechTranslations.RemoveAll(partOfSpeechTranslation => !HasPriorityItems(partOfSpeechTranslation, translationEntry));
            hasPriorityItems = priorityPartOfSpeechTranslations.Any();
            if (hasPriorityItems)
            {
                _logger.DebugFormat("There are {0} priority translations", priorityPartOfSpeechTranslations.Count);
                return priorityPartOfSpeechTranslations;
            }

            _logger.Debug("There are no priority translations");
            return translationResult.PartOfSpeechTranslations;
        }

        private IReadOnlyCollection<Word> GetPossibleTranslations(
            (TranslationVariant TranslationVariant, bool HasPriorityItems) translationVariantWithPriorityInfo,
            TranslationEntry translationEntry)
        {
            var (translationVariant, hasPriorityItems) = translationVariantWithPriorityInfo;
            _logger.TraceFormat("Getting accepted words groups for {0}...", translationVariant);
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

        private IGrouping<PartOfSpeech, PartOfSpeechTranslation> GetRandomPartOfSpeechGroup(
            IReadOnlyCollection<IGrouping<PartOfSpeech, PartOfSpeechTranslation>> partOfSpeechGroups)
        {
            _logger.Trace("Getting random part of speech group...");
            var randomPartOfSpeechGroupIndex = Random.Next(partOfSpeechGroups.Count);
            var result = partOfSpeechGroups.ElementAt(randomPartOfSpeechGroupIndex);
            return result;
        }

        private AssessmentInfo GetReverseAssessmentInfo(bool needRandom, IReadOnlyCollection<GroupingInfo> acceptedWordGroups)
        {
            _logger.Trace("Getting reverse assessment info...");
            return needRandom ? GetReverseAssessmentInfoFromRandomTranslation(acceptedWordGroups) : GetReverseAssessmentInfoFromFirstTranslation(acceptedWordGroups);
        }

        private AssessmentInfo GetReverseAssessmentInfoFromFirstTranslation(IEnumerable<GroupingInfo> acceptedWordGroups)
        {
            _logger.Trace("Getting info from first translation...");
            var acceptedWordGroup = acceptedWordGroups.First();
            var translation = acceptedWordGroup.Words.First();
            var correct = acceptedWordGroup.PartOfSpeechTranslation;
            return new AssessmentInfo(
                new HashSet<Word>
                {
                    correct
                },
                translation,
                correct,
                true,
                acceptedWordGroup.Synonyms);
        }

        private AssessmentInfo GetReverseAssessmentInfoFromRandomTranslation(IReadOnlyCollection<GroupingInfo> acceptedWordGroups)
        {
            _logger.Trace("Getting info from random translation...");
            var randomAcceptedWordGroupIndex = Random.Next(acceptedWordGroups.Count);
            var randomAcceptedWordGroup = acceptedWordGroups.ElementAt(randomAcceptedWordGroupIndex);
            var randomTranslationIndex = Random.Next(randomAcceptedWordGroup.Words.Count);
            var randomTranslation = randomAcceptedWordGroup.Words.ElementAt(randomTranslationIndex);
            var correct = randomAcceptedWordGroup.PartOfSpeechTranslation;
            return new AssessmentInfo(
                new HashSet<Word>
                {
                    correct
                },
                randomTranslation,
                correct,
                true,
                randomAcceptedWordGroup.Synonyms);
        }

        private AssessmentInfo GetStraightAssessmentInfo(IReadOnlyCollection<GroupingInfo> acceptedWordGroups)
        {
            _logger.Trace("Getting straight assessment info...");
            var acceptedAnswers = new HashSet<Word>(acceptedWordGroups.SelectMany(x => x.Words));
            var acceptedWordGroup = acceptedWordGroups.First();
            var word = acceptedWordGroup.PartOfSpeechTranslation;
            var correct = acceptedAnswers.First();
            return new AssessmentInfo(acceptedAnswers, word, correct, false, acceptedWordGroup.Meanings);
        }

        private IGrouping<PartOfSpeech, PartOfSpeechTranslation> SelectSinglePartOfSpeechGroup(bool randomPossible, IEnumerable<PartOfSpeechTranslation> partOfSpeechTranslations)
        {
            _logger.Trace("Selecting single part of speech group...");
            var partOfSpeechGroups = partOfSpeechTranslations.GroupBy(x => x.PartOfSpeech).ToArray();
            var partOfSpeechGroup = randomPossible ? GetRandomPartOfSpeechGroup(partOfSpeechGroups) : partOfSpeechGroups.First();
            if (partOfSpeechGroup == null)
            {
                throw new LocalizableException(Errors.NoAssessmentTranslations, "No translations found");
            }

            return partOfSpeechGroup;
        }

        private sealed class GroupingInfo
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