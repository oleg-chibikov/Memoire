using System.Windows;
using Remembrance.Contracts.View.Card;
using Remembrance.ViewModel;

namespace Remembrance.View.Windows
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
