using JetBrains.Annotations;
using Remembrance.DAL.Contracts.Model;
using Scar.Common.WPF.View.Contracts;

namespace Remembrance.Card.Management.Contracts
{
    public interface ITranslationResultCardManager
    {
        void ShowCard([NotNull] TranslationInfo translationInfo, [CanBeNull] IWindow ownerWindow = null);
    }
}