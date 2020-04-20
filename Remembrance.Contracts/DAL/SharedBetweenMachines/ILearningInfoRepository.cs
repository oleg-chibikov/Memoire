using System;
using System.Collections.Generic;
using Remembrance.Contracts.DAL.Model;
using Scar.Common.DAL;

namespace Remembrance.Contracts.DAL.SharedBetweenMachines
{
    public interface ILearningInfoRepository : IRepository<LearningInfo, TranslationEntryKey>, ISharedRepository, IDisposable
    {
        IEnumerable<LearningInfo> GetMostSuitable(int count);

        LearningInfo GetOrInsert(TranslationEntryKey translationEntryKey);
    }
}
