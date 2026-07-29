using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Techer.Common.Domain.DataSource
{
    public class DynamoDbDataSourceRequest<T>
    {
        public string AfterToken { get; set; }
        public string BeforeToken { get; set; }
        public int Take { get; set; }
        public T Filters { get; set; }
    }
}
