using Amazon.SQS;
using Amazon.SQS.Model;
using Techer.Aws.Shared;
using Techer.Common.Domain.Interfaces;
using Techer.Common.Extensions;

namespace Techer.Aws.Queue;

public class SqsHelper
{
    private AmazonSQSClient sqsClient;
    private string queueUrl;

    public SqsHelper(string region, string queueUrl)
    {
        this.Initiate(region, queueUrl);
    }

    public SqsHelper(AwsUrlResource resource, IEnvironmentVariables env)
    {
        Initiate(resource.Region, resource.Get(env.GetEnvironmentEnum()));
    }

    private void Initiate(string region, string queueUrl)
    {
        var awsRegion = Amazon.RegionEndpoint.GetBySystemName(region);

        this.sqsClient = new AmazonSQSClient(awsRegion);
        this.queueUrl = queueUrl;
    }

    public async Task<string> SendMessageFifo(string messageBody, string messageGroupId, string messageDeduplicationId, Dictionary<string, string> messageAttributes = null)
    {
        return await SendMessage(messageBody, messageGroupId, messageDeduplicationId, 0, messageAttributes);
    }

    public async Task<string> SendMessageStandard(string messageBody, int delaySeconds, Dictionary<string, string> messageAttributes = null)
    {
        return await SendMessage(messageBody, null, null, delaySeconds, messageAttributes);
    }

    public async Task<string> SendMessage(string messageBody, string messageGroupId, string messageDeduplicationId, int delaySeconds, Dictionary<string, string> messageAttributes)
    {
        if (string.IsNullOrEmpty(this.queueUrl))
            throw new Exception("URL da fila inválido.");

        if (string.IsNullOrEmpty(messageBody))
            throw new Exception("Corpo da mensagem inválido.");

        var request = new SendMessageRequest
        {
            QueueUrl = this.queueUrl,
            MessageGroupId = messageGroupId,
            MessageDeduplicationId = messageDeduplicationId,
            MessageBody = messageBody,
            DelaySeconds = delaySeconds,
            MessageAttributes = new Dictionary<string, MessageAttributeValue>()
        };

        if (messageAttributes != null)
        {
            foreach (var item in messageAttributes)
            {
                var messageTypeAttribute = new MessageAttributeValue()
                {
                    DataType = "String",
                    StringValue = item.Value
                };

                request.MessageAttributes.Add(item.Key, messageTypeAttribute);
            }
        }

        var res = await sqsClient.SendMessageAsync(request);

        return res.MessageId;
    }

    public async Task SendMessageBatchAsync(List<string> messages)
    {
        if (string.IsNullOrEmpty(this.queueUrl))
            throw new Exception("URL da fila inválido.");

        var listSqs = new List<SendMessageBatchRequestEntry>();

        messages.ForEach(m => listSqs.Add(new SendMessageBatchRequestEntry(Guid.NewGuid().ToString(), m)));

        foreach (var batch in listSqs.Batch(10))
        {
            var request = new SendMessageBatchRequest
            {
                QueueUrl = this.queueUrl,
                Entries = batch.ToList()
            };

            var result = await sqsClient.SendMessageBatchAsync(request);

            if (result.HttpStatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception(result.HttpStatusCode.ToString());
        }
    }

    public async Task<IList<Models.SqsMessage>> ReceiveMessage(List<string> messageAttributes, int maxNumberOfMessages = 1, int waitTimeSeconds = 0, bool deleteMessage = true, List<string> attributeNames = null)
    {
        var res = new List<Models.SqsMessage>();

        var receiveMessageRequest = new ReceiveMessageRequest
        {
            QueueUrl = this.queueUrl,
            MaxNumberOfMessages = maxNumberOfMessages,
            WaitTimeSeconds = waitTimeSeconds
        };

        if (messageAttributes != null)
        {
            receiveMessageRequest.MessageAttributeNames = new List<string>();

            foreach (var item in messageAttributes)
            {
                receiveMessageRequest.MessageAttributeNames.Add(item);
            }
        }

        if (attributeNames != null)
        {
            receiveMessageRequest.AttributeNames = new List<string>();

            foreach (var item in attributeNames)
            {
                receiveMessageRequest.AttributeNames.Add(item);
            }
        }

        var receiveMessageResponse = await sqsClient.ReceiveMessageAsync(receiveMessageRequest);

        if (receiveMessageResponse.Messages.Any())
        {
            foreach (var message in receiveMessageResponse.Messages)
            {
                var sqsMessage = new Models.SqsMessage
                {
                    Body = message.Body,
                    MessageId = message.MessageId,
                    ReceiptHandle = message.ReceiptHandle
                };

                if (message.MessageAttributes != null && message.MessageAttributes.Any())
                {
                    sqsMessage.MessageAttributes = message.MessageAttributes.ToDictionary(m => m.Key, m => m.Value.StringValue);
                }

                if (message.Attributes != null && message.Attributes.Any())
                {
                    sqsMessage.Attributes = message.Attributes.ToDictionary(m => m.Key, m => m.Value);
                }

                res.Add(sqsMessage);

                // Deletar mensagem para impedir que outro request a obtenha
                if (deleteMessage)
                {
                    await this.DeleteMessage(message.ReceiptHandle);
                }
            }
        }

        return res;
    }

    public async Task DeleteMessage(string messageReceiptHandle)
    {
        DeleteMessageRequest deleteRequest = new DeleteMessageRequest
        {
            QueueUrl = this.queueUrl,
            ReceiptHandle = messageReceiptHandle
        };

        var res = await sqsClient.DeleteMessageAsync(deleteRequest);

        if (res.HttpStatusCode != System.Net.HttpStatusCode.OK)
            throw new Exception("Não foi possível deletar a mensagem.");
    }

    public static async Task<string> SendMessageFifo(IEnvironmentVariables env, AwsUrlResource queue, string messageGroupId, string messageDeduplicationId, string messageBody, Dictionary<string, string> messageAttributes = null)
    {
        var helper = new SqsHelper(queue, env);

        return await helper.SendMessageFifo(
            messageBody,
            messageGroupId,
            messageDeduplicationId,
            messageAttributes);
    }

    public static async Task<string> SendMessageStandard(IEnvironmentVariables env, AwsUrlResource queue, string messageBody, int delaySeconds, Dictionary<string, string> messageAttributes = null)
    {
        var helper = new SqsHelper(queue, env);

        return await helper.SendMessageStandard(
            messageBody,
            delaySeconds,
            messageAttributes);
    }
}
