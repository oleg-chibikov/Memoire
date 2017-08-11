using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Remembrance.Contracts.Translate
{
    public interface ITextToSpeechPlayer
    {
        //TODO: cancellation
        [NotNull]
        Task<bool> PlayTtsAsync([NotNull] string text, [NotNull] string lang);
    }
}