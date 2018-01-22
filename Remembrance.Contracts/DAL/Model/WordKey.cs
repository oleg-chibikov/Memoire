using System;
using JetBrains.Annotations;
using Remembrance.Contracts.Translate.Data.WordsTranslator;

namespace Remembrance.Contracts.DAL.Model
{
    public sealed class WordKey : IEquatable<WordKey>, IWord
    {
        [UsedImplicitly]
        public WordKey()
        {
        }

        public WordKey([NotNull] object translationEntryId, [NotNull] IWord word)
        {
            TranslationEntryId = translationEntryId ?? throw new ArgumentNullException(nameof(translationEntryId));
            Text = word?.Text ?? throw new ArgumentNullException(nameof(word));
            PartOfSpeech = word.PartOfSpeech;
        }

        public string Text { get; set; }

        [NotNull]
        public object TranslationEntryId { get; set; }

        public PartOfSpeech PartOfSpeech { get; set; }

        public bool Equals(WordKey other)
        {
            if (other == null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Text == other.Text && PartOfSpeech == other.PartOfSpeech && TranslationEntryId == other.TranslationEntryId;
        }

        public override bool Equals(object obj)
        {
            return obj is TranslationEntryKey key && Equals(key);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Text.GetHashCode();
                hashCode = (hashCode * 397) ^ TranslationEntryId.GetHashCode();
                hashCode = (hashCode * 397) ^ PartOfSpeech.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"{TranslationEntryId} - {Text} ({PartOfSpeech})";
        }
    }
}