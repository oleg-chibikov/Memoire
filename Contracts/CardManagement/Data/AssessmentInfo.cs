using System;
using System.Collections.Generic;
using System.Linq;
using Scar.Services.Contracts.Data.Translation;

namespace Mémoire.Contracts.CardManagement.Data
{
    public sealed class AssessmentInfo
    {
        public AssessmentInfo(HashSet<Word> acceptedAnswers, Word word, Word correctAnswer, bool isReverse, IEnumerable<Word> synonyms)
        {
            AcceptedAnswers = acceptedAnswers ?? throw new ArgumentNullException(nameof(acceptedAnswers));
            Word = word ?? throw new ArgumentNullException(nameof(word));
            CorrectAnswer = correctAnswer ?? throw new ArgumentNullException(nameof(correctAnswer));
            IsReverse = isReverse;
            Synonyms = synonyms?.Where(x => !x.Equals(word)).Distinct() ?? throw new ArgumentNullException(nameof(synonyms));
        }

        public HashSet<Word> AcceptedAnswers { get; }

        public Word CorrectAnswer { get; }

        public bool IsReverse { get; }

        public Word Word { get; }

        public IEnumerable<Word> Synonyms { get; }
    }
}
