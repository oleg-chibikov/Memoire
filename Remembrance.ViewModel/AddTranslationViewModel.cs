using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using JetBrains.Annotations;
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
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class AddTranslationViewModel : BaseViewModelWithAddTranslationControl
    {
        [NotNull]
        private readonly IWindowFactory<IAddTranslationWindow> _addTranslationWindowFactory;

        public AddTranslationViewModel(
            [NotNull] ILocalSettingsRepository localSettingsRepository,
            [NotNull] ILanguageManager languageManager,
            [NotNull] ITranslationEntryProcessor translationEntryProcessor,
            [NotNull] ILog logger,
            [NotNull] IWindowFactory<IAddTranslationWindow> addTranslationWindowFactory,
            [NotNull] ICommandManager commandManager)
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