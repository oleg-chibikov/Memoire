using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.Processing.Data;

namespace Remembrance.Contracts.CardManagement
{
    public interface IAssessmentInfoProvider
    {
        AssessmentInfo ProvideAssessmentInfo(TranslationInfo translationInfo);
    }
}