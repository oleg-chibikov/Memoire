using System;
using JetBrains.Annotations;
using Remembrance.Settings.View.Contracts;
using Remembrance.Settings.ViewModel.Contracts;

namespace Remembrance.Settings.View
{
    [UsedImplicitly]
    internal partial class TrayWindow : ITrayWindow
    {
        public TrayWindow([NotNull] ITrayViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }

        public bool CanDrag { get; set; }

        public void Restore()
        {
            throw new NotImplementedException();
        }
    }
}