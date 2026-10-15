using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Data.Extensions
{
    public static class QueryableExtensions
    {
        public static IQueryable<T> WhereIf<T>(this IQueryable<T> query, bool condition, Expression<Func<T, bool>> predicate)
        {
            return condition ? query.Where(predicate) : query;
        }

        public static IQueryable<T> OrderByIf<T, TKey>(this IQueryable<T> query, bool condition, Expression<Func<T, TKey>> keySelector)
        {
            return condition ? query.OrderBy(keySelector) : query;
        }

        public static IQueryable<T> OrderByDescendingIf<T, TKey>(this IQueryable<T> query, bool condition, Expression<Func<T, TKey>> keySelector)
        {
            return condition ? query.OrderByDescending(keySelector) : query;
        }

        public static IQueryable<T> Paginate<T>(this IQueryable<T> query, int pageNumber, int pageSize)
        {
            return query.Skip((pageNumber - 1) * pageSize).Take(pageSize);
        }
    }
}
