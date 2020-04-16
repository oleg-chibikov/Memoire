using System.Windows;
using Remembrance.Contracts.View.Card;
using Remembrance.ViewModel;

namespace Remembrance.View.Windows
{
    sealed partial class AssessmentTextInputCardWindow : IAssessmentTextInputCardWindow
    {
        public AssessmentTextInputCardWindow(AssessmentTextInputCardViewModel viewModel, Window? ownerWindow = null)
        {
            DataContext = viewModel;
            Owner = ownerWindow;
            InitializeComponent();
        }
    }
}
