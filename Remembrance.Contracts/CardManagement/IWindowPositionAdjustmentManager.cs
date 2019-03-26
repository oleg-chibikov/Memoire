using JetBrains.Annotations;
using Scar.Common.View.Contracts;

namespace Remembrance.Contracts.CardManagement
{
    public interface IWindowPositionAdjustmentManager
    {
        void AdjustAnyWindowPosition([NotNull] IDisplayable window);

        void AdjustDetailsCardWindowPosition([NotNull] IDisplayable window);

        void AdjustActivatedWindow([NotNull] IDisplayable window);
    }
}
