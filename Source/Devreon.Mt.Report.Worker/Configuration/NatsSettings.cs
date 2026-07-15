namespace Devreon.Mt.Report.Worker.Configuration;

public class NatsSettings
{
    public const string SectionName = "Nats";
    
    public required string Address { get; set; }
    public required string Environment { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
}
