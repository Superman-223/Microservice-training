using Polly;
using Polly.Extensions.Http;
using SearchService.Data;
using SearchService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services.AddHttpClient<AuctionServiceHttpClient>().AddPolicyHandler(GetPolicy());

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
