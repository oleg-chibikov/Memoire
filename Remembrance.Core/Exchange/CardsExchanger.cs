using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Easy.MessageHub;
using Remembrance.Contracts;
using Remembrance.Contracts.Exchange;
using Remembrance.Contracts.Exchange.Data;
using Remembrance.Resources;
using Scar.Common.Events;
using Scar.Common.Messages;

namespace Remembrance.Core.Exchange
{
    sealed class CardsExchanger : ICardsExchanger, IDisposable
    {
        readonly IFileExporter _exporter;

        readonly IFileImporter[] _importers;

        readonly ILog _logger;

        readonly IMessageHub _messageHub;

        readonly IOpenFileDialogProvider _openFileDialog;

        readonly ISaveFileDialogProvider _saveFileDialog;

        public CardsExchanger(ILog logger, IFileExporter exporter, IFileImporter[] importers, IMessageHub messageHub, IOpenFileDialogProvider openFileDialog, ISaveFileDialogProvider saveFileDialog)
        {
            _openFileDialog = openFileDialog ?? throw new ArgumentNullException(nameof(openFileDialog));
            _saveFileDialog = saveFileDialog ?? throw new ArgumentNullException(nameof(saveFileDialog));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
            _importers = importers ?? throw new ArgumentNullException(nameof(importers));
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            foreach (var importer in importers)
            {
                importer.Progress += ImporterExporter_Progress;
            }

            exporter.Progress += ImporterExporter_Progress;
        }

        public event EventHandler<ProgressEventArgs>? Progress;

        public async Task ExportAsync(CancellationToken cancellationToken)
        {
            var fileName = ShowSaveFileDialog();
            if (fileName == null)
            {
                return;
            }

            _logger.TraceFormat("Performing export to {0}...", fileName);
            ExchangeResult? exchangeResult = null;
            OnProgress(0, 1);
            try
            {
                await Task.Run(async () => { exchangeResult = await _exporter.ExportAsync(fileName, cancellationToken).ConfigureAwait(false); }, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                OnProgress(1, 1);
            }

            if (exchangeResult?.Success ?? throw new InvalidOperationException(nameof(ExchangeResult)))
            {
                _logger.InfoFormat("Export to {0} has been performed", fileName);
                _messageHub.Publish(Texts.ExportSucceeded.ToMessage());
                Process.Start(fileName);
            }
            else
            {
                _logger.WarnFormat("Export to {0} failed", fileName);
                _messageHub.Publish(Errors.ExportFailed.ToError());
            }
        }

        public async Task ImportAsync(CancellationToken cancellationToken)
        {
            var fileName = ShowOpenFileDialog();
            if (fileName == null)
            {
                return;
            }

            OnProgress(0, 1);
            try
            {
                await Task.Run(
                        async () =>
                        {
                            foreach (var importer in _importers)
                            {
                                _logger.TraceFormat("Performing import from {0} with {1}...", fileName, importer.GetType().Name);
                                var exchangeResult = await importer.ImportAsync(fileName, cancellationToken).ConfigureAwait(false);

                                if (exchangeResult.Success)
                                {
                                    _logger.InfoFormat("Import from {0} has been performed", fileName);
                                    var mainMessage = string.Format(CultureInfo.InvariantCulture, Texts.ImportSucceeded, exchangeResult.Count);
                                    _messageHub.Publish(
                                        exchangeResult.Errors != null
                                            ? $"[{importer.GetType().Name}] {mainMessage}. {Errors.ImportErrors}:{Environment.NewLine}{string.Join(Environment.NewLine, exchangeResult.Errors)}"
                                                .ToWarning()
                                            : $"[{importer.GetType().Name}] {mainMessage}".ToMessage());
                                    return;
                                }

                                _logger.WarnFormat("ImportAsync from {0} failed", fileName);
                            }

                            _messageHub.Publish(Errors.ImportFailed.ToError());
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                OnProgress(1, 1);
            }
        }

        public void Dispose()
        {
            foreach (var importer in _importers)
            {
                importer.Progress -= ImporterExporter_Progress;
            }

            _exporter.Progress -= ImporterExporter_Progress;
        }

        void ImporterExporter_Progress(object sender, ProgressEventArgs e)
        {
            Progress?.Invoke(this, e);
        }

        void OnProgress(int current, int total)
        {
            Progress?.Invoke(this, new ProgressEventArgs(current, total));
        }

        string? ShowOpenFileDialog()
        {
            return _openFileDialog.ShowDialog() == true ? _openFileDialog.FileName : null;
        }

        string? ShowSaveFileDialog()
        {
            _saveFileDialog.FileName = $"{nameof(Remembrance)} {DateTime.Now:yyyy-MM-dd hh-mm-ss}.json";
            return _saveFileDialog.ShowDialog() == true ? _saveFileDialog.FileName : null;
        }
    }
}
