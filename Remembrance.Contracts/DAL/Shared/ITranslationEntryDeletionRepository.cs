using System;
using Remembrance.Contracts.DAL.Model;
using Scar.Common.DAL;

namespace Remembrance.Contracts.DAL.Shared
{
    public interface ITranslationEntryDeletionRepository : IRepository<TranslationEntryDeletion, TranslationEntryKey>, ISharedRepository, IDisposable
    {
    }
}