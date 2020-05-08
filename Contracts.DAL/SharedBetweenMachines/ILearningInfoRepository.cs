using System;
using System.Collections.Generic;
using Mémoire.Contracts.DAL.Model;
using Scar.Common.DAL.Contracts;

namespace Mémoire.Contracts.DAL.SharedBetweenMachines
{
    public interface ILearningInfoRepository : IRepository<LearningInfo, TranslationEntryKey>, ISharedRepository, IDisposable
    {
        IEnumerable<LearningInfo> GetMostSuitable(int count);

        LearningInfo GetOrInsert(TranslationEntryKey translationEntryKey);
    }
}
