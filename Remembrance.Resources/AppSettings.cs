using System;
using System.Configuration;
using System.Globalization;

namespace Remembrance.Resources
{
    public static class AppSettings
    {
        static readonly Lazy<int> DictionaryPageSizeLazy = new Lazy<int>(() => int.Parse(ConfigurationManager.AppSettings[nameof(DictionaryPageSize)], CultureInfo.InvariantCulture));

        static readonly Lazy<TimeSpan> MessageCloseTimeoutLazy = new Lazy<TimeSpan>(() => TimeSpan.Parse(ConfigurationManager.AppSettings[nameof(MessageCloseTimeout)], CultureInfo.InvariantCulture));

        static readonly Lazy<TimeSpan> AssessmentCardSuccessCloseTimeoutLazy =
            new Lazy<TimeSpan>(() => TimeSpan.Parse(ConfigurationManager.AppSettings[nameof(AssessmentCardSuccessCloseTimeout)], CultureInfo.InvariantCulture));

        static readonly Lazy<TimeSpan> AssessmentCardFailureCloseTimeoutLazy =
            new Lazy<TimeSpan>(() => TimeSpan.Parse(ConfigurationManager.AppSettings[nameof(AssessmentCardFailureCloseTimeout)], CultureInfo.InvariantCulture));

        static readonly Lazy<TimeSpan> TranslationCardCloseTimeoutLazy =
            new Lazy<TimeSpan>(() => TimeSpan.Parse(ConfigurationManager.AppSettings[nameof(TranslationCardCloseTimeout)], CultureInfo.InvariantCulture));

        public static TimeSpan AssessmentCardFailureCloseTimeout => AssessmentCardFailureCloseTimeoutLazy.Value;

        public static TimeSpan AssessmentCardSuccessCloseTimeout => AssessmentCardSuccessCloseTimeoutLazy.Value;

        public static int DictionaryPageSize => DictionaryPageSizeLazy.Value;

        public static TimeSpan MessageCloseTimeout => MessageCloseTimeoutLazy.Value;

        public static TimeSpan TranslationCardCloseTimeout => TranslationCardCloseTimeoutLazy.Value;
    }
}
