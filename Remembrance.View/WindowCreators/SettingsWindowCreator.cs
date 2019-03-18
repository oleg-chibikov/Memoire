using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Remembrance.Contracts.View.Settings;
using Scar.Common.View.WindowFactory;

namespace Remembrance.View.WindowCreators
{
    [UsedImplicitly]
    internal sealed class SettingsWindowCreator : IWindowCreator<ISettingsWindow>
    {
        [NotNull]
        private readonly IWindowFactory<IDictionaryWindow> _dictionaryWindowFactory;

        [NotNull]
        private readonly Func<IDictionaryWindow, ISettingsWindow> _settingsWindowFactory;

        public SettingsWindowCreator([NotNull] IWindowFactory<IDictionaryWindow> dictionaryWindowFactory, [NotNull] Func<IDictionaryWindow, ISettingsWindow> settingsWindowFactory)
        {
            _dictionaryWindowFactory = dictionaryWindowFactory ?? throw new ArgumentNullException(nameof(dictionaryWindowFactory));
            _settingsWindowFactory = settingsWindowFactory ?? throw new ArgumentNullException(nameof(settingsWindowFactory));
        }

        public async Task<ISettingsWindow> CreateWindowAsync(CancellationToken cancellationToken)
        {
            var dictionaryWindow = await _dictionaryWindowFactory.GetWindowIfExistsAsync(cancellationToken).ConfigureAwait(false);
            return _settingsWindowFactory(dictionaryWindow);
        }
    }
}