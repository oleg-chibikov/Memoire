using System.Windows;
using Mémoire.Contracts.View.Card;
using Mémoire.ViewModel;

namespace Mémoire.View.Windows
{
    public sealed partial class TranslationDetailsCardWindow : ITranslationDetailsCardWindow
    {
        public TranslationDetailsCardWindow(TranslationDetailsCardViewModel viewModel, Window? ownerWindow = null)
        {
            DataContext = viewModel;
            Owner = ownerWindow;
            InitializeComponent();
        }
    }
}
