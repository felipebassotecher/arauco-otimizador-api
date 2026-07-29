using Newtonsoft.Json;
using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Arauco.Otimizador.WebApi.Flow.Models;

public class DataExchangeRequest
{
    public string Version { get; set; }
    public string Action { get; set; }
    public string Screen { get; set; }
    public dynamic Data { get; set; }

    [JsonProperty("flow_token")]
    public string FlowToken { get; set; }
}
