using System;
using Remembrance.Contracts.DAL.Model;
using Scar.Common.DAL;

namespace Remembrance.Contracts.DAL.Shared
{
    public interface ILearningInfoRepository : IRepository<LearningInfo, TranslationEntryKey>, ISharedRepository, IDisposable
    {
        LearningInfo? GetMostSuitable();

        LearningInfo GetOrInsert(TranslationEntryKey translationEntryKey);
    }
}