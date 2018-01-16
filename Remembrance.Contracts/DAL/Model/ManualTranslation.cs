using System;
using JetBrains.Annotations;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Scar.Common;

namespace Remembrance.Contracts.DAL.Model
{
    public sealed class ManualTranslation : IWord
    {
        private string _example;
        private string _meaning;
        private string _text;

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

        public string Text
        {
            get => _text;
            set => _text = value.Capitalize();
        }

        public PartOfSpeech PartOfSpeech { get; set; }

        public override string ToString()
        {
            return $"{Text} ({PartOfSpeech})";
        }
    }
}