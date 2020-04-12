using System;
using System.Threading;
using System.Threading.Tasks;
using Remembrance.Contracts.View.Settings;
using Scar.Common.View.WindowFactory;

namespace Remembrance.View.WindowCreators
{
    internal sealed class SettingsWindowCreator : IWindowCreator<ISettingsWindow>
    {
        private readonly IWindowFactory<IDictionaryWindow> _dictionaryWindowFactory;
        private readonly Func<IDictionaryWindow?, ISettingsWindow> _settingsWindowFactory;

        public SettingsWindowCreator(IWindowFactory<IDictionaryWindow> dictionaryWindowFactory, Func<IDictionaryWindow?, ISettingsWindow> settingsWindowFactory)
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
