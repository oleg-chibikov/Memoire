using System;
using Common.Logging;
using JetBrains.Annotations;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.Translate;
using Remembrance.Contracts.View.Settings;
using Scar.Common.WPF.View;
using Scar.Common.WPF.View.Contracts;

namespace Remembrance.ViewModel.Settings
{
    [UsedImplicitly]
    public sealed class AddTranslationViewModel : BaseViewModelWithAddTranslationControl
    {
        [NotNull]
        private readonly WindowFactory<IAddTranslationWindow> _addTranslationWindowFactory;

        public AddTranslationViewModel(
            [NotNull] ISettingsRepository settingsRepository,
            [NotNull] ILanguageDetector languageDetector,
            [NotNull] IWordsProcessor wordsProcessor,
            [NotNull] ILog logger,
            [NotNull] WindowFactory<IAddTranslationWindow> addTranslationWindowFactory)
            : base(settingsRepository, languageDetector, wordsProcessor, logger)
        {
            _addTranslationWindowFactory = addTranslationWindowFactory ?? throw new ArgumentNullException(nameof(addTranslationWindowFactory));
        }

        protected override IWindow Window => _addTranslationWindowFactory.GetWindowIfExists();
    }
}