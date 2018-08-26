using System.Collections.Generic;
using JetBrains.Annotations;

// ReSharper disable NotNullMemberIsNotInitialized
namespace Remembrance.Contracts.Translate.Data.WordsTranslator
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithMembers)]
    public sealed class PartOfSpeechTranslation : Word
    {
        public bool IsManual { get; set; }

        [CanBeNull]
        public string Transcription { get; set; }

        [NotNull]
        public ICollection<TranslationVariant> TranslationVariants { get; set; }
    }
}