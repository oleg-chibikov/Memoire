using System.Windows;
using Remembrance.Contracts.View.Card;
using Remembrance.ViewModel;

namespace Remembrance.View.Windows
{
    sealed partial class AssessmentViewOnlyCardWindow : IAssessmentViewOnlyCardWindow
    {
        public AssessmentViewOnlyCardWindow(AssessmentViewOnlyCardViewModel viewModel, Window? ownerWindow = null)
        {
            DataContext = viewModel;
            Owner = ownerWindow;
            InitializeComponent();
        }
    }
}
