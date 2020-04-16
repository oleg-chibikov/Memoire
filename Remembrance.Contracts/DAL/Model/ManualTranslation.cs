using System;
using Remembrance.Contracts.Translate.Data.WordsTranslator;

// ReSharper disable NotNullMemberIsNotInitialized
namespace Remembrance.Contracts.DAL.Model
{
    public sealed class ManualTranslation : BaseWord
    {
        // ReSharper disable once UnusedMember.Global
        public ManualTranslation()
        {
            Example = Meaning = Text = string.Empty;
        }

        public ManualTranslation(string text, string example = "", string meaning = "", PartOfSpeech partOfSpeech = PartOfSpeech.Unknown)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            Example = example ?? throw new ArgumentNullException(nameof(example));
            Meaning = meaning ?? throw new ArgumentNullException(nameof(meaning));
            PartOfSpeech = partOfSpeech;
        }

        public string Example { get; set; }

        public string Meaning { get; set; }
    }
}
