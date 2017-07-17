using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using GalaSoft.MvvmLight.Messaging;
using JetBrains.Annotations;
using Remembrance.Card.Management.Contracts;
using Remembrance.DAL.Contracts;
using Remembrance.Resources;
using Scar.Common.Events;
using Scar.Common.IO;

namespace Remembrance.Card.Management
{
    [UsedImplicitly]
    internal sealed class CardsExchanger : ICardsExchanger, IDisposable
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
            foreach (var importer in importers)
                importer.Progress += Importer_Progress;
        }

        public event EventHandler<ProgressEventArgs> Progress;

        public async Task ExportAsync(CancellationToken token)
        {
            if (!_saveFileService.SaveFileDialog($"{Texts.Title}: {Texts.Export}", JsonFilesFilter))
                return;

            _logger.Info($"Performing export to {_saveFileService.FileName}...");
            var exchangeResult = await _exporter.ExportAsync(_saveFileService.FileName, token).ConfigureAwait(false);
            if (exchangeResult.Success)
            {
                _logger.Info($"Export to {_saveFileService.FileName} has been performed");
                //_messenger.Send(Texts.ExportSucceeded, MessengerTokens.UserMessageToken);
                Process.Start(_saveFileService.FileName);
            }
            else
            {
                _logger.Warn($"Export to {_saveFileService.FileName} failed");
                _messenger.Send(Texts.ExportFailed, MessengerTokens.UserWarningToken);
            }
        }

        public async Task ImportAsync(CancellationToken token)
        {
            if (!_openFileService.OpenFileDialog($"{Texts.Title}: {Texts.Import}", JsonFilesFilter))
                return;

            foreach (var importer in _importers)
            {
                _logger.Info($"Performing import from {_openFileService.FileName} with {importer.GetType().Name}...");

                var exchangeResult = await importer.ImportAsync(_openFileService.FileName, token).ConfigureAwait(false);
                if (exchangeResult.Success)
                {
                    _logger.Info($"ImportAsync from {_openFileService.FileName} has been performed");
                    var mainMessage = string.Format(Texts.ImportSucceeded, exchangeResult.Count);
                    if (exchangeResult.Errors != null)
                        _messenger.Send(
                            $"[{importer.GetType().Name}] {mainMessage}. {Texts.ImportErrors}:{Environment.NewLine}{string.Join(Environment.NewLine, exchangeResult.Errors)}",
                            MessengerTokens.UserWarningToken);
                    else
                        _messenger.Send($"[{importer.GetType().Name}] {mainMessage}", MessengerTokens.UserMessageToken);
                    return;
                }
            }

            _logger.Warn($"ImportAsync from {_openFileService.FileName} failed");
            _messenger.Send(Texts.ImportFailed, MessengerTokens.UserWarningToken);
        }

        public void Dispose()
        {
            foreach (var importer in _importers)
                importer.Progress -= Importer_Progress;
        }

        private void Importer_Progress(object sender, ProgressEventArgs e)
        {
            Progress?.Invoke(this, e);
        }
    }
}