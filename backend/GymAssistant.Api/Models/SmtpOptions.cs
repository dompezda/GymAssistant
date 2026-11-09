public class SmtpOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }

    public string FromEmail {get;set;}
    public string FromName {get;set;}
    public bool UseSsl {get;set;} = false;
}