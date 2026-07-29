namespace Techer.Common.Domain.DataSource
{
    public class DataSourceResponse<T> where T : class
    {
        public IEnumerable<T> Data { get; set; }
        public int Total { get; set; }
    }
}
