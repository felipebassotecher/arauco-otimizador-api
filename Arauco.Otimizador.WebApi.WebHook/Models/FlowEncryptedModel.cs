using Newtonsoft.Json;

namespace Arauco.Otimizador.WebApi.Flow.Models;

public class FlowEncryptedModel
{
    [JsonProperty("encrypted_flow_data")]
    public string EncryptedFlowData { get; set; }
    [JsonProperty("encrypted_aes_key")]
    public string EncryptedAesKey { get; set; }
    [JsonProperty("initial_vector")]
    public string InitialVector { get; set; }
}
