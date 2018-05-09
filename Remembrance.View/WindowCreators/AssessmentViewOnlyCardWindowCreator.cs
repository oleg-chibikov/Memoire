using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Remembrance.Contracts.Processing.Data;
using Remembrance.Contracts.View.Card;
using Remembrance.ViewModel.Card;
using Scar.Common.WPF.View;
using Scar.Common.WPF.View.Contracts;

namespace Remembrance.View.WindowCreators
{
    [UsedImplicitly]

    // ReSharper disable once StyleCop.SA1009
    internal sealed class AssessmentViewOnlyCardWindowCreator : IWindowCreator<IAssessmentViewOnlyCardWindow, (IWindow window, TranslationInfo translationInfo)>
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
            _assessmentViewOnlyCardViewModelFactory = assessmentViewOnlyCardViewModelFactory;
            _assessmentViewOnlyCardWindowFactory = assessmentViewOnlyCardWindowFactory;
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
        }

        [NotNull]
        public Task<IAssessmentViewOnlyCardWindow> CreateWindowAsync((IWindow window, TranslationInfo translationInfo) param, CancellationToken cancellationToken)
        {
            if (param.translationInfo == null)
            {
                throw new ArgumentNullException(nameof(param.translationInfo));
            }

            var assessmentViewModel = _assessmentViewOnlyCardViewModelFactory(param.translationInfo);
            IAssessmentViewOnlyCardWindow window = null;
            _synchronizationContext.Send(x => window = _assessmentViewOnlyCardWindowFactory(param.window, assessmentViewModel), null);
            return Task.FromResult(window);
        }
    }
}