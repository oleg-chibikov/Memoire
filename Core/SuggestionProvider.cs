using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Easy.MessageHub;
using Mémoire.Resources;
using Scar.Common.Autocomplete.Contracts;
using Scar.Common.Messages;
using Scar.Services.Contracts;

namespace Mémoire.Core
{
    public sealed class SuggestionProvider : IAutoCompleteDataProvider
    {
        readonly IPredictor _predictor;
        readonly ILanguageDetector _languageDetector;
        readonly IMessageHub _messageHub;

        public SuggestionProvider(IPredictor predictor, ILanguageDetector languageDetector, IMessageHub messageHub)
        {
            _predictor = predictor ?? throw new ArgumentNullException(nameof(predictor));
            _languageDetector = languageDetector ?? throw new ArgumentNullException(nameof(languageDetector));
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
        }

        public async Task<IEnumerable<object>> GetItemsAsync(string textPattern, CancellationToken cancellationToken)
        {
            _ = textPattern ?? throw new ArgumentNullException(nameof(textPattern));
            var language = await _languageDetector.DetectLanguageAsync(textPattern, ex => _messageHub.Publish(Errors.CannotDetectLanguage.ToError(ex)), cancellationToken).ConfigureAwait(false);
            var variants = await _predictor.PredictAsync(textPattern, 5, language.Code, ex => _messageHub.Publish(Errors.CannotPredict.ToError(ex)), cancellationToken).ConfigureAwait(false);
            return variants?.Position < 0 ? variants.PredictionVariants : Enumerable.Empty<object>();
        }
    }
}
