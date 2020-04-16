using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Remembrance.Contracts.Translate;
using Scar.Common.WPF.Controls.AutoCompleteTextBox.Provider;

namespace Remembrance.View.Various
{
    sealed class SuggestionProvider : IAutoCompleteDataProvider
    {
        readonly IPredictor _predictor;

        public SuggestionProvider(IPredictor predictor)
        {
            _predictor = predictor ?? throw new ArgumentNullException(nameof(predictor));
        }

        public async Task<IEnumerable<object>> GetItemsAsync(string textPattern, CancellationToken cancellationToken)
        {
            _ = textPattern ?? throw new ArgumentNullException(nameof(textPattern));
            var variants = await _predictor.PredictAsync(textPattern, 5, cancellationToken).ConfigureAwait(false);
            return variants?.Position < 0 ? variants.PredictionVariants : Enumerable.Empty<object>();
        }
    }
}
