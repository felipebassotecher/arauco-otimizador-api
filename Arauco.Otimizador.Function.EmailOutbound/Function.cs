using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Arauco.Otimizador.Common.Domain.Events;
using Mustache;
using System.Reflection;
using Techer.Common.Extensions;
using Techer.Common.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Arauco.Otimizador.Function.EmailOutbound;

public class Function
{
    private static FormatCompiler formatCompiler = new FormatCompiler();

    public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
    {
        foreach (var message in evnt.Records)
        {
            await ProcessMessageAsync(message, context);
        }
    }

    private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
    {
        context.Logger.LogLine($"E-Mail {message.Body}");

        var model = JsonHelper.Deserialize<MailEvent>(message.Body);

        // Destiny
        var destination = new Destination();

        if (model.To != null)
            destination.ToAddresses.AddRange(model.To.Select(m => m.Address));

        if (model.Cc != null)
            destination.CcAddresses.AddRange(model.Cc.Select(m => m.Address));

        if (model.Bcc != null)
            destination.BccAddresses.AddRange(model.Bcc.Select(m => m.Address));

        // Body
        string? html = null;
        string? text = null;

        if (model.TemplateEnum.HasValue)
        {
            var assembly = Assembly.GetExecutingAssembly();

            var resourceName = assembly
                .GetManifestResourceNames()
                .FirstOrDefault(str => str.EndsWith($"{model.TemplateEnum.Value.GetEnumMemberValue()}.html"));

            Console.WriteLine($"Resource = {resourceName}");

            if (resourceName != null)
            {
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    html = await reader.ReadToEndAsync();
                }
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(model.Html))
                html = model.Html;

            if (!string.IsNullOrWhiteSpace(model.Text))
                text = model.Text;
        }

        if (model.Model != null)
        {
            if (!string.IsNullOrWhiteSpace(html))
            {
                var generator = formatCompiler.Compile(html);
                html = generator.Render(model.Model);
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                var generator = formatCompiler.Compile(text);
                text = generator.Render(model.Model);
            }
        }

        // Subject
        var title = new Content(model.Subject);

        var body = new Body();

        if (html != null)
            body.Html = new Content(html);

        if (text != null)
            body.Text = new Content(text);

        var msg = new Message(title, body);

        var mailRequest = new SendEmailRequest(model.From.Address, destination, msg);

        if (model.ReplyTo != null)
            mailRequest.ReplyToAddresses.AddRange(model.ReplyTo.Select(s => s.Address));

        using (var sesClient = new AmazonSimpleEmailServiceClient(Amazon.RegionEndpoint.SAEast1))
        {
            context.Logger.LogLine(JsonHelper.Serialize(mailRequest));

            var mailResponse = await sesClient.SendEmailAsync(mailRequest);

            context.Logger.LogLine($"MessageId = {mailResponse.MessageId}");
        }
    }
}