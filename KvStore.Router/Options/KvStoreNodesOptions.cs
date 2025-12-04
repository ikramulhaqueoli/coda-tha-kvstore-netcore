using System.ComponentModel.DataAnnotations;

namespace KvStore.Router.Options;

public sealed class KvStoreNodesOptions
{
    public const string SectionName = "KvStoreNodes";

    [Required]
    public IList<NodeOption> Nodes { get; init; } = new List<NodeOption>();

    public sealed class NodeOption
    {
        private const string DefaultScheme = "http";

        [Required]
        public string Id { get; init; } = string.Empty;

        [Required]
        public string Host { get; init; } = string.Empty;

        [Range(1, 65535)]
        public int Port { get; init; } = 7000;

        public string Scheme { get; init; } = DefaultScheme;

        public Uri BuildBaseAddress()
        {
            if (string.IsNullOrWhiteSpace(Host))
            {
                throw new ValidationException("Host must be provided for every node.");
            }

            var scheme = string.IsNullOrWhiteSpace(Scheme) ? DefaultScheme : Scheme;
            return new Uri($"{scheme}://{Host}:{Port}/", UriKind.Absolute);
        }
    }
}

