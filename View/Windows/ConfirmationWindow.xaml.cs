using System;
using Mémoire.Contracts.View;
using Mémoire.ViewModel;

namespace Mémoire.View.Windows
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
