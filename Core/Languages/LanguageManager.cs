using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Easy.MessageHub;
using Mémoire.Contracts.DAL.Local;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.DAL.SharedBetweenMachines;
using Mémoire.Contracts.Languages;
using Mémoire.Contracts.Languages.Data;
using Mémoire.Resources;
using Microsoft.Extensions.Logging;
using Scar.Common.Messages;
using Scar.Services.Contracts;
using Scar.Services.Contracts.Data;

namespace Mémoire.Core.Languages
{
    sealed class LanguageManager : ILanguageManager
    {
        readonly IMessageHub _messageHub;
        readonly AvailableLanguagesInfo _availableLanguages;
        readonly ILanguageDetector _languageDetector;
        readonly ILocalSettingsRepository _localSettingsRepository;
        readonly ILogger _logger;
        readonly ISharedSettingsRepository _sharedSettingsRepository;

        public LanguageManager(
            ILocalSettingsRepository localSettingsRepository,
            ISharedSettingsRepository sharedSettingsRepository,
            ILogger<LanguageManager> logger,
            ILanguageDetector languageDetector,
            IMessageHub messageHub)
        {
            _localSettingsRepository = localSettingsRepository ?? throw new ArgumentNullException(nameof(localSettingsRepository));
            _sharedSettingsRepository = sharedSettingsRepository ?? throw new ArgumentNullException(nameof(sharedSettingsRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _languageDetector = languageDetector ?? throw new ArgumentNullException(nameof(languageDetector));
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));

            var cachedLanguages = _localSettingsRepository.AvailableLanguages;
            if (DateTimeOffset.Now > (_localSettingsRepository.AvailableLanguagesModifiedDate + TimeSpan.FromDays(7)))
            {
                cachedLanguages = null;
            }

            _availableLanguages = cachedLanguages ?? ReloadAvailableLanguagesAsync().Result;
        }

        public bool CheckTargetLanguageIsValid(string sourceLanguage, string targetLanguage)
        {
            _ = sourceLanguage ?? throw new ArgumentNullException(nameof(sourceLanguage));
            _ = targetLanguage ?? throw new ArgumentNullException(nameof(targetLanguage));
            var availableTargetLanguages = _availableLanguages.Directions[sourceLanguage];
            return availableTargetLanguages.Contains(targetLanguage);
        }

        public IReadOnlyDictionary<string, string> GetAvailableLanguages()
        {
            return _availableLanguages.Languages;
        }

        public LanguagesCollection GetAvailableSourceLanguages(bool addAuto)
        {
            var languages = _availableLanguages.Languages.Where(availableLanguage => _availableLanguages.Directions.Keys.Contains(availableLanguage.Key))
                .ToDictionary(language => language.Key, language => language.Value);

            if (!(languages.Count > 0))
            {
                throw new InvalidOperationException("No source languages available");
            }

            var lastUsed = _localSettingsRepository.LastUsedSourceLanguage;
            var toSelect = lastUsed;

            if (addAuto)
            {
                languages[LanguageConstants.AutoDetectLanguage] = "--AutoDetect--";
                toSelect ??= LanguageConstants.AutoDetectLanguage;
            }

            var ordered = languages
                .OrderBy(language => GetLanguageOrder(language.Key, _sharedSettingsRepository.PreferredLanguage, Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName, lastUsed))
                .ThenBy(x => x.Value)
                .Select(language => new Language(language.Key, language.Value));

            return new LanguagesCollection(ordered, toSelect);
        }

        public LanguagesCollection GetAvailableTargetLanguages(string sourceLanguage)
        {
            _ = sourceLanguage ?? throw new ArgumentNullException(nameof(sourceLanguage));
            IDictionary<string, string> languages;

            if (sourceLanguage == LanguageConstants.AutoDetectLanguage)
            {
                languages = _availableLanguages.Languages.ToDictionary(x => x.Key, x => x.Value);
            }
            else
            {
                var acceptableTargetLanguages = _availableLanguages.Directions[sourceLanguage];
                if (!(acceptableTargetLanguages.Count > 0))
                {
                    throw new InvalidOperationException("No target languages available");
                }

                languages = _availableLanguages.Languages.Where(language => acceptableTargetLanguages.Contains(language.Key)).ToDictionary(language => language.Key, language => language.Value);
            }

            languages[LanguageConstants.AutoDetectLanguage] = "--Reverse--";

            var lastUsed = _localSettingsRepository.LastUsedTargetLanguage;
            var toSelect = (lastUsed != null) && languages.ContainsKey(lastUsed) ? lastUsed : LanguageConstants.AutoDetectLanguage;
            var ordered = languages
                .OrderBy(language => GetLanguageOrder(language.Key, _sharedSettingsRepository.PreferredLanguage, Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName, lastUsed))
                .ThenBy(x => x.Value)
                .Select(language => new Language(language.Key, language.Value));
            return new LanguagesCollection(ordered, toSelect);
        }

        public async Task<string> GetSourceAutoSubstituteAsync(string text, CancellationToken cancellationToken)
        {
            _ = text ?? throw new ArgumentNullException(nameof(text));
            var detectionResult = await _languageDetector.DetectLanguageAsync(text, ex => _messageHub.Publish(Errors.CannotDetectLanguage.ToError(ex)), cancellationToken).ConfigureAwait(false);
            return detectionResult.Code;
        }

        public string GetTargetAutoSubstitute(string sourceLanguage)
        {
            _ = sourceLanguage ?? throw new ArgumentNullException(nameof(sourceLanguage));
            if (!_availableLanguages.Directions.TryGetValue(sourceLanguage, out var availableTargetLanguages) || !(availableTargetLanguages.Count > 0))
            {
                _logger.LogWarning($"No target languages available for source language {sourceLanguage}");
                return LanguageConstants.EnLanguageTwoLetters;
            }

            var ordered = availableTargetLanguages.Where(x => x != sourceLanguage)
                .OrderBy(
                    languageCode => GetLanguageOrder(
                        languageCode,
                        _sharedSettingsRepository.PreferredLanguage,
                        Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName,
                        _localSettingsRepository.LastUsedTargetLanguage));

            return ordered.First();
        }

        static int GetLanguageOrder(string languageCode, string preferred, string currentCultureLanguage, string? lastUsed)
        {
            return languageCode == LanguageConstants.AutoDetectLanguage ? 1 :
                languageCode == preferred ? 2 :
                languageCode == currentCultureLanguage ? 3 :
                languageCode == LanguageConstants.EnLanguageTwoLetters ? 4 :
                (lastUsed != null) && (languageCode == lastUsed) ? 5 : 6;
        }

        async Task<AvailableLanguagesInfo> ReloadAvailableLanguagesAsync()
        {
            _logger.LogTrace("Loading available languages...");
            var languageListResult = await _languageDetector.ListLanguagesAsync(
                    Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName,
                    ex => _messageHub.Publish(Errors.CannotListLanguages.ToError(ex)),
                    CancellationToken.None)
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
                    directions[from] = new HashSet<string> { to };
                }
            }

            var availableLanguages = new AvailableLanguagesInfo { Directions = directions, Languages = languageListResult.Languages };
            _localSettingsRepository.AvailableLanguages = availableLanguages;
            return availableLanguages;
        }
    }
}
