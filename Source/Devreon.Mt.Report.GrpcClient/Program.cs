using Devreon.Mt.Report.Contracts.Grpc;
using Devreon.Mt.Report.GrpcClient.Configuration;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Devreon.Mt.Report.GrpcClient;

public class Program
{
    private const long Login = 12345;

    public static async Task Main(string[] args)
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);
        var settings = builder.Configuration.GetSection(GrpcSettings.SectionName).Get<GrpcSettings>()!;
        builder.Services.AddGrpcClient<MtCacheService.MtCacheServiceClient>(x => x.Address = new Uri(settings.Address));
        using var host = builder.Build();

        var client = host.Services.GetRequiredService<MtCacheService.MtCacheServiceClient>();
        // every request passes the token of the GRPC server in the "token" header
        var headers = new Metadata { { "token", settings.Token } };
        var request = new GetAccountInfoRequest { Login = Login };

        try
        {
            var info = await client.GetAccountInfoAsync(request, headers);
            if (!info.Found)
            {
                Console.WriteLine($"Account {Login} is not found");
                return;
            }

            Console.WriteLine($"Account {Login}: balance = {info.Balance} {info.Currency}, credit = {info.Credit}, profit = {info.Profit}, margin = {info.Margin}, leverage = {info.Leverage}");

            var state = await client.GetAccountStateAsync(request, headers);
            foreach (var position in state.Positions)
            {
                // the size is negative for sell positions
                Console.WriteLine($"Position {position.Id}: {position.Symbol}, size = {position.Size}, price = {position.Price}, profit = {position.Profit}");
            }

            foreach (var order in state.Orders)
            {
                // type: 1 - limit, 2 - stop; side: 1 - buy, 2 - sell
                Console.WriteLine($"Order {order.Id}: {order.Symbol}, type = {order.Type}, side = {order.Side}, size = {order.Size}, price = {order.Price}");
            }
        }
        catch (RpcException ex)
        {
            Console.WriteLine($"Request failed: {ex.Status}");
        }
    }
}
