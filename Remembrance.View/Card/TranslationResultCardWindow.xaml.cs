using System.Windows;
using JetBrains.Annotations;
using Remembrance.Contracts.View.Card;
using Remembrance.ViewModel.Card;

namespace Remembrance.View.Card
{
    [UsedImplicitly]
    internal sealed partial class TranslationResultCardWindow : ITranslationResultCardWindow
    {
        public TranslationResultCardWindow([NotNull] TranslationResultCardViewModel viewModel, [CanBeNull] Window ownerWindow = null)
        {
            DataContext = viewModel;
            Owner = ownerWindow;
            InitializeComponent();
        }
    }
}