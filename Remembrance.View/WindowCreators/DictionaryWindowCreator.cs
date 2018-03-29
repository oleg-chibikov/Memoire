using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Remembrance.Contracts.View.Settings;
using Scar.Common.WPF.View;

namespace Remembrance.View.WindowCreators
{
    [UsedImplicitly]
    internal sealed class DictionaryWindowCreator : IWindowCreator<IDictionaryWindow>
    {
        [NotNull]
        private readonly Func<IDictionaryWindow> _dictionaryWindowFactory;

        public DictionaryWindowCreator([NotNull] Func<IDictionaryWindow> dictionaryWindowFactory)
        {
            _dictionaryWindowFactory = dictionaryWindowFactory ?? throw new ArgumentNullException(nameof(dictionaryWindowFactory));
        }

        [NotNull]
        public Task<IDictionaryWindow> CreateWindowAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_dictionaryWindowFactory());
        }
    }
}