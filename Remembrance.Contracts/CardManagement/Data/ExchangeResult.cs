using JetBrains.Annotations;

namespace Remembrance.Contracts.CardManagement.Data
{
    public class ExchangeResult
    {
        public ExchangeResult(bool success, [CanBeNull] string[] errors, int count)
        {
            Success = success;
            Errors = errors;
            Count = count;
        }

        public bool Success { get; }

        [CanBeNull]
        public string[] Errors { get; }

        public int Count { get; }
    }
}