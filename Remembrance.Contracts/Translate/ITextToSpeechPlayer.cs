using System.Threading;
using System.Threading.Tasks;

namespace Remembrance.Contracts.Translate
{
    public interface ITextToSpeechPlayer
    {
        Task<bool> PlayTtsAsync(string text, string lang, CancellationToken cancellationToken);
    }
}