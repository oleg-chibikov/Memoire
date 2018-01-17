using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Model;
using Scar.Common.WPF.View.Contracts;

namespace Remembrance.Contracts.CardManagement
{
    public interface IWordsProcessor
    {
        [ItemNotNull]
        Task<TranslationInfo> AddOrChangeWordAsync(
            [CanBeNull] string text,
            CancellationToken cancellationToken,
            [CanBeNull] string sourceLanguage = null,
            [CanBeNull] string targetLanguage = null,
            [CanBeNull] IWindow ownerWindow = null,
            bool needPostProcess = true,
            [CanBeNull] object id = null,
            [CanBeNull] ManualTranslation[] manualTranslations = null);

        [ItemNotNull]
        Task<string> GetDefaultTargetLanguageAsync([NotNull] string sourceLanguage, CancellationToken cancellationToken);

        [ItemNotNull]
        Task<TranslationDetails> ReloadTranslationDetailsIfNeededAsync(
            [NotNull] object id,
            [NotNull] string text,
            [NotNull] string sourceLanguage,
            [NotNull] string targetLanguage,
            [CanBeNull] ManualTranslation[] manualTranslations,
            CancellationToken cancellationToken,
            [CanBeNull] Action<TranslationDetails> processNonReloaded = null);

        [ItemNotNull]
        Task<TranslationInfo> UpdateManualTranslationsAsync([NotNull] object id, [CanBeNull] ManualTranslation[] manualTranslations, CancellationToken cancellationToken);

        Task ReloadAdditionalInfoAsync([NotNull] string text, [NotNull] TranslationDetails translationDetails, CancellationToken cancellationToken);
    }
}