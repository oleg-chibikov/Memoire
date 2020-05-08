using System.Collections.Generic;
using System.Windows.Input;
using Scar.Services.Contracts.Data.ExtendedTranslation;

namespace Mémoire.ViewModel
{
    public interface IWithExtendedExamples
    {
        IReadOnlyCollection<ExtendedExample>? ExtendedExamples { get; }

        bool IsExpanded { get; set; }

        bool HasExtendedExamples { get; set; }

        ICommand OpenImdbLinkCommand { get; set; }
    }
}
