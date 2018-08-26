using System;
using JetBrains.Annotations;
using Remembrance.Contracts.Translate.Data.WordsTranslator;

// ReSharper disable NotNullMemberIsNotInitialized
namespace Remembrance.Contracts.DAL.Model
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithMembers)]
    public sealed class ManualTranslation : BaseWord
    {
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
        public string Example { get; set; }

        [NotNull]
        public string Meaning { get; set; }
    }
}