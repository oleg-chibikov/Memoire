using System;
using System.Threading;
using System.Threading.Tasks;
using Mémoire.Contracts.Processing.Data;
using Mémoire.Contracts.View.Card;
using Mémoire.ViewModel;
using Scar.Common.View.Contracts;
using Scar.Common.View.WindowCreation;

namespace Mémoire.WindowCreators
{
    public sealed class TranslationDetailsCardWindowCreator : IWindowCreator<ITranslationDetailsCardWindow, (IDisplayable Window, TranslationInfo TranslationInfo)>
    {
        readonly SynchronizationContext _synchronizationContext;
        readonly Func<TranslationInfo, TranslationDetailsCardViewModel> _translationDetailsCardWindowCreatorsFactory;
        readonly Func<IDisplayable, TranslationDetailsCardViewModel, ITranslationDetailsCardWindow> _translationDetailsCardWindowFactory;

        public TranslationDetailsCardWindowCreator(
            Func<IDisplayable, TranslationDetailsCardViewModel, ITranslationDetailsCardWindow> translationDetailsCardWindowFactory,
            Func<TranslationInfo, TranslationDetailsCardViewModel> translationDetailsCardWindowCreatorsFactory,
            SynchronizationContext synchronizationContext)
        {
            _translationDetailsCardWindowFactory = translationDetailsCardWindowFactory ?? throw new ArgumentNullException(nameof(translationDetailsCardWindowFactory));
            _translationDetailsCardWindowCreatorsFactory = translationDetailsCardWindowCreatorsFactory ?? throw new ArgumentNullException(nameof(translationDetailsCardWindowCreatorsFactory));
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
        }

        public Task<ITranslationDetailsCardWindow> CreateWindowAsync((IDisplayable Window, TranslationInfo TranslationInfo) param, CancellationToken cancellationToken)
        {
            _ = param.TranslationInfo ?? throw new ArgumentException($"{nameof(param.TranslationInfo)} is null");
            var translationDetailsCardWindowCreators = _translationDetailsCardWindowCreatorsFactory(param.TranslationInfo);
            ITranslationDetailsCardWindow? window = null;
            _synchronizationContext.Send(_ => window = _translationDetailsCardWindowFactory(param.Window, translationDetailsCardWindowCreators), null);
            return Task.FromResult(window!);
        }
    }
}
