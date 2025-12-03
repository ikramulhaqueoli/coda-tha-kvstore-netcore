using KvStore.Api.Middleware;
using KvStore.Core.Application.Abstractions;
using KvStore.Core.Application.Dispatchers;
using KvStore.Core.Application.KeyValue.Commands.PatchKeyValue;
using KvStore.Core.Application.KeyValue.Commands.PutKeyValue;
using KvStore.Core.Application.KeyValue.Queries.GetKeyValue;
using KvStore.Core.Application.KeyValue.Responses;
using KvStore.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<ICommandDispatcher, CommandDispatcher>();
builder.Services.AddScoped<IQueryDispatcher, QueryDispatcher>();
builder.Services.AddScoped<ICommandHandler<PutKeyValueCommand, KeyValueResponse>, PutKeyValueCommandHandler>();
builder.Services.AddScoped<ICommandHandler<PatchKeyValueCommand, KeyValueResponse>, PatchKeyValueCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetKeyValueQuery, KeyValueResponse>, GetKeyValueQueryHandler>();

builder.Services.AddKvStoreInfrastructure();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
