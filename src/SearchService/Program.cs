using MassTransit;
using Polly;
using Polly.Extensions.Http;
using SearchService.Consumers;
using SearchService.Data;
using SearchService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddHttpClient<AuctionServiceHttpClient>().AddPolicyHandler(GetPolicy());
builder.Services.AddMassTransit(x => 
{
    x.AddConsumersFromNamespaceContaining<AuctionCreatedConsumer>();
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("search", false));
    x.UsingRabbitMq((context, cfg) =>
    {

        cfg.Host(builder.Configuration["RabbitMq:Host"], "/", host => 
       {
         host.Username(builder.Configuration.GetValue("RabbitMq:Username", "guest"));
         host.Password(builder.Configuration.GetValue("RabbitMq:Password", "guest"));
       });

        // When an exception occurs when trying to read an item from the rabbit queue, it retry to read it again and again.
       // Can be set per endpoint basis
       cfg.ReceiveEndpoint("search-auction-created", e =>
        {
        e.UseMessageRetry(r => r.Interval(5,5));
        e.ConfigureConsumer<AuctionCreatedConsumer>(context);
       });
       cfg.ConfigureEndpoints(context);
    });
});
var app = builder.Build();

app.MapControllers();

// we use this line to register a callback that get executed right after the app has started
app.Lifetime.ApplicationStarted.Register(async() => {
    try
{
    await DbInitializer.InitDb(app);
}
catch (Exception e)
{
    Console.WriteLine(e);
}
});
app.UseHttpsRedirection();
app.Run();

static IAsyncPolicy<HttpResponseMessage> GetPolicy() => HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
            .WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(3));
