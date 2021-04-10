using System;
using System.Threading;
using System.Threading.Tasks;
using Mémoire.Contracts.DAL.Local;
using Mémoire.Contracts.Languages;
using Mémoire.Contracts.Processing;
using Mémoire.Contracts.View.Settings;
using Microsoft.Extensions.Logging;
using PropertyChanged;
using Scar.Common.MVVM.Commands;
using Scar.Common.View.Contracts;
using Scar.Common.View.WindowCreation;

namespace Mémoire.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public sealed class AddTranslationViewModel : BaseViewModelWithAddTranslationControl
    {
        readonly IWindowFactory<IAddTranslationWindow> _addTranslationWindowFactory;

        public AddTranslationViewModel(
            ILocalSettingsRepository localSettingsRepository,
            ILanguageManager languageManager,
            ITranslationEntryProcessor translationEntryProcessor,
            ILogger<BaseViewModelWithAddTranslationControl> baseLogger,
            ILogger<AddTranslationViewModel> logger,
            IWindowFactory<IAddTranslationWindow> addTranslationWindowFactory,
            ICommandManager commandManager) : base(localSettingsRepository, languageManager, translationEntryProcessor, baseLogger, commandManager)
        {
            _ = logger ?? throw new ArgumentNullException(nameof(logger));
            _addTranslationWindowFactory = addTranslationWindowFactory ?? throw new ArgumentNullException(nameof(addTranslationWindowFactory));
            logger.LogDebug($"Initialized {GetType().Name}");
        }

        protected override async Task<IDisplayable?> GetWindowAsync()
        {
            return await _addTranslationWindowFactory.GetWindowIfExistsAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }
}
