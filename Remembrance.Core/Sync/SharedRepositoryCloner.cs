using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Contracts.Sync;
using Remembrance.Resources;
using Scar.Common;

namespace Remembrance.Core.Sync
{
    [UsedImplicitly]
    internal sealed class SharedRepositoryCloner : ISharedRepositoryCloner, IDisposable
    {
        private readonly IDictionary<ISharedRepository, IRateLimiter> _cloneableRepositoriesWithRateLimiters;

        public SharedRepositoryCloner([NotNull] IReadOnlyCollection<ISharedRepository> cloneableRepositories, [NotNull] Func<IRateLimiter> rateLimiterFactory)
        {
            _ = cloneableRepositories ?? throw new ArgumentNullException(nameof(cloneableRepositories));
            _ = rateLimiterFactory ?? throw new ArgumentNullException(nameof(rateLimiterFactory));
            _cloneableRepositoriesWithRateLimiters = cloneableRepositories.ToDictionary(cloneableRepository => cloneableRepository, cloneableRepository => rateLimiterFactory());

            foreach (var repository in cloneableRepositories)
            {
                repository.Changed += Repository_Changed;
            }
        }

        public void Dispose()
        {
            foreach (var repository in _cloneableRepositoriesWithRateLimiters.Keys)
            {
                repository.Changed -= Repository_Changed;
            }
        }

        private void Repository_Changed([NotNull] object sender, EventArgs e)
        {
            var repository = (ISharedRepository)sender;
            var rateLimiter = _cloneableRepositoriesWithRateLimiters[repository];
            rateLimiter.Throttle(
                TimeSpan.FromMinutes(1),
                () =>
                {
                    var fileName = repository.DbFileName + repository.DbFileExtension;
                    var oldFilePath = Path.Combine(repository.DbDirectoryPath, fileName);
                    var newDirectoryPath = RemembrancePaths.SharedDataPath;
                    var newFilePath = Path.Combine(newDirectoryPath, fileName);
                    if (File.Exists(newFilePath))
                    {
                        File.Delete(newFilePath);
                    }

                    File.Copy(oldFilePath, newFilePath);
                });
        }
    }
}