using System;
using JetBrains.Annotations;
using Remembrance.Contracts.ImageSearch.Data;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Scar.Common.DAL.Model;

namespace Remembrance.Contracts.DAL.Model
{
    public sealed class WordImageInfo : Entity<int>, IWord
    {
        [UsedImplicitly]
        public WordImageInfo()
        {
        }

        public WordImageInfo([NotNull] object translationEntryId, int searchIndex, [NotNull] IWord word, [CanBeNull] ImageInfoWithBitmap image)
        {
            if (word == null)
            {
                throw new ArgumentNullException(nameof(word));
            }

            TranslationEntryId = translationEntryId ?? throw new ArgumentNullException(nameof(translationEntryId));
            SearchIndex = searchIndex;
            Image = image;
            Text = word.Text;
            PartOfSpeech = word.PartOfSpeech;
        }

        [NotNull]
        public object TranslationEntryId { get; set; }

        public int SearchIndex { get; set; }

        [CanBeNull]
        public ImageInfoWithBitmap Image { get; set; }

        public string Text { get; set; }

        public PartOfSpeech PartOfSpeech { get; set; }

        public override string ToString()
        {
            return $"Image for {TranslationEntryId} - {Text} ({PartOfSpeech})";
        }
    }
}