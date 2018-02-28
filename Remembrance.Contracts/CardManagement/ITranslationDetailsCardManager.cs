using JetBrains.Annotations;
using Remembrance.Contracts.Processing.Data;
using Scar.Common.WPF.View.Contracts;

namespace Remembrance.Contracts.CardManagement
{
    public interface ITranslationDetailsCardManager
    {
        void ShowCard([NotNull] TranslationInfo translationInfo, [CanBeNull] IWindow ownerWindow = null);
    }
}