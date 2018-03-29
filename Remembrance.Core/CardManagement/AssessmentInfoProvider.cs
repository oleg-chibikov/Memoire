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
            var filteredPriorityPartOfSpeechTranslations = FilterPriorityPartOfSpeechTranslations(translationResult, translationInfo.TranslationEntry);
            var partOfSpeechGroup = SelectSinglePartOfSpeechGroup(randomTranslation, filteredPriorityPartOfSpeechTranslations);
            var acceptedWordGroups = GetAcceptedWordGroups(partOfSpeechGroup, translationInfo.TranslationEntry);
            return isReverse ? GetReverseAssessmentInfo(randomTranslation, acceptedWordGroups) : GetStraightAssessmentInfo(acceptedWordGroups);
        }

        /// <summary>
        /// Decide whether reverse translation is needed
        /// </summary>
        private static bool IsReverse(RepeatType repeatType)
        {
            var isReverse = false;
            if (repeatType >= RepeatType.Advanced)
            {
                isReverse = Random.Next(2) == 1;
            }

            return isReverse;
        }

        private bool IsPriority([NotNull] Word word, [NotNull] TranslationEntry translationEntry)
        {
            return translationEntry.PriorityWords?.Contains(word) == true;
        }

        /// <summary>
        /// If there are any priority translations - leave only them, otherwise leave all.
        /// </summary>
        [NotNull]
        private ICollection<KeyValuePair<PartOfSpeechTranslation, ICollection<Word>>> FilterAcceptedWordsGroupsByPriority(
            [NotNull] ICollection<KeyValuePair<PartOfSpeechTranslation, ICollection<Word>>> acceptedWordGroups,
            [NotNull] TranslationEntry translationEntry)
        {
            _logger.Trace("Filtering accepted words groups by priority...");
            var tmp = new List<KeyValuePair<PartOfSpeechTranslation, ICollection<Word>>>();

            foreach (var acceptedWordGroup in acceptedWordGroups)
            {
                var lst = acceptedWordGroup.Value.ToList();
                lst.RemoveAll(word => !IsPriority(word, translationEntry));
                if (lst.Any())
                {
                    tmp.Add(new KeyValuePair<PartOfSpeechTranslation, ICollection<Word>>(acceptedWordGroup.Key, lst.ToArray()));
                }
            }

            if (tmp.Any())
            {
                _logger.DebugFormat("There are {0} groups that contain priority translations. Filtering was applied", tmp.Count);
                return tmp.ToArray();
            }

            _logger.Debug("There are no groups that contain priority translations. Filtering was not applied");
            return acceptedWordGroups;
        }

        /// <summary>
        /// If there are any priority translations - leave only their part of speech groups, otherwise leave all.
        /// </summary>
        [NotNull]
        private ICollection<PartOfSpeechTranslation> FilterPriorityPartOfSpeechTranslations([NotNull] TranslationResult translationResult, [NotNull] TranslationEntry translationEntry)
        {
            _logger.Trace("Filtering translations by priority...");
            var priorityPartOfSpeechTranslations = translationResult.PartOfSpeechTranslations.ToList();
            priorityPartOfSpeechTranslations.RemoveAll(
                partOfSpeechTranslation => !partOfSpeechTranslation.TranslationVariants.Any(
                    translationVariant => IsPriority(translationVariant, translationEntry) || translationVariant.Synonyms?.Any(synonym => IsPriority(synonym, translationEntry)) == true));
            var hasPriorityItems = priorityPartOfSpeechTranslations.Any();
            if (hasPriorityItems)
            {
                _logger.DebugFormat("There are {0} priority translations. Filtering was applied", priorityPartOfSpeechTranslations.Count);
                return priorityPartOfSpeechTranslations;
            }

            _logger.Debug("There are no priority translations. Filtering was not applied");
            return translationResult.PartOfSpeechTranslations;
        }

        /// <summary>
        /// Get all possible original word variants of this part of speech
        /// </summary>
        [NotNull]
        private ICollection<KeyValuePair<PartOfSpeechTranslation, ICollection<Word>>> GetAcceptedWordGroups(
            [NotNull] IGrouping<PartOfSpeech, PartOfSpeechTranslation> partOfSpeechGroup,
            [NotNull] TranslationEntry translationEntry)
        {
            _logger.TraceFormat("Getting accepted words groups for {0}...", partOfSpeechGroup.Key);
            ICollection<KeyValuePair<PartOfSpeechTranslation, ICollection<Word>>> acceptedWordGroups = partOfSpeechGroup.SelectMany(
                    partOfSpeechTranslation => partOfSpeechTranslation.TranslationVariants.Select(traslationVariant => GetPossibleTranslations(traslationVariant, partOfSpeechTranslation)))
                .ToArray();
            if (!acceptedWordGroups.Any())
            {
                throw new LocalizableException(Errors.NoAssessmentTranslations, "No translations found");
            }

            _logger.DebugFormat("There are {0} accepted words groups", acceptedWordGroups.Count);
            acceptedWordGroups = FilterAcceptedWordsGroupsByPriority(acceptedWordGroups, translationEntry);

            return acceptedWordGroups.Select(acceptedWordGroup => new KeyValuePair<PartOfSpeechTranslation, ICollection<Word>>(acceptedWordGroup.Key, acceptedWordGroup.Value)).ToArray();
        }

        /// <summary>
        /// Get all possible translations (including synonyms)
        /// </summary>
        private KeyValuePair<PartOfSpeechTranslation, ICollection<Word>> GetPossibleTranslations([NotNull] TranslationVariant traslationVariant, [NotNull] PartOfSpeechTranslation partOfSpeechTranslation)
        {
            _logger.TraceFormat("Getting accepted words groups for {0}...", traslationVariant);
            var result = new Word[]
            {
                traslationVariant
            };
            if (traslationVariant.Synonyms != null)
            {
                result = result.Concat(traslationVariant.Synonyms.Select(synonym => synonym)).ToArray();
            }

            return new KeyValuePair<PartOfSpeechTranslation, ICollection<Word>>(partOfSpeechTranslation, result);
        }

        /// <summary>
        /// Get the most suitable part of speech group according to POS priorities
        /// </summary>
        [NotNull]
        private IGrouping<PartOfSpeech, PartOfSpeechTranslation> GetRandomPartOfSpeechGroup([NotNull] ICollection<IGrouping<PartOfSpeech, PartOfSpeechTranslation>> partOfSpeechGroups)
        {
            _logger.Trace("Getting random part of speech group...");
            var randomPartOfSpeechGroupIndex = Random.Next(partOfSpeechGroups.Count);
            var result = partOfSpeechGroups.ElementAt(randomPartOfSpeechGroupIndex);
            return result;
        }

        /// <summary>
        /// Decide whether the Word would be chosen randomly or not
        /// </summary>
        [NotNull]
        private AssessmentInfo GetReverseAssessmentInfo(bool needRandom, [NotNull] ICollection<KeyValuePair<PartOfSpeechTranslation, ICollection<Word>>> acceptedWordGroups)
        {
            _logger.Trace("Getting reverse assessment info...");
            return needRandom ? GetReverseAssessmentInfoFromRandomTranslation(acceptedWordGroups) : GetReverseAssessmentInfoFromFirstTranslation(acceptedWordGroups);
        }

        /// <summary>
        /// The first variant for part of speech will be selected and the first translation inside it
        /// </summary>
        [NotNull]
        private AssessmentInfo GetReverseAssessmentInfoFromFirstTranslation([NotNull] ICollection<KeyValuePair<PartOfSpeechTranslation, ICollection<Word>>> acceptedWordGroups)
        {
            _logger.Trace("Getting info from first translation...");
            var acceptedWordGroup = acceptedWordGroups.First();
            var translation = acceptedWordGroup.Value.First();
            var correct = acceptedWordGroup.Key;
            return new AssessmentInfo(
                new HashSet<Word>
                {
                    correct
                },
                translation,
                correct,
                true);
        }

        /// <summary>
        /// Random variant for part of speech will be selected and random translation inside it
        /// </summary>
        [NotNull]
        private AssessmentInfo GetReverseAssessmentInfoFromRandomTranslation([NotNull] ICollection<KeyValuePair<PartOfSpeechTranslation, ICollection<Word>>> acceptedWordGroups)
        {
            _logger.Trace("Getting info from random translation...");
            var randomAcceptedWordGroupIndex = Random.Next(acceptedWordGroups.Count);
            var randomAcceptedWordGroup = acceptedWordGroups.ElementAt(randomAcceptedWordGroupIndex);
            var randomTranslationIndex = Random.Next(randomAcceptedWordGroup.Value.Count);
            var randomTranslation = randomAcceptedWordGroup.Value.ElementAt(randomTranslationIndex);
            var correct = randomAcceptedWordGroup.Key;
            return new AssessmentInfo(
                new HashSet<Word>
                {
                    correct
                },
                randomTranslation,
                correct,
                true);
        }

        /// <summary>
        /// Settings the first variant in the group as the Word and all possible variants as the Acceptable answers
        /// </summary>
        [NotNull]
        private AssessmentInfo GetStraightAssessmentInfo([NotNull] ICollection<KeyValuePair<PartOfSpeechTranslation, ICollection<Word>>> acceptedWordGroups)
        {
            _logger.Trace("Getting straight assessment info...");
            var accept = new HashSet<Word>(acceptedWordGroups.SelectMany(acceptedWordGroup => acceptedWordGroup.Value));
            var word = acceptedWordGroups.First().Key;
            var correct = accept.First();
            return new AssessmentInfo(accept, word, correct, false);
        }

        /// <summary>
        /// Choose the single part of speech group
        /// </summary>
        [NotNull]
        private IGrouping<PartOfSpeech, PartOfSpeechTranslation> SelectSinglePartOfSpeechGroup(bool randomPossible, [NotNull] ICollection<PartOfSpeechTranslation> partOfSpeechTranslations)
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
    }
}