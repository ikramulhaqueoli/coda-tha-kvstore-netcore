using KvStore.Api.Middleware;
using KvStore.Core.Application.Abstractions;
using KvStore.Core.Application.Commands;
using KvStore.Core.Application.Commands.PatchKeyValue;
using KvStore.Core.Application.Commands.PutKeyValue;
using KvStore.Core.Application.KeyValue.Responses;
using KvStore.Core.Application.Queries;
using KvStore.Core.Application.Queries.GetKeyValue;
using KvStore.Core.Application.Queries.ListKeys;
using KvStore.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

builder.Services.AddScoped<ICommandDispatcher, CommandDispatcher>();
builder.Services.AddScoped<IQueryDispatcher, QueryDispatcher>();
builder.Services.AddScoped<ICommandHandler<PutKeyValueCommand, KeyValueResponse>, PutKeyValueCommandHandler>();
builder.Services.AddScoped<ICommandHandler<PatchKeyValueCommand, KeyValueResponse>, PatchKeyValueCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetKeyValueQuery, KeyValueResponse>, GetKeyValueQueryHandler>();
builder.Services.AddScoped<IQueryHandler<ListKeysQuery, IReadOnlyCollection<string>>, ListKeysQueryHandler>();
builder.Services.AddScoped<ICommandValidator<PutKeyValueCommand>, PutKeyValueCommandValidator>();
builder.Services.AddScoped<ICommandValidator<PatchKeyValueCommand>, PatchKeyValueCommandValidator>();

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
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
