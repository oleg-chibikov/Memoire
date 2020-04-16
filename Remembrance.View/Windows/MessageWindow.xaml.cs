using System;
using Remembrance.Contracts.View.Settings;
using Remembrance.ViewModel;

namespace Remembrance.View.Windows
{
    sealed partial class MessageWindow : IMessageWindow
    {
        public MessageWindow(MessageViewModel viewModel)
        {
            DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            InitializeComponent();
        }
    }
}