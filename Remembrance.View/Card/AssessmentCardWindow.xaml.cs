using System.Windows;
using JetBrains.Annotations;
using Remembrance.Contracts.View.Card;
using Remembrance.ViewModel.Card;

namespace Remembrance.View.Card
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