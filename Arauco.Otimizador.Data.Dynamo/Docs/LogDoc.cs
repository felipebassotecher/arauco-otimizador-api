using Amazon.DynamoDBv2.DataModel;
using Arauco.Otimizador.Common.Domain.Models;
using Techer.Common.Extensions;

namespace Arauco.Otimizador.Data.Dynamo.Docs;

[DynamoDBTable("OtimizadorLog")]
public class LogDoc
{
    [DynamoDBHashKey]
    public string PK
    {
        get
        {
            return $"{TipoLogId}#{Agrupador}";
        }
        private set
        {
        }
    }

    [DynamoDBRangeKey]
    public string Sort
    {
        get
        {
            return $"{DataHora:s}";
        }
        private set
        {
        }
    }

    [DynamoDBProperty]
    public string Agrupador { get; set; }

    [DynamoDBProperty]
    public int TipoLogId { get; set; }

    [DynamoDBProperty]
    public string LogId { get; set; }

    [DynamoDBProperty]
    public DateTime DataHora { get; set; }

    [DynamoDBProperty]
    public string Descricao { get; set; }

    // TTL
    [DynamoDBProperty(AttributeName = "TTL")]
    public int TimeToLive { get; set; }

    public LogModel ToModel()
    {
        return new LogModel
        {
            LogId = LogId,
            Agrupador = Agrupador,
            DataHora = DataHora,
            TipoLogEnum = (Common.Domain.Enums.TipoLogEnum)TipoLogId,
            Descricao = Descricao
        };
    }

    public static LogDoc FromModel(LogModel m)
    {
        return new LogDoc
        {
            LogId = m.LogId,
            Agrupador = m.Agrupador,
            TipoLogId = (int)m.TipoLogEnum,
            DataHora = m.DataHora,
            Descricao = m.Descricao,
            TimeToLive = DateTime.UtcNow.AddMonths(3).ToEpoch()
        };
    }
}
