using System;
using Mémoire.Contracts.View.Settings;
using Mémoire.ViewModel;

namespace Mémoire.View.Windows
{
    public sealed partial class MessageWindow : IMessageWindow
    {
        public MessageWindow(MessageViewModel viewModel)
        {
            DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            InitializeComponent();
        }
    }
}
