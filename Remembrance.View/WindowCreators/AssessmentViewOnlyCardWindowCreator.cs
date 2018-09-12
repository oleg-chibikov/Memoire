using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Remembrance.Contracts.Processing.Data;
using Remembrance.Contracts.View.Card;
using Remembrance.ViewModel;
using Scar.Common.WPF.View;
using Scar.Common.WPF.View.Contracts;

namespace Remembrance.View.WindowCreators
{
    [UsedImplicitly]

    // ReSharper disable once StyleCop.SA1009
    internal sealed class AssessmentViewOnlyCardWindowCreator : IWindowCreator<IAssessmentViewOnlyCardWindow, (IWindow Window, TranslationInfo TranslationInfo)>
    {
        [NotNull]
        private readonly Func<TranslationInfo, AssessmentViewOnlyCardViewModel> _assessmentViewOnlyCardViewModelFactory;

        [NotNull]
        private readonly Func<IWindow, AssessmentViewOnlyCardViewModel, IAssessmentViewOnlyCardWindow> _assessmentViewOnlyCardWindowFactory;

        [NotNull]
        private readonly SynchronizationContext _synchronizationContext;

        public AssessmentViewOnlyCardWindowCreator(
            [NotNull] Func<TranslationInfo, AssessmentViewOnlyCardViewModel> assessmentViewOnlyCardViewModelFactory,
            [NotNull] Func<IWindow, AssessmentViewOnlyCardViewModel, IAssessmentViewOnlyCardWindow> assessmentViewOnlyCardWindowFactory,
            [NotNull] SynchronizationContext synchronizationContext)
        {
            _assessmentViewOnlyCardViewModelFactory = assessmentViewOnlyCardViewModelFactory ?? throw new ArgumentNullException(nameof(assessmentViewOnlyCardViewModelFactory));
            _assessmentViewOnlyCardWindowFactory = assessmentViewOnlyCardWindowFactory ?? throw new ArgumentNullException(nameof(assessmentViewOnlyCardWindowFactory));
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
        }

        public Task<IAssessmentViewOnlyCardWindow> CreateWindowAsync((IWindow Window, TranslationInfo TranslationInfo) param, CancellationToken cancellationToken)
        {
            _ = param.TranslationInfo ?? throw new ArgumentNullException(nameof(param.TranslationInfo));
            var assessmentViewModel = _assessmentViewOnlyCardViewModelFactory(param.TranslationInfo);
            IAssessmentViewOnlyCardWindow window = null;
            _synchronizationContext.Send(x => window = _assessmentViewOnlyCardWindowFactory(param.Window, assessmentViewModel), null);
            return Task.FromResult(window);
        }
    }
}