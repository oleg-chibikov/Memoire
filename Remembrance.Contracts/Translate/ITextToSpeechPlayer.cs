using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Remembrance.Contracts.Translate
{
    public interface ITextToSpeechPlayer
    {
        [NotNull]
        Task<bool> PlayTtsAsync([NotNull] string text, [NotNull] string lang, CancellationToken token);
    }
}