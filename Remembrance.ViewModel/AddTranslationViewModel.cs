using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using PropertyChanged;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.Languages;
using Remembrance.Contracts.Processing;
using Remembrance.Contracts.View.Settings;
using Scar.Common.MVVM.Commands;
using Scar.Common.View.Contracts;
using Scar.Common.View.WindowFactory;

namespace Remembrance.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public sealed class AddTranslationViewModel : BaseViewModelWithAddTranslationControl
    {
        private readonly IWindowFactory<IAddTranslationWindow> _addTranslationWindowFactory;

        public AddTranslationViewModel(
            ILocalSettingsRepository localSettingsRepository,
            ILanguageManager languageManager,
            ITranslationEntryProcessor translationEntryProcessor,
            ILog logger,
            IWindowFactory<IAddTranslationWindow> addTranslationWindowFactory,
            ICommandManager commandManager)
            : base(localSettingsRepository, languageManager, translationEntryProcessor, logger, commandManager)
        {
            _addTranslationWindowFactory = addTranslationWindowFactory ?? throw new ArgumentNullException(nameof(addTranslationWindowFactory));
        }

        protected override async Task<IDisplayable?> GetWindowAsync()
        {
            return await _addTranslationWindowFactory.GetWindowIfExistsAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }
}