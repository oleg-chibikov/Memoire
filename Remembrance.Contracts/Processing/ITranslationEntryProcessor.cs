using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Processing.Data;
using Scar.Common.View.Contracts;

namespace Remembrance.Contracts.Processing
{
    public interface ITranslationEntryProcessor
    {
        Task<TranslationInfo?> AddOrUpdateTranslationEntryAsync(
            TranslationEntryAdditionInfo translationEntryAdditionInfo,
            CancellationToken cancellationToken,
            IDisplayable? ownerWindow = null,
            bool needPostProcess = true,
            IReadOnlyCollection<ManualTranslation>? manualTranslations = null);

        void DeleteTranslationEntry(TranslationEntryKey translationEntryKey, bool needDeletionRecord = true);

        Task<TranslationDetails> ReloadTranslationDetailsIfNeededAsync(
            TranslationEntryKey translationEntryKey,
            IReadOnlyCollection<ManualTranslation>? manualTranslations,
            CancellationToken cancellationToken);

        Task<TranslationInfo> UpdateManualTranslationsAsync(
            TranslationEntryKey translationEntryKey,
            IReadOnlyCollection<ManualTranslation>? manualTranslations,
            CancellationToken cancellationToken);
    }
}