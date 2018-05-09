using System;
using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Model;
using Scar.Common.DAL;

namespace Remembrance.Contracts.DAL.Shared
{
    public interface IWordImageSearchIndexRepository : IRepository<WordImageSearchIndex, WordKey>, ISharedRepository, IDisposable
    {
        void ClearForTranslationEntry([NotNull] TranslationEntryKey translationEntryKey);
    }
}