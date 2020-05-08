using System.Collections.Generic;
using Scar.Services.Contracts.Data.ExtendedTranslation;

namespace Mémoire.ViewModel
{
    public interface IWithExtendedExamples
    {
        IReadOnlyCollection<ExtendedExample>? OrphanExtendedExamples { get; }
    }
}
