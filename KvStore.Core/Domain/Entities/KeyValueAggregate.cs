using System.Text.Json.Nodes;
using KvStore.Core.Domain.Exceptions;
using KvStore.Core.Domain.Validation;

namespace KvStore.Core.Domain.Entities;

public sealed class KeyValueAggregate
{
    private KeyValueAggregate(string key, JsonNode? value, int version)
    {
        KeyValidator.EnsureValid(key);
        Key = key;
        Value = value?.DeepClone();
        Version = version;
    }

    public string Key { get; }
    public JsonNode? Value { get; private set; }
    public int Version { get; private set; }

    public static KeyValueAggregate Create(string key, JsonNode? value)
        => new(key, value, 1);

    public KeyValueAggregate Clone()
    {
        return new KeyValueAggregate(Key, Value?.DeepClone(), Version);
    }

    public void Replace(JsonNode? newValue, int? expectedVersion)
    {
        EnsureVersion(expectedVersion);
        Value = newValue?.DeepClone();
        Version += 1;
    }

    public void Merge(JsonNode? delta, int? expectedVersion)
    {
        EnsureVersion(expectedVersion);

        if (Value is JsonObject existingObj && delta is JsonObject deltaObj)
        {
            var merged = (JsonObject)existingObj.DeepClone();
            foreach (var kvp in deltaObj)
            {
                merged[kvp.Key] = kvp.Value?.DeepClone();
            }

            Value = merged;
        }
        else
        {
            Value = delta?.DeepClone();
        }

        Version += 1;
    }

    public KeyValueResponseSnapshot ToSnapshot()
    {
        return new KeyValueResponseSnapshot(Key, Value?.DeepClone(), Version);
    }

    private void EnsureVersion(int? expectedVersion)
    {
        if (expectedVersion.HasValue && Version != expectedVersion.Value)
        {
            throw new VersionMismatchException(Key, expectedVersion.Value, Version);
        }
    }
}

public sealed record KeyValueResponseSnapshot(string Key, JsonNode? Value, int Version);

