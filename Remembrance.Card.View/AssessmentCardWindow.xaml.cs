using System.Windows;
using JetBrains.Annotations;
using Remembrance.Card.View.Contracts;
using Remembrance.Card.ViewModel.Contracts;

namespace Remembrance.Card.View
{
    [UsedImplicitly]
    internal partial class AssessmentCardWindow : IAssessmentCardWindow
    {
        public AssessmentCardWindow([NotNull] IAssessmentCardViewModel viewModel, [CanBeNull] Window ownerWindow = null)
        {
            DataContext = viewModel;
            Owner = ownerWindow;
            InitializeComponent();
            AnswerControl.AnswerTextBox.Focus();
        }
    }
}