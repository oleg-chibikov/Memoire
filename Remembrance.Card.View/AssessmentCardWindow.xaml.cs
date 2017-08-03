using System.Windows;
using JetBrains.Annotations;
using Remembrance.Card.View.Contracts;
using Remembrance.ViewModel.Card;

namespace Remembrance.Card.View
{
    [UsedImplicitly]
    internal sealed partial class AssessmentCardWindow : IAssessmentCardWindow
    {
        public AssessmentCardWindow([NotNull] AssessmentCardViewModel viewModel, [CanBeNull] Window ownerWindow = null)
        {
            DataContext = viewModel;
            Owner = ownerWindow;
            InitializeComponent();
            AnswerControl.AnswerTextBox.Focus();
        }
    }
}