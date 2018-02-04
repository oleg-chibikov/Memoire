using System;
using JetBrains.Annotations;
using Remembrance.Contracts.Translate.Data.WordsTranslator;

namespace Remembrance.Contracts.DAL.Model
{
    public sealed class ManualTranslation : BaseWord
    {
        private string _example;
        private string _meaning;

        [UsedImplicitly]
        public ManualTranslation()
        {
        }

        public ManualTranslation([NotNull] string text, [NotNull] string example = "", [NotNull] string meaning = "", PartOfSpeech partOfSpeech = PartOfSpeech.Unknown)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            Example = example;
            Meaning = meaning;
            PartOfSpeech = partOfSpeech;
        }

        [NotNull]
        public string Example
        {
            get => _example;
            set => _example = value ?? "";
        }

        [NotNull]
        public string Meaning
        {
            get => _meaning;
            set => _meaning = value ?? "";
        }
    }
}