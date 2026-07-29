using Amazon.DynamoDBv2.DataModel;

namespace Arauco.Otimizador.Data.Dynamo.Docs;

[DynamoDBTable("KeyValue")]
public class KeyValueDoc
{
    [DynamoDBHashKey]
    public string Key { get; set; }

    [DynamoDBProperty]
    public string Value { get; set; }

    [DynamoDBProperty(AttributeName = "TTL")]
    public int? TimeToLive { get; set; }
}
