using System;
using Common.Logging;
using JetBrains.Annotations;
using Remembrance.Card.Management.Contracts;
using Remembrance.DAL.Contracts;
using Remembrance.Settings.View.Contracts;
using Remembrance.Settings.ViewModel.Contracts;
using Remembrance.Translate.Contracts.Interfaces;
using Scar.Common.WPF.View;
using Scar.Common.WPF.View.Contracts;

namespace Remembrance.Settings.ViewModel
{
    [UsedImplicitly]
    internal sealed class AddTranslationViewModel : BaseViewModelWithAddTranslationControl, IAddTranslationViewModel
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