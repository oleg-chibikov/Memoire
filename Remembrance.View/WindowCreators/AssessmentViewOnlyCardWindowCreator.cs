using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Remembrance.Contracts.Processing.Data;
using Remembrance.Contracts.View.Card;
using Remembrance.ViewModel;
using Scar.Common.View.Contracts;
using Scar.Common.View.WindowFactory;

namespace Remembrance.View.WindowCreators
{
    [UsedImplicitly]

    internal sealed class AssessmentViewOnlyCardWindowCreator : IWindowCreator<IAssessmentViewOnlyCardWindow, (IDisplayable Window, TranslationInfo TranslationInfo)>
    {
        [NotNull]
        private readonly Func<TranslationInfo, AssessmentViewOnlyCardViewModel> _assessmentViewOnlyCardViewModelFactory;

        [NotNull]
        private readonly Func<IDisplayable, AssessmentViewOnlyCardViewModel, IAssessmentViewOnlyCardWindow> _assessmentViewOnlyCardWindowFactory;

        [NotNull]
        private readonly SynchronizationContext _synchronizationContext;

        public AssessmentViewOnlyCardWindowCreator(
            [NotNull] Func<TranslationInfo, AssessmentViewOnlyCardViewModel> assessmentViewOnlyCardViewModelFactory,
            [NotNull] Func<IDisplayable, AssessmentViewOnlyCardViewModel, IAssessmentViewOnlyCardWindow> assessmentViewOnlyCardWindowFactory,
            [NotNull] SynchronizationContext synchronizationContext)
        {
            _assessmentViewOnlyCardViewModelFactory = assessmentViewOnlyCardViewModelFactory ?? throw new ArgumentNullException(nameof(assessmentViewOnlyCardViewModelFactory));
            _assessmentViewOnlyCardWindowFactory = assessmentViewOnlyCardWindowFactory ?? throw new ArgumentNullException(nameof(assessmentViewOnlyCardWindowFactory));
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
        }

        public Task<IAssessmentViewOnlyCardWindow> CreateWindowAsync((IDisplayable Window, TranslationInfo TranslationInfo) param, CancellationToken cancellationToken)
        {
            _ = param.TranslationInfo ?? throw new ArgumentNullException(nameof(param.TranslationInfo));
            var assessmentViewModel = _assessmentViewOnlyCardViewModelFactory(param.TranslationInfo);
            IAssessmentViewOnlyCardWindow window = null;
            _synchronizationContext.Send(x => window = _assessmentViewOnlyCardWindowFactory(param.Window, assessmentViewModel), null);
            return Task.FromResult(window);
        }
    }
}