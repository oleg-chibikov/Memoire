using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.Logging;
using Remembrance.Contracts;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Contracts.Sync;
using Scar.Common;

namespace Remembrance.Core.Sync
{
    internal sealed class SharedRepositoryCloner : ISharedRepositoryCloner, IDisposable
    {
        private readonly IDictionary<ISharedRepository, IRateLimiter> _cloneableRepositoriesWithRateLimiters;
        private readonly ILocalSettingsRepository _localSettingsRepository;
        private readonly ILog _logger;
        private readonly IRemembrancePathsProvider _remembrancePathsProvider;

        public SharedRepositoryCloner(
            IReadOnlyCollection<ISharedRepository> cloneableRepositories,
            Func<IRateLimiter> rateLimiterFactory,
            ILocalSettingsRepository localSettingsRepository,
            ILog logger,
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

        private void Repository_Changed(object sender, EventArgs e)
        {
            var repository = (ISharedRepository)sender;
            var rateLimiter = _cloneableRepositoriesWithRateLimiters[repository];
            rateLimiter.Throttle(
                TimeSpan.FromSeconds(5),
                () =>
                {
                    var syncBus = _localSettingsRepository.SyncBus;
                    if (syncBus == SyncBus.NoSync)
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
                        _logger.InfoFormat("Cloned repository {0} to {1}", oldFilePath, newFilePath);
                    }
                    catch (IOException ex)
                    {
                        _logger.WarnFormat("Cannot clone repository {0} to {1}. Retrying...", ex, oldFilePath, newFilePath);
                        Repository_Changed(sender, e);
                    }
                });
        }
    }
}
