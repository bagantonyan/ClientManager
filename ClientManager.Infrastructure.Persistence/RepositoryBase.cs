using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ClientManager.Infrastructure.Persistence
{
    public abstract class RepositoryBase<T> where T : class
    {
        protected RepositoryContext RepositoryContext;

        protected RepositoryBase(RepositoryContext repositoryContext)
            => RepositoryContext = repositoryContext;

        protected IQueryable<T> FindAll(bool trackChanges) =>
            trackChanges
                ? RepositoryContext.Set<T>()
                : RepositoryContext.Set<T>().AsNoTracking();

        protected IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression, bool trackChanges) =>
            trackChanges
                ? RepositoryContext.Set<T>().Where(expression)
                : RepositoryContext.Set<T>().Where(expression).AsNoTracking();

        protected void Create(T entity) => RepositoryContext.Set<T>().Add(entity);

        protected void Delete(T entity) => RepositoryContext.Set<T>().Remove(entity);
    }
}