using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Remembrance.Translate.Contracts.Interfaces
{
    public interface ITextToSpeechPlayer
    {
        [NotNull]
        Task<bool> PlayTtsAsync([NotNull] string text, [NotNull] string lang);
    }
}