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
using Scar.Common.WPF.View;
using Scar.Common.WPF.View.Contracts;

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
            [NotNull] IWindowFactory<IAddTranslationWindow> addTranslationWindowFactory)
            : base(localSettingsRepository, languageManager, translationEntryProcessor, logger)
        {
            _addTranslationWindowFactory = addTranslationWindowFactory ?? throw new ArgumentNullException(nameof(addTranslationWindowFactory));
        }

        protected override async Task<IWindow> GetWindowAsync()
        {
            return await _addTranslationWindowFactory.GetWindowIfExistsAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }
}