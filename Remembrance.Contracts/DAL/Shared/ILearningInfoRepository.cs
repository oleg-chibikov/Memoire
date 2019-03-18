using System;
using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Model;
using Scar.Common.DAL;

namespace Remembrance.Contracts.DAL.Shared
{
    public interface ILearningInfoRepository : IRepository<LearningInfo, TranslationEntryKey>, ISharedRepository, IDisposable
    {
        [CanBeNull]
        LearningInfo? GetMostSuitable();

        [NotNull]
        LearningInfo GetOrInsert([NotNull] TranslationEntryKey translationEntryKey);
    }
}