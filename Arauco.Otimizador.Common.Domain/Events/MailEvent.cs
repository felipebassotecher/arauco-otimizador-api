using Arauco.Otimizador.Common.Domain.Enums;
using Newtonsoft.Json;

namespace Arauco.Otimizador.Common.Domain.Events;

public class MailEvent
{
    public AddressMailModel From { get; set; }
    public List<AddressMailModel> To { get; set; }
    public List<AddressMailModel> Cc { get; set; }
    public List<AddressMailModel> Bcc { get; set; }

    public List<AddressMailModel> ReplyTo { get; set; }
    public string InReplyTo { get; set; }

    public string Subject { get; set; }
    public string Text { get; set; }
    public string Html { get; set; }
    public Dictionary<string, string> Model;

    [JsonProperty("templateName")]
    public TemplateEmailEnum? TemplateEnum { get; set; }

    public List<FileMailModel> Attachments { get; set; }

    public MailEvent()
    {
        this.To = new List<AddressMailModel>();
        this.Cc = new List<AddressMailModel>();
        this.Bcc = new List<AddressMailModel>();
        this.ReplyTo = new List<AddressMailModel>();
        this.Attachments = new List<FileMailModel>();
    }

    public MailEvent(string from, string to, string subject, string html, string text = null) : this()
    {
        this.From = from;
        this.To.Add(to);
        this.Subject = subject;
        this.Html = html;
        this.Text = text;
    }

    public MailEvent(string from, string to, string subject, TemplateEmailEnum template, Dictionary<string, string> data) : this(from, to, subject, null, null)
    {
        this.TemplateEnum = template;
        this.Model = data;
    }

    public void Validate()
    {
        if (this.From.IsEmpty())
            throw new Exception("É necessário informar o remetente do e-mail");

        if (To.Count == 0 && Cc.Count == 0 && Bcc.Count == 0)
            throw new Exception("É necessário informar o destinatário do e-mail.");

        if (string.IsNullOrEmpty(Subject))
            throw new Exception("O assunto do e-mail não foi definido.");

        if (string.IsNullOrWhiteSpace(Text) &&
            string.IsNullOrWhiteSpace(Html) &&
            (!TemplateEnum.HasValue || Model == null))
            throw new Exception("O corpo do e-mail não foi definido.");
    }
}

public class AddressMailModel
{
    public string Name { get; set; }
    public string Address { get; set; }

    public AddressMailModel()
    {
    }

    public AddressMailModel(string name, string address)
    {
        this.Name = name;
        this.Address = address;
    }

    public AddressMailModel(string address)
    {
        this.Address = address;
    }

    public bool IsEmpty()
    {
        return string.IsNullOrWhiteSpace(this.Address);
    }

    public static implicit operator AddressMailModel(string s)
    {
        return new AddressMailModel(s);
    }
}

public class FileMailModel
{
    public string Bucket { get; set; }
    public string Key { get; set; }
    public string Filename { get; set; }
}
