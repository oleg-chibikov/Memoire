using System;
using System.Configuration;
using JetBrains.Annotations;

namespace Remembrance.Resources
{
    public static class AppSettings
    {
        [NotNull]
        private static readonly Lazy<int> DictionaryPageSizeLazy = new Lazy<int>(() => int.Parse(ConfigurationManager.AppSettings[nameof(DictionaryPageSize)]));

        [NotNull]
        private static readonly Lazy<TimeSpan> MessageCloseTimeoutLazy = new Lazy<TimeSpan>(() => TimeSpan.Parse(ConfigurationManager.AppSettings[nameof(MessageCloseTimeout)]));

        [NotNull]
        private static readonly Lazy<TimeSpan> AssessmentCardSuccessCloseTimeoutLazy = new Lazy<TimeSpan>(() => TimeSpan.Parse(ConfigurationManager.AppSettings[nameof(AssessmentCardSuccessCloseTimeout)]));

        [NotNull]
        private static readonly Lazy<TimeSpan> AssessmentCardFailureCloseTimeoutLazy = new Lazy<TimeSpan>(() => TimeSpan.Parse(ConfigurationManager.AppSettings[nameof(AssessmentCardFailureCloseTimeout)]));

        [NotNull]
        private static readonly Lazy<TimeSpan> TranslationCardCloseTimeoutLazy = new Lazy<TimeSpan>(() => TimeSpan.Parse(ConfigurationManager.AppSettings[nameof(TranslationCardCloseTimeout)]));

        public static TimeSpan AssessmentCardFailureCloseTimeout => AssessmentCardFailureCloseTimeoutLazy.Value;

        public static TimeSpan AssessmentCardSuccessCloseTimeout => AssessmentCardSuccessCloseTimeoutLazy.Value;

        public static int DictionaryPageSize => DictionaryPageSizeLazy.Value;

        public static TimeSpan MessageCloseTimeout => MessageCloseTimeoutLazy.Value;

        public static TimeSpan TranslationCardCloseTimeout => TranslationCardCloseTimeoutLazy.Value;
    }
}