using System;
using JetBrains.Annotations;
using Remembrance.Contracts.View.Settings;
using Remembrance.ViewModel.Settings;

namespace Remembrance.View.Settings
{
    /// <summary>
    /// The message window.
    /// </summary>
    [UsedImplicitly]
    internal sealed partial class MessageWindow : IMessageWindow
    {
        public MessageWindow([NotNull] MessageViewModel viewModel)
        {
            DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            InitializeComponent();
        }
    }
}