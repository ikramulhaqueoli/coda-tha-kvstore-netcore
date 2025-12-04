using KvStore.Router.Clients;
using KvStore.Router.Nodes;
using KvStore.Router.Options;
using KvStore.Router.Partitioning;
using KvStore.Router.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

builder.Services.Configure<KvStoreNodesOptions>(builder.Configuration.GetSection(KvStoreNodesOptions.SectionName));
builder.Services.AddSingleton<INodeRegistry, NodeRegistry>();
builder.Services.AddSingleton<IKeyPartitioner, ConsistentHashPartitioner>();
builder.Services.AddSingleton<IKvStoreNodeClient, KvStoreNodeClient>();
builder.Services.AddSingleton<IKeyValueForwardingService, KeyValueForwardingService>();
builder.Services.AddSingleton<IKeyListingService, KeyListingService>();
builder.Services.AddHttpClient(KvStoreNodeClient.HttpClientName);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
