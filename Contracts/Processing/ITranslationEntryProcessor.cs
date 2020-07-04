using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.Processing.Data;
using Scar.Common.View.Contracts;

namespace Mémoire.Contracts.Processing
{
    public interface ITranslationEntryProcessor
    {
        Task<TranslationInfo?> AddOrUpdateTranslationEntryAsync(
            TranslationEntryAdditionInfo translationEntryAdditionInfo,
            IDisplayable? ownerWindow = null,
            bool needPostProcess = true,
            IReadOnlyCollection<ManualTranslation>? manualTranslations = null,
            CancellationToken cancellationToken = default);

        void DeleteTranslationEntry(TranslationEntryKey translationEntryKey, bool needDeletionRecord = true);

        Task<TranslationDetails> ReloadTranslationDetailsIfNeededAsync(
            TranslationEntryKey translationEntryKey,
            IReadOnlyCollection<ManualTranslation>? manualTranslations,
            CancellationToken cancellationToken = default);

        Task<TranslationInfo> UpdateManualTranslationsAsync(TranslationEntryKey translationEntryKey, IReadOnlyCollection<ManualTranslation>? manualTranslations, CancellationToken cancellationToken = default);
    }
}
