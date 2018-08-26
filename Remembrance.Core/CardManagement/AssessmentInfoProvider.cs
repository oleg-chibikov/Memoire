using System;
using System.Collections.Generic;
using System.Linq;
using Common.Logging;
using JetBrains.Annotations;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Processing.Data;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Remembrance.Resources;
using Scar.Common.Exceptions;

namespace Remembrance.Core.CardManagement
{
    [UsedImplicitly]
    internal sealed class AssessmentInfoProvider : IAssessmentInfoProvider
    {
        [NotNull]
        private static readonly Random Random = new Random();

        [NotNull]
        private readonly ILog _logger;

        public AssessmentInfoProvider([NotNull] ILog logger)
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

        private static bool HasPriorityItems([NotNull] TranslationVariant translationVariant, [NotNull] TranslationEntry translationEntry)
        {
            return IsPriority(translationVariant, translationEntry) || translationVariant.Synonyms?.Any(synonym => IsPriority(synonym, translationEntry)) == true;
        }

        private static bool HasPriotityItems([NotNull] PartOfSpeechTranslation partOfSpeechTranslation, [NotNull] TranslationEntry translationEntry)
        {
            return partOfSpeechTranslation.TranslationVariants.Any(translationVariant => HasPriorityItems(translationVariant, translationEntry));
        }

        private static bool IsPriority([NotNull] BaseWord word, [NotNull] TranslationEntry translationEntry)
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

        [NotNull]
        private ICollection<GroupingInfo> GetAcceptedWordGroups(
            [NotNull] IGrouping<PartOfSpeech, PartOfSpeechTranslation> partOfSpeechGroup,
            [NotNull] TranslationEntry translationEntry,
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
                                GetPossibleTranslations(translationVariantWithPriorityInfo, translationEntry))))
                .ToArray();

            if (!acceptedWordGroups.Any())
            {
                throw new LocalizableException(Errors.NoAssessmentTranslations, "No translations found");
            }

            _logger.DebugFormat("There are {0} accepted words groups", acceptedWordGroups.Length);

            return acceptedWordGroups;
        }

        [NotNull]
        private ICollection<PartOfSpeechTranslation> GetPartOfSpeechTranslationsWithRespectToPriority(
            [NotNull] TranslationResult translationResult,
            [NotNull] TranslationEntry translationEntry,
            out bool hasPriorityItems)
        {
            _logger.Trace("Getting translations with respect to priority...");
            var priorityPartOfSpeechTranslations = translationResult.PartOfSpeechTranslations.ToList();
            priorityPartOfSpeechTranslations.RemoveAll(partOfSpeechTranslation => !HasPriotityItems(partOfSpeechTranslation, translationEntry));
            hasPriorityItems = priorityPartOfSpeechTranslations.Any();
            if (hasPriorityItems)
            {
                _logger.DebugFormat("There are {0} priority translations", priorityPartOfSpeechTranslations.Count);
                return priorityPartOfSpeechTranslations;
            }

            _logger.Debug("There are no priority translations");
            return translationResult.PartOfSpeechTranslations;
        }

        [NotNull]
        private ICollection<Word> GetPossibleTranslations(
            (TranslationVariant TranslationVariant, bool HasPriorityItems) translationVariantWithPriorityInfo,
            [NotNull] TranslationEntry translationEntry)
        {
            var translationVariant = translationVariantWithPriorityInfo.TranslationVariant;
            _logger.TraceFormat("Getting accepted words groups for {0}...", translationVariant);
            IEnumerable<Word> result = new Word[]
            {
                translationVariant
            };
            if (translationVariant.Synonyms != null)
            {
                result = result.Concat(translationVariant.Synonyms.Select(synonym => synonym));
            }

            if (translationVariantWithPriorityInfo.HasPriorityItems)
            {
                result = result.OrderByDescending(word => IsPriority(word, translationEntry));
            }

            return result.ToArray();
        }

        [NotNull]
        private IGrouping<PartOfSpeech, PartOfSpeechTranslation> GetRandomPartOfSpeechGroup(
            [NotNull] ICollection<IGrouping<PartOfSpeech, PartOfSpeechTranslation>> partOfSpeechGroups)
        {
            _logger.Trace("Getting random part of speech group...");
            var randomPartOfSpeechGroupIndex = Random.Next(partOfSpeechGroups.Count);
            var result = partOfSpeechGroups.ElementAt(randomPartOfSpeechGroupIndex);
            return result;
        }

        [NotNull]
        private AssessmentInfo GetReverseAssessmentInfo(bool needRandom, [NotNull] ICollection<GroupingInfo> acceptedWordGroups)
        {
            _logger.Trace("Getting reverse assessment info...");
            return needRandom ? GetReverseAssessmentInfoFromRandomTranslation(acceptedWordGroups) : GetReverseAssessmentInfoFromFirstTranslation(acceptedWordGroups);
        }

        [NotNull]
        private AssessmentInfo GetReverseAssessmentInfoFromFirstTranslation([NotNull] ICollection<GroupingInfo> acceptedWordGroups)
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
                true);
        }

        [NotNull]
        private AssessmentInfo GetReverseAssessmentInfoFromRandomTranslation([NotNull] ICollection<GroupingInfo> acceptedWordGroups)
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
                true);
        }

        [NotNull]
        private AssessmentInfo GetStraightAssessmentInfo([NotNull] ICollection<GroupingInfo> acceptedWordGroups)
        {
            _logger.Trace("Getting straight assessment info...");
            var acceptedAnswers = new HashSet<Word>(acceptedWordGroups.SelectMany(acceptedWordGroup => acceptedWordGroup.Words));
            var word = acceptedWordGroups.First().PartOfSpeechTranslation;
            var correct = acceptedAnswers.First();
            return new AssessmentInfo(acceptedAnswers, word, correct, false);
        }

        [NotNull]
        private IGrouping<PartOfSpeech, PartOfSpeechTranslation> SelectSinglePartOfSpeechGroup(
            bool randomPossible,
            [NotNull] ICollection<PartOfSpeechTranslation> partOfSpeechTranslations)
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
            public GroupingInfo([NotNull] PartOfSpeechTranslation partOfSpeechTranslation, [NotNull] ICollection<Word> words)
            {
                PartOfSpeechTranslation = partOfSpeechTranslation ?? throw new ArgumentNullException(nameof(partOfSpeechTranslation));
                Words = words ?? throw new ArgumentNullException(nameof(words));
            }

            [NotNull]
            public PartOfSpeechTranslation PartOfSpeechTranslation { get; }

            [NotNull]
            public ICollection<Word> Words { get; }
        }
    }
}