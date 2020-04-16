using System.Threading.Tasks;
using Remembrance.Contracts.Processing.Data;
using Scar.Common.View.Contracts;

namespace Remembrance.Contracts.CardManagement
{
    public interface ITranslationDetailsCardManager
    {
        Task ShowCardAsync(TranslationInfo translationInfo, IDisplayable? ownerWindow = null);
    }
}
