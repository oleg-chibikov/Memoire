using System.Windows;
using Mémoire.Contracts.View.Card;
using Mémoire.ViewModel;

namespace Mémoire.View.Windows
{
    sealed partial class AssessmentBatchCardWindow : IAssessmentBatchCardWindow
    {
        public AssessmentBatchCardWindow(AssessmentBatchCardViewModel viewModel, Window? ownerWindow = null)
        {
            DataContext = viewModel;
            Owner = ownerWindow;
            InitializeComponent();
        }
    }
}
