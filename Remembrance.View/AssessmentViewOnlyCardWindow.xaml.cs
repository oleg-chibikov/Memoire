using System.Windows;
using JetBrains.Annotations;
using Remembrance.Contracts.View.Card;
using Remembrance.ViewModel;

namespace Remembrance.View
{
    /// <summary>
    /// The assessment view only card window.
    /// </summary>
    [UsedImplicitly]
    internal sealed partial class AssessmentViewOnlyCardWindow : IAssessmentViewOnlyCardWindow
    {
        public AssessmentViewOnlyCardWindow([NotNull] AssessmentViewOnlyCardViewModel viewModel, [CanBeNull] Window ownerWindow = null)
        {
            DataContext = viewModel;
            Owner = ownerWindow;
            InitializeComponent();
        }
    }
}