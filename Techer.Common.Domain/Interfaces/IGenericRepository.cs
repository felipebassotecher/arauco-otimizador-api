using System.Linq.Expressions;

namespace Techer.Common.Domain.Interfaces;

public interface IGenericRepository<T> where T : class
{
    void Add(T entity);
    void AddRange(IEnumerable<T> entities);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
    IQueryable<T> AsQueryable();
    void Attach(T entity);
    T First(Expression<Func<T, bool>> where);
    T FirstOrDefault(Expression<Func<T, bool>> where);
    Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> where);
    void LoadCollection<TProperty>(T entity, Expression<Func<T, IEnumerable<TProperty>>> propertyExpression) where TProperty : class;
    void LoadReference<TProperty>(T entity, Expression<Func<T, TProperty>> propertyExpression) where TProperty : class;
    Task Reload(T entity);
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);
    IQueryable<T> Where(Expression<Func<T, bool>> where);
}
