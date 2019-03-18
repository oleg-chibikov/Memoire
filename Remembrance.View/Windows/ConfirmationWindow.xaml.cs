using System;
using JetBrains.Annotations;
using Remembrance.Contracts.View;
using Remembrance.ViewModel;

namespace Remembrance.View.Windows
{
    internal sealed partial class ConfirmationWindow : IConfirmationWindow
    {
        public ConfirmationWindow([NotNull] ConfirmationViewModel confirmationViewModel)
        {
            InitializeComponent();
            DataContext = confirmationViewModel ?? throw new ArgumentNullException(nameof(confirmationViewModel));
        }
    }
}