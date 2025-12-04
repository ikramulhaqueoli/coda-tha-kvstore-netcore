namespace KvStore.Router.Nodes;

public sealed record NodeDefinition(string Id, Uri BaseAddress)
{
    public override string ToString() => $"{Id} ({BaseAddress})";
}

