using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Remembrance.Contracts.View.Settings;
using Scar.Common.WPF.View;

namespace Remembrance.View.WindowCreators
{
    [UsedImplicitly]
    internal sealed class AddTranslationWindowCreator : IWindowCreator<IAddTranslationWindow>
    {
        [NotNull]
        private readonly Func<IAddTranslationWindow> _addTranslationWindowFactory;

        public AddTranslationWindowCreator([NotNull] Func<IAddTranslationWindow> addTranslationWindowFactory)
        {
            _addTranslationWindowFactory = addTranslationWindowFactory ?? throw new ArgumentNullException(nameof(addTranslationWindowFactory));
        }

        [NotNull]
        public Task<IAddTranslationWindow> CreateWindowAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_addTranslationWindowFactory());
        }
    }
}