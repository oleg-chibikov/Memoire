using JetBrains.Annotations;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.Processing.Data;

namespace Remembrance.Contracts.CardManagement
{
    public interface IAssessmentInfoProvider
    {
        [NotNull]
        AssessmentInfo ProvideAssessmentInfo([NotNull] TranslationInfo translationInfo);
    }
}
