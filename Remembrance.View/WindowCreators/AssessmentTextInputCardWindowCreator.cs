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
    internal sealed class AssessmentTextInputCardWindowCreator : IWindowCreator<IAssessmentTextInputCardWindow, (IWindow Window, TranslationInfo TranslationInfo)>
    {
        [NotNull]
        private readonly Func<TranslationInfo, AssessmentTextInputCardViewModel> _assessmentTextInputCardViewModelFactory;

        [NotNull]
        private readonly Func<IWindow, AssessmentTextInputCardViewModel, IAssessmentTextInputCardWindow> _assessmentTextInputCardWindowFactory;

        [NotNull]
        private readonly SynchronizationContext _synchronizationContext;

        public AssessmentTextInputCardWindowCreator(
            [NotNull] Func<TranslationInfo, AssessmentTextInputCardViewModel> assessmentTextInputCardViewModelFactory,
            [NotNull] Func<IWindow, AssessmentTextInputCardViewModel, IAssessmentTextInputCardWindow> assessmentTextInputCardWindowFactory,
            [NotNull] SynchronizationContext synchronizationContext)
        {
            _assessmentTextInputCardViewModelFactory = assessmentTextInputCardViewModelFactory ?? throw new ArgumentNullException(nameof(assessmentTextInputCardViewModelFactory));
            _assessmentTextInputCardWindowFactory = assessmentTextInputCardWindowFactory ?? throw new ArgumentNullException(nameof(assessmentTextInputCardWindowFactory));
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
        }

        [NotNull]
        public Task<IAssessmentTextInputCardWindow> CreateWindowAsync((IWindow Window, TranslationInfo TranslationInfo) param, CancellationToken cancellationToken)
        {
            if (param.TranslationInfo == null)
            {
                throw new ArgumentNullException(nameof(param.TranslationInfo));
            }

            var assessmentViewModel = _assessmentTextInputCardViewModelFactory(param.TranslationInfo);
            IAssessmentTextInputCardWindow window = null;
            _synchronizationContext.Send(x => window = _assessmentTextInputCardWindowFactory(param.Window, assessmentViewModel), null);
            return Task.FromResult(window);
        }
    }
}