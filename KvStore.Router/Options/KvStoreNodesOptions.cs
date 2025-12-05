using System.ComponentModel.DataAnnotations;

namespace KvStore.Router.Options;

public sealed class KvStoreNodesOptions
{
    public const string SectionName = "KvStoreNodes";
    public const string DefaultHostTemplate = "{nodeId}.{headlessServiceName}.{namespace}.{clusterDomain}";

    private const string DefaultScheme = "http";
    private const string DefaultNamespace = "default";
    private const string DefaultClusterDomain = "svc.cluster.local";

    [Required]
    public string ServiceName { get; init; } = string.Empty;

    [Required]
    public string HeadlessServiceName { get; init; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; set; } = 7000;

    [Range(1, int.MaxValue)]
    public int ReplicaCount { get; set; } = 1;

    public string Scheme { get; init; } = DefaultScheme;

    public string Namespace { get; init; } = DefaultNamespace;

    public string ClusterDomain { get; init; } = DefaultClusterDomain;

    public string HostTemplate { get; init; } = DefaultHostTemplate;
}

