using KvStore.Core.Application.Abstractions;
using KvStore.Core.Domain.Validation;

namespace KvStore.Core.Application.KeyValue.Commands.PutKeyValue;

public sealed class PutKeyValueCommandValidator : ICommandValidator<PutKeyValueCommand>
{
    public void Validate(PutKeyValueCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        KeyValidator.EnsureValid(command.Key);
    }
}

