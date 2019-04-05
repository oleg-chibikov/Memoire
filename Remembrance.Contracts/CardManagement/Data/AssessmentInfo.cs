using System;
using System.Collections.Generic;
using Remembrance.Contracts.Translate.Data.WordsTranslator;

namespace Remembrance.Contracts.CardManagement.Data
{
    public sealed class AssessmentInfo
    {
        public AssessmentInfo(HashSet<Word> acceptedAnswers, Word word, Word correctAnswer, bool isReverse)
        {
            AcceptedAnswers = acceptedAnswers ?? throw new ArgumentNullException(nameof(acceptedAnswers));
            Word = word ?? throw new ArgumentNullException(nameof(word));
            CorrectAnswer = correctAnswer ?? throw new ArgumentNullException(nameof(correctAnswer));
            IsReverse = isReverse;
        }

        public HashSet<Word> AcceptedAnswers { get; }

        public Word CorrectAnswer { get; }

        public bool IsReverse { get; }

        public Word Word { get; }
    }
}