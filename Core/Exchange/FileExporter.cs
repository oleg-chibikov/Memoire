using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.DAL.SharedBetweenMachines;
using Mémoire.Contracts.Exchange;
using Mémoire.Contracts.Exchange.Data;
using Mémoire.Core.CardManagement.Data;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Scar.Common.Events;

namespace Mémoire.Core.Exchange
{
    sealed class FileExporter : IFileExporter
    {
        static readonly JsonSerializerSettings ExportEntrySerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new TranslationEntryContractResolver(),
            NullValueHandling = NullValueHandling.Ignore
        };

        readonly ILearningInfoRepository _learningInfoRepository;
        readonly ILogger _logger;
        readonly ITranslationEntryRepository _translationEntryRepository;

        public FileExporter(ITranslationEntryRepository translationEntryRepository, ILogger<FileExporter> logger, ILearningInfoRepository learningInfoRepository)
        {
            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _learningInfoRepository = learningInfoRepository ?? throw new ArgumentNullException(nameof(learningInfoRepository));
        }

        public event EventHandler<ProgressEventArgs>? Progress;

        public ExchangeResult Export(string fileName)
        {
            _ = fileName ?? throw new ArgumentNullException(nameof(fileName));
            var translationEntries = _translationEntryRepository.GetAll();
            var learningInfos = _learningInfoRepository.GetAll().ToDictionary(x => x.Id, x => x);
            var exportEntries = new List<ExchangeEntry>(translationEntries.Count);
            var totalCount = translationEntries.Count;
            var count = 0;
            foreach (var translationEntry in translationEntries)
            {
                learningInfos.TryGetValue(translationEntry.Id, out var learningInfo);
                exportEntries.Add(new ExchangeEntry(translationEntry, learningInfo ?? new LearningInfo()));
                OnProgress(Interlocked.Increment(ref count), totalCount);
            }

            try
            {
                var json = JsonConvert.SerializeObject(exportEntries, Formatting.Indented, ExportEntrySerializerSettings);
                File.WriteAllText(fileName, json);
                return new ExchangeResult(true, null, exportEntries.Count);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Cannot save file to disk");
            }
            catch (JsonSerializationException ex)
            {
                _logger.LogWarning(ex, "Cannot serialize object");
            }

            return new ExchangeResult(false, null, 0);
        }

        void OnProgress(int current, int total)
        {
            Progress?.Invoke(this, new ProgressEventArgs(current, total));
        }
    }
}
