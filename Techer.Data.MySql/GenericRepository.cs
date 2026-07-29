using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Techer.Common.Domain.Interfaces;

namespace Techer.Data.MySql
{
    public class GenericRepository<C, T> : IGenericRepository<T> where C : DbContext where T : class
    {
        protected C dbContext;
        protected DbSet<T> dbSet;

        public GenericRepository(C dbContext)
        {
            this.dbContext = dbContext;
            dbSet = dbContext.Set<T>();
        }

        public IQueryable<T> AsQueryable()
        {
            return dbSet;
        }

        public IQueryable<T> Where(Expression<Func<T, bool>> where)
        {
            return dbSet.Where(where);
        }

        public T First(Expression<Func<T, bool>> where)
        {
            return dbSet.First(where);
        }

        public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        {
            return await dbSet.AnyAsync(predicate);
        }

        public T FirstOrDefault(Expression<Func<T, bool>> where)
        {
            return dbSet.FirstOrDefault(where);
        }

        public Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> where)
        {
            return dbSet.FirstOrDefaultAsync(where);
        }

        public void Remove(T entity)
        {
            dbSet.Remove(entity);
        }

        public void RemoveRange(IEnumerable<T> entities)
        {
            dbSet.RemoveRange(entities);
        }

        public void Add(T entity)
        {
            dbSet.Add(entity);
        }

        public void AddRange(IEnumerable<T> entities)
        {
            dbSet.AddRange(entities);
        }

        public void Attach(T entity)
        {
            dbSet.Attach(entity);
        }

        public async Task Reload(T entity)
        {
            await dbContext.Entry(entity).ReloadAsync();
        }

        public void LoadCollection<TProperty>(T entity, Expression<Func<T, IEnumerable<TProperty>>> propertyExpression) where TProperty : class
        {
            dbContext
                .Entry(entity)
                .Collection(propertyExpression)
                .Load();

        }

        public void LoadReference<TProperty>(T entity, Expression<Func<T, TProperty>> propertyExpression) where TProperty : class
        {
            dbContext
                .Entry(entity)
                .Reference(propertyExpression)
                .Load();

        }
    }
}
