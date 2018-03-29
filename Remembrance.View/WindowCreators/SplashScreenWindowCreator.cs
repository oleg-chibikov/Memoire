using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Remembrance.Contracts.View.Settings;
using Scar.Common.WPF.View;

namespace Remembrance.View.WindowCreators
{
    [UsedImplicitly]
    internal sealed class SplashScreenWindowCreator : IWindowCreator<ISplashScreenWindow>
    {
        [NotNull]
        private readonly Func<ISplashScreenWindow> _splashScreenWindowFactory;

        public SplashScreenWindowCreator([NotNull] Func<ISplashScreenWindow> splashScreenWindowFactory)
        {
            _splashScreenWindowFactory = splashScreenWindowFactory ?? throw new ArgumentNullException(nameof(splashScreenWindowFactory));
        }

        [NotNull]
        public Task<ISplashScreenWindow> CreateWindowAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_splashScreenWindowFactory());
        }
    }
}