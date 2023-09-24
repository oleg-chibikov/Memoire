using Scar.Common.View.Contracts;

namespace Mémoire.Contracts.CardManagement
{
    public interface IWindowPositionAdjustmentManager
    {
        void AdjustAssessmentWindowPosition(IDisplayable window);

        void AdjustDetailsCardWindowPosition(IDisplayable window);

        void AdjustActivatedWindow(IDisplayable window);
    }
}
