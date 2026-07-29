using System.Collections.Generic;

namespace Techer.Aws.Dynamo
{
    public class DynamoDbDataSourceResult<T>
    {
        /// <summary>
        /// Represents a single page of processed data.
        /// </summary>
        public IList<T> Data { get; set; }

        /// <summary>
        /// The pagination token.
        /// </summary>
        public DynamoDbDataSourceCursor Cursor { get; set; }

    }
}
