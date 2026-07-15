using System.Reflection;
using Devreon.Mt.Report.Worker.Configuration;
using Devreon.Mt.Report.Worker.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;

namespace Devreon.Mt.Report.Worker;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);

        if (WindowsServiceHelpers.IsWindowsService())
        {
            Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            builder.Services.AddWindowsService();
        }

        ConfigureServices(builder.Services, builder.Configuration);
        var host = builder.Build();
        await host.RunAsync();
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<NatsSettings>(configuration.GetSection(NatsSettings.SectionName).Bind);
        services.AddHostedService<MtReportDealReaderWorker>();
    }
}
