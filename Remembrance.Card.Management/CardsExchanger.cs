using System;
using Common.Logging;
using GalaSoft.MvvmLight.Messaging;
using JetBrains.Annotations;
using Remembrance.Card.Management.Contracts;
using Remembrance.DAL.Contracts;
using Remembrance.Resources;
using Scar.Common.IO;

namespace Remembrance.Card.Management
{
    [UsedImplicitly]
    internal sealed class CardsExchanger : ICardsExchanger
    {
        private const string JsonFilesFilter = "Json files (*.json)|*.json;";

        [NotNull]
        private readonly IFileExporter _exporter;

        [NotNull]
        private readonly IFileImporter[] _importers;

        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly IMessenger _messenger;

        [NotNull]
        private readonly IOpenFileService _openFileService;

        [NotNull]
        private readonly ISaveFileService _saveFileService;

        public CardsExchanger(
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] IOpenFileService openFileService,
            [NotNull] ISaveFileService saveFileService,
            [NotNull] ILog logger,
            [NotNull] IWordsAdder wordsAdder,
            [NotNull] IFileExporter exporter,
            [NotNull] IFileImporter[] importers,
            [NotNull] IMessenger messenger)
        {
            if (wordsAdder == null)
                throw new ArgumentNullException(nameof(wordsAdder));

            _openFileService = openFileService ?? throw new ArgumentNullException(nameof(openFileService));
            _saveFileService = saveFileService ?? throw new ArgumentNullException(nameof(saveFileService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
            _importers = importers ?? throw new ArgumentNullException(nameof(importers));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        }

        public void Export()
        {
            if (!_saveFileService.SaveFileDialog(Texts.Title, JsonFilesFilter))
                return;

            _logger.Info($"Performing export to {_saveFileService.FileName}...");
            if (_exporter.Export(_saveFileService.FileName))
            {
                _logger.Info($"Export to {_saveFileService.FileName} has been performed");
                _messenger.Send(Texts.ExportSucceeded, MessengerTokens.UserMessageToken);
            }
            else
            {
                _logger.Warn($"Export to {_saveFileService.FileName} failed");
                _messenger.Send(Texts.ExportFailed, MessengerTokens.UserWarningToken);
            }
        }

        public void Import()
        {
            if (!_openFileService.OpenFileDialog(Texts.Title, JsonFilesFilter))
                return;

            foreach (var importer in _importers)
            {
                _logger.Info($"Performing import from {_openFileService.FileName} with {importer.GetType().Name}...");
                string[] errors;
                int count;
                if (importer.Import(_openFileService.FileName, out errors, out count))
                {
                    _logger.Info($"Import from {_openFileService.FileName} has been performed");
                    var mainMessage = string.Format(Texts.ImportSucceeded, count);
                    if (errors != null)
                        _messenger.Send($"[{importer.GetType().Name}] {mainMessage}. {Texts.ImportErrors}:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}", MessengerTokens.UserWarningToken);
                    else
                        _messenger.Send($"[{importer.GetType().Name}] {mainMessage}", MessengerTokens.UserMessageToken);
                    return;
                }
            }

            _logger.Warn($"Import from {_openFileService.FileName} failed");
            _messenger.Send(Texts.ImportFailed, MessengerTokens.UserWarningToken);
        }
    }
}