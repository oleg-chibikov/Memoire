using System.Diagnostics.CodeAnalysis;

namespace Mémoire.Contracts.CardManagement
{
    [SuppressMessage("Design", "CA1040:Avoid empty interfaces", Justification = "Empty interface is OK here")]
    public interface IAssessmentCardManager
    {
        void Pause(string title);

        void ResetInterval();
    }
}
