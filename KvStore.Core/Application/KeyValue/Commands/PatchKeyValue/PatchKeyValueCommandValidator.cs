using KvStore.Core.Domain.Exceptions;
using KvStore.Core.Domain.Validation;

namespace KvStore.Core.Application.KeyValue.Commands.PatchKeyValue;

public static class PatchKeyValueCommandValidator
{
    public static void Validate(PatchKeyValueCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        KeyValidator.EnsureValid(command.Key);

        if (command.Delta is null)
        {
            throw new InvalidPatchDeltaException();
        }
    }
}

