using System;
using System.Linq.Expressions;
using JetBrains.Annotations;
using Remembrance.DAL.Contracts.Model;

namespace Remembrance.DAL.Contracts
{
    public interface IRepository<T>
        where T : Entity
    {
        void Delete([NotNull] T entity);
        void Delete(int id);

        [NotNull]
        T[] Get([NotNull] Expression<Func<T, bool>> predicate);

        [NotNull]
        T[] GetAll();

        [NotNull]
        T GetById(int id);

        int Save([NotNull] T entity);
    }
}