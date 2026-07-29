using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using System.Text;
using Techer.Common.Domain.DataSource;

namespace Techer.Common.Extensions
{
    public static class FilterExtensions
    {
        public static async Task<IEnumerable<T>> ApplyFilters<T>(this IQueryable<T> query, DataSourceRequest dataSourceRequest, bool limitResults = true) where T : class
        {
            // Filter
            query = Filter(query, dataSourceRequest.Filters);

            // Sorting
            query = Sort(query, dataSourceRequest.Sorts);

            // Page
            int take = dataSourceRequest.Take;
            if (take > 50 && limitResults)
                take = 50;

            var res = await query
                .Skip(dataSourceRequest.Skip)
                .Take(take)
                .ToListAsync();

            return res;
        }

        private static IQueryable<T> Sort<T>(IQueryable<T> query, SortDescriptor[] sorts)
        {
            if (sorts != null && sorts.Length > 0)
            {
                // Create ordering expression e.g. Field1 asc, Field2 desc
                var ordering = string.Join(",", sorts.Select(s => s.ToExpression()));

                // Use the OrderBy method of Dynamic Linq to sort the data
                query = query.OrderBy(ordering);
            }

            return query;
        }

        private static IQueryable<T> Filter<T>(IQueryable<T> query, FilterDescriptor[] filters)
        {
            if (filters != null && filters.Length > 0)
            {
                var predicate = new StringBuilder();
                var values = new List<object>();

                int idx = 0;
                foreach (var filter in filters)
                {
                    // Create a predicate expression e.g. Field1 = @0 And Field2 > @1
                    var res = filter.ToExpression(idx);

                    if (idx > 0)
                        predicate.Append(" And ");

                    predicate.Append(res.Item1);
                    values.AddRange(res.Item2);

                    idx += res.Item2.Length;
                }

                // Use the Where method of Dynamic Linq to filter the data
                //query = query.Where(predicate.ToString(), values.ToArray());

                query = query.Where(new ParsingConfig()
                {
                    UseParameterizedNamesInDynamicQuery = true
                }, predicate.ToString(), values.ToArray());
            }

            return query;
        }
    }
}
