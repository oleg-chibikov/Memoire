using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Remembrance.Contracts.Translate;
using Scar.Common.WPF.Controls.AutoCompleteTextBox.Provider;

namespace Remembrance.View.Various
{
    [UsedImplicitly]
    internal sealed class SuggestionProvider : IAutoCompleteDataProvider
    {
        [NotNull]
        private readonly IPredictor _predictor;

        public SuggestionProvider([NotNull] IPredictor predictor)
        {
            _predictor = predictor ?? throw new ArgumentNullException(nameof(predictor));
        }

        [ItemCanBeNull]
        [NotNull]
        public async Task<IEnumerable<object>> GetItemsAsync([NotNull] string textPattern, CancellationToken cancellationToken)
        {
            if (textPattern == null)
            {
                throw new ArgumentNullException(nameof(textPattern));
            }

            var variants = await _predictor.PredictAsync(textPattern, 5, cancellationToken).ConfigureAwait(false);
            return variants?.Position < 0 ? variants.PredictionVariants : null;
        }
    }
}