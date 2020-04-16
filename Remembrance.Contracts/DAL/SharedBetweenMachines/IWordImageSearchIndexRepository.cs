using System;
using Remembrance.Contracts.DAL.Model;
using Scar.Common.DAL;

namespace Remembrance.Contracts.DAL.SharedBetweenMachines
{
    public interface IWordImageSearchIndexRepository : IRepository<WordImageSearchIndex, WordKey>, ISharedRepository, IDisposable
    {
        void ClearForTranslationEntry(TranslationEntryKey translationEntryKey);
    }
}
