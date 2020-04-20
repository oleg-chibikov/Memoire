using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Remembrance.Contracts.Processing.Data;
using Remembrance.Contracts.View.Card;
using Scar.Common;

namespace Remembrance.ViewModel.WindowCreators
{
    sealed class AssessmentBatchCardWindowCreator : IWindowCreator<IAssessmentBatchCardWindow, IReadOnlyCollection<TranslationInfo>>
    {
        readonly Func<IReadOnlyCollection<TranslationInfo>, AssessmentBatchCardViewModel> _assessmentBatchCardViewModelFactory;
        readonly Func<AssessmentBatchCardViewModel, IAssessmentBatchCardWindow> _assessmentBatchCardWindowFactory;
        readonly SynchronizationContext _synchronizationContext;

        public AssessmentBatchCardWindowCreator(
            Func<IReadOnlyCollection<TranslationInfo>, AssessmentBatchCardViewModel> assessmentBatchCardViewModelFactory,
            Func<AssessmentBatchCardViewModel, IAssessmentBatchCardWindow> assessmentBatchCardWindowFactory,
            SynchronizationContext synchronizationContext)
        {
            _assessmentBatchCardViewModelFactory = assessmentBatchCardViewModelFactory ?? throw new ArgumentNullException(nameof(assessmentBatchCardViewModelFactory));
            _assessmentBatchCardWindowFactory = assessmentBatchCardWindowFactory ?? throw new ArgumentNullException(nameof(assessmentBatchCardWindowFactory));
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
        }

        public Task<IAssessmentBatchCardWindow> CreateWindowAsync(IReadOnlyCollection<TranslationInfo> translationInfos, CancellationToken cancellationToken)
        {
            _ = translationInfos ?? throw new ArgumentNullException(nameof(translationInfos));

            var assessmentViewModel = _assessmentBatchCardViewModelFactory(translationInfos);
            IAssessmentBatchCardWindow? window = null;
            _synchronizationContext.Send(x => window = _assessmentBatchCardWindowFactory(assessmentViewModel), null);
            return Task.FromResult(window ?? throw new InvalidOperationException("Window is null"));
        }
    }
}
