using System;
using JetBrains.Annotations;
using Remembrance.Contracts.ImageSearch.Data;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Scar.Common.DAL.Model;

namespace Remembrance.Contracts.DAL.Model
{
    public sealed class WordImagesInfo : Entity<int>, IWord
    {
        [UsedImplicitly]
        public WordImagesInfo()
        {
        }

        public WordImagesInfo([NotNull] object translationEntryId, [NotNull] string text, PartOfSpeech partOfSpeech, [NotNull] ImageInfoWithBitmap[] images)
        {
            TranslationEntryId = translationEntryId ?? throw new ArgumentNullException(nameof(translationEntryId));
            Images = images ?? throw new ArgumentNullException(nameof(images));
            Text = text ?? throw new ArgumentNullException(nameof(text));
            PartOfSpeech = partOfSpeech;
        }

        [NotNull]
        public object TranslationEntryId { get; set; }

        [NotNull]
        public ImageInfoWithBitmap[] Images { get; set; }

        public string Text { get; }
        public PartOfSpeech PartOfSpeech { get; }

        public override string ToString()
        {
            return $"Images for {TranslationEntryId} - {Text} ({PartOfSpeech})";
        }
    }
}