using Mémoire.Contracts.CardManagement.Data;
using Mémoire.Contracts.Processing.Data;

namespace Mémoire.Contracts.CardManagement
{
    public interface IAssessmentInfoProvider
    {
        AssessmentInfo ProvideAssessmentInfo(TranslationInfo translationInfo);
    }
}
