using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Contracts.Languages;
using Remembrance.Contracts.Languages.Data;
using Remembrance.Contracts.Translate;
using Remembrance.Resources;
using Scar.Common.WPF.Localization;

namespace Remembrance.Core.Languages
{
    [UsedImplicitly]
    internal sealed class LanguageManager : ILanguageManager
    {
        [NotNull]
        private readonly AvailableLanguagesInfo _availableLanguages;

        [NotNull]
        private readonly ILanguageDetector _languageDetector;

        [NotNull]
        private readonly ILocalSettingsRepository _localSettingsRepository;

        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly ISettingsRepository _settingsRepository;

        public LanguageManager(
            [NotNull] ILocalSettingsRepository localSettingsRepository,
            [NotNull] ISettingsRepository settingsRepository,
            [NotNull] ILog logger,
            [NotNull] ILanguageDetector languageDetector)
        {
            _localSettingsRepository = localSettingsRepository ?? throw new ArgumentNullException(nameof(localSettingsRepository));
            _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _languageDetector = languageDetector ?? throw new ArgumentNullException(nameof(languageDetector));

            var cachedLanguages = _localSettingsRepository.AvailableLanguages;
            if (DateTime.Now > _localSettingsRepository.AvailableLanguagesModifiedDate + TimeSpan.FromDays(7))
            {
                cachedLanguages = null;
            }

            _availableLanguages = cachedLanguages ?? ReloadAvailableLanguagesAsync().Result;
        }

        public async Task<string> GetSourceAutoSubstituteAsync(string text, CancellationToken cancellationToken)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var detectionResult = await _languageDetector.DetectLanguageAsync(text, cancellationToken).ConfigureAwait(false);
            return detectionResult.Language;
        }

        public string GetTargetAutoSubstitute(string sourceLanguage)
        {
            if (sourceLanguage == null)
            {
                throw new ArgumentNullException(nameof(sourceLanguage));
            }

            var availableTargetLanguages = _availableLanguages.Directions[sourceLanguage];

            if (!availableTargetLanguages.Any())
            {
                throw new InvalidOperationException("No target languages available");
            }

            var ordered = availableTargetLanguages.OrderBy(
                languageCode => GetLanguageOrder(
                    languageCode,
                    _settingsRepository.PreferredLanguage,
                    CultureUtilities.GetCurrentCulture().TwoLetterISOLanguageName,
                    _localSettingsRepository.LastUsedTargetLanguage));

            return ordered.First();
        }

        public bool CheckTargetLanguageIsValid(string sourceLanguage, [NotNull] string targetLanguage)
        {
            if (sourceLanguage == null)
            {
                throw new ArgumentNullException(nameof(sourceLanguage));
            }

            if (targetLanguage == null)
            {
                throw new ArgumentNullException(nameof(targetLanguage));
            }

            var availableTargetLanguages = _availableLanguages.Directions[sourceLanguage];
            return availableTargetLanguages.Contains(targetLanguage);
        }

        public LanguagesCollection GetAvailableSourceLanguages(bool addAuto)
        {
            var languages = _availableLanguages.Languages.Where(availableLanguage => _availableLanguages.Directions.Keys.Contains(availableLanguage.Key))
                .ToDictionary(language => language.Key, language => language.Value);

            if (!languages.Any())
            {
                throw new InvalidOperationException("No source languages available");
            }

            var lastUsed = _localSettingsRepository.LastUsedSourceLanguage;
            var toSelect = lastUsed;

            if (addAuto)
            {
                languages[Constants.AutoDetectLanguage] = "--AutoDetect--";
                toSelect = toSelect ?? Constants.AutoDetectLanguage;
            }

            var ordered = languages
                .OrderBy(language => GetLanguageOrder(language.Key, _settingsRepository.PreferredLanguage, CultureUtilities.GetCurrentCulture().TwoLetterISOLanguageName, lastUsed))
                .ThenBy(x => x.Value)
                .Select(language => new Language(language.Key, language.Value));

            return new LanguagesCollection(ordered, toSelect);
        }

        public LanguagesCollection GetAvailableTargetLanguages(string sourceLanguage)
        {
            if (sourceLanguage == null)
            {
                throw new ArgumentNullException(nameof(sourceLanguage));
            }

            IDictionary<string, string> languages;

            if (sourceLanguage == Constants.AutoDetectLanguage)
            {
                languages = _availableLanguages.Languages;
            }
            else
            {
                var acceptableTargetLanguages = _availableLanguages.Directions[sourceLanguage];
                if (!acceptableTargetLanguages.Any())
                {
                    throw new InvalidOperationException("No target languages available");
                }

                languages = _availableLanguages.Languages.Where(language => acceptableTargetLanguages.Contains(language.Key))
                    .ToDictionary(language => language.Key, language => language.Value);
            }

            languages[Constants.AutoDetectLanguage] = "--Reverse--";

            var lastUsed = _localSettingsRepository.LastUsedTargetLanguage;
            var toSelect = lastUsed != null && languages.ContainsKey(lastUsed) ? lastUsed : Constants.AutoDetectLanguage;
            var ordered = languages
                .OrderBy(language => GetLanguageOrder(language.Key, _settingsRepository.PreferredLanguage, CultureUtilities.GetCurrentCulture().TwoLetterISOLanguageName, lastUsed))
                .ThenBy(x => x.Value)
                .Select(language => new Language(language.Key, language.Value));
            return new LanguagesCollection(ordered, toSelect);
        }

        private static int GetLanguageOrder([NotNull] string languageCode, [NotNull] string preferred, [NotNull] string currentCultureLanguage, [CanBeNull] string lastUsed)
        {
            return languageCode == Constants.AutoDetectLanguage ? 1 :
                languageCode == preferred ? 2 :
                languageCode == currentCultureLanguage ? 3 :
                languageCode == Constants.EnLanguageTwoLetters ? 4 :
                lastUsed != null && languageCode == lastUsed ? 5 : 6;
        }

        [NotNull]
        [ItemNotNull]
        private async Task<AvailableLanguagesInfo> ReloadAvailableLanguagesAsync()
        {
            _logger.Trace("Loading available languages...");
            var languageListResult = await _languageDetector.ListLanguagesAsync(CultureUtilities.GetCurrentCulture().TwoLetterISOLanguageName, CancellationToken.None)
                .ConfigureAwait(false);
            if (languageListResult == null)
            {
                throw new InvalidOperationException("No languages info available");
            }

            var directions = new Dictionary<string, HashSet<string>>();

            foreach (var direction in languageListResult.Directions)
            {
                var split = direction.Split('-');
                var from = split[0];
                var to = split[1];
                if (directions.ContainsKey(from))
                {
                    directions[from].Add(to);
                }
                else
                {
                    directions[from] = new HashSet<string>
                    {
                        to
                    };
                }
            }

            var availableLanguages = new AvailableLanguagesInfo
            {
                Directions = directions,
                Languages = languageListResult.Languages
            };
            _localSettingsRepository.AvailableLanguages = availableLanguages;
            return availableLanguages;
        }
    }
}