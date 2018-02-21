using System.Collections.Generic;
using JetBrains.Annotations;

namespace Remembrance.Contracts.Sync
{
    public interface ISharedRepositoryPathsProvider
    {
        [NotNull]
        string BaseDirectoryPath { get; }

        [NotNull]
        ICollection<string> GetSharedRepositoriesPaths();
    }
}