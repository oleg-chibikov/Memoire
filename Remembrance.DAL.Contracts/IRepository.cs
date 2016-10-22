using System;
using System.Linq.Expressions;
using JetBrains.Annotations;
using Remembrance.DAL.Contracts.Model;

namespace Remembrance.DAL.Contracts
{
    public interface IRepository<T> where T : Entity
    {
        [NotNull]
        T[] GetAll();

        [NotNull]
        T[] Get([NotNull] Expression<Func<T, bool>> predicate);

        [NotNull]
        T GetById(int id);

        int Save([NotNull] T entity);
        void Delete([NotNull] T entity);
        void Delete(int id);
    }
}