using JetBrains.Annotations;

namespace Remembrance.Contracts.Translate.Data.WordsTranslator
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithMembers)]
    public class TextEntry
    {
        [NotNull]
        public virtual string Text { get; set; }

        public override string ToString()
        {
            return Text;
        }
    }
}