using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Processing.Data;
using Scar.Common.View.Contracts;

namespace Remembrance.Contracts.Processing
{
    public interface ITranslationEntryProcessor
    {
        [ItemCanBeNull]
        [NotNull]
        Task<TranslationInfo> AddOrUpdateTranslationEntryAsync(
            [NotNull] TranslationEntryAdditionInfo translationEntryAdditionInfo,
            CancellationToken cancellationToken,
            [CanBeNull] IDisplayable ownerWindow = null,
            bool needPostProcess = true,
            [CanBeNull] IReadOnlyCollection<ManualTranslation> manualTranslations = null);

        void DeleteTranslationEntry([NotNull] TranslationEntryKey translationEntryKey, bool needDeletionRecord = true);

        [ItemNotNull]
        [NotNull]
        Task<TranslationDetails> ReloadTranslationDetailsIfNeededAsync(
            [NotNull] TranslationEntryKey translationEntryKey,
            [CanBeNull] IReadOnlyCollection<ManualTranslation> manualTranslations,
            CancellationToken cancellationToken);

        [ItemNotNull]
        [NotNull]
        Task<TranslationInfo> UpdateManualTranslationsAsync(
            [NotNull] TranslationEntryKey translationEntryKey,
            [CanBeNull] IReadOnlyCollection<ManualTranslation> manualTranslations,
            CancellationToken cancellationToken);
    }
}