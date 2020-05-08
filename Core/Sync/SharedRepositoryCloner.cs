using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Remembrance.Contracts;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.DAL.SharedBetweenMachines;
using Remembrance.Contracts.Sync;
using Scar.Common;

namespace Remembrance.Core.Sync
{
    sealed class SharedRepositoryCloner : ISharedRepositoryCloner, IDisposable
    {
        readonly IDictionary<ISharedRepository, IRateLimiter> _cloneableRepositoriesWithRateLimiters;
        readonly ILocalSettingsRepository _localSettingsRepository;
        readonly ILogger _logger;
        readonly IRemembrancePathsProvider _remembrancePathsProvider;

        public SharedRepositoryCloner(
            IReadOnlyCollection<ISharedRepository> cloneableRepositories,
            Func<IRateLimiter> rateLimiterFactory,
            ILocalSettingsRepository localSettingsRepository,
            ILogger<SharedRepositoryCloner> logger,
            IRemembrancePathsProvider remembrancePathsProvider)
        {
            _remembrancePathsProvider = remembrancePathsProvider ?? throw new ArgumentNullException(nameof(remembrancePathsProvider));
            _localSettingsRepository = localSettingsRepository ?? throw new ArgumentNullException(nameof(localSettingsRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ = cloneableRepositories ?? throw new ArgumentNullException(nameof(cloneableRepositories));
            _ = rateLimiterFactory ?? throw new ArgumentNullException(nameof(rateLimiterFactory));
            _cloneableRepositoriesWithRateLimiters = cloneableRepositories.ToDictionary(cloneableRepository => cloneableRepository, cloneableRepository => rateLimiterFactory());

            foreach (var repository in cloneableRepositories)
            {
                repository.Changed += Repository_ChangedAsync;
            }
        }

        public void Dispose()
        {
            foreach (var repository in _cloneableRepositoriesWithRateLimiters.Keys)
            {
                repository.Changed -= Repository_ChangedAsync;
            }
        }

        async void Repository_ChangedAsync(object sender, EventArgs e)
        {
            var repository = (ISharedRepository)sender;
            var rateLimiter = _cloneableRepositoriesWithRateLimiters[repository];
            await rateLimiter.ThrottleAsync(
                TimeSpan.FromSeconds(5),
                () =>
                {
                    var syncBus = _localSettingsRepository.SyncEngine;
                    if (syncBus == SyncEngine.NoSync)
                    {
                        return;
                    }

                    var fileName = repository.DbFileName + repository.DbFileExtension;
                    var oldFilePath = Path.Combine(repository.DbDirectoryPath, fileName);
                    var newDirectoryPath = _remembrancePathsProvider.GetSharedPath(syncBus);
                    var newFilePath = Path.Combine(newDirectoryPath, fileName);

                    try
                    {
                        if (!File.Exists(oldFilePath))
                        {
                            throw new InvalidOperationException($"{oldFilePath} does not exist");
                        }

                        if (!Directory.Exists(newDirectoryPath))
                        {
                            Directory.CreateDirectory(newDirectoryPath);
                        }

                        if (File.Exists(newFilePath))
                        {
                            File.Delete(newFilePath);
                        }

                        File.Copy(oldFilePath, newFilePath);
                        _logger.LogInformation("Cloned repository {0} to {1}", oldFilePath, newFilePath);
                    }
                    catch (IOException ex)
                    {
                        _logger.LogWarning(ex, "Cannot clone repository {0} to {1}. Retrying...", oldFilePath, newFilePath);
                        Repository_ChangedAsync(sender, e);
                    }
                }).ConfigureAwait(true);
        }
    }
}
