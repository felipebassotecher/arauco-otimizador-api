using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Arauco.Otimizador.Function.Base;
using System.Text.Json;
using System.Text;
using System.Globalization;
using CsvHelper;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Arauco.Otimizador.Function.Florestal;

public class Function : BaseFunction
{
    private static readonly string SenderAddress = Environment.GetEnvironmentVariable("SENDER_EMAIL") ?? "Inteligência de Dados Florestal <inteligenciadedadosflorestal@techer.com.br>";
    private static readonly List<string> RecipientAddresses = (Environment.GetEnvironmentVariable("RECIPIENT_EMAILS") ?? "ext.luciano.motti@arauco.com").Split(',').ToList();

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        context.Logger.LogLine("Processing Florestal request...");

        try
        {
            var input = JsonSerializer.Deserialize<FlorestalInput>(request.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (input == null || string.IsNullOrEmpty(input.CsvData))
            {
                throw new ArgumentException("Request body inválido ou sem os dados CSV.");
            }
            
            var csvData = Encoding.UTF8.GetString(Convert.FromBase64String(input.CsvData));
            var records = ParseCsv(csvData);
            var htmlBody = GenerateHtmlBody(records, input.ImageData);

            await SendEmailAsync(htmlBody, context);

            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = JsonSerializer.Serialize(new { Message = "CSV e imagem processados e email enviado." }),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
        catch (Exception ex)
        {
            context.Logger.LogLine($"Erro processando a requisição: {ex.Message}");
            return new APIGatewayProxyResponse
            {
                StatusCode = 500,
                Body = JsonSerializer.Serialize(new { Message = "Erro processando a requisição." }),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
    }

    private List<FlorestalData> ParseCsv(string csvData)
    {
        using var reader = new StringReader(csvData);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        return csv.GetRecords<FlorestalData>().ToList();
    }

    private async Task SendEmailAsync(string htmlBody, ILambdaContext context)
    {
        using var client = new AmazonSimpleEmailServiceClient(Amazon.RegionEndpoint.SAEast1);

        var sendRequest = new SendEmailRequest
        {
            Source = SenderAddress,
            Destination = new Destination
            {
                ToAddresses = RecipientAddresses
            },
            Message = new Message
            {
                Subject = new Content($"Relatório Florestal - KPIM {DateTime.Now.ToString("dd/MM/yyyy")}"),
                Body = new Body
                {
                    Html = new Content
                    {
                        Charset = "UTF-8",
                        Data = htmlBody
                    }
                }
            }
        };

        try
        {
            context.Logger.LogLine("Sending email...");
            await client.SendEmailAsync(sendRequest);
            context.Logger.LogLine("Email sent successfully!");
        }
        catch (Exception ex)
        {
            context.Logger.LogLine($"Failed to send email: {ex.Message}");
            throw;
        }
    }

    private string GenerateHtmlBody(List<FlorestalData> data, string? imageBase64)
    {
        var sb = new StringBuilder();

        sb.Append(@"
            <!DOCTYPE html>
            <html lang='pt-BR'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Relatório Florestal KPIM</title>
                <style>
                    body {
                        font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif, 'Apple Color Emoji', 'Segoe UI Emoji', 'Segoe UI Symbol';
                        margin: 0;
                        padding: 0;
                        background-color: #f4f4f4;
                        color: #333;
                    }
                    .container {
                        max-width: 1024px;
                        margin: 10px auto;
                        background-color: #ffffff;
                        border-radius: 8px;
                        box-shadow: 0 2px 4px rgba(0,0,0,0.1);
                        overflow: hidden;
                    }
                    .header {
                        background-color: #004a2f;
                        color: #ffffff;
                        padding: 15px 20px;
                        text-align: center;
                    }
                    .header h1 {
                        margin: 0;
                        font-size: 22px;
                    }
                    .content {
                        padding: 20px;
                    }
                    .content p {
                        font-size: 15px;
                        line-height: 1.5;
                    }
                    .region-header {
                        background-color: #e9ecef;
                        padding: 10px 15px;
                        font-weight: bold;
                        font-size: 16px;
                        margin-top: 20px;
                        border-left: 3px solid #007a4d;
                    }
                    .table-responsive {
                        overflow-x: auto; 
                        -webkit-overflow-scrolling: touch;
                    }
                    table {
                        width: 100%;
                        border-collapse: collapse;
                        margin-top: 15px;
                        font-size: 13px;
                    }
                    th, td {
                        border: 1px solid #dee2e6;
                        padding: 4px 10px; /* Reduced vertical padding */
                        text-align: left;
                        vertical-align: middle;
                    }
                    thead th {
                        background-color: #007a4d;
                        color: #ffffff;
                        font-weight: 600;
                    }
                    tbody tr:nth-child(even) {
                        background-color: #f8f9fa;
                    }
                    .precip-cell {
                        font-size: 12px;
                    }
                    .precip-cell span {
                        padding: 1px 0; /* Reduced vertical padding */
                    }
                    .precip-cell .d1 {
                        font-weight: bold;
                        color: #004a2f; /* Darker green for 1-day */
                    }
                    .precip-cell .dvalor {
                        
                    }
                    
                    .kpi-cell {
                        text-align: center;
                        font-weight: bold;
                    }
                    .footer {
                        background-color: #f4f4f4;
                        color: #6c757d;
                        padding: 10px;
                        text-align: center;
                        font-size: 12px;
                        border-top: 1px solid #dee2e6;
                    }
                    .image-container {
                        text-align: center;
                        padding: 15px 0;
                    }
                    .image-container img {
                        max-width: 100%;
                        height: auto;
                        border-radius: 4px;
                    }
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Relatório do dia DATADEHOJE do KPI Meteorológico - KPIM</h1>
                    </div>
                    <div class='content'>
        ");

        if (!string.IsNullOrEmpty(imageBase64))
        {
            sb.Append($@"
                <div class='image-container'>
                    <img src='{imageBase64}' alt='Imagem do Relatório'>
                </div>
            ");
        }

        sb.Append($@"
                        <p>Prezados,</p>
                        <p>Segue o relatório de KPI Meteorológico (KPIM) atualizado para o dia <strong>{DateTime.Now.ToString("dd/MM/yyyy")}</strong>. Abaixo estão os dados detalhados por localidade.</p>
        ");

        var groupedData = data.GroupBy(d => d.Regiao).OrderBy(g => g.Key);

        foreach (var group in groupedData)
        {
            sb.Append($"<div class='region-header'>{group.Key}</div>");
            sb.Append("<div class='table-responsive'>");
            sb.Append(@"
                <table style='margin-top: 10px;'> <!-- Reduced top margin for table -->
                    <thead>
                        <tr>
                            <th>Prédio / Fazenda</th>
                            <th>Gleba</th>
                            <th>Precipitação (mm) nas próximas</th>
                            <th>KPIM</th>
                        </tr>
                    </thead>
                    <tbody>
            ");

            foreach (var row in group)
            {
                var kpi = row.KpiMeteorologico;
                string kpiCellStyle = "";

                if (kpi.HasValue)
                {
                    if (kpi > 3)
                    {
                        kpiCellStyle = "style='background-color: #FFC7CE; color: #9C0006;'"; // Light Red
                    }
                    else if (kpi > 2)
                    {
                        kpiCellStyle = "style='background-color: #FFEB9C; color: #9C6500;'"; // Light Yellow
                    }
                }

                sb.Append($"<tr {kpiCellStyle}>");
                sb.Append($"<td>{row.PredioFazenda}</td>");
                sb.Append($"<td>{row.Gleba}</td>");
                sb.Append("<td class='precip-cell'>");
                sb.Append($"<span class='d1'>24h: </span>");
                sb.Append($"<span class='dvalor'>{row.Precip_1_dia?.ToString("F0", CultureInfo.InvariantCulture) ?? "-"}</span>");
                sb.Append($"<span class='d1'> / 72h: </span>");
                sb.Append($"<span class='dvalor'>{row.Precip_3_dias?.ToString("F0", CultureInfo.InvariantCulture) ?? "-"}</span>");
                sb.Append($"<span class='d1'> / 5d: </span>");
                sb.Append($"<span class='dvalor'>{row.Precip_5_dias?.ToString("F0", CultureInfo.InvariantCulture) ?? "-"}</span>");
                sb.Append("</td>");
                sb.Append($"<td class='kpi-cell'>{kpi?.ToString("F1", CultureInfo.InvariantCulture)} - {row.Risco}</td>");
                sb.Append("</tr>");
            }

            sb.Append(@"
                    </tbody>
                </table>
            </div>
            ");
        }

        sb.Append(@"
                    </div>
                    <div class='footer'>
                        <p>Este é um e-mail gerado automaticamente. Por favor, não responda.</p>
                    </div>
                </div>
            </body>
            </html>
        ");

        return sb.ToString().Replace("DATADEHOJE", DateTime.Now.ToString("dd/MM/yyyy"));
    }
}
