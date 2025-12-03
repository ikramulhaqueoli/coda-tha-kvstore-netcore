using System.Text.Json.Nodes;
using KvStore.Core.Domain.Exceptions;

namespace KvStore.Core.Domain.Entities;

public sealed class KeyValueAggregate
{
    private KeyValueAggregate(string key, JsonNode? value, long version)
    {
        Key = key;
        Value = value?.DeepClone();
        Version = version;
    }

    public string Key { get; }
    public JsonNode? Value { get; private set; }
    public long Version { get; private set; }

    public static KeyValueAggregate Create(string key, JsonNode? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return new KeyValueAggregate(key, value, 1);
    }

    public KeyValueAggregate Clone()
    {
        return new KeyValueAggregate(Key, Value?.DeepClone(), Version);
    }

    public void Replace(JsonNode? newValue, long? expectedVersion)
    {
        EnsureVersion(expectedVersion);
        Value = newValue?.DeepClone();
        Version += 1;
    }

    public void Merge(JsonNode? delta, long? expectedVersion)
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

    private void EnsureVersion(long? expectedVersion)
    {
        if (!expectedVersion.HasValue)
        {
            return;
        }

        if (Version != expectedVersion.Value)
        {
            throw new VersionMismatchException(Key, expectedVersion.Value, Version);
        }
    }
}

public sealed record KeyValueResponseSnapshot(string Key, JsonNode? Value, long Version);

