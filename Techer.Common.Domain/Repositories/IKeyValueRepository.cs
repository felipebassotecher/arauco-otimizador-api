namespace Techer.Common.Domain.Repositories;

public interface IKeyValueRepository
{
    Task<(T? Data, DateTime? timeToLive)> GetAsync<T>(string key, bool consistentRead = true) where T : class;
    Task SaveAsync<T>(string key, T data, DateTime? timeToLive = null, bool noSerializeDictionaryKeys = false) where T : class;
    Task DeleteAsync(string key);
}
