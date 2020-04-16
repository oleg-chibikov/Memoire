using System;
using System.Threading;
using System.Threading.Tasks;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Processing.Data;
using Remembrance.Contracts.View.Card;
using Remembrance.ViewModel;
using Scar.Common;
using Scar.Common.View.Contracts;

namespace Remembrance.View.WindowCreators
{
    sealed class TranslationDetailsCardWindowCreator : IWindowCreator<ITranslationDetailsCardWindow, (IDisplayable Window, TranslationInfo TranslationInfo)>
    {
        readonly Func<LearningInfo, LearningInfoViewModel> _learningInfoViewModelFactory;
        readonly SynchronizationContext _synchronizationContext;
        readonly Func<TranslationInfo, LearningInfoViewModel, TranslationDetailsCardViewModel> _translationDetailsCardViewModelFactory;
        readonly Func<IDisplayable, TranslationDetailsCardViewModel, ITranslationDetailsCardWindow> _translationDetailsCardWindowFactory;

        public TranslationDetailsCardWindowCreator(
            Func<IDisplayable, TranslationDetailsCardViewModel, ITranslationDetailsCardWindow> translationDetailsCardWindowFactory,
            Func<TranslationInfo, LearningInfoViewModel, TranslationDetailsCardViewModel> translationDetailsCardViewModelFactory,
            Func<LearningInfo, LearningInfoViewModel> learningInfoViewModelFactory,
            SynchronizationContext synchronizationContext)
        {
            _translationDetailsCardWindowFactory = translationDetailsCardWindowFactory ?? throw new ArgumentNullException(nameof(translationDetailsCardWindowFactory));
            _translationDetailsCardViewModelFactory = translationDetailsCardViewModelFactory ?? throw new ArgumentNullException(nameof(translationDetailsCardViewModelFactory));
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
            _learningInfoViewModelFactory = learningInfoViewModelFactory ?? throw new ArgumentNullException(nameof(learningInfoViewModelFactory));
        }
        public Task<ITranslationDetailsCardWindow> CreateWindowAsync((IDisplayable Window, TranslationInfo TranslationInfo) param, CancellationToken cancellationToken)
        {
            _ = param.TranslationInfo ?? throw new ArgumentNullException(nameof(param.TranslationInfo));
            var learningInfoViewModel = _learningInfoViewModelFactory(param.TranslationInfo.LearningInfo);
            var translationDetailsCardViewModel = _translationDetailsCardViewModelFactory(param.TranslationInfo, learningInfoViewModel);
            ITranslationDetailsCardWindow? window = null;
            _synchronizationContext.Send(x => window = _translationDetailsCardWindowFactory(param.Window, translationDetailsCardViewModel), null);
            return Task.FromResult(window ?? throw new InvalidOperationException("Window is null"));
        }
    }
}
