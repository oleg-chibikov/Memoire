using Scar.Common.View.Contracts;

namespace Remembrance.Contracts.CardManagement
{
    public interface IWindowPositionAdjustmentManager
    {
        void AdjustAnyWindowPosition(IDisplayable Window);

        void AdjustDetailsCardWindowPosition(IDisplayable Window);

        void AdjustActivatedWindow(IDisplayable Window);
    }
}
