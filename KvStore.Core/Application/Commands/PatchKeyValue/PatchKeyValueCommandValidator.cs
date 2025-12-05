using KvStore.Core.Application.Abstractions;
using KvStore.Core.Domain.Exceptions;
using KvStore.Core.Domain.Validation;

namespace KvStore.Core.Application.Commands.PatchKeyValue;

public sealed class PatchKeyValueCommandValidator : ICommandValidator<PatchKeyValueCommand>
{
    public void Validate(PatchKeyValueCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        KeyValidator.EnsureValid(command.Key);

        if (command.Delta is null)
        {
            throw new InvalidPatchDeltaException();
        }
    }
}

