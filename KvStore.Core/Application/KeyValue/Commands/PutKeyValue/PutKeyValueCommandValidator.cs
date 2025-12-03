using KvStore.Core.Domain.Validation;

namespace KvStore.Core.Application.KeyValue.Commands.PutKeyValue;

public static class PutKeyValueCommandValidator
{
    public static void Validate(PutKeyValueCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        KeyValidator.EnsureValid(command.Key);
    }
}

