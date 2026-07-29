using Techer.Aws.Dynamo;
using Techer.Common.Domain.Repositories;
using Techer.Common.Extensions;
using Techer.Common.Json;

namespace Arauco.Otimizador.Data.Dynamo;

public class KeyValueRepository : GenericRepository<Docs.KeyValueDoc>, IKeyValueRepository
{
    public async Task DeleteAsync(string key)
    {
        await DeleteItemAsync(key);
    }

    public async Task<(T? Data, DateTime? timeToLive)> GetAsync<T>(string key, bool consistentRead = true) where T : class
    {
        var doc = await this.GetItemAsync(key, consistentRead: consistentRead);

        if (doc == null)
            return (null, null);

        var obj = JsonHelper.Deserialize<T>(doc.Value);

        return (obj, doc.TimeToLive?.ToDatetimeFromEpochSeconds());
    }

    public async Task SaveAsync<T>(string key, T data, DateTime? timeToLive = null, bool noSerializeDictionaryKeys = false) where T : class
    {
        string value;
        if (noSerializeDictionaryKeys)
        {
            value = JsonHelper.Serialize(data, JsonHelper.GetJsonSerializerSettingsNoDictionaryKeys());
        }
        else
        {
            value = JsonHelper.Serialize(data);
        }

        var doc = new Docs.KeyValueDoc
        {
            Key = key,
            Value = value,
            TimeToLive = timeToLive?.ToEpoch()
        };

        await UpdateItemAsync(doc);
    }
}
