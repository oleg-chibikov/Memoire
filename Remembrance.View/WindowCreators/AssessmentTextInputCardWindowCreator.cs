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

    internal sealed class AssessmentTextInputCardWindowCreator : IWindowCreator<IAssessmentTextInputCardWindow, (IDisplayable Window, TranslationInfo TranslationInfo)>
    {
        [NotNull]
        private readonly Func<TranslationInfo, AssessmentTextInputCardViewModel> _assessmentTextInputCardViewModelFactory;

        [NotNull]
        private readonly Func<IDisplayable, AssessmentTextInputCardViewModel, IAssessmentTextInputCardWindow> _assessmentTextInputCardWindowFactory;

        [NotNull]
        private readonly SynchronizationContext _synchronizationContext;

        public AssessmentTextInputCardWindowCreator(
            [NotNull] Func<TranslationInfo, AssessmentTextInputCardViewModel> assessmentTextInputCardViewModelFactory,
            [NotNull] Func<IDisplayable, AssessmentTextInputCardViewModel, IAssessmentTextInputCardWindow> assessmentTextInputCardWindowFactory,
            [NotNull] SynchronizationContext synchronizationContext)
        {
            _assessmentTextInputCardViewModelFactory = assessmentTextInputCardViewModelFactory ?? throw new ArgumentNullException(nameof(assessmentTextInputCardViewModelFactory));
            _assessmentTextInputCardWindowFactory = assessmentTextInputCardWindowFactory ?? throw new ArgumentNullException(nameof(assessmentTextInputCardWindowFactory));
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
        }

        public Task<IAssessmentTextInputCardWindow> CreateWindowAsync((IDisplayable Window, TranslationInfo TranslationInfo) param, CancellationToken cancellationToken)
        {
            _ = param.TranslationInfo ?? throw new ArgumentNullException(nameof(param.TranslationInfo));
            var assessmentViewModel = _assessmentTextInputCardViewModelFactory(param.TranslationInfo);
            IAssessmentTextInputCardWindow window = null;
            _synchronizationContext.Send(x => window = _assessmentTextInputCardWindowFactory(param.Window, assessmentViewModel), null);
            return Task.FromResult(window);
        }
    }
}