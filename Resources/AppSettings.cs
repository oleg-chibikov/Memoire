using System;
using System.Configuration;
using System.Globalization;

namespace Mémoire.Resources
{
    public static class AppSettings
    {
        static readonly Lazy<int> DictionaryPageSizeLazy = new Lazy<int>(
            () => int.Parse(
                ConfigurationManager.AppSettings[nameof(DictionaryPageSize)] ?? throw new InvalidOperationException(nameof(DictionaryPageSize) + " is null"),
                CultureInfo.InvariantCulture));

        static readonly Lazy<TimeSpan> MessageCloseTimeoutLazy = new Lazy<TimeSpan>(
            () => TimeSpan.Parse(
                ConfigurationManager.AppSettings[nameof(MessageCloseTimeout)] ?? throw new InvalidOperationException(nameof(MessageCloseTimeout) + " is null"),
                CultureInfo.InvariantCulture));

        static readonly Lazy<TimeSpan> AssessmentCardSuccessCloseTimeoutLazy = new Lazy<TimeSpan>(
            () => TimeSpan.Parse(
                ConfigurationManager.AppSettings[nameof(AssessmentCardSuccessCloseTimeout)] ?? throw new InvalidOperationException(nameof(AssessmentCardSuccessCloseTimeout) + " is null"),
                CultureInfo.InvariantCulture));

        static readonly Lazy<TimeSpan> AssessmentCardFailureCloseTimeoutLazy = new Lazy<TimeSpan>(
            () => TimeSpan.Parse(
                ConfigurationManager.AppSettings[nameof(AssessmentCardFailureCloseTimeout)] ?? throw new InvalidOperationException(nameof(AssessmentCardFailureCloseTimeout) + " is null"),
                CultureInfo.InvariantCulture));

        static readonly Lazy<TimeSpan> TranslationCardCloseTimeoutLazy = new Lazy<TimeSpan>(
            () => TimeSpan.Parse(
                ConfigurationManager.AppSettings[nameof(TranslationCardCloseTimeout)] ?? throw new InvalidOperationException(nameof(TranslationCardCloseTimeout) + " is null"),
                CultureInfo.InvariantCulture));

        public static TimeSpan AssessmentCardFailureCloseTimeout => AssessmentCardFailureCloseTimeoutLazy.Value;

        public static TimeSpan AssessmentCardSuccessCloseTimeout => AssessmentCardSuccessCloseTimeoutLazy.Value;

        public static int DictionaryPageSize => DictionaryPageSizeLazy.Value;

        public static TimeSpan MessageCloseTimeout => MessageCloseTimeoutLazy.Value;

        public static TimeSpan TranslationCardCloseTimeout => TranslationCardCloseTimeoutLazy.Value;
    }
}
