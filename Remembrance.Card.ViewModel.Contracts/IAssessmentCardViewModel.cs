using System.Windows.Input;
using JetBrains.Annotations;
using Remembrance.Card.ViewModel.Contracts.Data;

namespace Remembrance.Card.ViewModel.Contracts
{
    public interface IAssessmentCardViewModel
    {
        [CanBeNull]
        bool? Accepted { get; }

        [NotNull]
        string CorrectAnswer { get; }

        [NotNull]
        string LanguagePair { get; }

        [NotNull]
        ICommand ProvideAnswerCommand { get; }

        [NotNull]
        WordViewModel Word { get; }
    }
}