using System.Windows;
using JetBrains.Annotations;
using Remembrance.Contracts.View.Card;
using Remembrance.ViewModel.Card;

namespace Remembrance.View.Card
{
    [UsedImplicitly]
    internal sealed partial class AssessmentViewOnlyCardWindow : IAssessmentViewOnlyCardWindow
    {
        public AssessmentViewOnlyCardWindow([NotNull] AssessmentViewOnlyCardViewModel viewModel, [CanBeNull] Window ownerWindow = null)
        {
            //TODO: Store Learning info not for TranEntry, but for the particular PartOfSpeechTranslation or even more detailed.
            DataContext = viewModel;
            Owner = ownerWindow;
            InitializeComponent();
        }
    }
}