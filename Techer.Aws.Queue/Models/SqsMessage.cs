namespace Techer.Aws.Queue.Models;

public class SqsMessage
{
    public string MessageId { get; set; }
    public string Body { get; set; }
    public string ReceiptHandle { get; set; }
    public Dictionary<string, string> MessageAttributes { get; set; }
    public Dictionary<string, string> Attributes { get; set; }
}
