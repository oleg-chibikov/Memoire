using System;
using System.Threading;
using System.Threading.Tasks;
using Remembrance.Contracts.Processing.Data;
using Remembrance.Contracts.View.Card;
using Scar.Common;
using Scar.Common.View.Contracts;

namespace Remembrance.ViewModel.WindowCreators
{
    sealed class TranslationDetailsCardWindowCreator : IWindowCreator<ITranslationDetailsCardWindow, (IDisplayable Window, TranslationInfo TranslationInfo)>
    {
        readonly SynchronizationContext _synchronizationContext;
        readonly Func<TranslationInfo, TranslationDetailsCardViewModel> _translationDetailsCardViewModelFactory;
        readonly Func<IDisplayable, TranslationDetailsCardViewModel, ITranslationDetailsCardWindow> _translationDetailsCardWindowFactory;

        public TranslationDetailsCardWindowCreator(
            Func<IDisplayable, TranslationDetailsCardViewModel, ITranslationDetailsCardWindow> translationDetailsCardWindowFactory,
            Func<TranslationInfo, TranslationDetailsCardViewModel> translationDetailsCardViewModelFactory,
            SynchronizationContext synchronizationContext)
        {
            _translationDetailsCardWindowFactory = translationDetailsCardWindowFactory ?? throw new ArgumentNullException(nameof(translationDetailsCardWindowFactory));
            _translationDetailsCardViewModelFactory = translationDetailsCardViewModelFactory ?? throw new ArgumentNullException(nameof(translationDetailsCardViewModelFactory));
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
        }

        public Task<ITranslationDetailsCardWindow> CreateWindowAsync((IDisplayable Window, TranslationInfo TranslationInfo) param, CancellationToken cancellationToken)
        {
            _ = param.TranslationInfo ?? throw new ArgumentException(nameof(param.TranslationInfo) + " is null");
            var translationDetailsCardViewModel = _translationDetailsCardViewModelFactory(param.TranslationInfo);
            ITranslationDetailsCardWindow? window = null;
            _synchronizationContext.Send(x => window = _translationDetailsCardWindowFactory(param.Window, translationDetailsCardViewModel), null);
            return Task.FromResult(window ?? throw new InvalidOperationException("Window is null"));
        }
    }
}
