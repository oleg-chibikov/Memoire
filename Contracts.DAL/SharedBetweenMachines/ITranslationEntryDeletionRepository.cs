using System;
using Mémoire.Contracts.DAL.Model;
using Scar.Common.DAL.Contracts;

namespace Mémoire.Contracts.DAL.SharedBetweenMachines
{
    public interface ITranslationEntryDeletionRepository : IRepository<TranslationEntryDeletion, TranslationEntryKey>, ISharedRepository, IDisposable
    {
    }
}
