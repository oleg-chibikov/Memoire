using System;
using System.Threading;
using System.Threading.Tasks;
using Remembrance.Contracts.Processing.Data;
using Remembrance.Contracts.View.Card;
using Remembrance.ViewModel;
using Scar.Common;
using Scar.Common.View.Contracts;

namespace Remembrance.View.WindowCreators
{
    // TODO: Move to viewModel
    sealed class AssessmentTextInputCardWindowCreator : IWindowCreator<IAssessmentTextInputCardWindow, (IDisplayable Window, TranslationInfo TranslationInfo)>
    {
        readonly Func<TranslationInfo, AssessmentTextInputCardViewModel> _assessmentTextInputCardViewModelFactory;
        readonly Func<IDisplayable, AssessmentTextInputCardViewModel, IAssessmentTextInputCardWindow> _assessmentTextInputCardWindowFactory;
        readonly SynchronizationContext _synchronizationContext;

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
            _ = param.TranslationInfo ?? throw new ArgumentException(nameof(param.TranslationInfo) + " is null");
            var assessmentViewModel = _assessmentTextInputCardViewModelFactory(param.TranslationInfo);
            IAssessmentTextInputCardWindow? window = null;
            _synchronizationContext.Send(x => window = _assessmentTextInputCardWindowFactory(param.Window, assessmentViewModel), null);
            return Task.FromResult(window ?? throw new InvalidOperationException("Window is null"));
        }
    }
}
