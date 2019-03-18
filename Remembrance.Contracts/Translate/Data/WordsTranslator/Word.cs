using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Model;

namespace Remembrance.Contracts.Translate.Data.WordsTranslator
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithMembers)]
    public class Word : BaseWord
    {
        [CanBeNull]
        public string? NounAnimacy { get; set; }

        [CanBeNull]
        public string? NounGender { get; set; }

        [CanBeNull]
        public string? VerbType { get; set; }
    }
}