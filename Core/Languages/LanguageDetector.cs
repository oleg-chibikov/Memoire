using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Scar.Services.Contracts;
using Scar.Services.Contracts.Data;
using Scar.Services.Contracts.Data.LanguageDetection;

namespace Mémoire.Core.Languages
{
    public sealed class LanguageDetector : ILanguageDetector
    {
        const double Threshold = 0.02;
        const int ThresholdMultiplier = 3;
        readonly LanguageDetection.LanguageDetector _allLanguagesDetector;

        public LanguageDetector(ILogger<LanguageDetector> logger)
        {
            _ = logger ?? throw new ArgumentNullException(nameof(logger));
            logger.LogTrace("Initializing {Type}...", GetType().Name);
            _allLanguagesDetector = new LanguageDetection.LanguageDetector();
            _allLanguagesDetector.AddAllLanguages();
            _allLanguagesDetector.ProbabilityThreshold = Threshold;
            _allLanguagesDetector.ConvergenceThreshold = Threshold;
            logger.LogDebug("Initialized {Type}", GetType().Name);
        }

        public Task<DetectionResult> DetectLanguageAsync(string text, Action<Exception>? handleError = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ConvertLanguage(GetLanguage(text, handleError)));
        }

        public Task<LanguageListResult?> ListLanguagesAsync(string ui = "en", Action<Exception>? handleError = null, CancellationToken cancellationToken = default)
        {
            const string directionsJson =
                "[\"be-be\",\"be-ru\",\"bg-ru\",\"cs-cs\",\"cs-en\",\"cs-ru\",\"da-en\",\"da-ru\",\"de-de\",\"de-en\",\"de-ru\",\"de-tr\",\"el-en\",\"el-ru\",\"en-cs\",\"en-da\",\"en-de\",\"en-el\",\"en-en\",\"en-es\",\"en-et\",\"en-fi\",\"en-fr\",\"en-it\",\"en-lt\",\"en-lv\",\"en-nl\",\"en-no\",\"en-pt\",\"en-ru\",\"en-sk\",\"en-sv\",\"en-tr\",\"en-uk\",\"es-en\",\"es-es\",\"es-ru\",\"et-en\",\"et-ru\",\"fi-en\",\"fi-fi\",\"fi-ru\",\"fr-en\",\"fr-fr\",\"fr-ru\",\"hu-hu\",\"hu-ru\",\"it-en\",\"it-it\",\"it-ru\",\"lt-en\",\"lt-lt\",\"lt-ru\",\"lv-en\",\"lv-ru\",\"mhr-ru\",\"mrj-ru\",\"nl-en\",\"nl-ru\",\"no-en\",\"no-ru\",\"pl-ru\",\"pt-en\",\"pt-ru\",\"ru-be\",\"ru-bg\",\"ru-cs\",\"ru-da\",\"ru-de\",\"ru-el\",\"ru-en\",\"ru-es\",\"ru-et\",\"ru-fi\",\"ru-fr\",\"ru-hu\",\"ru-it\",\"ru-lt\",\"ru-lv\",\"ru-mhr\",\"ru-mrj\",\"ru-nl\",\"ru-no\",\"ru-pl\",\"ru-pt\",\"ru-ru\",\"ru-sk\",\"ru-sv\",\"ru-tr\",\"ru-tt\",\"ru-uk\",\"ru-zh\",\"sk-en\",\"sk-ru\",\"sv-en\",\"sv-ru\",\"tr-de\",\"tr-en\",\"tr-ru\",\"tt-ru\",\"uk-en\",\"uk-ru\",\"uk-uk\",\"zh-ru\"]";

            const string languagesJson =
                "{\"af\":\"Afrikaans\",\"sq\":\"Albanian\",\"am\":\"Amharic\",\"ar\":\"Arabic\",\"hy\":\"Armenian\",\"az\":\"Azerbaijani\",\"ba\":\"Bashkir\",\"eu\":\"Basque\",\"be\":\"Belarusian\",\"bn\":\"Bengali\",\"bs\":\"Bosnian\",\"bg\":\"Bulgarian\",\"my\":\"Burmese\",\"ca\":\"Catalan\",\"ceb\":\"Cebuano\",\"zh\":\"Chinese\",\"cv\":\"Chuvash\",\"hr\":\"Croatian\",\"cs\":\"Czech\",\"da\":\"Danish\",\"nl\":\"Dutch\",\"sjn\":\"Elvish (Sindarin)\",\"emj\":\"Emoji\",\"en\":\"English\",\"eo\":\"Esperanto\",\"et\":\"Estonian\",\"fi\":\"Finnish\",\"fr\":\"French\",\"gl\":\"Galician\",\"ka\":\"Georgian\",\"de\":\"German\",\"el\":\"Greek\",\"gu\":\"Gujarati\",\"ht\":\"Haitian\",\"he\":\"Hebrew\",\"mrj\":\"Hill Mari\",\"hi\":\"Hindi\",\"hu\":\"Hungarian\",\"is\":\"Icelandic\",\"id\":\"Indonesian\",\"ga\":\"Irish\",\"it\":\"Italian\",\"ja\":\"Japanese\",\"jv\":\"Javanese\",\"kn\":\"Kannada\",\"kk\":\"Kazakh\",\"kazlat\":\"Kazakh (Latin)\",\"km\":\"Khmer\",\"ko\":\"Korean\",\"ky\":\"Kyrgyz\",\"lo\":\"Lao\",\"la\":\"Latin\",\"lv\":\"Latvian\",\"lt\":\"Lithuanian\",\"lb\":\"Luxembourgish\",\"mk\":\"Macedonian\",\"mg\":\"Malagasy\",\"ms\":\"Malay\",\"ml\":\"Malayalam\",\"mt\":\"Maltese\",\"mi\":\"Maori\",\"mr\":\"Marathi\",\"mhr\":\"Mari\",\"mn\":\"Mongolian\",\"ne\":\"Nepali\",\"no\":\"Norwegian\",\"pap\":\"Papiamento\",\"fa\":\"Persian\",\"pl\":\"Polish\",\"pt\":\"Portuguese\",\"pa\":\"Punjabi\",\"ro\":\"Romanian\",\"ru\":\"Russian\",\"gd\":\"Scottish Gaelic\",\"sr\":\"Serbian\",\"si\":\"Sinhalese\",\"sk\":\"Slovak\",\"sl\":\"Slovenian\",\"es\":\"Spanish\",\"su\":\"Sundanese\",\"sw\":\"Swahili\",\"sv\":\"Swedish\",\"tl\":\"Tagalog\",\"tg\":\"Tajik\",\"ta\":\"Tamil\",\"tt\":\"Tatar\",\"te\":\"Telugu\",\"th\":\"Thai\",\"tr\":\"Turkish\",\"udm\":\"Udmurt\",\"uk\":\"Ukrainian\",\"ur\":\"Urdu\",\"uz\":\"Uzbek\",\"uzbcyr\":\"Uzbek (Cyrillic)\",\"vi\":\"Vietnamese\",\"cy\":\"Welsh\",\"xh\":\"Xhosa\",\"sah\":\"Yakut\",\"yi\":\"Yiddish\"}";

            var deserializedDirections = JsonConvert.DeserializeObject<string[]>(directionsJson);
            var deserializedLanguages = JsonConvert.DeserializeObject<Dictionary<string, string>>(languagesJson);
            var languageListResult = new LanguageListResult { Directions = deserializedDirections, Languages = deserializedLanguages };
            return Task.FromResult<LanguageListResult?>(languageListResult);
        }

        static DetectionResult ConvertLanguage(string language)
        {
            var twoLetters = language[..2].ToLowerInvariant();
            var secondPart = twoLetters == "en" ? "US" : twoLetters.ToUpperInvariant();
            return new DetectionResult { Code = twoLetters, Language = $"{twoLetters}-{secondPart}" };
        }

        string GetLanguage(string text, Action<Exception>? handleError)
        {
            var detectedLanguages = _allLanguagesDetector.DetectAll(text).ToDictionary(x => x.Language, x => x.Probability);
            if (detectedLanguages.Count == 0)
            {
                handleError?.Invoke(new InvalidOperationException($"Cannot detect language for {text}"));
                return LanguageConstants.EnLanguageTwoLetters;
            }

            var top = detectedLanguages.First();
            var ruEnThreshold = top.Value / ThresholdMultiplier;

            if (detectedLanguages.TryGetValue("eng", out var probability) && probability > ruEnThreshold)
            {
                return "eng";
            }

            if (detectedLanguages.TryGetValue("rus", out probability) && probability > ruEnThreshold)
            {
                return "rus";
            }

            var first = detectedLanguages.Keys.First();
            return first;
        }
    }
}
