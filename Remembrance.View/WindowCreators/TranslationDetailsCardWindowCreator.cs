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
    internal sealed class TranslationDetailsCardWindowCreator : IWindowCreator<ITranslationDetailsCardWindow, (IWindow window, TranslationInfo translationInfo)>
    {
        [NotNull]
        private readonly SynchronizationContext _synchronizationContext;

        [NotNull]
        private readonly Func<TranslationInfo, TranslationDetailsCardViewModel> _translationDetailsCardViewModelFactory;

        [NotNull]
        private readonly Func<IWindow, TranslationDetailsCardViewModel, ITranslationDetailsCardWindow> _translationDetailsCardWindowFactory;

        public TranslationDetailsCardWindowCreator(
            [NotNull] Func<IWindow, TranslationDetailsCardViewModel, ITranslationDetailsCardWindow> translationDetailsCardWindowFactory,
            [NotNull] Func<TranslationInfo, TranslationDetailsCardViewModel> translationDetailsCardViewModelFactory,
            [NotNull] SynchronizationContext synchronizationContext)
        {
            _translationDetailsCardWindowFactory = translationDetailsCardWindowFactory ?? throw new ArgumentNullException(nameof(translationDetailsCardWindowFactory));
            _translationDetailsCardViewModelFactory = translationDetailsCardViewModelFactory ?? throw new ArgumentNullException(nameof(translationDetailsCardViewModelFactory));
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
        }

        [NotNull]
        public Task<ITranslationDetailsCardWindow> CreateWindowAsync((IWindow window, TranslationInfo translationInfo) param, CancellationToken cancellationToken)
        {
            if (param.translationInfo == null)
            {
                throw new ArgumentNullException(nameof(param.translationInfo));
            }

            var translationDetailsCardViewModel = _translationDetailsCardViewModelFactory(param.translationInfo);
            ITranslationDetailsCardWindow window = null;
            _synchronizationContext.Send(x => window = _translationDetailsCardWindowFactory(param.window, translationDetailsCardViewModel), null);
            return Task.FromResult(window);
        }
    }
}