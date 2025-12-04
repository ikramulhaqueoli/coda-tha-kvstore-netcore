using KvStore.Router.Models;

namespace KvStore.Router.Services;

public interface IKeyListingService
{
    Task<IReadOnlyList<KeyListingRecord>> ListAsync(CancellationToken cancellationToken);
}

