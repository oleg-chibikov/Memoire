using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Remembrance.Contracts.Sync;
using Remembrance.Resources;

namespace Remembrance.Core.Sync
{
    [UsedImplicitly]
    internal sealed class SharedRepositoryPathsProvider : ISharedRepositoryPathsProvider
    {
        public string BaseDirectoryPath { get; } = Directory.GetParent(Paths.SharedDataPath).FullName;

        public ICollection<string> GetSharedRepositoriesPaths()
        {
            return Directory.GetDirectories(BaseDirectoryPath).Where(directoryPath => directoryPath != Paths.SharedDataPath).SelectMany(Directory.GetFiles).ToArray();
        }
    }
}