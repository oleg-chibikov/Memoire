using System.Threading.Tasks;
using Mémoire.Contracts.Processing.Data;
using Scar.Common.View.Contracts;

namespace Mémoire.Contracts.CardManagement
{
    public interface ITranslationDetailsCardManager
    {
        Task ShowCardAsync(TranslationInfo translationInfo, IDisplayable? ownerWindow = null);
    }
}
