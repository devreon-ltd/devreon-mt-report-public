namespace Devreon.Mt.Report.GrpcClient.Configuration;

public class GrpcSettings
{
    public const string SectionName = "Grpc";

    public required string Address { get; set; }
    public required string Token { get; set; }
}
