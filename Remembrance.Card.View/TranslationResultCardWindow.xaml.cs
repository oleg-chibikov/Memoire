using System.Windows;
using JetBrains.Annotations;
using Remembrance.Card.View.Contracts;
using Remembrance.ViewModel;

namespace Remembrance.Card.View
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