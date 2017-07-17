using System.Windows;
using JetBrains.Annotations;
using Remembrance.Card.View.Contracts;
using Remembrance.Card.ViewModel.Contracts;

namespace Remembrance.Card.View
{
    [UsedImplicitly]
    internal sealed partial class TranslationResultCardWindow : ITranslationResultCardWindow
    {
        public TranslationResultCardWindow([NotNull] ITranslationResultCardViewModel viewModel, [CanBeNull] Window ownerWindow = null)
        {
            DataContext = viewModel;
            Owner = ownerWindow;
            InitializeComponent();
        }
    }
}