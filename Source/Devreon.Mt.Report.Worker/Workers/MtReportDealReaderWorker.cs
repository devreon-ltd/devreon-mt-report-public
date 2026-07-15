using System.Text.Json;
using Devreon.Mt.Report.Contracts.Nats;
using Devreon.Mt.Report.Worker.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NATS.Client.Serializers.Json;

namespace Devreon.Mt.Report.Worker.Workers;

public class MtReportDealReaderWorker(
    IOptions<NatsSettings> settings) : BackgroundService
{
    private static readonly TimeSpan ExceptionsDelay = TimeSpan.FromSeconds(1);
    private readonly string _consumerName = $"mt_stream_{settings.Value.Environment}_deal-add";
    private readonly string _streamName = $"mt_stream_{settings.Value.Environment}";
    private readonly string _subjectFilter = $"mt.stream.{settings.Value.Environment}.deal.add";

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var jsonOpts = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true,
                };

                var registry = new NatsJsonContextOptionsSerializerRegistry(jsonOpts);
                jsonOpts.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

                var opts = NatsOpts.Default with
                {
                    Url = settings.Value.Address,
                    SerializerRegistry = registry,
                    AuthOpts = new NatsAuthOpts
                    {
                        Username = settings.Value.Username,
                        Password = settings.Value.Password
                    }
                };

                await using var nats = new NatsConnection(opts);
                await nats.ConnectAsync();

                var js = new NatsJSContext(nats);

                var consumer = await js.CreateOrUpdateConsumerAsync(
                    stream: _streamName,
                    config: new ConsumerConfig(_consumerName)
                    {
                        FilterSubject = _subjectFilter,
                        AckPolicy = ConsumerConfigAckPolicy.Explicit,
                        DeliverPolicy = ConsumerConfigDeliverPolicy.New,
                        MaxAckPending = 1000,
                        AckWait = TimeSpan.FromSeconds(30)
                    },
                    cancellationToken: ct);

                await foreach (var msg in consumer.ConsumeAsync<NatsReportDeal>(cancellationToken: ct))
                {
                    if (null != msg.Data)
                    {
                        ProcessDeal(msg.Data);
                    }
                    else if(null != msg.Error)
                    {
                        Console.WriteLine($"Error processing message: {msg.Error}");
                    }
                    await msg.AckAsync(cancellationToken: ct);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            await Task.Delay(ExceptionsDelay, ct);
        }
    }

    private void ProcessDeal(NatsReportDeal deal)
    {
        Console.WriteLine($"Deal: {deal.Ticket}");
    }
}
