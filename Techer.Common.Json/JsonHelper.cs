using Newtonsoft.Json;

namespace Techer.Common.Json;

public static class JsonHelper
{
    public static JsonSerializerSettings Settings = new JsonSerializerSettings
    {
        ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Ignore,
        DateTimeZoneHandling = DateTimeZoneHandling.Utc
    };

    public static string Serialize(object data, JsonSerializerSettings settings = null)
    {
        return JsonConvert.SerializeObject(data, settings ?? Settings);
    }

    public static T Deserialize<T>(string json, JsonSerializerSettings settings = null) where T : class
    {
        return JsonConvert.DeserializeObject<T>(json, settings ?? Settings);
    }

    public static JsonSerializerSettings GetJsonSerializerSettingsNoDictionaryKeys()
    {
        var resolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
        {
            NamingStrategy = new Newtonsoft.Json.Serialization.CamelCaseNamingStrategy
            {
                ProcessDictionaryKeys = false,
                OverrideSpecifiedNames = true
            }
        };

        return new JsonSerializerSettings
        {
            ContractResolver = resolver
        };
    }
}
