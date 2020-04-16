using System;
using Remembrance.Contracts.View;
using Remembrance.ViewModel;

namespace Remembrance.View.Windows
{
    sealed partial class ConfirmationWindow : IConfirmationWindow
    {
        public ConfirmationWindow(ConfirmationViewModel confirmationViewModel)
        {
            InitializeComponent();
            DataContext = confirmationViewModel ?? throw new ArgumentNullException(nameof(confirmationViewModel));
        }
    }
}
