using System.Threading.Tasks;
using JetBrains.Annotations;
using Remembrance.Contracts.Processing.Data;
using Scar.Common.WPF.View.Contracts;

namespace Remembrance.Contracts.CardManagement
{
    public interface ITranslationDetailsCardManager
    {
        [NotNull]
        Task ShowCardAsync([NotNull] TranslationInfo translationInfo, [CanBeNull] IWindow ownerWindow = null);
    }
}