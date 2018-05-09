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
        private readonly IDictionary<ISharedRepository, IRateLimiter> _clonableRepositoriesWithRateLimiters;

        public SharedRepositoryCloner([NotNull] ICollection<ISharedRepository> clonableRepositories, [NotNull] Func<IRateLimiter> rateLimiterFactory)
        {
            if (clonableRepositories == null)
            {
                throw new ArgumentNullException(nameof(clonableRepositories));
            }

            if (rateLimiterFactory == null)
            {
                throw new ArgumentNullException(nameof(rateLimiterFactory));
            }

            _clonableRepositoriesWithRateLimiters = clonableRepositories.ToDictionary(clonableRepository => clonableRepository, clonableRepository => rateLimiterFactory());

            foreach (var repository in clonableRepositories)
            {
                repository.Changed += Repository_Changed;
            }
        }

        public void Dispose()
        {
            foreach (var repository in _clonableRepositoriesWithRateLimiters.Keys)
            {
                repository.Changed -= Repository_Changed;
            }
        }

        private void Repository_Changed([NotNull] object sender, EventArgs e)
        {
            var repository = (ISharedRepository)sender;
            var rateLimiter = _clonableRepositoriesWithRateLimiters[repository];
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