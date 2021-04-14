using System.Threading;
using System.Threading.Tasks;

namespace Mémoire.Contracts
{
    public interface ITextToSpeechPlayerWrapper
    {
        Task PlayTtsAsync(string text, string language, CancellationToken cancellationToken);
    }
}
