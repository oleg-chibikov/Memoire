using JetBrains.Annotations;
using Remembrance.DAL.Contracts.Model;

namespace Remembrance.Card.Management.Contracts
{
    public interface ITranslationResultCardManager
    {
        void ShowCard([NotNull] TranslationInfo translationInfo);
    }
}