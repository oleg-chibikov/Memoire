using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mémoire.Contracts.Processing.Data;
using Mémoire.Contracts.View.Card;
using Mémoire.ViewModel;
using Scar.Common.View.WindowCreation;

namespace Mémoire.WindowCreators
{
    sealed class AssessmentBatchCardWindowCreator : IWindowCreator<IAssessmentBatchCardWindow, IReadOnlyCollection<TranslationInfo>>
    {
        readonly Func<IReadOnlyCollection<TranslationInfo>, AssessmentBatchCardViewModel> _assessmentBatchCardWindowCreatorsFactory;
        readonly Func<AssessmentBatchCardViewModel, IAssessmentBatchCardWindow> _assessmentBatchCardWindowFactory;
        readonly SynchronizationContext _synchronizationContext;

        public AssessmentBatchCardWindowCreator(
            Func<IReadOnlyCollection<TranslationInfo>, AssessmentBatchCardViewModel> assessmentBatchCardWindowCreatorsFactory,
            Func<AssessmentBatchCardViewModel, IAssessmentBatchCardWindow> assessmentBatchCardWindowFactory,
            SynchronizationContext synchronizationContext)
        {
            _assessmentBatchCardWindowCreatorsFactory = assessmentBatchCardWindowCreatorsFactory ?? throw new ArgumentNullException(nameof(assessmentBatchCardWindowCreatorsFactory));
            _assessmentBatchCardWindowFactory = assessmentBatchCardWindowFactory ?? throw new ArgumentNullException(nameof(assessmentBatchCardWindowFactory));
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
        }

        public Task<IAssessmentBatchCardWindow> CreateWindowAsync(IReadOnlyCollection<TranslationInfo> translationInfos, CancellationToken cancellationToken)
        {
            _ = translationInfos ?? throw new ArgumentNullException(nameof(translationInfos));

            var assessmentWindowCreators = _assessmentBatchCardWindowCreatorsFactory(translationInfos);
            IAssessmentBatchCardWindow? window = null;
            _synchronizationContext.Send(x => window = _assessmentBatchCardWindowFactory(assessmentWindowCreators), null);
            return Task.FromResult(window ?? throw new InvalidOperationException("Window is null"));
        }
    }
}
