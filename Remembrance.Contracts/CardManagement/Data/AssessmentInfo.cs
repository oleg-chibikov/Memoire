using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Remembrance.Contracts.Translate.Data.WordsTranslator;

namespace Remembrance.Contracts.CardManagement.Data
{
    public sealed class AssessmentInfo
    {
        public AssessmentInfo([NotNull] HashSet<Word> acceptedAnswers, [NotNull] Word word, [NotNull] Word correctAnswer, bool isReverse)
        {
            AcceptedAnswers = acceptedAnswers ?? throw new ArgumentNullException(nameof(acceptedAnswers));
            Word = word ?? throw new ArgumentNullException(nameof(word));
            CorrectAnswer = correctAnswer ?? throw new ArgumentNullException(nameof(correctAnswer));
            IsReverse = isReverse;
        }

        [NotNull]
        public HashSet<Word> AcceptedAnswers { get; }

        [NotNull]
        public Word CorrectAnswer { get; }

        public bool IsReverse { get; }

        [NotNull]
        public Word Word { get; }
    }
}