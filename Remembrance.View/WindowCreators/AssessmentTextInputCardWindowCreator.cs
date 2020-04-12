using System;
using System.Threading;
using System.Threading.Tasks;
using Remembrance.Contracts.Processing.Data;
using Remembrance.Contracts.View.Card;
using Remembrance.ViewModel;
using Scar.Common.View.Contracts;
using Scar.Common.View.WindowFactory;

namespace Remembrance.View.WindowCreators
{
    //TODO: Move to viewModel
    internal sealed class AssessmentTextInputCardWindowCreator : IWindowCreator<IAssessmentTextInputCardWindow, (IDisplayable Window, TranslationInfo TranslationInfo)>
    {
        private readonly Func<TranslationInfo, AssessmentTextInputCardViewModel> _assessmentTextInputCardViewModelFactory;
        private readonly Func<IDisplayable, AssessmentTextInputCardViewModel, IAssessmentTextInputCardWindow> _assessmentTextInputCardWindowFactory;
        private readonly SynchronizationContext _synchronizationContext;

        public AssessmentTextInputCardWindowCreator(
            Func<TranslationInfo, AssessmentTextInputCardViewModel> assessmentTextInputCardViewModelFactory,
            Func<IDisplayable, AssessmentTextInputCardViewModel, IAssessmentTextInputCardWindow> assessmentTextInputCardWindowFactory,
            SynchronizationContext synchronizationContext)
        {
            _assessmentTextInputCardViewModelFactory = assessmentTextInputCardViewModelFactory ?? throw new ArgumentNullException(nameof(assessmentTextInputCardViewModelFactory));
            _assessmentTextInputCardWindowFactory = assessmentTextInputCardWindowFactory ?? throw new ArgumentNullException(nameof(assessmentTextInputCardWindowFactory));
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
        }
        public Task<IAssessmentTextInputCardWindow> CreateWindowAsync((IDisplayable Window, TranslationInfo TranslationInfo) param, CancellationToken cancellationToken)
        {
            _ = param.TranslationInfo ?? throw new ArgumentNullException(nameof(param.TranslationInfo));
            var assessmentViewModel = _assessmentTextInputCardViewModelFactory(param.TranslationInfo);
            IAssessmentTextInputCardWindow? window = null;
            _synchronizationContext.Send(x => window = _assessmentTextInputCardWindowFactory(param.Window, assessmentViewModel), null);
            return Task.FromResult(window ?? throw new InvalidOperationException("Window is null"));
        }
    }
}
