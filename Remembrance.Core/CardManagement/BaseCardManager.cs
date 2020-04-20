using System;
using System.Threading;
using Common.Logging;
using Remembrance.Contracts.CardManagement;
using Scar.Common.View.Contracts;

namespace Remembrance.Core.CardManagement
{
    abstract class BaseCardManager
    {
        protected BaseCardManager(
            ILog logger,
            SynchronizationContext synchronizationContext,
            IWindowPositionAdjustmentManager windowPositionAdjustmentManager)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            SynchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
            WindowPositionAdjustmentManager = windowPositionAdjustmentManager ?? throw new ArgumentNullException(nameof(windowPositionAdjustmentManager));
        }

        protected ILog Logger { get; }

        protected SynchronizationContext SynchronizationContext { get; }

        protected IWindowPositionAdjustmentManager WindowPositionAdjustmentManager { get; }

        protected void ShowWindow(IDisplayable window)
        {
            // CultureUtilities.ChangeCulture(LocalSettingsRepository.UiLanguage);
            SynchronizationContext.Send(
                x =>
                {
                    WindowPositionAdjustmentManager.AdjustAnyWindowPosition(window);
                    window.Restore();
                },
                null);
        }
    }
}
