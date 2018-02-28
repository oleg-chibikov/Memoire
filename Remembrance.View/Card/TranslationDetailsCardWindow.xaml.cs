using System.Windows;
using JetBrains.Annotations;
using Remembrance.Contracts.View.Card;
using Remembrance.ViewModel.Card;

namespace Remembrance.View.Card
{
    /// <summary>
    /// The translation details card window.
    /// </summary>
    [UsedImplicitly]
    internal sealed partial class TranslationDetailsCardWindow : ITranslationDetailsCardWindow
    {
        public TranslationDetailsCardWindow([NotNull] TranslationDetailsCardViewModel viewModel, [CanBeNull] Window ownerWindow = null)
        {
            DataContext = viewModel;
            Owner = ownerWindow;
            InitializeComponent();
        }
    }
}