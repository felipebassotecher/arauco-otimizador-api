namespace Techer.Common.Domain.DataSource
{
    public class DdbDatasourceResult<T>
    {
        /// <summary>
        /// Represents a single page of processed data.
        /// </summary>
        public IList<T> Data { get; set; }

        /// <summary>
        /// The pagination token.
        /// </summary>
        public DdbDataSourceCursor Cursor { get; set; }
    }

    public class DdbDataSourceCursor
    {
        public string AfterToken { get; set; }
        public string BeforeToken { get; set; }
    }
}
