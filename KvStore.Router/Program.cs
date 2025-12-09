using KvStore.Router.Clients;
using KvStore.Router.Nodes;
using KvStore.Router.Options;
using KvStore.Router.Partitioning;
using KvStore.Router.Services;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var environment = builder.Environment;
const string KubernetesNodeOverridesSection = "KvStoreApiNodes";

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

builder.Services.Configure<KvStoreNodesOptions>(configuration.GetSection(KvStoreNodesOptions.SectionName));
builder.Services.PostConfigure<KvStoreNodesOptions>(options =>
{
    var kubernetesOverrides = configuration.GetSection(KubernetesNodeOverridesSection);
    if (!kubernetesOverrides.Exists())
    {
        return;
    }

    if (kubernetesOverrides.GetValue<int?>("Port") is { } port)
    {
        options.Port = port;
    }

    if (kubernetesOverrides.GetValue<int?>("ReplicaCount") is { } replicaCount)
    {
        options.ReplicaCount = replicaCount;
    }

    if (kubernetesOverrides.GetValue<string>("Namespace") is { } namespaceValue)
    {
        options.Namespace = namespaceValue;
    }
});
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
