using System;
using System.Configuration;

namespace Remembrance.Resources
{
    public static class AppSettings
    {
        static readonly Lazy<int> DictionaryPageSizeLazy = new Lazy<int>(() => int.Parse(ConfigurationManager.AppSettings[nameof(DictionaryPageSize)]));

        static readonly Lazy<TimeSpan> MessageCloseTimeoutLazy = new Lazy<TimeSpan>(() => TimeSpan.Parse(ConfigurationManager.AppSettings[nameof(MessageCloseTimeout)]));

        static readonly Lazy<TimeSpan> AssessmentCardSuccessCloseTimeoutLazy =
            new Lazy<TimeSpan>(() => TimeSpan.Parse(ConfigurationManager.AppSettings[nameof(AssessmentCardSuccessCloseTimeout)]));

        static readonly Lazy<TimeSpan> AssessmentCardFailureCloseTimeoutLazy =
            new Lazy<TimeSpan>(() => TimeSpan.Parse(ConfigurationManager.AppSettings[nameof(AssessmentCardFailureCloseTimeout)]));

        static readonly Lazy<TimeSpan> TranslationCardCloseTimeoutLazy =
            new Lazy<TimeSpan>(() => TimeSpan.Parse(ConfigurationManager.AppSettings[nameof(TranslationCardCloseTimeout)]));

        public static TimeSpan AssessmentCardFailureCloseTimeout => AssessmentCardFailureCloseTimeoutLazy.Value;

        public static TimeSpan AssessmentCardSuccessCloseTimeout => AssessmentCardSuccessCloseTimeoutLazy.Value;

        public static int DictionaryPageSize => DictionaryPageSizeLazy.Value;

        public static TimeSpan MessageCloseTimeout => MessageCloseTimeoutLazy.Value;

        public static TimeSpan TranslationCardCloseTimeout => TranslationCardCloseTimeoutLazy.Value;
    }
}