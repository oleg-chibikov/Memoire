﻿using System;
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
    internal class CardsExchanger : ICardsExchanger
    {
        private const string JsonFilesFilter = "Json files (*.json)|*.json;";

        [NotNull]
        private readonly IFileExporter exporter;

        [NotNull]
        private readonly IFileImporter[] importers;

        [NotNull]
        private readonly ILog logger;

        [NotNull]
        private readonly IMessenger messenger;

        [NotNull]
        private readonly IOpenFileService openFileService;

        [NotNull]
        private readonly ISaveFileService saveFileService;

        public CardsExchanger(
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] IOpenFileService openFileService,
            [NotNull] ISaveFileService saveFileService,
            [NotNull] ILog logger,
            [NotNull] IWordsChecker wordsChecker,
            [NotNull] IFileExporter exporter,
            [NotNull] IFileImporter[] importers,
            [NotNull] IMessenger messenger)
        {
            if (openFileService == null)
                throw new ArgumentNullException(nameof(openFileService));
            if (saveFileService == null)
                throw new ArgumentNullException(nameof(saveFileService));
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            if (wordsChecker == null)
                throw new ArgumentNullException(nameof(wordsChecker));
            if (exporter == null)
                throw new ArgumentNullException(nameof(exporter));
            if (importers == null)
                throw new ArgumentNullException(nameof(importers));
            if (messenger == null)
                throw new ArgumentNullException(nameof(messenger));

            this.openFileService = openFileService;
            this.saveFileService = saveFileService;
            this.logger = logger;
            this.exporter = exporter;
            this.importers = importers;
            this.messenger = messenger;
        }

        public void Export()
        {
            if (!saveFileService.SaveFileDialog(Texts.Title, JsonFilesFilter))
                return;
            logger.Info($"Performing export to {saveFileService.FileName}...");
            if (exporter.Export(saveFileService.FileName))
            {
                logger.Info($"Export to {saveFileService.FileName} has been performed");
                messenger.Send(Texts.ExportSucceeded, MessengerTokens.UserMessageToken);
            }
            else
            {
                logger.Warn($"Export to {saveFileService.FileName} failed");
                messenger.Send(Texts.ExportFailed, MessengerTokens.UserWarningToken);
            }
        }

        public void Import()
        {
            if (!openFileService.OpenFileDialog(Texts.Title, JsonFilesFilter))
                return;
            foreach (var importer in importers)
            {
                logger.Info($"Performing import from {openFileService.FileName} with {importer.GetType().Name}...");
                string[] errors;
                int count;
                if (importer.Import(openFileService.FileName, out errors, out count))
                {
                    logger.Info($"Import from {openFileService.FileName} has been performed");
                    var mainMessage = string.Format(Texts.ImportSucceeded, count);
                    if (errors != null)
                        messenger.Send($"[{importer.GetType().Name}] {mainMessage}. {Texts.ImportErrors}:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}", MessengerTokens.UserWarningToken);
                    else
                        messenger.Send($"[{importer.GetType().Name}] {mainMessage}", MessengerTokens.UserMessageToken);
                    return;
                }
            }
            logger.Warn($"Import from {openFileService.FileName} failed");
            messenger.Send(Texts.ImportFailed, MessengerTokens.UserWarningToken);
        }
    }
}