using System.Windows;
using Mémoire.Contracts.CardManagement;
using Mémoire.Resources;
using Scar.Common.View.Contracts;
using Scar.Common.WPF.View.Contracts;

namespace Mémoire.Windows.Common
{
    public sealed class WindowPositionAdjustmentManager : IWindowPositionAdjustmentManager
    {
        public void AdjustAnyWindowPosition(IDisplayable window)
        {
            if (window is not IWindow wpfWindow)
            {
                return;
            }

            wpfWindow.Draggable = false;
            wpfWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            if (wpfWindow.AdvancedWindowStartupLocation == AdvancedWindowStartupLocation.Default)
            {
                wpfWindow.AdvancedWindowStartupLocation = AdvancedWindowStartupLocation.BottomRight;
            }

            wpfWindow.ShowActivated = false;
            wpfWindow.Topmost = true;
        }

        public void AdjustDetailsCardWindowPosition(IDisplayable window)
        {
            if (window is not IWindow wpfWindow)
            {
                return;
            }

            wpfWindow.AdvancedWindowStartupLocation = AdvancedWindowStartupLocation.TopRight;
            wpfWindow.Draggable = false;
            wpfWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            wpfWindow.ShowActivated = false;
            wpfWindow.Topmost = true;
            wpfWindow.AutoCloseTimeout = AppSettings.TranslationCardCloseTimeout;
        }

        public void AdjustActivatedWindow(IDisplayable window)
        {
            if (window is not IWindow wpfWindow)
            {
                return;
            }

            wpfWindow.ShowActivated = true;
        }
    }
}
