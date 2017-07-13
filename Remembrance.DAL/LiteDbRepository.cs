using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using Common.Logging;
using JetBrains.Annotations;
using LiteDB;
using Remembrance.DAL.Contracts;
using Remembrance.DAL.Contracts.Model;
using Remembrance.Resources;

namespace Remembrance.DAL
{
    public abstract class LiteDbRepository<T> : IRepository<T>, IDisposable
        where T : Entity, new()
    {
        [NotNull]
        protected readonly LiteDatabase Db;

        [NotNull]
        private readonly ILog logger;

        protected LiteDbRepository([NotNull] ILog logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            // ReSharper disable once VirtualMemberCallInConstructor
            logger.Debug($"Initializing database for {TableName}...");
            if (!Directory.Exists(Paths.SettingsPath))
                Directory.CreateDirectory(Paths.SettingsPath);
            // ReSharper disable once VirtualMemberCallInConstructor
            Db = new LiteDatabase(Path.Combine(Paths.SettingsPath, $"{DbName}.db"));
            Db.Shrink();
            // ReSharper disable once VirtualMemberCallInConstructor
            logger.Debug($"Database for {TableName} is initialized");
        }

        [NotNull]
        protected virtual string DbName { get; } = "Data";

        [NotNull]
        protected abstract string TableName { get; }

        public void Dispose()
        {
            Db.Dispose();
            logger.Debug("Repository is disposed");
        }

        public T[] GetAll()
        {
            return Db.GetCollection<T>(TableName).FindAll().ToArray();
        }

        public T[] Get(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return Db.GetCollection<T>(TableName).Find(predicate).ToArray();
        }

        public T GetById(int id)
        {
            var record = Db.GetCollection<T>(TableName).FindById(id);
            if (record == null)
                throw new InvalidOperationException($"No record for {id}");

            return record;
        }

        public int Save(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            //TODO:lock?
            logger.Debug($"Saving {entity} to database...");
            var dbEntities = Db.GetCollection<T>(TableName);
            var existing = dbEntities.FindById(entity.Id);
            if (existing == null)
            {
                var id = dbEntities.Insert(entity);
                logger.Debug($"{entity} is inserted");
                return id;
            }

            var result = dbEntities.Update(entity.Id, entity);
            logger.Debug($"{entity} is{(result ? null : " not")} updated");
            return entity.Id;
        }

        public void Delete(int id)
        {
            logger.Debug($"Deleting entity with id {id} from database...");
            var dbEntities = Db.GetCollection<T>(TableName);
            var result = dbEntities.Delete(id);
            if (result)
                logger.Debug($"Entity with id {id} is deleted");
            else
                logger.Warn($"Entity with id {id} is not deleted");
        }

        public void Delete(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            Delete(entity.Id);
        }
    }
}