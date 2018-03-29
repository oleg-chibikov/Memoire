using System.Collections.Generic;
using JetBrains.Annotations;
using Remembrance.Contracts.Translate.Data.WordsTranslator;

namespace Remembrance.Contracts.CardManagement.Data
{
    public sealed class AssessmentInfo
    {
        public AssessmentInfo([NotNull] HashSet<Word> acceptedAnswers, [NotNull] Word word, [NotNull] Word correctAnswer, bool isReverse)
        {
            AcceptedAnswers = acceptedAnswers;
            Word = word;
            CorrectAnswer = correctAnswer;
            IsReverse = isReverse;
        }

        [NotNull]
        public HashSet<Word> AcceptedAnswers { get; }

        [NotNull]
        public Word CorrectAnswer { get; }

        [NotNull]
        public Word Word { get; }

        public bool IsReverse { get; }
    }
}
