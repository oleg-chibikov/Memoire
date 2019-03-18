using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Processing.Data;
using Remembrance.Contracts.View.Card;
using Remembrance.ViewModel;
using Scar.Common.View.Contracts;
using Scar.Common.View.WindowFactory;

namespace Remembrance.View.WindowCreators
{
    [UsedImplicitly]

    // ReSharper disable once StyleCop.SA1009
    internal sealed class TranslationDetailsCardWindowCreator : IWindowCreator<ITranslationDetailsCardWindow, (IDisplayable Window, TranslationInfo TranslationInfo)>
    {
        [NotNull]
        private readonly Func<LearningInfo, LearningInfoViewModel> _learningInfoViewModelFactory;

        [NotNull]
        private readonly SynchronizationContext _synchronizationContext;

        [NotNull]
        private readonly Func<TranslationInfo, LearningInfoViewModel, TranslationDetailsCardViewModel> _translationDetailsCardViewModelFactory;

        [NotNull]
        private readonly Func<IDisplayable, TranslationDetailsCardViewModel, ITranslationDetailsCardWindow> _translationDetailsCardWindowFactory;

        public TranslationDetailsCardWindowCreator(
            [NotNull] Func<IDisplayable, TranslationDetailsCardViewModel, ITranslationDetailsCardWindow> translationDetailsCardWindowFactory,
            [NotNull] Func<TranslationInfo, LearningInfoViewModel, TranslationDetailsCardViewModel> translationDetailsCardViewModelFactory,
            [NotNull] Func<LearningInfo, LearningInfoViewModel> learningInfoViewModelFactory,
            [NotNull] SynchronizationContext synchronizationContext)
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
            ITranslationDetailsCardWindow window = null;
            _synchronizationContext.Send(x => window = _translationDetailsCardWindowFactory(param.Window, translationDetailsCardViewModel), null);
            return Task.FromResult(window);
        }
    }
}