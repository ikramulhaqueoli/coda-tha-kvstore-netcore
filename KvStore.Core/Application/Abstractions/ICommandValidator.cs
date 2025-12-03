namespace KvStore.Core.Application.Abstractions;

public interface ICommandValidator<in TCommand>
{
    void Validate(TCommand command);
}

