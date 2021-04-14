using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Mémoire.Contracts.Processing.Data;
using PropertyChanged;
using Scar.Common.IO;
using Scar.Common.MVVM.Commands;
using Scar.Common.MVVM.ViewModel;
using Scar.Services.Contracts.Data.ExtendedTranslation;

namespace Mémoire.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class ExtendedExamplesViewModel : BaseViewModel, IWithExtendedExamples
    {
        readonly Action _loadExtendedExamples;
        bool _isExpanded;

        public ExtendedExamplesViewModel(
            TranslationInfo translationInfo,
            Func<TranslationInfo, IReadOnlyCollection<ExtendedPartOfSpeechTranslation>?> getExamples,
            ICommandManager commandManager) : base(commandManager)
        {
            _ = getExamples ?? throw new ArgumentNullException(nameof(getExamples));
            _ = translationInfo ?? throw new ArgumentNullException(nameof(translationInfo));

            var examples = getExamples(translationInfo);
            HasExtendedExamples = examples?.Count > 0;

            _loadExtendedExamples = () =>
            {
                ExtendedExamples = (examples ?? throw new InvalidOperationException("examples are null")).SelectMany(x => x.ExtendedExamples).ToArray();
            };

            OpenImdbLinkCommand = AddCommand<string>(OpenImdbLink);
        }

        public IReadOnlyCollection<ExtendedExample>? ExtendedExamples { get; private set; }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                if (value && ExtendedExamples == null)
                {
                    _loadExtendedExamples();
                }
            }
        }

        public bool HasExtendedExamples { get; set; }

        public ICommand OpenImdbLinkCommand { get; set; }

        static void OpenImdbLink(string url)
        {
            _ = url ?? throw new ArgumentNullException(nameof(url));

            url.OpenPathWithDefaultAction();
        }
    }
}
