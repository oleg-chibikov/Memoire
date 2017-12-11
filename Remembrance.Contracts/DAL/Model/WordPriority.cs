using System;
using JetBrains.Annotations;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Scar.Common.DAL.Model;

namespace Remembrance.Contracts.DAL.Model
{
    public sealed class WordPriority : Entity, IWord
    {
        [UsedImplicitly]
        public WordPriority()
        {
        }

        public WordPriority([NotNull] string text, PartOfSpeech partOfSpeech, [NotNull] object translationEntryId)
        {
            TranslationEntryId = translationEntryId ?? throw new ArgumentNullException(nameof(translationEntryId));
            Text = text ?? throw new ArgumentNullException(nameof(text));
            PartOfSpeech = partOfSpeech;
        }

        [NotNull]
        public object TranslationEntryId
        {
            get;
            [UsedImplicitly]
            set;
        }

        [NotNull]
        public string Text
        {
            get;
            [UsedImplicitly]
            set;
        }

        public PartOfSpeech PartOfSpeech
        {
            get;
            [UsedImplicitly]
            set;
        }

        public override string ToString()
        {
            return $"{Text} ({PartOfSpeech})";
        }
    }
}