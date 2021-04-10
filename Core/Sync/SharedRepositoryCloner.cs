using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mémoire.Contracts;
using Mémoire.Contracts.DAL.Local;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.DAL.SharedBetweenMachines;
using Mémoire.Contracts.Sync;
using Microsoft.Extensions.Logging;
using Scar.Common.RateLimiting;

namespace Mémoire.Core.Sync
{
    sealed class SharedRepositoryCloner : ISharedRepositoryCloner, IDisposable
    {
        readonly IDictionary<ISharedRepository, IRateLimiter> _cloneableRepositoriesWithRateLimiters;
        readonly ILocalSettingsRepository _localSettingsRepository;
        readonly ILogger _logger;
        readonly IPathsProvider _pathsProvider;

        public SharedRepositoryCloner(
            IReadOnlyCollection<ISharedRepository> cloneableRepositories,
            Func<IRateLimiter> rateLimiterFactory,
            ILocalSettingsRepository localSettingsRepository,
            ILogger<SharedRepositoryCloner> logger,
            IPathsProvider pathsProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            logger.LogTrace($"Initializing {GetType().Name}...");
            _pathsProvider = pathsProvider ?? throw new ArgumentNullException(nameof(pathsProvider));
            _localSettingsRepository = localSettingsRepository ?? throw new ArgumentNullException(nameof(localSettingsRepository));
            _ = cloneableRepositories ?? throw new ArgumentNullException(nameof(cloneableRepositories));
            _ = rateLimiterFactory ?? throw new ArgumentNullException(nameof(rateLimiterFactory));
            _cloneableRepositoriesWithRateLimiters = cloneableRepositories.ToDictionary(cloneableRepository => cloneableRepository, cloneableRepository => rateLimiterFactory());

            foreach (var repository in cloneableRepositories)
            {
                repository.Changed += Repository_ChangedAsync;
            }

            logger.LogDebug($"Initialized {GetType().Name}");
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
                        var newDirectoryPath = _pathsProvider.GetSharedPath(syncBus);
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
                    })
                .ConfigureAwait(true);
        }
    }
}
