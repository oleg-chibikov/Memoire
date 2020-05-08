using System;
using Remembrance.Contracts.DAL.Model;
using Scar.Common.DAL;

namespace Remembrance.Contracts.DAL.SharedBetweenMachines
{
    public interface ITranslationEntryDeletionRepository : IRepository<TranslationEntryDeletion, TranslationEntryKey>, ISharedRepository, IDisposable
    {
    }
}
