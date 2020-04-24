using System.Collections.Generic;

namespace Remembrance.Contracts.Classification.Data.UClassify
{
    public class UClassifyTopicsRequest
    {
        public UClassifyTopicsRequest(params string[] texts)
        {
            Texts = texts;
        }

        public IReadOnlyCollection<string> Texts { get; }
    }
}
