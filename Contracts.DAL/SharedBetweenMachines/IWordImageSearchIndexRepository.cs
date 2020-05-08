using System;
using Mémoire.Contracts.DAL.Model;
using Scar.Common.DAL.Contracts;

namespace Mémoire.Contracts.DAL.SharedBetweenMachines
{
    public interface IWordImageSearchIndexRepository : IRepository<WordImageSearchIndex, WordKey>, ISharedRepository, IDisposable
    {
        void ClearForTranslationEntry(TranslationEntryKey translationEntryKey);
    }
}
